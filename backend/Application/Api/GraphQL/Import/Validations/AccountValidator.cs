using System.Linq.Expressions;
using System.Threading.Tasks;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Concordium.Sdk.Client;
using Concordium.Sdk.Types;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import.Validations;

public class AccountValidator : IImportValidator
{
    private readonly ConcordiumClient _nodeClient;
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly ILogger _logger;

    public AccountValidator(ConcordiumClient nodeClient, IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _nodeClient = nodeClient;
        _dbContextFactory = dbContextFactory;
        _logger = Log.ForContext<AccountValidator>();
    }

    public async Task Validate(Block block)
    {
        await InternalValidate(block);
    }

    public async Task ValidateSingle(Block block, SingleAccountValidationInfo singleAccountValidationInfo)
    {
        await InternalValidate(block, singleAccountValidationInfo);
    }

    private async Task InternalValidate(Block block, SingleAccountValidationInfo? singleAccountValidationInfo = null)
    {
        var blockHash = BlockHash.From(block.BlockHash);
        var blockHeight = (ulong)block.BlockHeight;
        var given = new Given(blockHash);

        var accountAddresses = singleAccountValidationInfo == null
            ? await (await _nodeClient.GetAccountListAsync(given)).Response.ToArrayAsync()
            : new [] { Concordium.Sdk.Types.AccountAddress.From(singleAccountValidationInfo.CanonicalAccountAddress) };

        var nodeAccountInfos = new List<AccountInfo>();
        var nodeAccountBakers = new List<AccountBaker>();

        foreach (var chunk in Chunk(accountAddresses, 10))
        {
            var accountInfoResults = await Task.WhenAll(chunk
                .Select(x => _nodeClient.GetAccountInfoAsync(x, given)));
            
            var accountInfos = accountInfoResults
                .Where(x => x.Response != null)
                .Select(x => x.Response!)
                .ToArray();

            nodeAccountInfos.AddRange(accountInfos);

            nodeAccountBakers.AddRange(accountInfos
                .Select(ai => ai.AccountStakingInfo as AccountBaker)
                .Where(ab => ab is not null)
                .Select(x => x!));
        }

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await ValidateAccounts(nodeAccountInfos, blockHeight, dbContext, singleAccountValidationInfo);
        await ValidateBakers(nodeAccountBakers, block, dbContext, singleAccountValidationInfo);
    }

    private bool TryGetAccountDelegation(IAccountStakingInfo? info, out AccountDelegation? delegation)
    {
        delegation = null;
        if (info is not AccountDelegation accountDelegation)
        {
            return false;
        }
        delegation = accountDelegation;
        return true;
    }

    private async Task ValidateAccounts(List<AccountInfo> nodeAccountInfos, ulong blockHeight, GraphQlDbContext dbContext, SingleAccountValidationInfo? singleAccountAddress)
    {
        var mappedNodeAccounts = nodeAccountInfos.Select(x => new
            {
                AccountAddress = x.AccountAddress.ToString(),
                AccountBalance = x.AccountAmount.Value,
                Delegation = TryGetAccountDelegation(x.AccountStakingInfo, out var delegation) ? new
                {
                    StakedAmount = delegation!.StakedAmount.Value,
                    RestakeEarnings = delegation.RestakeEarnings,
                    PendingChange = delegation.PendingChange == null ? null : Format(delegation.PendingChange),
                    Delegation = Format(delegation.DelegationTarget)
                } : null
            })
            .OrderBy(x => x.AccountAddress)
            .ToArray();

        Expression<Func<Account, bool>> whereClause = singleAccountAddress == null
            ? account => true
            : account => account.Id == singleAccountAddress.AccountId;  
        
        var dbAccountInfos = await dbContext.Accounts
            .AsNoTracking()
            .Where(whereClause)
            .Select(x => new
            {
                x.CanonicalAddress,
                x.Amount,
                x.Delegation
            })
            .ToArrayAsync();

        var mappedDbAccounts = dbAccountInfos.Select(x => new
            {
                AccountAddress = x.CanonicalAddress.AsString,
                AccountBalance = x.Amount,
                Delegation = x.Delegation == null ? null : new
                {
                    StakedAmount = x.Delegation.StakedAmount,
                    RestakeEarnings = x.Delegation.RestakeEarnings,
                    PendingChange = x.Delegation.PendingChange == null ? null : Format(x.Delegation.PendingChange),
                    Delegation = Format(x.Delegation.DelegationTarget)
                }

            })
            .OrderBy(x => x.AccountAddress)
            .ToArray();
        
        var equal = mappedNodeAccounts.SequenceEqual(mappedDbAccounts);
        
        _logger.Information(
            "Validated {accountCount} accounts at block height {blockHeight}. Node and database accounts equal: {result}",
            nodeAccountInfos.Count, blockHeight, equal);

        if (!equal)
        {
            var diff1 = mappedNodeAccounts.Except(mappedDbAccounts).ToArray();
            if (diff1.Length > 0)
            {
                var format = String.Join(Environment.NewLine, diff1.Select(diff => $"   {diff}"));
                _logger.Warning($"node had accounts not in database: {Environment.NewLine}{format}");
            }

            var diff2 = mappedDbAccounts.Except(mappedNodeAccounts).ToArray();
            if (diff2.Length > 0)
            {
                var format = String.Join(Environment.NewLine, diff2.Select(diff => $"   {diff}"));
                _logger.Warning($"database had accounts not in node: {Environment.NewLine}{format}");
            }
        }
    }

    private static string Format(DelegationTarget value)
    {
        return value switch
        {
            PassiveDelegationTarget => "passive",
            BakerDelegationTarget x => $"baker {x.BakerId}",
            _ => throw new NotImplementedException()
        };
    }

    private static string Format(Concordium.Sdk.Types.DelegationTarget value)
    {
        return value switch
        {
            Concordium.Sdk.Types.PassiveDelegationTarget => "passive",
            Concordium.Sdk.Types.BakerDelegationTarget x => $"baker {x.BakerId}",
            _ => throw new NotImplementedException()
        };
    }

    private static string Format(IAccountDelegationPendingChange pendingChange)
    {
        return pendingChange switch
        {
            RemoveStakePending x => $"Remove@{x.EffectiveTime:O}",
            ReduceStakePending x => $"ReduceStake@{x.EffectiveTime:O}->{x.NewStake.Value}",
            _ => throw new NotImplementedException()
        };
    }

    private static string Format(PendingDelegationChange pendingChange)
    {
        return pendingChange switch
        {
            PendingDelegationRemoval x => $"Remove@{x.EffectiveTime:O}",
            PendingDelegationReduceStake x => $"ReduceStake@{x.EffectiveTime:O}->{x.NewStakedAmount}",
            _ => throw new NotImplementedException()
        };
    }

    private async Task ValidateBakers(List<AccountBaker> nodeAccountBakers, Block block, GraphQlDbContext dbContext, SingleAccountValidationInfo? singleAccountAddress)
    {
        var blockHeight = (ulong)block.BlockHeight;
        var blockHash = BlockHash.From(block.BlockHash);
        var given = new Given(blockHash);

        var poolStatuses = new List<BakerPoolStatus>();

        var nodeInfo = await _nodeClient.GetNodeInfoAsync();
        if (nodeInfo.Version.Major >= 4)
        {
            foreach (var chunk in Chunk(nodeAccountBakers, 10))
            {
                var chunkResult = await Task.WhenAll(chunk
                    .Select(x => _nodeClient.GetPoolInfoAsync(x.BakerId, given)));
            
                poolStatuses.AddRange(chunkResult
                    .Where(x => x.Response != null)
                    .Select(x => x.Response!)
                );
            }
        }

        var nodeBakers = nodeAccountBakers
            .Select(x =>
            {
                var bakerPoolStatus = x.BakerPoolInfo != null 
                    ? poolStatuses.Single(status => status.BakerId == x.BakerId) 
                    : null;
                
                return new
                {
                    Id = x.BakerId.Id.Index,
                    StakedAmount = x.StakedAmount.Value,
                    RestakeEarnings = x.RestakeEarnings,
                    Pool = x.BakerPoolInfo == null ? null : new
                        {
                            OpenStatus = x.BakerPoolInfo.OpenStatus.MapToGraphQlEnum(),
                            MetadataUrl = x.BakerPoolInfo.MetadataUrl,
                            TransactionCommission = x.BakerPoolInfo.CommissionRates.TransactionCommission.AsDecimal(),
                            FinalizationCommission = x.BakerPoolInfo.CommissionRates.FinalizationCommission.AsDecimal(),
                            BakingCommission = x.BakerPoolInfo.CommissionRates.BakingCommission.AsDecimal(),
                            DelegatedStake = bakerPoolStatus.DelegatedCapital.Value,
                            DelegatedStakeCap = bakerPoolStatus.DelegatedCapitalCap.Value,
                            PaydayStatus = bakerPoolStatus?.CurrentPaydayStatus == null ? null : new
                            {
                                BakerStake = bakerPoolStatus.CurrentPaydayStatus.BakerEquityCapital.Value,
                                DelegatedStake = bakerPoolStatus.CurrentPaydayStatus.DelegatedCapital.Value,
                                EffectiveStake = bakerPoolStatus.CurrentPaydayStatus.EffectiveStake.Value,
                                LotteryPower = bakerPoolStatus.CurrentPaydayStatus.LotteryPower
                            }
                        }
                };
            })
            .OrderBy(x => x.Id)
            .ToArray();

        Expression<Func<Baker, bool>> whereClause = singleAccountAddress == null
            ? account => true
            : baker => baker.Id == singleAccountAddress.AccountId;  

        var dbBakers = await dbContext.Bakers
            .AsNoTracking()
            .Where(x => x.ActiveState != null)
            .Where(whereClause)
            .Select(x => new
            {
                Id = (ulong)x.Id,
                StakedAmount = x.ActiveState!.StakedAmount,
                RestakeEarnings = x.ActiveState!.RestakeEarnings,
                Pool = x.ActiveState!.Pool == null ? null : new
                {
                    OpenStatus = x.ActiveState!.Pool.OpenStatus,
                    MetadataUrl = x.ActiveState!.Pool.MetadataUrl,
                    TransactionCommission = x.ActiveState!.Pool.CommissionRates.TransactionCommission,
                    FinalizationCommission = x.ActiveState!.Pool.CommissionRates.FinalizationCommission,
                    BakingCommission = x.ActiveState!.Pool.CommissionRates.BakingCommission,
                    DelegatedStake = x.ActiveState!.Pool.DelegatedStake,
                    DelegatedStakeCap = x.ActiveState!.Pool.DelegatedStakeCap,
                    PaydayStatus = x.ActiveState!.Pool.PaydayStatus == null ? null : new
                    {
                        BakerStake = x.ActiveState!.Pool.PaydayStatus.BakerStake,
                        DelegatedStake = x.ActiveState!.Pool.PaydayStatus.DelegatedStake,
                        EffectiveStake = x.ActiveState!.Pool.PaydayStatus.EffectiveStake,
                        LotteryPower = x.ActiveState!.Pool.PaydayStatus.LotteryPower
                    }
                }
            })
            .OrderBy(x => x.Id)
            .ToArrayAsync();

        var activeBakersEqual = nodeBakers.SequenceEqual(dbBakers);
        _logger.Information(
            "Validated {bakerCount} bakers at block height {blockHeight}. Node and database bakers equal: {result}",
            nodeBakers.Length, blockHeight, activeBakersEqual);
        if (!activeBakersEqual)
        {
            var diff1 = nodeBakers.Except(dbBakers).ToArray();
            if (diff1.Length > 0)
            {
                var format = String.Join(Environment.NewLine, diff1.Select(diff => $"   {diff}"));
                _logger.Warning($"node had bakers not in database: {Environment.NewLine}{format}");
            }

            var diff2 = dbBakers.Except(nodeBakers).ToArray();
            if (diff2.Length > 0)
            {
                var format = String.Join(Environment.NewLine, diff2.Select(diff => $"   {diff}"));
                _logger.Warning($"database had bakers not in node: {Environment.NewLine}{format}");
            }
        }
        
        await ValidateBakerConsistencyInDatabase(dbContext);
    }

    private async Task ValidateBakerConsistencyInDatabase(GraphQlDbContext graphQlDbContext)
    {
        var conn = graphQlDbContext.Database.GetDbConnection();
        await conn.OpenAsync();

        var expectedSql = @"select id, active_pool_delegator_count 
                            from graphql_bakers 
                            where active_pool_delegator_count > 0 
                            order by id;";
        
        var expectedRows = await conn.QueryAsync(expectedSql);
        var expectedMapped = expectedRows
            .Select(row => new
            {
                BakerId = (long)row.id,
                DelegatorCount = (int)row.active_pool_delegator_count
            })
            .ToArray();
        
        var actualSql = @"select delegation_target_baker_id as baker_id, count(*) as delegator_count 
                          from graphql_accounts 
                          where delegation_target_baker_id >= 0 
                          group by (delegation_target_baker_id) 
                          order by delegation_target_baker_id;";
        
        var actualRows = await conn.QueryAsync(actualSql);
        var actualMapped = actualRows
            .Select(row => new
            {
                BakerId = (long)row.baker_id,
                DelegatorCount = (int)row.delegator_count
            })
            .ToArray();

        var equal = expectedMapped.SequenceEqual(actualMapped);
        _logger.Information("Delegator count on bakers matched expected: {equal}", equal);
        if (!equal)
        {
            var diff1 = expectedMapped.Except(actualMapped).ToArray();
            if (diff1.Length > 0)
            {
                var format = String.Join(Environment.NewLine, diff1.Select(diff => $"   {diff}"));
                _logger.Warning($"expected list had items not in actual: {Environment.NewLine}{format}");
            }

            var diff2 = actualMapped.Except(expectedMapped).ToArray();
            if (diff2.Length > 0)
            {
                var format = String.Join(Environment.NewLine, diff2.Select(diff => $"   {diff}"));
                _logger.Warning($"actual list had items not in expected: {Environment.NewLine}{format}");
            }
        }
        
        await conn.CloseAsync();
    }

    private IEnumerable<IEnumerable<T>> Chunk<T>(IReadOnlyCollection<T> list, int batchSize)
    {
        var total = 0;
        while (total < list.Count)
        {
            yield return list.Skip(total).Take(batchSize);
            total += batchSize;
        }
    }
}

public record SingleAccountValidationInfo(string CanonicalAccountAddress, long AccountId);