//! Function and types for populating the database with initial information
//! found in the genesis block.

use super::block_preprocessor::compute_validator_staking_information;
use crate::transaction_event::baker::BakerPoolOpenStatus;
use anyhow::Context;
use concordium_rust_sdk::{
    types::{AccountStakingInfo, PartsPerHundredThousands},
    v2,
};
use futures::{StreamExt, TryStreamExt};
use sqlx::Connection;

/// Function for initializing the database with the genesis block.
/// This should only be called if the database is empty.
pub async fn save_genesis_data(
    endpoint: v2::Endpoint,
    pool: &mut sqlx::PgConnection,
    stake_recompute_interval_in_blocks: u64,
) -> anyhow::Result<()> {
    let mut client = v2::Client::new(endpoint)
        .await
        .context("Failed to establish connection to Concordium Node")?;
    let mut tx = pool.begin().await.context("Failed to create SQL transaction")?;
    let genesis_height = v2::BlockIdentifier::AbsoluteHeight(0.into());

    let genesis_block_info = client.get_block_info(genesis_height).await?.response;
    let block_hash = genesis_block_info.block_hash.to_string();
    let slot_time = genesis_block_info.block_slot_time;
    let genesis_tokenomics = client.get_tokenomics_info(genesis_height).await?.response;

    let (total_staked_capital, _) = compute_validator_staking_information(
        &mut client,
        genesis_height,
        stake_recompute_interval_in_blocks,
    )
    .await?;
    let total_staked = i64::try_from(total_staked_capital.micro_ccd())?;

    let total_amount =
        i64::try_from(genesis_tokenomics.common_reward_data().total_amount.micro_ccd())?;
    sqlx::query!(
        "INSERT INTO blocks (
            height,
            hash,
            slot_time,
            block_time,
            finalization_time,
            total_amount,
            total_staked,
            cumulative_num_txs
        ) VALUES (0, $1, $2, 0, 0, $3, $4, 0);",
        block_hash,
        slot_time,
        total_amount,
        total_staked,
    )
    .execute(&mut *tx)
    .await?;
    let genesis_bakers_count: i64 =
        client.get_baker_list(genesis_height).await?.response.count().await.try_into()?;
    sqlx::query!(
        "INSERT INTO metrics_bakers (block_height, total_bakers_added, total_bakers_removed)
        VALUES (0, $1, 0)",
        genesis_bakers_count,
    )
    .execute(&mut *tx)
    .await?;

    let mut genesis_accounts = client.get_account_list(genesis_height).await?.response;
    while let Some(account) = genesis_accounts.try_next().await? {
        let info = client.get_account_info(&account.into(), genesis_height).await?.response;
        let index = i64::try_from(info.account_index.index)?;
        let account_address = account.to_string();
        let canonical_address = account.get_canonical_address();
        let amount = i64::try_from(info.account_amount.micro_ccd)?;

        // Note that we override the usual default num_txs = 1 here
        // because the genesis accounts do not have a creation transaction.
        sqlx::query!(
            "INSERT INTO accounts (index, address, amount, canonical_address, num_txs)
            VALUES ($1, $2, $3, $4, 0)",
            index,
            account_address,
            amount,
            canonical_address.0.as_slice()
        )
        .execute(&mut *tx)
        .await?;

        if let Some(AccountStakingInfo::Baker {
            staked_amount,
            restake_earnings,
            baker_info: _,
            pending_change: _,
            pool_info,
            is_suspended: _,
        }) = info.account_stake
        {
            let stake = i64::try_from(staked_amount.micro_ccd())?;
            let open_status = pool_info.as_ref().map(|i| BakerPoolOpenStatus::from(i.open_status));
            let metadata_url = pool_info.as_ref().map(|i| i.metadata_url.to_string());
            let transaction_commission = pool_info.as_ref().map(|i| {
                i64::from(u32::from(PartsPerHundredThousands::from(i.commission_rates.transaction)))
            });
            let baking_commission = pool_info.as_ref().map(|i| {
                i64::from(u32::from(PartsPerHundredThousands::from(i.commission_rates.baking)))
            });
            let finalization_commission = pool_info.as_ref().map(|i| {
                i64::from(u32::from(PartsPerHundredThousands::from(
                    i.commission_rates.finalization,
                )))
            });
            sqlx::query!(
                "INSERT INTO bakers (id, staked, restake_earnings, open_status, metadata_url, \
                 transaction_commission, baking_commission, finalization_commission, \
                 pool_total_staked, pool_delegator_count)
        VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)",
                index,
                stake,
                restake_earnings,
                open_status as Option<BakerPoolOpenStatus>,
                metadata_url,
                transaction_commission,
                baking_commission,
                finalization_commission,
                stake,
                0
            )
            .execute(&mut *tx)
            .await?;
        }
    }

    tx.commit().await.context("Failed to commit SQL transaction")?;
    Ok(())
}
