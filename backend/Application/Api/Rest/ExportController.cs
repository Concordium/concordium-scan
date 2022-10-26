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
    public async Task<ActionResult> GetStatementExport(string accountAddress)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

        if (!ConcordiumSdk.Types.AccountAddress.TryParse(accountAddress, out var parsed))
            throw new InvalidOperationException("Account does not exist!");

        var baseAddress = new AccountAddress(parsed!.GetBaseAddress().AsString);
        var account = dbContext.Accounts
            .AsNoTracking()
            .SingleOrDefault(account => account.BaseAddress == baseAddress);
        if (account == null)
            throw new InvalidOperationException("Account does not exist!");

        var query = dbContext.AccountStatementEntries
            .AsNoTracking()
            .Where(x => x.AccountId == account.Id);

        var scalarQuery = query.Select(x => new
        {
            x.Timestamp,
            x.EntryType,
            x.Amount,
        });
        var values = await scalarQuery.ToListAsync();
        // TODO Use something like 'CsvHelper' (see 'https://joshclose.github.io/CsvHelper/examples/writing/write-anonymous-type-objects/')?
        var sb = new StringBuilder("Time,Amount (CCD),Label\n");
        var csv = values.Aggregate(sb, (acc, v) => acc.Append($"{v.Timestamp.ToString("u")},{v.Amount / 1e6},{v.EntryType}\n"));
        var result = new FileContentResult(Encoding.ASCII.GetBytes(csv.ToString()), "text/csv")
        {
            FileDownloadName = $"statement-{accountAddress}.csv",
        };
        return result;
    }
}