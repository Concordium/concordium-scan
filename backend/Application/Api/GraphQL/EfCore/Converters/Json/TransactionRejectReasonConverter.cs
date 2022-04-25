using Application.Api.GraphQL.Transactions;

namespace Application.Api.GraphQL.EfCore.Converters.Json;

public class TransactionRejectReasonConverter : PolymorphicJsonConverter<TransactionRejectReason>
{
    private static readonly Dictionary<Type, int> SerializeMap = new()
    {
        { typeof(ModuleNotWf), 1 },
        { typeof(ModuleHashAlreadyExists), 2 },
        { typeof(InvalidAccountReference), 3 },
        { typeof(InvalidInitMethod), 4 },
        { typeof(InvalidReceiveMethod), 5 },
        { typeof(InvalidModuleReference), 6 },
        { typeof(InvalidContractAddress), 7 },
        { typeof(RuntimeFailure), 8 },
        { typeof(AmountTooLarge), 9 },
        { typeof(SerializationFailure), 10 },
        { typeof(OutOfEnergy), 11 },
        { typeof(RejectedInit), 12 },
        { typeof(RejectedReceive), 13 },
        { typeof(NonExistentRewardAccount), 14 },
        { typeof(InvalidProof), 15 },
        { typeof(AlreadyABaker), 16 },
        { typeof(NotABaker), 17 },
        { typeof(InsufficientBalanceForBakerStake), 18 },
        { typeof(StakeUnderMinimumThresholdForBaking), 19 },
        { typeof(BakerInCooldown), 20 },
        { typeof(DuplicateAggregationKey), 21 },
        { typeof(NonExistentCredentialId), 22 },
        { typeof(KeyIndexAlreadyInUse), 23 },
        { typeof(InvalidAccountThreshold), 24 },
        { typeof(InvalidCredentialKeySignThreshold), 25 },
        { typeof(InvalidEncryptedAmountTransferProof), 26 },
        { typeof(InvalidTransferToPublicProof), 27 },
        { typeof(EncryptedAmountSelfTransfer), 28 },
        { typeof(InvalidIndexOnEncryptedTransfer), 29 },
        { typeof(ZeroScheduledAmount), 30 },
        { typeof(NonIncreasingSchedule), 31 },
        { typeof(FirstScheduledReleaseExpired), 32 },
        { typeof(ScheduledSelfTransfer), 33 },
        { typeof(InvalidCredentials), 34 },
        { typeof(DuplicateCredIds), 35 },
        { typeof(NonExistentCredIds), 36 },
        { typeof(RemoveFirstCredential), 37 },
        { typeof(CredentialHolderDidNotSign), 38 },
        { typeof(NotAllowedMultipleCredentials), 39 },
        { typeof(NotAllowedToReceiveEncrypted), 40 },
        { typeof(NotAllowedToHandleEncrypted), 41 },
    };

    public TransactionRejectReasonConverter() : base(SerializeMap)
    {
    }
}