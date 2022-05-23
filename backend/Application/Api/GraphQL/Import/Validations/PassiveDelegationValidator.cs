using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import.Validations;

public class PassiveDelegationValidator : IImportValidator
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private readonly ILogger _logger;

    public PassiveDelegationValidator(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
        _logger = Log.ForContext<PassiveDelegationValidator>();
    }

    public async Task Validate(Block block)
    {
        await ValidateDelegatorCount();
    }
    
    private async Task ValidateDelegatorCount()
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var expectedRow = await dbContext.PassiveDelegations.SingleOrDefaultAsync();
        var expectedValue = expectedRow?.DelegatorCount;
        
        var conn = dbContext.Database.GetDbConnection();
        await conn.OpenAsync();
        var actualSql = "select count(*) from graphql_accounts where delegation_target_baker_id = -1;";
        var actualValue = await conn.QuerySingleOrDefaultAsync<int?>(actualSql);
        await conn.CloseAsync();

        var equal = expectedValue == actualValue;
        _logger.Information("Passive delegator count matched expected: {equal}", equal);
        if (!equal)
            _logger.Information("Entity count: {expectedCount}, aggregated database value: {actualCount}", expectedValue, actualValue);
    }
}