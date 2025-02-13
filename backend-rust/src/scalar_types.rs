use anyhow::Context;
use async_graphql::{scalar, InputValueError, InputValueResult, Scalar, ScalarType, Value};
use bigdecimal::BigDecimal;
use num_traits::{FromPrimitive, ToPrimitive};
use std::fmt;

pub type Amount = UnsignedLong;
pub type TokenId = String;
pub type Energy = i64; // TODO: should be UnsignedLong in graphQL
pub type DateTime = chrono::DateTime<chrono::Utc>; // TODO check format matches.
pub type BakerId = Long;
pub type BlockHeight = i64;
pub type BlockHash = String;
pub type TransactionHash = String;
pub type ModuleReference = String;
pub type TransactionIndex = i64;
pub type AccountIndex = i64;
pub type MetadataUrl = String;

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
#[display("{_0}")]
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

impl From<UnsignedLong> for bigdecimal::BigDecimal {
    fn from(UnsignedLong(v): UnsignedLong) -> Self { v.into() }
}

impl TryFrom<i64> for UnsignedLong {
    type Error = <u64 as TryFrom<i64>>::Error;

    fn try_from(number: i64) -> Result<Self, Self::Error> { Ok(UnsignedLong(number.try_into()?)) }
}

impl TryFrom<UnsignedLong> for i64 {
    type Error = <i64 as TryFrom<u64>>::Error;

    fn try_from(number: UnsignedLong) -> Result<Self, Self::Error> { number.0.try_into() }
}

impl From<concordium_rust_sdk::common::types::Amount> for UnsignedLong {
    fn from(value: concordium_rust_sdk::common::types::Amount) -> Self { Self(value.micro_ccd()) }
}

/// The `Long` scalar type represents non-fractional signed whole 64-bit numeric
/// values. Long can represent values between -(2^63) and 2^63 - 1.
#[derive(
    Debug,
    serde::Serialize,
    serde::Deserialize,
    Clone,
    Copy,
    derive_more::From,
    derive_more::FromStr,
    derive_more::Into,
    derive_more::Display,
)]
#[repr(transparent)]
#[serde(transparent)]
pub struct Long(pub i64);
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
impl TryFrom<u64> for Long {
    type Error = std::num::TryFromIntError;

    fn try_from(value: u64) -> Result<Self, Self::Error> { Ok(Self(value.try_into()?)) }
}
impl TryFrom<concordium_rust_sdk::types::BakerId> for Long {
    type Error = std::num::TryFromIntError;

    fn try_from(value: concordium_rust_sdk::types::BakerId) -> Result<Self, Self::Error> {
        value.id.index.try_into()
    }
}

#[derive(serde::Serialize, serde::Deserialize, derive_more::From)]
#[repr(transparent)]
#[serde(transparent)]
pub struct Decimal(pub rust_decimal::Decimal);
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

impl TryFrom<&BigDecimal> for Decimal {
    type Error = anyhow::Error;

    fn try_from(value: &BigDecimal) -> Result<Self, Self::Error> {
        let float_value =
            value.to_f64().ok_or_else(|| anyhow::anyhow!("Failed to convert BigDecimal to f64"))?;

        let decimal = rust_decimal::Decimal::from_f64(float_value)
            .ok_or_else(|| anyhow::anyhow!("Failed to convert f64 to rust_decimal::Decimal"))?;

        Ok(Decimal(decimal))
    }
}

/// The `TimeSpan` scalar represents an ISO-8601 compliant duration type.
#[derive(serde::Serialize, serde::Deserialize, Clone)]
#[repr(transparent)]
#[serde(try_from = "String", into = "String")]
pub struct TimeSpan(pub chrono::Duration);

impl fmt::Display for TimeSpan {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        let total_secs = self.0.num_seconds();
        let days = total_secs / 86_400; // 86,400 seconds in a day
        let remainder = total_secs % 86_400;
        let hours = remainder / 3600;
        let remainder = remainder % 3600;
        let minutes = remainder / 60;
        let seconds = remainder % 60;

        let mut s = String::new();
        s.push('P');

        if days > 0 {
            s.push_str(&format!("{}D", days));
        }

        if days == 0 || hours > 0 || minutes > 0 || seconds > 0 {
            s.push('T');
            if hours > 0 {
                s.push_str(&format!("{}H", hours));
            }
            if minutes > 0 {
                s.push_str(&format!("{}M", minutes));
            }
            if seconds > 0 {
                s.push_str(&format!("{}S", seconds));
            }
            if s.ends_with('T') {
                s.push_str("0S");
            }
        }
        write!(f, "{}", s)
    }
}

#[Scalar]
impl ScalarType for TimeSpan {
    fn parse(value: Value) -> InputValueResult<Self> {
        let Value::String(string) = value else {
            return Err(InputValueError::expected_type(value));
        };
        Ok(Self::try_from(string)?)
    }

    fn to_value(&self) -> Value { Value::String(self.to_string()) }
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

/// The `BigInteger` scalar represents an `BigDecimal` compliant type.
#[derive(serde::Serialize, serde::Deserialize, Clone)]
#[repr(transparent)]
#[serde(try_from = "String", into = "String")]
pub struct BigInteger(pub bigdecimal::BigDecimal);

scalar!(BigInteger);

impl TryFrom<String> for BigInteger {
    type Error = anyhow::Error;

    fn try_from(value: String) -> Result<Self, Self::Error> {
        let big_decimal: bigdecimal::BigDecimal =
            value.parse().map_err(|err| anyhow::anyhow!("Invalid BigDecimal format: {}", err))?;
        Ok(Self(big_decimal))
    }
}
impl From<BigInteger> for String {
    fn from(value: BigInteger) -> Self { value.0.to_string() }
}
impl From<bigdecimal::BigDecimal> for BigInteger {
    fn from(value: bigdecimal::BigDecimal) -> Self { BigInteger(value) }
}

#[derive(serde::Serialize, serde::Deserialize, derive_more::From)]
#[repr(transparent)]
#[serde(transparent)]
pub struct Byte(pub u8);
#[Scalar]
impl ScalarType for Byte {
    fn parse(value: Value) -> InputValueResult<Self> {
        let Value::Number(number) = &value else {
            return Err(InputValueError::expected_type(value));
        };
        let Some(v) = number.as_u64() else {
            return Err(InputValueError::expected_type(value));
        };

        if let Ok(v) = u8::try_from(v) {
            Ok(Self(v))
        } else {
            Err(InputValueError::expected_type(value))
        }
    }

    fn to_value(&self) -> Value { Value::Number(self.0.into()) }
}

#[cfg(test)]
mod tests {
    use crate::scalar_types::TimeSpan;
    use chrono::Duration;

    #[test]
    fn test_zero_duration() {
        let d = TimeSpan(Duration::seconds(0));
        assert_eq!(d.to_string(), "PT0S");
    }

    #[test]
    fn test_two_minutes() {
        let d = TimeSpan(Duration::seconds(120));
        assert_eq!(d.to_string(), "PT2M");
    }

    #[test]
    fn test_an_hour() {
        let d = TimeSpan(Duration::seconds(3600));
        assert_eq!(d.to_string(), "PT1H");
    }

    #[test]
    fn test_six_hours() {
        let d = TimeSpan(Duration::seconds(3600 * 6));
        assert_eq!(d.to_string(), "PT6H");
    }

    // P15D
    #[test]
    fn test_1d() {
        let d = TimeSpan(Duration::seconds(3600 * 24));
        assert_eq!(d.to_string(), "P1D");
    }

    #[test]
    fn test_15d() {
        let d = TimeSpan(Duration::seconds(3600 * 24 * 15));
        assert_eq!(d.to_string(), "P15D");
    }
}
