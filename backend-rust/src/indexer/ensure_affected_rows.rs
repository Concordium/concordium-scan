//! This module contains the extension trait `EnsureAffectedRows` adding
//! validation methods on the query result.

use std::ops::RangeInclusive;

/// Extension trait providing methods for validating number of affected rows of
/// a query result.
/// Only implemented for `sqlx::postgres::PgQueryResult`.
pub trait EnsureAffectedRows: Sized {
    /// Validate number of affected rows are exactly as expected for a query
    /// result.
    fn ensure_affected_rows(
        self,
        expected_count: u64,
    ) -> Result<Self, EnsureAffectedRowsExactError>;
    /// Validate number of affected rows are within an expected range for a
    /// query result.
    fn ensure_affected_rows_in_range(
        self,
        expected_range: RangeInclusive<u64>,
    ) -> Result<Self, EnsureAffectedRowsInRangeError>;

    /// Validate number of affected rows are exactly 1 for a query result.
    fn ensure_affected_one_row(self) -> Result<Self, EnsureAffectedRowsExactError> {
        self.ensure_affected_rows(1)
    }
}

impl EnsureAffectedRows for sqlx::postgres::PgQueryResult {
    fn ensure_affected_rows(self, expected: u64) -> Result<Self, EnsureAffectedRowsExactError> {
        let affected = self.rows_affected();
        if affected != expected {
            Err(EnsureAffectedRowsExactError {
                affected,
                expected,
            })
        } else {
            Ok(self)
        }
    }

    fn ensure_affected_rows_in_range(
        self,
        expected: RangeInclusive<u64>,
    ) -> Result<Self, EnsureAffectedRowsInRangeError> {
        let affected = self.rows_affected();
        if !expected.contains(&affected) {
            Err(EnsureAffectedRowsInRangeError {
                affected,
                expected,
            })
        } else {
            Ok(self)
        }
    }
}

#[derive(Debug, Copy, Clone, thiserror::Error)]
#[error(
    "Unexpected number of rows affected by query. Affected {affected} but expected exactly \
     {expected}"
)]
pub struct EnsureAffectedRowsExactError {
    affected: u64,
    expected: u64,
}

#[derive(Debug, Clone, thiserror::Error)]
#[error(
    "Unexpected number of rows affected by query. Affected {} but expected in range of [{}, {}]",
    affected,
    expected.start(),
    expected.end()
)]
pub struct EnsureAffectedRowsInRangeError {
    affected: u64,
    expected: RangeInclusive<u64>,
}
