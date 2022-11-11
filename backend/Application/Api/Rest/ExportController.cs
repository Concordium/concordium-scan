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
            return BadRequest("invalid account format");
        }

        var baseAddress = new AccountAddress(parsed!.GetBaseAddress().AsString);
        var account = dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefault(account => account.BaseAddress == baseAddress);
        if (account == null)
        {
            return NotFound($"account '{accountAddress}' does not exist");
        }

        var query = dbContext.AccountStatementEntries
            .AsNoTracking()
            .Where(x => x.AccountId == account.Id);
        if (fromTime.HasValue)
        {
            if (fromTime.Value.Kind != DateTimeKind.Utc)
            {
                return BadRequest("time zone missing on 'fromTime'");
            }

            DateTimeOffset t = fromTime.Value;
            query = query.Where(e => e.Timestamp >= t);
        }
        if (toTime.HasValue)
        {
            if (toTime.Value.Kind != DateTimeKind.Utc)
            {
                return BadRequest("time zone missing on 'toTime'");
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
        var csv = new StringBuilder("Time,Amount (CCD),Label\n");
        DateTimeOffset? firstTime = null;
        DateTimeOffset? lastTime = null;
        foreach (var v in values)
        {
            var t = v.Timestamp;
            
            // Keep timestamps for result filename.
            firstTime ??= t;
            lastTime = t;
            
            // Append row onto result contents.
            csv.Append(t.ToString("u"));
            csv.Append(',');
            csv.Append(v.Amount / 1e6);
            csv.Append(',');
            csv.Append(v.EntryType);
            csv.Append('\n');
        }

        return new FileContentResult(Encoding.ASCII.GetBytes(csv.ToString()), "text/csv")
        {
            FileDownloadName = $"statement-{accountAddress}_{firstTime:yyyyMMddHHmmss}_{lastTime:yyyyMMddHHmmss}.csv",
        };
    }
}
