use super::{SchemaVersion, Transaction};
use crate::transaction_event::{chain_update::ChainUpdatePayload, events_from_summary};
use anyhow::Context;
use async_graphql::futures_util::StreamExt;
use concordium_rust_sdk::{
    types::{AbsoluteBlockHeight, BlockItemSummaryDetails, TransactionType},
    v2::{self},
};

/// Performs a migration that creates and populates the baker metrics table.
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
            WHERE type_account IN ('TransferWithSchedule', 'TransferWithScheduleAndMemo')
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
            let BlockItemSummaryDetails::AccountTransaction(update) = &summary.details else {
                continue
            };
            if !matches!(
                update.transaction_type(),
                Some(
                    TransactionType::TransferWithSchedule
                        | TransactionType::TransferWithScheduleAndMemo
                )
            ) {
                continue;
            }
            let payload = events_from_summary(summary.details)?;
            let transaction_index: i64 = summary.index.index.try_into()?;
            sqlx::query(
                "
                UPDATE transactions
                SET events = $1::jsonb
                WHERE index = $2;
            ",
            )
            .bind(serde_json::to_value(payload)?)
            .bind(transaction_index)
            .execute(tx.as_mut())
            .await?;
        }
    }
    Ok(next_schema_version)
}
