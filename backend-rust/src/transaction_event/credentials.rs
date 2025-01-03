use crate::{address::AccountAddress, scalar_types::Byte};
use async_graphql::SimpleObject;

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
