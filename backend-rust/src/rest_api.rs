use std::sync::Arc;

use crate::graphql_api::AccountStatementEntryType;
use axum::{
    extract::{Query, State},
    http::HeaderName,
    response::{AppendHeaders, IntoResponse},
    routing::get,
    Router,
};
use chrono::{DateTime, TimeDelta, Utc};
use concordium_rust_sdk::{common::types::Amount, id::types::AccountAddress};
use futures::TryStreamExt as _;
use reqwest::StatusCode;
use sqlx::PgPool;
use tower_http::cors::{Any, CorsLayer};

pub struct Service {
    pool: PgPool,
}

const EXPORT_MAX_DAYS: i64 = 31;

impl Service {
    pub fn new(pool: PgPool) -> Self {
        Self {
            pool,
        }
    }

    pub fn as_router(self) -> Router {
        let cors_layer = CorsLayer::new()
            .allow_origin(Any)  // Open access to selected route
            .allow_methods(Any)
            .allow_headers(Any);
        Router::new()
            .route("/rest/export/statement", get(Self::export_account_statements))
            .layer(cors_layer)
            .with_state(self.pool)
    }

    async fn export_account_statements(
        Query(params): Query<ExportAccountStatement>,
        State(pool): State<PgPool>,
    ) -> ApiResult<(AppendHeaders<[(HeaderName, String); 2]>, String)> {
        let to = params.to_time.unwrap_or_else(|| chrono::Utc::now());
        let from = params.from_time.unwrap_or_else(|| to - TimeDelta::days(EXPORT_MAX_DAYS));
        if to - from > TimeDelta::days(EXPORT_MAX_DAYS) {
            return Err(ApiError::ExceedsMaxAllowed);
        }
        let mut rows = sqlx::query_as!(
            ExportAccountStatementEntry,
            r#"SELECT
                blocks.slot_time as timestamp,
                account_statements.amount,
                account_statements.account_balance,
                entry_type as "entry_type: AccountStatementEntryType"
            FROM accounts
                JOIN account_statements
                    ON accounts.index = account_statements.account_index
                JOIN blocks
                    ON blocks.height = account_statements.block_height
            WHERE
                accounts.address = $1
                AND slot_time > $2
                AND slot_time < $3
            ORDER BY slot_time DESC"#,
            params.account_address.to_string(),
            from,
            to
        )
        .fetch(&pool);
        let mut csv = String::from("Time,Amount (CCD),Balance (CCD),Label\n");
        while let Some(row) = rows.try_next().await? {
            let amount = Amount::from_micro_ccd(row.amount.try_into()?);
            let account_balance = Amount::from_micro_ccd(row.account_balance.try_into()?);
            csv.push_str(
                format!(
                    "{},{},{},{}\n",
                    row.timestamp.to_rfc3339_opts(chrono::SecondsFormat::Secs, true),
                    amount,
                    account_balance,
                    row.entry_type
                )
                .as_str(),
            )
        }
        let filename = format!(
            "statement-{}_{}-{}.csv",
            params.account_address,
            from.to_rfc3339_opts(chrono::SecondsFormat::Secs, true),
            to.to_rfc3339_opts(chrono::SecondsFormat::Secs, true)
        );
        let headers = AppendHeaders([
            (axum::http::header::CONTENT_TYPE, "text/csv; charset=utf-8".to_string()),
            (
                axum::http::header::CONTENT_DISPOSITION,
                format!("attachment; filename=\"{}\"", filename),
            ),
        ]);
        Ok((headers, csv))
    }
}

#[derive(Debug, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
struct ExportAccountStatement {
    account_address: AccountAddress,
    from_time:       Option<DateTime<Utc>>,
    to_time:         Option<DateTime<Utc>>,
}

struct ExportAccountStatementEntry {
    timestamp:       DateTime<Utc>,
    amount:          i64,
    account_balance: i64,
    entry_type:      AccountStatementEntryType,
}

#[derive(Debug, thiserror::Error, Clone)]
enum ApiError {
    #[error("Chosen time span exceeds max allowed days: '{EXPORT_MAX_DAYS}'")]
    ExceedsMaxAllowed,
    #[error("Internal error (FailedDatabaseQuery): {0}")]
    FailedDatabaseQuery(Arc<sqlx::Error>),
    #[error("Invalid integer: {0}")]
    InvalidInt(#[from] std::num::TryFromIntError),
}
impl From<sqlx::Error> for ApiError {
    fn from(value: sqlx::Error) -> Self { ApiError::FailedDatabaseQuery(Arc::new(value)) }
}

type ApiResult<A> = Result<A, ApiError>;

impl IntoResponse for ApiError {
    fn into_response(self) -> axum::response::Response {
        let status = match self {
            ApiError::ExceedsMaxAllowed => StatusCode::BAD_REQUEST,
            ApiError::FailedDatabaseQuery(_) => StatusCode::INTERNAL_SERVER_ERROR,
            ApiError::InvalidInt(_) => StatusCode::INTERNAL_SERVER_ERROR,
        };
        (status, self.to_string()).into_response()
    }
}
