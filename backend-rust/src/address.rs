use async_graphql::{ComplexObject, SimpleObject, Union};

use crate::{graphql_api::ApiResult, types::UnsignedLong};

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

pub type ContractIndex = UnsignedLong; // TODO check format.

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
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

#[derive(Union, serde::Serialize, serde::Deserialize)]
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
