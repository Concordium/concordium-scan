use anyhow::Ok;
use concordium_rust_sdk::types::{CreatePlt, TokenCreationDetails};

use crate::transaction_event::protocol_level_tokens::TokenUpdate;

#[derive(Debug)]
#[allow(dead_code)]
pub struct PreparedTokenCreationDetails {
    pub create_plt: CreatePlt,       // The PLT creation identifier
    pub events:     Vec<TokenUpdate>, // List of prepared token governance events
}

impl PreparedTokenCreationDetails {
    /// Converts a protocol-level token creation details into a prepared
    /// version.
    pub fn prepare(details: &TokenCreationDetails) -> anyhow::Result<Self> {
        Ok(PreparedTokenCreationDetails {
            create_plt: details.create_plt.clone(),
            events:     details
                .events
                .iter()
                .map(TokenUpdate::prepare)
                .collect::<anyhow::Result<Vec<_>>>()?,
        })
    }

    pub fn save(
        &self,
        _tx: &mut sqlx::PgTransaction<'_>,
        _transaction_index: i64,
    ) -> anyhow::Result<()> {
        Ok(())
    }
}
