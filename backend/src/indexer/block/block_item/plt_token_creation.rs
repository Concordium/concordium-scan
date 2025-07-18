use anyhow::Ok;
use bigdecimal::BigDecimal;
use concordium_rust_sdk::types::TokenCreationDetails;

use crate::transaction_event::protocol_level_tokens::{
    CreatePlt, InitializationParameters, TokenUpdate,
};

#[derive(Debug)]
#[allow(dead_code)]
pub struct PreparedTokenCreationDetails {
    pub create_plt: CreatePlt,        // The PLT creation identifier
    pub events:     Vec<TokenUpdate>, // List of prepared token governance events
}

impl PreparedTokenCreationDetails {
    /// Converts a protocol-level token creation details into a prepared
    /// version.
    pub fn prepare(details: &TokenCreationDetails) -> anyhow::Result<Self> {
        let initialization_parameters: InitializationParameters =
            ciborium::de::from_reader::<InitializationParameters, _>(
                details.create_plt.initialization_parameters.as_ref(),
            )
            .map_err(|e| anyhow::anyhow!("Failed to decode initialization parameters: {}", e))?;

        Ok(PreparedTokenCreationDetails {
            create_plt: CreatePlt {
                token_id:                  details.create_plt.token_id.clone().into(),
                token_module:              details.create_plt.token_module.to_string(),
                decimals:                  details.create_plt.decimals,
                initialization_parameters: initialization_parameters.into(),
            },
            events:     details
                .events
                .iter()
                .map(TokenUpdate::prepare)
                .collect::<anyhow::Result<Vec<_>>>()?,
        })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        let token_id = self.create_plt.token_id.to_string();
        let name = self.create_plt.initialization_parameters.name.clone();
        let decimal = self.create_plt.decimals as i32;
        let module_reference = self.create_plt.token_module.to_string();
        let metadata =
            serde_json::to_value(self.create_plt.initialization_parameters.metadata.clone())
                .map_err(|e| anyhow::anyhow!("Failed to serialize metadata: {}", e))?;

        let value: u64 = self
            .create_plt
            .initialization_parameters
            .initial_supply
            .clone()
            .map(|supply| supply.value.parse::<u64>().unwrap_or(0))
            .unwrap_or(0);
        let initial_supply = BigDecimal::from(value);
        let issuer = self
            .create_plt
            .initialization_parameters
            .governance_account
            .clone()
            .map(|account| account.address.to_string())
            .ok_or(anyhow::anyhow!("Missing governance account in token creation details"))?;

        sqlx::query!(
            "INSERT INTO plt_tokens (
                index,
                token_id,
                transaction_index,
                name,
                decimal,
                issuer_index,
                module_reference,
                metadata,
                initial_supply
            ) VALUES (
                (SELECT COALESCE(MAX(index) + 1, 0) FROM plt_tokens),
                $1,
                $2,
                $3,
                $4,
                (SELECT index FROM accounts WHERE address = $5),
                $6,
                $7,
                $8
                              )",
            token_id,
            transaction_index,
            name,
            decimal,
            issuer,
            module_reference,
            metadata,
            initial_supply
        )
        .execute(tx.as_mut())
        .await?;

        for event in &self.events {
            event.save(tx, transaction_index).await?;
        }

        Ok(())
    }
}
