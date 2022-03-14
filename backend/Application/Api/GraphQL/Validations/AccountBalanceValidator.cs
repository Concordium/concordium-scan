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
        var accountInfos = await Task.WhenAll(accountAddresses
            .Select(x => _nodeClient.GetAccountInfoAsync(x, blockHash)));
        var nodeBalances = accountInfos
            .Select(x => new Item(x.AccountAddress.AsString, (long)x.AccountAmount.MicroCcdValue))
            .OrderBy(x => x.Address)
            .ToArray();

        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var dbBalances = await dbContext.Accounts
            .Select(x => new Item(x.CanonicalAddress, (long)x.Amount))
            .ToArrayAsync();
        dbBalances = dbBalances.OrderBy(x => x.Address).ToArray();
        
        var equal = nodeBalances.SequenceEqual(dbBalances);
        _logger.Information("Validated {accountCount} accounts at block height {blockHeight}. Node and database balances equal: {result}", nodeBalances.Length, blockHeight, equal);
    }
    
    private record Item(string Address, long Amount);
}