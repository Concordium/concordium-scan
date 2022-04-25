using Application.Api.GraphQL.Accounts;
using HotChocolate;
using HotChocolate.Types;

namespace Application.Api.GraphQL.Transactions;

[UnionType]
public abstract record TransactionRejectReason
{
    [GraphQLDeprecated("Don't use! This field is only in the schema to make sure reject reasons without any fields are valid types in GraphQL (which does not allow types without any fields)")]
    public bool Get_() // Will translate to a boolean field named _ in the GraphQL schema.
    {
        return false;
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
    AccountAddress AccountAddress) : TransactionRejectReason
{
    [GraphQLDeprecated("Use 'accountAddress.asString' instead. This field will be removed in the near future.")]
    public string AccountAddressString => AccountAddress.AsString;
}

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
    string MessageAsHex) : TransactionRejectReason;

/// <summary>
/// Reward account desired by the baker does not exist.
/// </summary>
public record NonExistentRewardAccount(
    AccountAddress AccountAddress) : TransactionRejectReason
{
    [GraphQLDeprecated("Use 'accountAddress.asString' instead. This field will be removed in the near future.")]
    public string AccountAddressString => AccountAddress.AsString;
}

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
    AccountAddress AccountAddress) : TransactionRejectReason
{
    [GraphQLDeprecated("Use 'accountAddress.asString' instead. This field will be removed in the near future.")]
    public string AccountAddressString => AccountAddress.AsString;
}

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
    AccountAddress AccountAddress) : TransactionRejectReason
{
    [GraphQLDeprecated("Use 'accountAddress.asString' instead. This field will be removed in the near future.")]
    public string AccountAddressString => AccountAddress.AsString;
}

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
    AccountAddress AccountAddress) : TransactionRejectReason
{
    [GraphQLDeprecated("Use 'accountAddress.asString' instead. This field will be removed in the near future.")]
    public string AccountAddressString => AccountAddress.AsString;
}

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
