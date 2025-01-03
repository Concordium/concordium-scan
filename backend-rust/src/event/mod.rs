use async_graphql::Union;

#[derive(Union, serde::Serialize, serde::Deserialize)]
pub enum Event {
    /// A transfer of CCD. Can be either from an account or a smart contract
    /// instance, but the receiver in this event is always an account.
    Transferred(Transferred),
    AccountCreated(AccountCreated),
    AmountAddedByDecryption(AmountAddedByDecryption),
    BakerAdded(BakerAdded),
    BakerKeysUpdated(BakerKeysUpdated),
    BakerRemoved(BakerRemoved),
    BakerSetRestakeEarnings(BakerSetRestakeEarnings),
    BakerStakeDecreased(BakerStakeDecreased),
    BakerStakeIncreased(BakerStakeIncreased),
    ContractInitialized(ContractInitialized),
    ContractModuleDeployed(ContractModuleDeployed),
    ContractUpdated(ContractUpdated),
    ContractCall(ContractCall),
    CredentialDeployed(CredentialDeployed),
    CredentialKeysUpdated(CredentialKeysUpdated),
    CredentialsUpdated(CredentialsUpdated),
    DataRegistered(DataRegistered),
    EncryptedAmountsRemoved(EncryptedAmountsRemoved),
    EncryptedSelfAmountAdded(EncryptedSelfAmountAdded),
    NewEncryptedAmount(NewEncryptedAmount),
    TransferMemo(TransferMemo),
    TransferredWithSchedule(TransferredWithSchedule),
    ChainUpdateEnqueued(ChainUpdateEnqueued),
    ContractInterrupted(ContractInterrupted),
    ContractResumed(ContractResumed),
    ContractUpgraded(ContractUpgraded),
    BakerSetOpenStatus(BakerSetOpenStatus),
    BakerSetMetadataURL(BakerSetMetadataURL),
    BakerSetTransactionFeeCommission(BakerSetTransactionFeeCommission),
    BakerSetBakingRewardCommission(BakerSetBakingRewardCommission),
    BakerSetFinalizationRewardCommission(BakerSetFinalizationRewardCommission),
    DelegationAdded(DelegationAdded),
    DelegationRemoved(DelegationRemoved),
    DelegationStakeIncreased(DelegationStakeIncreased),
    DelegationStakeDecreased(DelegationStakeDecreased),
    DelegationSetRestakeEarnings(DelegationSetRestakeEarnings),
    DelegationSetDelegationTarget(DelegationSetDelegationTarget),
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
                vec![Event::ContractModuleDeployed(ContractModuleDeployed {
                    module_ref: module_ref.to_string(),
                })]
            }
            AccountTransactionEffects::ContractInitialized {
                data,
            } => {
                vec![Event::ContractInitialized(ContractInitialized {
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
                        } => Ok(Event::ContractUpdated(ContractUpdated {
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
                        } => Ok(Event::Transferred(Transferred {
                            amount: amount.micro_ccd().try_into()?,
                            from:   Address::ContractAddress(from.into()),
                            to:     to.into(),
                        })),
                        ContractTraceElement::Interrupted {
                            address,
                            events,
                        } => Ok(Event::ContractInterrupted(ContractInterrupted {
                            contract_address:  address.into(),
                            contract_logs_raw: events.iter().map(|e| e.as_ref().to_vec()).collect(),
                        })),
                        ContractTraceElement::Resumed {
                            address,
                            success,
                        } => Ok(Event::ContractResumed(ContractResumed {
                            contract_address: address.into(),
                            success,
                        })),
                        ContractTraceElement::Upgraded {
                            address,
                            from,
                            to,
                        } => Ok(Event::ContractUpgraded(ContractUpgraded {
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
                vec![Event::Transferred(Transferred {
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
                    Event::Transferred(Transferred {
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
                vec![Event::BakerAdded(BakerAdded {
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
                vec![Event::BakerRemoved(BakerRemoved {
                    baker_id: baker_id.id.index.try_into()?,
                })]
            }
            AccountTransactionEffects::BakerStakeUpdated {
                data,
            } => {
                if let Some(data) = data {
                    if data.increased {
                        vec![Event::BakerStakeIncreased(BakerStakeIncreased {
                            baker_id:          data.baker_id.id.index.try_into()?,
                            new_staked_amount: data.new_stake.micro_ccd.try_into()?,
                        })]
                    } else {
                        vec![Event::BakerStakeDecreased(BakerStakeDecreased {
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
                vec![Event::BakerSetRestakeEarnings(BakerSetRestakeEarnings {
                    baker_id: baker_id.id.index.try_into()?,
                    restake_earnings,
                })]
            }
            AccountTransactionEffects::BakerKeysUpdated {
                data,
            } => {
                vec![Event::BakerKeysUpdated(BakerKeysUpdated {
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
                vec![Event::EncryptedSelfAmountAdded(EncryptedSelfAmountAdded {
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
                    Event::AmountAddedByDecryption(AmountAddedByDecryption {
                        amount:          amount.micro_ccd().try_into()?,
                        account_address: details.sender.into(),
                    }),
                ]
            }
            AccountTransactionEffects::TransferredWithSchedule {
                to,
                amount,
            } => {
                vec![Event::TransferredWithSchedule(TransferredWithSchedule {
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
                    Event::TransferredWithSchedule(TransferredWithSchedule {
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
                vec![Event::CredentialKeysUpdated(CredentialKeysUpdated {
                    cred_id: cred_id.to_string(),
                })]
            }
            AccountTransactionEffects::CredentialsUpdated {
                new_cred_ids,
                removed_cred_ids,
                new_threshold,
            } => {
                vec![Event::CredentialsUpdated(CredentialsUpdated {
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
                        } => Ok(Event::BakerAdded(BakerAdded {
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
                        } => Ok(Event::BakerRemoved(BakerRemoved {
                            baker_id: baker_id.id.index.try_into()?,
                        })),
                        BakerEvent::BakerStakeIncreased {
                            baker_id,
                            new_stake,
                        } => Ok(Event::BakerStakeIncreased(BakerStakeIncreased {
                            baker_id:          baker_id.id.index.try_into()?,
                            new_staked_amount: new_stake.micro_ccd.try_into()?,
                        })),
                        BakerEvent::BakerStakeDecreased {
                            baker_id,
                            new_stake,
                        } => Ok(Event::BakerStakeDecreased(BakerStakeDecreased {
                            baker_id:          baker_id.id.index.try_into()?,
                            new_staked_amount: new_stake.micro_ccd.try_into()?,
                        })),
                        BakerEvent::BakerRestakeEarningsUpdated {
                            baker_id,
                            restake_earnings,
                        } => Ok(Event::BakerSetRestakeEarnings(BakerSetRestakeEarnings {
                            baker_id: baker_id.id.index.try_into()?,
                            restake_earnings,
                        })),
                        BakerEvent::BakerKeysUpdated {
                            data,
                        } => Ok(Event::BakerKeysUpdated(BakerKeysUpdated {
                            baker_id:        data.baker_id.id.index.try_into()?,
                            sign_key:        serde_json::to_string(&data.sign_key)?,
                            election_key:    serde_json::to_string(&data.election_key)?,
                            aggregation_key: serde_json::to_string(&data.aggregation_key)?,
                        })),
                        BakerEvent::BakerSetOpenStatus {
                            baker_id,
                            open_status,
                        } => Ok(Event::BakerSetOpenStatus(BakerSetOpenStatus {
                            baker_id:        baker_id.id.index.try_into()?,
                            account_address: details.sender.into(),
                            open_status:     open_status.into(),
                        })),
                        BakerEvent::BakerSetMetadataURL {
                            baker_id,
                            metadata_url,
                        } => Ok(Event::BakerSetMetadataURL(BakerSetMetadataURL {
                            baker_id:        baker_id.id.index.try_into()?,
                            account_address: details.sender.into(),
                            metadata_url:    metadata_url.into(),
                        })),
                        BakerEvent::BakerSetTransactionFeeCommission {
                            baker_id,
                            transaction_fee_commission,
                        } => Ok(Event::BakerSetTransactionFeeCommission(
                            BakerSetTransactionFeeCommission {
                                baker_id:                   baker_id.id.index.try_into()?,
                                account_address:            details.sender.into(),
                                transaction_fee_commission: transaction_fee_commission.into(),
                            },
                        )),
                        BakerEvent::BakerSetBakingRewardCommission {
                            baker_id,
                            baking_reward_commission,
                        } => Ok(Event::BakerSetBakingRewardCommission(
                            BakerSetBakingRewardCommission {
                                baker_id:                 baker_id.id.index.try_into()?,
                                account_address:          details.sender.into(),
                                baking_reward_commission: baking_reward_commission.into(),
                            },
                        )),
                        BakerEvent::BakerSetFinalizationRewardCommission {
                            baker_id,
                            finalization_reward_commission,
                        } => Ok(Event::BakerSetFinalizationRewardCommission(
                            BakerSetFinalizationRewardCommission {
                                baker_id: baker_id.id.index.try_into()?,
                                account_address: details.sender.into(),
                                finalization_reward_commission: finalization_reward_commission
                                    .into(),
                            },
                        )),
                        BakerEvent::DelegationRemoved {
                            delegator_id,
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
                        } => Ok(Event::DelegationStakeIncreased(DelegationStakeIncreased {
                            delegator_id:      delegator_id.id.index.try_into()?,
                            account_address:   details.sender.into(),
                            new_staked_amount: new_stake.micro_ccd().try_into()?,
                        })),
                        DelegationEvent::DelegationStakeDecreased {
                            delegator_id,
                            new_stake,
                        } => Ok(Event::DelegationStakeDecreased(DelegationStakeDecreased {
                            delegator_id:      delegator_id.id.index.try_into()?,
                            account_address:   details.sender.into(),
                            new_staked_amount: new_stake.micro_ccd().try_into()?,
                        })),
                        DelegationEvent::DelegationSetRestakeEarnings {
                            delegator_id,
                            restake_earnings,
                        } => {
                            Ok(Event::DelegationSetRestakeEarnings(DelegationSetRestakeEarnings {
                                delegator_id: delegator_id.id.index.try_into()?,
                                account_address: details.sender.into(),
                                restake_earnings,
                            }))
                        }
                        DelegationEvent::DelegationSetDelegationTarget {
                            delegator_id,
                            delegation_target,
                        } => Ok(Event::DelegationSetDelegationTarget(
                            DelegationSetDelegationTarget {
                                delegator_id:      delegator_id.id.index.try_into()?,
                                account_address:   details.sender.into(),
                                delegation_target: delegation_target.try_into()?,
                            },
                        )),
                        DelegationEvent::DelegationAdded {
                            delegator_id,
                        } => Ok(Event::DelegationAdded(DelegationAdded {
                            delegator_id:    delegator_id.id.index.try_into()?,
                            account_address: details.sender.into(),
                        })),
                        DelegationEvent::DelegationRemoved {
                            delegator_id,
                        } => Ok(Event::DelegationRemoved(DelegationRemoved {
                            delegator_id:    delegator_id.id.index.try_into()?,
                            account_address: details.sender.into(),
                        })),
                        DelegationEvent::BakerRemoved {
                            baker_id,
                        } => Ok(Event::BakerRemoved(BakerRemoved {
                            baker_id: baker_id.id.index.try_into()?,
                        })),
                    })
                    .collect::<anyhow::Result<Vec<_>>>()?
            }
        },
        BlockItemSummaryDetails::AccountCreation(details) => {
            vec![Event::AccountCreated(AccountCreated {
                account_address: details.address.into(),
            })]
        }
        BlockItemSummaryDetails::Update(details) => {
            vec![Event::ChainUpdateEnqueued(ChainUpdateEnqueued {
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

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct Transferred {
    amount: Amount,
    from:   Address,
    to:     AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct AccountCreated {
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct AmountAddedByDecryption {
    amount:          Amount,
    account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerAdded {
    staked_amount:    Amount,
    restake_earnings: bool,
    baker_id:         BakerId,
    sign_key:         String,
    election_key:     String,
    aggregation_key:  String,
}
#[ComplexObject]
impl BakerAdded {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo_api!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerKeysUpdated {
    baker_id:        BakerId,
    sign_key:        String,
    election_key:    String,
    aggregation_key: String,
}
#[ComplexObject]
impl BakerKeysUpdated {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo_api!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerRemoved {
    baker_id: BakerId,
}
#[ComplexObject]
impl BakerRemoved {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo_api!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerSetRestakeEarnings {
    baker_id:         BakerId,
    restake_earnings: bool,
}
#[ComplexObject]
impl BakerSetRestakeEarnings {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo_api!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerStakeDecreased {
    baker_id:          BakerId,
    new_staked_amount: Amount,
}
#[ComplexObject]
impl BakerStakeDecreased {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo_api!()
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct BakerStakeIncreased {
    baker_id:          BakerId,
    new_staked_amount: Amount,
}
#[ComplexObject]
impl BakerStakeIncreased {
    async fn account_address<'a>(&self, _ctx: &Context<'a>) -> ApiResult<AccountAddress> {
        todo_api!()
    }
}

