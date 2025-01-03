use crate::address::AccountAddress;
use async_graphql::{InputValueError, InputValueResult, Scalar, ScalarType, SimpleObject, Value};

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

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct AccountCreated {
    pub account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct CredentialDeployed {
    pub reg_id:          String,
    pub account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct CredentialKeysUpdated {
    pub cred_id: String,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct CredentialsUpdated {
    pub account_address:  AccountAddress,
    pub new_cred_ids:     Vec<String>,
    pub removed_cred_ids: Vec<String>,
    pub new_threshold:    Byte,
}
