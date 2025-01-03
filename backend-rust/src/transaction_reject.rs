use crate::{
    address::{AccountAddress, Address, ContractAddress},
    scalar_types::{Amount, BakerId},
};
use async_graphql::{SimpleObject, Union};

#[derive(Union, serde::Serialize, serde::Deserialize)]
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
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ModuleNotWf {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ModuleHashAlreadyExists {
    module_ref: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidInitMethod {
    module_ref: String,
    init_name:  String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidReceiveMethod {
    module_ref:   String,
    receive_name: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidAccountReference {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidModuleReference {
    module_ref: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidContractAddress {
    contract_address: ContractAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct RuntimeFailure {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct AmountTooLarge {
    address: Address,
    amount:  Amount,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct SerializationFailure {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct OutOfEnergy {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct RejectedInit {
    reject_reason: i32,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct RejectedReceive {
    reject_reason:    i32,
    contract_address: ContractAddress,
    receive_name:     String,
    message_as_hex:   String,
    // TODO message: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NonExistentRewardAccount {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidProof {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct AlreadyABaker {
    baker_id: BakerId,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NotABaker {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InsufficientBalanceForBakerStake {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InsufficientBalanceForDelegationStake {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InsufficientDelegationStake {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct StakeUnderMinimumThresholdForBaking {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct StakeOverMaximumThresholdForPool {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerInCooldown {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DuplicateAggregationKey {
    aggregation_key: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NonExistentCredentialId {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct KeyIndexAlreadyInUse {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidAccountThreshold {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidCredentialKeySignThreshold {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidEncryptedAmountTransferProof {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidTransferToPublicProof {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct EncryptedAmountSelfTransfer {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidIndexOnEncryptedTransfer {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ZeroScheduledAmount {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NonIncreasingSchedule {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct FirstScheduledReleaseExpired {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct ScheduledSelfTransfer {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct InvalidCredentials {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DuplicateCredIds {
    cred_ids: Vec<String>,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NonExistentCredIds {
    cred_ids: Vec<String>,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct RemoveFirstCredential {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct CredentialHolderDidNotSign {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NotAllowedMultipleCredentials {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NotAllowedToReceiveEncrypted {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NotAllowedToHandleEncrypted {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct MissingBakerAddParameters {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct FinalizationRewardCommissionNotInRange {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakingRewardCommissionNotInRange {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct TransactionFeeCommissionNotInRange {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct AlreadyADelegator {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct MissingDelegationAddParameters {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DelegatorInCooldown {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NotADelegator {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DelegationTargetNotABaker {
    baker_id: BakerId,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct PoolWouldBecomeOverDelegated {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct PoolClosed {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    dummy: bool,
}

impl TryFrom<concordium_rust_sdk::types::RejectReason> for TransactionRejectReason {
    type Error = anyhow::Error;

    fn try_from(reason: concordium_rust_sdk::types::RejectReason) -> Result<Self, Self::Error> {
        use concordium_rust_sdk::types::RejectReason;
        match reason {
            RejectReason::ModuleNotWF => Ok(TransactionRejectReason::ModuleNotWf(ModuleNotWf {
                dummy: true,
            })),
            RejectReason::ModuleHashAlreadyExists {
                contents,
            } => Ok(TransactionRejectReason::ModuleHashAlreadyExists(ModuleHashAlreadyExists {
                module_ref: contents.to_string(),
            })),
            RejectReason::InvalidAccountReference {
                contents,
            } => Ok(TransactionRejectReason::InvalidAccountReference(InvalidAccountReference {
                account_address: contents.into(),
            })),
            RejectReason::InvalidInitMethod {
                contents,
            } => Ok(TransactionRejectReason::InvalidInitMethod(InvalidInitMethod {
                module_ref: contents.0.to_string(),
                init_name:  contents.1.to_string(),
            })),
            RejectReason::InvalidReceiveMethod {
                contents,
            } => Ok(TransactionRejectReason::InvalidReceiveMethod(InvalidReceiveMethod {
                module_ref:   contents.0.to_string(),
                receive_name: contents.1.to_string(),
            })),
            RejectReason::InvalidModuleReference {
                contents,
            } => Ok(TransactionRejectReason::InvalidModuleReference(InvalidModuleReference {
                module_ref: contents.to_string(),
            })),
            RejectReason::InvalidContractAddress {
                contents,
            } => Ok(TransactionRejectReason::InvalidContractAddress(InvalidContractAddress {
                contract_address: contents.into(),
            })),
            RejectReason::RuntimeFailure => {
                Ok(TransactionRejectReason::RuntimeFailure(RuntimeFailure {
                    dummy: true,
                }))
            }
            RejectReason::AmountTooLarge {
                contents,
            } => Ok(TransactionRejectReason::AmountTooLarge(AmountTooLarge {
                address: contents.0.into(),
                amount:  contents.1.micro_ccd().try_into()?,
            })),
            RejectReason::SerializationFailure => {
                Ok(TransactionRejectReason::SerializationFailure(SerializationFailure {
                    dummy: true,
                }))
            }
            RejectReason::OutOfEnergy => Ok(TransactionRejectReason::OutOfEnergy(OutOfEnergy {
                dummy: true,
            })),
            RejectReason::RejectedInit {
                reject_reason,
            } => Ok(TransactionRejectReason::RejectedInit(RejectedInit {
                reject_reason,
            })),
            RejectReason::RejectedReceive {
                reject_reason,
                contract_address,
                receive_name,
                parameter,
            } => {
                Ok(TransactionRejectReason::RejectedReceive(RejectedReceive {
                    reject_reason,
                    contract_address: contract_address.into(),
                    receive_name: receive_name.to_string(),
                    message_as_hex: hex::encode(parameter.as_ref()),
                    // message: todo!(),
                }))
            }
            RejectReason::InvalidProof => Ok(TransactionRejectReason::InvalidProof(InvalidProof {
                dummy: true,
            })),
            RejectReason::AlreadyABaker {
                contents,
            } => Ok(TransactionRejectReason::AlreadyABaker(AlreadyABaker {
                baker_id: contents.id.index.try_into()?,
            })),
            RejectReason::NotABaker {
                contents,
            } => Ok(TransactionRejectReason::NotABaker(NotABaker {
                account_address: contents.into(),
            })),
            RejectReason::InsufficientBalanceForBakerStake => {
                Ok(TransactionRejectReason::InsufficientBalanceForBakerStake(
                    InsufficientBalanceForBakerStake {
                        dummy: true,
                    },
                ))
            }
            RejectReason::StakeUnderMinimumThresholdForBaking => {
                Ok(TransactionRejectReason::StakeUnderMinimumThresholdForBaking(
                    StakeUnderMinimumThresholdForBaking {
                        dummy: true,
                    },
                ))
            }
            RejectReason::BakerInCooldown => {
                Ok(TransactionRejectReason::BakerInCooldown(BakerInCooldown {
                    dummy: true,
                }))
            }
            RejectReason::DuplicateAggregationKey {
                contents,
            } => Ok(TransactionRejectReason::DuplicateAggregationKey(DuplicateAggregationKey {
                aggregation_key: serde_json::to_string(&contents)?,
            })),
            RejectReason::NonExistentCredentialID => {
                Ok(TransactionRejectReason::NonExistentCredentialId(NonExistentCredentialId {
                    dummy: true,
                }))
            }
            RejectReason::KeyIndexAlreadyInUse => {
                Ok(TransactionRejectReason::KeyIndexAlreadyInUse(KeyIndexAlreadyInUse {
                    dummy: true,
                }))
            }
            RejectReason::InvalidAccountThreshold => {
                Ok(TransactionRejectReason::InvalidAccountThreshold(InvalidAccountThreshold {
                    dummy: true,
                }))
            }
            RejectReason::InvalidCredentialKeySignThreshold => {
                Ok(TransactionRejectReason::InvalidCredentialKeySignThreshold(
                    InvalidCredentialKeySignThreshold {
                        dummy: true,
                    },
                ))
            }
            RejectReason::InvalidEncryptedAmountTransferProof => {
                Ok(TransactionRejectReason::InvalidEncryptedAmountTransferProof(
                    InvalidEncryptedAmountTransferProof {
                        dummy: true,
                    },
                ))
            }
            RejectReason::InvalidTransferToPublicProof => {
                Ok(TransactionRejectReason::InvalidTransferToPublicProof(
                    InvalidTransferToPublicProof {
                        dummy: true,
                    },
                ))
            }
            RejectReason::EncryptedAmountSelfTransfer {
                contents,
            } => Ok(TransactionRejectReason::EncryptedAmountSelfTransfer(
                EncryptedAmountSelfTransfer {
                    account_address: contents.into(),
                },
            )),
            RejectReason::InvalidIndexOnEncryptedTransfer => {
                Ok(TransactionRejectReason::InvalidIndexOnEncryptedTransfer(
                    InvalidIndexOnEncryptedTransfer {
                        dummy: true,
                    },
                ))
            }
            RejectReason::ZeroScheduledAmount => {
                Ok(TransactionRejectReason::ZeroScheduledAmount(ZeroScheduledAmount {
                    dummy: true,
                }))
            }
            RejectReason::NonIncreasingSchedule => {
                Ok(TransactionRejectReason::NonIncreasingSchedule(NonIncreasingSchedule {
                    dummy: true,
                }))
            }
            RejectReason::FirstScheduledReleaseExpired => {
                Ok(TransactionRejectReason::FirstScheduledReleaseExpired(
                    FirstScheduledReleaseExpired {
                        dummy: true,
                    },
                ))
            }
            RejectReason::ScheduledSelfTransfer {
                contents,
            } => Ok(TransactionRejectReason::ScheduledSelfTransfer(ScheduledSelfTransfer {
                account_address: contents.into(),
            })),
            RejectReason::InvalidCredentials => {
                Ok(TransactionRejectReason::InvalidCredentials(InvalidCredentials {
                    dummy: true,
                }))
            }
            RejectReason::DuplicateCredIDs {
                contents,
            } => Ok(TransactionRejectReason::DuplicateCredIds(DuplicateCredIds {
                cred_ids: contents.into_iter().map(|cred_id| cred_id.to_string()).collect(),
            })),
            RejectReason::NonExistentCredIDs {
                contents,
            } => Ok(TransactionRejectReason::NonExistentCredIds(NonExistentCredIds {
                cred_ids: contents.into_iter().map(|cred_id| cred_id.to_string()).collect(),
            })),
            RejectReason::RemoveFirstCredential => {
                Ok(TransactionRejectReason::RemoveFirstCredential(RemoveFirstCredential {
                    dummy: true,
                }))
            }
            RejectReason::CredentialHolderDidNotSign => Ok(
                TransactionRejectReason::CredentialHolderDidNotSign(CredentialHolderDidNotSign {
                    dummy: true,
                }),
            ),
            RejectReason::NotAllowedMultipleCredentials => {
                Ok(TransactionRejectReason::NotAllowedMultipleCredentials(
                    NotAllowedMultipleCredentials {
                        dummy: true,
                    },
                ))
            }
            RejectReason::NotAllowedToReceiveEncrypted => {
                Ok(TransactionRejectReason::NotAllowedToReceiveEncrypted(
                    NotAllowedToReceiveEncrypted {
                        dummy: true,
                    },
                ))
            }
            RejectReason::NotAllowedToHandleEncrypted => Ok(
                TransactionRejectReason::NotAllowedToHandleEncrypted(NotAllowedToHandleEncrypted {
                    dummy: true,
                }),
            ),
            RejectReason::MissingBakerAddParameters => {
                Ok(TransactionRejectReason::MissingBakerAddParameters(MissingBakerAddParameters {
                    dummy: true,
                }))
            }
            RejectReason::FinalizationRewardCommissionNotInRange => {
                Ok(TransactionRejectReason::FinalizationRewardCommissionNotInRange(
                    FinalizationRewardCommissionNotInRange {
                        dummy: true,
                    },
                ))
            }
            RejectReason::BakingRewardCommissionNotInRange => {
                Ok(TransactionRejectReason::BakingRewardCommissionNotInRange(
                    BakingRewardCommissionNotInRange {
                        dummy: true,
                    },
                ))
            }
            RejectReason::TransactionFeeCommissionNotInRange => {
                Ok(TransactionRejectReason::TransactionFeeCommissionNotInRange(
                    TransactionFeeCommissionNotInRange {
                        dummy: true,
                    },
                ))
            }
            RejectReason::AlreadyADelegator => {
                Ok(TransactionRejectReason::AlreadyADelegator(AlreadyADelegator {
                    dummy: true,
                }))
            }
            RejectReason::InsufficientBalanceForDelegationStake => {
                Ok(TransactionRejectReason::InsufficientBalanceForDelegationStake(
                    InsufficientBalanceForDelegationStake {
                        dummy: true,
                    },
                ))
            }
            RejectReason::MissingDelegationAddParameters => {
                Ok(TransactionRejectReason::MissingDelegationAddParameters(
                    MissingDelegationAddParameters {
                        dummy: true,
                    },
                ))
            }
            RejectReason::InsufficientDelegationStake => Ok(
                TransactionRejectReason::InsufficientDelegationStake(InsufficientDelegationStake {
                    dummy: true,
                }),
            ),
            RejectReason::DelegatorInCooldown => {
                Ok(TransactionRejectReason::DelegatorInCooldown(DelegatorInCooldown {
                    dummy: true,
                }))
            }
            RejectReason::NotADelegator {
                address,
            } => Ok(TransactionRejectReason::NotADelegator(NotADelegator {
                account_address: address.into(),
            })),
            RejectReason::DelegationTargetNotABaker {
                target,
            } => {
                Ok(TransactionRejectReason::DelegationTargetNotABaker(DelegationTargetNotABaker {
                    baker_id: target.id.index.try_into()?,
                }))
            }
            RejectReason::StakeOverMaximumThresholdForPool => {
                Ok(TransactionRejectReason::StakeOverMaximumThresholdForPool(
                    StakeOverMaximumThresholdForPool {
                        dummy: true,
                    },
                ))
            }
            RejectReason::PoolWouldBecomeOverDelegated => {
                Ok(TransactionRejectReason::PoolWouldBecomeOverDelegated(
                    PoolWouldBecomeOverDelegated {
                        dummy: true,
                    },
                ))
            }
            RejectReason::PoolClosed => Ok(TransactionRejectReason::PoolClosed(PoolClosed {
                dummy: true,
            })),
        }
    }
}
