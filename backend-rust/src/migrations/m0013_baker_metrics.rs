use super::{SchemaVersion, Transaction};
use anyhow::Context;
use async_graphql::futures_util::StreamExt;
use concordium_rust_sdk::{
    types::AbsoluteBlockHeight,
    v2::{self, BlockIdentifier},
};
use sqlx::Executor;

pub async fn run(
    tx: &mut Transaction,
    endpoints: &[v2::Endpoint],
    next_schema_version: SchemaVersion,
) -> anyhow::Result<SchemaVersion> {
    tx.as_mut().execute(sqlx::raw_sql(include_str!("./m0013-baker-metrics.sql"))).await?;
    let endpoint = endpoints.first().context(format!(
        "Migration '{}' must be provided access to a Concordium node",
        next_schema_version
    ))?;
    let mut client = v2::Client::new(endpoint.clone()).await?;
    let result: Option<i64> =
        sqlx::query_scalar("SELECT height FROM blocks ORDER BY height DESC LIMIT 1")
            .fetch_optional(tx.as_mut())
            .await?;
    if let Some(height) = result {
        let block = BlockIdentifier::AbsoluteHeight(AbsoluteBlockHeight {
            height: height.try_into()?,
        });
        let mut genesis_bakers_count = 0;
        let mut stream = client.get_baker_list(block).await?.response;
        while let Some(_) = stream.next().await.transpose()? {
            genesis_bakers_count += 1;
        }
        sqlx::query(
            r#"
            INSERT INTO metrics_bakers (
              block_height,
              total_bakers_added,
              total_bakers_removed
            ) VALUES (
              0,
              $1,
              0
            )
            "#,
        )
        .bind(genesis_bakers_count)
        .execute(tx.as_mut())
        .await?;
        sqlx::query(
            r#"
                    INSERT INTO metrics_bakers (block_height, total_bakers_removed, total_bakers_added)
                    WITH block_events AS (
                    SELECT
                        t.block_height,
                        0 AS baker_removed_count,
                        COUNT(*) AS baker_added_count
                    FROM transactions t
                    CROSS JOIN LATERAL jsonb_array_elements(t.events) AS event(elem)
                    WHERE event.elem ? 'BakerAdded'
                    GROUP BY t.block_height
                    UNION ALL
                    SELECT
                        t.block_height,
                        COUNT(*) AS baker_removed_count,
                        0 AS baker_added_count
                    FROM transactions t
                    CROSS JOIN LATERAL jsonb_array_elements(t.events) AS event(elem)
                    WHERE event.elem ? 'BakerRemoved'
                    GROUP BY t.block_height
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

    Ok(SchemaVersion::BakerMetrics)
}
