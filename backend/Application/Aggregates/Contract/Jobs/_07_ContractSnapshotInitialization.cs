using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.EventLogs;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using Application.Resilience;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Application.Aggregates.Contract.Jobs;

public class _07_ContractSnapshotInitialization : IStatelessJob
{
    private readonly IDbContextFactory<GraphQlDbContext> _contextFactory;
    private readonly ContractAggregateOptions _options;
    private readonly ILogger _logger;

    /// <summary>
    /// WARNING - Do not change this if job already executed on environment, since it will trigger rerun of job.
    /// </summary>
    private const string JobName = "_07_ContractSnapshotInitialization";

    /// <inheritdoc/>
    public string GetUniqueIdentifier() => JobName;

    /// <inheritdoc/>
    public async Task<IEnumerable<int>> GetIdentifierSequence(CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var max = context.Contract.Max(ce => ce.ContractAddressIndex);
        return Enumerable.Range(0, (int)max + 1);
    }

    /// <inheritdoc/>
    public ValueTask Setup(CancellationToken token = default) => ValueTask.CompletedTask;

    /// <inheritdoc/>
    public async ValueTask Process(int identifier, CancellationToken token = default)
    {
        _logger.Debug($"Start processing {identifier}");
        await Policies.GetTransientPolicy(GetUniqueIdentifier(), _logger, _options.RetryCount, _options.RetryDelay)
            .ExecuteAsync(async () =>
            {
                var contractAddress = new ContractAddress((ulong)identifier, 0);
                
                await using var context = await _contextFactory.CreateDbContextAsync(token);
                var connection = context.Database.GetDbConnection();
                
                var parameter = new { Index = (long)contractAddress.Index, Subindex = 0};
                var contractEvents = (await connection.QueryAsync<ContractEvent>(ContractEventsSql, parameter))?.ToList();
                if (contractEvents == null || contractEvents.Count == 0)
                {
                    return;
                }
                
                var blockHeight = contractEvents.Last().BlockHeight;
                
                var contractEvent = contractEvents
                    .First(e => e.Event is ContractInitialized);
                var contractName = (contractEvent.Event as ContractInitialized)!.GetName();

                var amount = ContractSnapshot.GetAmount(contractEvents, contractAddress, 0);

                var link = await context.ModuleReferenceContractLinkEvents
                    .Where(m => m.ContractAddressIndex == contractAddress.Index && m.LinkAction == ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
                    .OrderByDescending(m => m.BlockHeight)
                    .ThenByDescending(m => m.TransactionIndex)
                    .ThenByDescending(m => m.EventIndex)
                    .FirstAsync(cancellationToken: token);

                var contractSnapshot = new ContractSnapshot(
                    blockHeight,
                    contractAddress,
                    contractName,
                    link.ModuleReference,
                    amount,
                    ImportSource.DatabaseImport
                );

                await context.AddAsync(contractSnapshot, token);
                await context.SaveChangesAsync(token);
            });
        _logger.Debug($"Completed successfully processing {identifier}");
    }

    /// <inheritdoc/>
    public bool ShouldNodeImportAwait() => true;

    private const string ContractEventsSql = @"
    SELECT 
        g0.block_height as BlockHeight,
        g0.transaction_index as TransactionIndex,
        g0.event_index as EventIndex,
        g0.contract_address_index as ContractAddressIndex,
        g0.contract_address_subindex as ContractAddressSubIndex,
        g0.block_slot_time as BlockSlotTime,
        g0.created_at as CreatedAt,
        g0.event as Event,
        g0.sender as Creator,
        g0.source as Source,
        g0.transaction_hash as TransactionHash
    FROM graphql_contract_events AS g0
    WHERE (g0.contract_address_index = @Index) AND (g0.contract_address_subindex = @Subindex)
    ORDER BY g0.block_height, g0.transaction_index, g0.event_index;
";        
}
