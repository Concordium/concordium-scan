namespace Application.Api.GraphQL.Accounts;

public record DelegationSummary(
    AccountAddress AccountAddress,
    ulong StakedAmount,
    bool RestakeEarnings);