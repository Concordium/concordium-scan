using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.PassiveDelegations;
using Concordium.Sdk.Client;
using Concordium.Sdk.Types;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import.Validations;

public class PassiveDelegationValidator : IImportValidator
{
    private readonly ConcordiumClient _nodeClient;
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly ILogger _logger;

    public PassiveDelegationValidator(ConcordiumClient nodeClient, IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _nodeClient = nodeClient;
        _dbContextFactory = dbContextFactory;
        _logger = Log.ForContext<PassiveDelegationValidator>();
    }

    public async Task Validate(Block block)
    {
        var nodeSwVersion = await _nodeClient.GetPeerVersionAsync();
        if (nodeSwVersion.Major >= 4)
        {
            var target = await ReadPassiveDelegation();

            await ValidateDatabaseConsistent(target);
            await ValidateAgainstNodeData(target, block);
        }
    }

    private async Task ValidateAgainstNodeData(PassiveDelegation? passiveDelegation, Block block)
    {
        var expectedValue = new
        {
            DelegatedStake = passiveDelegation?.DelegatedStake,
            CurrentPaydayDelegatedStake = passiveDelegation?.CurrentPaydayDelegatedStake
        };

        var nodePoolStatus = await _nodeClient.GetPassiveDelegationInfoAsync(block.Into());
        var actualValue = new
        {
            DelegatedStake = nodePoolStatus?.DelegatedCapital.Value,
            CurrentPaydayDelegatedStake = nodePoolStatus?.CurrentPaydayDelegatedCapital.Value
        };

        var equal = expectedValue.Equals(actualValue);
        _logger.Information("Passive delegator delegated stake matched expected: {equal}", equal);
        if (!equal)
            _logger.Information("Entity stake: {expectedValue}, node value: {actualCount}", expectedValue, actualValue);
    }

    private async Task ValidateDatabaseConsistent(PassiveDelegation? passiveDelegation)
    {
        var expectedValue = passiveDelegation?.DelegatorCount;
        
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        var conn = dbContext.Database.GetDbConnection();
        await conn.OpenAsync();
        var actualSql = "select count(*) from graphql_accounts where delegation_target_baker_id = -1;";
        var actualValue = await conn.QuerySingleOrDefaultAsync<int>(actualSql);
        await conn.CloseAsync();

        var equal = expectedValue.HasValue ? expectedValue.Value == actualValue : actualValue == 0;
        _logger.Information("Passive delegator count matched expected: {equal}", equal);
        if (!equal)
            _logger.Information("Entity count: {expectedCount}, aggregated database value: {actualCount}", expectedValue, actualValue);
    }

    private async Task<PassiveDelegation?> ReadPassiveDelegation()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
        return await dbContext.PassiveDelegations.SingleOrDefaultAsync();
    }
}