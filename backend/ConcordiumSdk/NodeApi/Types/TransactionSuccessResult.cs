using System.Text.Json;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class TransactionSuccessResult : TransactionResult
{
    public TransactionResultEvent[] Events { get; init; }
}

public abstract record TransactionResultEvent;

public record JsonTransactionResultEvent(
    JsonElement Data) : TransactionResultEvent;

public record ModuleDeployed(
    ModuleRef Contents) : TransactionResultEvent;

public record ContractInitialized(
    ModuleRef Ref,
    ContractAddress Address,
    CcdAmount Amount, 
    string InitName,
    ContractEvent[] Events) : TransactionResultEvent;

public record Updated(
    ContractAddress Address,
    Address Instigator,
    CcdAmount Amount,
    ContractParameter Message,
    string ReceiveName,
    ContractEvent[] Events) : TransactionResultEvent;

public record Transferred(
    CcdAmount Amount,
    Address To,
    Address From) : TransactionResultEvent;

public record AccountCreated(
    AccountAddress Contents) : TransactionResultEvent;

public record CredentialDeployed(
    string RegId, // CredentialRegistrationID: ArCurve
    AccountAddress Account) : TransactionResultEvent;

public record BakerAdded(
    CcdAmount Stake,
    bool RestakeEarnings,
    ulong BakerId,
    AccountAddress Account,
    string SignKey,
    string ElectionKey,
    string AggregationKey) : TransactionResultEvent;

public record BakerRemoved(
    AccountAddress Account,
    ulong BakerId) : TransactionResultEvent;

public record BakerStakeIncreased(
    ulong BakerId,
    AccountAddress Account,
    CcdAmount NewStake) : TransactionResultEvent;

public record BakerStakeDecreased(
    ulong BakerId,
    AccountAddress Account,
    CcdAmount NewStake) : TransactionResultEvent;

public record BakerSetRestakeEarnings(
    ulong BakerId,
    AccountAddress Account,
    bool RestakeEarnings) : TransactionResultEvent;

public record BakerKeysUpdated(
    ulong BakerId,
    AccountAddress Account,
    string SignKey,
    string ElectionKey,
    string AggregationKey) : TransactionResultEvent;

public record CredentialKeysUpdated(
    string CredId) : TransactionResultEvent; // CredentialRegistrationID: ArCurve

public record NewEncryptedAmount(
    AccountAddress Account,
    ulong NewIndex,
    string EncryptedAmount) : TransactionResultEvent;

public record EncryptedAmountsRemoved(
    AccountAddress Account,
    string NewAmount,
    string InputAmount,
    ulong UpToIndex) : TransactionResultEvent;

public record AmountAddedByDecryption(
    CcdAmount Amount,
    AccountAddress Account) : TransactionResultEvent;

public record EncryptedSelfAmountAdded(
    AccountAddress Account,
    string NewAmount,
    CcdAmount Amount) : TransactionResultEvent;
    
//TODO: UpdateEnqueued!

public record TransferredWithSchedule(
    AccountAddress From,
    AccountAddress To,
    TimestampedAmount[] Amount) : TransactionResultEvent;

public record TimestampedAmount(DateTimeOffset Timestamp, CcdAmount Amount);
    
public record CredentialsUpdated(
    AccountAddress Account,
    string[] NewCredIds, // CredentialRegistrationID: ArCurve
    string[] RemovedCredIds, // CredentialRegistrationID: ArCurve
    byte NewThreshold) : TransactionResultEvent;
    
public record DataRegistered(
    RegisteredData Data) : TransactionResultEvent;

public record TransferMemo(
    Memo Memo) : TransactionResultEvent;
