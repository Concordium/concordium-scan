use super::{SchemaVersion, Transaction};
use crate::transaction_event::{events_from_summary};
use anyhow::Context;
use async_graphql::futures_util::StreamExt;
use concordium_rust_sdk::{
    types::{AbsoluteBlockHeight, BlockItemSummaryDetails},
    v2::{self},
};

/// Performs a migration that alters the events types for transactions of type
/// Update
pub async fn run(
    tx: &mut Transaction,
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
        let mut block_summary = client
            .get_block_transaction_events(AbsoluteBlockHeight {
                height: height.try_into()?,
            })
            .await?
            .response;
        while let Some(summary) = block_summary.next().await.transpose()? {
            let BlockItemSummaryDetails::Update(_) = &summary.details else {
                continue
            };
            let events = events_from_summary(summary.details)?;
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
        }
    }
    Ok(next_schema_version)
}
