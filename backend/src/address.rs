use std::fmt;

use crate::{graphql_api::ApiResult, scalar_types::UnsignedLong};
use async_graphql::{ComplexObject, SimpleObject, Union};

#[derive(SimpleObject, Clone, serde::Serialize, serde::Deserialize)]
pub struct AccountAddress {
    as_string: String,
}

impl From<concordium_rust_sdk::common::types::AccountAddress> for AccountAddress {
    fn from(address: concordium_rust_sdk::common::types::AccountAddress) -> Self {
        address.to_string().into()
    }
}

impl From<String> for AccountAddress {
    fn from(as_string: String) -> Self {
        Self {
            as_string,
        }
    }
}
// TODO: will remove this when we will read from the db (this is for creating a
// hashset of addresses to find unique as we are reading from json data)
impl fmt::Display for AccountAddress {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result { write!(f, "{}", self.as_string) }
}

impl fmt::Debug for AccountAddress {
    fn fmt(&self, f: &mut fmt::Formatter) -> fmt::Result { write!(f, "{}", self.as_string) }
}

pub type ContractIndex = UnsignedLong; // TODO check format.

#[derive(Debug, SimpleObject, serde::Serialize, serde::Deserialize, Clone, Copy)]
#[graphql(complex)]
pub struct ContractAddress {
    pub index:     ContractIndex,
    pub sub_index: ContractIndex,
}
#[ComplexObject]
impl ContractAddress {
    async fn as_string(&self) -> String {
        concordium_rust_sdk::types::ContractAddress::new(self.index.0, self.sub_index.0).to_string()
    }
}
impl ContractAddress {
    pub fn new(index: i64, sub_index: i64) -> ApiResult<Self> {
        Ok(Self {
            index:     u64::try_from(index)?.into(),
            sub_index: u64::try_from(sub_index)?.into(),
        })
    }
}

impl From<concordium_rust_sdk::types::ContractAddress> for ContractAddress {
    fn from(value: concordium_rust_sdk::types::ContractAddress) -> Self {
        Self {
            index:     value.index.into(),
            sub_index: value.subindex.into(),
        }
    }
}

#[derive(Union, serde::Serialize, serde::Deserialize, Clone)]
pub enum Address {
    ContractAddress(ContractAddress),
    AccountAddress(AccountAddress),
}

impl From<concordium_rust_sdk::types::Address> for Address {
    fn from(value: concordium_rust_sdk::types::Address) -> Self {
        use concordium_rust_sdk::types::Address as Addr;
        match value {
            Addr::Account(a) => Address::AccountAddress(a.into()),
            Addr::Contract(c) => Address::ContractAddress(c.into()),
        }
    }
}

impl From<concordium_rust_sdk::common::types::AccountAddress> for Address {
    fn from(address: concordium_rust_sdk::common::types::AccountAddress) -> Self {
        Address::AccountAddress(address.into())
    }
}
