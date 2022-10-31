using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using CsvHelper;
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
    public async Task<ActionResult> GetStatementExport(string accountAddress)
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
            return NotFound("account does not exist");
        }

        var query = dbContext.AccountStatementEntries
            .AsNoTracking()
            .Where(x => x.AccountId == account.Id);

        var result = query.Select(x => new
        {
            Time = x.Timestamp,
            Amount = x.Amount / 1e6,
            Label = x.EntryType,
        });
        var values = await result.ToListAsync();

        await using var csv = new StringWriter();
        await using (var writer = new CsvWriter(csv, CultureInfo.InvariantCulture))
        {
            await writer.WriteRecordsAsync(values);
        }

        return new FileContentResult(Encoding.ASCII.GetBytes(csv.ToString()), "text/csv")
        {
            FileDownloadName = $"statement-{accountAddress}.csv",
        };
    }
}