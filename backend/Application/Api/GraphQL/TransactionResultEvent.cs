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

/// <summary>
/// The public balance of the account was increased via a transfer from
/// encrypted to public balance.
/// </summary>
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

/// <summary>
/// Event generated when one or more encrypted amounts are consumed from the account
/// </summary>
/// <param name="AccountAddress">The affected account</param>
/// <param name="NewEncryptedAmount">The new self encrypted amount on the affected account</param>
/// <param name="InputAmount">The input encrypted amount that was removed</param>
/// <param name="UpToIndex">The index indicating which amounts were used</param>
public record EncryptedAmountsRemoved(
    string AccountAddress,
    string NewEncryptedAmount,
    string InputAmount,
    ulong UpToIndex) : TransactionResultEvent;

/// <summary>
/// The encrypted balance of the account was updated due to transfer from
/// public to encrypted balance of the account.
/// </summary>
/// <param name="AccountAddress">The affected account</param>
/// <param name="NewEncryptedAmount">The new self encrypted amount of the account</param>
/// <param name="Amount">The amount that was transferred from public to encrypted balance</param>
public record EncryptedSelfAmountAdded(
    string AccountAddress,
    string NewEncryptedAmount,
    ulong Amount) : TransactionResultEvent;

public record ModuleDeployed(
    string ModuleRef) : TransactionResultEvent;

/// <summary>
/// A new encrypted amount was added to the account.
/// </summary>
/// <param name="AccountAddress">The account onto which the amount was added.</param>
/// <param name="NewIndex">The index the amount was assigned.</param>
/// <param name="EncryptedAmount">The encrypted amount that was added.</param>
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