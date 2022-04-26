using Application.Api.GraphQL.Accounts;
using HotChocolate;

namespace Application.Api.GraphQL.Blocks;

public class BlockRewards
{
    public ulong TransactionFees { get; init; }
    public ulong OldGasAccount { get; init; }
    public ulong NewGasAccount { get; init; }
    public ulong BakerReward { get; init; }
    public ulong FoundationCharge { get; init; }
    public AccountAddress BakerAccountAddress { get; init; }
    public AccountAddress FoundationAccountAddress { get; init; }
}