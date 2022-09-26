using System.Globalization;
using System.Threading.Tasks;
using System.Text;
using Application.Api.GraphQL.EfCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.Rest;

[ApiController]
public class RewardsCsvController : ControllerBase
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;

    public RewardsCsvController(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    [HttpGet]
    [Route("rest/rewards")]
    public async Task<ActionResult> GetRewards(long accountId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var query = dbContext.AccountRewards
            .AsNoTracking()
            .Where(x => x.AccountId == accountId);

        var scalarQuery = query.Select(x => new {
            Timestamp = x.Timestamp,
            RewardType = x.RewardType,
            Amount = x.Amount,
        });
        var values = await scalarQuery.ToListAsync();
        // TODO Better to use something like 'CsvHelper' (see 'https://joshclose.github.io/CsvHelper/examples/writing/write-anonymous-type-objects/')?
        var sb = new StringBuilder("timestamp,type,amount_uccd\n");
        var csv = values.Aggregate(sb, (acc, v) => acc.Append($"{v.Timestamp},{v.RewardType},{v.Amount}\n"));
        var result = new FileContentResult(Encoding.ASCII.GetBytes(csv.ToString()), "text/csv")
        {
            FileDownloadName = $"rewards-{accountId}.csv",
        };
        return result;
    }
}
