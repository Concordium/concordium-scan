use anyhow::Context;
use async_graphql::{InputValueError, InputValueResult, Scalar, ScalarType, Value};

pub type Amount = i64; // TODO: should be UnsignedLong in graphQL
pub type Energy = i64; // TODO: should be UnsignedLong in graphQL
pub type DateTime = chrono::DateTime<chrono::Utc>; // TODO check format matches.
pub type BakerId = i64;
pub type BlockHeight = i64;
pub type BlockHash = String;
pub type TransactionHash = String;
pub type ModuleReference = String;
pub type TransactionIndex = i64;
pub type AccountIndex = i64;

pub type BigInteger = u64; // TODO check format.
pub type MetadataUrl = String;

#[derive(serde::Serialize, serde::Deserialize, derive_more::From)]
#[repr(transparent)]
#[serde(transparent)]
pub struct Decimal(rust_decimal::Decimal);
#[Scalar]
impl ScalarType for Decimal {
    fn parse(value: Value) -> InputValueResult<Self> {
        let Value::String(string) = value else {
            return Err(InputValueError::expected_type(value));
        };
        Ok(Self(string.parse()?))
    }

    fn to_value(&self) -> Value { Value::String(self.0.to_string()) }
}

impl From<concordium_rust_sdk::types::AmountFraction> for Decimal {
    fn from(fraction: concordium_rust_sdk::types::AmountFraction) -> Self {
        Self(concordium_rust_sdk::types::PartsPerHundredThousands::from(fraction).into())
    }
}

/// The UnsignedLong scalar type represents a unsigned 64-bit numeric
/// non-fractional value greater than or equal to 0.
#[derive(
    Clone,
    Copy,
    derive_more::Display,
    Debug,
    serde::Serialize,
    serde::Deserialize,
    derive_more::From,
    derive_more::FromStr,
)]
#[repr(transparent)]
#[serde(transparent)]
pub struct UnsignedLong(pub u64);
#[Scalar]
impl ScalarType for UnsignedLong {
    fn parse(value: Value) -> InputValueResult<Self> {
        let Value::Number(number) = &value else {
            return Err(InputValueError::expected_type(value));
        };
        if let Some(v) = number.as_u64() {
            Ok(Self(v))
        } else {
            Err(InputValueError::expected_type(value))
        }
    }

    fn to_value(&self) -> Value { Value::Number(self.0.into()) }
}

impl TryFrom<i64> for UnsignedLong {
    type Error = <u64 as TryFrom<i64>>::Error;

    fn try_from(number: i64) -> Result<Self, Self::Error> { Ok(UnsignedLong(number.try_into()?)) }
}

/// The `Long` scalar type represents non-fractional signed whole 64-bit numeric
/// values. Long can represent values between -(2^63) and 2^63 - 1.
#[derive(serde::Serialize, serde::Deserialize, derive_more::From)]
#[repr(transparent)]
#[serde(transparent)]
pub struct Long(i64);
#[Scalar]
impl ScalarType for Long {
    fn parse(value: Value) -> InputValueResult<Self> {
        let Value::Number(number) = &value else {
            return Err(InputValueError::expected_type(value));
        };
        if let Some(v) = number.as_i64() {
            Ok(Self(v))
        } else {
            Err(InputValueError::expected_type(value))
        }
    }

    fn to_value(&self) -> Value { Value::Number(self.0.into()) }
}

/// The `TimeSpan` scalar represents an ISO-8601 compliant duration type.
#[derive(serde::Serialize, serde::Deserialize, Clone)]
#[repr(transparent)]
#[serde(try_from = "String", into = "String")]
pub struct TimeSpan(pub chrono::Duration);
#[Scalar]
impl ScalarType for TimeSpan {
    fn parse(value: Value) -> InputValueResult<Self> {
        let Value::String(string) = value else {
            return Err(InputValueError::expected_type(value));
        };
        Ok(Self::try_from(string)?)
    }

    fn to_value(&self) -> Value { Value::String(self.0.to_string()) }
}
impl TryFrom<String> for TimeSpan {
    type Error = anyhow::Error;

    fn try_from(value: String) -> Result<Self, Self::Error> {
        let duration: iso8601_duration::Duration =
            value.parse().map_err(|_| anyhow::anyhow!("Invalid duration, expected ISO-8601"))?;
        Ok(Self(duration.to_chrono().context("Failed to construct duration")?))
    }
}
impl From<TimeSpan> for String {
    fn from(time: TimeSpan) -> Self { time.0.to_string() }
}
impl From<chrono::Duration> for TimeSpan {
    fn from(duration: chrono::Duration) -> Self { TimeSpan(duration) }
}
