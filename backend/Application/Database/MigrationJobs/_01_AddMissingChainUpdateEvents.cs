using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Transactions;
using Application.Exceptions;
using Application.Import.ConcordiumNode;
using Application.Observability;
using Concordium.Sdk.Types;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Database.MigrationJobs;

/// <summary>
/// Some transaction events hasn't been mapped to the database. Those missing are
/// <see cref="Concordium.Sdk.Types.UpdateType"/> of one of below values
/// - <see cref="Concordium.Sdk.Types.UpdateType.GasRewardsCpv2Update"/>
/// - <see cref="Concordium.Sdk.Types.UpdateType.TimeoutParametersUpdate"/>
/// - <see cref="Concordium.Sdk.Types.UpdateType.MinBlockTimeUpdate"/>
/// - <see cref="Concordium.Sdk.Types.UpdateType.BlockEnergyLimitUpdate"/>
/// - <see cref="Concordium.Sdk.Types.UpdateType.FinalizationCommitteeParametersUpdate"/>
///
/// The events are mapped to <see cref="ChainUpdateEnqueued"/>. This migration job adds missing events.
///
/// Even though transaction events for these cases hasn't been mapped the event type has been set on the
/// <see cref="Transaction"/> entity on property <see cref="Transaction.TransactionType"/>. The jobs starts by
/// querying the transaction table for those transaction with is of one of the missing event types. 
/// 
/// The mapping between the <see cref="Transaction.TransactionType"/> and the string value stored in the database
/// is present at <see cref="Application.Api.GraphQL.EfCore.Converters.EfCore.TransactionTypeToStringConverter"/>.
///  
/// There is an one-to-one relation between transaction and events when the transaction is of type
/// <see cref="Concordium.Sdk.Types.UpdateDetails"/>. Hence the job is idempotent because it checks if any transaction
/// event for the given transaction already exist, and only generates an event if none is present.
/// </summary>
public class _01_AddMissingChainUpdateEvents : IMainMigrationJob {
    /// <summary>
    /// WARNING - Do not change this if job already executed on environment, since it will trigger rerun of job.
    /// </summary>
    private const string JobName = "_00_FixValidatorCommissionRates";
    
    /// <summary>
    /// The mapping between the <see cref="Transaction.TransactionType"/> and the string value stored in the database
    /// is present at <see cref="Application.Api.GraphQL.EfCore.Converters.EfCore.TransactionTypeToStringConverter"/>.
    /// </summary>
    private const string AffectedTransactionTypesSql = @"
SELECT id as Id, block_id as BlockId, index as TransactionIndex, transaction_hash as TransactionHash
FROM graphql_transactions
WHERE transaction_type IN ('2.22', '2.21', '2.20', '2.19', '2.18');
";    
    
    private readonly IDbContextFactory<GraphQlDbContext> _contextFactory;
    private readonly IConcordiumNodeClient _client;
    private readonly JobHealthCheck _jobHealthCheck;
    private readonly ILogger _logger;
    private readonly MainMigrationJobOptions _mainMigrationJobOptions;
    
    public _01_AddMissingChainUpdateEvents(
        IDbContextFactory<GraphQlDbContext> contextFactory,
        IConcordiumNodeClient client,
        JobHealthCheck jobHealthCheck,
        IOptions<MainMigrationJobOptions> options
    )
    {
        _contextFactory = contextFactory;
        _client = client;
        _jobHealthCheck = jobHealthCheck;
        _logger = Log.ForContext<_00_UpdateValidatorCommissionRates>();
        _mainMigrationJobOptions = options.Value;
    }
    
    /// <summary>
    /// Start import of missing transaction events.
    /// </summary>
    /// <exception cref="Exception"></exception>
    public async Task StartImport(CancellationToken token)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token);
        var connection = context.Database.GetDbConnection();
        
        var transactions = await connection.QueryAsync<Transaction>(AffectedTransactionTypesSql);
        
        foreach (var transaction in transactions)
        {
            var count = await context.TransactionResultEvents
                .Where(te => te.TransactionId == transaction.Id)
                .CountAsync(cancellationToken: token);
            // Conditions true since one to one relation between transaction and update event.
            if (count > 0)
            {
                continue;
            }

            var blockItemStatus = await _client.GetBlockItemStatusAsync(TransactionHash.From(transaction.TransactionHash), token);

            if (blockItemStatus is not TransactionStatusFinalized finalized)
            {
                throw JobException.Create(GetUniqueIdentifier(),
                    $"Transaction was of wrong type {blockItemStatus.GetType()}");
            }
            if (finalized.State.Summary.Details is not UpdateDetails updateDetails)
            {
                throw JobException.Create(GetUniqueIdentifier(),
                    $"Transaction details was of wrong type {finalized.State.Summary.Details.GetType()}");
            }

            var block = await context
                .Blocks
                .SingleAsync(b => b.Id == transaction.BlockId, cancellationToken: token);

            var chainUpdateEnqueued = ChainUpdateEnqueued.From(updateDetails, block.BlockSlotTime);


            var transactionRelated = new TransactionRelated<TransactionResultEvent>(transaction.Id, 0, chainUpdateEnqueued);
            await context.TransactionResultEvents.AddAsync(transactionRelated, token);
            await context.SaveChangesAsync(token);
        }
    }

    public string GetUniqueIdentifier() => JobName;

    public bool ShouldNodeImportAwait() => false;
}
