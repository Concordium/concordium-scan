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

    public override IEnumerable<ulong> GetBakerIds()
    {
        return Events.SelectMany(x => x.GetBakerIds());
    }

    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates(TransactionSummary owningTransaction)
    {
        return Events.SelectMany(evt => evt.GetAccountBalanceUpdates(owningTransaction));
    }
}

public abstract record TransactionResultEvent
{
    public virtual IEnumerable<AccountAddress> GetAccountAddresses()
    {
        return Array.Empty<AccountAddress>();
    }

    public virtual IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates(TransactionSummary owningTransaction)
    {
        return Enumerable.Empty<AccountBalanceUpdate>();
    }

    public virtual IEnumerable<ulong> GetBakerIds()
    {
        return Enumerable.Empty<ulong>();
    }
}

public record ModuleDeployed(
    ModuleRef Contents) : TransactionResultEvent;

public record ContractInitialized(
    ModuleRef Ref,
    ContractAddress Address,
    CcdAmount Amount,
    string InitName,
    BinaryData[] Events) : TransactionResultEvent
{
    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates(TransactionSummary owningTransaction)
    {
        if (Amount > CcdAmount.Zero)
        {
            var accountAddress = owningTransaction.Sender ?? throw new InvalidOperationException("Sender of transaction was null for transaction that included a contract initialized event.");
            yield return new AccountBalanceUpdate(accountAddress, -1 * (long)Amount.MicroCcdValue, BalanceUpdateType.TransferOut);
        }
    }
}

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

    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates(TransactionSummary owningTransaction)
    {
        if (Instigator is AccountAddress accountAddress && Amount > CcdAmount.Zero)
            yield return new AccountBalanceUpdate(accountAddress, -1 * (long)Amount.MicroCcdValue, BalanceUpdateType.TransferOut);
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

    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates(TransactionSummary owningTransaction)
    {
        if (From is AccountAddress fromAccountAddress)
            yield return new AccountBalanceUpdate(fromAccountAddress, -1 * (long)Amount.MicroCcdValue, BalanceUpdateType.TransferOut);
        if (To is AccountAddress toAccountAddress)
            yield return new AccountBalanceUpdate(toAccountAddress, (long)Amount.MicroCcdValue, BalanceUpdateType.TransferIn);
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

    public override IEnumerable<ulong> GetBakerIds()
    {
        yield return BakerId;
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

    public override IEnumerable<ulong> GetBakerIds()
    {
        yield return BakerId;
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

    public override IEnumerable<ulong> GetBakerIds()
    {
        yield return BakerId;
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

    public override IEnumerable<ulong> GetBakerIds()
    {
        yield return BakerId;
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

    public override IEnumerable<ulong> GetBakerIds()
    {
        yield return BakerId;
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

    public override IEnumerable<ulong> GetBakerIds()
    {
        yield return BakerId;
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

    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates(TransactionSummary owningTransaction)
    {
        yield return new AccountBalanceUpdate(Account, (long)Amount.MicroCcdValue, BalanceUpdateType.AmountDecrypted);
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

    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates(TransactionSummary owningTransaction)
    {
        yield return new AccountBalanceUpdate(Account, -1 * (long)Amount.MicroCcdValue, BalanceUpdateType.AmountEncrypted);
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

    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates(TransactionSummary owningTransaction)
    {
        var totalAmount = Amount.Sum(amount => (long)amount.Amount.MicroCcdValue);
        yield return new AccountBalanceUpdate(From, -1 * totalAmount, BalanceUpdateType.TransferOut);
        yield return new AccountBalanceUpdate(To, totalAmount, BalanceUpdateType.TransferIn);
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

// TODO: Interrupted 
// TODO: Resumed

public record BakerSetOpenStatus(
    ulong BakerId,
    AccountAddress Account,
    BakerPoolOpenStatus OpenStatus) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }

    public override IEnumerable<ulong> GetBakerIds()
    {
        yield return BakerId;
    }
}

public record BakerSetMetadataURL(
    ulong BakerId,
    AccountAddress Account,
    string MetadataURL) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }

    public override IEnumerable<ulong> GetBakerIds()
    {
        yield return BakerId;
    }
}

public record BakerSetTransactionFeeCommission(
    ulong BakerId,
    AccountAddress Account,
    decimal TransactionFeeCommission) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }

    public override IEnumerable<ulong> GetBakerIds()
    {
        yield return BakerId;
    }
}

public record BakerSetBakingRewardCommission(
    ulong BakerId,
    AccountAddress Account,
    decimal BakingRewardCommission) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }

    public override IEnumerable<ulong> GetBakerIds()
    {
        yield return BakerId;
    }
}

public record BakerSetFinalizationRewardCommission(
    ulong BakerId,
    AccountAddress Account,
    decimal FinalizationRewardCommission) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }

    public override IEnumerable<ulong> GetBakerIds()
    {
        yield return BakerId;
    }
}
    
public record DelegationStakeIncreased(
    ulong DelegatorId,
    AccountAddress Account,
    CcdAmount NewStake) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}

public record DelegationStakeDecreased(
    ulong DelegatorId,
    AccountAddress Account,
    CcdAmount NewStake) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}

public record DelegationSetRestakeEarnings(
    ulong DelegatorId,
    AccountAddress Account,
    bool RestakeEarnings) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}

public record DelegationSetDelegationTarget(
    ulong DelegatorId,
    AccountAddress Account,
    DelegationTarget DelegationTarget) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}

public record DelegationAdded(
    ulong DelegatorId,
    AccountAddress Account) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}

public record DelegationRemoved(
    ulong DelegatorId,
    AccountAddress Account) : TransactionResultEvent
{
    public override IEnumerable<AccountAddress> GetAccountAddresses()
    {
        yield return Account;
    }
}
