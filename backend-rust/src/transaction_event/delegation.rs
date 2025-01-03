use crate::{
    address::AccountAddress,
    types::{AccountIndex, Amount, BakerId},
};
use async_graphql::{SimpleObject, Union};

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DelegationAdded {
    pub delegator_id:    AccountIndex,
    pub account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DelegationRemoved {
    pub delegator_id:    AccountIndex,
    pub account_address: AccountAddress,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DelegationSetDelegationTarget {
    pub delegator_id:      AccountIndex,
    pub account_address:   AccountAddress,
    pub delegation_target: DelegationTarget,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DelegationSetRestakeEarnings {
    pub delegator_id:     AccountIndex,
    pub account_address:  AccountAddress,
    pub restake_earnings: bool,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DelegationStakeDecreased {
    pub delegator_id:      AccountIndex,
    pub account_address:   AccountAddress,
    pub new_staked_amount: Amount,
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct DelegationStakeIncreased {
    pub delegator_id:      AccountIndex,
    pub account_address:   AccountAddress,
    pub new_staked_amount: Amount,
}

#[derive(Union, serde::Serialize, serde::Deserialize)]
pub enum DelegationTarget {
    PassiveDelegationTarget(PassiveDelegationTarget),
    BakerDelegationTarget(BakerDelegationTarget),
}

impl TryFrom<concordium_rust_sdk::types::DelegationTarget> for DelegationTarget {
    type Error = anyhow::Error;

    fn try_from(target: concordium_rust_sdk::types::DelegationTarget) -> Result<Self, Self::Error> {
        use concordium_rust_sdk::types::DelegationTarget as Target;
        match target {
            Target::Passive => {
                Ok(DelegationTarget::PassiveDelegationTarget(PassiveDelegationTarget {
                    dummy: true,
                }))
            }
            Target::Baker {
                baker_id,
            } => Ok(DelegationTarget::BakerDelegationTarget(BakerDelegationTarget {
                baker_id: baker_id.id.index.try_into()?,
            })),
        }
    }
}

#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct PassiveDelegationTarget {
    #[graphql(
        name = "_",
        deprecation = "Don't use! This field is only in the schema to make this a valid GraphQL \
                       type (which does not allow types without any fields)"
    )]
    pub dummy: bool,
}
#[derive(SimpleObject, serde::Serialize, serde::Deserialize)]
pub struct BakerDelegationTarget {
    pub baker_id: BakerId,
}
