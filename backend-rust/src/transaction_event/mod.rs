use crate::{
    address::Address,
    decoded_text::DecodedText,
    graphql_api::{ApiError, ApiResult},
    scalar_types::{Byte, DateTime},
};
use anyhow::Context;
use async_graphql::{ComplexObject, SimpleObject, Union};
use tracing::error;

pub mod baker;
pub mod chain_update;
pub mod credentials;
pub mod delegation;
pub mod smart_contracts;
pub mod transfers;

#[derive(Union, serde::Serialize, serde::Deserialize)]
pub enum Event {
    /// A transfer of CCD. Can be either from an account or a smart contract
    /// instance, but the receiver in this event is always an account.
    Transferred(transfers::Transferred),
    TransferMemo(transfers::TransferMemo),
    TransferredWithSchedule(transfers::TransferredWithSchedule),
    EncryptedAmountsRemoved(transfers::EncryptedAmountsRemoved),
    AmountAddedByDecryption(transfers::AmountAddedByDecryption),
    EncryptedSelfAmountAdded(transfers::EncryptedSelfAmountAdded),
    NewEncryptedAmount(transfers::NewEncryptedAmount),
    AccountCreated(credentials::AccountCreated),
    CredentialDeployed(credentials::CredentialDeployed),
    CredentialKeysUpdated(credentials::CredentialKeysUpdated),
    CredentialsUpdated(credentials::CredentialsUpdated),
    BakerAdded(baker::BakerAdded),
    BakerKeysUpdated(baker::BakerKeysUpdated),
    BakerRemoved(baker::BakerRemoved),
    BakerSetRestakeEarnings(baker::BakerSetRestakeEarnings),
    BakerStakeDecreased(baker::BakerStakeDecreased),
    BakerStakeIncreased(baker::BakerStakeIncreased),
    BakerSetOpenStatus(baker::BakerSetOpenStatus),
    BakerSetMetadataURL(baker::BakerSetMetadataURL),
    BakerSetTransactionFeeCommission(baker::BakerSetTransactionFeeCommission),
    BakerSetBakingRewardCommission(baker::BakerSetBakingRewardCommission),
    BakerSetFinalizationRewardCommission(baker::BakerSetFinalizationRewardCommission),
    DataRegistered(DataRegistered),
    ChainUpdateEnqueued(chain_update::ChainUpdateEnqueued),
    ContractInitialized(smart_contracts::ContractInitialized),
    ContractModuleDeployed(smart_contracts::ContractModuleDeployed),
    ContractUpdated(smart_contracts::ContractUpdated),
    ContractCall(smart_contracts::ContractCall),
    ContractInterrupted(smart_contracts::ContractInterrupted),
    ContractResumed(smart_contracts::ContractResumed),
    ContractUpgraded(smart_contracts::ContractUpgraded),

    DelegationAdded(delegation::DelegationAdded),
    DelegationRemoved(delegation::DelegationRemoved),
    DelegationStakeIncreased(delegation::DelegationStakeIncreased),
    DelegationStakeDecreased(delegation::DelegationStakeDecreased),
    DelegationSetRestakeEarnings(delegation::DelegationSetRestakeEarnings),
    DelegationSetDelegationTarget(delegation::DelegationSetDelegationTarget),
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct DataRegistered {
    data_as_hex: String,
}

#[ComplexObject]
impl DataRegistered {
    async fn decoded(&self) -> ApiResult<DecodedText> {
        let decoded_data = hex::decode(&self.data_as_hex).map_err(|e| {
            error!("Invalid hex encoding {:?} in a controlled environment", e);
            ApiError::InternalError("Failed to decode hex data".to_string())
        })?;

        Ok(DecodedText::from_bytes(decoded_data.as_slice()))
    }
}

pub fn events_from_summary(
    value: concordium_rust_sdk::types::BlockItemSummaryDetails,
) -> anyhow::Result<Vec<Event>> {
    use concordium_rust_sdk::types::{AccountTransactionEffects, BlockItemSummaryDetails};
    let events = match value {
        BlockItemSummaryDetails::AccountTransaction(details) => match details.effects {
            AccountTransactionEffects::None {
                ..
            } => {
                anyhow::bail!("Transaction was rejected")
            }
            AccountTransactionEffects::ModuleDeployed {
                module_ref,
            } => {
                vec![Event::ContractModuleDeployed(smart_contracts::ContractModuleDeployed {
                    module_ref: module_ref.to_string(),
                })]
            }
            AccountTransactionEffects::ContractInitialized {
                data,
            } => {
                vec![Event::ContractInitialized(smart_contracts::ContractInitialized {
                    module_ref:        data.origin_ref.to_string(),
                    contract_address:  data.address.into(),
                    amount:            i64::try_from(data.amount.micro_ccd)?,
                    init_name:         data.init_name.to_string(),
                    version:           data.contract_version.into(),
                    contract_logs_raw: data.events.iter().map(|e| e.as_ref().to_vec()).collect(),
                })]
            }
            AccountTransactionEffects::ContractUpdateIssued {
                effects,
            } => {
                use concordium_rust_sdk::types::ContractTraceElement;
                effects
                    .into_iter()
                    .map(|effect| match effect {
                        ContractTraceElement::Updated {
                            data,
                        } => Ok(Event::ContractUpdated(smart_contracts::ContractUpdated {
                            contract_address:  data.address.into(),
                            instigator:        data.instigator.into(),
                            amount:            data.amount.micro_ccd().try_into()?,
                            receive_name:      data.receive_name.to_string(),
                            version:           data.contract_version.into(),
                            input_parameter:   data.message.as_ref().to_vec(),
                            contract_logs_raw: data
                                .events
                                .iter()
                                .map(|e| e.as_ref().to_vec())
                                .collect(),
                        })),
                        ContractTraceElement::Transferred {
                            from,
                            amount,
                            to,
                        } => Ok(Event::Transferred(transfers::Transferred {
                            amount: amount.micro_ccd().try_into()?,
                            from:   Address::ContractAddress(from.into()),
                            to:     to.into(),
                        })),
                        ContractTraceElement::Interrupted {
                            address,
                            events,
                        } => Ok(Event::ContractInterrupted(smart_contracts::ContractInterrupted {
                            contract_address:  address.into(),
                            contract_logs_raw: events.iter().map(|e| e.as_ref().to_vec()).collect(),
                        })),
                        ContractTraceElement::Resumed {
                            address,
                            success,
                        } => Ok(Event::ContractResumed(smart_contracts::ContractResumed {
                            contract_address: address.into(),
                            success,
                        })),
                        ContractTraceElement::Upgraded {
                            address,
                            from,
                            to,
                        } => Ok(Event::ContractUpgraded(smart_contracts::ContractUpgraded {
                            contract_address: address.into(),
                            from:             from.to_string(),
                            to:               to.to_string(),
                        })),
                    })
                    .collect::<anyhow::Result<Vec<_>>>()?
            }
            AccountTransactionEffects::AccountTransfer {
                amount,
                to,
            } => {
                vec![Event::Transferred(transfers::Transferred {
                    amount: i64::try_from(amount.micro_ccd)?,
                    from:   Address::AccountAddress(details.sender.into()),
                    to:     to.into(),
                })]
            }
            AccountTransactionEffects::AccountTransferWithMemo {
                amount,
                to,
                memo,
            } => {
                vec![
                    Event::Transferred(transfers::Transferred {
                        amount: i64::try_from(amount.micro_ccd)?,
                        from:   Address::AccountAddress(details.sender.into()),
                        to:     to.into(),
                    }),
                    Event::TransferMemo(memo.into()),
                ]
            }
            AccountTransactionEffects::BakerAdded {
                data,
            } => {
                vec![Event::BakerAdded(baker::BakerAdded {
                    staked_amount:    data.stake.micro_ccd.try_into()?,
                    restake_earnings: data.restake_earnings,
                    baker_id:         data.keys_event.baker_id.id.index.try_into()?,
                    sign_key:         serde_json::to_string(&data.keys_event.sign_key)?,
                    election_key:     serde_json::to_string(&data.keys_event.election_key)?,
                    aggregation_key:  serde_json::to_string(&data.keys_event.aggregation_key)?,
                })]
            }
            AccountTransactionEffects::BakerRemoved {
                baker_id,
            } => {
                vec![Event::BakerRemoved(baker::BakerRemoved {
                    baker_id: baker_id.id.index.try_into()?,
                })]
            }
            AccountTransactionEffects::BakerStakeUpdated {
                data,
            } => {
                if let Some(data) = data {
                    if data.increased {
                        vec![Event::BakerStakeIncreased(baker::BakerStakeIncreased {
                            baker_id:          data.baker_id.id.index.try_into()?,
                            new_staked_amount: data.new_stake.micro_ccd.try_into()?,
                        })]
                    } else {
                        vec![Event::BakerStakeDecreased(baker::BakerStakeDecreased {
                            baker_id:          data.baker_id.id.index.try_into()?,
                            new_staked_amount: data.new_stake.micro_ccd.try_into()?,
                        })]
                    }
                } else {
                    Vec::new()
                }
            }
            AccountTransactionEffects::BakerRestakeEarningsUpdated {
                baker_id,
                restake_earnings,
            } => {
                vec![Event::BakerSetRestakeEarnings(baker::BakerSetRestakeEarnings {
                    baker_id: baker_id.id.index.try_into()?,
                    restake_earnings,
                })]
            }
            AccountTransactionEffects::BakerKeysUpdated {
                data,
            } => {
                vec![Event::BakerKeysUpdated(baker::BakerKeysUpdated {
                    baker_id:        data.baker_id.id.index.try_into()?,
                    sign_key:        serde_json::to_string(&data.sign_key)?,
                    election_key:    serde_json::to_string(&data.election_key)?,
                    aggregation_key: serde_json::to_string(&data.aggregation_key)?,
                })]
            }
            AccountTransactionEffects::EncryptedAmountTransferred {
                removed,
                added,
            } => {
                vec![
                    Event::EncryptedAmountsRemoved((*removed).try_into()?),
                    Event::NewEncryptedAmount((*added).try_into()?),
                ]
            }
            AccountTransactionEffects::EncryptedAmountTransferredWithMemo {
                removed,
                added,
                memo,
            } => {
                vec![
                    Event::EncryptedAmountsRemoved((*removed).try_into()?),
                    Event::NewEncryptedAmount((*added).try_into()?),
                    Event::TransferMemo(memo.into()),
                ]
            }
            AccountTransactionEffects::TransferredToEncrypted {
                data,
            } => {
                vec![Event::EncryptedSelfAmountAdded(transfers::EncryptedSelfAmountAdded {
                    account_address:      data.account.into(),
                    new_encrypted_amount: serde_json::to_string(&data.new_amount)?,
                    amount:               data.amount.micro_ccd.try_into()?,
                })]
            }
            AccountTransactionEffects::TransferredToPublic {
                removed,
                amount,
            } => {
                vec![
                    Event::EncryptedAmountsRemoved((*removed).try_into()?),
                    Event::AmountAddedByDecryption(transfers::AmountAddedByDecryption {
                        amount:          amount.micro_ccd().try_into()?,
                        account_address: details.sender.into(),
                    }),
                ]
            }
            AccountTransactionEffects::TransferredWithSchedule {
                to,
                amount,
            } => {
                vec![Event::TransferredWithSchedule(transfers::TransferredWithSchedule {
                    from_account_address: details.sender.into(),
                    to_account_address:   to.into(),
                    total_amount:         amount
                        .into_iter()
                        .map(|(_, amount)| amount.micro_ccd())
                        .sum::<u64>()
                        .try_into()?,
                })]
            }
            AccountTransactionEffects::TransferredWithScheduleAndMemo {
                to,
                amount,
                memo,
            } => {
                vec![
                    Event::TransferredWithSchedule(transfers::TransferredWithSchedule {
                        from_account_address: details.sender.into(),
                        to_account_address:   to.into(),
                        total_amount:         amount
                            .into_iter()
                            .map(|(_, amount)| amount.micro_ccd())
                            .sum::<u64>()
                            .try_into()?,
                    }),
                    Event::TransferMemo(memo.into()),
                ]
            }
            AccountTransactionEffects::CredentialKeysUpdated {
                cred_id,
            } => {
                vec![Event::CredentialKeysUpdated(credentials::CredentialKeysUpdated {
                    cred_id: cred_id.to_string(),
                })]
            }
            AccountTransactionEffects::CredentialsUpdated {
                new_cred_ids,
                removed_cred_ids,
                new_threshold,
            } => {
                vec![Event::CredentialsUpdated(credentials::CredentialsUpdated {
                    account_address:  details.sender.into(),
                    new_cred_ids:     new_cred_ids
                        .into_iter()
                        .map(|cred| cred.to_string())
                        .collect(),
                    removed_cred_ids: removed_cred_ids
                        .into_iter()
                        .map(|cred| cred.to_string())
                        .collect(),
                    new_threshold:    Byte(u8::from(new_threshold)),
                })]
            }
            AccountTransactionEffects::DataRegistered {
                data,
            } => {
                vec![Event::DataRegistered(DataRegistered {
                    data_as_hex: hex::encode(data.as_ref()),
                })]
            }
            AccountTransactionEffects::BakerConfigured {
                data,
            } => data
                .into_iter()
                .map(|baker_event| {
                    use concordium_rust_sdk::types::BakerEvent;
                    match baker_event {
                        BakerEvent::BakerAdded {
                            data,
                        } => Ok(Event::BakerAdded(baker::BakerAdded {
                            staked_amount:    data.stake.micro_ccd.try_into()?,
                            restake_earnings: data.restake_earnings,
                            baker_id:         data.keys_event.baker_id.id.index.try_into()?,
                            sign_key:         serde_json::to_string(&data.keys_event.sign_key)?,
                            election_key:     serde_json::to_string(&data.keys_event.election_key)?,
                            aggregation_key:  serde_json::to_string(
                                &data.keys_event.aggregation_key,
                            )?,
                        })),
                        BakerEvent::BakerRemoved {
                            baker_id,
                        } => Ok(Event::BakerRemoved(baker::BakerRemoved {
                            baker_id: baker_id.id.index.try_into()?,
                        })),
                        BakerEvent::BakerStakeIncreased {
                            baker_id,
                            new_stake,
                        } => Ok(Event::BakerStakeIncreased(baker::BakerStakeIncreased {
                            baker_id:          baker_id.id.index.try_into()?,
                            new_staked_amount: new_stake.micro_ccd.try_into()?,
                        })),
                        BakerEvent::BakerStakeDecreased {
                            baker_id,
                            new_stake,
                        } => Ok(Event::BakerStakeDecreased(baker::BakerStakeDecreased {
                            baker_id:          baker_id.id.index.try_into()?,
                            new_staked_amount: new_stake.micro_ccd.try_into()?,
                        })),
                        BakerEvent::BakerRestakeEarningsUpdated {
                            baker_id,
                            restake_earnings,
                        } => Ok(Event::BakerSetRestakeEarnings(baker::BakerSetRestakeEarnings {
                            baker_id: baker_id.id.index.try_into()?,
                            restake_earnings,
                        })),
                        BakerEvent::BakerKeysUpdated {
                            data,
                        } => Ok(Event::BakerKeysUpdated(baker::BakerKeysUpdated {
                            baker_id:        data.baker_id.id.index.try_into()?,
                            sign_key:        serde_json::to_string(&data.sign_key)?,
                            election_key:    serde_json::to_string(&data.election_key)?,
                            aggregation_key: serde_json::to_string(&data.aggregation_key)?,
                        })),
                        BakerEvent::BakerSetOpenStatus {
                            baker_id,
                            open_status,
                        } => Ok(Event::BakerSetOpenStatus(baker::BakerSetOpenStatus {
                            baker_id:        baker_id.id.index.try_into()?,
                            account_address: details.sender.into(),
                            open_status:     open_status.into(),
                        })),
                        BakerEvent::BakerSetMetadataURL {
                            baker_id,
                            metadata_url,
                        } => Ok(Event::BakerSetMetadataURL(baker::BakerSetMetadataURL {
                            baker_id:        baker_id.id.index.try_into()?,
                            account_address: details.sender.into(),
                            metadata_url:    metadata_url.into(),
                        })),
                        BakerEvent::BakerSetTransactionFeeCommission {
                            baker_id,
                            transaction_fee_commission,
                        } => Ok(Event::BakerSetTransactionFeeCommission(
                            baker::BakerSetTransactionFeeCommission {
                                baker_id:                   baker_id.id.index.try_into()?,
                                account_address:            details.sender.into(),
                                transaction_fee_commission: transaction_fee_commission.into(),
                            },
                        )),
                        BakerEvent::BakerSetBakingRewardCommission {
                            baker_id,
                            baking_reward_commission,
                        } => Ok(Event::BakerSetBakingRewardCommission(
                            baker::BakerSetBakingRewardCommission {
                                baker_id:                 baker_id.id.index.try_into()?,
                                account_address:          details.sender.into(),
                                baking_reward_commission: baking_reward_commission.into(),
                            },
                        )),
                        BakerEvent::BakerSetFinalizationRewardCommission {
                            baker_id,
                            finalization_reward_commission,
                        } => Ok(Event::BakerSetFinalizationRewardCommission(
                            baker::BakerSetFinalizationRewardCommission {
                                baker_id: baker_id.id.index.try_into()?,
                                account_address: details.sender.into(),
                                finalization_reward_commission: finalization_reward_commission
                                    .into(),
                            },
                        )),
                        BakerEvent::DelegationRemoved {
                            ..
                        } => {
                            unimplemented!()
                        }
                    }
                })
                .collect::<anyhow::Result<Vec<Event>>>()?,
            AccountTransactionEffects::DelegationConfigured {
                data,
            } => {
                use concordium_rust_sdk::types::DelegationEvent;
                data.into_iter()
                    .map(|event| match event {
                        DelegationEvent::DelegationStakeIncreased {
                            delegator_id,
                            new_stake,
                        } => Ok(Event::DelegationStakeIncreased(
                            delegation::DelegationStakeIncreased {
                                delegator_id:      delegator_id.id.index.try_into()?,
                                account_address:   details.sender.into(),
                                new_staked_amount: new_stake.micro_ccd().try_into()?,
                            },
                        )),
                        DelegationEvent::DelegationStakeDecreased {
                            delegator_id,
                            new_stake,
                        } => Ok(Event::DelegationStakeDecreased(
                            delegation::DelegationStakeDecreased {
                                delegator_id:      delegator_id.id.index.try_into()?,
                                account_address:   details.sender.into(),
                                new_staked_amount: new_stake.micro_ccd().try_into()?,
                            },
                        )),
                        DelegationEvent::DelegationSetRestakeEarnings {
                            delegator_id,
                            restake_earnings,
                        } => Ok(Event::DelegationSetRestakeEarnings(
                            delegation::DelegationSetRestakeEarnings {
                                delegator_id: delegator_id.id.index.try_into()?,
                                account_address: details.sender.into(),
                                restake_earnings,
                            },
                        )),
                        DelegationEvent::DelegationSetDelegationTarget {
                            delegator_id,
                            delegation_target,
                        } => Ok(Event::DelegationSetDelegationTarget(
                            delegation::DelegationSetDelegationTarget {
                                delegator_id:      delegator_id.id.index.try_into()?,
                                account_address:   details.sender.into(),
                                delegation_target: delegation_target.try_into()?,
                            },
                        )),
                        DelegationEvent::DelegationAdded {
                            delegator_id,
                        } => Ok(Event::DelegationAdded(delegation::DelegationAdded {
                            delegator_id:    delegator_id.id.index.try_into()?,
                            account_address: details.sender.into(),
                        })),
                        DelegationEvent::DelegationRemoved {
                            delegator_id,
                        } => Ok(Event::DelegationRemoved(delegation::DelegationRemoved {
                            delegator_id:    delegator_id.id.index.try_into()?,
                            account_address: details.sender.into(),
                        })),
                        DelegationEvent::BakerRemoved {
                            baker_id,
                        } => Ok(Event::BakerRemoved(baker::BakerRemoved {
                            baker_id: baker_id.id.index.try_into()?,
                        })),
                    })
                    .collect::<anyhow::Result<Vec<_>>>()?
            }
        },
        BlockItemSummaryDetails::AccountCreation(details) => {
            vec![Event::AccountCreated(credentials::AccountCreated {
                account_address: details.address.into(),
            })]
        }
        BlockItemSummaryDetails::Update(details) => {
            vec![Event::ChainUpdateEnqueued(chain_update::ChainUpdateEnqueued {
                effective_time: DateTime::from_timestamp(
                    details.effective_time.seconds.try_into()?,
                    0,
                )
                .context("Failed to parse effective time")?,
                payload:        true, // placeholder
            })]
        }
    };
    Ok(events)
}