use super::{get_config, get_pool, ApiError, ApiResult, CollectionSegmentInfo};
use crate::{
    address::{AccountAddress, ContractAddress},
    scalar_types::{BlockHeight, DateTime, ModuleReference, TransactionHash},
    transaction_event::smart_contracts::ModuleReferenceContractLinkAction,
    transaction_reject::TransactionRejectReason,
};
use async_graphql::{ComplexObject, Context, Object, SimpleObject};
use concordium_rust_sdk::base::contracts_common::schema::VersionedModuleSchema;

#[derive(Default)]
pub struct QueryModuleReferenceEvent;

#[Object]
impl QueryModuleReferenceEvent {
    async fn module_reference_event<'a>(
        &self,
        ctx: &Context<'a>,
        module_reference: String,
    ) -> ApiResult<ModuleReferenceEvent> {
        let pool = get_pool(ctx)?;

        let row = sqlx::query!(
            r#"SELECT
                blocks.height as block_height,
                smart_contract_modules.transaction_index,
                schema as display_schema,
                blocks.slot_time as block_slot_time,
                transactions.hash as transaction_hash,
                accounts.address as sender
            FROM smart_contract_modules
            JOIN transactions ON smart_contract_modules.transaction_index = transactions.index
            JOIN blocks ON transactions.block_height = blocks.height
            JOIN accounts ON transactions.sender_index = accounts.index
            WHERE module_reference = $1"#,
            module_reference
        )
        .fetch_optional(pool)
        .await?
        .ok_or(ApiError::NotFound)?;

        let display_schema = row
            .display_schema
            .as_ref()
            .map(|s| VersionedModuleSchema::new(s, &None).map(|schema| schema.to_string()))
            .transpose()?;

        Ok(ModuleReferenceEvent {
            module_reference,
            sender: row.sender.into(),
            block_height: row.block_height,
            transaction_hash: row.transaction_hash,
            block_slot_time: row.block_slot_time,
            display_schema,
        })
    }
}

#[derive(SimpleObject)]
#[graphql(complex)]
pub struct ModuleReferenceEvent {
    module_reference: ModuleReference,
    sender:           AccountAddress,
    block_height:     BlockHeight,
    transaction_hash: TransactionHash,
    block_slot_time:  DateTime,
    display_schema:   Option<String>,
}
#[ComplexObject]
impl ModuleReferenceEvent {
    async fn module_reference_reject_events(
        &self,
        ctx: &Context<'_>,
        skip: Option<u64>,
        take: Option<u64>,
    ) -> ApiResult<ModuleReferenceRejectEventsCollectionSegment> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let min_index = i64::try_from(skip.unwrap_or(0))?;
        let limit = i64::try_from(
            take.map_or(config.module_reference_reject_events_collection_limit, |t| {
                config.module_reference_reject_events_collection_limit.min(t)
            }),
        )?;

        let total_count: u64 = sqlx::query_scalar!(
            "SELECT
                COUNT(*)
            FROM rejected_smart_contract_module_transactions
                WHERE module_reference = $1",
            self.module_reference,
        )
        .fetch_one(pool)
        .await?
        .unwrap_or(0)
        .try_into()?;

        let mut items = sqlx::query_as!(
            ModuleReferenceRejectEvent,
            r#"SELECT
                module_reference,
                transactions.reject as "reject: sqlx::types::Json<TransactionRejectReason>",
                transactions.block_height,
                transactions.hash as transaction_hash,
                blocks.slot_time as block_slot_time
            FROM rejected_smart_contract_module_transactions
                JOIN transactions ON transaction_index = transactions.index
                JOIN blocks ON blocks.height = transactions.block_height
            WHERE module_reference = $1
                AND rejected_smart_contract_module_transactions.index < $2
            ORDER BY rejected_smart_contract_module_transactions.index DESC
            LIMIT $3
        "#,
            self.module_reference,
            (total_count as i64).saturating_sub(min_index),
            limit + 1
        )
        .fetch_all(pool)
        .await?;

        // Determine if there is a next page by checking if we got more than `limit`
        // rows.
        let has_next_page = items.len() > limit as usize;
        // If there is a next page, remove the extra row used for pagination detection.
        if has_next_page {
            items.pop();
        }
        let has_previous_page = min_index > 0;

        Ok(ModuleReferenceRejectEventsCollectionSegment {
            page_info: CollectionSegmentInfo {
                has_next_page,
                has_previous_page,
            },
            total_count,
            items,
        })
    }

    async fn module_reference_contract_link_events(
        &self,
        ctx: &Context<'_>,
        skip: Option<u64>,
        take: Option<u64>,
    ) -> ApiResult<ModuleReferenceContractLinkEventsCollectionSegment> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let min_index = i64::try_from(skip.unwrap_or(0))?;
        let limit = i64::try_from(
            take.map_or(config.module_reference_contract_link_events_collection_limit, |t| {
                config.module_reference_contract_link_events_collection_limit.min(t)
            }),
        )?;

        let mut items = sqlx::query_as!(
            ModuleReferenceContractLinkEvent,
            r#"SELECT
                link_action as "link_action: ModuleReferenceContractLinkAction",
                contract_index,
                contract_sub_index,
                transactions.hash as transaction_hash,
                blocks.slot_time as block_slot_time
            FROM link_smart_contract_module_transactions
                JOIN transactions ON transaction_index = transactions.index
                JOIN blocks ON blocks.height = transactions.block_height
            WHERE module_reference = $1
                AND link_smart_contract_module_transactions.index >= $2
            LIMIT $3
        "#,
            self.module_reference,
            min_index,
            limit + 1
        )
        .fetch_all(pool)
        .await?;

        // Determine if there is a next page by checking if we got more than `limit`
        // rows.
        let has_next_page = items.len() > limit as usize;
        // If there is a next page, remove the extra row used for pagination detection.
        if has_next_page {
            items.pop();
        }
        let has_previous_page = min_index > 0;

        let total_count: u64 = sqlx::query_scalar!(
            "SELECT
                COUNT(*)
            FROM link_smart_contract_module_transactions
                WHERE module_reference = $1",
            self.module_reference,
        )
        .fetch_one(pool)
        .await?
        .unwrap_or(0)
        .try_into()?;

        Ok(ModuleReferenceContractLinkEventsCollectionSegment {
            page_info: CollectionSegmentInfo {
                has_next_page,
                has_previous_page,
            },
            total_count,
            items,
        })
    }

    async fn linked_contracts(
        &self,
        ctx: &Context<'_>,
        skip: Option<u64>,
        take: Option<u64>,
    ) -> ApiResult<LinkedContractsCollectionSegment> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let offset = i64::try_from(skip.unwrap_or(0))?;
        let limit = i64::try_from(
            take.map_or(config.module_reference_linked_contracts_collection_limit, |t| {
                config.module_reference_linked_contracts_collection_limit.min(t)
            }),
        )?;
        // This offset approach below does not scale well for smart contract modules
        // with a large number of instances currently linked, since a large
        // offset would traverse these. This might have to be improved in the
        // future by either indexing more or break the API to not use offset
        // pagination.
        let mut items = sqlx::query_as!(
            LinkedContract,
            "SELECT
                contracts.index as contract_index,
                contracts.sub_index as contract_sub_index,
                blocks.slot_time as linked_date_time
            FROM contracts
                JOIN transactions
                    ON transactions.index =
                        COALESCE(last_upgrade_transaction_index, transaction_index)
                JOIN blocks ON blocks.height = transactions.block_height
            WHERE contracts.module_reference = $1
            OFFSET $2
            LIMIT $3",
            self.module_reference,
            offset,
            limit + 1
        )
        .fetch_all(pool)
        .await?;

        // Determine if there is a next page by checking if we got more than `limit`
        // rows.
        let has_next_page = items.len() > limit as usize;
        // If there is a next page, remove the extra row used for pagination detection.
        if has_next_page {
            items.pop();
        }
        let has_previous_page = offset > 0;

        let total_count: u64 = sqlx::query_scalar!(
            "SELECT
                COUNT(*)
            FROM contracts
                WHERE module_reference = $1",
            self.module_reference,
        )
        .fetch_one(pool)
        .await?
        .unwrap_or(0)
        .try_into()?;

        Ok(LinkedContractsCollectionSegment {
            page_info: CollectionSegmentInfo {
                has_next_page,
                has_previous_page,
            },
            total_count,
            items,
        })
    }
}

#[derive(SimpleObject)]
struct ModuleReferenceRejectEventsCollectionSegment {
    page_info:   CollectionSegmentInfo,
    items:       Vec<ModuleReferenceRejectEvent>,
    total_count: u64,
}

#[derive(SimpleObject)]
#[graphql(complex)]
struct ModuleReferenceRejectEvent {
    module_reference: ModuleReference,
    #[graphql(skip)]
    reject:           Option<sqlx::types::Json<TransactionRejectReason>>,
    block_height:     BlockHeight,
    transaction_hash: TransactionHash,
    block_slot_time:  DateTime,
}
#[ComplexObject]
impl ModuleReferenceRejectEvent {
    async fn rejected_event(&self) -> ApiResult<&TransactionRejectReason> {
        if let Some(sqlx::types::Json(reason)) = self.reject.as_ref() {
            Ok(reason)
        } else {
            Err(ApiError::InternalError(
                "ModuleReferenceRejectEvent: No reject reason found".to_string(),
            ))
        }
    }
}

#[derive(SimpleObject)]
#[graphql(complex)]
struct ModuleReferenceContractLinkEvent {
    block_slot_time:    DateTime,
    transaction_hash:   TransactionHash,
    link_action:        ModuleReferenceContractLinkAction,
    #[graphql(skip)]
    contract_index:     i64,
    #[graphql(skip)]
    contract_sub_index: i64,
}
#[ComplexObject]
impl ModuleReferenceContractLinkEvent {
    async fn contract_address(&self) -> ApiResult<ContractAddress> {
        ContractAddress::new(self.contract_index, self.contract_sub_index)
    }
}

/// A segment of a collection.
#[derive(SimpleObject)]
struct ModuleReferenceContractLinkEventsCollectionSegment {
    /// Information to aid in pagination.
    page_info:   CollectionSegmentInfo,
    /// A flattened list of the items.
    items:       Vec<ModuleReferenceContractLinkEvent>,
    total_count: u64,
}

/// A segment of a collection.
#[derive(SimpleObject)]
struct LinkedContractsCollectionSegment {
    /// Information to aid in pagination.
    page_info:   CollectionSegmentInfo,
    /// A flattened list of the items.
    items:       Vec<LinkedContract>,
    total_count: u64,
}

struct LinkedContract {
    contract_index:     i64,
    contract_sub_index: i64,
    linked_date_time:   DateTime,
}

#[Object]
impl LinkedContract {
    async fn contract_address(&self) -> ApiResult<ContractAddress> {
        ContractAddress::new(self.contract_index, self.contract_sub_index)
    }

    async fn linked_date_time(&self) -> DateTime { self.linked_date_time }
}
