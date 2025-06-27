use anyhow::Ok;
use concordium_rust_sdk::types::TokenCreationDetails;

use crate::transaction_event::protocol_level_tokens::{CreatePlt, TokenEvent};

#[derive(Debug)]
#[allow(dead_code)]
pub struct PreparedTokenCreationDetails {
    pub create_plt: CreatePlt,   // The PLT creation identifier
    pub events: Vec<TokenEvent>, // List of prepared token governance events
}

impl PreparedTokenCreationDetails {
    /// Converts a protocol-level token creation details into a prepared
    /// version.
    pub fn prepare(details: &TokenCreationDetails) -> anyhow::Result<Self> {
        Ok(PreparedTokenCreationDetails {
            create_plt: CreatePlt {
                token_id: details.create_plt.token_id.clone().into(),
                token_module: details.create_plt.token_module.clone().into(),
                governance_account: details.create_plt.governance_account.clone().into(),
                decimals: details.create_plt.decimals,
                initialization_parameters: serde_json::to_value(
                    &details.create_plt.initialization_parameters,
                )
                .unwrap(),
            },
            events: details
                .events
                .iter()
                .map(TokenEvent::prepare)
                .collect::<anyhow::Result<Vec<_>>>()?,
        })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        self.create_plt.save(tx, transaction_index).await?;
        println!("Saving PLT creation: {:?} with {} events", self.create_plt, self.events.len());
        for event in &self.events {
            event.save(tx, transaction_index).await?;
        }

        Ok(())
    }
}
