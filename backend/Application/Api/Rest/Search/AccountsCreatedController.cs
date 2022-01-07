using Application.Persistence;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Microsoft.AspNetCore.Mvc;

namespace Application.Api.Rest.Search;

[ApiController]
[Route("api/search/[controller]")]
public class AccountsCreatedController : ControllerBase
{
    private readonly BlockRepository _repository;

    public AccountsCreatedController(BlockRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public ActionResult<string[]> Get(DateTimeOffset? startTime, DateTimeOffset? endTime, bool includeInitial = false, bool includeNormal = false)
    {
        if (!startTime.HasValue || !endTime.HasValue)
            return BadRequest("Start and end time must be provided");
        if (startTime.Value.Offset != TimeSpan.Zero || endTime.Value.Offset != TimeSpan.Zero)
            return BadRequest("Start and end time must be explicitly provided with offset Zero.");
        
        Log.Information("Retrieving accounts created between {startTime:u} and {endTime:u} [includeInitial:{includeInitial}] [includeNormal:{includeNormal}]", startTime, endTime, includeInitial, includeNormal);
        
        var transactionTypes = GetTransactionTypes(includeInitial, includeNormal);

        var result = _repository
            .FindTransactionSummaries(startTime.Value, endTime.Value, transactionTypes)
            .Select(x => x.Result)
            .OfType<TransactionSuccessResult>()
            .Select(CreateResult)
            .ToArray();

        return result.Select(x => x.AsString).ToArray();
    }

    private static TransactionType[] GetTransactionTypes(bool includeInitial, bool includeNormal)
    {
        var transactionTypes = new List<TransactionType>();
        if (includeInitial)
            transactionTypes.Add(TransactionType.Get(CredentialDeploymentTransactionType.Initial));
        if (includeNormal)
            transactionTypes.Add(TransactionType.Get(CredentialDeploymentTransactionType.Normal));
        return transactionTypes.ToArray();
    }

    private AccountAddress CreateResult(TransactionSuccessResult result)
    {
        var accountAddressAsString = result.Events
            .Cast<JsonTransactionResultEvent>()
            .Select(x => x.Data)
            .Single(e => e.GetProperty("tag").GetString() == "AccountCreated")
            .GetProperty("contents").GetString()!;
        
        return new AccountAddress(accountAddressAsString);
    }
}
