use std::sync::Arc;

use crate::graphql_api::{AccountStatementEntryType, ApiServiceConfig};
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

#[derive(Debug, Clone)]
pub struct Service {
    pool:   PgPool,
    config: Arc<ApiServiceConfig>,
}

const EXPORT_MAX_DAYS: i64 = 31;

impl Service {
    pub fn new(pool: PgPool, config: Arc<ApiServiceConfig>) -> Self {
        Self {
            pool,
            config,
        }
    }

    pub fn as_router(self) -> Router {
        let cors_layer = CorsLayer::new()
            .allow_origin(Any)  // Open access to selected route
            .allow_methods(Any)
            .allow_headers(Any);
        Router::new()
            .route("/rest/balance-statistics/latest", get(Self::latest_balance_statistics))
            .route("/rest/export/statement", get(Self::export_account_statements))
            .layer(cors_layer)
            .with_state(self)
    }

    async fn latest_balance_statistics(
        Query(params): Query<LatestBalanceStatistics>,
        State(state): State<Self>,
    ) -> ApiResult<String> {
        let amount = match params.field {
            Balance::TotalAmount | Balance::TotalAmountUnlocked => {
                sqlx::query_scalar!("SELECT total_amount FROM blocks ORDER BY height DESC LIMIT 1")
                    .fetch_optional(&state.pool)
                    .await?
            }
            Balance::TotalAmountCirculating => {
                let non_circulating_accounts = state
                    .config
                    .non_circulating_account
                    .iter()
                    .map(|a| a.to_string())
                    .collect::<Vec<_>>();
                sqlx::query_scalar!(
                    "WITH non_circulating_accounts AS (
                         SELECT
                             COALESCE(SUM(amount), 0)::BIGINT AS total_amount
                         FROM accounts
                         WHERE address = ANY($1)
                     )
                     SELECT
                         (blocks.total_amount
                             - non_circulating_accounts.total_amount)::BIGINT
                     FROM blocks, non_circulating_accounts
                     ORDER BY height DESC
                     LIMIT 1",
                    &non_circulating_accounts
                )
                .fetch_one(&state.pool)
                .await?
            }
        };
        let amount = u64::try_from(amount.ok_or(ApiError::NotFound)?)?;
        let amount = match params.unit {
            Unit::Ccd => amount / 1_000_000,
            Unit::MicroCcd => amount,
        };
        Ok(amount.to_string())
    }

    async fn export_account_statements(
        Query(params): Query<ExportAccountStatement>,
        State(state): State<Self>,
    ) -> ApiResult<(AppendHeaders<[(HeaderName, String); 2]>, String)> {
        let to = params.to_time.unwrap_or_else(Utc::now);
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
                AND slot_time >= $2
                AND slot_time <= $3
            ORDER BY slot_time DESC"#,
            params.account_address.to_string(),
            from,
            to
        )
        .fetch(&state.pool);
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

#[derive(Debug, serde::Deserialize)]
#[serde(rename_all = "lowercase")]
struct LatestBalanceStatistics {
    field: Balance,
    unit:  Unit,
}
#[derive(Debug, serde::Deserialize)]
#[serde(rename_all = "lowercase")]
#[allow(clippy::enum_variant_names)]
enum Balance {
    TotalAmount,
    TotalAmountCirculating,
    TotalAmountUnlocked,
}
#[derive(Debug, serde::Deserialize)]
#[serde(rename_all = "lowercase")]
enum Unit {
    Ccd,
    MicroCcd,
}

#[derive(Debug, thiserror::Error, Clone)]
enum ApiError {
    #[error("Information was not found.")]
    NotFound,
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
            ApiError::NotFound => StatusCode::NOT_FOUND,
        };
        (status, self.to_string()).into_response()
    }
}
