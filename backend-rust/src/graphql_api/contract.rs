use super::{
    get_config, get_pool, token::TokensCollectionSegment, ApiError, ApiResult,
    CollectionSegmentInfo, ConnectionQuery,
};
use crate::{
    address::{AccountAddress, ContractIndex},
    connection::DescendingI64,
    graphql_api::token::Token,
    scalar_types::{Amount, BlockHeight, DateTime, TransactionHash},
    transaction_event::Event,
    transaction_reject::TransactionRejectReason,
};
use async_graphql::{connection, ComplexObject, Context, Object, SimpleObject};
use futures::TryStreamExt;
use std::cmp::{max, min};

#[derive(Default)]
pub struct QueryContract;

#[Object]
impl QueryContract {
    async fn contract<'a>(
        &self,
        ctx: &Context<'a>,
        contract_address_index: ContractIndex,
        contract_address_sub_index: ContractIndex,
    ) -> ApiResult<Contract> {
        let pool = get_pool(ctx)?;

        let row = sqlx::query!(
            r#"SELECT
                module_reference,
                name as contract_name,
                contracts.amount,
                blocks.slot_time as block_slot_time,
                transactions.block_height,
                transactions.hash as transaction_hash,
                accounts.address as creator
            FROM contracts
            JOIN transactions ON transaction_index = transactions.index
            JOIN blocks ON transactions.block_height = blocks.height
            JOIN accounts ON transactions.sender_index = accounts.index
            WHERE contracts.index = $1 AND contracts.sub_index = $2"#,
            contract_address_index.0 as i64,
            contract_address_sub_index.0 as i64,
        )
        .fetch_optional(pool)
        .await?
        .ok_or(ApiError::NotFound)?;

        let snapshot = ContractSnapshot {
            block_height: row.block_height,
            contract_address_index,
            contract_address_sub_index,
            contract_name: row.contract_name,
            module_reference: row.module_reference,
            amount: row.amount.try_into()?,
        };

        Ok(Contract {
            contract_address_index,
            contract_address_sub_index,
            contract_address: format!(
                "<{},{}>",
                contract_address_index, contract_address_sub_index
            ),
            creator: row.creator.into(),
            block_height: row.block_height,
            transaction_hash: row.transaction_hash,
            block_slot_time: row.block_slot_time,
            snapshot,
        })
    }

    async fn contracts<'a>(
        &self,
        ctx: &Context<'a>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Contract>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<DescendingI64>::new(
            first,
            after,
            last,
            before,
            config.contract_connection_limit,
        )?;

        // The CCDScan front-end currently expects an DESC order of the nodes/edges
        // returned (outer `ORDER BY`), while the inner `ORDER BY` is a trick to
        // get the correct nodes/edges selected based on whether the `first` or `last`
        // query parameter is specified.
        let mut row_stream = sqlx::query!(
            "SELECT * FROM (
                SELECT
                    contracts.index as index,
                    sub_index,
                    module_reference,
                    name as contract_name,
                    contracts.amount,
                    blocks.slot_time as block_slot_time,
                    transactions.block_height,
                    transactions.hash as transaction_hash,
                    accounts.address as creator
                FROM contracts
                    JOIN transactions ON transaction_index = transactions.index
                    JOIN blocks ON transactions.block_height = blocks.height
                    JOIN accounts ON transactions.sender_index = accounts.index
                WHERE 
                    contracts.index < $1 AND 
                    contracts.index > $2
                ORDER BY
                    (CASE WHEN $4 THEN contracts.index END) ASC,
                    (CASE WHEN NOT $4 THEN contracts.index END) DESC
                LIMIT $3
            ) AS contract_data
            ORDER BY contract_data.index DESC",
            i64::from(query.from),
            i64::from(query.to),
            query.limit,
            query.is_last
        )
        .fetch(pool);

        let mut connection = connection::Connection::new(false, false);
        let mut page_max_index = None;
        let mut page_min_index = None;

        while let Some(row) = row_stream.try_next().await? {
            let contract_address_index =
                row.index.try_into().map_err(|e: <u64 as TryFrom<i64>>::Error| {
                    ApiError::InternalError(e.to_string())
                })?;
            let contract_address_sub_index =
                row.sub_index.try_into().map_err(|e: <u64 as TryFrom<i64>>::Error| {
                    ApiError::InternalError(e.to_string())
                })?;

            let snapshot = ContractSnapshot {
                block_height: row.block_height,
                contract_address_index,
                contract_address_sub_index,
                contract_name: row.contract_name,
                module_reference: row.module_reference,
                amount: row.amount.try_into()?,
            };

            let contract = Contract {
                contract_address_index,
                contract_address_sub_index,
                contract_address: format!(
                    "<{},{}>",
                    contract_address_index, contract_address_sub_index
                ),
                creator: row.creator.into(),
                block_height: row.block_height,
                transaction_hash: row.transaction_hash,
                block_slot_time: row.block_slot_time,
                snapshot,
            };
            page_max_index = Some(match page_max_index {
                None => row.index,
                Some(current_max) => max(current_max, row.index),
            });

            page_min_index = Some(match page_min_index {
                None => row.index,
                Some(current_min) => min(current_min, row.index),
            });

            connection
                .edges
                .push(connection::Edge::new(contract.contract_address_index.to_string(), contract));
        }

        if let (Some(page_min_id), Some(page_max_id)) = (page_min_index, page_max_index) {
            let result = sqlx::query!(
                "
                    SELECT MAX(index) as db_max_index, MIN(index) as db_min_index
                    FROM contracts
                "
            )
            .fetch_one(pool)
            .await?;

            connection.has_previous_page =
                result.db_max_index.map_or(false, |db_max| db_max > page_max_id);
            connection.has_next_page =
                result.db_min_index.map_or(false, |db_min| db_min < page_min_id);
        }

        Ok(connection)
    }
}

#[derive(SimpleObject)]
#[graphql(complex)]
pub struct Contract {
    pub contract_address_index:     ContractIndex,
    pub contract_address_sub_index: ContractIndex,
    pub contract_address:           String,
    pub creator:                    AccountAddress,
    pub block_height:               BlockHeight,
    pub transaction_hash:           String,
    pub block_slot_time:            DateTime,
    pub snapshot:                   ContractSnapshot,
}

#[ComplexObject]
impl Contract {
    // This function returns events from the `contract_events` table as well as
    // one `init_transaction_event` from when the contract was initialized. The
    // `skip` and `take` parameters are used to paginate the events.
    async fn contract_events(
        &self,
        ctx: &Context<'_>,
        skip: Option<u64>,
        take: Option<u64>,
    ) -> ApiResult<ContractEventsCollectionSegment> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let skip = skip.unwrap_or(0);
        let take = take.unwrap_or(config.contract_events_collection_limit);

        let total_contract_events_count: u64 = sqlx::query_scalar!(
            "SELECT
                COUNT(*)
            FROM contract_events
                WHERE contract_index = $1 AND contract_sub_index = $2",
            self.contract_address_index.0 as i64,
            self.contract_address_sub_index.0 as i64
        )
        .fetch_one(pool)
        .await?
        .unwrap_or(0)
        .try_into()?;
        // If `skip` is 0 and at least one event is taken, include the
        // `init_transaction_event`.
        let include_initial_event =
            skip <= total_contract_events_count && skip + take >= total_contract_events_count;
        // Adjust the `take` and `skip` values considering if the
        // `init_transaction_event` is requested to be included or not.

        // Limit the number of events to be fetched from the `contract_events` table.
        let limit = std::cmp::min(
            take,
            config.contract_events_collection_limit.saturating_sub(include_initial_event as u64),
        );

        let mut contract_events = vec![];
        let mut initial_contract_event_exists_in_database = false;

        // Get the events from the `contract_events` table.
        let mut rows = sqlx::query!(
            "
                SELECT
                    event_index_per_contract,
                    contract_events.transaction_index,
                    trace_element_index,
                    contract_events.block_height AS event_block_height,
                    transactions.hash as transaction_hash,
                    transactions.events,
                    accounts.address as creator,
                    blocks.slot_time as block_slot_time,
                    blocks.height as block_height
                FROM contract_events
                JOIN transactions
                    ON contract_events.block_height = transactions.block_height
                    AND contract_events.transaction_index = transactions.index
                JOIN accounts
                    ON transactions.sender_index = accounts.index
                JOIN blocks
                    ON contract_events.block_height = blocks.height
                WHERE contract_events.contract_index = $1 AND contract_events.contract_sub_index = \
             $2
                AND event_index_per_contract < $4
                ORDER BY event_index_per_contract DESC
                LIMIT $3
            ",
            self.contract_address_index.0 as i64,
            self.contract_address_sub_index.0 as i64,
            limit as i64 + 1,
            total_contract_events_count as i64 - skip as i64
        )
        .fetch_all(pool)
        .await?;

        // Determine if there is a next page by checking if we got more than `limit`
        // rows.
        let has_next_page = rows.len() > limit as usize;

        // If there is a next page, remove the extra row used for pagination detection.
        if has_next_page {
            rows.pop();
        }

        for row in rows {
            let Some(events) = row.events else {
                return Err(ApiError::InternalError("Missing events in database".to_string()));
            };

            let mut events: Vec<Event> = serde_json::from_value(events).map_err(|e| {
                ApiError::InternalError(format!(
                    "Failed to deserialize events from database: {}",
                    e
                ))
            })?;

            if row.trace_element_index as usize >= events.len() {
                return Err(ApiError::InternalError(
                    "Trace element index does not exist in events".to_string(),
                ));
            }

            // Get the associated contract event from the `events` vector.
            let event = events.swap_remove(row.trace_element_index as usize);

            match event {
                Event::Transferred(_)
                | Event::ContractInterrupted(_)
                | Event::ContractResumed(_)
                | Event::ContractUpgraded(_)
                | Event::ContractUpdated(_) => Ok(()),
                _ => Err(ApiError::InternalError(format!(
                    "Not Transferred, ContractInterrupted, ContractResumed, ContractUpgraded, or \
                     ContractUpdated event; Wrong event enum tag: {:?}",
                    std::mem::discriminant(&event)
                ))),
            }?;

            let contract_event = ContractEvent {
                contract_address_index: self.contract_address_index,
                contract_address_sub_index: self.contract_address_sub_index,
                sender: row.creator.into(),
                event,
                block_height: row.block_height,
                transaction_hash: row.transaction_hash,
                block_slot_time: row.block_slot_time,
            };

            contract_events.push(contract_event);
        }

        // Get the `init_transaction_event`.
        let row = sqlx::query!(
            "
                SELECT
                    module_reference,
                    name as contract_name,
                    contracts.amount as amount,
                    contracts.transaction_index as transaction_index,
                    transactions.events,
                    transactions.hash as transaction_hash,
                    transactions.block_height as block_height,
                    blocks.slot_time as block_slot_time,
                    accounts.address as creator
                FROM contracts
                JOIN transactions ON transaction_index=transactions.index
                JOIN blocks ON block_height = blocks.height
                JOIN accounts ON transactions.sender_index = accounts.index
                WHERE contracts.index = $1 AND contracts.sub_index = $2
                ",
            self.contract_address_index.0 as i64,
            self.contract_address_sub_index.0 as i64
        )
        .fetch_optional(pool)
        .await?;

        if row.is_some() {
            initial_contract_event_exists_in_database = true;
        }

        if include_initial_event {
            let row = row.ok_or(ApiError::NotFound)?;

            let Some(events) = row.events else {
                return Err(ApiError::InternalError("Missing events in database".to_string()));
            };

            let [event]: [Event; 1] = serde_json::from_value(events).map_err(|e| {
                ApiError::InternalError(format!(
                    "Failed to deserialize events from database. Contract init transaction \
                     expects exactly one event: {}",
                    e
                ))
            })?;

            match event {
                Event::ContractInitialized(_) => Ok(()),
                _ => Err(ApiError::InternalError(format!(
                    "Not ContractInitialized event; Wrong event enum tag: {:?}",
                    std::mem::discriminant(&event)
                ))),
            }?;

            let initial_event = ContractEvent {
                contract_address_index: self.contract_address_index,
                contract_address_sub_index: self.contract_address_sub_index,
                sender: row.creator.into(),
                event,
                block_height: row.block_height,
                transaction_hash: row.transaction_hash,
                block_slot_time: row.block_slot_time,
            };
            contract_events.push(initial_event);
        }

        Ok(ContractEventsCollectionSegment {
            page_info:   CollectionSegmentInfo {
                has_next_page,
                has_previous_page: skip > 0,
            },
            items:       contract_events,
            total_count: total_contract_events_count
                + initial_contract_event_exists_in_database as u64,
        })
    }

    async fn contract_reject_events(
        &self,
        ctx: &Context<'_>,
        skip: Option<u64>,
        take: Option<u64>,
    ) -> ApiResult<ContractRejectEventsCollectionSegment> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;

        let limit =
            i64::try_from(take.map_or(config.contract_reject_events_collection_limit, |t| {
                config.contract_reject_events_collection_limit.min(t)
            }))?;

        let total_count: u64 = sqlx::query_scalar!(
            "SELECT
                MAX(transaction_index_per_contract) + 1
            FROM contract_reject_transactions
            WHERE contract_reject_transactions.contract_index = $1
                AND contract_reject_transactions.contract_sub_index = $2",
            self.contract_address_index.0 as i64,
            self.contract_address_sub_index.0 as i64
        )
        .fetch_one(pool)
        .await?
        .unwrap_or(0)
        .try_into()?;

        let offset = i64::try_from(total_count.saturating_sub(skip.unwrap_or(0)))?;

        let items = sqlx::query_as!(
            ContractRejectEvent,
            r#"SELECT
                transactions.reject as "rejected_event: _",
                transactions.hash as transaction_hash,
                blocks.slot_time as block_slot_time
            FROM contract_reject_transactions
                JOIN transactions ON
                    transactions.index = contract_reject_transactions.transaction_index
                JOIN blocks ON blocks.height = transactions.block_height
            WHERE contract_reject_transactions.contract_index = $1
                AND contract_reject_transactions.contract_sub_index = $2
                AND contract_reject_transactions.transaction_index_per_contract < $3
            ORDER BY contract_reject_transactions.transaction_index_per_contract DESC
            LIMIT $4
            "#,
            self.contract_address_index.0 as i64,
            self.contract_address_sub_index.0 as i64,
            offset,
            limit,
        )
        .fetch_all(pool)
        .await?;

        Ok(ContractRejectEventsCollectionSegment {
            items,
            total_count,
        })
    }

    // This function fetches CIS2 tokens associated to a given contract, ordered by
    // their creation index in descending order. It retrieves the most recent
    // tokens first, with support for pagination through `skip` and `take`
    // parameters.
    // - `skip` determines how many of the most recent tokens to skip.
    // - `take` controls the number of tokens to return, respecting the collection
    //   limit.
    async fn tokens(
        &self,
        ctx: &Context<'_>,
        skip: Option<u64>,
        take: Option<u64>,
    ) -> ApiResult<TokensCollectionSegment> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;

        let total_count = sqlx::query_scalar!(
            "SELECT MAX(token_index_per_contract) + 1
            FROM tokens
            WHERE tokens.contract_index = $1 AND tokens.contract_sub_index = $2",
            self.contract_address_index.0 as i64,
            self.contract_address_sub_index.0 as i64,
        )
        .fetch_one(pool)
        .await?
        .unwrap_or(0) as u64;

        let max_index = i64::try_from(total_count.saturating_sub(skip.unwrap_or(0)))?;
        let limit = i64::try_from(take.map_or(config.contract_tokens_collection_limit, |t| {
            config.contract_tokens_collection_limit.min(t)
        }))?;

        let items = sqlx::query_as!(
            Token,
            "SELECT
                index,
                total_supply as raw_total_supply,
                token_id,
                contract_index,
                contract_sub_index,
                token_address,
                metadata_url,
                init_transaction_index
            FROM tokens
            WHERE tokens.contract_index = $1 AND tokens.contract_sub_index = $2
                AND tokens.token_index_per_contract < $3
            ORDER BY tokens.token_index_per_contract DESC
            LIMIT $4
            ",
            self.contract_address_index.0 as i64,
            self.contract_address_sub_index.0 as i64,
            max_index,
            limit,
        )
        .fetch_all(pool)
        .await?;

        Ok(TokensCollectionSegment {
            total_count,
            items,
        })
    }
}

/// A segment of a collection.
#[derive(SimpleObject)]
struct ContractRejectEventsCollectionSegment {
    /// A flattened list of the items.
    items:       Vec<ContractRejectEvent>,
    total_count: u64,
}

struct ContractRejectEvent {
    rejected_event:   Option<sqlx::types::Json<TransactionRejectReason>>,
    transaction_hash: TransactionHash,
    block_slot_time:  DateTime,
}
#[Object]
impl ContractRejectEvent {
    async fn rejected_event(&self) -> ApiResult<&TransactionRejectReason> {
        if let Some(sqlx::types::Json(reason)) = self.rejected_event.as_ref() {
            Ok(reason)
        } else {
            Err(ApiError::InternalError("ContractRejectEvent: No reject reason found".to_string()))
        }
    }

    async fn transaction_hash(&self) -> &TransactionHash { &self.transaction_hash }

    async fn block_slot_time(&self) -> &DateTime { &self.block_slot_time }
}

#[derive(SimpleObject)]
pub struct ContractSnapshot {
    pub block_height:               BlockHeight,
    pub contract_address_index:     ContractIndex,
    pub contract_address_sub_index: ContractIndex,
    pub contract_name:              String,
    pub module_reference:           String,
    pub amount:                     Amount,
}

/// A segment of a collection.
#[derive(SimpleObject)]
struct ContractEventsCollectionSegment {
    /// Information to aid in pagination.
    page_info:   CollectionSegmentInfo,
    /// A flattened list of the items.
    items:       Vec<ContractEvent>,
    total_count: u64,
}

#[derive(SimpleObject)]
struct ContractEvent {
    contract_address_index: ContractIndex,
    contract_address_sub_index: ContractIndex,
    sender: AccountAddress,
    event: Event,
    block_height: BlockHeight,
    transaction_hash: String,
    block_slot_time: DateTime,
}
