using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Dto;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.EventLogs;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Import;
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
public sealed class _10_CisEventReinitialization : IStatelessJob
{
    private readonly IDbContextFactory<GraphQlDbContext> _contextFactory;
    private readonly IEventLogWriter _writer;
    private readonly IAccountLookup _accountLookup;
    private readonly ContractAggregateOptions _options;
    private readonly ILogger _logger;

    /// <summary>
    /// WARNING - Do not change this if job already executed on environment, since it will trigger rerun of job.
    /// </summary>
    private const string JobName = "_10_CisEventReinitialization";

    public _10_CisEventReinitialization(
        IDbContextFactory<GraphQlDbContext> contextFactory,
        IEventLogWriter writer,
        IAccountLookup accountLookup,
        IOptions<ContractAggregateOptions> options)
    {
        _contextFactory = contextFactory;
        _writer = writer;
        _accountLookup = accountLookup;
        _options = options.Value;
        _logger = Log.ForContext<_10_CisEventReinitialization>();
    }

    /// <inheritdoc/>
    public string GetUniqueIdentifier() => JobName;

    /// <inheritdoc/>
    public async Task<IEnumerable<int>> GetIdentifierSequence(CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var max = context.ContractEvents.Max(ce => ce.ContractAddressIndex);
        return Enumerable.Range(0, (int)max + 1);
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
                // TransactionScope? transactionScope = null;
                try
                {
                    // transactionScope = new TransactionScope(
                    //     TransactionScopeOption.Required,
                    //     new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted },
                    //     TransactionScopeAsyncFlowOption.Enabled);

                    var contractEvents = await GetContractEvents(identifier, token);
                    var jobContractRepository = new JobContractRepository(contractEvents);
                    var (cisEventTokenUpdates, tokenEvents, cisAccountUpdates) = EventLogHandler.GetParsedTokenUpdate(jobContractRepository);
                    var optimizeCisEventTokenUpdate = OptimizeCisEventTokenUpdate(cisEventTokenUpdates);
                    var optimizeCisAccountUpdate = OptimizeCisAccountUpdate(cisAccountUpdates);
                    await InsertUpdatedEvents(optimizeCisEventTokenUpdate, tokenEvents, optimizeCisAccountUpdate);
                    // transactionScope.Complete();
                    _logger.Debug($"Completed successfully processing {identifier}");
                }
                catch (Exception e)
                {
                    _logger.Warning(e, $"Exception on identifier {identifier}");
                    await using var context = await _contextFactory.CreateDbContextAsync(token);
                    await context.Database.GetDbConnection().ExecuteAsync(
                        @"
delete from graphql_token_events
where contract_address_index = @Identifier;
delete from graphql_account_tokens
where contract_index = @Identifier;
delete from graphql_tokens
where contract_index = @Identifier;
", new { Identifier = identifier });
                    throw;
                }
                // finally
                // {
                //     transactionScope?.Dispose();
                // }
            });
    }

    /// <inheritdoc/>
    public bool ShouldNodeImportAwait() => true;

    private Task InsertUpdatedEvents(
        IEnumerable<CisEventTokenUpdate> tokenUpdates,
        IEnumerable<TokenEvent> tokenEvents,
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
    
private IList<CisAccountUpdate> OptimizeCisAccountUpdate(ICollection<CisAccountUpdate> accountUpdates)
    {
        _logger.Debug($"Initial {accountUpdates.Count} CisAccountUpdate updates");
        var accountBaseAddresses = accountUpdates
            .Select(u => 
                Concordium.Sdk.Types.AccountAddress.From(u.Address.AsString)
                    .GetBaseAddress()
                    .ToString())
            .Distinct();
        var accountsMap = _accountLookup.GetAccountIdsFromBaseAddresses(accountBaseAddresses);
        
        var accountTokenUpdates = new Dictionary<(ulong ContractIndex, ulong ContractSubIndex, string TokenId, string AccountAddress), BigInteger>();
        foreach (var accountUpdate in accountUpdates)
        {
            var accountBaseAddress = Concordium.Sdk.Types.AccountAddress.From(accountUpdate.Address.AsString)
                .GetBaseAddress()
                .ToString();
            if (accountsMap[accountBaseAddress] is null 
                || !accountsMap[accountBaseAddress].HasValue)
            {
                continue;
            }
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
        
        _logger.Debug($"From {accountUpdates.Count} to {cisAccountUpdates.Count} CisAccountUpdate");
        return cisAccountUpdates;
    }

    private IEnumerable<CisEventTokenUpdate> OptimizeCisEventTokenUpdate(ICollection<CisEventTokenUpdate> tokenUpdates)
    {
        _logger.Debug($"Initial {tokenUpdates.Count} CisEventTokenUpdate updates");
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
            ContractSubIndex = valuePair.Key.ContractIndex,
            TokenId = valuePair.Key.TokenId,
            AmountDelta = valuePair.Value
        });
        IEnumerable<CisEventTokenUpdate> cisEventTokenMetadataUpdates = tokenMetadataUpdates.Select(valuePair => new CisEventTokenMetadataUpdate
        {
            ContractIndex = valuePair.Key.ContractIndex,
            ContractSubIndex = valuePair.Key.ContractIndex,
            TokenId = valuePair.Key.TokenId,
            MetadataUrl = valuePair.Value
        });
        var cisEventTokenUpdates = cisEventTokenAmountUpdates.Concat(cisEventTokenMetadataUpdates).ToList();
        
        _logger.Debug($"From {tokenUpdates.Count} to {cisEventTokenUpdates.Count} CisEventTokenUpdate");
        return cisEventTokenUpdates;
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

    private async Task<IList<ContractEvent>> GetContractEvents(int address, CancellationToken token = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token);
        var parameter = new { Index = (long)address};
        var contractEvent = (await context.Database.GetDbConnection()
            .QueryAsync<ContractEvent>(ContractEventWithLogs, parameter))
            .ToList();
        _logger.Information($"Read count: {contractEvent.Count}");
        return contractEvent;
    }
    
    private sealed class JobContractRepository : IContractRepository
    {
        private readonly IEnumerable<ContractEvent> _contractEvents;
        
        public JobContractRepository(IEnumerable<ContractEvent> contractEvents)
        {
            _contractEvents = contractEvents;
        }
    
        public IEnumerable<ContractEvent> GetContractEventsAddedInTransaction() => _contractEvents;
    
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
    }
}
