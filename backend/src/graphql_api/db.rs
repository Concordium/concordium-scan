use crate::graphql_api::ApiResult;
use sqlx::{pool::PoolConnection, PgPool, Postgres};
use std::{future::Future, pin::Pin};

/// Execute the given closure with a database connection where
/// `force_custom_plan` has been set. This forces Postgres to make a plan takes
/// concrete arguments to prepared statements into account. See <https://www.postgresql.org/docs/current/sql-prepare.html>.
pub async fn with_force_custom_plan<T, F>(pool: &PgPool, execute: F) -> ApiResult<T>
where
    F: for<'a> FnOnce(
        &'a mut PoolConnection<Postgres>,
    ) -> Pin<Box<dyn Future<Output = ApiResult<T>> + 'a + Send>>, {
    let mut connection = pool.acquire().await?;
    let existing_plan_cache_mode: String =
        sqlx::query_scalar("show plan_cache_mode").fetch_one(connection.as_mut()).await?;
    sqlx::query("set plan_cache_mode = force_custom_plan").execute(connection.as_mut()).await?;
    let result = execute(&mut connection).await;
    sqlx::query(&format!("set plan_cache_mode = {}", existing_plan_cache_mode))
        .execute(connection.as_mut())
        .await?;

    result
}
