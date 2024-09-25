using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Dto;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.EventLogs;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using Application.Resilience;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Aggregates.Contract.Jobs;

/// <summary>
/// The job starts by truncating the tables graphql_account_tokens, graphql_token_events and graphql_tokens. Reinitialization
/// is needed because mint events was not correctly updating account token balances.
///
/// For each contract those contract actions, which generates log events, are processed
/// (contract initialization, contract interrupted and contract updated).
///
/// Each log event is checked if it should be parsed, see <see cref="CisEvent"/>. If the contract has a linked
/// schema and a successfully human interpretable log event linked, the human interpretable log event is linked to
/// the event. This may contain additional data.
/// </summary>
public sealed class _05_CisEventReinitialization : IStatelessJob
{
    private readonly IDbContextFactory<GraphQlDbContext> _contextFactory;
    private readonly IEventLogWriter _writer;
    private readonly ContractAggregateOptions _options;
    private readonly ILogger _logger;

    /// <summary>
    /// WARNING - Do not change this if job already executed on environment, since it will trigger rerun of job.
    /// </summary>
    private const string JobName = "_05_CisEventReinitialization";

    public _05_CisEventReinitialization(
        IDbContextFactory<GraphQlDbContext> contextFactory,
        IEventLogWriter writer,
        IOptions<ContractAggregateOptions> options)
    {
        _contextFactory = contextFactory;
        _writer = writer;
        _options = options.Value;
        _logger = Log.ForContext<_05_CisEventReinitialization>();
    }

    /// <inheritdoc/>
    public string GetUniqueIdentifier() => JobName;

    /// <inheritdoc/>
    public async Task<IEnumerable<int>> GetIdentifierSequence(CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var max = await context.ContractEvents.AnyAsync(cancellationToken: cancellationToken) ? context.ContractEvents.Max(ce => ce.ContractAddressIndex) + 1 : 0;
        return Enumerable.Range(0, (int)max);
    }

    /// <inheritdoc/>
    public async ValueTask Setup(CancellationToken token)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token);
        var connection = context.Database.GetDbConnection();
        await connection.ExecuteAsync("truncate table graphql_account_tokens, graphql_token_events, graphql_tokens;");
    }

    /// <inheritdoc/>
    public async ValueTask Process(int identifier, CancellationToken token = default)
    {
        _logger.Debug($"Start processing {identifier}");
        await Policies.GetTransientPolicy(GetUniqueIdentifier(), _logger, _options.RetryCount, _options.RetryDelay)
            .ExecuteAsync(async () =>
            {
                try
                {
                    var contractEvents = await GetOrderedContractEvents(identifier, token);
                    var jobContractRepository = new JobContractRepository(contractEvents);
                    var (cisEventTokenUpdates, tokenEvents, cisAccountUpdates) = EventLogHandler.GetParsedTokenUpdate(jobContractRepository);
                    var optimizeCisEventTokenUpdate = OptimizeCisEventTokenUpdate(cisEventTokenUpdates);
                    var optimizeCisAccountUpdate = OptimizeCisAccountUpdate(cisAccountUpdates);
                    await InsertUpdatedEvents(optimizeCisEventTokenUpdate, tokenEvents, optimizeCisAccountUpdate);
                    _logger.Debug($"Completed successfully processing {identifier}");
                }
                catch (Exception e)
                {
                    _logger.Warning(e, $"Exception on identifier {identifier}");
                    await using var context = await _contextFactory.CreateDbContextAsync(token);
                    await context.Database.GetDbConnection().ExecuteAsync(DeleteTokenEntitiesRelatedToContractIndexSql, new { Identifier = identifier });
                    throw;
                }
            });
    }

    /// <inheritdoc/>
    public bool ShouldNodeImportAwait() => true;
    
    /// <summary>
    /// Process in memory account updates such that only one row for each
    /// (contract index, contract subindex, token id, account address) are inserted.
    /// </summary>
    internal static IList<CisAccountUpdate> OptimizeCisAccountUpdate(ICollection<CisAccountUpdate> accountUpdates)
    {
        var accountTokenUpdates = new Dictionary<(ulong ContractIndex, ulong ContractSubIndex, string TokenId, string AccountAddress), BigInteger>();
        foreach (var accountUpdate in accountUpdates)
        {
            var key = (accountUpdate.ContractIndex, accountUpdate.ContractSubIndex, accountUpdate.TokenId, accountUpdate.Address.AsString);
            if (accountTokenUpdates.ContainsKey(key))
            {
                accountTokenUpdates[key] += accountUpdate.AmountDelta;
            }
            else
            {
                accountTokenUpdates[key] = accountUpdate.AmountDelta;
            }
        }

        var cisAccountUpdates = accountTokenUpdates.Select(keyValue => new CisAccountUpdate
        {
            AmountDelta = keyValue.Value,
            Address = new AccountAddress(keyValue.Key.AccountAddress),
            ContractSubIndex = keyValue.Key.ContractSubIndex,
            ContractIndex = keyValue.Key.ContractIndex,
            TokenId = keyValue.Key.TokenId
        }).ToList();
        
        return cisAccountUpdates;
    }

    /// <summary>
    /// Process in memory token updates such that only one row for each
    /// (contract index, contract subindex, token id) are inserted.
    /// </summary>
    internal static IEnumerable<CisEventTokenUpdate> OptimizeCisEventTokenUpdate(IEnumerable<CisEventTokenUpdate> tokenUpdates)
    {
        var tokenAmountUpdates = new Dictionary<(ulong ContractIndex, ulong ContractSubIndex, string TokenId), BigInteger>();
        var tokenMetadataUpdates = new Dictionary<(ulong ContractIndex, ulong ContractSubIndex, string TokenId), string>();
        foreach (var cisEventTokenUpdate in tokenUpdates)
        {
            var key = (cisEventTokenUpdate.ContractIndex, cisEventTokenUpdate.ContractSubIndex, cisEventTokenUpdate.TokenId);
            switch (cisEventTokenUpdate)
            {
                case CisEventTokenAmountUpdate tokenAmountUpdate:
                    if (tokenAmountUpdates.ContainsKey(key))
                    {
                        tokenAmountUpdates[key] += tokenAmountUpdate.AmountDelta;
                    }
                    else
                    {
                        tokenAmountUpdates[key] = tokenAmountUpdate.AmountDelta;
                    }
                    break;
                case CisEventTokenMetadataUpdate tokenMetadataUpdate:
                    tokenMetadataUpdates[key] = tokenMetadataUpdate.MetadataUrl;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cisEventTokenUpdate));
            }
        }

        IEnumerable<CisEventTokenUpdate> cisEventTokenAmountUpdates = tokenAmountUpdates.Select(valuePair => new CisEventTokenAmountUpdate
        {
            ContractIndex = valuePair.Key.ContractIndex,
            ContractSubIndex = valuePair.Key.ContractSubIndex,
            TokenId = valuePair.Key.TokenId,
            AmountDelta = valuePair.Value
        });
        IEnumerable<CisEventTokenUpdate> cisEventTokenMetadataUpdates = tokenMetadataUpdates.Select(valuePair => new CisEventTokenMetadataUpdate
        {
            ContractIndex = valuePair.Key.ContractIndex,
            ContractSubIndex = valuePair.Key.ContractSubIndex,
            TokenId = valuePair.Key.TokenId,
            MetadataUrl = valuePair.Value
        });
        var cisEventTokenUpdates = cisEventTokenAmountUpdates.Concat(cisEventTokenMetadataUpdates).ToList();
        
        return cisEventTokenUpdates;
    }
    
    /// <summary>
    /// Get contract events from contract index <see cref="contractIndex"/> ordered by
    /// <see cref="ContractEvent.BlockHeight"/>, <see cref="ContractEvent.TransactionIndex"/>, <see cref="ContractEvent.EventIndex"/>.
    /// </summary>
    internal async Task<IList<ContractEvent>> GetOrderedContractEvents(int contractIndex, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token);
        var parameter = new { Index = (long)contractIndex};
        var contractEvent = (await context.Database.GetDbConnection()
                .QueryAsync<ContractEvent>(ContractEventWithLogs, parameter))
            .ToList();
        return contractEvent;
    }    
    
    /// <summary>
    /// Delete all tokens related entities related to a contract index.
    /// </summary>
    private const string DeleteTokenEntitiesRelatedToContractIndexSql = @"
delete from graphql_token_events
where contract_address_index = @Identifier;
delete from graphql_account_tokens
where contract_index = @Identifier;
delete from graphql_tokens
where contract_index = @Identifier;
";    

    private Task InsertUpdatedEvents(
        IEnumerable<CisEventTokenUpdate> tokenUpdates,
        IList<TokenEvent> tokenEvents,
        IList<CisAccountUpdate> accountUpdates
    )
    {
        var tasks = new List<Task>
        {
            _writer.ApplyTokenUpdates(tokenUpdates),
            _writer.ApplyAccountUpdates(accountUpdates),
            _writer.ApplyTokenEvents(tokenEvents)
        };
        return Task.WhenAll(tasks);
    }    
    
    /// <summary>
    /// Return contract events with logs ordered by block height, transaction index and event index.
    ///
    /// <see cref="Application.Api.GraphQL.EfCore.Converters.Json.TransactionResultEventConverter"/> has event mapping.
    /// </summary>
    private const string ContractEventWithLogs = @"
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
WHERE g0.contract_address_index = @Index AND
            g0.event ->> 'tag' in ('16', '18', '34')
ORDER BY block_height, transaction_index, event_index
";    
    
    private sealed class JobContractRepository : IContractRepository
    {
        private readonly IEnumerable<ContractEvent> _contractEvents;
        
        public JobContractRepository(IEnumerable<ContractEvent> contractEvents)
        {
            _contractEvents = contractEvents;
        }
        
        public IEnumerable<T> GetEntitiesAddedInTransaction<T>() where T : class => 
            (_contractEvents as IEnumerable<T>)!;

        public Task<ContractSnapshot> GetReadonlyLatestContractSnapshot(ContractAddress contractAddress)
        {
            throw new NotImplementedException();
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    
        public Task<IList<TransactionRejectEventDto>> FromBlockHeightRangeGetContractRelatedRejections(ulong heightFrom, ulong heightTo)
        {
            throw new NotImplementedException();
        }
    
        public Task<IList<TransactionResultEventDto>> FromBlockHeightRangeGetContractRelatedTransactionResultEventRelations(ulong heightFrom, ulong heightTo)
        {
            throw new NotImplementedException();
        }
    
        public Task<List<ulong>> FromBlockHeightRangeGetBlockHeightsReadOrdered(ulong heightFrom, ulong heightTo)
        {
            throw new NotImplementedException();
        }
    
        public Task<ContractReadHeight?> GetReadonlyLatestContractReadHeight()
        {
            throw new NotImplementedException();
        }
    
        public Task<long> GetReadonlyLatestImportState(CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    
        public Task<ContractInitialized> GetReadonlyContractInitializedEventAsync(ContractAddress contractAddress)
        {
            throw new NotImplementedException();
        }
    
        public Task AddAsync<T>(params T[] entities) where T : class
        {
            throw new NotImplementedException();
        }
    
        public Task AddRangeAsync<T>(IEnumerable<T> heights) where T : class
        {
            throw new NotImplementedException();
        }
    
        public Task CommitAsync(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public Task<ModuleReferenceEvent> GetModuleReferenceEventAsync(string moduleReference)
        {
            throw new NotImplementedException();
        }

        public Task<ModuleReferenceEvent> GetModuleReferenceEventAtAsync(ContractAddress contractAddress, ulong blockHeight, ulong transactionIndex, uint eventIndex)
        {
            throw new NotImplementedException();
        }
    }
}
