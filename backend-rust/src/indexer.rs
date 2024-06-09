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
    indexer::{
        async_trait,
        Indexer,
        TraverseConfig,
        TraverseError,
    },
    types::{
        queries::BlockInfo,
        BlockItemSummary,
        BlockItemSummaryDetails,
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
    pool: PgPool,
    sender: mpsc::Sender<BlockData>,
) -> anyhow::Result<()> {
    let rec = sqlx::query!(
        r#"
SELECT MAX(height) FROM blocks
"#
    )
    .fetch_one(&pool)
    .await?;
    let last_height_stored = rec.max;

    if last_height_stored.is_none() {
        save_genesis_data(endpoints[0].clone(), &pool).await?;
    }

    let start_height = if let Some(height) = last_height_stored {
        u64::try_from(height).unwrap() + 1u64
    } else {
        1
    };

    let config = TraverseConfig::new(endpoints, start_height.into())
        .context("No gRPC endpoints provided")?;
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
    while let Some(res) = receiver.recv().await {
        println!(
            "Saving {}:{}",
            res.finalized_block_info.height, res.finalized_block_info.block_hash
        );
        res.save_to_database(&pool)
            .await
            .expect("Failed saving block")
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

        Ok(BlockData {
            finalized_block_info: fbi,
            block_info,
            events,
            chain_parameters,
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

pub struct BlockData {
    finalized_block_info: FinalizedBlockInfo,
    block_info: BlockInfo,
    events: Vec<BlockItemSummary>,
    chain_parameters: ChainParameters,
}

impl BlockData {
    // Relies on blocks being stored sequencially.
    async fn save_to_database(self, pool: &PgPool) -> anyhow::Result<()> {
        let mut tx = pool
            .begin()
            .await
            .context("Failed to create SQL transaction")?;

        let height = i64::try_from(self.finalized_block_info.height.height)?;
        let block_hash = self.finalized_block_info.block_hash.to_string();
        let slot_time = self.block_info.block_slot_time.naive_utc();
        let baker_id = if let Some(index) = self.block_info.block_baker {
            Some(i64::try_from(index.id.index)?)
        } else {
            None
        };

        sqlx::query!(
            r#"INSERT INTO blocks (height, hash, slot_time, finalized, baker_id) VALUES ($1, $2, $3, $4, $5);"#,
            height,
            block_hash,
            slot_time,
            self.block_info.finalized,
            baker_id
        )
        .execute(&mut *tx)
            .await?;

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
            let details = serde_json::to_value(&events_from_summary(block_item.details.clone())?)?;

            sqlx::query(
                r#"INSERT INTO transactions
(index, hash, ccd_cost, energy_cost, block, sender, type, type_account, type_credential_deployment, type_update, success, details)
VALUES
($1, $2, $3, $4, $5, (SELECT index FROM accounts WHERE address=$6), $7, $8, $9, $10, $11, $12);"#)
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
                .bind(details)
            .execute(&mut *tx)
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
                    .execute(&mut *tx)
                    .await?;
                },
                _ => {},
            }
        }

        tx.commit()
            .await
            .context("Failed to commit SQL transaction")?;
        Ok(())
    }
}

async fn save_genesis_data(endpoint: v2::Endpoint, pool: &PgPool) -> anyhow::Result<()> {
    let mut client = v2::Client::new(endpoint).await?;
    let genesis_height = v2::BlockIdentifier::AbsoluteHeight(0.into());

    let mut tx = pool
        .begin()
        .await
        .context("Failed to create SQL transaction")?;

    let genesis_block_info = client.get_block_info(genesis_height).await?.response;
    let block_hash = genesis_block_info.block_hash.to_string();
    let slot_time = genesis_block_info.block_slot_time.naive_utc();
    let finalized = genesis_block_info.finalized;
    let baker_id = if let Some(index) = genesis_block_info.block_baker {
        Some(i64::try_from(index.id.index)?)
    } else {
        None
    };
    sqlx::query!(
            r#"INSERT INTO blocks (height, hash, slot_time, finalized, baker_id) VALUES ($1, $2, $3, $4, $5);"#,
            0,
            block_hash,
            slot_time,
            finalized,
            baker_id
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
