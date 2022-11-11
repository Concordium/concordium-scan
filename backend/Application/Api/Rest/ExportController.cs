using System.Threading.Tasks;
using System.Text;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.Rest;

[ApiController]
public class ExportController : ControllerBase
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;

    public ExportController(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    [HttpGet]
    [Route("rest/export/statement")]
    public async Task<ActionResult> GetStatementExport(string accountAddress, DateTime? fromTime, DateTime? toTime)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        if (!ConcordiumSdk.Types.AccountAddress.TryParse(accountAddress, out var parsed))
        {
            return BadRequest("Invalid account format.");
        }

        var baseAddress = new AccountAddress(parsed!.GetBaseAddress().AsString);
        var account = dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefault(account => account.BaseAddress == baseAddress);
        if (account == null)
        {
            return NotFound($"Account '{accountAddress}' does not exist.");
        }

        var query = dbContext.AccountStatementEntries
            .AsNoTracking()
            .Where(x => x.AccountId == account.Id);
        if (fromTime.HasValue)
        {
            if (fromTime.Value.Kind != DateTimeKind.Utc)
            {
                return BadRequest("Time zone missing on 'fromTime'.");
            }

            DateTimeOffset t = fromTime.Value;
            query = query.Where(e => e.Timestamp >= t);
        }
        if (toTime.HasValue)
        {
            if (toTime.Value.Kind != DateTimeKind.Utc)
            {
                return BadRequest("Time zone missing on 'toTime'.");
            }
            DateTimeOffset t = toTime.Value;
            query = query.Where(e => e.Timestamp <= t);
        }
        
        var result = query.Select(x => new
        {
            x.Timestamp,
            x.EntryType,
            x.Amount,
        });
        var values = await result.ToListAsync();
        if (values.Count == 0)
        {
            return new ContentResult
            {
                Content = "Nothing to export.\n"
            };
        }
        
        var csv = new StringBuilder("Time,Amount (CCD),Label\n");
        foreach (var v in values)
        {
            csv.Append(v.Timestamp.ToString("u"));
            csv.Append(',');
            csv.Append(v.Amount / 1e6);
            csv.Append(',');
            csv.Append(v.EntryType);
            csv.Append('\n');
        }
        
        var firstTime = values.First().Timestamp;
        var lastTime = values.Last().Timestamp;
        return new FileContentResult(Encoding.ASCII.GetBytes(csv.ToString()), "text/csv")
        {
            FileDownloadName = $"statement-{accountAddress}_{firstTime:yyyyMMddHHmmss}-{lastTime:yyyyMMddHHmmss}.csv"
        };
    }
}
