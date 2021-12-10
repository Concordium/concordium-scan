using Application.Persistence;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Application.Api.Rest;

public class FindAccountsCreatedQuery
{
    private readonly BlockRepository _repository;

    public FindAccountsCreatedQuery(BlockRepository repository)
    {
        _repository = repository;
    }

    public AccountAddress[] FindAccountsCreated(DateTimeOffset startTime, DateTimeOffset endTime, bool includeInitial, bool includeNormal)
    {
        var transactionTypes = GetTransactionTypes(includeInitial, includeNormal);

        var result = _repository
            .FindTransactionSummaries(startTime, endTime, transactionTypes)
            .Select(x => x.Result)
            .OfType<TransactionSuccessResult>()
            .Select(CreateResult)
            .ToArray();

        return result;
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
            .EnumerateArray()
            .Single(e => e.GetProperty("tag").GetString() == "AccountCreated")
            .GetProperty("contents").GetString()!;

        return new AccountAddress(accountAddressAsString);
    }
}
