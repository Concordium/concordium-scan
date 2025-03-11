use super::ApiResult;

use super::account::Account;

use crate::scalar_types::Amount;
use async_graphql::{connection, Context, Object, SimpleObject};
// use futures::TryStreamExt;

#[derive(Default)]
pub struct QueryPassiveDelegation;

#[Object]
impl QueryPassiveDelegation {
    async fn passive_delegation<'a>(&self, _ctx: &Context<'a>) -> ApiResult<PassiveDelegation> {
        todo!()
    }
}

pub struct PassiveDelegation {
    // commissionRates:  CommissionRates!
    //
    // Query:
    //   poolRewards(
    //     after: $afterRewards
    //     before: $beforeRewards
    //     first: $firstRewards
    //     last: $lastRewards
    //   ) {
    //     pageInfo {
    //       hasNextPage
    //       hasPreviousPage
    //       startCursor
    //       endCursor
    //       __typename
    //     }
    //     nodes {
    //       block {
    //         blockHash
    //         __typename
    //       }
    //       id
    //       timestamp
    //       bakerReward {
    //         bakerAmount
    //         delegatorsAmount
    //         totalAmount
    //         __typename
    //       }
    //       finalizationReward {
    //         bakerAmount
    //         delegatorsAmount
    //         totalAmount
    //         __typename
    //       }
    //       transactionFees {
    //         bakerAmount
    //         delegatorsAmount
    //         totalAmount
    //         __typename
    //       }
    //       __typename
    //     }
    //     __typename
    //   }
    // Schema:
    // poolRewards("Returns the first _n_ elements from the list." first: Int "Returns the elements in the list that come after the specified cursor." after: String "Returns the last _n_ elements from the list." last: Int "Returns the elements in the list that come before the
    // specified cursor." before: String): PaydayPoolRewardConnection
    //
    // Query:
    // apy7days: apy(period: LAST7_DAYS)
    // apy30days: apy(period: LAST30_DAYS)
    // Schema:
    // apy(period: ApyPeriod!): Float
    //
    // delegatorCount: Int!
    //
    // "The total amount staked by delegators to passive delegation."
    // delegatedStake: UnsignedLong!
    //
    // "Total stake passively delegated as a percentage of all CCDs in existence."
    // delegatedStakePercentage: Decimal!
}

impl PassiveDelegation {}
#[Object]
impl PassiveDelegation {
    // Query:
    // delegators(
    //     after: $afterDelegators
    //     before: $beforeDelegators
    //     first: $firstDelegators
    //     last: $lastDelegators
    //   ) {
    //     nodes {
    //       accountAddress {
    //         asString
    //         __typename
    //       }
    //       stakedAmount
    //       restakeEarnings
    //       __typename
    //     }
    //     pageInfo {
    //       hasNextPage
    //       hasPreviousPage
    //       startCursor
    //       endCursor
    //       __typename
    //     }
    //     __typename
    //   }
    // Schema:
    // delegators("Returns the first _n_ elements from the list." first: Int
    // "Returns the elements in the list that come after the specified cursor."
    // after: String "Returns the last _n_ elements from the list." last: Int
    // "Returns the elements in the list that come before the specified cursor."
    // before: String): DelegatorsConnection
    async fn delegators(
        &self,
        _ctx: &Context<'_>,
        #[graphql(desc = "Returns the first _n_ elements from the list.")] _first: Option<u64>,
        #[graphql(desc = "Returns the elements in the list that come after the specified cursor.")]
        _after: Option<String>,
        #[graphql(desc = "Returns the last _n_ elements from the list.")] _last: Option<u64>,
        #[graphql(
            desc = "Returns the elements in the list that come before the specified cursor."
        )]
        _before: Option<String>,
    ) -> ApiResult<connection::Connection<String, DelegationSummary>> {
        todo!()
    }
}

#[derive(SimpleObject)]
struct DelegationSummary {
    account_address: Account,
    staked_amount: Amount,
    restake_earnings: bool,
}
