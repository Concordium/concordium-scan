use anyhow::Ok;
use bigdecimal::BigDecimal;
use concordium_rust_sdk::{protocol_level_tokens::TokenAmount, types::TokenCreationDetails};

use crate::transaction_event::protocol_level_tokens::{
    CreatePLTInitializationParameters, CreatePlt, InitializationParameters, TokenUpdate,
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
        let initialization_parameters_temp: InitializationParameters =
            ciborium::de::from_reader::<InitializationParameters, _>(
                details.create_plt.initialization_parameters.as_ref(),
            )
            .map_err(|e| anyhow::anyhow!("Failed to decode initialization parameters: {}", e))?;
        let initialization_parameters = serde_json::to_value(CreatePLTInitializationParameters {
            name:               initialization_parameters_temp.name,
            metadata:           initialization_parameters_temp.metadata,
            allow_list:         Some(initialization_parameters_temp.allow_list.unwrap_or(false)),
            deny_list:          Some(initialization_parameters_temp.deny_list.unwrap_or(false)),
            mintable:           Some(initialization_parameters_temp.mintable.unwrap_or(false)),
            burnable:           Some(initialization_parameters_temp.burnable.unwrap_or(false)),
            initial_supply:     initialization_parameters_temp.initial_supply.map(|v| v.into()),
            governance_account: initialization_parameters_temp.governance_account,
        })?;

        Ok(PreparedTokenCreationDetails {
            create_plt: CreatePlt {
                token_id: details.create_plt.token_id.clone().into(),
                token_module: details.create_plt.token_module.to_string(),
                decimals: details.create_plt.decimals,
                initialization_parameters,
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
        let name = self.create_plt.initialization_parameters["name"].to_string();
        let decimal = self.create_plt.decimals as i32;
        let module_reference = self.create_plt.token_module.to_string();
        let metadata = self.create_plt.initialization_parameters["metadata"].clone();

        let value = self.create_plt.initialization_parameters["initial_supply"]["value"]
            .as_str()
            .unwrap_or("0")
            .parse::<u64>()
            .unwrap_or(0);
        let decimals = self.create_plt.initialization_parameters["initial_supply"]["decimals"]
            .as_u64()
            .unwrap_or(0) as u8;
        let amount = TokenAmount::from_raw(value, decimals);
        let initial_supply = amount.value();
        let initial_supply =
            BigDecimal::from(initial_supply) / BigDecimal::from(10u64.pow(decimals as u32));
        let issuer = self.create_plt.initialization_parameters["governance_account"]["as_string"]
            .as_str()
            .ok_or_else(|| {
                anyhow::anyhow!("Missing governance account in initialization parameters")
            })?
            .to_string();

        sqlx::query!(
            "INSERT INTO plt_tokens (
                index,
                token_id,
                transaction_index,
                name,
                decimal,
                issuer_index,
                issuer,
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
                $8,
                $9
                              )",
            token_id,
            transaction_index,
            name,
            decimal,
            issuer,
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
