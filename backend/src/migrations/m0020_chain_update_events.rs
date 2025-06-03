use super::SchemaVersion;
use crate::transaction_event::events_from_summary;
use anyhow::{anyhow, Context};
use async_graphql::futures_util::StreamExt;
use chrono::{DateTime, Utc};
use concordium_rust_sdk::{
    types::{AbsoluteBlockHeight, BlockItemSummaryDetails},
    v2::{self},
};

/// Performs a migration that alters the events types for transactions of type
/// Update
pub async fn run(
    tx: &mut sqlx::PgTransaction<'_>,
    endpoints: &[v2::Endpoint],
    next_schema_version: SchemaVersion,
) -> anyhow::Result<SchemaVersion> {
    let endpoint = endpoints.first().context(format!(
        "Migration '{}' must be provided access to a Concordium node",
        next_schema_version
    ))?;

    let mut client = v2::Client::new(endpoint.clone()).await?;

    let rows = sqlx::query(
        "
            SELECT
                block_height,
                (SELECT slot_time FROM blocks WHERE height = block_height) as block_slot_time,
                COUNT(*) AS update_count
            FROM transactions
            WHERE type = 'Update'
            GROUP BY block_height
            ",
    )
    .fetch_all(tx.as_mut())
    .await?;

    for row in rows {
        let height: i64 = sqlx::Row::try_get(&row, "block_height")?;
        let mut expected_rows_to_be_affected_count: i64 = sqlx::Row::try_get(&row, "update_count")?;
        let block_slot_time: DateTime<Utc> = sqlx::Row::try_get(&row, "block_slot_time")?;
        let mut block_summary = client
            .get_block_transaction_events(AbsoluteBlockHeight {
                height: height.try_into()?,
            })
            .await?
            .response;
        while let Some(summary) = block_summary.next().await.transpose()? {
            let BlockItemSummaryDetails::Update(_) = &summary.details else {
                continue;
            };
            let events = events_from_summary(summary.details, block_slot_time)?;
            let json = serde_json::to_value(events)?;
            let hash = summary.hash.to_string();
            sqlx::query(
                "
                UPDATE transactions
                SET events = $1::jsonb
                WHERE hash = $2;
            ",
            )
            .bind(json)
            .bind(hash)
            .execute(tx.as_mut())
            .await?;
            expected_rows_to_be_affected_count -= 1;
        }
        let is_misalignment_between_expected_and_actual_rows_updated_count =
            expected_rows_to_be_affected_count != 0;
        if is_misalignment_between_expected_and_actual_rows_updated_count {
            return Err(anyhow!(
                "There is a misalignment between expected number of rows to be updated and actual \
                 number of rows to have become updated"
            ));
        }
    }
    Ok(next_schema_version)
}
