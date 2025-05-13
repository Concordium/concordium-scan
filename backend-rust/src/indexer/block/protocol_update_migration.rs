use crate::{
    indexer::{block_preprocessor::BlockData, ensure_affected_rows::EnsureAffectedRows},
    transaction_event::baker::BakerPoolOpenStatus,
};
use anyhow::Context;
use concordium_rust_sdk::{
    types::{self as sdk_types, PartsPerHundredThousands},
    v2,
};
use futures::TryStreamExt;

/// Represents a data migration due to an update of the protocol.
#[derive(Debug)]
pub enum ProtocolUpdateMigration {
    P4(P4ProtocolUpdateMigration),
}
impl ProtocolUpdateMigration {
    pub async fn prepare(
        node_client: &mut v2::Client,
        data: &BlockData,
    ) -> anyhow::Result<Option<Self>> {
        if data.block_info.era_block_height != sdk_types::BlockHeight::from(0) {
            // Not the first block in a new protocol version (era).
            return Ok(None);
        }
        let migration = match data.block_info.protocol_version {
            sdk_types::ProtocolVersion::P4 => Some(ProtocolUpdateMigration::P4(
                P4ProtocolUpdateMigration::prepare(node_client, data).await?,
            )),
            _ => None,
        };
        Ok(migration)
    }

    pub async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        match self {
            Self::P4(migration) => {
                migration.save(tx).await.context("Failed Protocol version 4 data migration")
            }
        }
    }
}

/// Data migration for the first block in Concordium protocol version 4.
#[derive(Debug)]
pub struct P4ProtocolUpdateMigration {
    baker_ids:                     Vec<i64>,
    open_statuses:                 Vec<BakerPoolOpenStatus>,
    metadata_urls:                 Vec<String>,
    transaction_commission_rates:  Vec<i64>,
    baking_commission_rates:       Vec<i64>,
    finalization_commission_rates: Vec<i64>,
}
impl P4ProtocolUpdateMigration {
    async fn prepare(node_client: &mut v2::Client, data: &BlockData) -> anyhow::Result<Self> {
        let block_height = data.finalized_block_info.height;
        let (
            baker_ids,
            (
                open_statuses,
                (
                    metadata_urls,
                    (
                        transaction_commission_rates,
                        (baking_commission_rates, finalization_commission_rates),
                    ),
                ),
            ),
        ) = node_client
            .get_baker_list(block_height)
            .await?
            .response
            .map_err(anyhow::Error::from)
            .and_then(|baker_id| {
                let mut client = node_client.clone();
                async move {
                    let pool_info = client.get_pool_info(block_height, baker_id).await?.response;
                    let status = pool_info
                        .active_baker_pool_status
                        .context("Unexpected missing pool info during P4 migration")?;
                    let pool = status.pool_info;
                    let validator_id: i64 = baker_id.id.index.try_into()?;
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

                    anyhow::Ok((
                        validator_id,
                        (
                            status,
                            (metadata_url, (transaction_rate, (baking_rate, finalization_rate))),
                        ),
                    ))
                }
            })
            .try_collect()
            .await?;

        Ok(Self {
            baker_ids,
            open_statuses,
            metadata_urls,
            transaction_commission_rates,
            baking_commission_rates,
            finalization_commission_rates,
        })
    }

    async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        sqlx::query!(
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
            self.baker_ids.as_slice(),
            self.open_statuses.as_slice() as &[BakerPoolOpenStatus],
            self.metadata_urls.as_slice(),
            self.transaction_commission_rates.as_slice(),
            self.baking_commission_rates.as_slice(),
            self.finalization_commission_rates.as_slice()
        )
        .execute(tx.as_mut())
        .await?
        .ensure_affected_rows(self.baker_ids.len().try_into()?)?;
        Ok(())
    }
}
