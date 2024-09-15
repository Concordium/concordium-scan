//! TODO:
//! - Check endpoints are using the same chain.

use crate::graphql_api::{
    events_from_summary,
    AccountTransactionType,
    CredentialDeploymentTransactionType,
    DbTransactionType,
    UpdateTransactionType,
};
use anyhow::Context;
use concordium_rust_sdk::{
    base::hashes::BlockHash,
    indexer::{
        async_trait,
        Indexer,
        TraverseConfig,
        TraverseError,
    },
    types::{
        queries::BlockInfo,
        AbsoluteBlockHeight,
        AccountTransactionDetails,
        AccountTransactionEffects,
        BlockHeight,
        BlockItemSummary,
        BlockItemSummaryDetails,
        RewardsOverview,
    },
    v2::{
        self,
        ChainParameters,
        FinalizedBlockInfo,
        QueryResult,
    },
};
use futures::TryStreamExt;
use sqlx::PgPool;
use tokio::sync::mpsc;

pub async fn traverse_chain(
    endpoints: Vec<v2::Endpoint>,
    sender: mpsc::Sender<BlockData>,
    start_height: AbsoluteBlockHeight,
) -> anyhow::Result<()> {
    let config =
        TraverseConfig::new(endpoints, start_height).context("No gRPC endpoints provided")?;
    let indexer = BlockIndexer;

    println!("Indexing from {}", start_height);
    config
        .traverse(indexer, sender)
        .await
        .context("Failed traversing the blocks in the chain")
}

pub async fn save_blocks(
    mut receiver: mpsc::Receiver<BlockData>,
    pool: PgPool,
) -> anyhow::Result<()> {
    let mut context = SaveContext::load_from_database(&pool).await?;

    while let Some(res) = receiver.recv().await {
        // TODO: Improve this by batching blocks within some time frame into the same
        // DB-transaction.
        // TODO: Handle failures and probably retry a few times
        println!(
            "Saving {}:{}",
            res.finalized_block_info.height, res.finalized_block_info.block_hash
        );
        let mut tx = pool
            .begin()
            .await
            .context("Failed to create SQL transaction")?;
        res.save_to_database(&mut context, &mut tx)
            .await
            .context("Failed saving block")?;
        tx.commit()
            .await
            .context("Failed to commit SQL transaction")?;
    }
    Ok(())
}

struct BlockIndexer;

#[async_trait]
impl Indexer for BlockIndexer {
    type Context = ();
    type Data = BlockData;

    async fn on_connect<'a>(
        &mut self,
        _endpoint: v2::Endpoint,
        _client: &'a mut v2::Client,
    ) -> QueryResult<Self::Context> {
        println!("Indexer connection");
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
        Ok(BlockData {
            finalized_block_info: fbi,
            block_info,
            events,
            chain_parameters,
            tokenomics_info,
        })
    }

    async fn on_failure(
        &mut self,
        _ep: v2::Endpoint,
        _successive_failures: u64,
        _err: TraverseError,
    ) -> bool {
        true
    }
}

/// Information for a block which is relevant for storing it into the database.
pub struct BlockData {
    finalized_block_info: FinalizedBlockInfo,
    block_info: BlockInfo,
    events: Vec<BlockItemSummary>,
    chain_parameters: ChainParameters,
    tokenomics_info: RewardsOverview,
}

/// Cross block context
struct SaveContext {
    /// The last finalized block height according to the latest indexed block.
    /// This is needed in order to compute the finalization time of blocks.
    last_finalized_height: BlockHeight,
    /// The last finalized block hash according to the latest indexed block.
    /// This is needed in order to compute the finalization time of blocks.
    last_finalized_hash: BlockHash,
}

impl SaveContext {
    /// The genesis block must already be in the database.
    async fn load_from_database(pool: &PgPool) -> anyhow::Result<Self> {
        let rec = sqlx::query!(
            r#"
SELECT height, hash FROM blocks WHERE finalization_time IS NULL ORDER BY height ASC LIMIT 1
"#
        )
        .fetch_one(pool)
        .await
        .context("Failed to query data for save context")?;

        Ok(Self {
            last_finalized_height: BlockHeight::from(u64::try_from(rec.height)?),
            last_finalized_hash: rec.hash.parse()?,
        })
    }
}

impl BlockData {
    /// Relies on blocks being stored sequentially.
    /// The genesis block must already be in the database.
    async fn save_to_database(
        self,
        context: &mut SaveContext,
        tx: &mut sqlx::Transaction<'static, sqlx::Postgres>,
    ) -> anyhow::Result<()> {
        let height = i64::try_from(self.finalized_block_info.height.height)?;
        let block_hash = self.finalized_block_info.block_hash.to_string();
        let slot_time = self.block_info.block_slot_time.naive_utc();
        let baker_id = if let Some(index) = self.block_info.block_baker {
            Some(i64::try_from(index.id.index)?)
        } else {
            None
        };
        let common_reward_data = match self.tokenomics_info {
            RewardsOverview::V0 { data } => data,
            RewardsOverview::V1 { common, .. } => common,
        };
        let total_amount = i64::try_from(common_reward_data.total_amount.micro_ccd())?;
        let total_staked = match self.tokenomics_info {
            RewardsOverview::V0 { .. } => {
                // TODO Compute the total staked capital.
                0i64
            },
            RewardsOverview::V1 {
                total_staked_capital,
                ..
            } => i64::try_from(total_staked_capital.micro_ccd())?,
        };

        sqlx::query!(
            r#"INSERT INTO blocks (height, hash, slot_time, block_time, baker_id, total_amount, total_staked)
VALUES ($1, $2, $3,
  (SELECT EXTRACT("MILLISECONDS" FROM $3 - b.slot_time) FROM blocks b WHERE b.height=($1 - 1::bigint)),
  $4, $5, $6);"#,
            height,
            block_hash,
            slot_time,
            baker_id,
            total_amount,
            total_staked
        )
        .execute(tx.as_mut())
            .await?;

        // Check if this block knows of a new finalized block.
        // If so, mark the blocks since last time as finalized by this block.
        if self.block_info.block_last_finalized != context.last_finalized_hash {
            let last_height = i64::try_from(context.last_finalized_height.height)?;
            let new_last_finalized_hash = self.block_info.block_last_finalized.to_string();

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
                new_last_finalized_hash,
                last_height,
                slot_time
            )
            .fetch_one(tx.as_mut())
            .await
            .context("Failed updating finalization_time")?;

            context.last_finalized_height = u64::try_from(rec.height)?.into();
            context.last_finalized_hash = self.block_info.block_last_finalized;
        }

        for block_item in self.events {
            let block_index = i64::try_from(block_item.index.index).unwrap();
            let tx_hash = block_item.hash.to_string();
            let ccd_cost = i64::try_from(
                self.chain_parameters
                    .ccd_cost(block_item.energy_cost)
                    .micro_ccd,
            )
            .unwrap();
            let energy_cost = i64::try_from(block_item.energy_cost.energy).unwrap();
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
                let events =
                    serde_json::to_value(&events_from_summary(block_item.details.clone())?)?;
                (Some(events), None)
            } else {
                let reject = if let BlockItemSummaryDetails::AccountTransaction(
                    AccountTransactionDetails {
                        effects: AccountTransactionEffects::None { reject_reason, .. },
                        ..
                    },
                ) = &block_item.details
                {
                    serde_json::to_value(crate::graphql_api::TransactionRejectReason::try_from(
                        reject_reason.clone(),
                    )?)?
                } else {
                    anyhow::bail!("Invariant violation: Failed transaction without a reject reason")
                };
                (None, Some(reject))
            };

            sqlx::query(
                r#"INSERT INTO transactions
(index, hash, ccd_cost, energy_cost, block, sender, type, type_account, type_credential_deployment, type_update, success, events, reject)
VALUES
($1, $2, $3, $4, $5, (SELECT index FROM accounts WHERE address=$6), $7, $8, $9, $10, $11, $12, $13);"#)
            .bind(block_index)
                .bind(tx_hash)
                .bind(ccd_cost)
                .bind(energy_cost)
                .bind(height)
                .bind(sender)
                .bind(transaction_type)
                .bind(account_type)
                .bind(credential_type)
                .bind(update_type)
                .bind(success)
                .bind(events)
                                .bind(reject)
            .execute(tx.as_mut())
            .await?;

            match block_item.details {
                BlockItemSummaryDetails::AccountCreation(details) => {
                    let account_address = details.address.to_string();
                    sqlx::query!(
                        r#"INSERT INTO accounts (index, address, created_block, created_index, amount)
VALUES ((SELECT COALESCE(MAX(index) + 1, 0) FROM accounts), $1, $2, $3, 0)"#,
                        account_address,
                        height,
                        block_index
                    )
                    .execute(tx.as_mut())
                    .await?;
                },
                _ => {},
            }
        }

        Ok(())
    }
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
