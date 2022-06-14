create table graphql_bakers
(
    id                                        bigint primary key,
    active_staked_amount                      bigint    null,
    active_restake_earnings                   bool      null,
    active_pending_change                     json      null,
    active_pool_open_status                   int       null,
    active_pool_metadata_url                  text      null,
    active_pool_transaction_commission        numeric   null,
    active_pool_finalization_commission       numeric   null,
    active_pool_baking_commission             numeric   null,
    active_pool_delegated_stake               bigint    null,
    active_pool_total_stake                   bigint    null,
    active_pool_delegator_count               int       null,
    active_pool_delegated_stake_cap           bigint    null,
    active_pool_payday_status_baker_stake     bigint    null,
    active_pool_payday_status_delegated_stake bigint    null,
    active_pool_payday_status_effective_stake bigint    null,
    active_pool_payday_status_lottery_power   numeric   null,
    removed_timestamp                         timestamp null
);
