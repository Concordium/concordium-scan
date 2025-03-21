use super::{SchemaVersion, Transaction};
use crate::transaction_event::events_from_summary;
use anyhow::Context;
use async_graphql::futures_util::StreamExt;
use concordium_rust_sdk::{
    types::{AbsoluteBlockHeight, BlockItemSummaryDetails},
    v2::{self, BlockIdentifier},
};
use sqlx::Executor;

/// Performs a migration that creates and populates the baker metrics table.
pub async fn run(
    tx: &mut Transaction,
    endpoints: &[v2::Endpoint],
    next_schema_version: SchemaVersion,
) -> anyhow::Result<SchemaVersion> {
    tx.as_mut().execute(sqlx::raw_sql(include_str!("m0014-baker-metrics.sql"))).await?;
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
    .fetch(tx.as_mut());

    while let row = rows.await? {
        let mut block_summary = client
            .get_block_transaction_events(AbsoluteBlockHeight {
                height: row.block_height.try_into()?,
            })
            .await?
            .response;
        while let Some(summary) = block_summary.next().await.transpose()? {
            let BlockItemSummaryDetails::Update(update) = summary.details else {
                continue
            };
            sqlx::query(
                "
                UPDATE transactions
                SET events = $1::jsonb
                WHERE index = $2;
            ",
            )
            .bind(serde_json::to_value(events_from_summary(update.payload.into())?)?)
            .bind(summary.index)
            .execute(tx.as_mut())
            .await?;
        }
    }

    todo!()
}
