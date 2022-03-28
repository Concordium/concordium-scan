using HotChocolate;

namespace Application.Api.GraphQL;

public class BlockRewards
{
    public ulong TransactionFees { get; init; }
    public ulong OldGasAccount { get; init; }
    public ulong NewGasAccount { get; init; }
    public ulong BakerReward { get; init; }
    public ulong FoundationCharge { get; init; }
    public AccountAddress BakerAccountAddress { get; init; }
    [GraphQLDeprecated("Use 'bakerAccountAddress.asString' instead. This field will be removed in the near future.")]
    public string BakerAccountAddressString => BakerAccountAddress.AsString;
    public AccountAddress FoundationAccountAddress { get; init; }
    [GraphQLDeprecated("Use 'foundationAccountAddressString' instead. This field will be removed in the near future.")]
    public string FoundationAccountAddressString => FoundationAccountAddress.AsString;
}