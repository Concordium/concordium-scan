use async_graphql::{Context, Object, SimpleObject};
use serde::Deserialize;
use std::collections::HashMap;
use std::fs::File;
use std::io::BufReader;

#[derive(Debug, Clone, Deserialize, SimpleObject)]
pub struct StableCoin {
    name: String,
    symbol: String,
    decimal: u8,
    contract_address: String,
    total_supply: i64,
    circulating_supply: i64,
    transfers: Option<Vec<Transfer>>, // Transfers sorted by date
    holding: Option<Vec<Holding>>,
}

#[derive(Debug, Clone, Deserialize, SimpleObject)]
pub struct Transfer {
    from: String,
    to: String,
    asset_name: String,
    date: String,
    amount: f64,
}

#[derive(Debug, Clone, Deserialize, SimpleObject)]
pub struct AssetInHold {
    asset_name: String,
    quantity: f64,
    percentage: f32,
}

#[derive(Debug, Clone, Deserialize, SimpleObject)]
pub struct Holding {
    address: String,
    holdings: Option<Vec<AssetInHold>>,
}

#[derive(Debug, Clone, SimpleObject)]
pub struct TransferSummary {
    date: String,
    total_amount: f64,
    transaction_count: usize,
}

impl StableCoin {
    pub fn top_holders(&self, top_holder: Option<usize>, min_quantity: Option<f64>) -> Option<Vec<Holding>> {
        let mut holders = self.holding.clone()?;
        
        if let Some(min) = min_quantity {
            holders.retain(|holding| {
                holding.holdings.as_ref().map_or(false, |h| {
                    h.iter().any(|asset| asset.quantity >= min)
                })
            });
        }
        
        holders.sort_by(|a, b| {
            let sum_a: f64 = a.holdings.as_ref().map_or(0.0, |h| h.iter().map(|x| x.quantity).sum());
            let sum_b: f64 = b.holdings.as_ref().map_or(0.0, |h| h.iter().map(|x| x.quantity).sum());
            sum_b.partial_cmp(&sum_a).unwrap()
        });
        
        let top_n = top_holder.unwrap_or(200);
        Some(holders.into_iter().take(top_n).collect())
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

    fn merge_transfers(mut stablecoins: Vec<StableCoin>, transfers: Vec<Transfer>, holdings: Vec<Holding>) -> Vec<StableCoin> {
        for coin in &mut stablecoins {
            coin.transfers = Some(
                transfers.iter()
                    .filter(|t| t.asset_name == coin.symbol)
                    .cloned()
                    .collect()
            );
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
                            address: holding.address.clone(),
                            holdings: Some(filtered_assets),
                        })
                    } else {
                        None
                    }
                })
                .collect();

            coin.holding = if relevant_holdings.is_empty() { None } else { Some(relevant_holdings) };
        }
        stablecoins
    }
}

#[Object]
impl QueryStableCoins {
    async fn stablecoin<'a>(&self, _ctx: &Context<'a>, symbol: String, top_holder: Option<usize>, min_quantity: Option<f64>) -> Option<StableCoin> {
        let mut stablecoins = Self::merge_transfers(Self::load_data(), Self::load_transfers(), Self::load_holdings());
        let coin = stablecoins.iter_mut().find(|coin| coin.symbol == symbol)?;
        
        coin.holding = coin.top_holders(top_holder, min_quantity);
        Some(coin.clone())
    }

    async fn stablecoins<'a>(&self, _ctx: &Context<'a>) -> Vec<StableCoin> {
        Self::merge_transfers(Self::load_data(), Self::load_transfers(), Self::load_holdings())
    }

    async fn stablecoins_by_supply<'a>(&self, _ctx: &Context<'a>, min_supply: i64) -> Vec<StableCoin> {
        Self::merge_transfers(Self::load_data(), Self::load_transfers(), Self::load_holdings())
            .into_iter()
            .filter(|coin| coin.total_supply >= min_supply)
            .collect()
    }
    
    async fn token_transfers<'a>(&self, _ctx: &Context<'a>, asset_name: String) -> Vec<Transfer> {
        Self::load_transfers()
            .into_iter()
            .filter(|transfer| transfer.asset_name == asset_name)
            .collect()
    }

    async fn daily_transfer_summary<'a>(&self, _ctx: &Context<'a>, asset_name: String) -> Vec<TransferSummary> {
        let transfers = Self::load_transfers();
        let mut summary: HashMap<String, (f64, usize)> = HashMap::new();
        
        for transfer in transfers.iter().filter(|t| t.asset_name == asset_name) {
            let entry = summary.entry(transfer.date.clone()).or_insert((0.0, 0));
            entry.0 += transfer.amount;
            entry.1 += 1;
        }
        
        let mut summary_vec: Vec<TransferSummary> = summary.into_iter()
            .map(|(date, (total_amount, transaction_count))| TransferSummary {
                date,
                total_amount,
                transaction_count,
            })
            .collect();
        
        summary_vec.sort_by(|a, b| a.date.cmp(&b.date));
        summary_vec
    }
}
