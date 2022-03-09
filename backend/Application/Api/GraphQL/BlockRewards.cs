using HotChocolate;

namespace Application.Api.GraphQL;

public class BlockRewards
{
    public ulong TransactionFees { get; init; }
    public ulong OldGasAccount { get; init; }
    public ulong NewGasAccount { get; init; }
    public ulong BakerReward { get; init; }
    public ulong FoundationCharge { get; init; }
    [GraphQLDeprecated("Use 'bakerAccountAddressString' instead. Type of this field will be changed to AccountAddress in the near future.")]
    public string BakerAccountAddress { get; init; }
    public string BakerAccountAddressString => BakerAccountAddress;
    [GraphQLDeprecated("Use 'foundationAccountAddressString' instead. Type of this field will be changed to AccountAddress in the near future.")]
    public string FoundationAccountAddress { get; init; }
    public string FoundationAccountAddressString => FoundationAccountAddress;
}