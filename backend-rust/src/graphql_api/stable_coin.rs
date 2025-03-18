use async_graphql::{Context, Object, SimpleObject};
use serde::Deserialize;
use std::fs::File;
use std::io::BufReader;

#[derive(Debug, Clone, Deserialize, SimpleObject)]
pub struct StableCoin {
    name: String,
    symbol: String,
    decimal: u8,
    contract_address: String,
    total_supply: String,
    circulating_supply: String,
    transfers: Option<Vec<Transfer>>,
}

#[derive(Debug, Clone, Deserialize, SimpleObject)]
pub struct Transfer {
    from: String,
    to: String,
    asset_name: String,
    date: String,
    amount: String,
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
        serde_json::from_reader(reader).unwrap()
    }

    fn merge_transfers(
        mut stablecoins: Vec<StableCoin>,
        transfers: Vec<Transfer>,
    ) -> Vec<StableCoin> {
        for coin in &mut stablecoins {
            coin.transfers =
                Some(transfers.iter().filter(|t| t.asset_name == coin.symbol).cloned().collect());
        }
        stablecoins
    }
}

#[Object]
impl QueryStableCoins {
    async fn stablecoin<'a>(&self, _ctx: &Context<'a>, symbol: String) -> Option<StableCoin> {
        let stablecoins = Self::merge_transfers(Self::load_data(), Self::load_transfers());
        stablecoins.into_iter().find(|coin| coin.symbol == symbol)
    }

    async fn stablecoins<'a>(&self, _ctx: &Context<'a>) -> Vec<StableCoin> {
        Self::merge_transfers(Self::load_data(), Self::load_transfers())
    }

    async fn stablecoins_by_supply<'a>(
        &self,
        _ctx: &Context<'a>,
        min_supply: String,
    ) -> Vec<StableCoin> {
        Self::merge_transfers(Self::load_data(), Self::load_transfers())
            .into_iter()
            .filter(|coin| {
                coin.total_supply.parse::<f64>().unwrap_or(0.0)
                    >= min_supply.parse::<f64>().unwrap_or(0.0)
            })
            .collect()
    }
}
