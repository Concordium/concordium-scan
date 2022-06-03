alter table graphql_bakers
    add column active_pool_delegated_stake_cap bigint null;

update graphql_bakers
set active_pool_delegated_stake_cap = 0
where active_pool_delegated_stake is not null;