//! TODO:
//! - Check endpoints are using the same chain.
//! - Extend with prometheus metrics.
//! - Batch blocks into the same SQL transaction.
//! - More logging
//! - Setup CI to check formatting and build.
//! - Build docker images.
//! - Setup CI for deployment.

use crate::graphql_api::{
    events_from_summary,
    AccountTransactionType,
    CredentialDeploymentTransactionType,
    DbTransactionType,
    UpdateTransactionType,
};
use anyhow::Context;
use chrono::NaiveDateTime;
use concordium_rust_sdk::{
    indexer::{
        async_trait,
        traverse_and_process,
        Indexer,
        ProcessEvent,
        TraverseConfig,
        TraverseError,
    },
    types::{
        queries::BlockInfo,
        AccountTransactionDetails,
        AccountTransactionEffects,
        BlockItemSummary,
        BlockItemSummaryDetails,
        RewardsOverview,
    },
    v2::{
        self,
        ChainParameters,
        FinalizedBlockInfo,
        QueryResult,
        RPCError,
    },
};
use futures::TryStreamExt;
use sqlx::PgPool;
use tokio_util::sync::CancellationToken;
use tracing::info;

pub struct CcdScanIndexer {
    endpoints: Vec<v2::Endpoint>,
    start_height: u64,
    block_processor: BlockProcessor,
}

impl CcdScanIndexer {
    pub async fn new(endpoints: Vec<v2::Endpoint>, pool: PgPool) -> anyhow::Result<Self> {
        let last_height_stored = sqlx::query!(
            r#"
SELECT height FROM blocks ORDER BY height DESC LIMIT 1
"#
        )
        .fetch_optional(&pool)
        .await?
        .map(|r| r.height);

        let start_height = if let Some(height) = last_height_stored {
            u64::try_from(height)? + 1
        } else {
            save_genesis_data(endpoints[0].clone(), &pool).await?;
            1
        };
        let block_processor = BlockProcessor::load_from_database(pool).await?;

        Ok(Self {
            endpoints,
            start_height,
            block_processor,
        })
    }

    pub async fn run(self, stop_signal: CancellationToken) -> anyhow::Result<()> {
        let traverse_config = TraverseConfig::new(self.endpoints, self.start_height.into())
            .context("Failed setting up TraverseConfig")?;
        let processor_config = concordium_rust_sdk::indexer::ProcessorConfig::new()
            .set_stop_signal(stop_signal.cancelled_owned());

        info!("Indexing from block height {}", self.start_height);
        traverse_and_process(
            traverse_config,
            BlockIndexer,
            processor_config,
            self.block_processor,
        )
        .await?;
        Ok(())
    }
}

struct BlockIndexer;
#[async_trait]
impl Indexer for BlockIndexer {
    type Context = ();
    type Data = PreparedBlock;

    async fn on_connect<'a>(
        &mut self,
        endpoint: v2::Endpoint,
        _client: &'a mut v2::Client,
    ) -> QueryResult<Self::Context> {
        info!("Connection established to node at uri: {}", endpoint.uri());
        Ok(())
    }

    async fn on_finalized<'a>(
        &self,
        mut client: v2::Client,
        _ctx: &'a Self::Context,
        fbi: FinalizedBlockInfo,
    ) -> QueryResult<Self::Data> {
        let block_info = client.get_block_info(fbi.height).await?.response;
        let events: Vec<_> = client
            .get_block_transaction_events(fbi.height)
            .await?
            .response
            .try_collect()
            .await?;
        let chain_parameters = client
            .get_block_chain_parameters(fbi.height)
            .await?
            .response;
        let tokenomics_info = client.get_tokenomics_info(fbi.height).await?.response;
        let data = BlockData {
            finalized_block_info: fbi,
            block_info,
            events,
            chain_parameters,
            tokenomics_info,
        };
        Ok(PreparedBlock::prepare(&data).map_err(|err| RPCError::ParseError(err))?)
    }

    async fn on_failure(
        &mut self,
        _ep: v2::Endpoint,
        _successive_failures: u64,
        _err: TraverseError,
    ) -> bool {
        // TODO: Add logging
        true
    }
}

/// Type implementing the `ProcessEvent` handling the insertion of prepared blocks.
struct BlockProcessor {
    /// Database connection pool
    pool: PgPool,
    /// The last finalized block height according to the latest indexed block.
    /// This is needed in order to compute the finalization time of blocks.
    last_finalized_height: i64,
    /// The last finalized block hash according to the latest indexed block.
    /// This is needed in order to compute the finalization time of blocks.
    last_finalized_hash: String,
}
impl BlockProcessor {
    /// Construct the block processor by loading the initial state from the database.
    /// This assumes at least the genesis block is in the database.
    async fn load_from_database(pool: PgPool) -> anyhow::Result<Self> {
        let rec = sqlx::query!(
            r#"
SELECT height, hash FROM blocks WHERE finalization_time IS NULL ORDER BY height ASC LIMIT 1
"#
        )
        .fetch_one(&pool)
        .await
        .context("Failed to query data for save context")?;

        Ok(Self {
            pool,
            last_finalized_height: rec.height,
            last_finalized_hash: rec.hash,
        })
    }
}

#[async_trait]
impl ProcessEvent for BlockProcessor {
    /// The type of events that are to be processed. Typically this will be all
    /// of the transactions of interest for a single block."]
    type Data = PreparedBlock;

    /// An error that can be signalled.
    type Error = anyhow::Error; // TODO: introduce proper error type

    /// A description returned by the [`process`](ProcessEvent::process) method.
    /// This message is logged by the [`ProcessorConfig`] and is intended to
    /// describe the data that was just processed.
    type Description = String;

    /// Process a single item. This should work atomically in the sense that
    /// either the entire `data` is processed or none of it is in case of an
    /// error. This property is relied upon by the [`ProcessorConfig`] to retry
    /// failed attempts.
    async fn process(&mut self, data: &Self::Data) -> Result<Self::Description, Self::Error> {
        // TODO: Improve this by batching blocks within some time frame into the same
        // DB-transaction.
        // TODO: Handle failures and probably retry a few times
        let mut tx = self
            .pool
            .begin()
            .await
            .context("Failed to create SQL transaction")?;
        data.save(&mut self, &mut tx)
            .await
            .context("Failed saving block")?;
        tx.commit()
            .await
            .context("Failed to commit SQL transaction")?;
        Ok(format!("Processed block {}:{}", data.height, data.hash))
    }

    /// The `on_failure` method is invoked by the [`ProcessorConfig`] when it
    /// fails to process an event. It is meant to retry to recreate the
    /// resources, such as a database connection, that might have been
    /// dropped. The return value should signal if the handler process
    /// should continue (`true`) or not.
    ///
    /// The function takes the `error` that occurred at the latest
    /// [`process`](Self::process) call that just failed, and the number of
    /// attempts of calling `process` that failed.
    async fn on_failure(
        &mut self,
        _error: Self::Error,
        _failed_attempts: u32,
    ) -> Result<bool, Self::Error> {
        Ok(true)
    }
}

/// Information for a block which is relevant for storing it into the database.
struct BlockData {
    finalized_block_info: FinalizedBlockInfo,
    block_info: BlockInfo,
    events: Vec<BlockItemSummary>,
    chain_parameters: ChainParameters,
    tokenomics_info: RewardsOverview,
}

pub async fn save_genesis_data(endpoint: v2::Endpoint, pool: &PgPool) -> anyhow::Result<()> {
    let mut client = v2::Client::new(endpoint).await?;
    let genesis_height = v2::BlockIdentifier::AbsoluteHeight(0.into());

    let mut tx = pool
        .begin()
        .await
        .context("Failed to create SQL transaction")?;

    let genesis_block_info = client.get_block_info(genesis_height).await?.response;
    let block_hash = genesis_block_info.block_hash.to_string();
    let slot_time = genesis_block_info.block_slot_time.naive_utc();
    let baker_id = if let Some(index) = genesis_block_info.block_baker {
        Some(i64::try_from(index.id.index)?)
    } else {
        None
    };
    let genesis_tokenomics = client.get_tokenomics_info(genesis_height).await?.response;
    let common_reward = match genesis_tokenomics {
        RewardsOverview::V0 { data } => data,
        RewardsOverview::V1 { common, .. } => common,
    };
    let total_staked = match genesis_tokenomics {
        RewardsOverview::V0 { .. } => {
            // TODO Compute the total staked capital.
            0i64
        },
        RewardsOverview::V1 {
            total_staked_capital,
            ..
        } => i64::try_from(total_staked_capital.micro_ccd())?,
    };

    let total_amount = i64::try_from(common_reward.total_amount.micro_ccd())?;
    sqlx::query!(
            r#"INSERT INTO blocks (height, hash, slot_time, block_time, baker_id, total_amount, total_staked) VALUES ($1, $2, $3, 0, $4, $5, $6);"#,
            0,
            block_hash,
            slot_time,
            baker_id,
        total_amount,
        total_staked
        )
        .execute(&mut *tx)
            .await?;

    let mut genesis_accounts = client.get_account_list(genesis_height).await?.response;
    while let Some(account) = genesis_accounts.try_next().await? {
        let info = client
            .get_account_info(&account.into(), genesis_height)
            .await?
            .response;
        let index = i64::try_from(info.account_index.index)?;
        let account_address = account.to_string();
        let amount = i64::try_from(info.account_amount.micro_ccd)?;

        sqlx::query!(
            r#"INSERT INTO accounts (index, address, created_block, amount)
        VALUES ($1, $2, $3, $4)"#,
            index,
            account_address,
            0,
            amount
        )
        .execute(&mut *tx)
        .await?;
    }
    tx.commit()
        .await
        .context("Failed to commit SQL transaction")?;
    Ok(())
}

pub struct PreparedBlock {
    hash: String,
    height: i64,
    slot_time: NaiveDateTime,
    baker_id: Option<i64>,
    total_amount: i64,
    total_staked: i64,
    block_last_finalized: String,
    prepared_block_items: Vec<PreparedBlockItem>,
}

impl PreparedBlock {
    fn prepare(data: &BlockData) -> anyhow::Result<Self> {
        let height = i64::try_from(data.finalized_block_info.height.height)?;
        let hash = data.finalized_block_info.block_hash.to_string();
        let block_last_finalized = data.block_info.block_last_finalized.to_string();
        let slot_time = data.block_info.block_slot_time.naive_utc();
        let baker_id = if let Some(index) = data.block_info.block_baker {
            Some(i64::try_from(index.id.index)?)
        } else {
            None
        };
        let common_reward_data = match data.tokenomics_info {
            RewardsOverview::V0 { data } => data,
            RewardsOverview::V1 { common, .. } => common,
        };
        let total_amount = i64::try_from(common_reward_data.total_amount.micro_ccd())?;
        let total_staked = match data.tokenomics_info {
            RewardsOverview::V0 { .. } => {
                // TODO Compute the total staked capital.
                0i64
            },
            RewardsOverview::V1 {
                total_staked_capital,
                ..
            } => i64::try_from(total_staked_capital.micro_ccd())?,
        };

        let mut prepared_block_items = Vec::new();
        for block_item in data.events.iter() {
            prepared_block_items.push(PreparedBlockItem::prepare(data, block_item)?)
        }

        Ok(Self {
            hash,
            height,
            slot_time,
            baker_id,
            total_amount,
            total_staked,
            block_last_finalized,
            prepared_block_items,
        })
    }

    async fn save(
        &self,
        context: &mut BlockProcessor,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            r#"INSERT INTO blocks (height, hash, slot_time, block_time, baker_id, total_amount, total_staked)
VALUES ($1, $2, $3,
  (SELECT EXTRACT("MILLISECONDS" FROM $3 - b.slot_time) FROM blocks b WHERE b.height=($1 - 1::bigint)),
  $4, $5, $6);"#,
            self.height,
            self.hash,
            self.slot_time,
            self.baker_id,
            self.total_amount,
            self.total_staked
        )
        .execute(tx.as_mut())
            .await?;

        // Check if this block knows of a new finalized block.
        // If so, mark the blocks since last time as finalized by this block.
        if self.block_last_finalized != context.last_finalized_hash {
            let last_height = context.last_finalized_height;

            let rec = sqlx::query!(
                r#"
WITH finalizer
   AS (SELECT height FROM blocks WHERE hash = $1)
UPDATE blocks b
   SET finalization_time = EXTRACT("MILLISECONDS" FROM $3 - b.slot_time),
       finalized_by = finalizer.height
FROM finalizer
WHERE $2 <= b.height AND b.height < finalizer.height
RETURNING finalizer.height"#,
                self.block_last_finalized,
                last_height,
                self.slot_time
            )
            .fetch_one(tx.as_mut())
            .await
            .context("Failed updating finalization_time")?;

            context.last_finalized_height = rec.height;
            context.last_finalized_hash = self.block_last_finalized.clone();
        }

        for item in self.prepared_block_items.iter() {
            item.save(context, tx).await?;
        }
        Ok(())
    }
}

struct PreparedBlockItem {
    block_index: i64,
    tx_hash: String,
    ccd_cost: i64,
    energy_cost: i64,
    height: i64,
    sender: Option<String>,
    transaction_type: DbTransactionType,
    account_type: Option<AccountTransactionType>,
    credential_type: Option<CredentialDeploymentTransactionType>,
    update_type: Option<UpdateTransactionType>,
    success: bool,
    events: Option<serde_json::Value>,
    reject: Option<serde_json::Value>,
    prepared_event: PreparedEvent,
}

impl PreparedBlockItem {
    fn prepare(data: &BlockData, block_item: &BlockItemSummary) -> anyhow::Result<Self> {
        let height = i64::try_from(data.finalized_block_info.height.height)?;
        let block_index = i64::try_from(block_item.index.index)?;
        let tx_hash = block_item.hash.to_string();
        let ccd_cost = i64::try_from(
            data.chain_parameters
                .ccd_cost(block_item.energy_cost)
                .micro_ccd,
        )?;
        let energy_cost = i64::try_from(block_item.energy_cost.energy)?;
        let sender = block_item.sender_account().map(|a| a.to_string());
        let (transaction_type, account_type, credential_type, update_type) =
            match &block_item.details {
                BlockItemSummaryDetails::AccountTransaction(details) => {
                    let account_transaction_type =
                        details.transaction_type().map(AccountTransactionType::from);
                    (
                        DbTransactionType::Account,
                        account_transaction_type,
                        None,
                        None,
                    )
                },
                BlockItemSummaryDetails::AccountCreation(details) => {
                    let credential_type =
                        CredentialDeploymentTransactionType::from(details.credential_type);
                    (
                        DbTransactionType::CredentialDeployment,
                        None,
                        Some(credential_type),
                        None,
                    )
                },
                BlockItemSummaryDetails::Update(details) => {
                    let update_type = UpdateTransactionType::from(details.update_type());
                    (DbTransactionType::Update, None, None, Some(update_type))
                },
            };
        let success = block_item.is_success();
        let (events, reject) = if success {
            let events = serde_json::to_value(&events_from_summary(block_item.details.clone())?)?;
            (Some(events), None)
        } else {
            let reject =
                if let BlockItemSummaryDetails::AccountTransaction(AccountTransactionDetails {
                    effects: AccountTransactionEffects::None { reject_reason, .. },
                    ..
                }) = &block_item.details
                {
                    serde_json::to_value(crate::graphql_api::TransactionRejectReason::try_from(
                        reject_reason.clone(),
                    )?)?
                } else {
                    anyhow::bail!("Invariant violation: Failed transaction without a reject reason")
                };
            (None, Some(reject))
        };
        let prepared_event = match &block_item.details {
            BlockItemSummaryDetails::AccountCreation(details) => {
                PreparedEvent::AccountCreation(PreparedAccountCreation::prepare(
                    data,
                    &block_item,
                    details,
                )?)
            },
            details => {
                todo!("details = \n {:#?}", details)
            },
        };

        Ok(Self {
            block_index,
            tx_hash,
            ccd_cost,
            energy_cost,
            height,
            sender,
            transaction_type,
            account_type,
            credential_type,
            update_type,
            success,
            events,
            reject,
            prepared_event,
        })
    }

    async fn save(
        &self,
        context: &mut BlockProcessor,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
    ) -> anyhow::Result<()> {
        sqlx::query!(
                r#"INSERT INTO transactions
(index, hash, ccd_cost, energy_cost, block, sender, type, type_account, type_credential_deployment, type_update, success, events, reject)
VALUES
($1, $2, $3, $4, $5, (SELECT index FROM accounts WHERE address=$6), $7, $8, $9, $10, $11, $12, $13);"#,
            self.block_index,
            self.tx_hash,
            self.ccd_cost,
            self.energy_cost,
            self.height,
            self.sender,
            self.transaction_type as DbTransactionType,
            self.account_type as Option<AccountTransactionType>,
            self.credential_type as Option<CredentialDeploymentTransactionType>,
            self.update_type as Option<UpdateTransactionType>,
            self.success,
            self.events,
            self.reject)
            .execute(tx.as_mut())
            .await?;

        match &self.prepared_event {
            PreparedEvent::AccountCreation(event) => event.save(context, tx).await?,
        }
        Ok(())
    }
}

enum PreparedEvent {
    AccountCreation(PreparedAccountCreation),
}

struct PreparedAccountCreation {
    account_address: String,
    height: i64,
    block_index: i64,
}

impl PreparedAccountCreation {
    fn prepare(
        data: &BlockData,
        block_item: &BlockItemSummary,
        details: &concordium_rust_sdk::types::AccountCreationDetails,
    ) -> anyhow::Result<Self> {
        let height = i64::try_from(data.finalized_block_info.height.height)?;
        let block_index = i64::try_from(block_item.index.index)?;
        Ok(Self {
            account_address: details.address.to_string(),
            height,
            block_index,
        })
    }

    async fn save(
        &self,
        _context: &mut BlockProcessor,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
    ) -> anyhow::Result<()> {
        sqlx::query!(
            r#"INSERT INTO accounts (index, address, created_block, created_index, amount)
VALUES ((SELECT COALESCE(MAX(index) + 1, 0) FROM accounts), $1, $2, $3, 0)"#,
            self.account_address,
            self.height,
            self.block_index
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }
}
