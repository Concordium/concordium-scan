//! This module contains information computed for smart contract module related
//! events in an account transaction during the concurrent preprocessing and the
//! logic for how to do the sequential processing into the database.

use crate::transaction_event::smart_contracts::ModuleReferenceContractLinkAction;
use anyhow::Context;
use concordium_rust_sdk::{
    base::smart_contracts::WasmVersion,
    smart_contracts::engine::utils::{get_embedded_schema_v0, get_embedded_schema_v1},
    types as sdk_types, v2,
};

#[derive(Debug)]
pub struct PreparedModuleDeployed {
    module_reference: String,
    schema: Option<Vec<u8>>,
}

impl PreparedModuleDeployed {
    pub async fn prepare(
        node_client: &mut v2::Client,
        module_reference: sdk_types::hashes::ModuleReference,
    ) -> anyhow::Result<Self> {
        // The `get_module_source` query on old blocks are currently not performing
        // well in the node. We query on the `lastFinal` block here as a result (https://github.com/Concordium/concordium-scan/issues/534).
        let wasm_module = node_client
            .get_module_source(&module_reference, v2::BlockIdentifier::LastFinal)
            .await?
            .response;
        let schema = match wasm_module.version {
            WasmVersion::V0 => get_embedded_schema_v0(wasm_module.source.as_ref()),
            WasmVersion::V1 => get_embedded_schema_v1(wasm_module.source.as_ref()),
        }
        .ok();

        let schema = schema
            .as_ref()
            .map(concordium_rust_sdk::base::contracts_common::to_bytes);

        Ok(Self {
            module_reference: module_reference.into(),
            schema,
        })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            "INSERT INTO smart_contract_modules (
                module_reference,
                transaction_index,
                schema
            ) VALUES ($1, $2, $3)",
            self.module_reference,
            transaction_index,
            self.schema
        )
        .execute(tx.as_mut())
        .await
        .with_context(|| format!("Failed inserting into smart_contract_modules: {:?}", self))?;
        Ok(())
    }
}

#[derive(Debug)]
pub struct PreparedModuleLinkAction {
    module_reference: String,
    contract_index: i64,
    contract_sub_index: i64,
    link_action: ModuleReferenceContractLinkAction,
}
impl PreparedModuleLinkAction {
    pub fn prepare(
        module_reference: sdk_types::hashes::ModuleReference,
        contract_address: sdk_types::ContractAddress,
        link_action: ModuleReferenceContractLinkAction,
    ) -> anyhow::Result<Self> {
        Ok(Self {
            contract_index: i64::try_from(contract_address.index)?,
            contract_sub_index: i64::try_from(contract_address.subindex)?,
            module_reference: module_reference.into(),
            link_action,
        })
    }

    pub async fn save(
        &self,
        tx: &mut sqlx::PgTransaction<'_>,
        transaction_index: i64,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            r#"INSERT INTO link_smart_contract_module_transactions (
                index,
                module_reference,
                transaction_index,
                contract_index,
                contract_sub_index,
                link_action
            ) VALUES (
                (SELECT COALESCE(MAX(index) + 1, 0)
                 FROM link_smart_contract_module_transactions
                 WHERE module_reference = $1),
                $1, $2, $3, $4, $5)"#,
            self.module_reference,
            transaction_index,
            self.contract_index,
            self.contract_sub_index,
            self.link_action as ModuleReferenceContractLinkAction
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }
}
