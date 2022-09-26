using System.Globalization;
using System.Threading.Tasks;
using System.Text;
using Application.Api.GraphQL.EfCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.Rest;

[ApiController]
public class CsvExportController : ControllerBase
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;

    public CsvExportController(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    [HttpGet]
    [Route("rest/export/statement")]
    public async Task<ActionResult> GetStatementExport(long accountId)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        var query = dbContext.AccountStatementEntries
            .AsNoTracking()
            .Where(x => x.AccountId == accountId);

        var scalarQuery = query.Select(x => new {
            Timestamp = x.Timestamp,
            EntryType = x.EntryType,
            Amount = x.Amount,
        });
        var values = await scalarQuery.ToListAsync();
        // TODO Use something like 'CsvHelper' (see 'https://joshclose.github.io/CsvHelper/examples/writing/write-anonymous-type-objects/')?
        var sb = new StringBuilder("timestamp,type,amount_uccd\n");
        var csv = values.Aggregate(sb, (acc, v) => acc.Append($"{v.Timestamp.ToString("u")},{v.EntryType},{v.Amount}\n"));
        var result = new FileContentResult(Encoding.ASCII.GetBytes(csv.ToString()), "text/csv")
        {
            FileDownloadName = $"statement-{accountId}.csv",
        };
        return result;
    }
}