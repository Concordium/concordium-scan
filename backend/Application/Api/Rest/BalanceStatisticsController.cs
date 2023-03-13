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
    public async Task<ActionResult> GetLatest(string field, string? unit = "microccd")
    {
        if (field == null) throw new ArgumentNullException(nameof(field));
        
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var query = dbContext.Blocks
            .AsNoTracking()
            .OrderByDescending(x => x.Id);

        var scalarQuery = field.ToLowerInvariant() switch
        {
            "totalamount" => query.Select(x => (ulong?)x.BalanceStatistics.TotalAmount),
            "totalamountcirculating" => query.Select(x => x.BalanceStatistics.TotalAmountReleased),
            "totalamoununlocked" => query.Select(x => x.BalanceStatistics.TotalAmountUnlocked),
            "totalamountnotreleased" => query.Select(x => x.BalanceStatistics.TotalAmount - x.BalanceStatistics.TotalAmountReleased),
            "totalamountstaked" => query.Select(x => (ulong?)x.BalanceStatistics.TotalAmountStaked),
            "totalamountencrypted" => query.Select(x => (ulong?)x.BalanceStatistics.TotalAmountEncrypted),
            "totalamountlockedinreleaseschedules" => query.Select(x => (ulong?)x.BalanceStatistics.TotalAmountLockedInReleaseSchedules),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, "Supported values for field are: ")
        };

        var value = await scalarQuery.FirstAsync();
        var retValue = "";
        if (!String.IsNullOrWhiteSpace(unit) && value.HasValue)
        {
            retValue = unit.ToLowerInvariant() switch
            {
                "ccd" => retValue = ((decimal)value / 10_00_000).ToString(CultureInfo.InvariantCulture),
                "microccd" => ((decimal)value).ToString(CultureInfo.InvariantCulture),
                _ => ((decimal)value).ToString(CultureInfo.InvariantCulture)
            };
        }

        var result = new ContentResult
        {
            ContentType = "text/plain",
            Content = retValue,
            StatusCode = 200
        };
        return result;
    }
}
