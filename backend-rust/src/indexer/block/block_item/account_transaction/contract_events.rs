//! This module contains information computed for smart contract instance
//! related events in an account transaction during the concurrent preprocessing
//! and the logic for how to do the sequential processing into the database.

use super::module_events::PreparedModuleLinkAction;
use crate::{
    graphql_api::AccountStatementEntryType,
    indexer::{
        block_preprocessor::BlockData, db::update_account_balance::PreparedUpdateAccountBalance,
        ensure_affected_rows::EnsureAffectedRows,
    },
    transaction_event::{
        smart_contracts::ModuleReferenceContractLinkAction, CisBurnEvent, CisEvent, CisMintEvent,
        CisTokenMetadataEvent, CisTransferEvent,
    },
};
use anyhow::Context;
use bigdecimal::BigDecimal;
use concordium_rust_sdk::{
    cis0, cis2,
    common::types::Amount,
    id::types::AccountAddress,
    types::{
        self as sdk_types, AbsoluteBlockHeight, ContractAddress, ContractInitializedEvent,
        ContractTraceElement,
    },
    v2,
};
use futures::future::join_all;

#[derive(Debug)]
pub struct PreparedContractInitialized {
    index:                i64,
    sub_index:            i64,
    module_reference:     String,
    name:                 String,
    amount:               i64,
    module_link_event:    PreparedModuleLinkAction,
    transfer_to_contract: PreparedUpdateAccountBalance,
    cis2_token_events:    Vec<CisEvent>,
}

impl PreparedContractInitialized {
    pub async fn prepare(
        node_client: &mut v2::Client,
        data: &BlockData,
        event: &ContractInitializedEvent,
        sender_account: &AccountAddress,
    ) -> anyhow::Result<Self> {
        let contract_address = event.address;
        let index = i64::try_from(event.address.index)?;
        let sub_index = i64::try_from(event.address.subindex)?;
        let module_reference = event.origin_ref;
        // We remove the `init_` prefix from the name to get the contract name.
        let name = event.init_name.as_contract_name().contract_name().to_string();
        let amount = i64::try_from(event.amount.micro_ccd())?;

        let module_link_event = PreparedModuleLinkAction::prepare(
            module_reference,
            event.address,
            ModuleReferenceContractLinkAction::Added,
        )?;
        let transfer_to_contract = PreparedUpdateAccountBalance::prepare(
            sender_account,
            -amount,
            data.block_info.block_height,
            AccountStatementEntryType::TransferOut,
        )?;

        // To track CIS2 tokens (e.g., token balances, total supply, token metadata
        // URLs), we gather the CIS2 events here. We check if logged contract
        // events can be parsed as CIS2 events. In addition, we check if the
        // contract supports the `CIS2` standard by calling the on-chain
        // `supports` endpoint before considering the CIS2 events valid.
        //
        // There are two edge cases that the indexer would not identify a CIS2 event
        // correctly. Nonetheless, to avoid complexity it was deemed acceptable
        // behavior.
        // - Edge case 1: A contract code upgrades and no longer
        // supports CIS2 then logging a CIS2-like event within the same block.
        // - Edge case 2: A contract logs a CIS2-like event and then upgrades to add
        // support for CIS2 in the same block.
        //
        // There are three chain events (`ContractInitializedEvent`,
        // `ContractInterruptedEvent` and `ContractUpdatedEvent`) that can generate
        // `contract_logs`. CIS2 events logged by the first chain event are
        // handled here while CIS2 events logged in the `ContractInterruptedEvent` and
        // `ContractUpdatedEvent` are handled at its corresponding
        // transaction type.
        let potential_cis2_events =
            event.events.iter().filter_map(|log| log.try_into().ok()).collect::<Vec<_>>();

        // If the vector `potential_cis2_events` is not empty, we verify that the smart
        // contract supports the CIS2 standard before accepting the events as
        // valid.
        let cis2_token_events = if potential_cis2_events.is_empty() {
            vec![]
        } else {
            let supports_cis2 = cis0::supports(
                node_client,
                &v2::BlockIdentifier::AbsoluteHeight(data.block_info.block_height),
                contract_address,
                event.init_name.as_contract_name(),
                cis0::StandardIdentifier::CIS2,
            )
            .await
            .is_ok_and(|r| r.response.is_support());

            if supports_cis2 {
                potential_cis2_events.into_iter().map(|event: cis2::Event| event.into()).collect()
            } else {
                // If contract does not support `CIS2`, don't consider the events as CIS2
                // events.
                vec![]
            }
        };

        Ok(Self {
            index,
            sub_index,
            module_reference: module_reference.into(),
            name,
            amount,
            module_link_event,
            transfer_to_contract,
            cis2_token_events,
        })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "INSERT INTO contracts (
                index,
                sub_index,
                module_reference,
                name,
                amount,
                transaction_index
            ) VALUES ($1, $2, $3, $4, $5, $6)",
            self.index,
            self.sub_index,
            self.module_reference,
            self.name,
            self.amount,
            transaction_index
        )
        .execute(tx.as_mut())
        .await
        .with_context(|| format!("Failed inserting new to 'contracts' table: {:?}", self))?;

        self.module_link_event
            .save(tx, transaction_index)
            .await
            .context("Failed linking new contract to module")?;

        for log in self.cis2_token_events.iter() {
            process_cis2_token_event(log, self.index, self.sub_index, transaction_index, tx)
                .await
                .context("Failed processing a CIS-2 event")?
        }
        self.transfer_to_contract.save(tx, Some(transaction_index)).await?;
        Ok(())
    }
}

#[derive(Debug)]
pub struct PreparedContractUpdates {
    /// Additional events to track from the trace elements in the update
    /// transaction.
    trace_elements: Vec<PreparedTraceElement>,
}

impl PreparedContractUpdates {
    pub async fn prepare(
        node_client: &mut v2::Client,
        data: &BlockData,
        events: &[ContractTraceElement],
    ) -> anyhow::Result<Self> {
        let trace_elements =
            join_all(events.iter().enumerate().map(|(trace_element_index, effect)| {
                PreparedTraceElement::prepare(
                    node_client.clone(),
                    data,
                    effect,
                    trace_element_index,
                )
            }))
            .await
            .into_iter()
            .collect::<Result<Vec<_>, anyhow::Error>>()?;
        Ok(Self {
            trace_elements,
        })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        for elm in &self.trace_elements {
            elm.save(tx, transaction_index).await.with_context(|| {
                format!(
                    "Failed processing contract update trace element with index {} related to \
                     contract: <{},{}>",
                    elm.trace_element_index, elm.contract_index, elm.contract_sub_index
                )
            })?;
        }
        Ok(())
    }
}

#[derive(Debug)]
struct PreparedTraceElement {
    height:              i64,
    contract_index:      i64,
    contract_sub_index:  i64,
    trace_element_index: i64,
    cis2_token_events:   Vec<CisEvent>,
    trace_event:         PreparedContractTraceEvent,
}

impl PreparedTraceElement {
    async fn prepare(
        mut node_client: v2::Client,
        data: &BlockData,
        event: &ContractTraceElement,
        trace_element_index: usize,
    ) -> anyhow::Result<Self> {
        let contract_address = event.affected_address();

        let trace_element_index = trace_element_index.try_into()?;
        let height = data.finalized_block_info.height;
        let index = i64::try_from(contract_address.index)?;
        let sub_index = i64::try_from(contract_address.subindex)?;

        let trace_event = match event {
            ContractTraceElement::Updated {
                data: update,
            } => PreparedContractTraceEvent::Update(PreparedTraceEventUpdate::prepare(
                update.instigator,
                update.address,
                update.amount,
                data.finalized_block_info.height,
            )?),
            ContractTraceElement::Transferred {
                from,
                amount,
                to,
            } => PreparedContractTraceEvent::Transfer(PreparedTraceEventTransfer::prepare(
                *from,
                to,
                *amount,
                data.finalized_block_info.height,
            )?),
            ContractTraceElement::Interrupted {
                ..
            }
            | ContractTraceElement::Resumed {
                ..
            } => PreparedContractTraceEvent::NoEvent,
            ContractTraceElement::Upgraded {
                address,
                from,
                to,
            } => PreparedContractTraceEvent::Upgrade(PreparedTraceEventUpgrade::prepare(
                *address, *from, *to,
            )?),
        };

        // To track CIS2 tokens (e.g., token balances, total supply, token metadata
        // URLs), we gather the CIS2 events here. We check if logged contract
        // events can be parsed as CIS2 events. In addition, we check if the
        // contract supports the `CIS2` standard by calling the on-chain
        // `supports` endpoint before considering the CIS2 events valid.
        //
        // There are two edge cases that the indexer would not identify a CIS2 event
        // correctly. Nonetheless, to avoid complexity it was deemed acceptable
        // behavior.
        // - Edge case 1: A contract code upgrades and no longer
        // supports CIS2 then logging a CIS2-like event within the same block.
        // - Edge case 2: A contract logs a CIS2-like event and then upgrades to add
        // support for CIS2 in the same block.
        //
        // There are three chain events (`ContractInitializedEvent`,
        // `ContractInterruptedEvent` and `ContractUpdatedEvent`) that can generate
        // `contract_logs`. CIS2 events logged by the last two chain events are
        // handled here while CIS2 events logged in the
        // `ContractInitializedEvent` are handled at its corresponding
        // transaction type.
        let potential_cis2_events = match event {
            ContractTraceElement::Updated {
                data,
            } => data.events.iter().filter_map(|log| log.try_into().ok()).collect::<Vec<_>>(),
            ContractTraceElement::Transferred {
                ..
            } => vec![],
            ContractTraceElement::Interrupted {
                events,
                ..
            } => events.iter().filter_map(|log| log.try_into().ok()).collect::<Vec<_>>(),
            ContractTraceElement::Resumed {
                ..
            } => vec![],
            ContractTraceElement::Upgraded {
                ..
            } => vec![],
        };

        // If the vector `potential_cis2_events` is not empty, we verify that the smart
        // contract supports the CIS2 standard before accepting the events as
        // valid.
        let cis2_token_events = if potential_cis2_events.is_empty() {
            vec![]
        } else {
            let contract_info = node_client
                .get_instance_info(
                    contract_address,
                    &v2::BlockIdentifier::AbsoluteHeight(data.block_info.block_height),
                )
                .await?;
            let contract_name = contract_info.response.name().as_contract_name();

            let supports_cis2 = cis0::supports(
                &mut node_client,
                &v2::BlockIdentifier::AbsoluteHeight(data.block_info.block_height),
                contract_address,
                contract_name,
                cis0::StandardIdentifier::CIS2,
            )
            .await
            .is_ok_and(|r| r.response.is_support());

            if supports_cis2 {
                potential_cis2_events.into_iter().map(|event: cis2::Event| event.into()).collect()
            } else {
                // If contract does not support `CIS2`, don't consider the events as CIS2
                // events.
                vec![]
            }
        };

        Ok(Self {
            height: height.height.try_into()?,
            contract_index: index,
            contract_sub_index: sub_index,
            trace_element_index,
            cis2_token_events,
            trace_event,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "INSERT INTO contract_events (
                transaction_index,
                trace_element_index,
                block_height,
                contract_index,
                contract_sub_index,
                event_index_per_contract
            )
            VALUES (
                $1, $2, $3, $4, $5, (SELECT COALESCE(MAX(event_index_per_contract) + 1, 0) FROM \
             contract_events WHERE contract_index = $4 AND contract_sub_index = $5)
            )",
            transaction_index,
            self.trace_element_index,
            self.height,
            self.contract_index,
            self.contract_sub_index
        )
        .execute(tx.as_mut())
        .await?;

        self.trace_event
            .save(tx, transaction_index)
            .await
            .context("Failed processing trace event")?;

        for log in self.cis2_token_events.iter() {
            process_cis2_token_event(
                log,
                self.contract_index,
                self.contract_sub_index,
                transaction_index,
                tx,
            )
            .await
            .context("Failed processing CIS-2 token event")?
        }
        Ok(())
    }
}

#[derive(Debug)]
enum PreparedContractTraceEvent {
    /// Potential module link events from a smart contract upgrade
    Upgrade(PreparedTraceEventUpgrade),
    /// Transfer to account.
    Transfer(PreparedTraceEventTransfer),
    /// Send messages (and CCD) updating another contract.
    Update(PreparedTraceEventUpdate),
    /// Nothing further needs to be tracked.
    NoEvent,
}

impl PreparedContractTraceEvent {
    async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        match self {
            PreparedContractTraceEvent::Upgrade(event) => event
                .save(tx, transaction_index)
                .await
                .context("Failed processing contract upgrade trace event"),
            PreparedContractTraceEvent::Transfer(event) => event
                .save(tx, transaction_index)
                .await
                .context("Failed processing contract transfer of CCD trace even"),
            PreparedContractTraceEvent::Update(event) => event
                .save(tx, transaction_index)
                .await
                .context("Failed processing contract update trace event"),
            PreparedContractTraceEvent::NoEvent => Ok(()),
        }
    }
}

#[derive(Debug)]
struct PreparedTraceEventUpgrade {
    module_removed:        PreparedModuleLinkAction,
    module_added:          PreparedModuleLinkAction,
    contract_last_upgrade: PreparedUpdateContractLastUpgrade,
}

impl PreparedTraceEventUpgrade {
    fn prepare(
        address: ContractAddress,
        from: sdk_types::hashes::ModuleReference,
        to: sdk_types::hashes::ModuleReference,
    ) -> anyhow::Result<Self> {
        Ok(Self {
            module_removed:        PreparedModuleLinkAction::prepare(
                from,
                address,
                ModuleReferenceContractLinkAction::Removed,
            )?,
            module_added:          PreparedModuleLinkAction::prepare(
                to,
                address,
                ModuleReferenceContractLinkAction::Added,
            )?,
            contract_last_upgrade: PreparedUpdateContractLastUpgrade::prepare(address)?,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        self.module_removed.save(tx, transaction_index).await?;
        self.module_added.save(tx, transaction_index).await?;
        self.contract_last_upgrade.save(tx, transaction_index).await
    }
}

#[derive(Debug)]
struct PreparedUpdateContractLastUpgrade {
    contract_index:     i64,
    contract_sub_index: i64,
}
impl PreparedUpdateContractLastUpgrade {
    fn prepare(address: ContractAddress) -> anyhow::Result<Self> {
        Ok(Self {
            contract_index:     i64::try_from(address.index)?,
            contract_sub_index: i64::try_from(address.subindex)?,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "UPDATE contracts
             SET last_upgrade_transaction_index = $1
             WHERE index = $2 AND sub_index = $3",
            transaction_index,
            self.contract_index,
            self.contract_sub_index
        )
        .execute(tx.as_mut())
        .await?
        .ensure_affected_one_row()
        .context("Failed update contract with last upgrade transaction index")?;
        Ok(())
    }
}

/// Represent a transfer from contract to an account.
#[derive(Debug)]
struct PreparedTraceEventTransfer {
    /// Update the contract balance with the transferred CCD.
    update_contract_balance:  PreparedUpdateContractBalance,
    /// Update the account balance receiving CCD.
    update_receiving_account: PreparedUpdateAccountBalance,
}

impl PreparedTraceEventTransfer {
    fn prepare(
        sender_contract: ContractAddress,
        receiving_account: &AccountAddress,
        amount: Amount,
        block_height: AbsoluteBlockHeight,
    ) -> anyhow::Result<Self> {
        let amount: i64 = amount.micro_ccd().try_into()?;
        let update_contract_balance =
            PreparedUpdateContractBalance::prepare(sender_contract, -amount)?;
        let update_receiving_account = PreparedUpdateAccountBalance::prepare(
            receiving_account,
            amount,
            block_height,
            AccountStatementEntryType::TransferIn,
        )?;
        Ok(Self {
            update_contract_balance,
            update_receiving_account,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        self.update_contract_balance.save(tx).await?;
        self.update_receiving_account.save(tx, Some(transaction_index)).await?;
        Ok(())
    }
}

#[derive(Debug)]
struct PreparedTraceEventUpdate {
    /// Update the caller balance (either an account or contract).
    sender:             PreparedTraceEventUpdateSender,
    /// Update the receiving contract balance.
    receiving_contract: PreparedUpdateContractBalance,
}

#[derive(Debug)]
enum PreparedTraceEventUpdateSender {
    Account(PreparedUpdateAccountBalance),
    Contract(PreparedUpdateContractBalance),
}

impl PreparedTraceEventUpdate {
    fn prepare(
        sender: sdk_types::Address,
        receiver: ContractAddress,
        amount: Amount,
        block_height: AbsoluteBlockHeight,
    ) -> anyhow::Result<Self> {
        let amount: i64 = amount.micro_ccd().try_into()?;
        let sender = match sender {
            sdk_types::Address::Account(address) => {
                PreparedTraceEventUpdateSender::Account(PreparedUpdateAccountBalance::prepare(
                    &address,
                    -amount,
                    block_height,
                    AccountStatementEntryType::TransferOut,
                )?)
            }
            sdk_types::Address::Contract(contract) => PreparedTraceEventUpdateSender::Contract(
                PreparedUpdateContractBalance::prepare(contract, -amount)?,
            ),
        };
        let receiving_contract = PreparedUpdateContractBalance::prepare(receiver, amount)?;
        Ok(Self {
            sender,
            receiving_contract,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        match &self.sender {
            PreparedTraceEventUpdateSender::Account(sender) => sender
                .save(tx, Some(transaction_index))
                .await
                .context("Failed updating account balance with sending of CCD")?,
            PreparedTraceEventUpdateSender::Contract(sender) => sender
                .save(tx)
                .await
                .context("Failed updating contract balance with sending of CCD")?,
        }
        self.receiving_contract
            .save(tx)
            .await
            .context("Failed updating contract balance with receiving of CCD")?;
        Ok(())
    }
}

/// Update of the balance of a contract
#[derive(Debug)]
struct PreparedUpdateContractBalance {
    contract_index:     i64,
    contract_sub_index: i64,
    /// Difference in CCD balance.
    change:             i64,
}

impl PreparedUpdateContractBalance {
    fn prepare(contract: ContractAddress, change: i64) -> anyhow::Result<Self> {
        let contract_index: i64 = contract.index.try_into()?;
        let contract_sub_index: i64 = contract.subindex.try_into()?;
        Ok(Self {
            contract_index,
            contract_sub_index,
            change,
        })
    }

    async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        sqlx::query!(
            "UPDATE contracts SET amount = amount + $1 WHERE index = $2 AND sub_index = $3",
            self.change,
            self.contract_index,
            self.contract_sub_index
        )
        .execute(tx.as_mut())
        .await?
        .ensure_affected_one_row()
        .context("Failed update contract CCD balance")?;
        Ok(())
    }
}

async fn process_cis2_token_event(
    cis2_token_event: &CisEvent,
    contract_index: i64,
    contract_sub_index: i64,
    transaction_index: i64,
    tx: &mut sqlx::PgTransaction<'_>,
) -> anyhow::Result<()> {
    match cis2_token_event {
        // - The `total_supply` value of a token is inserted/updated in the database here.
        // Only `Mint` and `Burn` events affect the `total_supply` of a
        // token.
        // - The `balance` value of the token owner is inserted/updated in the database here.
        // Only `Mint`, `Burn`, and `Transfer` events affect the `balance` of a token owner.
        // - The `tokenEvent` is inserted in the database here.
        // Only `Mint`, `Burn`, `Transfer`, and `TokenMetadata` events are tracked as token events.
        cis2_mint_event @ CisEvent::Mint(CisMintEvent {
            raw_token_id,
            amount,
            owner,
        }) => {
            let token_address = cis2::TokenAddress::new(
                ContractAddress::new(contract_index as u64, contract_sub_index as u64),
                raw_token_id.clone(),
            )
            .to_string();

            // Note: Some `buggy` CIS2 token contracts might mint more tokens than the
            // MAX::TOKEN_AMOUNT specified in the CIS2 standard. The
            // `total_supply/balance` eventually overflows in that case.
            let tokens_minted = BigDecimal::from_biguint(amount.0.clone(), 0);
            // If the `token_address` does not exist, insert the new token with its
            // `total_supply` set to `tokens_minted`. If the `token_address` exists,
            // update the `total_supply` value by adding the `tokens_minted` to the existing
            // value in the database.
            sqlx::query!(
                "
                    INSERT INTO tokens (index, token_index_per_contract, token_address, \
                 contract_index, contract_sub_index, total_supply, token_id, \
                 init_transaction_index)
                    VALUES (
                        (SELECT COALESCE(MAX(index) + 1, 0) FROM tokens),
                        (SELECT COALESCE(MAX(token_index_per_contract) + 1, 0) FROM tokens WHERE \
                 contract_index = $2 AND contract_sub_index = $3),
                        $1,
                        $2,
                        $3,
                        $4,
                        $5,
                        $6
                    )
                    ON CONFLICT (token_address)
                    DO UPDATE SET total_supply = tokens.total_supply + EXCLUDED.total_supply",
                token_address,
                contract_index,
                contract_sub_index,
                tokens_minted.clone(),
                raw_token_id.to_string(),
                transaction_index
            )
            .execute(tx.as_mut())
            .await
            .context("Failed inserting or updating token from mint event")?;

            // If the owner doesn't already hold this token, insert a new row with a balance
            // of `tokens_minted`. Otherwise, update the existing row by
            // incrementing the owner's balance by `tokens_minted`.
            // Note: CCDScan currently only tracks token balances of accounts (issue #357).
            if let sdk_types::Address::Account(owner) = owner {
                let canonical_address = owner.get_canonical_address();
                sqlx::query!(
                    "
                    INSERT INTO account_tokens (index, account_index, token_index, balance)
                    SELECT
                        COALESCE((SELECT MAX(index) + 1 FROM account_tokens), 0),
                        accounts.index,
                        tokens.index,
                        $3
                    FROM accounts, tokens
                    WHERE accounts.canonical_address = $1
                        AND tokens.token_address = $2
                    ON CONFLICT (token_index, account_index)
                    DO UPDATE SET balance = account_tokens.balance + EXCLUDED.balance",
                    canonical_address.0.as_slice(),
                    token_address,
                    tokens_minted,
                )
                .execute(tx.as_mut())
                .await
                .context("Failed inserting or updating account balance from mint event")?;
            }

            // Insert the token event into the table.
            sqlx::query!(
                "INSERT INTO cis2_token_events (
                    index_per_token,
                    transaction_index,
                    token_index,
                    cis2_token_event
                )
                SELECT
                    COALESCE((SELECT MAX(index_per_token) + 1 FROM cis2_token_events WHERE \
                 cis2_token_events.token_index = tokens.index), 0),
                    $1,
                    tokens.index,
                    $3
                FROM tokens
                WHERE tokens.token_address = $2",
                transaction_index,
                token_address,
                serde_json::to_value(cis2_mint_event)?,
            )
            .execute(tx.as_mut())
            .await?;
        }

        // - The `total_supply` value of a token is inserted/updated in the database here.
        // Only `Mint` and `Burn` events affect the `total_supply` of a
        // token.
        // - The `balance` value of the token owner is inserted/updated in the database here.
        // Only `Mint`, `Burn`, and `Transfer` events affect the `balance` of a token owner.
        // - The `tokenEvent` is inserted in the database here.
        // Only `Mint`, `Burn`, `Transfer`, and `TokenMetadata` events are tracked as token events.
        // Note: Some `buggy` CIS2 token contracts might burn more tokens than they have
        // initially minted. The `total_supply/balance` can have a negative value in that case
        // and even underflow.
        cis2_burn_event @ CisEvent::Burn(CisBurnEvent {
            raw_token_id,
            amount,
            owner,
        }) => {
            let token_address = cis2::TokenAddress::new(
                ContractAddress::new(contract_index as u64, contract_sub_index as u64),
                raw_token_id.clone(),
            )
            .to_string();

            // Note: Some `buggy` CIS2 token contracts might burn more tokens than they have
            // initially minted. The `total_supply/balance` will be set to a negative value
            // and eventually underflow in that case.
            let tokens_burned = BigDecimal::from_biguint(amount.0.clone(), 0);
            // If the `token_address` does not exist (likely a `buggy` CIS2 token contract),
            // insert the new token with its `total_supply` set to `-tokens_burned`. If the
            // `token_address` exists, update the `total_supply` value by
            // subtracting the `tokens_burned` from the existing value in the
            // database.
            sqlx::query!(
                "
                    INSERT INTO tokens (index, token_index_per_contract, token_address, \
                 contract_index, contract_sub_index, total_supply, token_id, \
                 init_transaction_index)
                    VALUES (
                        (SELECT COALESCE(MAX(index) + 1, 0) FROM tokens),
                        (SELECT COALESCE(MAX(token_index_per_contract) + 1, 0) FROM tokens WHERE \
                 contract_index = $2 AND contract_sub_index = $3),
                        $1,
                        $2,
                        $3,
                        $4,
                        $5,
                        $6
                    )
                    ON CONFLICT (token_address)
                    DO UPDATE SET total_supply = tokens.total_supply + EXCLUDED.total_supply",
                token_address,
                contract_index,
                contract_sub_index,
                -tokens_burned.clone(),
                raw_token_id.to_string(),
                transaction_index
            )
            .execute(tx.as_mut())
            .await
            .context("Failed inserting or updating token from burn event")?;

            if let sdk_types::Address::Account(owner) = owner {
                let canonical_address = owner.get_canonical_address();
                sqlx::query!(
                    "
                    INSERT INTO account_tokens (index, account_index, token_index, balance)
                    SELECT
                        COALESCE((SELECT MAX(index) + 1 FROM account_tokens), 0),
                        accounts.index,
                        tokens.index,
                        $3
                    FROM accounts, tokens
                    WHERE accounts.canonical_address = $1
                        AND tokens.token_address = $2
                    ON CONFLICT (token_index, account_index)
                    DO UPDATE SET balance = account_tokens.balance + EXCLUDED.balance",
                    canonical_address.0.as_slice(),
                    token_address.to_string(),
                    -tokens_burned
                )
                .execute(tx.as_mut())
                .await
                .context("Failed inserting or updating account balance from burn event")?
                .ensure_affected_rows_in_range(0..=1)?;
            }

            // Insert the token event into the table.
            sqlx::query!(
                "INSERT INTO cis2_token_events (
                    index_per_token,
                    transaction_index,
                    token_index,
                    cis2_token_event
                )
                SELECT
                    COALESCE((SELECT MAX(index_per_token) + 1 FROM cis2_token_events WHERE \
                 cis2_token_events.token_index = tokens.index), 0),
                    $1,
                    tokens.index,
                    $3
                FROM tokens
                WHERE tokens.token_address = $2",
                transaction_index,
                token_address,
                serde_json::to_value(cis2_burn_event)?,
            )
            .execute(tx.as_mut())
            .await?
            .ensure_affected_one_row()?;
        }

        // - The `balance` values of the token are inserted/updated in the database here for the
        //   `from` and `to` addresses.
        // Only `Mint`, `Burn`, and `Transfer` events affect the `balance` of a token owner.
        // - The `tokenEvent` is inserted in the database here.
        // Only `Mint`, `Burn`, `Transfer`, and `TokenMetadata` events are tracked as token events.
        // Note: Some `buggy` CIS2 token contracts might transfer more tokens than an owner owns.
        // The `balance` can have a negative value in that case.
        cis2_transfer_event @ CisEvent::Transfer(CisTransferEvent {
            raw_token_id,
            amount,
            from,
            to,
        }) => {
            let token_address = cis2::TokenAddress::new(
                ContractAddress::new(contract_index as u64, contract_sub_index as u64),
                raw_token_id.clone(),
            )
            .to_string();

            let tokens_transferred = BigDecimal::from_biguint(amount.0.clone(), 0);

            // If the `token_address` does not exist (a `buggy` CIS2 token contract),
            // insert the new token with its `total_supply` set to `0`.
            sqlx::query!(
                "
                    INSERT INTO tokens (index, token_index_per_contract, token_address, \
                 contract_index, contract_sub_index, total_supply, token_id, \
                 init_transaction_index)
                    VALUES (
                        (SELECT COALESCE(MAX(index) + 1, 0) FROM tokens),
                        (SELECT COALESCE(MAX(token_index_per_contract) + 1, 0) FROM tokens WHERE \
                 contract_index = $2 AND contract_sub_index = $3),
                        $1,
                        $2,
                        $3,
                        0,
                        $4,
                        $5
                    )
                    ON CONFLICT (token_address)
                    DO NOTHING",
                token_address,
                contract_index,
                contract_sub_index,
                raw_token_id.to_string(),
                transaction_index
            )
            .execute(tx.as_mut())
            .await
            .context("Failed inserting token from transfer event")?;

            // If the `from` address doesn't already hold this token, insert a new row with
            // a balance of `-tokens_transferred`. Otherwise, update the existing row
            // by decrementing the owner's balance by `tokens_transferred`.
            // Note: CCDScan currently only tracks token balances of accounts (issue #357).
            if let sdk_types::Address::Account(from) = from {
                let canonical_address = from.get_canonical_address();
                sqlx::query!(
                    "
                    INSERT INTO account_tokens (index, account_index, token_index, balance)
                    SELECT
                        COALESCE((SELECT MAX(index) + 1 FROM account_tokens), 0),
                        accounts.index,
                        tokens.index,
                        $3
                    FROM accounts, tokens
                    WHERE accounts.canonical_address = $1
                        AND tokens.token_address = $2
                    ON CONFLICT (token_index, account_index)
                    DO UPDATE SET balance = account_tokens.balance + EXCLUDED.balance",
                    canonical_address.0.as_slice(),
                    token_address,
                    -tokens_transferred.clone(),
                )
                .execute(tx.as_mut())
                .await
                .context(
                    "Failed inserting or updating account balance from transfer event (sender)",
                )?;
            }

            // If the `to` address doesn't already hold this token, insert a new row with a
            // balance of `tokens_transferred`. Otherwise, update the existing row by
            // incrementing the owner's balance by `tokens_transferred`.
            // Note: CCDScan currently only tracks token balances of accounts (issue #357).
            if let sdk_types::Address::Account(to) = to {
                let canonical_address = to.get_canonical_address();
                sqlx::query!(
                    "
                    INSERT INTO account_tokens (index, account_index, token_index, balance)
                    SELECT
                        COALESCE((SELECT MAX(index) + 1 FROM account_tokens), 0),
                        accounts.index,
                        tokens.index,
                        $3
                    FROM accounts, tokens
                    WHERE accounts.canonical_address = $1
                        AND tokens.token_address = $2
                    ON CONFLICT (token_index, account_index)
                        DO UPDATE SET balance = account_tokens.balance + EXCLUDED.balance",
                    canonical_address.0.as_slice(),
                    token_address,
                    tokens_transferred
                )
                .execute(tx.as_mut())
                .await
                .context("Failed inserting or updating account balance from transfer event (to)")?
                .ensure_affected_rows_in_range(0..=1)?;
            }

            // Insert the token event into the table.
            sqlx::query!(
                "INSERT INTO cis2_token_events (
                    index_per_token,
                    transaction_index,
                    token_index,
                    cis2_token_event
                )
                SELECT
                    COALESCE((SELECT MAX(index_per_token) + 1 FROM cis2_token_events WHERE \
                 cis2_token_events.token_index = tokens.index), 0),
                    $1,
                    tokens.index,
                    $3
                FROM tokens
                WHERE tokens.token_address = $2",
                transaction_index,
                token_address,
                serde_json::to_value(cis2_transfer_event)?,
            )
            .execute(tx.as_mut())
            .await
            .with_context(|| {
                format!("Failed inserting the token transfer event: {:?}", cis2_transfer_event)
            })?
            .ensure_affected_one_row()
            .with_context(|| {
                format!("Failed inserting the token transfer event: {:?}", cis2_transfer_event)
            })?;
        }

        // - The `metadata_url` of a token is inserted/updated in the database here.
        // Only `TokenMetadata` events affect the `metadata_url` of a
        // token.
        // - The `tokenEvent` is inserted in the database here.
        // Only `Mint`, `Burn`, `Transfer`, and `TokenMetadata` events are tracked as token events.
        cis2_token_metadata_event @ CisEvent::TokenMetadata(CisTokenMetadataEvent {
            raw_token_id,
            metadata_url,
        }) => {
            let token_address = cis2::TokenAddress::new(
                ContractAddress::new(contract_index as u64, contract_sub_index as u64),
                raw_token_id.clone(),
            )
            .to_string();

            // If the `token_address` does not exist, insert the new token.
            // If the `token_address` exists, update the `metadata_url` value in the
            // database.
            sqlx::query!(
                "
                    INSERT INTO tokens (index, token_index_per_contract, token_address, \
                 contract_index, contract_sub_index, metadata_url, token_id, \
                 init_transaction_index)
                    VALUES (
                        (SELECT COALESCE(MAX(index) + 1, 0) FROM tokens),
                        (SELECT COALESCE(MAX(token_index_per_contract) + 1, 0) FROM tokens WHERE \
                 contract_index = $2 AND contract_sub_index = $3),
                        $1,
                        $2,
                        $3,
                        $4,
                        $5,
                        $6
                    )
                    ON CONFLICT (token_address)
                    DO UPDATE SET metadata_url = EXCLUDED.metadata_url",
                token_address,
                contract_index,
                contract_sub_index,
                metadata_url.url(),
                raw_token_id.to_string(),
                transaction_index
            )
            .execute(tx.as_mut())
            .await
            .context("Failed inserting or updating token from token metadata event")?;

            // Insert the token event into the table.
            sqlx::query!(
                "INSERT INTO cis2_token_events (
                    index_per_token,
                    transaction_index,
                    token_index,
                    cis2_token_event
                )
                SELECT
                    COALESCE((SELECT MAX(index_per_token) + 1 FROM cis2_token_events WHERE \
                 cis2_token_events.token_index = tokens.index), 0),
                    $1,
                    tokens.index,
                    $3
                FROM tokens
                WHERE tokens.token_address = $2",
                transaction_index,
                token_address,
                serde_json::to_value(cis2_token_metadata_event)?,
            )
            .execute(tx.as_mut())
            .await?
            .ensure_affected_one_row()?;
        }
        _ => (),
    }
    Ok(())
}
