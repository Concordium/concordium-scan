using Application.Api.GraphQL.Accounts;

namespace Application.Api.GraphQL.Blocks;

public class FinalizationReward
{
    public ulong Amount { get; init; }
    public AccountAddress Address { get; init; }
}