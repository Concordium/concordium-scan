using System.Linq;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class TransactionSuccessResult : TransactionResult
{
    public TransactionResultEvent[] Events { get; init; }
    
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        return Events.SelectMany(x => x.GetAccountAddresses());
    }
}

public abstract record TransactionResultEvent
{
    public virtual IEnumerable<AccountAddress> GetAccountAddresses()
    {
        return Array.Empty<AccountAddress>();
    }
}

public record ModuleDeployed(
    ModuleRef Contents) : TransactionResultEvent;

public record ContractInitialized(
    ModuleRef Ref,
    ContractAddress Address,
    CcdAmount Amount, 
    string InitName,
    BinaryData[] Events) : TransactionResultEvent;

public record Updated(
    ContractAddress Address,
    Address Instigator,
    CcdAmount Amount,
    BinaryData Message,
    string ReceiveName,
    BinaryData[] Events) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        if (Instigator is AccountAddress accountAddress)
            yield return accountAddress;
    }
}

public record Transferred(
    CcdAmount Amount,
    Address To,
    Address From) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        if (To is AccountAddress toAccountAddress)
            yield return toAccountAddress;
        if (From is AccountAddress fromAccountAddress)
            yield return fromAccountAddress;
    }
}

public record AccountCreated(
    AccountAddress Contents) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Contents;
    }
}

public record CredentialDeployed(
    string RegId, // CredentialRegistrationID: ArCurve
    AccountAddress Account) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}

public record BakerAdded(
    CcdAmount Stake,
    bool RestakeEarnings,
    ulong BakerId,
    AccountAddress Account,
    string SignKey,
    string ElectionKey,
    string AggregationKey) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}

public record BakerRemoved(
    AccountAddress Account,
    ulong BakerId) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}

public record BakerStakeIncreased(
    ulong BakerId,
    AccountAddress Account,
    CcdAmount NewStake) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}

public record BakerStakeDecreased(
    ulong BakerId,
    AccountAddress Account,
    CcdAmount NewStake) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}

public record BakerSetRestakeEarnings(
    ulong BakerId,
    AccountAddress Account,
    bool RestakeEarnings) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}

public record BakerKeysUpdated(
    ulong BakerId,
    AccountAddress Account,
    string SignKey,
    string ElectionKey,
    string AggregationKey) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}

public record CredentialKeysUpdated(
    string CredId) : TransactionResultEvent; // CredentialRegistrationID: ArCurve

public record NewEncryptedAmount(
    AccountAddress Account,
    ulong NewIndex,
    string EncryptedAmount) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}

public record EncryptedAmountsRemoved(
    AccountAddress Account,
    string NewAmount,
    string InputAmount,
    ulong UpToIndex) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}

public record AmountAddedByDecryption(
    CcdAmount Amount,
    AccountAddress Account) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}

public record EncryptedSelfAmountAdded(
    AccountAddress Account,
    string NewAmount,
    CcdAmount Amount) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}

public record UpdateEnqueued(
    UnixTimeSeconds EffectiveTime,
    UpdatePayload Payload) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        if (Payload is FoundationAccountUpdatePayload foundationAccountUpdatePayload)
            yield return foundationAccountUpdatePayload.Account;
    }
}

public record TransferredWithSchedule(
    AccountAddress From,
    AccountAddress To,
    TimestampedAmount[] Amount) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return To;
        yield return From;
    }
}

public record TimestampedAmount(DateTimeOffset Timestamp, CcdAmount Amount);

public record CredentialsUpdated(
    AccountAddress Account,
    string[] NewCredIds, // CredentialRegistrationID: ArCurve
    string[] RemovedCredIds, // CredentialRegistrationID: ArCurve
    byte NewThreshold) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}
    
public record DataRegistered(
    RegisteredData Data) : TransactionResultEvent;

public record TransferMemo(
    Memo Memo) : TransactionResultEvent;
