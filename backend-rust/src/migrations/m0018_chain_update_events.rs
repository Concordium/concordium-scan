use super::{SchemaVersion, Transaction};
use anyhow::Context;
use async_graphql::futures_util::StreamExt;
use concordium_rust_sdk::{
    types::AbsoluteBlockHeight,
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
    let events = client.get_block_transaction_events();

    todo!()
}
