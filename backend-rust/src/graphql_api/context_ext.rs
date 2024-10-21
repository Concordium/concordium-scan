//! Simple extension trait for the GraphQL context to make it easier to extract certain data.

use async_graphql::Context;
use sqlx::PgPool;

use super::{ApiError, ApiResult, ApiServiceConfig};

pub(crate) trait ContextExt {
    /// Get the database pool from the context.
    fn pool(&self) -> ApiResult<&PgPool>;

    /// Get service configuration from the context.
    fn config(&self) -> ApiResult<&ApiServiceConfig>;
}

impl ContextExt for Context<'_> {
    fn pool(&self) -> ApiResult<&PgPool> {
        self.data::<PgPool>().map_err(ApiError::NoDatabasePool)
    }

    fn config(&self) -> ApiResult<&ApiServiceConfig> {
        self.data::<ApiServiceConfig>().map_err(ApiError::NoServiceConfig)
    }
}
