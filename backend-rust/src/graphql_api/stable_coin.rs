use async_graphql::{Context, Object, SimpleObject};
use chrono::{Duration, Utc};
use serde::Deserialize;
use std::{
    collections::{HashMap, HashSet},
    fs::File,
    io::BufReader,
};

#[derive(Debug, Clone, Deserialize, SimpleObject)]
pub struct StableCoin {
    name:                String,
    symbol:              String,
    decimal:             u8,
    total_supply:        i64,
    circulating_supply:  i64,
    value_in_doller:     f64,
    total_unique_holder: Option<i64>,
    transfers:           Option<Vec<Transfer>>, // Transfers sorted by date
    holding:             Option<Vec<Holding>>,
    metadata:            Option<Metadata>,
    transactions:        Option<Vec<TransactionM>>,
}
#[derive(Debug, Clone, Deserialize, SimpleObject)]
pub struct TransactionM {
    from:       String,
    to:         String,
    asset_name: String,
    date:       String,
    amount:     f64,
    value:      f64,
    signature:  String,
}

#[derive(Debug, Clone, Deserialize, SimpleObject)]
pub struct Metadata {
    icon_url: String,
}

#[derive(Debug, Clone, Deserialize, SimpleObject)]
pub struct StableCoinOverview {
    total_marketcap:          f64,
    number_of_unique_holder:  f64,
    no_of_txn:                f64,
    values_transferd:         f64,
    no_of_txn_last24h:        f64,
    values_transferd_last24h: f64,
}

#[derive(Debug, Clone, Deserialize, SimpleObject)]
pub struct Transfer {
    from:       String,
    to:         String,
    asset_name: String,
    date:       String,
    amount:     f64,
}

#[derive(Debug, Clone, Deserialize, SimpleObject)]
pub struct AssetInHold {
    asset_name: String,
    quantity:   f64,
    percentage: f32,
}

#[derive(Debug, Clone, Deserialize, SimpleObject)]
pub struct Holding {
    address:  String,
    holdings: Option<Vec<AssetInHold>>,
}

#[derive(Debug, Clone, SimpleObject)]
pub struct TransferSummary {
    date:              String,
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
        top_holder: Option<usize>,
        min_quantity: Option<f64>,
    ) -> Option<Vec<Holding>> {
        let mut holders = self.holding.clone()?;

        if let Some(min) = min_quantity {
            holders.retain(|holding| {
                holding
                    .holdings
                    .as_ref()
                    .map_or(false, |h| h.iter().any(|asset| asset.quantity >= min))
            });
        }

        holders.sort_by(|a, b| {
            let sum_a: f64 =
                a.holdings.as_ref().map_or(0.0, |h| h.iter().map(|x| x.quantity).sum());
            let sum_b: f64 =
                b.holdings.as_ref().map_or(0.0, |h| h.iter().map(|x| x.quantity).sum());
            sum_b.partial_cmp(&sum_a).unwrap()
        });

        let top_n = top_holder.unwrap_or(200);
        Some(holders.into_iter().take(top_n).collect())
    }

    pub fn last_n_transactions(
        &self,
        last_n_transactions: Option<usize>,
    ) -> Option<Vec<TransactionM>> {
        let mut transactions = self.transactions.clone()?;
        transactions.sort_by(|a, b| b.date.cmp(&a.date));
        let last_n = last_n_transactions.unwrap_or(20);
        Some(transactions.into_iter().take(last_n).collect())
    }
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
        transfers.sort_by(|a, b| b.date.cmp(&a.date));
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

            let relevant_holdings: Vec<Holding> = holdings
                .iter()
                .filter_map(|holding| {
                    let filtered_assets: Vec<AssetInHold> = holding
                        .holdings
                        .as_ref()?
                        .iter()
                        .filter(|asset| asset.asset_name == coin.symbol)
                        .cloned()
                        .collect();

                    if !filtered_assets.is_empty() {
                        Some(Holding {
                            address:  holding.address.clone(),
                            holdings: Some(filtered_assets),
                        })
                    } else {
                        None
                    }
                })
                .collect();

            coin.holding = if relevant_holdings.is_empty() {
                None
            } else {
                Some(relevant_holdings.clone())
            };

            // Compute unique holders
            let unique_holders: HashSet<String> =
                relevant_holdings.iter().map(|h| h.address.clone()).collect();

            coin.total_unique_holder = Some(unique_holders.len() as i64);
            coin.transactions = Some(
                transactions.iter().filter(|txn| txn.asset_name == coin.symbol).cloned().collect(),
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
        top_holder: Option<usize>,
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

        coin.holding = coin.top_holders(top_holder, min_quantity);
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
        let now = Utc::now();
        let days = days.unwrap_or(7);
        let last_n_days = now - Duration::days(days);

        let mut summary: HashMap<String, (f64, usize)> = HashMap::new();
        let mut total_value = 0.0;
        let mut total_txn_count = 0;

        for transfer in transfers.iter().filter(|t| t.asset_name == asset_name) {
            let transfer_date =
                match chrono::NaiveDateTime::parse_from_str(&transfer.date, "%Y-%m-%d %H:%M:%S") {
                    Ok(date) => date,
                    Err(_) => continue, // Skip invalid date format
                };

            // Filter transactions within the last `days`
            if transfer_date >= last_n_days.naive_utc() {
                let date_str = transfer_date.format("%Y-%m-%d").to_string();
                let entry = summary.entry(date_str).or_insert((0.0, 0));
                entry.0 += transfer.amount;
                entry.1 += 1;
                total_value += transfer.amount;
                total_txn_count += 1;
            }
        }

        let mut summary_vec: Vec<TransferSummary> = summary
            .into_iter()
            .map(|(date, (total_amount, transaction_count))| TransferSummary {
                date,
                total_amount,
                transaction_count,
            })
            .collect();

        // Sort in ascending order (earliest date first)
        summary_vec.sort_by(|a, b| a.date.cmp(&b.date));

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
                        Some(holding.address.clone())
                    }
                })
            })
            .collect();
        let now = Utc::now();
        let last_24h = now - Duration::hours(24);
        let last_24h_str = last_24h.format("%Y-%m-%d %H:%M:%S").to_string();

        let (values_transferd_last_24h, no_of_txn_last_24h) = transfers
            .iter()
            .filter(|t| t.date >= last_24h_str)
            .fold((0.0, 0), |(total_val, count), t| (total_val + t.amount, count + 1));

        StableCoinOverview {
            total_marketcap:          stablecoins.iter().map(|coin| coin.total_supply as f64).sum(),
            number_of_unique_holder:  unique_holders.len() as f64,
            no_of_txn:                transfers.len() as f64,
            values_transferd:         transfers.iter().map(|t| t.amount).sum(),
            no_of_txn_last24h:        no_of_txn_last_24h as f64,
            values_transferd_last24h: values_transferd_last_24h,
        }
    }
}
