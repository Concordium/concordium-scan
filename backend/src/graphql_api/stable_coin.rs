use crate::address::AccountAddress;
use anyhow::Context as AnyhowContext;
use async_graphql::{Context, Object, SimpleObject};
use chrono::{Duration, TimeZone, Utc};
use clap::Parser;
use concordium_rust_sdk::v2::{self, BlockIdentifier};
use futures::StreamExt;
use serde::Deserialize;
use std::{
    collections::{HashMap, HashSet},
    fs::File,
    io::BufReader,
};

pub type DateTime = chrono::DateTime<chrono::Utc>;

#[derive(Clone, Deserialize, SimpleObject)]
pub struct StableCoin {
    name:                 String,
    symbol:               String,
    decimal:              u8,
    // Total supply = Created tokens – Burned tokens.
    total_supply:         i64,
    // Circulating supply = Tokens available on the market.
    circulating_supply:   i64,
    value_in_dollar:      f64,
    total_unique_holders: Option<i64>,
    transfers:            Option<Vec<Transfer>>, // Transfers sorted by date
    holdings:             Option<Vec<HoldingResponse>>,
    metadata:             Option<Metadata>,
    transactions:         Option<Vec<TransactionMResponse>>, /* TransactionM type is only for
                                                              * the mock dataset */
    issuer:               String, // Keeping this issuer as string as reading from json
}
#[derive(Clone, Deserialize, SimpleObject)]
pub struct TransactionM {
    from:             AccountAddress,
    to:               AccountAddress,
    asset_name:       String,
    date_time:        DateTime,
    amount:           f64,
    value:            f64,
    transaction_hash: String,
}
#[derive(Clone, Deserialize, SimpleObject)]
pub struct TransactionMResponse {
    from:             String,
    to:               String,
    asset_name:       String,
    date_time:        DateTime,
    amount:           f64,
    value:            f64,
    transaction_hash: String,
}

#[derive(Clone, Deserialize, SimpleObject)]
pub struct LatestTransactionResponse {
    from:             String,
    to:               String,
    asset_name:       String,
    date_time:        DateTime,
    amount:           f64,
    value:            f64,
    transaction_hash: String,
    asset_metadata:   Option<Metadata>,
}

#[derive(Debug, Clone, Deserialize, SimpleObject)]
pub struct Metadata {
    icon_url: String,
}

#[derive(Debug, Clone, Deserialize, SimpleObject)]
pub struct StableCoinOverview {
    // Total market cap = Total supply * Value in dollar
    // For Our mockdata we are skipping the calculation of market cap
    // and just using the total supply as market cap
    total_marketcap:            f64,
    // Number of unique holders = Number of unique addresses holding the token
    number_of_unique_holders:   usize,
    no_of_txn:                  usize,
    values_transferred:         f64,
    no_of_txn_last24h:          usize,
    values_transferred_last24h: f64,
}

#[derive(Clone, Deserialize, SimpleObject)]
pub struct Transfer {
    from:       AccountAddress,
    to:         AccountAddress,
    asset_name: String,
    date_time:  DateTime,
    amount:     f64,
}

#[derive(Debug, Clone, Deserialize, SimpleObject)]
pub struct AssetInHold {
    asset_name: String,
    quantity:   f64,
    percentage: f32,
}

#[derive(Clone, Deserialize, SimpleObject)]
pub struct Holding {
    address:  AccountAddress,
    holdings: Option<Vec<AssetInHold>>,
}

#[derive(Clone, Deserialize, SimpleObject)]
pub struct HoldingResponse {
    address:    String,
    asset_name: String,
    quantity:   f64,
    percentage: f32,
}

#[derive(Debug, Clone, SimpleObject)]
pub struct TransferSummary {
    date_time:         DateTime,
    total_amount:      f64,
    transaction_count: usize,
}

#[derive(Debug, Clone, SimpleObject)]
pub struct TransferSummaryResponse {
    daily_summary:   Vec<TransferSummary>,
    total_txn_count: usize,
    total_value:     f64,
}

impl StableCoin {
    pub fn top_holders(
        &self,
        limit: Option<usize>,
        min_quantity: Option<f64>,
    ) -> Option<Vec<HoldingResponse>> {
        if let Some(holdings) = &self.holdings {
            let mut filtered_holders = holdings
                .iter()
                .filter(|h| {
                    h.asset_name == self.symbol
                        && (min_quantity.is_none() || h.quantity >= min_quantity.unwrap())
                })
                .cloned()
                .collect::<Vec<HoldingResponse>>();

            // Sort by quantity in descending order
            filtered_holders.sort_by(|a, b| b.quantity.partial_cmp(&a.quantity).unwrap());

            // Limit the number of holders if a limit is provided
            if let Some(l) = limit {
                filtered_holders.truncate(l);
            }

            Some(filtered_holders)
        } else {
            None
        }
    }

    pub fn last_n_transactions(
        &self,
        last_n_transactions: Option<usize>,
    ) -> Option<Vec<TransactionMResponse>> {
        let mut transactions = self.transactions.clone()?;
        transactions.sort_by(|a, b| b.date_time.cmp(&a.date_time));
        let last_n = last_n_transactions.unwrap_or(20);
        Some(transactions.into_iter().take(last_n).collect())
    }
}

#[derive(Parser)]
struct App {
    #[arg(
        long = "node",
        env = "CCDSCAN_INDEXER_GRPC_ENDPOINTS",
        default_value = "http://localhost:20000"
    )]
    endpoint: v2::Endpoint,
}

#[derive(Default)]
pub(crate) struct QueryStableCoins;

impl QueryStableCoins {
    fn load_data() -> Vec<StableCoin> {
        let file = File::open("stablecoin.json").unwrap();
        let reader = BufReader::new(file);
        serde_json::from_reader(reader).unwrap()
    }

    fn load_transfers() -> Vec<Transfer> {
        let file = File::open("transfers.json").unwrap();
        let reader = BufReader::new(file);
        let mut transfers: Vec<Transfer> = serde_json::from_reader(reader).unwrap();

        // Sort transfers by date in descending order (newest first)
        transfers.sort_by(|a, b| b.date_time.cmp(&a.date_time));
        transfers
    }

    fn load_holdings() -> Vec<Holding> {
        let file = File::open("walletholdings.json").unwrap();
        let reader = BufReader::new(file);
        serde_json::from_reader(reader).unwrap()
    }

    fn load_transactions() -> Vec<TransactionM> {
        let file = File::open("transaction.json").unwrap();
        let reader = BufReader::new(file);
        serde_json::from_reader(reader).unwrap()
    }

    fn merge_transfers(
        mut stablecoins: Vec<StableCoin>,
        transfers: Vec<Transfer>,
        holdings: Vec<Holding>,
        transactions: Vec<TransactionM>,
    ) -> Vec<StableCoin> {
        for coin in &mut stablecoins {
            coin.transfers =
                Some(transfers.iter().filter(|t| t.asset_name == coin.symbol).cloned().collect());

            let relevant_holdings: Vec<HoldingResponse> = holdings
                .iter()
                .filter_map(|holding| {
                    holding.holdings.as_ref().and_then(|h| {
                        h.iter().find(|asset| asset.asset_name == coin.symbol).map(|asset| {
                            HoldingResponse {
                                address:    holding.address.to_string(),
                                asset_name: asset.asset_name.clone(),
                                quantity:   asset.quantity,
                                percentage: asset.percentage,
                            }
                        })
                    })
                })
                .collect();

            coin.holdings = if relevant_holdings.is_empty() {
                None
            } else {
                Some(relevant_holdings.clone())
            };

            // Compute unique holders
            let unique_holders: HashSet<String> =
                relevant_holdings.iter().map(|h| h.address.to_string()).collect();

            coin.total_unique_holders = Some(unique_holders.len() as i64);
            coin.transactions = Some(
                transactions
                    .iter()
                    .filter(|t| t.asset_name == coin.symbol)
                    .map(|t| TransactionMResponse {
                        from:             t.from.to_string(),
                        to:               t.to.to_string(),
                        asset_name:       t.asset_name.clone(),
                        date_time:        t.date_time,
                        amount:           t.amount,
                        value:            t.value,
                        transaction_hash: t.transaction_hash.clone(),
                    })
                    .collect(),
            );
        }
        stablecoins
    }
}

#[Object]
impl QueryStableCoins {
    async fn stablecoin<'a>(
        &self,
        _ctx: &Context<'a>,
        symbol: String,
        limit: Option<usize>,
        min_quantity: Option<f64>,
        last_n_transactions: Option<usize>,
    ) -> Option<StableCoin> {
        let mut stablecoins = Self::merge_transfers(
            Self::load_data(),
            Self::load_transfers(),
            Self::load_holdings(),
            Self::load_transactions(),
        );
        let coin = stablecoins.iter_mut().find(|coin: &&mut StableCoin| coin.symbol == symbol)?;
        coin.holdings = coin.top_holders(limit, min_quantity);
        coin.transactions = coin.last_n_transactions(last_n_transactions);
        Some(coin.clone())
    }

    async fn stablecoins<'a>(&self, _ctx: &Context<'a>) -> Vec<StableCoin> {
        Self::merge_transfers(
            Self::load_data(),
            Self::load_transfers(),
            Self::load_holdings(),
            Self::load_transactions(),
        )
    }

    async fn stablecoins_by_supply<'a>(
        &self,
        _ctx: &Context<'a>,
        min_supply: i64,
    ) -> Vec<StableCoin> {
        Self::merge_transfers(
            Self::load_data(),
            Self::load_transfers(),
            Self::load_holdings(),
            Self::load_transactions(),
        )
        .into_iter()
        .filter(|coin| coin.total_supply >= min_supply)
        .collect()
    }

    async fn transfer_summary<'a>(
        &self,
        _ctx: &Context<'a>,
        asset_name: String,
        days: Option<i64>, // Number of days to fetch
    ) -> TransferSummaryResponse {
        let transfers = Self::load_transfers();
        let now: chrono::DateTime<Utc> = Utc::now();
        let days = days.unwrap_or(7);
        let days = if days <= 7 {
            6
        } else {
            days - 1
        };
        let last_n_days = now - chrono::Duration::days(days);
        let mut summary: HashMap<DateTime, (f64, usize)> = HashMap::new();
        let mut total_value = 0.0;
        let mut total_txn_count = 0;

        for transfer in transfers.iter().filter(|t| t.asset_name == asset_name) {
            let transfer_date = transfer.date_time;
            // Filter transactions within the last `days`
            if transfer_date >= last_n_days {
                let date_str = Utc
                    .from_utc_datetime(&transfer_date.date_naive().and_hms_opt(0, 0, 0).unwrap());
                let entry = summary.entry(date_str).or_insert((0.0, 0));
                entry.0 += transfer.amount;
                entry.1 += 1;
                total_value += transfer.amount;
                total_txn_count += 1;
            }
        }

        let mut summary_vec: Vec<TransferSummary> = summary
            .into_iter()
            .map(|(date_time, (total_amount, transaction_count))| TransferSummary {
                date_time,
                total_amount,
                transaction_count,
            })
            .collect();

        // Sort in ascending order (earliest date first)
        summary_vec.sort_by(|a, b| a.date_time.cmp(&b.date_time));

        TransferSummaryResponse {
            daily_summary: summary_vec,
            total_txn_count,
            total_value,
        }
    }

    async fn stablecoin_overview<'a>(&self, _ctx: &Context<'a>) -> StableCoinOverview {
        let stablecoins = Self::load_data();
        let transfers = Self::load_transfers();
        let holdings: Vec<Holding> = Self::load_holdings();

        let unique_holders: HashSet<String> = holdings
            .iter()
            .filter_map(|holding| {
                holding.holdings.as_ref().and_then(|h| {
                    if h.is_empty() {
                        None
                    } else {
                        Some(holding.address.to_string())
                    }
                })
            })
            .collect();
        let now = Utc::now();
        let last_24h = now - Duration::hours(24);
        let last_24h_str = last_24h;

        let (values_transferred_last_24h, no_of_txn_last_24h) = transfers
            .iter()
            .filter(|t| t.date_time >= last_24h_str)
            .fold((0.0, 0), |(total_val, count), t| (total_val + t.amount, count + 1));

        StableCoinOverview {
            total_marketcap:            stablecoins
                .iter()
                .map(|coin| coin.total_supply as f64)
                .sum(),
            number_of_unique_holders:   unique_holders.len(),
            no_of_txn:                  transfers.len(),
            values_transferred:         transfers.iter().map(|t| t.amount).sum(),
            no_of_txn_last24h:          no_of_txn_last_24h,
            values_transferred_last24h: values_transferred_last_24h,
        }
    }

    async fn latest_transactions<'a>(
        &self,
        _ctx: &Context<'a>,
        limit: Option<usize>,
    ) -> Option<Vec<LatestTransactionResponse>> {
        let transactions = Self::load_transactions();
        let stablecoins = Self::load_data();
        let stablecoins_metadata_map: HashMap<String, Option<Metadata>> =
            stablecoins.into_iter().map(|s| (s.symbol, s.metadata)).collect();
        let effective_limit = limit.unwrap_or(10); // default to 10 if None
        let txn_summary: Option<Vec<LatestTransactionResponse>> = Some(
            transactions
                .iter()
                .take(effective_limit)
                .map(|t| LatestTransactionResponse {
                    from:             t.from.to_string(),
                    to:               t.to.to_string(),
                    asset_name:       t.asset_name.clone(),
                    date_time:        t.date_time,
                    amount:           t.amount,
                    value:            t.value,
                    transaction_hash: t.transaction_hash.clone(),
                    asset_metadata:   stablecoins_metadata_map
                        .get(&t.asset_name.clone())
                        .unwrap()
                        .clone(),
                })
                .collect(),
        );
        txn_summary
    }

    async fn live_stablecoins(&self, _ctx: &Context<'_>) -> anyhow::Result<Vec<StableCoin>> {
        let app = App::parse();
        let mut client = v2::Client::new(app.endpoint).await.context("Failed to create client")?;

        let mut response = client
            .get_token_list(&BlockIdentifier::LastFinal)
            .await
            .context("Failed to get token list")
            .unwrap();
        let mut coins: Vec<StableCoin> = vec![];

        while let Some(token_id) = response.response.next().await.transpose().unwrap() {
            let token_info =
                client.get_token_info(token_id.clone(), BlockIdentifier::LastFinal).await.unwrap();

            let token_state = token_info.response.token_state;
            let token_module_state = token_state.decode_module_state().unwrap();
            let name = token_module_state.name;

            let total_supply = token_state.total_supply.to_string().parse::<f64>().unwrap() as i64;

            let circulating_supply = total_supply; // Assumed same for now

            coins.push(StableCoin {
                name,
                symbol: String::from(token_id),
                total_supply,
                circulating_supply,
                decimal: token_state.decimals,
                value_in_dollar: 1.0, // Placeholder value
                total_unique_holders: None,
                transfers: None,
                holdings: None,
                metadata: None,
                issuer: token_state.issuer.to_string(),
                transactions: None,
            });
        }

        Ok(coins)
    }
}
