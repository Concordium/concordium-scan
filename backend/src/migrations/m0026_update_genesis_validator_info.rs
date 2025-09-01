//! Migration updating pool information those bakers where this is missing.
//!
//! Since the baker/validator pool concept was introduced as part of Concordium
//! protocol version 4, bakers prior to this protocol version became pools
//! implicitly and older versions of the indexer missed their pool information.

use super::SchemaVersion;
use crate::transaction_event::baker::BakerPoolOpenStatus;
use anyhow::Context;
use concordium_rust_sdk::{
    types::{PartsPerHundredThousands, ProtocolVersion},
    v2::{self, BlockIdentifier},
};
use futures::TryStreamExt;

/// Resulting database schema version from running this migration.
const NEXT_SCHEMA_VERSION: SchemaVersion = SchemaVersion::UpdateGenesisValidatorInfo;

/// Migration updating pool information those bakers where this is missing.
pub async fn run(
    tx: &mut sqlx::PgTransaction<'_>,
    endpoints: &[v2::Endpoint],
) -> anyhow::Result<SchemaVersion> {
    let latest_height: Option<i64> =
        sqlx::query_scalar("SELECT height FROM blocks ORDER BY height DESC LIMIT 1")
            .fetch_optional(tx.as_mut())
            .await?;
    let Some(latest_height) = latest_height else {
        // No blocks processed yet, meaning no data to migrate.
        return Ok(NEXT_SCHEMA_VERSION);
    };
    let latest_block = BlockIdentifier::AbsoluteHeight(u64::try_from(latest_height)?.into());
    let endpoint = endpoints.first().context(format!(
        "Migration '{}' must be provided access to a Concordium node",
        NEXT_SCHEMA_VERSION
    ))?;
    let mut client = v2::Client::new(endpoint.clone()).await?;

    {
        let latest_block_info = client.get_block_info(latest_block).await?.response;
        if latest_block_info.protocol_version < ProtocolVersion::P4 {
            // No data to migrate at this point.
            // The indexer will handle the data migration when reaching P4.
            return Ok(NEXT_SCHEMA_VERSION);
        }
    }
    type UnzippedInfo = (
        Vec<i64>,
        (
            Vec<BakerPoolOpenStatus>,
            (Vec<String>, (Vec<i64>, (Vec<i64>, Vec<i64>))),
        ),
    );

    let (ids, (statuses, (metadata_urls, (transaction_rates, (baking_rates, finalization_rates))))): UnzippedInfo = sqlx::query_scalar(
        "SELECT
             id
         FROM bakers
         WHERE
             open_status IS NULL
             OR metadata_url IS NULL
             OR transaction_commission IS NULL
             OR baking_commission IS NULL
             OR finalization_commission IS NULL"
    )
    .fetch(tx.as_mut())
    .map_err(anyhow::Error::from)
    .try_filter_map(|validator_id: i64| {
        let mut client = client.clone();
        async move {
            let baker_id = concordium_rust_sdk::types::AccountIndex::from(u64::try_from(validator_id)?).into();
            let pool_info = client.get_pool_info(latest_block, baker_id).await?.response;
            let Some(status) = pool_info.active_baker_pool_status else {
                return Ok(None);
            };
            let pool = status.pool_info;
            let status = BakerPoolOpenStatus::from(pool.open_status);
            let metadata_url = String::from(pool.metadata_url);
            let transaction_rate = i64::from(u32::from(PartsPerHundredThousands::from(
                pool.commission_rates.transaction,
            )));
            let baking_rate = i64::from(u32::from(PartsPerHundredThousands::from(
                pool.commission_rates.baking,
            )));
            let finalization_rate = i64::from(u32::from(PartsPerHundredThousands::from(
                pool.commission_rates.finalization,
            )));
            anyhow::Ok(Some((
                validator_id,
                (status,
                 (metadata_url,
                  (transaction_rate,
                   (baking_rate,
                    finalization_rate)))))))
        }
    }).try_collect().await?;

    sqlx::query(
        "UPDATE bakers SET
             open_status = status,
             metadata_url = url,
             transaction_commission = transaction,
             baking_commission = baking,
             finalization_commission = finalization
         FROM UNNEST(
             $1::BIGINT[],
             $2::pool_open_status[],
             $3::TEXT[],
             $4::BIGINT[],
             $5::BIGINT[],
             $6::BIGINT[]
         ) AS input(
             id,
             status,
             url,
             transaction,
             baking,
             finalization
         ) WHERE bakers.id = input.id",
    )
    .bind(ids.as_slice())
    .bind(statuses.as_slice())
    .bind(metadata_urls.as_slice())
    .bind(transaction_rates.as_slice())
    .bind(baking_rates.as_slice())
    .bind(finalization_rates.as_slice())
    .execute(tx.as_mut())
    .await?;

    Ok(NEXT_SCHEMA_VERSION)
}
