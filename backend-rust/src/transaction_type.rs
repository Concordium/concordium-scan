//! Transaction types, several types in this module are used both by the indexer
//! and the graphQL API.
use async_graphql::{Enum, SimpleObject, Union};

#[derive(Debug, sqlx::Type, Copy, Clone)]
#[sqlx(type_name = "transaction_type")] // only for PostgreSQL to match a type definition
pub enum DbTransactionType {
    Account,
    CredentialDeployment,
    Update,
}

#[derive(Union)]
#[allow(clippy::enum_variant_names)]
pub enum TransactionType {
    AccountTransaction(AccountTransaction),
    CredentialDeploymentTransaction(CredentialDeploymentTransaction),
    UpdateTransaction(UpdateTransaction),
}

#[derive(SimpleObject)]
pub struct AccountTransaction {
    pub account_transaction_type: Option<AccountTransactionType>,
}

#[derive(Debug, Enum, Clone, Copy, PartialEq, Eq, sqlx::Type)]
#[sqlx(type_name = "account_transaction_type")]
pub enum AccountTransactionType {
    InitializeSmartContractInstance,
    UpdateSmartContractInstance,
    SimpleTransfer,
    EncryptedTransfer,
    SimpleTransferWithMemo,
    EncryptedTransferWithMemo,
    TransferWithScheduleWithMemo,
    DeployModule,
    AddBaker,
    RemoveBaker,
    UpdateBakerStake,
    UpdateBakerRestakeEarnings,
    UpdateBakerKeys,
    UpdateCredentialKeys,
    TransferToEncrypted,
    TransferToPublic,
    TransferWithSchedule,
    UpdateCredentials,
    RegisterData,
    ConfigureBaker,
    ConfigureDelegation,
}

impl From<concordium_rust_sdk::types::TransactionType> for AccountTransactionType {
    fn from(value: concordium_rust_sdk::types::TransactionType) -> Self {
        use concordium_rust_sdk::types::TransactionType as TT;
        use AccountTransactionType as ATT;
        #[allow(deprecated)]
        match value {
            TT::DeployModule => ATT::DeployModule,
            TT::InitContract => ATT::InitializeSmartContractInstance,
            TT::Update => ATT::UpdateSmartContractInstance,
            TT::Transfer => ATT::SimpleTransfer,
            TT::AddBaker => ATT::AddBaker,
            TT::RemoveBaker => ATT::RemoveBaker,
            TT::UpdateBakerStake => ATT::UpdateBakerStake,
            TT::UpdateBakerRestakeEarnings => ATT::UpdateBakerRestakeEarnings,
            TT::UpdateBakerKeys => ATT::UpdateBakerKeys,
            TT::UpdateCredentialKeys => ATT::UpdateCredentialKeys,
            TT::EncryptedAmountTransfer => ATT::EncryptedTransfer,
            TT::TransferToEncrypted => ATT::TransferToEncrypted,
            TT::TransferToPublic => ATT::TransferToPublic,
            TT::TransferWithSchedule => ATT::TransferWithSchedule,
            TT::UpdateCredentials => ATT::UpdateCredentials,
            TT::RegisterData => ATT::RegisterData,
            TT::TransferWithMemo => ATT::SimpleTransferWithMemo,
            TT::EncryptedAmountTransferWithMemo => ATT::EncryptedTransferWithMemo,
            TT::TransferWithScheduleAndMemo => ATT::TransferWithScheduleWithMemo,
            TT::ConfigureBaker => ATT::ConfigureBaker,
            TT::ConfigureDelegation => ATT::ConfigureDelegation,
        }
    }
}

#[derive(SimpleObject)]
pub struct CredentialDeploymentTransaction {
    pub credential_deployment_transaction_type: CredentialDeploymentTransactionType,
}

#[derive(Debug, Enum, Clone, Copy, PartialEq, Eq, sqlx::Type)]
#[sqlx(type_name = "credential_deployment_transaction_type")]
pub enum CredentialDeploymentTransactionType {
    Initial,
    Normal,
}

impl From<concordium_rust_sdk::types::CredentialType> for CredentialDeploymentTransactionType {
    fn from(value: concordium_rust_sdk::types::CredentialType) -> Self {
        use concordium_rust_sdk::types::CredentialType;
        match value {
            CredentialType::Initial => CredentialDeploymentTransactionType::Initial,
            CredentialType::Normal => CredentialDeploymentTransactionType::Normal,
        }
    }
}

#[derive(SimpleObject)]
pub struct UpdateTransaction {
    pub update_transaction_type: UpdateTransactionType,
}

#[derive(Debug, Enum, Clone, Copy, PartialEq, Eq, sqlx::Type)]
#[sqlx(type_name = "update_transaction_type")]
pub enum UpdateTransactionType {
    UpdateProtocol,
    UpdateElectionDifficulty,
    UpdateEuroPerEnergy,
    UpdateMicroGtuPerEuro,
    UpdateFoundationAccount,
    UpdateMintDistribution,
    UpdateTransactionFeeDistribution,
    UpdateGasRewards,
    UpdateBakerStakeThreshold,
    UpdateAddAnonymityRevoker,
    UpdateAddIdentityProvider,
    UpdateRootKeys,
    UpdateLevel1Keys,
    UpdateLevel2Keys,
    UpdatePoolParameters,
    UpdateCooldownParameters,
    UpdateTimeParameters,
    MintDistributionCpv1Update,
    GasRewardsCpv2Update,
    TimeoutParametersUpdate,
    MinBlockTimeUpdate,
    BlockEnergyLimitUpdate,
    FinalizationCommitteeParametersUpdate,
    ValidatorScoreParametersUpdate,
}

impl From<concordium_rust_sdk::types::UpdateType> for UpdateTransactionType {
    fn from(value: concordium_rust_sdk::types::UpdateType) -> Self {
        use concordium_rust_sdk::types::UpdateType;
        match value {
            UpdateType::UpdateProtocol => UpdateTransactionType::UpdateProtocol,
            UpdateType::UpdateElectionDifficulty => UpdateTransactionType::UpdateElectionDifficulty,
            UpdateType::UpdateEuroPerEnergy => UpdateTransactionType::UpdateEuroPerEnergy,
            UpdateType::UpdateMicroGTUPerEuro => UpdateTransactionType::UpdateMicroGtuPerEuro,
            UpdateType::UpdateFoundationAccount => UpdateTransactionType::UpdateFoundationAccount,
            UpdateType::UpdateMintDistribution => UpdateTransactionType::UpdateMintDistribution,
            UpdateType::UpdateTransactionFeeDistribution => {
                UpdateTransactionType::UpdateTransactionFeeDistribution
            }
            UpdateType::UpdateGASRewards => UpdateTransactionType::UpdateGasRewards,
            UpdateType::UpdateAddAnonymityRevoker => {
                UpdateTransactionType::UpdateAddAnonymityRevoker
            }
            UpdateType::UpdateAddIdentityProvider => {
                UpdateTransactionType::UpdateAddIdentityProvider
            }
            UpdateType::UpdateRootKeys => UpdateTransactionType::UpdateRootKeys,
            UpdateType::UpdateLevel1Keys => UpdateTransactionType::UpdateLevel1Keys,
            UpdateType::UpdateLevel2Keys => UpdateTransactionType::UpdateLevel2Keys,
            UpdateType::UpdatePoolParameters => UpdateTransactionType::UpdatePoolParameters,
            UpdateType::UpdateCooldownParameters => UpdateTransactionType::UpdateCooldownParameters,
            UpdateType::UpdateTimeParameters => UpdateTransactionType::UpdateTimeParameters,
            UpdateType::UpdateGASRewardsCPV2 => UpdateTransactionType::GasRewardsCpv2Update,
            UpdateType::UpdateTimeoutParameters => UpdateTransactionType::TimeoutParametersUpdate,
            UpdateType::UpdateMinBlockTime => UpdateTransactionType::MinBlockTimeUpdate,
            UpdateType::UpdateBlockEnergyLimit => UpdateTransactionType::BlockEnergyLimitUpdate,
            UpdateType::UpdateFinalizationCommitteeParameters => {
                UpdateTransactionType::FinalizationCommitteeParametersUpdate
            }
            UpdateType::UpdateValidatorScoreParameters => {
                UpdateTransactionType::ValidatorScoreParametersUpdate
            }
        }
    }
}
