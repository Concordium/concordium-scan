use crate::{
    address::{AccountAddress, Address},
    decoded_text::DecodedText,
    graphql_api::{ApiError, ApiResult},
    scalar_types::Amount,
};
use async_graphql::{ComplexObject, SimpleObject};
use tracing::error;

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct Transferred {
    pub amount: Amount,
    pub from:   Address,
    pub to:     AccountAddress,
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
            ApiError::InternalError("Failed to decode hex data".to_string())
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

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct TransferredWithSchedule {
    pub from_account_address: AccountAddress,
    pub to_account_address:   AccountAddress,
    pub total_amount:         Amount,
    // TODO: amountsSchedule("Returns the first _n_ elements from the list." first: Int "Returns
    // the elements in the list that come after the specified cursor." after: String "Returns the
    // last _n_ elements from the list." last: Int "Returns the elements in the list that come
    // before the specified cursor." before: String): AmountsScheduleConnection
}
