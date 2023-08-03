using Concordium.Sdk.Types;
using HotChocolate.Types;

namespace Application.Api.GraphQL.Configurations;

public sealed class UpdateTypeType : EnumType<UpdateType> {
    protected override void Configure(IEnumTypeDescriptor<UpdateType> descriptor)
    {
        // Change enum names from gRPC v2 to align with gRPC v1 to avoid breaking schema changes.
        descriptor.Name("UpdateTransactionType");

        ChangeEnumNames(descriptor);
    }
    
    /// <summary>
    /// Change enum value names from those in gRPC v2 to avoid breaking schema changes.
    /// </summary>
    private static void ChangeEnumNames(IEnumTypeDescriptor<UpdateType> descriptor)
    {
        descriptor.Value(UpdateType.ProtocolUpdate)
            .Name("UPDATE_PROTOCOL");
        descriptor.Value(UpdateType.ElectionDifficultyUpdate)
            .Name("UPDATE_ELECTION_DIFFICULTY");
        descriptor.Value(UpdateType.EuroPerEnergyUpdate)
            .Name("UPDATE_EURO_PER_ENERGY");
        descriptor.Value(UpdateType.MicroCcdPerEuroUpdate)
            .Name("UPDATE_MICRO_GTU_PER_EURO");
        descriptor.Value(UpdateType.FoundationAccountUpdate)
            .Name("UPDATE_FOUNDATION_ACCOUNT");
        descriptor.Value(UpdateType.MintDistributionUpdate)
            .Name("UPDATE_MINT_DISTRIBUTION");
        descriptor.Value(UpdateType.TransactionFeeDistributionUpdate)
            .Name("UPDATE_TRANSACTION_FEE_DISTRIBUTION");
        descriptor.Value(UpdateType.GasRewardsUpdate)
            .Name("UPDATE_GAS_REWARDS");
        descriptor.Value(UpdateType.BakerStakeThresholdUpdate)
            .Name("UPDATE_BAKER_STAKE_THRESHOLD");
        descriptor.Value(UpdateType.AddAnonymityRevokerUpdate)
            .Name("UPDATE_ADD_ANONYMITY_REVOKER");
        descriptor.Value(UpdateType.AddIdentityProviderUpdate)
            .Name("UPDATE_ADD_IDENTITY_PROVIDER");
        descriptor.Value(UpdateType.RootUpdate)
            .Name("UPDATE_ROOT_KEYS");
        descriptor.Value(UpdateType.Level1Update)
            .Name("UPDATE_LEVEL1_KEYS");
        descriptor.Value(UpdateType.Level2Update)
            .Name("UPDATE_LEVEL2_KEYS");
        descriptor.Value(UpdateType.PoolParametersCpv1Update)
            .Name("UPDATE_POOL_PARAMETERS");
        descriptor.Value(UpdateType.CooldownParametersCpv1Update)
            .Name("UPDATE_COOLDOWN_PARAMETERS");
        descriptor.Value(UpdateType.TimeParametersCpv1Update)
            .Name("UPDATE_TIME_PARAMETERS");
    }
}