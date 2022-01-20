using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConcordiumSdk.NodeApi.Types.JsonConverters;

public class TransactionRejectReasonConverter : JsonConverter<TransactionRejectReason>
{
    private readonly IDictionary<string, Type> _deserializeMap;
    private readonly IDictionary<Type, string> _serializeMap;

    public TransactionRejectReasonConverter()
    {
        _deserializeMap = new Dictionary<string, Type>()
        {
            { "ModuleNotWF", typeof(ModuleNotWf) },
            { "ModuleHashAlreadyExists", typeof(ModuleHashAlreadyExists) },
            { "InvalidAccountReference", typeof(InvalidAccountReference) },
            { "InvalidInitMethod", typeof(InvalidInitMethod) },
            { "InvalidReceiveMethod", typeof(InvalidReceiveMethod) },
            { "InvalidModuleReference", typeof(InvalidModuleReference) },
            { "InvalidContractAddress", typeof(InvalidContractAddress) },
            { "RuntimeFailure", typeof(RuntimeFailure) },
            { "AmountTooLarge", typeof(AmountTooLarge) },
            { "SerializationFailure", typeof(SerializationFailure) },
            { "OutOfEnergy", typeof(OutOfEnergy) },
            { "RejectedInit", typeof(RejectedInit) },
            { "RejectedReceive", typeof(RejectedReceive) },
            { "NonExistentRewardAccount", typeof(NonExistentRewardAccount) },
            { "InvalidProof", typeof(InvalidProof) },
            { "AlreadyABaker", typeof(AlreadyABaker) },
            { "NotABaker", typeof(NotABaker) },
            { "InsufficientBalanceForBakerStake", typeof(InsufficientBalanceForBakerStake) },
            { "StakeUnderMinimumThresholdForBaking", typeof(StakeUnderMinimumThresholdForBaking) },
            { "BakerInCooldown", typeof(BakerInCooldown) },
            { "DuplicateAggregationKey", typeof(DuplicateAggregationKey) },
            { "NonExistentCredentialID", typeof(NonExistentCredentialId) },
            { "KeyIndexAlreadyInUse", typeof(KeyIndexAlreadyInUse) },
            { "InvalidAccountThreshold", typeof(InvalidAccountThreshold) },
            { "InvalidCredentialKeySignThreshold", typeof(InvalidCredentialKeySignThreshold) },
            { "InvalidEncryptedAmountTransferProof", typeof(InvalidEncryptedAmountTransferProof) },
            { "InvalidTransferToPublicProof", typeof(InvalidTransferToPublicProof) },
            { "EncryptedAmountSelfTransfer", typeof(EncryptedAmountSelfTransfer) },
            { "InvalidIndexOnEncryptedTransfer", typeof(InvalidIndexOnEncryptedTransfer) },
            { "ZeroScheduledAmount", typeof(ZeroScheduledAmount) },
            { "NonIncreasingSchedule", typeof(NonIncreasingSchedule) },
            { "FirstScheduledReleaseExpired", typeof(FirstScheduledReleaseExpired) },
            { "ScheduledSelfTransfer", typeof(ScheduledSelfTransfer) },
            { "InvalidCredentials", typeof(InvalidCredentials) },
            { "DuplicateCredIDs", typeof(DuplicateCredIds) },
            { "NonExistentCredIDs", typeof(NonExistentCredIds) },
            { "RemoveFirstCredential", typeof(RemoveFirstCredential) },
            { "CredentialHolderDidNotSign", typeof(CredentialHolderDidNotSign) },
            { "NotAllowedMultipleCredentials", typeof(NotAllowedMultipleCredentials) },
            { "NotAllowedToReceiveEncrypted", typeof(NotAllowedToReceiveEncrypted) },
            { "NotAllowedToHandleEncrypted", typeof(NotAllowedToHandleEncrypted) },
        };
        
        _serializeMap = _deserializeMap
            .ToDictionary(x => x.Value, x => x.Key);
    }

    public override TransactionRejectReason? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var tagValue = ReadTagValue(reader);
        if (!_deserializeMap.TryGetValue(tagValue, out var tagType))
            throw new NotImplementedException($"Deserialization of '{tagValue}' is not implemented.");
        return (TransactionRejectReason)JsonSerializer.Deserialize(ref reader, tagType, options)!;
    }

    private string ReadTagValue(Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("Expected a start object");

        reader.Read();
        while (!(reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "tag"))
            reader.Read();
        
        reader.Read();
        return reader.GetString()!;
    }

    public override void Write(Utf8JsonWriter writer, TransactionRejectReason value, JsonSerializerOptions options)
    {
        var valueType = value.GetType();
        var serializedBytes = (Span<byte>)JsonSerializer.SerializeToUtf8Bytes(value, valueType, options);
        var serializedBytesWithoutStartAndEndObjectTag = serializedBytes.Slice(1, serializedBytes.Length - 2);

        if (!_serializeMap.TryGetValue(valueType, out var tagValue))
            throw new InvalidOperationException($"Type {valueType.Name} is not represented in mapping dictionary.");
        
        writer.WriteStartObject();
        writer.WritePropertyName("tag");
        writer.WriteStringValue(tagValue);
        if (!serializedBytesWithoutStartAndEndObjectTag.IsEmpty)
            writer.WriteRawValue(serializedBytesWithoutStartAndEndObjectTag, true);
        writer.WriteEndObject();
    }
}