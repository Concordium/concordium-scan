using HotChocolate.Types;

namespace Application.Api.GraphQL;

[UnionType("Event")]
public abstract record TransactionResultEvent;

public record Transferred(
    ulong Amount,
    Address From,
    Address To) : TransactionResultEvent;

public record AccountCreated(
    string Address) : TransactionResultEvent;

public record AmountAddedByDecryption(
    ulong Amount,
    string AccountAddress) : TransactionResultEvent;

public record BakerAdded(
    ulong StakedAmount,
    bool RestakeEarnings,
    ulong BakerId,
    string AccountAddress,
    string SignKey,
    string ElectionKey,
    string AggregationKey) : TransactionResultEvent;

public record BakerKeysUpdated(
    ulong BakerId,
    string AccountAddress,
    string SignKey,
    string ElectionKey,
    string AggregationKey) : TransactionResultEvent;

public record BakerRemoved(
    ulong BakerId,
    string AccountAddress) : TransactionResultEvent;

public record BakerSetRestakeEarnings(
    ulong BakerId,
    string AccountAddress,
    bool RestakeEarnings) : TransactionResultEvent;

public record BakerStakeDecreased(
    ulong BakerId,
    string AccountAddress,
    ulong NewStakedAmount) : TransactionResultEvent;

public record BakerStakeIncreased(
    ulong BakerId,
    string AccountAddress,
    ulong NewStakedAmount) : TransactionResultEvent;

public record ContractInitialized(
    string ModuleRef,
    ContractAddress Address,
    ulong Amount,
    string InitName,
    [property:UsePaging(InferConnectionNameFromField = false)]
    IEnumerable<string> Events) : TransactionResultEvent;  //TODO: how should we represent binary data on graphql?

public record CredentialDeployed(
    string RegId,
    string AccountAddress) : TransactionResultEvent;

public record CredentialKeysUpdated(
    string CredId) : TransactionResultEvent;

public record CredentialsUpdated(
    string AccountAddress,
    string[] NewCredIds,
    string[] RemovedCredIds,
    byte NewThreshold) : TransactionResultEvent;

public record DataRegistered(
    string Data) : TransactionResultEvent; //TODO: how should we represent binary data on graphql?

public record EncryptedAmountsRemoved(
    string AccountAddress,
    string NewAmount,
    string InputAmount,
    ulong UpToIndex) : TransactionResultEvent;

public record EncryptedSelfAmountAdded(
    string AccountAddress,
    string NewAmount,
    ulong Amount) : TransactionResultEvent;

public record ModuleDeployed(
    string ModuleRef) : TransactionResultEvent;

public record NewEncryptedAmount(
    string AccountAddress,
    ulong NewIndex,
    string EncryptedAmount) : TransactionResultEvent;

public record TransferMemo(
    string DecodedText,
    TextDecodeType DecodeType,
    string RawHex) : TransactionResultEvent;

public enum TextDecodeType
{
    Cbor,
    None
}

public record TransferredWithSchedule(
    string FromAccountAddress,
    string ToAccountAddress,
    [property:UsePaging]
    IEnumerable<TimestampedAmount> AmountsSchedule) : TransactionResultEvent;

public record TimestampedAmount(DateTimeOffset Timestamp, ulong Amount);

public record UpdateEnqueued(
    DateTime EffectiveTime,
    string UpdateType) : TransactionResultEvent; // TODO: How to represent the updates - probably another large union!

public record Updated(
    ContractAddress Address,
    Address Instigator,
    ulong Amoung,
    string MessageAsHex, // TODO: How to represent binary data?
    string ReceiveName,
    [property:UsePaging(InferConnectionNameFromField = false)]
    IEnumerable<string> EventsAsHex) : TransactionResultEvent; // TODO: How to represent binary data?