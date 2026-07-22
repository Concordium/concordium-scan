use crate::transaction_event::{
    protocol_level_locks::{
        LockCanceled, LockCreateConfig, LockCreated, LockDestroyed, LockFunded, LockReturned,
        LockSent,
    },
    protocol_level_tokens::TokenAmount,
    Event,
};
use anyhow::Context;
use bigdecimal::BigDecimal;
use chrono::{DateTime, Utc};
use std::str::FromStr;

#[derive(Debug)]
pub struct PreparedLockEvents {
    events: Vec<PreparedLockEvent>,
}

impl PreparedLockEvents {
    pub fn prepare(events: Vec<Event>, sender: String) -> anyhow::Result<Self> {
        let events = events
            .into_iter()
            .filter_map(|event| PreparedLockEvent::prepare(event, &sender).transpose())
            .collect::<anyhow::Result<Vec<_>>>()?;
        Ok(Self { events })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
        slot_time: DateTime<Utc>,
    ) -> anyhow::Result<()> {
        for (operation_order, event) in self.events.iter().enumerate() {
            event
                .save(tx, transaction_index, slot_time, operation_order as i32)
                .await?;
        }
        Ok(())
    }
}

#[derive(Debug)]
enum PreparedLockEvent {
    Created {
        event: LockCreated,
        creator: String,
    },
    Funded {
        event: LockFunded,
        account: String,
    },
    Sent {
        event: LockSent,
        sender: String,
    },
    Returned {
        event: LockReturned,
        sender: String,
    },
    Canceled {
        event: LockCanceled,
        sender: String,
    },
    Destroyed {
        event: LockDestroyed,
        sender: String,
    },
}

impl PreparedLockEvent {
    fn prepare(event: Event, sender: &str) -> anyhow::Result<Option<Self>> {
        Ok(match event {
            Event::LockCreated(event) => Some(Self::Created {
                event,
                creator: sender.to_string(),
            }),
            Event::LockFunded(event) => Some(Self::Funded {
                event,
                account: sender.to_string(),
            }),
            Event::LockSent(event) => Some(Self::Sent {
                event,
                sender: sender.to_string(),
            }),
            Event::LockReturned(event) => Some(Self::Returned {
                event,
                sender: sender.to_string(),
            }),
            Event::LockCanceled(event) => Some(Self::Canceled {
                event,
                sender: sender.to_string(),
            }),
            Event::LockDestroyed(event) => Some(Self::Destroyed {
                event,
                sender: sender.to_string(),
            }),
            _ => None,
        })
    }

    async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
        slot_time: DateTime<Utc>,
        operation_order: i32,
    ) -> anyhow::Result<()> {
        match self {
            PreparedLockEvent::Created { event, creator } => {
                if let Some(lock_id) = &event.lock_id {
                    upsert_created_lock(tx, transaction_index, slot_time, lock_id, creator, event)
                        .await?;
                    insert_lock_event(
                        tx,
                        InsertLockEvent {
                            transaction_index,
                            slot_time,
                            operation_order,
                            event_type: "LockCreate",
                            lock_id,
                            token_id: None,
                            account: Some(creator),
                            source: None,
                            recipient: None,
                            amount: None,
                            memo: None,
                            event,
                        },
                    )
                    .await?;
                    upsert_relationship(tx, lock_id, creator, "Creator", transaction_index).await?;
                    upsert_relationship(tx, lock_id, creator, "Touched", transaction_index).await?;
                    if let Some(config) = &event.config {
                        save_config_relationships(tx, lock_id, config, transaction_index).await?;
                    }
                }
            }
            PreparedLockEvent::Funded { event, account } => {
                ensure_lock_exists(tx, &event.lock_id).await?;
                insert_lock_event(
                    tx,
                    InsertLockEvent {
                        transaction_index,
                        slot_time,
                        operation_order,
                        event_type: "LockFund",
                        lock_id: &event.lock_id,
                        token_id: Some(&event.token_id),
                        account: Some(account),
                        source: None,
                        recipient: None,
                        amount: Some(&event.amount),
                        memo: event.memo.as_ref(),
                        event,
                    },
                )
                .await?;
                update_balance(
                    tx,
                    &event.lock_id,
                    account,
                    &event.token_id,
                    &event.amount,
                    1,
                )
                .await?;
                upsert_relationship(
                    tx,
                    &event.lock_id,
                    account,
                    "BalanceHolder",
                    transaction_index,
                )
                .await?;
                upsert_relationship(tx, &event.lock_id, account, "Touched", transaction_index)
                    .await?;
            }
            PreparedLockEvent::Sent { event, sender } => {
                ensure_lock_exists(tx, &event.lock_id).await?;
                let source = event.source.address.to_string();
                let recipient = event.recipient.address.to_string();
                insert_lock_event(
                    tx,
                    InsertLockEvent {
                        transaction_index,
                        slot_time,
                        operation_order,
                        event_type: "LockSend",
                        lock_id: &event.lock_id,
                        token_id: Some(&event.token_id),
                        account: None,
                        source: Some(&source),
                        recipient: Some(&recipient),
                        amount: Some(&event.amount),
                        memo: event.memo.as_ref(),
                        event,
                    },
                )
                .await?;
                update_balance(
                    tx,
                    &event.lock_id,
                    &source,
                    &event.token_id,
                    &event.amount,
                    -1,
                )
                .await?;
                update_balance(
                    tx,
                    &event.lock_id,
                    &recipient,
                    &event.token_id,
                    &event.amount,
                    1,
                )
                .await?;
                upsert_relationship(
                    tx,
                    &event.lock_id,
                    &source,
                    "BalanceHolder",
                    transaction_index,
                )
                .await?;
                upsert_relationship(
                    tx,
                    &event.lock_id,
                    &recipient,
                    "BalanceHolder",
                    transaction_index,
                )
                .await?;
                upsert_relationship(tx, &event.lock_id, &source, "Touched", transaction_index)
                    .await?;
                upsert_relationship(tx, &event.lock_id, &recipient, "Touched", transaction_index)
                    .await?;
                upsert_relationship(tx, &event.lock_id, sender, "Touched", transaction_index)
                    .await?;
            }
            PreparedLockEvent::Returned { event, sender } => {
                ensure_lock_exists(tx, &event.lock_id).await?;
                let source = event.source.address.to_string();
                insert_lock_event(
                    tx,
                    InsertLockEvent {
                        transaction_index,
                        slot_time,
                        operation_order,
                        event_type: "LockReturn",
                        lock_id: &event.lock_id,
                        token_id: Some(&event.token_id),
                        account: None,
                        source: Some(&source),
                        recipient: None,
                        amount: Some(&event.amount),
                        memo: event.memo.as_ref(),
                        event,
                    },
                )
                .await?;
                update_balance(
                    tx,
                    &event.lock_id,
                    &source,
                    &event.token_id,
                    &event.amount,
                    -1,
                )
                .await?;
                upsert_relationship(
                    tx,
                    &event.lock_id,
                    &source,
                    "BalanceHolder",
                    transaction_index,
                )
                .await?;
                upsert_relationship(tx, &event.lock_id, &source, "Touched", transaction_index)
                    .await?;
                upsert_relationship(tx, &event.lock_id, sender, "Touched", transaction_index)
                    .await?;
            }
            PreparedLockEvent::Canceled { event, sender } => {
                ensure_lock_exists(tx, &event.lock_id).await?;
                mark_lock_canceled(tx, transaction_index, slot_time, &event.lock_id).await?;
                insert_lock_event(
                    tx,
                    InsertLockEvent {
                        transaction_index,
                        slot_time,
                        operation_order,
                        event_type: "LockCancel",
                        lock_id: &event.lock_id,
                        token_id: None,
                        account: Some(sender),
                        source: None,
                        recipient: None,
                        amount: None,
                        memo: event.memo.as_ref(),
                        event,
                    },
                )
                .await?;
                clear_balances(tx, &event.lock_id).await?;
                upsert_relationship(tx, &event.lock_id, sender, "Touched", transaction_index)
                    .await?;
            }
            PreparedLockEvent::Destroyed { event, sender } => {
                ensure_lock_exists(tx, &event.lock_id).await?;
                mark_lock_canceled(tx, transaction_index, slot_time, &event.lock_id).await?;
                insert_lock_event(
                    tx,
                    InsertLockEvent {
                        transaction_index,
                        slot_time,
                        operation_order,
                        event_type: "LockDestroy",
                        lock_id: &event.lock_id,
                        token_id: None,
                        account: None,
                        source: None,
                        recipient: None,
                        amount: None,
                        memo: None,
                        event,
                    },
                )
                .await?;
                clear_balances(tx, &event.lock_id).await?;
                upsert_relationship(tx, &event.lock_id, sender, "Touched", transaction_index)
                    .await?;
            }
        }
        Ok(())
    }
}

struct InsertLockEvent<'a, T> {
    transaction_index: i64,
    slot_time: DateTime<Utc>,
    operation_order: i32,
    event_type: &'a str,
    lock_id: &'a str,
    token_id: Option<&'a str>,
    account: Option<&'a str>,
    source: Option<&'a str>,
    recipient: Option<&'a str>,
    amount: Option<&'a TokenAmount>,
    memo: Option<&'a crate::transaction_event::protocol_level_tokens::Memo>,
    event: &'a T,
}

async fn insert_lock_event<T: serde::Serialize>(
    tx: &mut sqlx::PgTransaction<'_>,
    params: InsertLockEvent<'_, T>,
) -> anyhow::Result<()> {
    let amount = params.amount.map(amount_value).transpose()?;
    let decimals = params.amount.map(amount_decimals).transpose()?;
    let memo = params.memo.map(serde_json::to_value).transpose()?;
    let event = serde_json::to_value(params.event)?;
    let account = params.account.map(canonical_address).transpose()?;
    let source = params.source.map(canonical_address).transpose()?;
    let recipient = params.recipient.map(canonical_address).transpose()?;

    sqlx::query(
        r#"
        INSERT INTO plt_lock_events (
            transaction_index,
            block_height,
            slot_time,
            operation_order,
            event_type,
            lock_id,
            token_index,
            account_index,
            source_account_index,
            recipient_account_index,
            amount,
            decimals,
            memo,
            event
        )
        VALUES (
            $1,
            (SELECT block_height FROM transactions WHERE index = $1),
            $2,
            $3,
            $4,
            $5,
            (SELECT index FROM plt_tokens WHERE token_id = $6),
            (SELECT index FROM accounts WHERE canonical_address = $7::bytea),
            (SELECT index FROM accounts WHERE canonical_address = $8::bytea),
            (SELECT index FROM accounts WHERE canonical_address = $9::bytea),
            $10,
            $11,
            $12,
            $13
        )
        "#,
    )
    .bind(params.transaction_index)
    .bind(params.slot_time)
    .bind(params.operation_order)
    .bind(params.event_type)
    .bind(params.lock_id)
    .bind(params.token_id)
    .bind(account)
    .bind(source)
    .bind(recipient)
    .bind(amount)
    .bind(decimals)
    .bind(memo)
    .bind(event)
    .execute(tx.as_mut())
    .await?;

    Ok(())
}

async fn upsert_created_lock(
    tx: &mut sqlx::PgTransaction<'_>,
    transaction_index: i64,
    slot_time: DateTime<Utc>,
    lock_id: &str,
    creator: &str,
    event: &LockCreated,
) -> anyhow::Result<()> {
    let creator = canonical_address(creator)?;
    let config = event
        .config
        .as_ref()
        .map(serde_json::to_value)
        .transpose()?;
    let expiry = event.config.as_ref().map(|config| config.expiry);
    let metadata_name = event
        .config
        .as_ref()
        .and_then(|config| config.metadata.as_ref())
        .and_then(|metadata| metadata.name.clone());
    let metadata_description = event
        .config
        .as_ref()
        .and_then(|config| config.metadata.as_ref())
        .and_then(|metadata| metadata.description.clone());

    sqlx::query(
        r#"
        INSERT INTO plt_locks (
            lock_id,
            creator_account_index,
            created_transaction_index,
            created_block_height,
            created_at,
            expiry,
            config,
            raw_config,
            metadata_name,
            metadata_description
        )
        VALUES (
            $1,
            (SELECT index FROM accounts WHERE canonical_address = $2::bytea),
            $3,
            (SELECT block_height FROM transactions WHERE index = $3),
            $4,
            $5,
            $6,
            $7,
            $8,
            $9
        )
        ON CONFLICT (lock_id) DO UPDATE SET
            creator_account_index = COALESCE(plt_locks.creator_account_index, EXCLUDED.creator_account_index),
            created_transaction_index = COALESCE(plt_locks.created_transaction_index, EXCLUDED.created_transaction_index),
            created_block_height = COALESCE(plt_locks.created_block_height, EXCLUDED.created_block_height),
            created_at = COALESCE(plt_locks.created_at, EXCLUDED.created_at),
            expiry = COALESCE(plt_locks.expiry, EXCLUDED.expiry),
            config = COALESCE(plt_locks.config, EXCLUDED.config),
            raw_config = COALESCE(plt_locks.raw_config, EXCLUDED.raw_config),
            metadata_name = COALESCE(plt_locks.metadata_name, EXCLUDED.metadata_name),
            metadata_description = COALESCE(plt_locks.metadata_description, EXCLUDED.metadata_description)
        "#,
    )
    .bind(lock_id)
    .bind(creator)
    .bind(transaction_index)
    .bind(slot_time)
    .bind(expiry)
    .bind(config)
    .bind(&event.raw_config)
    .bind(metadata_name)
    .bind(metadata_description)
    .execute(tx.as_mut())
    .await?;

    Ok(())
}

async fn ensure_lock_exists(tx: &mut sqlx::PgTransaction<'_>, lock_id: &str) -> anyhow::Result<()> {
    sqlx::query("INSERT INTO plt_locks (lock_id) VALUES ($1) ON CONFLICT DO NOTHING")
        .bind(lock_id)
        .execute(tx.as_mut())
        .await?;
    Ok(())
}

async fn mark_lock_canceled(
    tx: &mut sqlx::PgTransaction<'_>,
    transaction_index: i64,
    slot_time: DateTime<Utc>,
    lock_id: &str,
) -> anyhow::Result<()> {
    sqlx::query(
        r#"
        UPDATE plt_locks
        SET
            canceled_transaction_index = COALESCE(canceled_transaction_index, $1),
            canceled_block_height = COALESCE(canceled_block_height, (SELECT block_height FROM transactions WHERE index = $1)),
            canceled_at = COALESCE(canceled_at, $2)
        WHERE lock_id = $3
        "#,
    )
    .bind(transaction_index)
    .bind(slot_time)
    .bind(lock_id)
    .execute(tx.as_mut())
    .await?;
    Ok(())
}

async fn save_config_relationships(
    tx: &mut sqlx::PgTransaction<'_>,
    lock_id: &str,
    config: &LockCreateConfig,
    transaction_index: i64,
) -> anyhow::Result<()> {
    for recipient in &config.recipients.accounts {
        upsert_relationship(
            tx,
            lock_id,
            &recipient.address.to_string(),
            "Recipient",
            transaction_index,
        )
        .await?;
    }

    if let Some(simple_v0) = &config.controller.simple_v0 {
        for grant in &simple_v0.grants {
            upsert_relationship(
                tx,
                lock_id,
                &grant.account.address.to_string(),
                "Controller",
                transaction_index,
            )
            .await?;
        }
    }

    Ok(())
}

async fn upsert_relationship(
    tx: &mut sqlx::PgTransaction<'_>,
    lock_id: &str,
    account: &str,
    relationship_type: &str,
    transaction_index: i64,
) -> anyhow::Result<()> {
    let account = canonical_address(account)?;
    sqlx::query(
        r#"
        INSERT INTO plt_lock_accounts (
            lock_id,
            account_index,
            relationship_type,
            first_transaction_index,
            last_transaction_index
        )
        SELECT $1, accounts.index, $3, $4, $4
        FROM accounts
        WHERE accounts.canonical_address = $2::bytea
        ON CONFLICT (lock_id, account_index, relationship_type) DO UPDATE SET
            last_transaction_index = EXCLUDED.last_transaction_index
        "#,
    )
    .bind(lock_id)
    .bind(account)
    .bind(relationship_type)
    .bind(transaction_index)
    .execute(tx.as_mut())
    .await?;
    Ok(())
}

async fn update_balance(
    tx: &mut sqlx::PgTransaction<'_>,
    lock_id: &str,
    account: &str,
    token_id: &str,
    amount: &TokenAmount,
    sign: i32,
) -> anyhow::Result<()> {
    let account = canonical_address(account)?;
    let amount_value = amount_value(amount)?;
    let amount_value = if sign < 0 {
        -amount_value
    } else {
        amount_value
    };
    let decimals = amount_decimals(amount)?;

    sqlx::query(
        r#"
        INSERT INTO plt_lock_balances (lock_id, account_index, token_index, amount, decimal)
        SELECT $1, accounts.index, plt_tokens.index, $4, $5
        FROM accounts, plt_tokens
        WHERE accounts.canonical_address = $2::bytea
          AND plt_tokens.token_id = $3
        ON CONFLICT (lock_id, account_index, token_index) DO UPDATE SET
            amount = plt_lock_balances.amount + EXCLUDED.amount,
            decimal = EXCLUDED.decimal
        "#,
    )
    .bind(lock_id)
    .bind(account)
    .bind(token_id)
    .bind(amount_value)
    .bind(decimals)
    .execute(tx.as_mut())
    .await?;

    Ok(())
}

async fn clear_balances(tx: &mut sqlx::PgTransaction<'_>, lock_id: &str) -> anyhow::Result<()> {
    sqlx::query("UPDATE plt_lock_balances SET amount = 0 WHERE lock_id = $1")
        .bind(lock_id)
        .execute(tx.as_mut())
        .await?;
    Ok(())
}

fn amount_value(amount: &TokenAmount) -> anyhow::Result<BigDecimal> {
    BigDecimal::from_str(&amount.value).context("Failed to parse lock token amount")
}

fn amount_decimals(amount: &TokenAmount) -> anyhow::Result<i32> {
    amount
        .decimals
        .parse()
        .context("Failed to parse lock token decimals")
}

fn canonical_address(account: &str) -> anyhow::Result<Vec<u8>> {
    let account = concordium_rust_sdk::base::contracts_common::AccountAddress::from_str(account)
        .map_err(|_| anyhow::anyhow!("Failed to parse account address: {}", account))?;
    Ok(account.get_canonical_address().0.to_vec())
}
