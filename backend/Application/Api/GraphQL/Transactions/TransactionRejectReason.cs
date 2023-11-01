using System.IO;
using System.Threading.Tasks;
using Application.Aggregates.Contract;
using Application.Aggregates.Contract.Types;
using Application.Exceptions;
using Application.Interop;
using Concordium.Sdk.Types;
using HotChocolate;
using HotChocolate.Types;
using Serilog.Context;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;
using ReceiveName = Application.Types.ReceiveName;

namespace Application.Api.GraphQL.Transactions;

[UnionType]
public abstract record TransactionRejectReason
{
    [GraphQLDeprecated(
        "Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields)")]
    public bool Get_() // Will translate to a boolean field named _ in the GraphQL schema.
    {
        return false;
    }

    internal static TransactionRejectReason MapRejectReason(IRejectReason rejectReason)
    {
        return rejectReason switch
        {
            Concordium.Sdk.Types.ModuleNotWf => new ModuleNotWf(),
            Concordium.Sdk.Types.ModuleHashAlreadyExists x => new ModuleHashAlreadyExists(x.ModuleReference.ToString()),
            Concordium.Sdk.Types.InvalidAccountReference x => new InvalidAccountReference(
                AccountAddress.From(x.AccountAddress)),
            Concordium.Sdk.Types.InvalidInitMethod x => new InvalidInitMethod(x.ModuleReference.ToString(),
                x.ContractName.Name),
            Concordium.Sdk.Types.InvalidReceiveMethod x => new InvalidReceiveMethod(x.ModuleReference.ToString(),
                x.ReceiveName.Receive),
            Concordium.Sdk.Types.InvalidModuleReference x => new InvalidModuleReference(x.ModuleReference.ToString()),
            Concordium.Sdk.Types.InvalidContractAddress x => new InvalidContractAddress(
                ContractAddress.From(x.ContractAddress)),
            Concordium.Sdk.Types.RuntimeFailure => new RuntimeFailure(),
            Concordium.Sdk.Types.AmountTooLarge x => new AmountTooLarge(Address.From(x.Address), x.Amount.Value),
            Concordium.Sdk.Types.SerializationFailure => new SerializationFailure(),
            Concordium.Sdk.Types.OutOfEnergy => new OutOfEnergy(),
            Concordium.Sdk.Types.RejectedInit x => new RejectedInit(x.RejectReason),
            Concordium.Sdk.Types.RejectedReceive x => new RejectedReceive(x.RejectReason,
                ContractAddress.From(x.ContractAddress), x.ReceiveName.Receive, x.Parameter.ToHexString()),
            Concordium.Sdk.Types.InvalidProof => new InvalidProof(),
            Concordium.Sdk.Types.AlreadyABaker x => new AlreadyABaker(x.BakerId.Id.Index),
            Concordium.Sdk.Types.NotABaker x => new NotABaker(AccountAddress.From(x.AccountAddress)),
            Concordium.Sdk.Types.InsufficientBalanceForBakerStake => new InsufficientBalanceForBakerStake(),
            Concordium.Sdk.Types.StakeUnderMinimumThresholdForBaking => new StakeUnderMinimumThresholdForBaking(),
            Concordium.Sdk.Types.BakerInCooldown => new BakerInCooldown(),
            Concordium.Sdk.Types.DuplicateAggregationKey x => new DuplicateAggregationKey(Convert.ToHexString(x.Key).ToLowerInvariant()),
            Concordium.Sdk.Types.NonExistentCredentialId => new NonExistentCredentialId(),
            Concordium.Sdk.Types.KeyIndexAlreadyInUse => new KeyIndexAlreadyInUse(),
            Concordium.Sdk.Types.InvalidAccountThreshold => new InvalidAccountThreshold(),
            Concordium.Sdk.Types.InvalidCredentialKeySignThreshold => new InvalidCredentialKeySignThreshold(),
            Concordium.Sdk.Types.InvalidEncryptedAmountTransferProof => new InvalidEncryptedAmountTransferProof(),
            Concordium.Sdk.Types.InvalidTransferToPublicProof => new InvalidTransferToPublicProof(),
            Concordium.Sdk.Types.EncryptedAmountSelfTransfer x => new EncryptedAmountSelfTransfer(AccountAddress.From(x.AccountAddress)),
            Concordium.Sdk.Types.InvalidIndexOnEncryptedTransfer => new InvalidIndexOnEncryptedTransfer(),
            Concordium.Sdk.Types.ZeroScheduledAmount => new ZeroScheduledAmount(),
            Concordium.Sdk.Types.NonIncreasingSchedule => new NonIncreasingSchedule(),
            Concordium.Sdk.Types.FirstScheduledReleaseExpired => new FirstScheduledReleaseExpired(),
            Concordium.Sdk.Types.ScheduledSelfTransfer x => new ScheduledSelfTransfer(AccountAddress.From(x.AccountAddress)),
            Concordium.Sdk.Types.InvalidCredentials => new InvalidCredentials(),
            Concordium.Sdk.Types.DuplicateCredIds x => new DuplicateCredIds(x.ToHexStrings().ToArray()),
            Concordium.Sdk.Types.NonExistentCredIds x => new NonExistentCredIds(x.ToHexStrings().ToArray()),
            Concordium.Sdk.Types.RemoveFirstCredential => new RemoveFirstCredential(),
            Concordium.Sdk.Types.CredentialHolderDidNotSign => new CredentialHolderDidNotSign(),
            Concordium.Sdk.Types.NotAllowedMultipleCredentials => new NotAllowedMultipleCredentials(),
            Concordium.Sdk.Types.NotAllowedToReceiveEncrypted => new NotAllowedToReceiveEncrypted(),
            Concordium.Sdk.Types.NotAllowedToHandleEncrypted => new NotAllowedToHandleEncrypted(),
            Concordium.Sdk.Types.MissingBakerAddParameters => new MissingBakerAddParameters(),
            Concordium.Sdk.Types.FinalizationRewardCommissionNotInRange => new FinalizationRewardCommissionNotInRange(),
            Concordium.Sdk.Types.BakingRewardCommissionNotInRange => new BakingRewardCommissionNotInRange(),
            Concordium.Sdk.Types.TransactionFeeCommissionNotInRange => new TransactionFeeCommissionNotInRange(),
            Concordium.Sdk.Types.AlreadyADelegator => new AlreadyADelegator(),
            Concordium.Sdk.Types.InsufficientBalanceForDelegationStake => new InsufficientBalanceForDelegationStake(),
            Concordium.Sdk.Types.MissingDelegationAddParameters => new MissingDelegationAddParameters(),
            Concordium.Sdk.Types.InsufficientDelegationStake => new InsufficientDelegationStake(),
            Concordium.Sdk.Types.DelegatorInCooldown => new DelegatorInCooldown(),
            Concordium.Sdk.Types.NotADelegator x => new NotADelegator(AccountAddress.From(x.AccountAddress)),
            Concordium.Sdk.Types.DelegationTargetNotABaker x => new DelegationTargetNotABaker(x.BakerId.Id.Index),
            Concordium.Sdk.Types.StakeOverMaximumThresholdForPool => new StakeOverMaximumThresholdForPool(),
            Concordium.Sdk.Types.PoolWouldBecomeOverDelegated => new PoolWouldBecomeOverDelegated(),
            Concordium.Sdk.Types.PoolClosed => new PoolClosed(),
            _ => throw new NotImplementedException("Reject reason not mapped!")
        };
    }
}

/// <summary>
/// Error raised when validating the Wasm module.
/// </summary>
public record ModuleNotWf : TransactionRejectReason;

/// <summary>
/// Module hash already exists.
/// </summary>
public record ModuleHashAlreadyExists(
    string ModuleRef) : TransactionRejectReason;

/// <summary>
/// Account does not exist.
/// </summary>
public record InvalidAccountReference(
    AccountAddress AccountAddress) : TransactionRejectReason;

/// <summary>
/// Reference to a non-existing contract init method.
/// </summary>
public record InvalidInitMethod(
    string ModuleRef,
    string InitName) : TransactionRejectReason;

/// <summary>
/// Reference to a non-existing contract receive method.
/// </summary>
public record InvalidReceiveMethod(
    string ModuleRef,
    string ReceiveName) : TransactionRejectReason;

/// <summary>
/// Reference to a non-existing module.
/// </summary>
public record InvalidModuleReference(
    string ModuleRef) : TransactionRejectReason;

/// <summary>
/// Contract instance does not exist.
/// </summary>
public record InvalidContractAddress(
    ContractAddress ContractAddress) : TransactionRejectReason;

/// <summary>
/// Runtime exception occurred when running either the init or receive method.
/// </summary>
public record RuntimeFailure : TransactionRejectReason;

/// <summary>
/// When one wishes to transfer an amount from A to B but there
/// are not enough funds on account/contract A to make this
/// possible.
/// </summary>
public record AmountTooLarge(
    Address Address,
    ulong Amount) : TransactionRejectReason;

/// <summary>
/// Serialization of the body failed.
/// </summary>
public record SerializationFailure : TransactionRejectReason;

/// <summary>
/// We ran of out energy to process this transaction.
/// </summary>
public record OutOfEnergy : TransactionRejectReason;

/// <summary>
/// Rejected due to contract logic in init function of a contract.
/// </summary>
public record RejectedInit(
    int RejectReason) : TransactionRejectReason;

public record RejectedReceive(
    int RejectReason,
    ContractAddress ContractAddress,
    string ReceiveName,
    string MessageAsHex,
    string? Message = null) : TransactionRejectReason {
    
    /// <summary>
    /// Try parse hexadecimal <see cref="MessageAsHex"/> from module schema.
    ///
    /// If succeed a new <see cref="RejectedReceive"/> is returned.
    ///
    /// If no module schema exist or the parsing fails null will be returned. In case of error the error will be logged.
    /// </summary>
    internal async Task<RejectedReceive?> TryUpdateMessage(
        IModuleReadonlyRepository moduleReadonlyRepository,
        ulong blockHeight,
        ulong transactionIndex
    )
    {
        if (IsParseError())
        {
            return null;
        }
        var logger = Log.ForContext<RejectedReceive>();
        using var _ = LogContext.PushProperty("ContractAddress", ContractAddress.AsString);
        
        var moduleReferenceEvent = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(ContractAddress, blockHeight, transactionIndex, 0);
        if (moduleReferenceEvent.Schema == null)
        {
            return null;
        }

        var receiveName = new ReceiveName(ReceiveName);
        var message = receiveName.DeserializeMessage(
            MessageAsHex,
            moduleReferenceEvent.Schema,
            moduleReferenceEvent.SchemaVersion,
            logger,
            moduleReferenceEvent.ModuleReference
        );
        return message != null ? 
            new RejectedReceive(RejectReason, ContractAddress, ReceiveName, MessageAsHex, message) : 
            null;
    }
    
    /// <summary>
    /// Don't deserialize the message if the error is exactly an error related to parsing.
    /// </summary>
    /// <remarks>
    /// <see href="https://github.com/Concordium/concordium-rust-smart-contracts/blob/673d09236b40e4583e60b8aa2cd7b6849b1c6189/concordium-std/src/lib.rs#L202">Common error cases</see>
    /// </remarks>
    private bool IsParseError()
    {
        return RejectReason == -2_147_483_646;
    }
}

/// <summary>
/// Reward account desired by the baker does not exist.
/// </summary>
[Obsolete("Not present in gRPC v2")]
public record NonExistentRewardAccount(
    AccountAddress AccountAddress) : TransactionRejectReason;

/// <summary>
/// Proof that the baker owns relevant private keys is not valid.
/// </summary>
public record InvalidProof : TransactionRejectReason;

/// <summary>
/// Tried to add baker for an account that already has a baker.
/// </summary>
public record AlreadyABaker(
    ulong BakerId) : TransactionRejectReason;

/// <summary>
/// Tried to remove a baker for an account that has no baker.
/// </summary>
public record NotABaker(
    AccountAddress AccountAddress) : TransactionRejectReason;

/// <summary>
/// The amount on the account was insufficient to cover the proposed stake.
/// </summary>
public record InsufficientBalanceForBakerStake : TransactionRejectReason;

/// <summary>
/// The amount provided is under the threshold required for becoming a baker.
/// </summary>
public record StakeUnderMinimumThresholdForBaking : TransactionRejectReason;

/// <summary>
/// The change could not be made because the baker is in cooldown for another change.
/// </summary>
public record BakerInCooldown : TransactionRejectReason;

/// <summary>
/// A baker with the given aggregation key already exists.
/// </summary>
public record DuplicateAggregationKey(
    string AggregationKey) : TransactionRejectReason;

/// <summary>
/// Encountered credential ID that does not exist.
/// </summary>
public record NonExistentCredentialId : TransactionRejectReason;

/// <summary>
/// Attempted to add an account key to a key index already in use.
/// </summary>
public record KeyIndexAlreadyInUse : TransactionRejectReason;

/// <summary>
/// When the account threshold is updated, it must not exceed the amount of existing keys.
/// </summary>
public record InvalidAccountThreshold : TransactionRejectReason;

/// <summary>
/// When the credential key threshold is updated, it must not exceed the amount of existing keys.
/// </summary>
public record InvalidCredentialKeySignThreshold : TransactionRejectReason;

/// <summary>
/// Proof for an encrypted amount transfer did not validate.
/// </summary>
public record InvalidEncryptedAmountTransferProof : TransactionRejectReason;

/// <summary>
/// Proof for a secret to public transfer did not validate.
/// </summary>
public record InvalidTransferToPublicProof : TransactionRejectReason;

/// <summary>
/// Account tried to transfer an encrypted amount to itself, that's not allowed.
/// </summary>
public record EncryptedAmountSelfTransfer(
    AccountAddress AccountAddress) : TransactionRejectReason;

/// <summary>
/// The provided index is below the start index or above `startIndex + length incomingAmounts`
/// </summary>
public record InvalidIndexOnEncryptedTransfer : TransactionRejectReason;

/// <summary>
/// The transfer with schedule is going to send 0 tokens.
/// </summary>
public record ZeroScheduledAmount : TransactionRejectReason;

/// <summary>
/// The transfer with schedule has a non strictly increasing schedule.
/// </summary>
public record NonIncreasingSchedule : TransactionRejectReason;

/// <summary>
/// The first scheduled release in a transfer with schedule has already expired.
/// </summary>
public record FirstScheduledReleaseExpired : TransactionRejectReason;

/// <summary>
/// Account tried to transfer with schedule to itself, that's not allowed.
/// </summary>
public record ScheduledSelfTransfer(
    AccountAddress AccountAddress) : TransactionRejectReason;

/// <summary>
/// At least one of the credentials was either malformed or its proof was incorrect.
/// </summary>
public record InvalidCredentials : TransactionRejectReason;

/// <summary>
/// Some of the credential IDs already exist or are duplicated in the transaction.
/// </summary>
public record DuplicateCredIds(
    string[] CredIds) : TransactionRejectReason;

/// <summary>
/// A credential id that was to be removed is not part of the account. 
/// </summary>
public record NonExistentCredIds(
    string[] CredIds) : TransactionRejectReason;

/// <summary>
/// Attempt to remove the first credential.
/// </summary>
public record RemoveFirstCredential : TransactionRejectReason;

/// <summary>
/// The credential holder of the keys to be updated did not sign the transaction.
/// </summary>
public record CredentialHolderDidNotSign : TransactionRejectReason;

/// <summary>
/// Account is not allowed to have multiple credentials because it contains a non-zero encrypted transfer.
/// </summary>
public record NotAllowedMultipleCredentials : TransactionRejectReason;

/// <summary>
/// The account is not allowed to receive encrypted transfers because it has multiple credentials.
/// </summary>
public record NotAllowedToReceiveEncrypted : TransactionRejectReason;

/// <summary>
/// The account is not allowed to send encrypted transfers (or transfer from/to public to/from encrypted).
/// </summary>
public record NotAllowedToHandleEncrypted : TransactionRejectReason;

/// <summary>
/// A configure baker transaction is missing one or more arguments in order to add a baker.
/// </summary>
public record MissingBakerAddParameters : TransactionRejectReason;

/// <summary>
/// Finalization reward commission is not in the valid range for a baker
/// </summary>
public record FinalizationRewardCommissionNotInRange : TransactionRejectReason;

/// <summary>
/// Baking reward commission is not in the valid range for a baker
/// </summary>
public record BakingRewardCommissionNotInRange : TransactionRejectReason;

/// <summary>
/// Transaction fee commission is not in the valid range for a baker
/// </summary>
public record TransactionFeeCommissionNotInRange : TransactionRejectReason;

/// <summary>
/// Tried to add baker for an account that already has a delegator
/// </summary>
public record AlreadyADelegator : TransactionRejectReason;

/// <summary>
/// The amount on the account was insufficient to cover the proposed stake
/// </summary>
public record InsufficientBalanceForDelegationStake : TransactionRejectReason;

/// <summary>
/// A configure delegation transaction is missing one or more arguments in order to add a delegator.
/// </summary>
public record MissingDelegationAddParameters : TransactionRejectReason;

/// <summary>
/// The delegation stake when adding a baker was 0.
/// </summary>
public record InsufficientDelegationStake : TransactionRejectReason;

/// <summary>
/// The change could not be made because the delegator is in cooldown
/// </summary>
public record DelegatorInCooldown : TransactionRejectReason;

/// <summary>
/// Account is not a delegation account
/// </summary>
public record NotADelegator(
    AccountAddress AccountAddress) : TransactionRejectReason;

/// <summary>
/// Delegation target is not a baker
/// </summary>
public record DelegationTargetNotABaker(
    ulong BakerId) : TransactionRejectReason;

/// <summary>
/// The amount would result in pool capital higher than the maximum threshold
/// </summary>
public record StakeOverMaximumThresholdForPool : TransactionRejectReason;

/// <summary>
/// The amount would result in pool with a too high fraction of delegated capital.
/// </summary>
public record PoolWouldBecomeOverDelegated : TransactionRejectReason;

/// <summary>
/// The pool is not open to delegators.
/// </summary>
public record PoolClosed : TransactionRejectReason;
