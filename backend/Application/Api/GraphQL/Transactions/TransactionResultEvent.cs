using System.Threading.Tasks;
using Application.Aggregates.Contract;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL.Bakers;
using Application.Exceptions;
using Application.Interop;
using Concordium.Sdk.Types;
using HotChocolate.Types;
using Serilog.Context;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;
using AccountCreationDetails = Concordium.Sdk.Types.AccountCreationDetails;
using AccountTransactionDetails = Concordium.Sdk.Types.AccountTransactionDetails;
using BakerPoolOpenStatus = Application.Api.GraphQL.Bakers.BakerPoolOpenStatus;
using BakerStakeUpdatedData = Concordium.Sdk.Types.BakerStakeUpdatedData;
using EncryptedAmountRemovedEvent = Concordium.Sdk.Types.EncryptedAmountRemovedEvent;
using NewEncryptedAmountEvent = Concordium.Sdk.Types.NewEncryptedAmountEvent;
using ReceiveName = Application.Types.ReceiveName;
using UpdateDetails = Concordium.Sdk.Types.UpdateDetails;

namespace Application.Api.GraphQL.Transactions;

[UnionType("Event")]
public abstract record TransactionResultEvent
{
    internal static IEnumerable<TransactionResultEvent> ToIter(IBlockItemSummaryDetails blockItemSummaryDetails, DateTimeOffset blockSlotTime)
    {
        switch (blockItemSummaryDetails)
        {
            case AccountCreationDetails accountCreationDetails:
                yield return CredentialDeployed.From(accountCreationDetails);
                yield return AccountCreated.From(accountCreationDetails);
                break;
            case AccountTransactionDetails accountTransactionDetails:
                switch (accountTransactionDetails.Effects)
                {
                    case AccountTransfer accountTransfer:
                        yield return Transferred.From_(
                            accountTransactionDetails.Sender,
                            accountTransfer);
                        if (accountTransfer.Memo != null)
                        {
                            yield return TransferMemo.From(accountTransfer.Memo!);
                        }
                        break;
                    case Concordium.Sdk.Types.BakerAdded bakerAdded:
                        yield return BakerAdded.From(bakerAdded);
                        break;
                    case BakerConfigured bakerConfigured:
                        foreach (var transactionResultEvent in ToIter(accountTransactionDetails.Sender, bakerConfigured))
                        {
                            yield return transactionResultEvent;
                        }
                        break;
                    case Concordium.Sdk.Types.BakerKeysUpdated bakerKeysUpdated:
                        yield return BakerKeysUpdated.From(bakerKeysUpdated);
                        break;
                    case Concordium.Sdk.Types.BakerRemoved bakerRemoved:
                        yield return BakerRemoved.From(accountTransactionDetails.Sender, bakerRemoved);
                        break;
                    case BakerRestakeEarningsUpdated bakerRestakeEarningsUpdated:
                        yield return BakerSetRestakeEarnings.From(
                            accountTransactionDetails.Sender,
                            bakerRestakeEarningsUpdated);
                        break;
                    case BakerStakeUpdated bakerStakeUpdated:
                        if (bakerStakeUpdated.Data == null)
                        {
                            break;
                        }
                        var tryIncreased = BakerStakeIncreased.TryFrom(
                                accountTransactionDetails.Sender,
                                bakerStakeUpdated.Data!,
                                out var increased);
                        BakerStakeDecreased.TryFrom(
                                accountTransactionDetails.Sender,
                                bakerStakeUpdated.Data!,
                                out var decreased);
                        yield return tryIncreased ? increased! : decreased!;
                        break;
                    case Concordium.Sdk.Types.ContractInitialized contractInitialized:
                        yield return ContractInitialized.From(contractInitialized);
                        break;
                    case ContractUpdateIssued contractUpdateIssued:
                        foreach (var transactionResultEvent in ToIter(contractUpdateIssued))
                        {
                            yield return transactionResultEvent;
                        }
                        break;
                    case Concordium.Sdk.Types.CredentialKeysUpdated credentialKeysUpdated:
                        yield return CredentialKeysUpdated.From(credentialKeysUpdated);
                        break;
                    case Concordium.Sdk.Types.CredentialsUpdated credentialsUpdated:
                        yield return CredentialsUpdated.From(
                            accountTransactionDetails.Sender,
                            credentialsUpdated);
                        break;
                    case EncryptedAmountTransferred encryptedAmountTransferred:
                        yield return EncryptedAmountsRemoved.From(encryptedAmountTransferred.Removed);
                        yield return NewEncryptedAmount.From(encryptedAmountTransferred.Added);
                        break;
                    case ModuleDeployed moduleDeployed:
                        yield return ContractModuleDeployed.From(moduleDeployed);
                        break;
                    case TransferredToEncrypted transferredToEncrypted:
                        yield return EncryptedSelfAmountAdded.From(transferredToEncrypted);
                        break;
                    case TransferredToPublic transferredToPublic:
                        yield return AmountAddedByDecryption.From(transferredToPublic);
                        break;
                    case Concordium.Sdk.Types.TransferredWithSchedule transferredWithSchedule:
                        yield return TransferredWithSchedule.From(
                            accountTransactionDetails.Sender,
                            transferredWithSchedule);
                        break;
                    case Concordium.Sdk.Types.DataRegistered dataRegistered:
                        yield return DataRegistered.From(dataRegistered);
                        break;
                    case DelegationConfigured delegationConfigured:
                        foreach (var transactionResultEvent in ToIter(accountTransactionDetails.Sender, delegationConfigured))
                        {
                            yield return transactionResultEvent;
                        }
                        break;
                }
                
                break;
            case UpdateDetails updateDetails:
                if (ChainUpdateEnqueued.TryFrom(updateDetails, blockSlotTime, out var chainUpdateEnqueued))
                {
                    yield return chainUpdateEnqueued!;
                }
                break;
        }
    }
    
    private static IEnumerable<TransactionResultEvent> ToIter(
        Concordium.Sdk.Types.AccountAddress sender,
        DelegationConfigured delegationConfigured)
    {
        foreach (var delegationEvent in delegationConfigured.Data)
        {
            switch (delegationEvent)
            {
                case Concordium.Sdk.Types.DelegationAdded delegationAdded:
                    yield return DelegationAdded.From(sender, delegationAdded);
                    break;
                case Concordium.Sdk.Types.DelegationRemoved delegationRemoved:
                    yield return DelegationRemoved.From(sender, delegationRemoved);
                    break;
                case Concordium.Sdk.Types.DelegationSetDelegationTarget delegationSetDelegationTarget:
                    yield return DelegationSetDelegationTarget.From(sender, delegationSetDelegationTarget);
                    break;
                case Concordium.Sdk.Types.DelegationSetRestakeEarnings delegationSetRestakeEarnings:
                    yield return DelegationSetRestakeEarnings.From(sender, delegationSetRestakeEarnings);
                    break;
                case Concordium.Sdk.Types.DelegationStakeDecreased delegationStakeDecreased:
                    yield return DelegationStakeDecreased.From(sender, delegationStakeDecreased);
                    break;
                case Concordium.Sdk.Types.DelegationStakeIncreased delegationStakeIncreased:
                    yield return DelegationStakeIncreased.From(sender, delegationStakeIncreased);
                    break;
            }
        }
    }

    internal static IEnumerable<TransactionResultEvent> ToIter(ContractUpdateIssued update)
    {
        foreach (var contractTraceElement in update.Effects)
        {
            switch (contractTraceElement)
            {
                case Interrupted interrupted:
                    yield return ContractInterrupted.From(interrupted);
                    break;
                case Resumed resumed:
                    yield return ContractResumed.From(resumed);
                    break;
                case Updated updated:
                    yield return ContractUpdated.From(updated);
                    break;
                case Upgraded upgraded:
                    yield return ContractUpgraded.From_(upgraded);
                    break;
                case Concordium.Sdk.Types.Transferred transferred:
                    yield return Transferred.From_(transferred);
                    break;
            }
        }
    }

    /// <summary>
    /// Try parse hexadecimal events from module schema.
    ///
    /// <see cref="instigator"/> is used to identify the instigator and are used for metric labelling.
    /// </summary>
    protected string[]? GetParsedEvents(
        ModuleReferenceEvent moduleReferenceEvent,
        string contractName,
        string[] eventsAsHex,
        ILogger logger,
        string instigator
        )
    {
        if (moduleReferenceEvent.Schema == null)
        {
            return null;
        }
        var events = new string[eventsAsHex.Length];
        for (var i = 0; i < eventsAsHex.Length; i++)
        {
            try
            {
                var eventAsHex = eventsAsHex[i];
                var eventContract = InteropBinding.GetEventContract(moduleReferenceEvent.Schema, contractName, eventAsHex, moduleReferenceEvent.SchemaVersion);
                if (eventContract == null)
                {
                    logger.Warning("{ContractName} on {Module} got null returned when parsing hexadecimal event {EventAsHex}", contractName, moduleReferenceEvent.ModuleReference, eventAsHex);
                    return null;
                }
                events[i] = eventContract;
            }
            catch (InteropBindingException e)
            {
                Observability.ApplicationMetrics.IncInteropErrors($"{instigator}.{nameof(GetParsedEvents)}", e);
                switch (e.Error)
                {
                    case InteropError.EventNotSupported:
                        logger.Debug(e, "Event's from {ContractName} on {Module} not supported", contractName, moduleReferenceEvent.ModuleReference);
                        break;
                    case InteropError.NoEventInContract:
                        logger.Debug(e, "Event's from {ContractName} not in schema on {Module}", contractName, moduleReferenceEvent.ModuleReference);
                        break;
                    case InteropError.Deserialization:
                        logger.Debug(e, "Error when parsing {Event} from {ContractName} on {Module}", eventsAsHex[i], contractName, moduleReferenceEvent.ModuleReference);
                        break;
                    case InteropError.Undefined:
                    case InteropError.EmptyMessage:
                    case InteropError.NoReceiveInContract:
                    case InteropError.NoParamsInReceive:
                    case InteropError.NoContractInModule:
                    default:
                        logger.Error(e, "Error when parsing events from {ContractName} on {Module}", contractName, moduleReferenceEvent.ModuleReference);
                        break;
                }
                return null;
            }
        }    

        return events;
    }

    private static IEnumerable<TransactionResultEvent> ToIter(Concordium.Sdk.Types.AccountAddress sender, BakerConfigured bakerConfigured)
    {
        foreach (var bakerEvent in bakerConfigured.Data)
        {
            switch (bakerEvent)
            {
                case BakerAddedEvent bakerAddedEvent:
                    yield return BakerAdded.From(bakerAddedEvent);          
                    break;
                case BakerKeysUpdatedEvent bakerKeysUpdatedEvent:
                    yield return BakerKeysUpdated.From(bakerKeysUpdatedEvent);
                    break;
                case BakerRemovedEvent bakerRemovedEvent:
                    yield return BakerRemoved.From(
                        sender,
                        bakerRemovedEvent);
                    break;
                case BakerRestakeEarningsUpdatedEvent bakerRestakeEarningsUpdatedEvent:
                    yield return BakerSetRestakeEarnings.From(
                        sender,
                        bakerRestakeEarningsUpdatedEvent);
                    break;
                case BakerStakeDecreasedEvent bakerStakeDecreasedEvent:
                    yield return BakerStakeDecreased.From(
                        sender,
                        bakerStakeDecreasedEvent);
                    break;
                case BakerStakeIncreasedEvent bakerStakeIncreasedEvent:
                    yield return BakerStakeIncreased.From(
                        sender,
                        bakerStakeIncreasedEvent);
                    break;
                case BakerSetOpenStatusEvent bakerSetOpenStatusEvent:
                    yield return BakerSetOpenStatus.From(
                        sender,
                        bakerSetOpenStatusEvent);
                    break;
                case BakerSetMetadataUrlEvent bakerSetMetadataUrlEvent:
                    yield return BakerSetMetadataURL.From(
                        sender,
                        bakerSetMetadataUrlEvent);
                    break;
                case BakerSetTransactionFeeCommissionEvent bakerSetTransactionFeeCommissionEvent:
                    yield return BakerSetTransactionFeeCommission.From(
                        sender,
                        bakerSetTransactionFeeCommissionEvent);
                    break;
                case BakerSetBakingRewardCommissionEvent bakerSetBakingRewardCommissionEvent:
                    yield return BakerSetBakingRewardCommission.From(
                        sender,
                        bakerSetBakingRewardCommissionEvent
                    );
                    break;
                case BakerSetFinalizationRewardCommissionEvent bakerSetFinalizationRewardCommissionEvent:
                    yield return BakerSetFinalizationRewardCommission.From(
                        sender,
                        bakerSetFinalizationRewardCommissionEvent);
                    break;
            }
        }
    }
}


/// <summary>
/// This event is used in cases
/// - Account transfer. This is where one account transfer CCD to another account.
/// - Contract transfer. When a contract transfer CCD to an account.
/// </summary>
public record Transferred(
    ulong Amount,
    Address From,
    Address To) : TransactionResultEvent
{
    internal static Transferred From_(Concordium.Sdk.Types.AccountAddress sender, AccountTransfer accountTransfer) =>
        new(
            Amount: accountTransfer.Amount.Value,
            From: AccountAddress.From(sender), 
            AccountAddress.From(accountTransfer.To));

    internal static Transferred From_(Concordium.Sdk.Types.Transferred transferred)
    {
        return new Transferred(
            transferred.Amount.Value,
            Address.From(transferred.From),
            Address.From(transferred.To)
            );
    }
}

public record AccountCreated(
    AccountAddress AccountAddress) : TransactionResultEvent
{
    internal static AccountCreated From(AccountCreationDetails accountCreationDetails) => new(AccountAddress.From(accountCreationDetails.Address));
}

/// <summary>
/// The public balance of the account was increased via a transfer from
/// encrypted to public balance.
/// </summary>
public record AmountAddedByDecryption(
    ulong Amount,
    AccountAddress AccountAddress) : TransactionResultEvent
{
    internal static AmountAddedByDecryption From(
        TransferredToPublic blockItemSummaryDetails)
    {
        return new AmountAddedByDecryption(
            blockItemSummaryDetails.Amount.Value,
            AccountAddress.From(blockItemSummaryDetails.Removed.Account)
        );
    }
}

public record BakerAdded(
    ulong StakedAmount,
    bool RestakeEarnings,
    ulong BakerId,
    AccountAddress AccountAddress,
    string SignKey,
    string ElectionKey,
    string AggregationKey) : TransactionResultEvent
{
    internal static BakerAdded From(Concordium.Sdk.Types.BakerAdded bakerAdded) =>
        new(
            bakerAdded.Stake.Value,
            bakerAdded.RestakeEarnings,
            bakerAdded.KeysEvent.BakerId.Id.Index,
            AccountAddress.From(bakerAdded.KeysEvent.Account),
            Convert.ToHexString(bakerAdded.KeysEvent.SignKey).ToLowerInvariant(),
            Convert.ToHexString(bakerAdded.KeysEvent.ElectionKey).ToLowerInvariant(),
            Convert.ToHexString(bakerAdded.KeysEvent.AggregationKey).ToLowerInvariant()
        );

    internal static BakerAdded From(BakerAddedEvent bakerAdded) =>
        new(
            bakerAdded.Stake.Value,
            bakerAdded.RestakeEarnings,
            bakerAdded.KeysEvent.BakerId.Id.Index,
            AccountAddress.From(bakerAdded.KeysEvent.Account),
            Convert.ToHexString(bakerAdded.KeysEvent.SignKey).ToLowerInvariant(),
            Convert.ToHexString(bakerAdded.KeysEvent.ElectionKey).ToLowerInvariant(),
            Convert.ToHexString(bakerAdded.KeysEvent.AggregationKey).ToLowerInvariant()
        );
}

public record BakerKeysUpdated(
    ulong BakerId,
    AccountAddress AccountAddress,
    string SignKey,
    string ElectionKey,
    string AggregationKey) : TransactionResultEvent
{
    internal static BakerKeysUpdated From(Concordium.Sdk.Types.BakerKeysUpdated bakerKeysUpdated) =>
        new(
            bakerKeysUpdated.KeysEvent.BakerId.Id.Index,
            AccountAddress.From(bakerKeysUpdated.KeysEvent.Account),
            Convert.ToHexString(bakerKeysUpdated.KeysEvent.SignKey).ToLowerInvariant(),
            Convert.ToHexString(bakerKeysUpdated.KeysEvent.ElectionKey).ToLowerInvariant(),
            Convert.ToHexString(bakerKeysUpdated.KeysEvent.AggregationKey).ToLowerInvariant()
        );

    internal static BakerKeysUpdated From(BakerKeysUpdatedEvent bakerKeysUpdated) =>
        new(
            bakerKeysUpdated.Data.BakerId.Id.Index,
            AccountAddress.From(bakerKeysUpdated.Data.Account),
            Convert.ToHexString(bakerKeysUpdated.Data.SignKey).ToLowerInvariant(),
            Convert.ToHexString(bakerKeysUpdated.Data.ElectionKey).ToLowerInvariant(),
            Convert.ToHexString(bakerKeysUpdated.Data.AggregationKey).ToLowerInvariant()
        );
}

public record BakerRemoved(
    ulong BakerId,
    AccountAddress AccountAddress) : TransactionResultEvent
{
    internal static BakerRemoved From(Concordium.Sdk.Types.AccountAddress sender, BakerRemovedEvent bakerRemovedEvent) =>
        new(
            bakerRemovedEvent.BakerId.Id.Index,
            AccountAddress.From(sender));

    internal static BakerRemoved From(Concordium.Sdk.Types.AccountAddress sender, Concordium.Sdk.Types.BakerRemoved bakerRemoved) =>
        new(
            bakerRemoved.BakerId.Id.Index,
            AccountAddress.From(sender));
}

public record BakerSetRestakeEarnings(
    ulong BakerId,
    AccountAddress AccountAddress,
    bool RestakeEarnings) : TransactionResultEvent
{
    internal static BakerSetRestakeEarnings From(Concordium.Sdk.Types.AccountAddress sender, BakerRestakeEarningsUpdatedEvent bakerRestakeEarningsUpdatedEvent) =>
        new(
            bakerRestakeEarningsUpdatedEvent.BakerId.Id.Index,
            AccountAddress.From(sender),
            bakerRestakeEarningsUpdatedEvent.RestakeEarnings
        );

    internal static BakerSetRestakeEarnings From(Concordium.Sdk.Types.AccountAddress sender, BakerRestakeEarningsUpdated bakerRestakeEarningsUpdated) =>
        new(
            bakerRestakeEarningsUpdated.BakerId.Id.Index,
            AccountAddress.From(sender),
            bakerRestakeEarningsUpdated.RestakeEarnings
        );
}

public record BakerStakeDecreased(
    ulong BakerId,
    AccountAddress AccountAddress,
    ulong NewStakedAmount) : TransactionResultEvent
{
    internal static bool TryFrom(
        Concordium.Sdk.Types.AccountAddress sender,
        BakerStakeUpdatedData bakerStakeUpdated,
        out BakerStakeDecreased? stakeDecreased)
    {
        stakeDecreased = null;
        if (bakerStakeUpdated.Increased)
        {
            return false;
        }

        stakeDecreased = new BakerStakeDecreased(
            bakerStakeUpdated.BakerId.Id.Index,
            AccountAddress.From(sender),
            bakerStakeUpdated.NewStake.Value
        );

        return true;
    }

    internal static BakerStakeDecreased From(
        Concordium.Sdk.Types.AccountAddress sender,
        BakerStakeDecreasedEvent bakerStakeDecreasedEvent) =>
        new(
            bakerStakeDecreasedEvent.BakerId.Id.Index,
            AccountAddress.From(sender),
            bakerStakeDecreasedEvent.NewStake.Value
        );
}

public record BakerStakeIncreased(
    ulong BakerId,
    AccountAddress AccountAddress,
    ulong NewStakedAmount) : TransactionResultEvent
{
    internal static bool TryFrom(
        Concordium.Sdk.Types.AccountAddress sender,
        BakerStakeUpdatedData bakerStakeUpdated,
        out BakerStakeIncreased? stakeIncreased)
    {
        stakeIncreased = null;
        if (!bakerStakeUpdated.Increased)
        {
            return false;
        }

        stakeIncreased = new BakerStakeIncreased(
            bakerStakeUpdated.BakerId.Id.Index,
            AccountAddress.From(sender),
            bakerStakeUpdated.NewStake.Value
        );

        return true;
    }
    
    internal static BakerStakeIncreased From(
        Concordium.Sdk.Types.AccountAddress sender,
        BakerStakeIncreasedEvent bakerStakeDecreasedEvent) =>
        new(
            bakerStakeDecreasedEvent.BakerId.Id.Index,
            AccountAddress.From(sender),
            bakerStakeDecreasedEvent.NewStake.Value
        );
}

/// <summary>
/// A new smart contract instance was created.
/// </summary>
/// <param name="ModuleRef">Module with the source code of the contract.</param>
/// <param name="ContractAddress">The newly assigned address of the contract.</param>
/// <param name="Amount">The amount the instance was initialized with.</param>
/// <param name="InitName">The name of the contract.</param>
/// <param name="EventsAsHex">Any contract events that might have been generated by the contract initialization.</param>
/// <param name="Events">Parsed <see cref="EventsAsHex"/>.</param>
public record ContractInitialized(
    string ModuleRef,
    ContractAddress ContractAddress,
    ulong Amount,
    string InitName,
    ContractVersion? Version,
    [property: UsePaging(InferConnectionNameFromField = false)]
    string[] EventsAsHex,
    [property: UsePaging(InferConnectionNameFromField = false)]
    string[]? Events = null) : TransactionResultEvent
{
    internal static ContractInitialized From(Concordium.Sdk.Types.ContractInitialized contract) =>
        new(
            contract.Data.ModuleReference.ToString(),
            ContractAddress.From(contract.Data.ContractAddress),
            contract.Data.Amount.Value,
            contract.Data.InitName.Name,
            ContractVersionFactory.From(contract.Data.ContractVersion),
            contract.Data.Events.Select(d => d.ToHexString()).ToArray()
        );

    /// <summary>
    /// Try parse hexadecimal <see cref="EventsAsHex"/> from module schema.
    ///
    /// If succeed a new <see cref="ContractInitialized"/> is returned.
    ///
    /// If no module schema exist or the parsing fails null will be returned. In case of error the error will be logged.
    /// </summary>
    internal async Task<ContractInitialized?> TryUpdateWithParsedEvents(IModuleReadonlyRepository moduleReadonlyRepository)
    {
        var events = await ParseEvents(moduleReadonlyRepository);
        return events != null ? 
            new ContractInitialized(ModuleRef, ContractAddress, Amount, InitName, Version, EventsAsHex, events) : 
            null;
    }

    internal string GetName() => InitName[5..];
    
    private async Task<string[]?> ParseEvents(IModuleReadonlyRepository moduleReadonlyRepository)
    {
        if (EventsAsHex.Length == 0)
        { 
            return null;
        }
        var logger = Log.ForContext<ContractInitialized>();
        using var _ = LogContext.PushProperty("ContractAddress", ContractAddress.AsString);
        
        var moduleReferenceEvent = await moduleReadonlyRepository.GetModuleReferenceEventAsync(ModuleRef);
        if (moduleReferenceEvent.Schema == null)
        {
            return null;
        }
        var contractName = GetName(InitName);

        return GetParsedEvents(moduleReferenceEvent, contractName, EventsAsHex, logger, nameof(ContractInitialized));
    }
    
    private static string GetName(string initName) => initName[5..];
}

/// <summary>
/// A smart contract module was successfully deployed.
/// </summary>
/// <param name="ModuleRef"></param>
public record ContractModuleDeployed(
    string ModuleRef) : TransactionResultEvent
{
    internal static ContractModuleDeployed From(ModuleDeployed moduleDeployed) =>
        new(
            moduleDeployed.ModuleReference.ToString()
        );
}

/// <summary>
/// A smart contract instance was updated.
/// </summary>
/// <param name="ContractAddress">Address of the affected instance.</param>
/// <param name="Instigator">The origin of the message to the smart contract. This can be either an account or a smart contract.</param>
/// <param name="Amount">The amount the method was invoked with.</param>
/// <param name="MessageAsHex">The message passed to method.</param>
/// <param name="ReceiveName">The name of the method that was executed.</param>
/// <param name="EventsAsHex">Any contract events that might have been generated by the contract execution.</param>
/// <param name="Events">Parsed <see cref="EventsAsHex"/>.</param>
/// <param name="Message">Parsed <see cref="MessageAsHex"/>.</param>
public record ContractUpdated(
    ContractAddress ContractAddress,
    Address Instigator,
    ulong Amount,
    string MessageAsHex,
    string ReceiveName,
    ContractVersion? Version,
    [property: UsePaging(InferConnectionNameFromField = false)]
    string[] EventsAsHex,
    [property: UsePaging(InferConnectionNameFromField = false)]
    string[]? Events = null,
    string? Message = null) : TransactionResultEvent
{
    internal static ContractUpdated From(Updated updated) =>
        new(
            ContractAddress.From(updated.Address),
            Address.From(updated.Instigator),
            updated.Amount.Value,
            updated.Message.ToHexString(),
            updated.ReceiveName.Receive,
            ContractVersionFactory.From(updated.ContractVersion),
            updated.Events.Select(e => e.ToHexString()).ToArray()
        );
    
    /// <summary>
    /// Try parse hexadecimal <see cref="EventsAsHex"/> and <see cref="MessageAsHex"/> from module schema.
    ///
    /// If succeed a new <see cref="ContractUpdated"/> is returned.
    ///
    /// If no module schema exist or the parsing fails null will be returned. In case of error the error will be logged.
    /// </summary>
    internal async Task<ContractUpdated?> TryUpdate(
        IModuleReadonlyRepository moduleReadonlyRepository,
        ulong blockHeight,
        ulong transactionIndex,
        uint eventIndex
        )
    {
        if (EventsAsHex.Length == 0)
        { 
            return null;
        }
        var logger = Log.ForContext<ContractUpdated>();
        using var _ = LogContext.PushProperty("ContractAddress", ContractAddress.AsString);
        
        var moduleReferenceEvent = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(ContractAddress, blockHeight, transactionIndex, eventIndex);
        if (moduleReferenceEvent.Schema == null)
        {
            return null;
        }
        var contractName = ReceiveName[..ReceiveName.IndexOf('.')];
        var events = GetParsedEvents(moduleReferenceEvent, contractName, EventsAsHex, logger, nameof(ContractUpdated));
        
        var receiveName = new ReceiveName(ReceiveName);
        var message = receiveName.DeserializeMessage(
            MessageAsHex,
            moduleReferenceEvent.Schema,
            moduleReferenceEvent.SchemaVersion,
            logger,
            moduleReferenceEvent.ModuleReference,
            nameof(ContractUpdated)
        );
        return events != null || message != null ? 
            new ContractUpdated(ContractAddress, Instigator, Amount, MessageAsHex, ReceiveName, Version, EventsAsHex, events, message) : 
            null;
    }
}

/// <summary>
/// This event is created to have a reference from a invoking contract to the update event.
///
/// This event is not mapped from node events, but are used by <see cref="Application.Aggregates.Contract.ContractAggregate"/>
/// such that a <see cref="Application.Aggregates.Contract.Entities.ContractEvent"/> can be created linked to the invoking contract.
///
/// The Instigator Contract will be at <see cref="ContractUpdated.Instigator"/> and the called contract
/// at <see cref="ContractUpdated.ContractAddress"/>.
/// </summary>
/// <param name="ContractUpdated">The updated event generated on the contract invoked.</param>
public record ContractCall(ContractUpdated ContractUpdated) : TransactionResultEvent
{
    /// <summary>
    /// Try parse hexadecimal <see cref="ContractUpdated.EventsAsHex"/> and <see cref="ContractUpdated.MessageAsHex"/> from module schema.
    ///
    /// If succeed a new <see cref="ContractCall"/> is returned.
    ///
    /// If no module schema exist or the parsing fails null will be returned. In case of error the error will be logged.
    /// </summary>
    internal async Task<ContractCall?> TryUpdate(
        IModuleReadonlyRepository moduleReadonlyRepository,
        ulong blockHeight,
        ulong transactionIndex,
        uint eventIndex
    )
    {
        var contractUpdated = await ContractUpdated.TryUpdate(moduleReadonlyRepository, blockHeight, transactionIndex, eventIndex);
        return contractUpdated != null ? new ContractCall(contractUpdated) : null;
    }
}

public record CredentialDeployed(
    string RegId,
    AccountAddress AccountAddress) : TransactionResultEvent
{
    internal static CredentialDeployed From(AccountCreationDetails accountCreationDetails) =>
        new(accountCreationDetails.RegId.ToHexString(),
            AccountAddress.From(accountCreationDetails.Address));
}

/// <summary>
/// Keys of the given credential were updated.
/// </summary>
/// <param name="CredId">ID of the credential whose keys were updated.</param>
public record CredentialKeysUpdated(
    string CredId) : TransactionResultEvent
{
    internal static CredentialKeysUpdated From(Concordium.Sdk.Types.CredentialKeysUpdated keys) =>
        new(
            keys.CredId.ToHexString()
        );
}

/// <summary>
/// The credentials of the account were updated.
/// </summary>
/// <param name="AccountAddress">The affected account.</param>
/// <param name="NewCredIds">The credential ids that were added.</param>
/// <param name="RemovedCredIds">The credential ids that were removed.</param>
/// <param name="NewThreshold">The (possibly) updated account threshold.</param>
public record CredentialsUpdated(
    AccountAddress AccountAddress,
    string[] NewCredIds,
    string[] RemovedCredIds,
    byte NewThreshold) : TransactionResultEvent
{
    internal static CredentialsUpdated From(
        Concordium.Sdk.Types.AccountAddress sender,
        Concordium.Sdk.Types.CredentialsUpdated updated) =>
        new(
            AccountAddress.From(sender),
            updated.NewCredIds.Select(c => c.ToHexString()).ToArray(),
            updated.RemovedCredIds.Select(c => c.ToHexString()).ToArray(),
            (byte)updated.NewThreshold.Threshold
        );
}

/// <summary>
/// Data was registered on the chain.
/// </summary>
/// <param name="DataAsHex">The data that was registered.</param>
public record DataRegistered(string DataAsHex) : TransactionResultEvent
{
    public DecodedText GetDecoded()
    {
        return DecodedText.CreateFromHex(DataAsHex);
    }

    internal static DataRegistered From(Concordium.Sdk.Types.DataRegistered registered) => new(registered.ToHexString());
}

/// <summary>
/// Event generated when one or more encrypted amounts are consumed from the account
/// </summary>
/// <param name="AccountAddress">The affected account</param>
/// <param name="NewEncryptedAmount">The new self encrypted amount on the affected account</param>
/// <param name="InputAmount">The input encrypted amount that was removed</param>
/// <param name="UpToIndex">The index indicating which amounts were used</param>
public record EncryptedAmountsRemoved(
    AccountAddress AccountAddress,
    string NewEncryptedAmount,
    string InputAmount,
    ulong UpToIndex) : TransactionResultEvent
{
    internal static EncryptedAmountsRemoved From(EncryptedAmountRemovedEvent transfer) =>
        new(
            AccountAddress.From(transfer.Account),
            Convert.ToHexString(transfer.NewAmount).ToLowerInvariant(),
            Convert.ToHexString(transfer.InputAmount).ToLowerInvariant(),
            transfer.UpToIndex
        );
}

/// <summary>
/// The encrypted balance of the account was updated due to transfer from
/// public to encrypted balance of the account.
/// </summary>
/// <param name="AccountAddress">The affected account</param>
/// <param name="NewEncryptedAmount">The new self encrypted amount of the account</param>
/// <param name="Amount">The amount that was transferred from public to encrypted balance</param>
public record EncryptedSelfAmountAdded(
    AccountAddress AccountAddress,
    string NewEncryptedAmount,
    ulong Amount) : TransactionResultEvent
{
    internal static EncryptedSelfAmountAdded From(TransferredToEncrypted transfer) =>
        new(
            AccountAddress.From(transfer.Data.Account),
            Convert.ToHexString(transfer.Data.NewAmount).ToLowerInvariant(),
            transfer.Data.Amount.Value
        );
}

/// <summary>
/// A new encrypted amount was added to the account.
/// </summary>
/// <param name="AccountAddress">The account onto which the amount was added.</param>
/// <param name="NewIndex">The index the amount was assigned.</param>
/// <param name="EncryptedAmount">The encrypted amount that was added.</param>
public record NewEncryptedAmount(
    AccountAddress AccountAddress,
    ulong NewIndex,
    string EncryptedAmount) : TransactionResultEvent
{
    internal static NewEncryptedAmount From(NewEncryptedAmountEvent transfer) =>
        new(
            AccountAddress.From(transfer.Receiver),
            transfer.NewIndex,
            Convert.ToHexString(transfer.EncryptedAmount).ToLowerInvariant()
        );
}

public record TransferMemo(string RawHex) : TransactionResultEvent
{
    public DecodedText GetDecoded()
    {
        return DecodedText.CreateFromHex(RawHex);
    }

    internal static TransferMemo From(OnChainData data) => new(data.ToString());
}

/// <summary>
/// A transfer with schedule was enqueued.
/// </summary>
/// <param name="FromAccountAddress">Sender account address.</param>
/// <param name="ToAccountAddress">Receiver account address.</param>
/// <param name="AmountsSchedule">The list of releases. Ordered by increasing timestamp.</param>
public record TransferredWithSchedule(
    AccountAddress FromAccountAddress,
    AccountAddress ToAccountAddress,
    [property: UsePaging] TimestampedAmount[] AmountsSchedule) : TransactionResultEvent
{
    public ulong TotalAmount => AmountsSchedule.Aggregate(0UL, (val, item) => val + item.Amount);

    internal static TransferredWithSchedule From(
        Concordium.Sdk.Types.AccountAddress sender,
        Concordium.Sdk.Types.TransferredWithSchedule transferredWithSchedule) =>
        new(
            AccountAddress.From(sender),
            AccountAddress.From(transferredWithSchedule.To), 
            TimestampedAmount.From(transferredWithSchedule.Amount).ToArray()
        );
}

/// <summary>
/// A chain update was enqueued for the given time.
/// </summary>
public record ChainUpdateEnqueued(
    DateTimeOffset EffectiveTime,
    bool EffectiveImmediately,
    ChainUpdatePayload Payload) : TransactionResultEvent
{
    internal static bool TryFrom(
        UpdateDetails updateDetails,
        DateTimeOffset blockSlotTime,
        out ChainUpdateEnqueued? chainUpdateEnqueued)
    {
        if (!ChainUpdatePayload.TryFrom(updateDetails.Payload, out var chainUpdatePayload))
        {
            chainUpdateEnqueued = null;
            return false;
        } 
        var isEffectiveImmediately = updateDetails.EffectiveTime.ToUnixTimeSeconds() == 0;
        var effectiveTime = isEffectiveImmediately ? blockSlotTime : updateDetails.EffectiveTime;
        chainUpdateEnqueued = new ChainUpdateEnqueued(effectiveTime, isEffectiveImmediately, chainUpdatePayload!);
        return true;
    }
}

public record ContractInterrupted(
    ContractAddress ContractAddress,
    [property: UsePaging(InferConnectionNameFromField = false)]
    string[] EventsAsHex,
    [property: UsePaging(InferConnectionNameFromField = false)]
    string[]? Events = null) : TransactionResultEvent
{
    internal static ContractInterrupted From(Interrupted interrupted) =>
        new(
            ContractAddress.From(interrupted.Address),
            interrupted.Events.Select(e => e.ToHexString()).ToArray()
        );

    /// <summary>
    /// Try parse hexadecimal <see cref="EventsAsHex"/> from module schema.
    ///
    /// If succeed a new <see cref="ContractInterrupted"/> is returned.
    ///
    /// If no module schema exist or the parsing fails null will be returned. In case of error the error will be logged.
    /// </summary>
    internal async Task<ContractInterrupted?> TryUpdateWithParsedEvents(
        IContractRepository contractRepository,
        IModuleReadonlyRepository moduleReadonlyRepository,
        ulong blockHeight,
        ulong transactionIndex,
        uint eventIndex)
    {
        var events = await ParseEvents(contractRepository, moduleReadonlyRepository, blockHeight, transactionIndex, eventIndex);
        return events != null ? 
            new ContractInterrupted(ContractAddress, EventsAsHex, events) : 
            null;
    }
    
    private async Task<string[]?> ParseEvents(
        IContractRepository contractRepository,
        IModuleReadonlyRepository moduleReadonlyRepository,
        ulong blockHeight,
        ulong transactionIndex,
        uint eventIndex)
    {
        if (EventsAsHex.Length == 0)
        { 
            return null;
        }
        var logger = Log.ForContext<ContractInterrupted>();
        using var _ = LogContext.PushProperty("ContractAddress", ContractAddress.AsString);
        
        var moduleReferenceEvent = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(ContractAddress, blockHeight, transactionIndex, eventIndex);
        if (moduleReferenceEvent.Schema == null)
        {
            return null;
        }
        var initialized = await contractRepository.GetReadonlyContractInitializedEventAsync(ContractAddress);
        var contractName = initialized.GetName();
        return GetParsedEvents(moduleReferenceEvent, contractName, EventsAsHex, logger, nameof(ContractInterrupted));
    }
}

public record ContractResumed(
    ContractAddress ContractAddress,
    bool Success) : TransactionResultEvent
{
    internal static ContractResumed From(Resumed resumed) =>
        new(
            ContractAddress.From(resumed.Address),
            resumed.Success
        );
}

public record ContractUpgraded(
    ContractAddress ContractAddress,
    string From,
    string To) : TransactionResultEvent
{
    internal static ContractUpgraded From_(Upgraded upgraded) =>
        new(
            ContractAddress.From(upgraded.Address),
            upgraded.From.ToString(),
            upgraded.To.ToString()
        );
}

public record BakerSetOpenStatus(
    ulong BakerId,
    AccountAddress AccountAddress,
    BakerPoolOpenStatus OpenStatus) : TransactionResultEvent
{
    internal static BakerSetOpenStatus From(
        Concordium.Sdk.Types.AccountAddress accountAddress,
        BakerSetOpenStatusEvent bakerSetOpenStatusEvent) =>
        new(
            bakerSetOpenStatusEvent.BakerId.Id.Index,
            AccountAddress.From(accountAddress),
            bakerSetOpenStatusEvent.OpenStatus.MapToGraphQlEnum()
        );
}

public record BakerSetMetadataURL(
    ulong BakerId,
    AccountAddress AccountAddress,
    string MetadataUrl) : TransactionResultEvent
{
    internal static BakerSetMetadataURL From(
        Concordium.Sdk.Types.AccountAddress sender,
        BakerSetMetadataUrlEvent bakerSetMetadataUrlEvent)
    {
        return new(
            bakerSetMetadataUrlEvent.BakerId.Id.Index,
            AccountAddress.From(sender),
            bakerSetMetadataUrlEvent.MetadataUrl
        );
    }
}

public record BakerSetTransactionFeeCommission(
    ulong BakerId,
    AccountAddress AccountAddress,
    decimal TransactionFeeCommission) : TransactionResultEvent
{
    internal static BakerSetTransactionFeeCommission From(
        Concordium.Sdk.Types.AccountAddress sender,
        BakerSetTransactionFeeCommissionEvent commissionEvent) =>
        new(
            commissionEvent.BakerId.Id.Index,
            AccountAddress.From(sender),
            commissionEvent.TransactionFeeCommission.AsDecimal()
        );
}

public record BakerSetBakingRewardCommission(
    ulong BakerId,
    AccountAddress AccountAddress,
    decimal BakingRewardCommission) : TransactionResultEvent
{
    internal static BakerSetBakingRewardCommission From(
        Concordium.Sdk.Types.AccountAddress sender,
        BakerSetBakingRewardCommissionEvent commissionEvent) =>
        new(
            commissionEvent.BakerId.Id.Index,
            AccountAddress.From(sender),
            commissionEvent.BakingRewardCommission.AsDecimal()
        );
}

public record BakerSetFinalizationRewardCommission(
    ulong BakerId,
    AccountAddress AccountAddress,
    decimal FinalizationRewardCommission) : TransactionResultEvent
{
    internal static BakerSetFinalizationRewardCommission From(
        Concordium.Sdk.Types.AccountAddress sender,
        BakerSetFinalizationRewardCommissionEvent commissionEvent) =>
        new(
            commissionEvent.BakerId.Id.Index,
            AccountAddress.From(sender),
            commissionEvent.FinalizationRewardCommission.AsDecimal()
        );
}

public record DelegationAdded(
    ulong DelegatorId,
    AccountAddress AccountAddress) : TransactionResultEvent
{
    internal static DelegationAdded From(
        Concordium.Sdk.Types.AccountAddress sender,
        Concordium.Sdk.Types.DelegationAdded delegationAdded) =>
        new(
            delegationAdded.DelegatorId.Id.Index,
            AccountAddress.From(sender));
}

public record DelegationRemoved(
    ulong DelegatorId,
    AccountAddress AccountAddress) : TransactionResultEvent
{
    internal static DelegationRemoved From(
        Concordium.Sdk.Types.AccountAddress sender,
        Concordium.Sdk.Types.DelegationRemoved delegationRemoved) =>
        new(
            delegationRemoved.DelegatorId.Id.Index,
            AccountAddress.From(sender));
}

public record DelegationStakeIncreased(
    ulong DelegatorId,
    AccountAddress AccountAddress,
    ulong NewStakedAmount) : TransactionResultEvent
{
    internal static DelegationStakeIncreased From(
        Concordium.Sdk.Types.AccountAddress sender,
        Concordium.Sdk.Types.DelegationStakeIncreased delegationStakeIncreased) =>
        new(
            delegationStakeIncreased.DelegatorId.Id.Index,
            AccountAddress.From(sender),
            delegationStakeIncreased.NewStake.Value);
}

public record DelegationStakeDecreased(
    ulong DelegatorId,
    AccountAddress AccountAddress,
    ulong NewStakedAmount) : TransactionResultEvent
{
    internal static DelegationStakeDecreased From(
        Concordium.Sdk.Types.AccountAddress sender,
        Concordium.Sdk.Types.DelegationStakeDecreased delegationStakeDecreased) =>
        new(
            delegationStakeDecreased.DelegatorId.Id.Index,
            AccountAddress.From(sender),
            delegationStakeDecreased.NewStake.Value);
}

public record DelegationSetRestakeEarnings(
    ulong DelegatorId,
    AccountAddress AccountAddress,
    bool RestakeEarnings) : TransactionResultEvent
{
    internal static DelegationSetRestakeEarnings From(
        Concordium.Sdk.Types.AccountAddress sender,
        Concordium.Sdk.Types.DelegationSetRestakeEarnings delegationSetRestake) =>
        new(
            delegationSetRestake.DelegatorId.Id.Index,
            AccountAddress.From(sender),
            delegationSetRestake.RestakeEarnings);
}

public record DelegationSetDelegationTarget(
    ulong DelegatorId,
    AccountAddress AccountAddress,
    DelegationTarget DelegationTarget) : TransactionResultEvent
{
    internal static DelegationSetDelegationTarget From(
        Concordium.Sdk.Types.AccountAddress sender,
        Concordium.Sdk.Types.DelegationSetDelegationTarget delegationSetDelegation) =>
        new(
            delegationSetDelegation.DelegatorId.Id.Index,
            AccountAddress.From(sender),
            DelegationTarget.From(delegationSetDelegation.DelegationTarget));
}
