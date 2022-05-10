using Application.Api.GraphQL.Transactions;

namespace Application.Api.GraphQL.EfCore.Converters.Json;

public class TransactionResultEventConverter : PolymorphicJsonConverter<TransactionResultEvent>
{
    private static readonly Dictionary<Type, int> SerializeMap = new()
    {
        { typeof(Transferred), 1 },
        { typeof(AccountCreated), 2 },
        { typeof(CredentialDeployed), 3 },
        { typeof(BakerAdded), 4 },
        { typeof(BakerKeysUpdated), 5 },
        { typeof(BakerRemoved), 6 },
        { typeof(BakerSetRestakeEarnings), 7 },
        { typeof(BakerStakeDecreased), 8 },
        { typeof(BakerStakeIncreased), 9 },
        { typeof(AmountAddedByDecryption), 10 },
        { typeof(EncryptedAmountsRemoved), 11 },
        { typeof(EncryptedSelfAmountAdded), 12 },
        { typeof(NewEncryptedAmount), 13 },
        { typeof(CredentialKeysUpdated), 14 },
        { typeof(CredentialsUpdated), 15 },
        { typeof(ContractInitialized), 16 },
        { typeof(ContractModuleDeployed), 17 },
        { typeof(ContractUpdated), 18 },
        { typeof(TransferredWithSchedule), 19 },
        { typeof(DataRegistered), 20 },
        { typeof(TransferMemo), 21 },
        { typeof(ChainUpdateEnqueued), 22 },
        { typeof(BakerSetOpenStatus), 23 },
        { typeof(BakerSetMetadataURL), 24 },
        { typeof(BakerSetTransactionFeeCommission), 25 },
        { typeof(BakerSetBakingRewardCommission), 26 },
        { typeof(BakerSetFinalizationRewardCommission), 27 },
        { typeof(DelegationAdded), 28 },
        { typeof(DelegationRemoved), 29 },
        { typeof(DelegationSetRestakeEarnings), 30 },
        { typeof(DelegationSetDelegationTarget), 31 },
        { typeof(DelegationStakeIncreased), 32 },
        { typeof(DelegationStakeDecreased), 33 },
    };

    public TransactionResultEventConverter() : base(SerializeMap)
    {
    }
}