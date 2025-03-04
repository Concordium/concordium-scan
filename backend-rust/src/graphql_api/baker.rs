use super::{
    account::Account, get_config, get_pool, transaction::Transaction, ApiError, ApiResult,
    ConnectionQuery, OrderDir,
};
use crate::{
    address::AccountAddress,
    connection::DescendingI64,
    graphql_api::node_status::{NodeInfoReceiver, NodeStatus},
    scalar_types::{Amount, BakerId, DateTime, Decimal, MetadataUrl},
    transaction_event::{baker::BakerPoolOpenStatus, Event},
    transaction_reject::TransactionRejectReason,
    transaction_type::{
        AccountTransactionType, CredentialDeploymentTransactionType, DbTransactionType,
        UpdateTransactionType,
    },
};
use async_graphql::{connection, types, Context, Enum, InputObject, Object, SimpleObject, Union};
use bigdecimal::BigDecimal;
use concordium_rust_sdk::types::AmountFraction;
use futures::TryStreamExt;
use sqlx::PgPool;
use std::cmp::{max, min};

#[derive(Default)]
pub struct QueryBaker;

#[Object]
impl QueryBaker {
    async fn baker<'a>(&self, ctx: &Context<'a>, id: types::ID) -> ApiResult<Baker> {
        let id = IdBaker::try_from(id)?.baker_id.into();
        Baker::query_by_id(get_pool(ctx)?, id).await?.ok_or(ApiError::NotFound)
    }

    async fn baker_by_baker_id<'a>(
        &self,
        ctx: &Context<'a>,
        baker_id: BakerId,
    ) -> ApiResult<Baker> {
        Baker::query_by_id(get_pool(ctx)?, baker_id.into()).await?.ok_or(ApiError::NotFound)
    }

    #[allow(clippy::too_many_arguments)]
    async fn bakers(
        &self,
        ctx: &Context<'_>,
        #[graphql(default)] sort: BakerSort,
        filter: Option<BakerFilterInput>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, Baker>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query =
            ConnectionQuery::<i64>::new(first, after, last, before, config.baker_connection_limit)?;

        let sort_direction = OrderDir::from(sort);
        let order_field = BakerOrderField::from(sort);

        let open_status_filter = filter.and_then(|input| input.open_status_filter);
        let _include_removed_filter =
            filter.and_then(|input| input.include_removed).unwrap_or(false);

        let mut row_stream = sqlx::query_as!(
            Baker,
            r#"SELECT * FROM (
                SELECT
                    bakers.id AS id,
                    staked,
                    restake_earnings,
                    open_status as "open_status: _",
                    metadata_url,
                    transaction_commission,
                    baking_commission,
                    finalization_commission,
                    payday_transaction_commission as "payday_transaction_commission?",
                    payday_baking_commission as "payday_baking_commission?",
                    payday_finalization_commission as "payday_finalization_commission?",
                    payday_lottery_power as "payday_lottery_power?",
                    payday_ranking_by_lottery_powers as "payday_ranking_by_lottery_powers?",
                    (SELECT MAX(payday_ranking_by_lottery_powers) FROM bakers_payday_lottery_powers) as "payday_total_ranking_by_lottery_powers?",
                    pool_total_staked,
                    pool_delegator_count
                FROM bakers
                    LEFT JOIN bakers_payday_commission_rates
                        ON bakers_payday_commission_rates.id = bakers.id
                    LEFT JOIN bakers_payday_lottery_powers
                        ON bakers_payday_lottery_powers.id = bakers.id
                WHERE
                    (NOT $6 OR bakers.id            > $1 AND bakers.id            < $2) AND
                    (NOT $7 OR staked               > $1 AND staked               < $2) AND
                    (NOT $8 OR pool_total_staked    > $1 AND pool_total_staked    < $2) AND
                    (NOT $9 OR pool_delegator_count > $1 AND pool_delegator_count < $2) AND
                    -- filters
                    ($10::pool_open_status IS NULL OR open_status = $10::pool_open_status)
                ORDER BY
                    (CASE WHEN $6 AND     $3 THEN bakers.id            END) DESC,
                    (CASE WHEN $6 AND NOT $3 THEN bakers.id            END) ASC,
                    (CASE WHEN $7 AND     $3 THEN staked               END) DESC,
                    (CASE WHEN $7 AND NOT $3 THEN staked               END) ASC,
                    (CASE WHEN $8 AND     $3 THEN pool_total_staked    END) DESC,
                    (CASE WHEN $8 AND NOT $3 THEN pool_total_staked    END) ASC,
                    (CASE WHEN $9 AND     $3 THEN pool_delegator_count END) DESC,
                    (CASE WHEN $9 AND NOT $3 THEN pool_delegator_count END) ASC
                LIMIT $4
            ) ORDER BY
                (CASE WHEN $6 AND     $5 THEN id                   END) DESC,
                (CASE WHEN $6 AND NOT $5 THEN id                   END) ASC,
                (CASE WHEN $7 AND     $5 THEN staked               END) DESC,
                (CASE WHEN $7 AND NOT $5 THEN staked               END) ASC,
                (CASE WHEN $8 AND     $5 THEN pool_total_staked    END) DESC,
                (CASE WHEN $8 AND NOT $5 THEN pool_total_staked    END) ASC,
                (CASE WHEN $9 AND     $5 THEN pool_delegator_count END) DESC,
                (CASE WHEN $9 AND NOT $5 THEN pool_delegator_count END) ASC"#,
            query.from,                                                // $1
            query.to,                                                  // $2
            query.is_last != matches!(sort_direction, OrderDir::Desc), // $3
            query.limit,                                               // $4
            matches!(sort_direction, OrderDir::Desc),                  // $5
            matches!(order_field, BakerOrderField::BakerId),           // $6
            matches!(order_field, BakerOrderField::BakerStakedAmount), // $7
            matches!(order_field, BakerOrderField::TotalStakedAmount), // $8
            matches!(order_field, BakerOrderField::DelegatorCount),    // $9
            open_status_filter as Option<BakerPoolOpenStatus>          // $10
        )
        .fetch(pool);
        // TODO:
        // matches!(order_field, BakerOrderField::BakerApy30Days), // $10
        // matches!(order_field, BakerOrderField::DelegatorApy30Days), // $11
        // matches!(order_field, BakerOrderField::BlockCommissions), // $12

        let mut connection = connection::Connection::new(false, false);
        connection.edges.reserve_exact(query.limit.try_into()?);
        while let Some(row) = row_stream.try_next().await? {
            let cursor = row.sort_field(order_field).to_string();
            connection.edges.push(connection::Edge::new(cursor, row));
        }
        connection.has_previous_page = if let Some(first_item) = connection.edges.first() {
            let first_item_sort_value = first_item.node.sort_field(order_field);
            sqlx::query_scalar!(
                "SELECT true
                FROM bakers
                WHERE
                    (NOT $3 OR NOT $2 AND id                   < $1
                            OR     $2 AND id                   > $1) AND
                    (NOT $4 OR NOT $2 AND staked               < $1
                            OR     $2 AND staked               > $1) AND
                    (NOT $5 OR NOT $2 AND pool_total_staked    < $1
                            OR     $2 AND pool_total_staked    > $1) AND
                    (NOT $6 OR NOT $2 AND pool_delegator_count < $1
                            OR     $2 AND pool_delegator_count > $1) AND
                    -- filters
                    ($7::pool_open_status IS NULL OR open_status = $7::pool_open_status)
                LIMIT 1",
                first_item_sort_value,                                     // $1
                matches!(sort_direction, OrderDir::Desc),                  // $2
                matches!(order_field, BakerOrderField::BakerId),           // $3
                matches!(order_field, BakerOrderField::BakerStakedAmount), // $4
                matches!(order_field, BakerOrderField::TotalStakedAmount), // $5
                matches!(order_field, BakerOrderField::DelegatorCount),    // $6
                open_status_filter as Option<BakerPoolOpenStatus>          // $7
            )
            .fetch_optional(pool)
            .await?
            .flatten()
            .unwrap_or_default()
        } else {
            false
        };
        connection.has_next_page = if let Some(last_item) = connection.edges.last() {
            let last_item_sort_value = last_item.node.sort_field(order_field);
            sqlx::query_scalar!(
                "SELECT true
                FROM bakers
                WHERE
                    (NOT $3 OR NOT $2 AND id                   > $1
                            OR     $2 AND id                   < $1) AND
                    (NOT $4 OR NOT $2 AND staked               > $1
                            OR     $2 AND staked               < $1) AND
                    (NOT $5 OR NOT $2 AND pool_total_staked    > $1
                            OR     $2 AND pool_total_staked    < $1) AND
                    (NOT $6 OR NOT $2 AND pool_delegator_count > $1
                            OR     $2 AND pool_delegator_count < $1) AND
                    -- filters
                    ($7::pool_open_status IS NULL OR open_status = $7::pool_open_status)
                LIMIT 1",
                last_item_sort_value,                                      // $1
                matches!(sort_direction, OrderDir::Desc),                  // $2
                matches!(order_field, BakerOrderField::BakerId),           // $3
                matches!(order_field, BakerOrderField::BakerStakedAmount), // $4
                matches!(order_field, BakerOrderField::TotalStakedAmount), // $5
                matches!(order_field, BakerOrderField::DelegatorCount),    // $6
                open_status_filter as Option<BakerPoolOpenStatus>          // $7
            )
            .fetch_optional(pool)
            .await?
            .flatten()
            .unwrap_or_default()
        } else {
            false
        };

        Ok(connection)
    }
}

#[repr(transparent)]
struct IdBaker {
    baker_id: BakerId,
}
impl std::str::FromStr for IdBaker {
    type Err = ApiError;

    fn from_str(value: &str) -> Result<Self, Self::Err> {
        let baker_id = value.parse()?;
        Ok(IdBaker {
            baker_id,
        })
    }
}
impl TryFrom<types::ID> for IdBaker {
    type Error = ApiError;

    fn try_from(value: types::ID) -> Result<Self, Self::Error> { value.0.parse() }
}

pub struct Baker {
    id: BakerId,
    staked: i64,
    restake_earnings: bool,
    open_status: Option<BakerPoolOpenStatus>,
    metadata_url: Option<MetadataUrl>,
    transaction_commission: Option<i64>,
    baking_commission: Option<i64>,
    finalization_commission: Option<i64>,
    payday_transaction_commission: Option<i64>,
    payday_baking_commission: Option<i64>,
    payday_finalization_commission: Option<i64>,
    payday_lottery_power: Option<BigDecimal>,
    payday_ranking_by_lottery_powers: Option<i64>,
    payday_total_ranking_by_lottery_powers: Option<i64>,
    pool_total_staked: i64,
    pool_delegator_count: i64,
}
impl Baker {
    pub async fn query_by_id(pool: &PgPool, baker_id: i64) -> ApiResult<Option<Self>> {
        Ok(sqlx::query_as!(
            Baker,
            r#"
            SELECT
                bakers.id as id,
                staked,
                restake_earnings,
                open_status as "open_status: BakerPoolOpenStatus",
                metadata_url,
                transaction_commission,
                baking_commission,
                finalization_commission,
                payday_transaction_commission as "payday_transaction_commission?",
                payday_baking_commission as "payday_baking_commission?",
                payday_finalization_commission as "payday_finalization_commission?",
                payday_lottery_power as "payday_lottery_power?",
                payday_ranking_by_lottery_powers as "payday_ranking_by_lottery_powers?",
                (SELECT MAX(payday_ranking_by_lottery_powers) FROM bakers_payday_lottery_powers) as "payday_total_ranking_by_lottery_powers?",
                pool_total_staked,
                pool_delegator_count
            FROM bakers
                LEFT JOIN bakers_payday_commission_rates ON bakers_payday_commission_rates.id = bakers.id
                LEFT JOIN bakers_payday_lottery_powers ON bakers_payday_lottery_powers.id = bakers.id
            WHERE bakers.id = $1;
            "#,
            baker_id
        )
        .fetch_optional(pool)
        .await?)
    }

    fn sort_field(&self, order_field: BakerOrderField) -> i64 {
        match order_field {
            BakerOrderField::BakerId => self.id.into(),
            BakerOrderField::BakerStakedAmount => self.staked,
            BakerOrderField::TotalStakedAmount => self.pool_total_staked,
            BakerOrderField::DelegatorCount => self.pool_delegator_count,
            BakerOrderField::BakerApy30Days => todo!(),
            BakerOrderField::DelegatorApy30days => todo!(),
            BakerOrderField::BlockCommissions => todo!(),
        }
    }
}
#[Object]
impl Baker {
    async fn id(&self) -> types::ID { types::ID::from(self.id.to_string()) }

    async fn baker_id(&self) -> BakerId { self.id }

    async fn state<'a>(&'a self, ctx: &Context<'a>) -> ApiResult<BakerState<'a>> {
        let pool = get_pool(ctx)?;

        let transaction_commission = self
            .transaction_commission
            .map(u32::try_from)
            .transpose()?
            .map(|c| AmountFraction::new_unchecked(c).into());
        let baking_commission = self
            .baking_commission
            .map(u32::try_from)
            .transpose()?
            .map(|c| AmountFraction::new_unchecked(c).into());
        let finalization_commission = self
            .finalization_commission
            .map(u32::try_from)
            .transpose()?
            .map(|c| AmountFraction::new_unchecked(c).into());

        // `payday_transaction_commission`, `payday_baking_commission` and
        // `payday_finalization_commission` are either set or not set
        // for a given baker. Hence we only check if `payday_transaction_commission` is
        // set.
        let payday_commission_rates = if self.payday_transaction_commission.is_some() {
            let payday_transaction_commission = self
                .payday_transaction_commission
                .map(u32::try_from)
                .transpose()?
                .map(|c| AmountFraction::new_unchecked(c).into());
            let payday_baking_commission = self
                .payday_baking_commission
                .map(u32::try_from)
                .transpose()?
                .map(|c| AmountFraction::new_unchecked(c).into());
            let payday_finalization_commission = self
                .payday_finalization_commission
                .map(u32::try_from)
                .transpose()?
                .map(|c| AmountFraction::new_unchecked(c).into());
            Some(CommissionRates {
                transaction_commission:  payday_transaction_commission,
                baking_commission:       payday_baking_commission,
                finalization_commission: payday_finalization_commission,
            })
        } else {
            None
        };

        let total_stake: i64 =
            sqlx::query_scalar!("SELECT total_staked FROM blocks ORDER BY height DESC LIMIT 1")
                .fetch_one(pool)
                .await?;

        let delegated_stake_of_pool = self.pool_total_staked - self.staked;

        // Division by 0 is not possible because `pool_total_staked` is always a
        // positive number.
        let total_stake_percentage = (rust_decimal::Decimal::from(self.pool_total_staked)
            * rust_decimal::Decimal::from(100))
        .checked_div(rust_decimal::Decimal::from(total_stake))
        .ok_or_else(|| ApiError::InternalError("Division by zero".to_string()))?
        .into();

        // The code is re-implemented from the node code so that the node and the
        // CCDScan report the same values:
        // https://github.com/Concordium/concordium-node/blob/3cb759e4607f20a9df94ace017f0e3c775d4cdb3/concordium-consensus/src/Concordium/Kontrol/Bakers.hs#L53

        // The delegated capital cap in the node (called delegated stake cap in CCDScan)
        // is defined to be the minimum of the capital bound cap and the
        // leverage bound cap:

        // leverage bound cap for pool p: Lₚ = λ * Cₚ - Cₚ = (λ - 1) * Cₚ
        // capital bound cap for pool p: Bₚ = floor( (κ * (T - Dₚ) - Cₚ) / (1 - K) )

        // Where
        // κ is the capital bound
        // λ is the leverage bound
        // T is the total staked capital on the whole chain (including passive
        // delegation)
        // Dₚ is the delegated capital of pool p
        // Cₚ is the equity capital (staked by the pool owner excluding delegated stake
        // to the pool) of pool p

        // The `leverage bound cap` ensures that each baker has skin
        // in the game with respect to its delegators by providing some of the CCD
        // staked from its own funds. The `capital bound cap` helps maintain
        // network decentralization by preventing a single baker from gaining
        // excessive power in the consensus protocol.

        let current_chain_parameters = sqlx::query_as!(
            DelegatedStakeBounds,
            "
            SELECT 
                leverage_bound_numerator,
                leverage_bound_denominator,
                capital_bound 
            FROM current_chain_parameters 
            WHERE id = true
            "
        )
        .fetch_one(pool)
        .await?;

        // The `leverage_bound` and `capital_bound` are Concordium chain parameters that
        // were set adhering to the below constraints to ensure the consensus
        // algorithm works. We check these constraints here to ensure the values
        // have been saved in this format to the database.
        if current_chain_parameters.leverage_bound_numerator < 0 {
            return Err(ApiError::InternalError(
                "`leverage_bound_numerator` is negative in the database".to_string(),
            ));
        }
        if current_chain_parameters.leverage_bound_denominator <= 0 {
            return Err(ApiError::InternalError(
                "`leverage_bound_denominator` is not greater than 0 in the database".to_string(),
            ));
        }

        if current_chain_parameters.leverage_bound_numerator
            < current_chain_parameters.leverage_bound_denominator
        {
            return Err(ApiError::InternalError(
                "`leverage_bound` is smaller than 1 in the database".to_string(),
            ));
        }
        if current_chain_parameters.capital_bound <= 0 {
            return Err(ApiError::InternalError(
                "`capital_bound` is not greater than 0 in the database".to_string(),
            ));
        }

        // Calculating the `leverage_bound_cap`

        #[rustfmt::skip]
        // Transformation applied to the `leverage_bound_cap_for_pool` formula:
        //
        // `leverage_bound_cap_for_pool`
        // = (λ – 1) * Cₚ
        // = (leverage_bound_numerator / leverage_bound_denominator – 1) * Cₚ
        // = (leverage_bound_numerator / leverage_bound_denominator – (leverage_bound_denominator / leverage_bound_denominator)) * Cₚ
        // = (leverage_bound_numerator – leverage_bound_denominator) / leverage_bound_denominator) * Cₚ
        // = (leverage_bound_numerator – leverage_bound_denominator) * Cₚ / leverage_bound_denominator
        //
        // WHERE
        // λ is the leverage bound
        // Cₚ is the equity capital (staked by the pool owner excluding delegated stake
        // to the pool) of pool p
        // `leverage_bound_numerator` is the value as stored in the database
        // `leverage_bound_denominator` is the value as stored in the database

        // To reduce loss of precision, the value is computed in u128.
        let leverage_bound_cap_for_pool_numerator: u128 =
            (current_chain_parameters.leverage_bound_numerator
                - current_chain_parameters.leverage_bound_denominator) as u128
                * self.staked as u128;
        // Denominator is not zero since we checked that before.
        let leverage_bound_cap_for_pool_denominator: u128 =
            current_chain_parameters.leverage_bound_denominator as u128;

        let leverage_bound_cap_for_pool: u64 = (leverage_bound_cap_for_pool_numerator
            / leverage_bound_cap_for_pool_denominator)
            .try_into()
            .unwrap_or(u64::MAX);
        let leverage_bound_cap_for_pool: Amount = leverage_bound_cap_for_pool.into();

        // Calculating the `capital_bound_cap`

        #[rustfmt::skip]
        // Transformation applied to the `capital_bound_cap_for_pool` formula:
        //
        // `capital_bound_cap_for_pool`
        // = floor( (κ * (T - Dₚ) - Cₚ) / (1 - K) )
        // = floor( (capital_bound/100_000 * (T - Dₚ) - Cₚ) / (1 - capital_bound/100_000) )
        // (Explanation: Since the `capital_bound (from the database)` is stored as a fraction with
        // precision of `1/100_000` in the database)
        //
        // = floor( ((capital_bound / 100_000 * (T - Dₚ) - Cₚ)  / (1 - capital_bound / 100_000)) * 1 )
        // = floor( ((capital_bound / 100_000 * (T - Dₚ) - Cₚ)  / (1 - capital_bound / 100_000)) * (100_000 / 100_000) )
        // = floor( (capital_bound / 100_000 * (T - Dₚ) - Cₚ) * 100_000 / (1 - capital_bound / 100_000) * 100_000) )
        // = floor( (capital_bound * (T - Dₚ) - 100_000 * Cₚ) / (100_000 - capital_bound) )

        // WHERE
        // κ is the capital bound
        // T is the total staked capital on the whole chain (including passive
        // delegation)
        // Dₚ is the delegated capital of pool p
        // Cₚ is the equity capital (staked by the pool owner excluding delegated stake
        // to the pool) of pool p
        // `capital_bound` is the value as stored in the database

        let capital_bound: u128 = current_chain_parameters.capital_bound as u128;

        let capital_bound_cap_for_pool: Amount = if capital_bound == 100_000u128 {
            // To avoid dividing by 0 in the `capital bound cap` formula,
            // we only apply the `leverage_bound_cap_for_pool` in that case.
            u64::MAX.into()
        } else {
            // Since the `capital_bound` is stored as a fraction with precision of
            // `1/100_000` in the database, we multiply the numerator and
            // denominator by 100_000. To reduce loss of precision, the value is computed in
            // u128.
            let capital_bound_cap_for_pool_numerator = capital_bound
                * ((total_stake - delegated_stake_of_pool) as u128)
                    .saturating_sub(100_000 * (self.staked as u128));

            // Denominator is not zero since we checked that `capital_bound != 100_000`.
            let capital_bound_cap_for_pool_denominator: u128 = 100_000u128 - capital_bound;

            let capital_bound_cap_for_pool: u64 = (capital_bound_cap_for_pool_numerator
                / capital_bound_cap_for_pool_denominator)
                .try_into()
                .unwrap_or(u64::MAX);

            capital_bound_cap_for_pool.into()
        };

        let delegated_stake_cap = min(leverage_bound_cap_for_pool, capital_bound_cap_for_pool);

        // Get the rank of the baker based on its lottery power.
        // An account id that is not a baker has no `payday_ranking_by_lottery_powers`
        // in general. A new baker has as well no
        // `payday_ranking_by_lottery_powers` until the next payday. We return
        // `None` as ranking in both cases. A baker that got just removed has a
        // `payday_ranking_by_lottery_powers` until the next payday. We return a
        // ranking in that case.
        let rank = match (
            self.payday_ranking_by_lottery_powers,
            self.payday_total_ranking_by_lottery_powers,
        ) {
            (Some(rank), Some(total)) => Some(Ranking {
                rank,
                total,
            }),
            (None, _) => None,
            (Some(_), None) => {
                return Err(ApiError::InternalError(
                    "Invalid ranking state in database".to_string(),
                ))
            }
        };

        let baker_id: u64 = self.id.0.try_into().map_err(|_| {
            ApiError::InternalError(format!("A baker has a negative id: {}", self.id.0))
        })?;

        let handler = ctx.data::<NodeInfoReceiver>().map_err(ApiError::NoReceiver)?;
        let statuses_ref = handler.borrow();
        let statuses = statuses_ref.as_ref().ok_or(ApiError::InternalError(
            "Node collector backend has not responded".to_string(),
        ))?;

        let node = statuses.iter().find(|x| x.consensus_baker_id == Some(baker_id)).cloned();
        let out = BakerState::ActiveBakerState(Box::new(ActiveBakerState {
            node_status:      node,
            staked_amount:    Amount::try_from(self.staked)?,
            restake_earnings: self.restake_earnings,
            pool:             BakerPool {
                id: self.id.0,
                open_status: self.open_status,
                commission_rates: CommissionRates {
                    transaction_commission,
                    baking_commission,
                    finalization_commission,
                },
                rank,
                payday_commission_rates,
                payday_lottery_power: self
                    .payday_lottery_power
                    .as_ref()
                    .unwrap_or(&BigDecimal::default())
                    .try_into()
                    .map_err(|e: anyhow::Error| ApiError::InternalError(e.to_string()))?,
                metadata_url: self.metadata_url.as_deref(),
                total_stake_percentage,
                total_stake: Amount::try_from(self.pool_total_staked)?,
                delegated_stake: Amount::try_from(delegated_stake_of_pool)?,
                delegated_stake_cap,
                delegator_count: self.pool_delegator_count,
            },
            pending_change:   None, // This is not used starting from P7.
        }));
        Ok(out)
    }

    async fn account<'a>(&self, ctx: &Context<'a>) -> ApiResult<Account> {
        Account::query_by_index(get_pool(ctx)?, i64::from(self.id)).await?.ok_or(ApiError::NotFound)
    }

    async fn transactions(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, InterimTransaction>> {
        let config = get_config(ctx)?;
        let pool = get_pool(ctx)?;
        let query = ConnectionQuery::<i64>::new(
            first,
            after,
            last,
            before,
            config.transactions_per_block_connection_limit,
        )?;

        let account_transaction_type_filter = &[
            AccountTransactionType::AddBaker,
            AccountTransactionType::RemoveBaker,
            AccountTransactionType::UpdateBakerStake,
            AccountTransactionType::UpdateBakerRestakeEarnings,
            AccountTransactionType::UpdateBakerKeys,
            AccountTransactionType::ConfigureBaker,
        ];

        // Retrieves the transactions related to a baker account ('AddBaker',
        // 'RemoveBaker', 'UpdateBakerStake', 'UpdateBakerRestakeEarnings',
        // 'UpdateBakerKeys', 'ConfigureBaker'). The transactions are ordered in
        // descending order (outer `ORDER BY`). If the `last` input parameter is
        // set, the inner `ORDER BY` reverses the transaction order to allow the
        // range be applied starting from the last element.
        let mut row_stream = sqlx::query_as!(
            Transaction,
            r#"
            SELECT * FROM (
                SELECT
                    index,
                    block_height,
                    hash,
                    ccd_cost,
                    energy_cost,
                    sender_index,
                    type as "tx_type: DbTransactionType",
                    type_account as "type_account: AccountTransactionType",
                    type_credential_deployment as "type_credential_deployment: CredentialDeploymentTransactionType",
                    type_update as "type_update: UpdateTransactionType",
                    success,
                    events as "events: sqlx::types::Json<Vec<Event>>",
                    reject as "reject: sqlx::types::Json<TransactionRejectReason>"
                FROM transactions
                WHERE transactions.sender_index = $5
                AND type_account = ANY($6)
                AND index > $1 AND index < $2
                ORDER BY
                    CASE WHEN NOT $3 THEN index END DESC,
                    CASE WHEN $3 THEN index END ASC
                LIMIT $4
            ) ORDER BY index DESC"#,
            query.from,
            query.to,
            query.is_last,
            query.limit,
            self.id.0,
            account_transaction_type_filter as &[AccountTransactionType]
        )
        .fetch(pool);

        let mut connection = connection::Connection::new(false, false);

        let mut page_max_index = None;
        let mut page_min_index = None;
        while let Some(tx) = row_stream.try_next().await? {
            page_max_index = Some(match page_max_index {
                None => tx.index,
                Some(current_max) => max(current_max, tx.index),
            });

            page_min_index = Some(match page_min_index {
                None => tx.index,
                Some(current_min) => min(current_min, tx.index),
            });

            connection.edges.push(connection::Edge::new(
                tx.index.to_string(),
                InterimTransaction {
                    transaction: tx,
                },
            ));
        }

        if let (Some(page_min_id), Some(page_max_id)) = (page_min_index, page_max_index) {
            let result = sqlx::query!(
                "
                    SELECT MAX(index) as max_id, MIN(index) as min_id
                    FROM transactions
                    WHERE transactions.sender_index = $1
                    AND type_account = ANY($2)
                ",
                &self.id.0,
                account_transaction_type_filter as &[AccountTransactionType]
            )
            .fetch_one(pool)
            .await?;

            connection.has_previous_page =
                result.min_id.map_or(false, |db_min| db_min < page_min_id);
            connection.has_next_page = result.max_id.map_or(false, |db_max| db_max > page_max_id);
        }

        Ok(connection)
    }
}

// Future improvement (API breaking changes): The function `Baker::transactions`
// can directly return a `Transaction` instead of the `IterimTransaction` type
// here.
#[derive(SimpleObject)]
struct InterimTransaction {
    transaction: Transaction,
}

#[derive(Union)]
enum BakerState<'a> {
    ActiveBakerState(Box<ActiveBakerState<'a>>),
    RemovedBakerState(RemovedBakerState),
}

#[derive(SimpleObject)]
struct ActiveBakerState<'a> {
    /// The status of the baker's node. Will be null if no status for the node
    /// exists.
    node_status:      Option<NodeStatus>,
    staked_amount:    Amount,
    restake_earnings: bool,
    pool:             BakerPool<'a>,
    // This will not be used starting from P7
    pending_change:   Option<PendingBakerChange>,
}

#[derive(Union)]
enum PendingBakerChange {
    PendingBakerRemoval(PendingBakerRemoval),
    PendingBakerReduceStake(PendingBakerReduceStake),
}

#[derive(SimpleObject)]
struct PendingBakerRemoval {
    effective_time: DateTime,
}

#[derive(SimpleObject)]
struct PendingBakerReduceStake {
    new_staked_amount: Amount,
    effective_time:    DateTime,
}

#[derive(SimpleObject)]
struct RemovedBakerState {
    removed_at: DateTime,
}

#[derive(InputObject, Clone, Copy)]
struct BakerFilterInput {
    open_status_filter: Option<BakerPoolOpenStatus>,
    include_removed:    Option<bool>,
}

#[derive(Enum, Clone, Copy, PartialEq, Eq, Default)]
enum BakerSort {
    #[default]
    BakerIdAsc,
    BakerIdDesc,
    BakerStakedAmountAsc,
    BakerStakedAmountDesc,
    TotalStakedAmountAsc,
    TotalStakedAmountDesc,
    DelegatorCountAsc,
    DelegatorCountDesc,
    BakerApy30DaysDesc,
    DelegatorApy30DaysDesc,
    BlockCommissionsAsc,
    BlockCommissionsDesc,
}

impl From<BakerSort> for OrderDir {
    fn from(value: BakerSort) -> Self {
        match value {
            BakerSort::BakerIdAsc => OrderDir::Asc,
            BakerSort::BakerIdDesc => OrderDir::Desc,
            BakerSort::BakerStakedAmountAsc => OrderDir::Asc,
            BakerSort::BakerStakedAmountDesc => OrderDir::Desc,
            BakerSort::TotalStakedAmountAsc => OrderDir::Asc,
            BakerSort::TotalStakedAmountDesc => OrderDir::Desc,
            BakerSort::DelegatorCountAsc => OrderDir::Asc,
            BakerSort::DelegatorCountDesc => OrderDir::Desc,
            BakerSort::BakerApy30DaysDesc => OrderDir::Desc,
            BakerSort::DelegatorApy30DaysDesc => OrderDir::Desc,
            BakerSort::BlockCommissionsAsc => OrderDir::Asc,
            BakerSort::BlockCommissionsDesc => OrderDir::Desc,
        }
    }
}

#[derive(Debug, Clone, Copy)]
enum BakerOrderField {
    BakerId,
    BakerStakedAmount,
    TotalStakedAmount,
    DelegatorCount,
    BakerApy30Days,
    DelegatorApy30days,
    BlockCommissions,
}

impl From<BakerSort> for BakerOrderField {
    fn from(value: BakerSort) -> Self {
        match value {
            BakerSort::BakerIdAsc => Self::BakerId,
            BakerSort::BakerIdDesc => Self::BakerId,
            BakerSort::BakerStakedAmountAsc => Self::BakerStakedAmount,
            BakerSort::BakerStakedAmountDesc => Self::BakerStakedAmount,
            BakerSort::TotalStakedAmountAsc => Self::TotalStakedAmount,
            BakerSort::TotalStakedAmountDesc => Self::TotalStakedAmount,
            BakerSort::DelegatorCountAsc => Self::DelegatorCount,
            BakerSort::DelegatorCountDesc => Self::DelegatorCount,
            BakerSort::BakerApy30DaysDesc => Self::BakerApy30Days,
            BakerSort::DelegatorApy30DaysDesc => Self::DelegatorApy30days,
            BakerSort::BlockCommissionsAsc => Self::BlockCommissions,
            BakerSort::BlockCommissionsDesc => Self::BlockCommissions,
        }
    }
}

struct BakerPool<'a> {
    id: i64,
    /// Total stake of the baker pool as a percentage of all CCDs in existence.
    /// Includes both baker stake and delegated stake.
    total_stake_percentage: Decimal,
    /// The total amount staked in this baker pool. Includes both baker stake
    /// and delegated stake.
    total_stake: Amount,
    /// The total amount staked by delegators to this baker pool.
    delegated_stake: Amount,
    /// The number of delegators that delegate to this baker pool.
    delegator_count: i64,
    /// The `commission_rates` represent the current commission settings of a
    /// baker pool, as configured by the baker account through
    /// `bakerConfiguration` transactions. These values are updated
    /// immediatly by observing the following `BakerEvent`s in the indexer:
    /// - `BakerSetBakingRewardCommission`
    /// - `BakerSetTransactionFeeCommission`
    /// - `BakerSetFinalizationRewardCommission`
    ///
    /// Both `commission_rates` and `payday_commission_rates` are optional and
    /// usually return the same value. But at the following edge cases, they
    /// return different values:
    /// - When a validator is initially added (observed by the event
    ///   `BakerEvent::Added`), only `commission_rates` are available until the
    ///   next payday. The `payday_commission_rates` will be set at the next
    ///   payday.
    /// - When a validator is removed (observed by the event
    ///   `BakerEvent::Removed`), `commission_rates` are immediately cleared
    ///   from the bakers table upon detecting the `BakerEvent::Removed`,
    ///   whereas `payday_commission_rates` persist until the next payday.
    commission_rates: CommissionRates,
    /// The `payday_commission_rates` represent the commission settings at the
    /// last payday block. These values are retrieved from the
    /// `get_bakers_reward_period(BlockIdentifier::AbsoluteHeight(payday_block_height))`
    /// endpoint at each payday.
    ///
    /// Both `commission_rates` and `payday_commission_rates` are optional and
    /// usually return the same value. But at the following edge cases, they
    /// return different values:
    /// - When a validator is initially added (observed by the event
    ///   `BakerEvent::Added`), only `commission_rates` are available until the
    ///   next payday. The `payday_commission_rates` will be set at the next
    ///   payday.
    /// - When a validator is removed (observed by the event
    ///   `BakerEvent::Removed`), `commission_rates` are immediately cleared
    ///   from the bakers table upon detecting the `BakerEvent::Removed`,
    ///   whereas `payday_commission_rates` persist until the next payday.
    payday_commission_rates: Option<CommissionRates>,
    /// The lottery power of the baker pool during the last payday period
    /// captured from the `get_election_info` node endpoint.`
    payday_lottery_power: Decimal,
    /// Ranking of the bakers by lottery powers staring with rank 1 for the
    /// baker with the highest lottery power and ending with the rank
    /// `total` for the baker with the lowest lottery power.
    /// An account id that is not a baker has no
    /// `payday_ranking_by_lottery_powers` in general. A new baker has as
    /// well no `payday_ranking_by_lottery_powers` until the next payday.
    /// We return `None` as ranking in both cases.
    /// A baker that got just removed has a `payday_ranking_by_lottery_powers`
    /// until the next payday. We return a ranking in that case.
    rank: Option<Ranking>,
    /// The maximum amount that may be delegated to the pool, accounting for
    /// leverage and capital bounds.
    delegated_stake_cap: Amount,
    open_status: Option<BakerPoolOpenStatus>,
    metadata_url: Option<&'a str>,
    // TODO: apy(period: ApyPeriod!): PoolApy!
    // TODO: poolRewards("Returns the first _n_ elements from the list." first: Int "Returns the
    // elements in the list that come after the specified cursor." after: String "Returns the last
    // _n_ elements from the list." last: Int "Returns the elements in the list that come before
    // the specified cursor." before: String): PaydayPoolRewardConnection
}

#[Object]
impl<'a> BakerPool<'a> {
    async fn total_stake_percentage(&self) -> &Decimal { &self.total_stake_percentage }

    async fn total_stake(&self) -> Amount { self.total_stake }

    // Future improvement (API breaking changes): The front end queries
    // `ranking_by_total_stake` which was renamed and updated to
    // `ranking_by_lottery_power` in the backend. Migrate the name change to the
    // front-end and make this field not optional anymore.
    async fn ranking_by_total_stake(&self) -> Option<Ranking> { self.rank }

    async fn delegated_stake(&self) -> Amount { self.delegated_stake }

    async fn delegated_stake_cap(&self) -> Amount { self.delegated_stake_cap }

    async fn delegator_count(&self) -> i64 { self.delegator_count }

    async fn commission_rates(&self) -> &CommissionRates { &self.commission_rates }

    async fn payday_commission_rates(&self) -> &Option<CommissionRates> {
        &self.payday_commission_rates
    }

    async fn lottery_power(&self) -> &Decimal { &self.payday_lottery_power }

    async fn open_status(&self) -> Option<BakerPoolOpenStatus> { self.open_status }

    async fn metadata_url(&self) -> Option<&'a str> { self.metadata_url }

    async fn delegators(
        &self,
        ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] last: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come before the specified cursor.")]
        before: Option<String>,
    ) -> ApiResult<connection::Connection<String, DelegationSummary>> {
        let pool = get_pool(ctx)?;
        let config = get_config(ctx)?;
        let query = ConnectionQuery::<DescendingI64>::new(
            first,
            after,
            last,
            before,
            config.delegators_connection_limit,
        )?;
        let mut row_stream = sqlx::query_as!(
            DelegationSummary,
            "SELECT * FROM (
                SELECT
                    index,
                    address as account_address,
                    delegated_restake_earnings as restake_earnings,
                    delegated_stake as staked_amount
                FROM accounts
                WHERE delegated_target_baker_id = $5 AND
                    accounts.index > $2 AND accounts.index < $1
                ORDER BY
                    (CASE WHEN $4 THEN accounts.index END) ASC,
                    (CASE WHEN NOT $4 THEN accounts.index END) DESC
                LIMIT $3
            ) AS delegators
            ORDER BY delegators.index DESC",
            i64::from(query.from),
            i64::from(query.to),
            query.limit,
            query.is_last,
            self.id
        )
        .fetch(pool);
        let mut connection = connection::Connection::new(false, false);
        while let Some(delegator) = row_stream.try_next().await? {
            connection.edges.push(connection::Edge::new(delegator.index.to_string(), delegator));
        }
        if let Some(page_max_index) = connection.edges.first() {
            if let Some(max_index) = sqlx::query_scalar!(
                "SELECT MAX(index) FROM accounts WHERE delegated_target_baker_id = $1",
                self.id
            )
            .fetch_one(pool)
            .await?
            {
                connection.has_previous_page = max_index > page_max_index.node.index;
            }
        }
        if let Some(edge) = connection.edges.last() {
            connection.has_next_page = edge.node.index != 0;
        }
        Ok(connection)
    }
}

struct DelegationSummary {
    index:            i64,
    account_address:  AccountAddress,
    staked_amount:    i64,
    restake_earnings: Option<bool>,
}

#[Object]
impl DelegationSummary {
    async fn account_address(&self) -> &AccountAddress { &self.account_address }

    async fn staked_amount(&self) -> ApiResult<Amount> {
        self.staked_amount.try_into().map_err(|_| {
            ApiError::InternalError(
                "Staked amount in database should be a valid UnsignedLong".to_string(),
            )
        })
    }

    async fn restake_earnings(&self) -> ApiResult<bool> {
        self.restake_earnings.ok_or(ApiError::InternalError(
            "Delegator should have a boolean in the `restake_earnings` variable.".to_string(),
        ))
    }
}
#[derive(SimpleObject)]
struct CommissionRates {
    transaction_commission:  Option<Decimal>,
    finalization_commission: Option<Decimal>,
    baking_commission:       Option<Decimal>,
}

struct DelegatedStakeBounds {
    /// The leverage bound (also called leverage factor in the node API) is the
    /// maximum proportion of total stake of a baker (including the baker's
    /// own stake and the delegated stake to the baker) to the baker's own stake
    /// (excluding delegated stake to the baker) that a baker can achieve where
    /// the total stake of the baker is considered for calculating the
    /// lottery power or finalizer weight in the consensus (effective
    /// stake). Once this bound is passed, some of the baker's total stake
    /// no longer contribute to lottery power or finalizer weight in the
    /// consensus algorithm, meaning that part of the baker's total stake
    /// will no longer be considered as effective stake.
    ///
    /// The value is 1 or greater (1 <= leverage_bound).
    /// The value's numerator and denominator is stored here.
    ///
    /// The `leverage bound` ensures that each baker has skin in the game with
    /// respect to its delegators by providing some of the CCD staked from
    /// its own funds.
    leverage_bound_numerator:   i64,
    leverage_bound_denominator: i64,
    /// The capital bound is the maximum proportion of the total stake in the
    /// protocol (from all bakers including passive delegation) to the total
    /// stake of a baker (including the baker's own stake and the delegated
    /// stake to the baker) that a baker can achieve where the total
    /// stake of the baker is considered for calculating the lottery power or
    /// finalizer weight in the consensus (effective stake).
    /// Once this bound is passed, some of the baker's total stake no longer
    /// contribute to lottery power or finalizer weight in the consensus
    /// algorithm, meaning that part of the baker's total stake will no longer
    /// be considered as effective stake.
    ///
    /// The capital bound is always greater than 0 (capital_bound > 0). The
    /// value is stored as a fraction with precision of `1/100_000`. For
    /// example, a capital bound of 0.05 is stored as 5000.
    ///
    /// The `capital_bound` helps maintain network
    /// decentralization by preventing a single baker from gaining excessive
    /// power in the consensus protocol.
    capital_bound:              i64,
}

/// Ranking of the bakers by lottery powers from the last payday block staring
/// with rank 1 for the baker with the highest lottery power and ending with the
/// rank `total` for the baker with the lowest lottery power.
#[derive(SimpleObject, Clone, Copy)]
struct Ranking {
    rank:  i64,
    total: i64,
}
