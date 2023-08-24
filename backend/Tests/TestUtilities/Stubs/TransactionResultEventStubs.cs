using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Transactions;

namespace Tests.TestUtilities.Stubs;

internal static class TransactionResultEventStubs
{
    private const string Address = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd";
    private const string ParameterHex = "080000d671a4d50101c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32";
    private const string Name = "inventory.transfer";
    private const ulong Amount = 15674371UL;
    private const string FirstEvent = "05080000d671a4d501aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c90309c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32";
    private const string SecondEvent = "01080000d671a4d50101aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c9030901c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32";

    internal static ContractUpdated ContractUpdated(
        ulong index = 1423UL,
        ulong subIndex = 1UL,
        ContractVersion? version = null) =>
        new(
            new ContractAddress(index, subIndex),
            new AccountAddress(Address),
            Amount,
            ParameterHex,
            Name,
            version,
            new[] { FirstEvent, SecondEvent }
        );
}