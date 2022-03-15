using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using ConcordiumSdk.NodeApi;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Validations;

public class AccountBalanceValidator
{
    private readonly GrpcNodeClient _nodeClient;
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly ILogger _logger;

    public AccountBalanceValidator(GrpcNodeClient nodeClient, IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _nodeClient = nodeClient;
        _dbContextFactory = dbContextFactory;
        _logger = Log.ForContext<AccountBalanceValidator>();
    }

    public async Task ValidateAccountBalances(ulong blockHeight)
    {
        var blockHashes = await _nodeClient.GetBlocksAtHeightAsync(blockHeight);
        var blockHash = blockHashes.Single();
            
        var accountAddresses = await _nodeClient.GetAccountListAsync(blockHash);
        var nodeBalances = new List<Item>();

        foreach (var chunk in Chunk(accountAddresses, 10))
        {
            var accountInfos = await Task.WhenAll(chunk
                .Select(x => _nodeClient.GetAccountInfoAsync(x, blockHash)));
            
            nodeBalances.AddRange(accountInfos
                .Select(x => new Item(x.AccountAddress.AsString, (long)x.AccountAmount.MicroCcdValue)));
        }
        
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var dbBalances = await dbContext.Accounts
            .Select(x => new Item(x.CanonicalAddress, (long)x.Amount))
            .ToArrayAsync();
        
        var equal = nodeBalances.OrderBy(x => x.Address)
            .SequenceEqual(dbBalances.OrderBy(x => x.Address));
        _logger.Information("Validated {accountCount} accounts at block height {blockHeight}. Node and database balances equal: {result}", nodeBalances.Count, blockHeight, equal);
        
        if (!equal)
        {
            var diff1 = nodeBalances.Except(dbBalances);
            var format = String.Join(Environment.NewLine, diff1.Select(diff => $"   [Address={diff.Address}] [Amount={diff.Amount}]"));
            _logger.Warning($"NodeBalances.Except(dbBalances): {Environment.NewLine}{format}");

            var diff2 = dbBalances.Except(nodeBalances);
             format = String.Join(Environment.NewLine, diff2.Select(diff => $"   [Address={diff.Address}] [Amount={diff.Amount}]"));
            _logger.Warning($"dbBalances.Except(nodeBalances): {Environment.NewLine}{format}");
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