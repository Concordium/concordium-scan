using System.Threading.Tasks;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import.Validations;

public class AccountValidator : IImportValidator
{
    private readonly GrpcNodeClient _nodeClient;
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly ILogger _logger;

    public AccountValidator(GrpcNodeClient nodeClient, IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _nodeClient = nodeClient;
        _dbContextFactory = dbContextFactory;
        _logger = Log.ForContext<AccountValidator>();
    }

    public async Task Validate(Block block)
    {
        var blockHeight = (ulong)block.BlockHeight;
        var blockHashes = await _nodeClient.GetBlocksAtHeightAsync(blockHeight);
        var blockHash = blockHashes.Single();
            
        var accountAddresses = await _nodeClient.GetAccountListAsync(blockHash);
        var nodeAccountInfos = new List<AccountInfo>();
        var nodeAccountBakers = new List<AccountBaker>();

        foreach (var chunk in Chunk(accountAddresses, 10))
        {
            var accountInfos = await Task.WhenAll(chunk
                .Select(x => _nodeClient.GetAccountInfoAsync(x, blockHash)));
            
            nodeAccountInfos.AddRange(accountInfos);
            
            nodeAccountBakers.AddRange(accountInfos
                .Where(x => x.AccountBaker != null)
                .Select(x => x.AccountBaker!)
                .Select(x => x));
        }
        
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await ValidateAccounts(nodeAccountInfos, blockHeight, dbContext);
        await ValidateBakers(nodeAccountBakers, block, dbContext);
    }

    private async Task ValidateAccounts(List<AccountInfo> nodeAccountInfos, ulong blockHeight, GraphQlDbContext dbContext)
    {
        var mappedNodeAccounts = nodeAccountInfos.Select(x => new
            {
                AccountAddress = x.AccountAddress.AsString,
                AccountBalance = x.AccountAmount.MicroCcdValue,
                Delegation = x.AccountDelegation == null ? null : new
                {
                    StakedAmount = x.AccountDelegation.StakedAmount.MicroCcdValue,
                    RestakeEarnings = x.AccountDelegation.RestakeEarnings,
                    PendingChange = x.AccountDelegation.PendingChange == null ? null : Format(x.AccountDelegation.PendingChange),
                    Delegation = Format(x.AccountDelegation.DelegationTarget)
                }
            })
            .OrderBy(x => x.AccountAddress)
            .ToArray();

        var dbAccountInfos = await dbContext.Accounts
            .AsNoTracking()
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

    private string Format(DelegationTarget value)
    {
        return value switch
        {
            PassiveDelegationTarget => "passive",
            BakerDelegationTarget x => $"baker {x.BakerId}",
            _ => throw new NotImplementedException()
        };
    }

    private string Format(ConcordiumSdk.NodeApi.Types.DelegationTarget value)
    {
        return value switch
        {
            ConcordiumSdk.NodeApi.Types.PassiveDelegationTarget => "passive",
            ConcordiumSdk.NodeApi.Types.BakerDelegationTarget x => $"baker {x.BakerId}",
            _ => throw new NotImplementedException()
        };
    }

    private string Format(AccountDelegationPendingChange pendingChange)
    {
        return pendingChange switch
        {
            AccountDelegationRemovePending x => $"Remove@{x.EffectiveTime:O}",
            AccountDelegationReduceStakePending x => $"ReduceStake@{x.EffectiveTime:O}->{x.NewStake.MicroCcdValue}",
            _ => throw new NotImplementedException()
        };
    }

    private string Format(PendingDelegationChange pendingChange)
    {
        return pendingChange switch
        {
            PendingDelegationRemoval x => $"Remove@{x.EffectiveTime:O}",
            PendingDelegationReduceStake x => $"ReduceStake@{x.EffectiveTime:O}->{x.NewStakedAmount}",
            _ => throw new NotImplementedException()
        };
    }

    private async Task ValidateBakers(List<AccountBaker> nodeAccountBakers, Block block, GraphQlDbContext dbContext)
    {
        var blockHeight = (ulong)block.BlockHeight;
        var blockHash = new BlockHash(block.BlockHash);

        var poolStatuses = new List<BakerPoolStatus>();

        var nodeSwVersion = await _nodeClient.GetPeerVersionAsync();
        if (nodeSwVersion.Major >= 4)
        {
            foreach (var chunk in Chunk(nodeAccountBakers.ToArray(), 10))
            {
                var chunkResult = await Task.WhenAll(chunk
                    .Select(x => _nodeClient.GetPoolStatusForBaker(x.BakerId, blockHash)));

                poolStatuses.AddRange(chunkResult.Where(x => x != null)!);
            }
        }

        var nodeBakers = nodeAccountBakers
            .Select(x => new
            {
                Id = x.BakerId,
                StakedAmount = x.StakedAmount.MicroCcdValue,
                RestakeEarnings = x.RestakeEarnings,
                Pool = x.BakerPoolInfo == null ? null : new
                {
                    OpenStatus = x.BakerPoolInfo.OpenStatus.MapToGraphQlEnum(),
                    MetadataUrl = x.BakerPoolInfo.MetadataUrl,
                    TransactionCommission = x.BakerPoolInfo.CommissionRates.TransactionCommission,
                    FinalizationCommission = x.BakerPoolInfo.CommissionRates.FinalizationCommission,
                    BakingCommission = x.BakerPoolInfo.CommissionRates.BakingCommission,
                    DelegatedStake = poolStatuses.Single(status => status.BakerId == x.BakerId).DelegatedCapital.MicroCcdValue
                }
            })
            .OrderBy(x => x.Id)
            .ToArray();

        var dbBakers = await dbContext.Bakers
            .AsNoTracking()
            .Where(x => x.ActiveState != null)
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
                    DelegatedStake = x.ActiveState!.Pool.DelegatedStake
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
    }

    private IEnumerable<IEnumerable<T>> Chunk<T>(T[] list, int batchSize)
    {
        int total = 0;
        while (total < list.Length)
        {
            yield return list.Skip(total).Take(batchSize);
            total += batchSize;
        }
    }
}