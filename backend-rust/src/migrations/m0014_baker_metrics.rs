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
    let is_genesis_created = sqlx::query("SELECT height FROM blocks LIMIT 1")
        .fetch_optional(tx.as_mut())
        .await?
        .is_some();
    if is_genesis_created {
        let block_identifier = BlockIdentifier::AbsoluteHeight(AbsoluteBlockHeight {
            height: 0,
        });
        let genesis_bakers_count: i64 =
            client.get_baker_list(block_identifier).await?.response.count().await.try_into()?;
        sqlx::query(
            "
            INSERT INTO metrics_bakers (
              block_height,
              total_bakers_added,
              total_bakers_removed
            ) VALUES (
              0,
              $1,
              0
            )
            ",
        )
        .bind(genesis_bakers_count)
        .execute(tx.as_mut())
        .await?;
        sqlx::query(
            r#"
                    INSERT INTO metrics_bakers (block_height, total_bakers_removed, total_bakers_added)
                    WITH block_events AS (
                    SELECT
                        block_height,
                        COUNT(*) FILTER (WHERE events @> '[{"BakerRemoved": {}}]'::JSONB) AS baker_removed_count,
                        COUNT(*) FILTER (WHERE events @> '[{"BakerAdded": {}}]'::JSONB) AS baker_added_count
                    FROM transactions
                    WHERE
                        type_account IN ('RemoveBaker', 'AddBaker')
                        OR (
                            type_account IN ('ConfigureBaker', 'ConfigureDelegation')
                            AND (events @> '[{"BakerRemoved": {}}]'::JSONB
                                OR events @> '[{"BakerAdded": {}}]'::JSONB
                            )
                        )
                    GROUP BY block_height
                    ORDER BY block_height
                    )
                    SELECT
                      block_height,
                      SUM(baker_removed_count) OVER (ORDER BY block_height ASC) AS cumulative_baker_removed,
                      SUM(baker_added_count) OVER (ORDER BY block_height ASC) + $1 AS cumulative_baker_added
                    FROM block_events;
            "#,
        )
            .bind(genesis_bakers_count)
            .execute(tx.as_mut())
            .await?;
    };

    Ok(next_schema_version)
}
