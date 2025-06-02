use crate::{
    address::{AccountAddress, Address},
    connection::connection_from_slice,
    decoded_text::DecodedText,
    graphql_api::{ApiResult, InternalError},
    scalar_types::{Amount, DateTime, UnsignedLong},
};
use async_graphql::{connection::Connection, ComplexObject, SimpleObject};
use tracing::error;

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct Transferred {
    pub amount: Amount,
    pub from:   Address,
    pub to:     Address,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct AmountAddedByDecryption {
    pub amount:          Amount,
    pub account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct EncryptedAmountsRemoved {
    pub account_address:      AccountAddress,
    pub new_encrypted_amount: String,
    pub input_amount:         String,
    pub up_to_index:          u64,
}

impl TryFrom<concordium_rust_sdk::types::EncryptedAmountRemovedEvent> for EncryptedAmountsRemoved {
    type Error = anyhow::Error;

    fn try_from(
        removed: concordium_rust_sdk::types::EncryptedAmountRemovedEvent,
    ) -> Result<Self, Self::Error> {
        Ok(EncryptedAmountsRemoved {
            account_address:      removed.account.into(),
            new_encrypted_amount: serde_json::to_string(&removed.new_amount)?,
            input_amount:         serde_json::to_string(&removed.input_amount)?,
            up_to_index:          removed.up_to_index.index,
        })
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct EncryptedSelfAmountAdded {
    pub account_address:      AccountAddress,
    pub new_encrypted_amount: String,
    pub amount:               Amount,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct NewEncryptedAmount {
    pub account_address:  AccountAddress,
    pub new_index:        u64,
    pub encrypted_amount: String,
}

impl TryFrom<concordium_rust_sdk::types::NewEncryptedAmountEvent> for NewEncryptedAmount {
    type Error = anyhow::Error;

    fn try_from(
        added: concordium_rust_sdk::types::NewEncryptedAmountEvent,
    ) -> Result<Self, Self::Error> {
        Ok(NewEncryptedAmount {
            account_address:  added.receiver.into(),
            new_index:        added.new_index.index,
            encrypted_amount: serde_json::to_string(&added.encrypted_amount)?,
        })
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct TransferMemo {
    pub raw_hex: String,
}

#[ComplexObject]
impl TransferMemo {
    async fn decoded(&self) -> ApiResult<DecodedText> {
        let decoded_data = hex::decode(&self.raw_hex).map_err(|e| {
            error!("Invalid hex encoding {:?} in a controlled environment", e);
            InternalError::InternalError("Failed to decode hex data".to_string())
        })?;

        Ok(DecodedText::from_bytes(decoded_data.as_slice()))
    }
}

impl From<concordium_rust_sdk::types::Memo> for TransferMemo {
    fn from(value: concordium_rust_sdk::types::Memo) -> Self {
        TransferMemo {
            raw_hex: hex::encode(value.as_ref()),
        }
    }
}

#[derive(SimpleObject, Clone, serde::Serialize, serde::Deserialize)]
pub struct TimestampedAmount {
    pub timestamp: DateTime,
    pub amount:    UnsignedLong,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
#[graphql(complex)]
pub struct TransferredWithSchedule {
    pub from_account_address:    AccountAddress,
    pub to_account_address:      AccountAddress,
    #[graphql(skip)]
    pub(crate) amounts_schedule: Vec<TimestampedAmount>,
}

#[ComplexObject]
impl TransferredWithSchedule {
    async fn total_amount(&self) -> Amount {
        self.amounts_schedule.iter().map(|amount| amount.amount.0).sum::<u64>().into()
    }

    async fn amounts_schedule(
        &self,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<usize>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<usize>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<Connection<String, TimestampedAmount>> {
        connection_from_slice(&self.amounts_schedule[..], first, after, last, before)
    }
}
