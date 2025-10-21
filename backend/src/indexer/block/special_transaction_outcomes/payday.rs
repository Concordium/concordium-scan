//! This module contains information computed for a single block with payday
//! during the concurrent preprocessing and the logic for how to do the
//! sequential processing into the database.
//!
//! The concept of a payday where first introduced in Concordium Protocol
//! Version 4, and is the event of rewards being paid out to validators and
//! finalizers by the end of a reward period.

use crate::indexer::ensure_affected_rows::EnsureAffectedRows;
use anyhow::Context;
use bigdecimal::BigDecimal;
use concordium_rust_sdk::{
    types::{
        queries::{BlockInfo, ProtocolVersionInt},
        AbsoluteBlockHeight, BakerRewardPeriodInfo, BirkBaker, DelegatorRewardPeriodInfo,
        PartsPerHundredThousands, PassiveDelegationStatus, ProtocolVersion,
    },
    v2,
};
use futures::TryStreamExt;
use num_traits::FromPrimitive;

/// Represents a payday block, its payday commission
/// rates, and the associated block height.
pub struct PreparedPayDayBlock {
    block_height: i64,
    /// Represents the payday baker pool commission rates captured from
    /// the `get_bakers_reward_period` node endpoint.
    baker_payday_commission_rates: PreparedBakerPaydayCommissionRates,
    /// Represents the payday pool commission rates to passive delegators
    /// captured from the `get_passive_delegation_info` node endpoint.
    passive_delegation_payday_commission_rates: PreparedPassiveDelegationPaydayCommissionRates,
    /// Represents the payday lottery power updates for bakers captured from
    /// the `get_election_info` node endpoint.
    payday_bakers_lottery_powers: PreparedPaydayLotteryPowers,
    /// Represents the baker pool stakes locked for reward period after this
    /// payday.
    baker_pool_stakes: PreparedPaydayBakerPoolStakes,
    /// Represents the passive pool stake locked for reward period after this
    /// payday.
    passive_pool_stake: PreparedPaydayPassivePoolStake,
    /// Recompute the latest baker APYs.
    refresh_latest_baker_apy_view: RefreshLatestBakerApy,
}

impl PreparedPayDayBlock {
    pub async fn prepare(
        node_client: &mut v2::Client,
        block_info: &BlockInfo,
    ) -> anyhow::Result<Self> {
        let block_height = block_info.block_height;

        // Fetching the `get_bakers_reward_period` endpoint prior to P4 results in a
        // InvalidArgument gRPC error, so we produce the empty vector of
        // `payday_pool_rewards` instead. The information of the last payday commission
        // rate of baker pools is expected to be used when the indexer has fully
        // caught up to the top of the chain.
        let (baker_reward_period_infos, passive_reward_period_info) =
            if block_info.protocol_version >= ProtocolVersionInt::from(ProtocolVersion::P4) {
                let baker_info = node_client
                    .get_bakers_reward_period(v2::BlockIdentifier::AbsoluteHeight(block_height))
                    .await?
                    .response
                    .try_collect()
                    .await?;
                let passive_info = node_client
                    .get_passive_delegators_reward_period(v2::BlockIdentifier::AbsoluteHeight(
                        block_height,
                    ))
                    .await?
                    .response
                    .try_collect()
                    .await?;
                (baker_info, passive_info)
            } else {
                (vec![], vec![])
            };

        let baker_pool_stakes =
            PreparedPaydayBakerPoolStakes::prepare(&baker_reward_period_infos, block_height)?;

        let passive_pool_stake =
            PreparedPaydayPassivePoolStake::prepare(&passive_reward_period_info, block_height)?;

        let baker_payday_commission_rates =
            PreparedBakerPaydayCommissionRates::prepare(baker_reward_period_infos)?;

        let passive_delegation_status = if block_info.protocol_version
            >= ProtocolVersionInt::from(ProtocolVersion::P4)
        {
            Some(
                node_client
                    .get_passive_delegation_info(v2::BlockIdentifier::AbsoluteHeight(block_height))
                    .await?
                    .response,
            )
        } else {
            None
        };
        let passive_delegation_payday_commission_rates =
            PreparedPassiveDelegationPaydayCommissionRates::prepare(passive_delegation_status)?;

        let election_info = node_client
            .get_election_info(v2::BlockIdentifier::AbsoluteHeight(block_height))
            .await?
            .response;
        let payday_bakers_lottery_powers =
            PreparedPaydayLotteryPowers::prepare(election_info.bakers)?;

        Ok(Self {
            block_height: block_height.height.try_into()?,
            baker_payday_commission_rates,
            passive_delegation_payday_commission_rates,
            payday_bakers_lottery_powers,
            baker_pool_stakes,
            passive_pool_stake,
            refresh_latest_baker_apy_view: RefreshLatestBakerApy,
        })
    }

    pub async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        // Save the commission rates to the database.
        self.baker_payday_commission_rates.save(tx).await?;
        self.passive_delegation_payday_commission_rates
            .save(tx)
            .await?;

        // Save the lottery_powers to the database.
        self.payday_bakers_lottery_powers.save(tx).await?;

        sqlx::query!(
            "UPDATE current_chain_parameters
                SET last_payday_block_height = $1",
            self.block_height
        )
        .execute(tx.as_mut())
        .await?
        .ensure_affected_one_row()?;
        self.baker_pool_stakes
            .save(tx)
            .await
            .context("Failed inserting the reward period baker pool stakes")?;
        self.passive_pool_stake
            .save(tx)
            .await
            .context("Failed inserting the reward period passive pool stake")?;
        self.refresh_latest_baker_apy_view
            .save(tx)
            .await
            .context("Failed to refresh baker APY materialized views")?;
        Ok(())
    }
}

/// Represents the payday pool commission rates to passive delegators captured
/// from the `get_passive_delegation_info` node endpoint.
struct PreparedPassiveDelegationPaydayCommissionRates {
    transaction_commission: Option<i64>,
    baking_commission: Option<i64>,
    finalization_commission: Option<i64>,
}

impl PreparedPassiveDelegationPaydayCommissionRates {
    fn prepare(passive_delegation_status: Option<PassiveDelegationStatus>) -> anyhow::Result<Self> {
        Ok(Self {
            transaction_commission: passive_delegation_status.as_ref().map(|status| {
                i64::from(u32::from(PartsPerHundredThousands::from(
                    status.commission_rates.transaction,
                )))
            }),

            baking_commission: passive_delegation_status.as_ref().map(|status| {
                i64::from(u32::from(PartsPerHundredThousands::from(
                    status.commission_rates.baking,
                )))
            }),

            finalization_commission: passive_delegation_status.as_ref().map(|status| {
                i64::from(u32::from(PartsPerHundredThousands::from(
                    status.commission_rates.finalization,
                )))
            }),
        })
    }

    async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        // The fields `transaction_commission`, `baking_commission`, and
        // `finalization_commission` are either all `Some` or all `None`.
        if let (
            Some(transaction_commission),
            Some(baking_commission),
            Some(finalization_commission),
        ) = (
            self.transaction_commission,
            self.baking_commission,
            self.finalization_commission,
        ) {
            sqlx::query!(
                "
                INSERT INTO passive_delegation_payday_commission_rates (
                    payday_transaction_commission,
                    payday_baking_commission,
                    payday_finalization_commission
                )
                VALUES ($1, $2, $3)
                ON CONFLICT (id)
                DO UPDATE SET
                    payday_transaction_commission = EXCLUDED.payday_transaction_commission,
                    payday_baking_commission = EXCLUDED.payday_baking_commission,
                    payday_finalization_commission = EXCLUDED.payday_finalization_commission
                ",
                &transaction_commission,
                &baking_commission,
                &finalization_commission
            )
            .execute(tx.as_mut())
            .await?;
        }
        Ok(())
    }
}

/// Represents the payday baker pool commission rates captured from
/// the `get_bakers_reward_period` node endpoint.
struct PreparedBakerPaydayCommissionRates {
    baker_ids: Vec<i64>,
    transaction_commissions: Vec<i64>,
    baking_commissions: Vec<i64>,
    finalization_commissions: Vec<i64>,
}

impl PreparedBakerPaydayCommissionRates {
    fn prepare(baker_reward_period_info: Vec<BakerRewardPeriodInfo>) -> anyhow::Result<Self> {
        let capacity = baker_reward_period_info.len();
        let mut baker_ids: Vec<i64> = Vec::with_capacity(capacity);
        let mut transaction_commissions: Vec<i64> = Vec::with_capacity(capacity);
        let mut baking_commissions: Vec<i64> = Vec::with_capacity(capacity);
        let mut finalization_commissions: Vec<i64> = Vec::with_capacity(capacity);
        for info in baker_reward_period_info.iter() {
            baker_ids.push(i64::try_from(info.baker.baker_id.id.index)?);
            let commission_rates = info.commission_rates;

            transaction_commissions.push(i64::from(u32::from(PartsPerHundredThousands::from(
                commission_rates.transaction,
            ))));
            baking_commissions.push(i64::from(u32::from(PartsPerHundredThousands::from(
                commission_rates.baking,
            ))));
            finalization_commissions.push(i64::from(u32::from(PartsPerHundredThousands::from(
                commission_rates.finalization,
            ))));
        }

        Ok(Self {
            baker_ids,
            transaction_commissions,
            baking_commissions,
            finalization_commissions,
        })
    }

    async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        sqlx::query!(
            "DELETE FROM
                bakers_payday_commission_rates"
        )
        .execute(tx.as_mut())
        .await?;

        sqlx::query!(
            "INSERT INTO bakers_payday_commission_rates (
                id,
                payday_transaction_commission,
                payday_baking_commission,
                payday_finalization_commission
            )
            SELECT
                UNNEST($1::BIGINT[]) AS id,
                UNNEST($2::BIGINT[]) AS transaction_commission,
                UNNEST($3::BIGINT[]) AS baking_commission,
                UNNEST($4::BIGINT[]) AS finalization_commission",
            &self.baker_ids,
            &self.transaction_commissions,
            &self.baking_commissions,
            &self.finalization_commissions
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }
}

/// Represents the payday lottery power updates for bakers captured from
/// the `get_election_info` node endpoint.
struct PreparedPaydayLotteryPowers {
    baker_ids: Vec<i64>,
    bakers_lottery_powers: Vec<BigDecimal>,
    ranks: Vec<i64>,
}

impl PreparedPaydayLotteryPowers {
    fn prepare(mut bakers: Vec<BirkBaker>) -> anyhow::Result<Self> {
        let capacity = bakers.len();
        let mut baker_ids: Vec<i64> = Vec::with_capacity(capacity);
        let mut bakers_lottery_powers: Vec<BigDecimal> = Vec::with_capacity(capacity);
        let mut ranks: Vec<i64> = Vec::with_capacity(capacity);

        // Sort bakers by lottery power. The baker with the highest lottery power comes
        // first in the vector and gets rank 1.
        bakers.sort_by(|self_baker, other_baker| {
            self_baker
                .baker_lottery_power
                .total_cmp(&other_baker.baker_lottery_power)
                .reverse()
        });

        for (rank, baker) in bakers.iter().enumerate() {
            baker_ids.push(i64::try_from(baker.baker_id.id.index)?);
            bakers_lottery_powers.push(
                BigDecimal::from_f64(baker.baker_lottery_power)
                    .context(
                        "Expected f64 type (baker_lottery_power) to be converted correctly into \
                         BigDecimal type",
                    )
                    .map_err(v2::RPCError::ParseError)?,
            );
            ranks.push((rank + 1) as i64);
        }

        Ok(Self {
            baker_ids,
            bakers_lottery_powers,
            ranks,
        })
    }

    async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        sqlx::query!(
            "DELETE FROM
                bakers_payday_lottery_powers"
        )
        .execute(tx.as_mut())
        .await?;

        sqlx::query!(
            "INSERT INTO bakers_payday_lottery_powers (
                id,
                payday_lottery_power,
                payday_ranking_by_lottery_powers
            )
            SELECT
                UNNEST($1::BIGINT[]) AS id,
                UNNEST($2::NUMERIC[]) AS payday_lottery_power,
                UNNEST($3::BIGINT[]) AS payday_ranking_by_lottery_powers",
            &self.baker_ids,
            &self.bakers_lottery_powers,
            &self.ranks
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }
}

struct PreparedPaydayBakerPoolStakes {
    block_height: i64,
    baker_ids: Vec<i64>,
    baker_stake: Vec<i64>,
    delegators_stake: Vec<i64>,
}

impl PreparedPaydayBakerPoolStakes {
    fn prepare(
        bakers: &[BakerRewardPeriodInfo],
        block_height: AbsoluteBlockHeight,
    ) -> anyhow::Result<Self> {
        let capacity = bakers.len();
        let mut out = Self {
            block_height: block_height.height.try_into()?,
            baker_ids: Vec::with_capacity(capacity),
            baker_stake: Vec::with_capacity(capacity),
            delegators_stake: Vec::with_capacity(capacity),
        };
        for baker in bakers.iter() {
            out.baker_ids
                .push(baker.baker.baker_id.id.index.try_into()?);
            out.baker_stake
                .push(baker.equity_capital.micro_ccd().try_into()?);
            out.delegators_stake
                .push(baker.delegated_capital.micro_ccd().try_into()?);
        }
        Ok(out)
    }

    async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        sqlx::query!(
            "INSERT INTO payday_baker_pool_stakes (
                 payday_block,
                 baker,
                 baker_stake,
                 delegators_stake
             ) SELECT $1, * FROM UNNEST(
                     $2::BIGINT[],
                     $3::BIGINT[],
                     $4::BIGINT[]
             ) AS payday_baker(owner, baker_stake, delegators_stake)",
            self.block_height,
            &self.baker_ids,
            &self.baker_stake,
            &self.delegators_stake
        )
        .execute(tx.as_mut())
        .await?
        .ensure_affected_rows(self.baker_ids.len().try_into()?)?;
        Ok(())
    }
}

struct PreparedPaydayPassivePoolStake {
    block_height: i64,
    delegators_stake: i64,
}

impl PreparedPaydayPassivePoolStake {
    fn prepare(
        infos: &[DelegatorRewardPeriodInfo],
        block_height: AbsoluteBlockHeight,
    ) -> anyhow::Result<Self> {
        let delegators_stake = infos
            .iter()
            .map(|info| info.stake.micro_ccd())
            .sum::<u64>()
            .try_into()?;
        Ok(Self {
            block_height: block_height.height.try_into()?,
            delegators_stake,
        })
    }

    async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        sqlx::query!(
            "INSERT INTO payday_passive_pool_stakes (
                 payday_block,
                 delegators_stake
             ) VALUES ($1, $2)",
            self.block_height,
            self.delegators_stake
        )
        .execute(tx.as_mut())
        .await?;
        Ok(())
    }
}

/// Represent the database operation refreshing the materialized views
/// precomputing the APYs of each baker.
/// Assumes the bakers payday stake and rewards have already been updated in the
/// database.
struct RefreshLatestBakerApy;

impl RefreshLatestBakerApy {
    async fn save(&self, tx: &mut sqlx::PgTransaction<'_>) -> anyhow::Result<()> {
        sqlx::query!("REFRESH MATERIALIZED VIEW CONCURRENTLY latest_baker_apy_30_days")
            .execute(tx.as_mut())
            .await?;
        sqlx::query!("REFRESH MATERIALIZED VIEW CONCURRENTLY latest_baker_apy_7_days")
            .execute(tx.as_mut())
            .await?;
        Ok(())
    }
}
