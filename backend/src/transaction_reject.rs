use crate::{
    address::{AccountAddress, Address, ContractAddress},
    scalar_types::{Amount, BakerId},
    transaction_event::protocol_level_tokens::TokenModuleRejectReasonTypes,
};
use anyhow::Context;
use async_graphql::{Enum, SimpleObject, Union};
use concordium_rust_sdk::{
    base::{
        contracts_common::schema::{VersionedModuleSchema, VersionedSchemaError},
        smart_contracts::ReceiveName,
    },
    protocol_level_tokens::TokenModuleRejectReason,
};

#[derive(Union, Clone, serde::Serialize, serde::Deserialize)]
pub enum TransactionRejectReason {
    ModuleNotWf(ModuleNotWf),
    ModuleHashAlreadyExists(ModuleHashAlreadyExists),
    InvalidAccountReference(InvalidAccountReference),
    InvalidInitMethod(InvalidInitMethod),
    InvalidReceiveMethod(InvalidReceiveMethod),
    InvalidModuleReference(InvalidModuleReference),
    InvalidContractAddress(InvalidContractAddress),
    RuntimeFailure(RuntimeFailure),
    AmountTooLarge(AmountTooLarge),
    SerializationFailure(SerializationFailure),
    OutOfEnergy(OutOfEnergy),
    RejectedInit(RejectedInit),
    RejectedReceive(RejectedReceive),
    NonExistentRewardAccount(NonExistentRewardAccount),
    InvalidProof(InvalidProof),
    AlreadyABaker(AlreadyABaker),
    NotABaker(NotABaker),
    InsufficientBalanceForBakerStake(InsufficientBalanceForBakerStake),
    StakeUnderMinimumThresholdForBaking(StakeUnderMinimumThresholdForBaking),
    BakerInCooldown(BakerInCooldown),
    DuplicateAggregationKey(DuplicateAggregationKey),
    NonExistentCredentialId(NonExistentCredentialId),
    KeyIndexAlreadyInUse(KeyIndexAlreadyInUse),
    InvalidAccountThreshold(InvalidAccountThreshold),
    InvalidCredentialKeySignThreshold(InvalidCredentialKeySignThreshold),
    InvalidEncryptedAmountTransferProof(InvalidEncryptedAmountTransferProof),
    InvalidTransferToPublicProof(InvalidTransferToPublicProof),
    EncryptedAmountSelfTransfer(EncryptedAmountSelfTransfer),
    InvalidIndexOnEncryptedTransfer(InvalidIndexOnEncryptedTransfer),
    ZeroScheduledAmount(ZeroScheduledAmount),
    NonIncreasingSchedule(NonIncreasingSchedule),
    FirstScheduledReleaseExpired(FirstScheduledReleaseExpired),
    ScheduledSelfTransfer(ScheduledSelfTransfer),
    InvalidCredentials(InvalidCredentials),
    DuplicateCredIds(DuplicateCredIds),
    NonExistentCredIds(NonExistentCredIds),
    RemoveFirstCredential(RemoveFirstCredential),
    CredentialHolderDidNotSign(CredentialHolderDidNotSign),
    NotAllowedMultipleCredentials(NotAllowedMultipleCredentials),
    NotAllowedToReceiveEncrypted(NotAllowedToReceiveEncrypted),
    NotAllowedToHandleEncrypted(NotAllowedToHandleEncrypted),
    MissingBakerAddParameters(MissingBakerAddParameters),
    FinalizationRewardCommissionNotInRange(FinalizationRewardCommissionNotInRange),
    BakingRewardCommissionNotInRange(BakingRewardCommissionNotInRange),
    TransactionFeeCommissionNotInRange(TransactionFeeCommissionNotInRange),
    AlreadyADelegator(AlreadyADelegator),
    InsufficientBalanceForDelegationStake(InsufficientBalanceForDelegationStake),
    MissingDelegationAddParameters(MissingDelegationAddParameters),
    InsufficientDelegationStake(InsufficientDelegationStake),
    DelegatorInCooldown(DelegatorInCooldown),
    NotADelegator(NotADelegator),
    DelegationTargetNotABaker(DelegationTargetNotABaker),
    StakeOverMaximumThresholdForPool(StakeOverMaximumThresholdForPool),
    PoolWouldBecomeOverDelegated(PoolWouldBecomeOverDelegated),
    PoolClosed(PoolClosed),
    NonExistentTokenId(NonExistentTokenId),
    TokenUpdateTransactionFailed(TokenModuleReject),
    UnauthorizedTokenGovernance(UnauthorizedTokenGovernance),
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone, Copy)]
pub struct ModuleNotWf {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct ModuleHashAlreadyExists {
    module_ref: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct InvalidInitMethod {
    module_ref: String,
    init_name:  String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct InvalidReceiveMethod {
    module_ref:   String,
    receive_name: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct InvalidAccountReference {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct InvalidModuleReference {
    module_ref: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct InvalidContractAddress {
    contract_address: ContractAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct RuntimeFailure {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct AmountTooLarge {
    address: Address,
    amount:  Amount,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct SerializationFailure {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct OutOfEnergy {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct RejectedInit {
    reject_reason: i32,
}

/// Transaction updating a smart contract instance was rejected.
#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct RejectedReceive {
    /// Reject reason code produced by the smart contract instance.
    reject_reason:          i32,
    /// Address of the smart contract instance which rejected the update.
    contract_address:       ContractAddress,
    /// The name of the entry point called in the smart contract instance (in
    /// ReceiveName format '<contract_name>.<entrypoint>').
    receive_name:           String,
    /// The HEX representation of the message provided for the smart contract
    /// instance as parameter.
    message_as_hex:         String,
    /// The JSON representation of the message provided for the smart contract
    /// instance as parameter. Decoded using the smart contract module
    /// schema if present otherwise undefined. Failing to parse the message
    /// will result in this being undefined and `message_parsing_status`
    /// representing the error.
    message:                Option<String>,
    /// The status of parsing `message` into its JSON representation using the
    /// smart contract module schema.
    message_parsing_status: InstanceMessageParsingStatus,
}

/// The status of parsing `message` into its JSON representation using the
/// smart contract module schema.
#[derive(Enum, PartialEq, Eq, Clone, Copy, serde::Serialize, serde::Deserialize)]
pub enum InstanceMessageParsingStatus {
    /// Parsing succeeded.
    Success,
    /// No message was provided.
    EmptyMessage,
    /// No module schema found in the deployed smart contract module.
    ModuleSchemaNotFound,
    /// Relevant smart contract not found in smart contract module schema.
    ContractNotFound,
    /// Relevant smart contract function not found in smart contract schema.
    FunctionNotFound,
    /// Schema for parameter not found in smart contract schema.
    ParamNotFound,
    /// Failed to construct the JSON representation from message using the smart
    /// contract schema.
    Failed,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct NonExistentRewardAccount {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct InvalidProof {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct AlreadyABaker {
    baker_id: BakerId,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct NotABaker {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct InsufficientBalanceForBakerStake {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct InsufficientBalanceForDelegationStake {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct InsufficientDelegationStake {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct StakeUnderMinimumThresholdForBaking {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct StakeOverMaximumThresholdForPool {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct BakerInCooldown {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct DuplicateAggregationKey {
    aggregation_key: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct NonExistentCredentialId {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct KeyIndexAlreadyInUse {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct InvalidAccountThreshold {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct InvalidCredentialKeySignThreshold {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct InvalidEncryptedAmountTransferProof {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct InvalidTransferToPublicProof {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct EncryptedAmountSelfTransfer {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct InvalidIndexOnEncryptedTransfer {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct ZeroScheduledAmount {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct NonIncreasingSchedule {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct FirstScheduledReleaseExpired {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct ScheduledSelfTransfer {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct InvalidCredentials {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct DuplicateCredIds {
    cred_ids: Vec<String>,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct NonExistentCredIds {
    cred_ids: Vec<String>,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct RemoveFirstCredential {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct CredentialHolderDidNotSign {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct NotAllowedMultipleCredentials {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct NotAllowedToReceiveEncrypted {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct NotAllowedToHandleEncrypted {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct MissingBakerAddParameters {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct FinalizationRewardCommissionNotInRange {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct BakingRewardCommissionNotInRange {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct TransactionFeeCommissionNotInRange {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct AlreadyADelegator {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct MissingDelegationAddParameters {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct DelegatorInCooldown {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct NotADelegator {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct DelegationTargetNotABaker {
    baker_id: BakerId,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct PoolWouldBecomeOverDelegated {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct PoolClosed {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct NonExistentTokenId {
    token_id: String,
}
#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]
pub struct TokenModuleReject {
    /// The unique symbol of the token, which produced this event.
    pub token_id:    String,
    /// The type of event produced.
    pub reason_type: String,
    /// The details of the event produced, in the raw byte encoded form.
    pub details:     serde_json::Value,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize, Clone)]

pub struct UnauthorizedTokenGovernance {
    /// The unique symbol of the token, which produced this event.
    pub token_id: String,
}

/// TransactionRejectReason being prepared for indexing.
/// Most reject reasons can just be inserted, but a few require some processing
/// before inserting.
#[derive(Debug)]
pub enum PreparedTransactionRejectReason {
    /// Reject reasons which are ready for indexing.
    Ready(serde_json::Value),
    /// Reject receive which require processing.
    RejectedReceive(PreparedRejectedReceive),
}

impl PreparedTransactionRejectReason {
    pub fn prepare(
        sdk_reject_reason: concordium_rust_sdk::types::RejectReason,
    ) -> anyhow::Result<Self> {
        use concordium_rust_sdk::types::RejectReason;
        if let RejectReason::RejectedReceive {
            reject_reason,
            contract_address,
            receive_name,
            parameter,
        } = sdk_reject_reason
        {
            return Ok(Self::RejectedReceive(PreparedRejectedReceive {
                reject_reason,
                contract_address: contract_address.into(),
                receive_name: receive_name.to_string(),
                message_as_hex: hex::encode(parameter.as_ref()),
            }));
        }
        let reason = match sdk_reject_reason {
            RejectReason::RejectedReceive {
                ..
            } => anyhow::bail!(
                "Unexpected RejectedReceive. This reject reason needs further processing, and \
                 should be handled above"
            ),
            RejectReason::ModuleNotWF => TransactionRejectReason::ModuleNotWf(ModuleNotWf {
                dummy: true,
            }),
            RejectReason::ModuleHashAlreadyExists {
                contents,
            } => TransactionRejectReason::ModuleHashAlreadyExists(ModuleHashAlreadyExists {
                module_ref: contents.to_string(),
            }),
            RejectReason::InvalidAccountReference {
                contents,
            } => TransactionRejectReason::InvalidAccountReference(InvalidAccountReference {
                account_address: contents.into(),
            }),
            RejectReason::InvalidInitMethod {
                contents,
            } => TransactionRejectReason::InvalidInitMethod(InvalidInitMethod {
                module_ref: contents.0.to_string(),
                init_name:  contents.1.to_string(),
            }),
            RejectReason::InvalidReceiveMethod {
                contents,
            } => TransactionRejectReason::InvalidReceiveMethod(InvalidReceiveMethod {
                module_ref:   contents.0.to_string(),
                receive_name: contents.1.to_string(),
            }),
            RejectReason::InvalidModuleReference {
                contents,
            } => TransactionRejectReason::InvalidModuleReference(InvalidModuleReference {
                module_ref: contents.to_string(),
            }),
            RejectReason::InvalidContractAddress {
                contents,
            } => TransactionRejectReason::InvalidContractAddress(InvalidContractAddress {
                contract_address: contents.into(),
            }),
            RejectReason::RuntimeFailure => {
                TransactionRejectReason::RuntimeFailure(RuntimeFailure {
                    dummy: true,
                })
            }
            RejectReason::AmountTooLarge {
                contents,
            } => TransactionRejectReason::AmountTooLarge(AmountTooLarge {
                address: contents.0.into(),
                amount:  contents.1.micro_ccd().into(),
            }),
            RejectReason::SerializationFailure => {
                TransactionRejectReason::SerializationFailure(SerializationFailure {
                    dummy: true,
                })
            }
            RejectReason::OutOfEnergy => TransactionRejectReason::OutOfEnergy(OutOfEnergy {
                dummy: true,
            }),
            RejectReason::RejectedInit {
                reject_reason,
            } => TransactionRejectReason::RejectedInit(RejectedInit {
                reject_reason,
            }),
            RejectReason::InvalidProof => TransactionRejectReason::InvalidProof(InvalidProof {
                dummy: true,
            }),
            RejectReason::AlreadyABaker {
                contents,
            } => TransactionRejectReason::AlreadyABaker(AlreadyABaker {
                baker_id: contents.id.index.try_into()?,
            }),
            RejectReason::NotABaker {
                contents,
            } => TransactionRejectReason::NotABaker(NotABaker {
                account_address: contents.into(),
            }),
            RejectReason::InsufficientBalanceForBakerStake => {
                TransactionRejectReason::InsufficientBalanceForBakerStake(
                    InsufficientBalanceForBakerStake {
                        dummy: true,
                    },
                )
            }
            RejectReason::StakeUnderMinimumThresholdForBaking => {
                TransactionRejectReason::StakeUnderMinimumThresholdForBaking(
                    StakeUnderMinimumThresholdForBaking {
                        dummy: true,
                    },
                )
            }
            RejectReason::BakerInCooldown => {
                TransactionRejectReason::BakerInCooldown(BakerInCooldown {
                    dummy: true,
                })
            }
            RejectReason::DuplicateAggregationKey {
                contents,
            } => TransactionRejectReason::DuplicateAggregationKey(DuplicateAggregationKey {
                aggregation_key: serde_json::to_string(&contents)?,
            }),
            RejectReason::NonExistentCredentialID => {
                TransactionRejectReason::NonExistentCredentialId(NonExistentCredentialId {
                    dummy: true,
                })
            }
            RejectReason::KeyIndexAlreadyInUse => {
                TransactionRejectReason::KeyIndexAlreadyInUse(KeyIndexAlreadyInUse {
                    dummy: true,
                })
            }
            RejectReason::InvalidAccountThreshold => {
                TransactionRejectReason::InvalidAccountThreshold(InvalidAccountThreshold {
                    dummy: true,
                })
            }
            RejectReason::InvalidCredentialKeySignThreshold => {
                TransactionRejectReason::InvalidCredentialKeySignThreshold(
                    InvalidCredentialKeySignThreshold {
                        dummy: true,
                    },
                )
            }
            RejectReason::InvalidEncryptedAmountTransferProof => {
                TransactionRejectReason::InvalidEncryptedAmountTransferProof(
                    InvalidEncryptedAmountTransferProof {
                        dummy: true,
                    },
                )
            }
            RejectReason::InvalidTransferToPublicProof => {
                TransactionRejectReason::InvalidTransferToPublicProof(
                    InvalidTransferToPublicProof {
                        dummy: true,
                    },
                )
            }
            RejectReason::EncryptedAmountSelfTransfer {
                contents,
            } => {
                TransactionRejectReason::EncryptedAmountSelfTransfer(EncryptedAmountSelfTransfer {
                    account_address: contents.into(),
                })
            }
            RejectReason::InvalidIndexOnEncryptedTransfer => {
                TransactionRejectReason::InvalidIndexOnEncryptedTransfer(
                    InvalidIndexOnEncryptedTransfer {
                        dummy: true,
                    },
                )
            }
            RejectReason::ZeroScheduledAmount => {
                TransactionRejectReason::ZeroScheduledAmount(ZeroScheduledAmount {
                    dummy: true,
                })
            }
            RejectReason::NonIncreasingSchedule => {
                TransactionRejectReason::NonIncreasingSchedule(NonIncreasingSchedule {
                    dummy: true,
                })
            }
            RejectReason::FirstScheduledReleaseExpired => {
                TransactionRejectReason::FirstScheduledReleaseExpired(
                    FirstScheduledReleaseExpired {
                        dummy: true,
                    },
                )
            }
            RejectReason::ScheduledSelfTransfer {
                contents,
            } => TransactionRejectReason::ScheduledSelfTransfer(ScheduledSelfTransfer {
                account_address: contents.into(),
            }),
            RejectReason::InvalidCredentials => {
                TransactionRejectReason::InvalidCredentials(InvalidCredentials {
                    dummy: true,
                })
            }
            RejectReason::DuplicateCredIDs {
                contents,
            } => TransactionRejectReason::DuplicateCredIds(DuplicateCredIds {
                cred_ids: contents.into_iter().map(|cred_id| cred_id.to_string()).collect(),
            }),
            RejectReason::NonExistentCredIDs {
                contents,
            } => TransactionRejectReason::NonExistentCredIds(NonExistentCredIds {
                cred_ids: contents.into_iter().map(|cred_id| cred_id.to_string()).collect(),
            }),
            RejectReason::RemoveFirstCredential => {
                TransactionRejectReason::RemoveFirstCredential(RemoveFirstCredential {
                    dummy: true,
                })
            }
            RejectReason::CredentialHolderDidNotSign => {
                TransactionRejectReason::CredentialHolderDidNotSign(CredentialHolderDidNotSign {
                    dummy: true,
                })
            }
            RejectReason::NotAllowedMultipleCredentials => {
                TransactionRejectReason::NotAllowedMultipleCredentials(
                    NotAllowedMultipleCredentials {
                        dummy: true,
                    },
                )
            }
            RejectReason::NotAllowedToReceiveEncrypted => {
                TransactionRejectReason::NotAllowedToReceiveEncrypted(
                    NotAllowedToReceiveEncrypted {
                        dummy: true,
                    },
                )
            }
            RejectReason::NotAllowedToHandleEncrypted => {
                TransactionRejectReason::NotAllowedToHandleEncrypted(NotAllowedToHandleEncrypted {
                    dummy: true,
                })
            }
            RejectReason::MissingBakerAddParameters => {
                TransactionRejectReason::MissingBakerAddParameters(MissingBakerAddParameters {
                    dummy: true,
                })
            }
            RejectReason::FinalizationRewardCommissionNotInRange => {
                TransactionRejectReason::FinalizationRewardCommissionNotInRange(
                    FinalizationRewardCommissionNotInRange {
                        dummy: true,
                    },
                )
            }
            RejectReason::BakingRewardCommissionNotInRange => {
                TransactionRejectReason::BakingRewardCommissionNotInRange(
                    BakingRewardCommissionNotInRange {
                        dummy: true,
                    },
                )
            }
            RejectReason::TransactionFeeCommissionNotInRange => {
                TransactionRejectReason::TransactionFeeCommissionNotInRange(
                    TransactionFeeCommissionNotInRange {
                        dummy: true,
                    },
                )
            }
            RejectReason::AlreadyADelegator => {
                TransactionRejectReason::AlreadyADelegator(AlreadyADelegator {
                    dummy: true,
                })
            }
            RejectReason::InsufficientBalanceForDelegationStake => {
                TransactionRejectReason::InsufficientBalanceForDelegationStake(
                    InsufficientBalanceForDelegationStake {
                        dummy: true,
                    },
                )
            }
            RejectReason::MissingDelegationAddParameters => {
                TransactionRejectReason::MissingDelegationAddParameters(
                    MissingDelegationAddParameters {
                        dummy: true,
                    },
                )
            }
            RejectReason::InsufficientDelegationStake => {
                TransactionRejectReason::InsufficientDelegationStake(InsufficientDelegationStake {
                    dummy: true,
                })
            }
            RejectReason::DelegatorInCooldown => {
                TransactionRejectReason::DelegatorInCooldown(DelegatorInCooldown {
                    dummy: true,
                })
            }
            RejectReason::NotADelegator {
                address,
            } => TransactionRejectReason::NotADelegator(NotADelegator {
                account_address: address.into(),
            }),
            RejectReason::DelegationTargetNotABaker {
                target,
            } => TransactionRejectReason::DelegationTargetNotABaker(DelegationTargetNotABaker {
                baker_id: target.id.index.try_into()?,
            }),
            RejectReason::StakeOverMaximumThresholdForPool => {
                TransactionRejectReason::StakeOverMaximumThresholdForPool(
                    StakeOverMaximumThresholdForPool {
                        dummy: true,
                    },
                )
            }
            RejectReason::PoolWouldBecomeOverDelegated => {
                TransactionRejectReason::PoolWouldBecomeOverDelegated(
                    PoolWouldBecomeOverDelegated {
                        dummy: true,
                    },
                )
            }
            RejectReason::PoolClosed => TransactionRejectReason::PoolClosed(PoolClosed {
                dummy: true,
            }),
            RejectReason::NonExistentTokenId(token_id) => {
                TransactionRejectReason::NonExistentTokenId(NonExistentTokenId {
                    token_id: token_id.clone().into(),
                })
            }
            RejectReason::TokenUpdateTransactionFailed(token_module_reject_reason) => {
                let details =
                    TokenModuleRejectReason::decode_reject_reason(&token_module_reject_reason)
                        .context("Failed to decode token module transaction failure reason")?;
                TransactionRejectReason::TokenUpdateTransactionFailed(TokenModuleReject {
                    token_id:    token_module_reject_reason.token_id.clone().into(),
                    reason_type: token_module_reject_reason.clone().reason_type.into(),
                    details:     serde_json::to_value::<TokenModuleRejectReasonTypes>(
                        details.into(),
                    )
                    .context(
                        "Failed to serialize token module transaction failure details to JSON",
                    )?,
                })
            }
        };
        let value = serde_json::to_value(&reason)?;
        Ok(Self::Ready(value))
    }

    pub async fn process(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
    ) -> anyhow::Result<serde_json::Value> {
        let reason = match self {
            Self::Ready(reason) => reason.clone(),
            Self::RejectedReceive(prepared) => {
                let reject = TransactionRejectReason::RejectedReceive(prepared.process(tx).await?);
                serde_json::to_value(reject)?
            }
        };
        Ok(reason)
    }
}

/// Reject receive which require processing the contract update message
/// using the smart contract module schema before insertion.
#[derive(Debug)]
pub struct PreparedRejectedReceive {
    reject_reason:    i32,
    contract_address: ContractAddress,
    receive_name:     String,
    message_as_hex:   String,
}

impl PreparedRejectedReceive {
    pub async fn process(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
    ) -> anyhow::Result<RejectedReceive> {
        // Handle and store errors
        let (message, message_parsing_status) = self.process_message(tx).await?;
        Ok(RejectedReceive {
            reject_reason: self.reject_reason,
            contract_address: self.contract_address,
            receive_name: self.receive_name.clone(),
            message_as_hex: self.message_as_hex.clone(),
            message,
            message_parsing_status,
        })
    }

    /// Parse the message using the smart contract module of the smart contract
    /// instance.
    async fn process_message(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
    ) -> anyhow::Result<(Option<String>, InstanceMessageParsingStatus)> {
        use InstanceMessageParsingStatus as Status;
        if self.message_as_hex.is_empty() {
            return Ok((None, Status::EmptyMessage));
        }
        let schema = sqlx::query_scalar!(
            "SELECT
                schema
            FROM contracts
                JOIN smart_contract_modules
                    ON smart_contract_modules.module_reference = contracts.module_reference
            WHERE index = $1 AND sub_index = $2",
            i64::try_from(self.contract_address.index.0)?,
            i64::try_from(self.contract_address.sub_index.0)?
        )
        .fetch_one(tx.as_mut())
        .await?;
        let Some(schema) = schema else {
            // No schema found in the smart contract module.
            return Ok((None, Status::ModuleSchemaNotFound));
        };
        let schema = VersionedModuleSchema::new(&schema, &None)
            .context("Failed to parse smart contract module schema")?;
        let receive_name = ReceiveName::new(&self.receive_name)
            .context("Invalid receive name for RejectedReceive")?;
        let schema_type = match schema.get_receive_param_schema(
            receive_name.contract_name(),
            receive_name.entrypoint_name().into(),
        ) {
            Ok(t) => t,
            Err(VersionedSchemaError::NoContractInModule) => {
                return Ok((None, Status::ContractNotFound))
            }
            Err(VersionedSchemaError::NoReceiveInContract) => {
                return Ok((None, Status::FunctionNotFound))
            }
            Err(VersionedSchemaError::NoParamsInReceive) => {
                return Ok((None, Status::ParamNotFound))
            }
            Err(err) => {
                anyhow::bail!("Database bytes should be a valid VersionedModuleSchema: {}", err);
            }
        };
        let message = hex::decode(&self.message_as_hex)
            .context("Failed hex decoding of RejectedReceive message")?;
        let Ok(message) = schema_type.to_json_string_pretty(&message) else {
            return Ok((None, Status::Failed));
        };
        Ok((Some(message), Status::Success))
    }
}
