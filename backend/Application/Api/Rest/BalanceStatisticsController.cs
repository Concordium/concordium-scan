using System.Globalization;
using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.Rest;

[ApiController]
public class BalanceStatisticsController : ControllerBase
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;

    public BalanceStatisticsController(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    [HttpGet]
    [Route("rest/balance-statistics/latest")]
    public async Task<ActionResult> GetLatest(string field)
    {
        if (field == null) throw new ArgumentNullException(nameof(field));
        
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var query = dbContext.Blocks
            .AsNoTracking()
            .OrderByDescending(x => x.Id);

        var scalarQuery = field.ToLowerInvariant() switch
        {
            "totalamount" => query.Select(x => (ulong?)x.BalanceStatistics.TotalAmount),
            "totalamountreleased" => query.Select(x => x.BalanceStatistics.TotalAmountReleased),
            "totalamoununlocked" => query.Select(x => x.BalanceStatistics.TotalAmountUnlocked),
            "totalamountnotreleased" => query.Select(x => x.BalanceStatistics.TotalAmount - x.BalanceStatistics.TotalAmountReleased),
            "totalamountstaked" => query.Select(x => (ulong?)x.BalanceStatistics.TotalAmountStaked),
            "totalamountencrypted" => query.Select(x => (ulong?)x.BalanceStatistics.TotalAmountEncrypted),
            "totalamountlockedinreleaseschedules" => query.Select(x => (ulong?)x.BalanceStatistics.TotalAmountLockedInReleaseSchedules),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Supported values for field are: ")
        };
        var value = await scalarQuery.FirstAsync();    

        var result = new ContentResult
        {
            ContentType = "text/plain",
            Content = value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "",
            StatusCode = 200
        };
        return result;
    }
}
