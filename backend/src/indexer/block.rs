//! This module contains the block information computed during the concurrent
//! preprocessing and the logic for how to do the sequential processing.
use std::collections::HashMap;
use crate::indexer::{
    block_preprocessor::BlockData, block_processor::BlockProcessingContext, statistics::Statistics,
};
use anyhow::Context;
use block_item::PreparedBlockItem;
use chrono::{DateTime, Utc};
use concordium_rust_sdk::v2;
use protocol_update_migration::ProtocolUpdateMigration;
use special_transaction_outcomes::{
    validator_suspension::PreparedUnmarkPrimedForSuspension, PreparedSpecialTransactionOutcomes,
};

pub mod block_item;
pub mod protocol_update_migration;
pub mod special_transaction_outcomes;

#[derive(Clone)]
pub struct ValidatorStakingInformation {
    pub stake_amount: u64,
    pub pool_total_staked_amount: u64,
}

/// Preprocessed block which is ready to be saved in the database.
pub struct PreparedBlock {
    /// Hash of the block.
    pub hash:                     String,
    /// Absolute height of the block.
    pub height:                   i64,
    /// Block slot time (UTC).
    slot_time:                    DateTime<Utc>,
    /// Id of the validator which constructed the block. Is only None for the
    /// genesis block.
    baker_id:                     Option<i64>,
    /// Total amount of CCD in existence at the time of this block.
    total_amount:                 i64,
    /// Total staked CCD at the time of this block.
    total_staked:                 i64,
    /// Block hash of the last finalized block.
    block_last_finalized:         String,
    /// Preprocessed block items, ready to be saved in the database.
    prepared_block_items:         Vec<PreparedBlockItem>,
    /// Preprocessed block special items, ready to be saved in the database.
    special_transaction_outcomes: PreparedSpecialTransactionOutcomes,
    /// Unmark the baker and signers of the Quorum Certificate from being primed
    /// for suspension.
    baker_unmark_suspended:       PreparedUnmarkPrimedForSuspension,
    /// Statistics gathered about frequency of events
    statistics:                   Statistics,
    /// Optional data migration for when this is the first block after a
    /// protocol update.
    protocol_update_migration:    Option<ProtocolUpdateMigration>,
    /// Validator staking information to be updated in the database
    //validator_stake_updates:      HashMap<i64, u64>, 
    validator_stake_updates:      HashMap<i64, ValidatorStakingInformation>, 
}

impl PreparedBlock {
    pub async fn prepare(node_client: &mut v2::Client, data: &BlockData) -> anyhow::Result<Self> {
        let height = i64::try_from(data.finalized_block_info.height.height)?;
        let hash = data.finalized_block_info.block_hash.to_string();
        let block_last_finalized = data.block_info.block_last_finalized.to_string();
        let slot_time = data.block_info.block_slot_time;
        let baker_id = if let Some(index) = data.block_info.block_baker {
            Some(i64::try_from(index.id.index)?)
        } else {
            None
        };
        let mut statistics = Statistics::new(height, slot_time);
        let total_amount =
            i64::try_from(data.tokenomics_info.common_reward_data().total_amount.micro_ccd())?;
        let total_staked = i64::try_from(data.total_staked_capital.micro_ccd())?;
        let mut prepared_block_items = Vec::new();
        for (item_summary, item) in data.events.iter().zip(data.items.iter()) {
            prepared_block_items.push(
                PreparedBlockItem::prepare(node_client, data, item_summary, item, &mut statistics)
                    .await?,
            )
        }

        let special_transaction_outcomes = PreparedSpecialTransactionOutcomes::prepare(
            node_client,
            &data.block_info,
            &data.special_events,
            &mut statistics,
        )
        .await?;
        let baker_unmark_suspended = PreparedUnmarkPrimedForSuspension::prepare(data)?;
        let protocol_update_migration =
            ProtocolUpdateMigration::prepare(node_client, data)
                .await
                .context("Failed to prepare for data migation caused by protocol update")?;

        let validator_stake_updates: HashMap<i64, ValidatorStakingInformation> = data.validator_stakes.clone();

        Ok(Self {
            hash,
            height,
            slot_time,
            baker_id,
            total_amount,
            total_staked,
            block_last_finalized,
            prepared_block_items,
            special_transaction_outcomes,
            baker_unmark_suspended,
            statistics,
            protocol_update_migration,
            validator_stake_updates,
        })
    }

    pub async fn batch_save(
        batch: &[Self],
        context: &mut BlockProcessingContext,
        tx: &mut sqlx::PgTransaction<'_>,
    ) -> anyhow::Result<()> {
        let mut heights = Vec::with_capacity(batch.len());
        let mut hashes = Vec::with_capacity(batch.len());
        let mut slot_times = Vec::with_capacity(batch.len());
        let mut baker_ids = Vec::with_capacity(batch.len());
        let mut total_amounts = Vec::with_capacity(batch.len());
        let mut total_staked = Vec::with_capacity(batch.len());
        let mut block_times = Vec::with_capacity(batch.len());
        let mut cumulative_num_txss = Vec::with_capacity(batch.len());

        let mut finalizers = Vec::with_capacity(batch.len());
        let mut last_finalizeds = Vec::with_capacity(batch.len());
        let mut finalizers_slot_time = Vec::with_capacity(batch.len());

        for block in batch {
            heights.push(block.height);
            hashes.push(block.hash.clone());
            slot_times.push(block.slot_time);
            baker_ids.push(block.baker_id);
            total_amounts.push(block.total_amount);
            total_staked.push(block.total_staked);
            block_times.push(
                block
                    .slot_time
                    .signed_duration_since(context.last_block_slot_time)
                    .num_milliseconds(),
            );
            context.last_cumulative_num_txs += block.prepared_block_items.len() as i64;
            cumulative_num_txss.push(context.last_cumulative_num_txs);
            context.last_block_slot_time = block.slot_time;

            // Check if this block knows of a new finalized block.
            // If so, note it down so we can mark the blocks since last time as finalized by
            // this block.
            if block.block_last_finalized != context.last_finalized_hash {
                finalizers.push(block.height);
                finalizers_slot_time.push(block.slot_time);
                last_finalizeds.push(block.block_last_finalized.clone());

                context.last_finalized_hash = block.block_last_finalized.clone();
            }
        }

        sqlx::query!(
            "INSERT INTO blocks (
                height, 
                hash, 
                slot_time, 
                block_time, 
                baker_id, 
                total_amount, 
                total_staked, 
                cumulative_num_txs
            )
            SELECT * FROM UNNEST(
                $1::BIGINT[],
                $2::TEXT[],
                $3::TIMESTAMPTZ[],
                $4::BIGINT[],
                $5::BIGINT[],
                $6::BIGINT[],
                $7::BIGINT[],
                $8::BIGINT[]
            );",
            &heights,
            &hashes,
            &slot_times,
            &block_times,
            &baker_ids as &[Option<i64>],
            &total_amounts,
            &total_staked,
            &cumulative_num_txss
        )
        .execute(tx.as_mut())
        .await?;

        // With all blocks in the batch inserted we update blocks which we now can
        // compute the finalization time for. Using the list of finalizer blocks
        // (those containing a last finalized block different from its predecessor)
        // we update the blocks below which does not contain finalization time and
        // compute it to be the difference between the slot_time of the block and the
        // finalizer block.
        sqlx::query!(
            "UPDATE blocks SET
                finalization_time = (
                    EXTRACT(EPOCH FROM finalizer.slot_time - blocks.slot_time)::double precision
                        * 1000
                )::bigint,
                finalized_by = finalizer.height
            FROM UNNEST(
                $1::BIGINT[],
                $2::TEXT[],
                $3::TIMESTAMPTZ[]
            ) AS finalizer(height, finalized, slot_time)
            JOIN blocks last ON finalizer.finalized = last.hash
            WHERE blocks.finalization_time IS NULL AND blocks.height <= last.height",
            &finalizers,
            &last_finalizeds,
            &finalizers_slot_time
        )
        .execute(tx.as_mut())
        .await?;

        // With the finalization_time update for each finalized block, we also have to
        // update the cumulative_finalization_time for these blocks.
        // Returns the cumulative_finalization_time of the latest finalized block.
        let new_last_cumulative_finalization_time = sqlx::query_scalar!(
            "WITH cumulated AS (
                -- Compute the sum of finalization_time for the finalized missing the cumulative.
                SELECT
                    height,
                    -- Note this sum is only of those without a cumulative_finalization_time and
                    -- not the entire table.
                    SUM(finalization_time) OVER (
                        ORDER BY height
                        RANGE BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
                    ) AS time
                FROM blocks
                WHERE blocks.cumulative_finalization_time IS NULL
                    AND blocks.finalization_time IS NOT NULL
                ORDER BY height
            ), updated AS (
                -- Update the cumulative time from the previous known plus the newly computed.
                UPDATE blocks
                    SET cumulative_finalization_time = $1 + cumulated.time
                FROM cumulated
                WHERE blocks.height = cumulated.height
                RETURNING cumulated.height, cumulative_finalization_time
            )
            -- Return only the latest cumulative_finalization_time.
            SELECT updated.cumulative_finalization_time
            FROM updated
            ORDER BY updated.height DESC
            LIMIT 1",
            context.last_cumulative_finalization_time
        )
        .fetch_optional(tx.as_mut())
        .await?
        .flatten();
        if let Some(cumulative_finalization_time) = new_last_cumulative_finalization_time {
            context.last_cumulative_finalization_time = cumulative_finalization_time;
        }
        Ok(())
    }

    pub async fn process_block_content(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
    ) -> anyhow::Result<()> {
        if let Some(migration) = self.protocol_update_migration.as_ref() {
            migration.save(tx).await?;
        }
        for item in self.prepared_block_items.iter() {
            item.save(tx).await.with_context(|| {
                format!(
                    "Failed processing block item with hash {} for block height {}",
                    item.block_item_hash, item.block_height
                )
            })?;
        }
        self.statistics.save(tx).await?;
        self.special_transaction_outcomes.save(tx).await?;

        // process validator updates for staked amount and pool total staked amount
        for (validator_id, validator_staking_information) in &self.validator_stake_updates {
            sqlx::query(
                "UPDATE BAKERS
                SET staked = $2, pool_total_staked = $3
                WHERE id = $1"
            )
            .bind(validator_id)
            .bind(validator_staking_information.stake_amount as i64)
            .bind(validator_staking_information.pool_total_staked_amount as i64)
            .execute(tx.as_mut())
            .await?;
        }

        self.baker_unmark_suspended.save(tx).await?;
        Ok(())
    }
}
