alter table graphql_bakers
    add column active_pool_payday_status_baker_stake bigint null,
    add column active_pool_payday_status_delegated_stake bigint null;
