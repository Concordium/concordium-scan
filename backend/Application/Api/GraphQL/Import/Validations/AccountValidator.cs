using System.Threading.Tasks;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import.Validations;

public class AccountValidator
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

    public async Task ValidateAccounts(ulong blockHeight)
    {
        var blockHashes = await _nodeClient.GetBlocksAtHeightAsync(blockHeight);
        var blockHash = blockHashes.Single();
            
        var accountAddresses = await _nodeClient.GetAccountListAsync(blockHash);
        var nodeBalances = new List<Item>();
        var nodeAccountBakers = new List<AccountBaker>();

        foreach (var chunk in Chunk(accountAddresses, 10))
        {
            var accountInfos = await Task.WhenAll(chunk
                .Select(x => _nodeClient.GetAccountInfoAsync(x, blockHash)));
            
            nodeBalances.AddRange(accountInfos
                .Select(x => new Item(x.AccountAddress.AsString, (long)x.AccountAmount.MicroCcdValue)));
            
            nodeAccountBakers.AddRange(accountInfos
                .Where(x => x.AccountBaker != null)
                .Select(x => x.AccountBaker!)
                .Select(x => x));
        }
        
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        await ValidateAccountBalances(nodeBalances, blockHeight, dbContext);
        await ValidateBakers(nodeAccountBakers, blockHeight, dbContext);
    }

    private async Task ValidateAccountBalances(List<Item> nodeBalances, ulong blockHeight, GraphQlDbContext dbContext)
    {
        var dbBalances = await dbContext.Accounts
            .Select(x => new Item(x.CanonicalAddress.AsString, (long)x.Amount))
            .ToArrayAsync();

        var equal = nodeBalances.OrderBy(x => x.Address)
            .SequenceEqual(dbBalances.OrderBy(x => x.Address));
        _logger.Information(
            "Validated {accountCount} accounts at block height {blockHeight}. Node and database balances equal: {result}",
            nodeBalances.Count, blockHeight, equal);

        if (!equal)
        {
            var diff1 = nodeBalances.Except(dbBalances);
            var format = String.Join(Environment.NewLine,
                diff1.Select(diff => $"   [Address={diff.Address}] [Amount={diff.Amount}]"));
            _logger.Warning($"NodeBalances.Except(dbBalances): {Environment.NewLine}{format}");

            var diff2 = dbBalances.Except(nodeBalances);
            format = String.Join(Environment.NewLine,
                diff2.Select(diff => $"   [Address={diff.Address}] [Amount={diff.Amount}]"));
            _logger.Warning($"dbBalances.Except(nodeBalances): {Environment.NewLine}{format}");
        }
    }

    private async Task ValidateBakers(List<AccountBaker> nodeAccountBakers, ulong blockHeight, GraphQlDbContext dbContext)
    {
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
                    BakingCommission = x.BakerPoolInfo.CommissionRates.BakingCommission
                }
            })
            .OrderBy(x => x.Id)
            .ToArray();

        var dbBakers = await dbContext.Bakers
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
                    BakingCommission = x.ActiveState!.Pool.CommissionRates.BakingCommission
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
                var format = String.Join(Environment.NewLine, diff1.Select(diff => $"   [BakerId={diff}]"));
                _logger.Warning($"node had bakers not in database: {Environment.NewLine}{format}");
            }

            var diff2 = dbBakers.Except(nodeBakers).ToArray();
            if (diff2.Length > 0)
            {
                var format = String.Join(Environment.NewLine, diff2.Select(diff => $"   [BakerId={diff}]"));
                _logger.Warning($"database had bakers not in node: {Environment.NewLine}{format}");
            }
        }
    }

    private record Item(string Address, long Amount);
    
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