using System.Globalization;
using System.Threading.Tasks;
using System.Text;

using Application.Api.GraphQL.EfCore;
using Concordium.Sdk.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;
using System.IO;
using EnumerableStreamFileResult;

namespace Application.Api.Rest;

[ApiController]
public class ExportController : ControllerBase
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private const int MAX_DAYS = 32;

    public ExportController(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    [HttpGet]
    [Route("rest/export/statement")]
    public async Task<ActionResult> GetStatementExport(string accountAddress, DateTime? fromTime, DateTime? toTime)
    {
        var dbContext = await _dbContextFactory.CreateDbContextAsync();

        if (!Concordium.Sdk.Types.AccountAddress.TryParse(accountAddress, out var parsed))
        {
            return BadRequest("Invalid account format.");
        }

        var baseAddress = new AccountAddress(parsed!.GetBaseAddress().ToString());
        var account = dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefault(account => account.BaseAddress == baseAddress);
        if (account == null)
        {
            return NotFound($"Account '{accountAddress}' does not exist.");
        }

        var query = dbContext.AccountStatementEntries
            .AsNoTracking()
            .OrderByDescending(x => x.Timestamp)
            .Where(x => x.AccountId == account.Id)
            ;


        if (fromTime != null && fromTime.Value.Kind != DateTimeKind.Utc)
        {
            return BadRequest("Time zone missing on 'fromTime'.");
        }

        DateTimeOffset from = fromTime ?? DateTime.Now.AddDays(-31);

        if (toTime != null && toTime.Value.Kind != DateTimeKind.Utc)
        {
            return BadRequest("Time zone missing on 'toTime'.");
        }

        DateTimeOffset to = toTime ?? DateTime.Now;

        if ((to - from).TotalDays > MAX_DAYS)
        {
            return BadRequest("Chosen time span exceeds max allowed days: '{MAX_DAYS}'");
        }

        query = query.Where(e => e.Timestamp >= from);
        query = query.Where(e => e.Timestamp <= to);

        var result = query.Select(x => string.Format("{0}, {1}, {2}, {3}\n", 
        x.Timestamp.ToString("u"), 
        (x.Amount / (decimal)CcdAmount.MicroCcdPerCcd).ToString(CultureInfo.InvariantCulture),
        (x.AccountBalance / (decimal)CcdAmount.MicroCcdPerCcd).ToString(CultureInfo.InvariantCulture),
         x.EntryType
        )
        );

        return new EnumerableFileResult<string>(result, new StreamWritingAdapter()) {
            FileDownloadName = $"statement-{accountAddress}_{from:yyyyMMdd}Z-{to:yyyyMMdd}Z.csv"
        };
    }
    private class StreamWritingAdapter : IStreamWritingAdapter<String>
    {
        public string ContentType => "text/csv";

        public async Task WriteAsync(string item, Stream stream)
        {
            byte[] line = Encoding.ASCII.GetBytes(item);
            await stream.WriteAsync(line);
        }

        public Task WriteFooterAsync(Stream stream, int recordCount) => Task.CompletedTask;
        public async Task WriteHeaderAsync(Stream stream)
        {
            byte[] line = Encoding.ASCII.GetBytes("Time,Amount (CCD),Balance (CCD),Label\n");
            await stream.WriteAsync(line);
        }
    }
}

