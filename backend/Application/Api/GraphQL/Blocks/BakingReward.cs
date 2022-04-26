using Application.Api.GraphQL.Accounts;
using HotChocolate;

namespace Application.Api.GraphQL.Blocks;

public class BakingReward
{
    public ulong Amount { get; init; }
    public AccountAddress Address { get; init; }
}