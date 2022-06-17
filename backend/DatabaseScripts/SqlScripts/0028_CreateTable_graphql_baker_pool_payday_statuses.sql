create table graphql_baker_pool_payday_statuses
(
    payout_block_id bigint  not null,
    baker_id        bigint  not null,
    baker_stake     bigint  not null,
    delegated_stake bigint  not null,
    effective_stake bigint  not null,
    lottery_power   numeric not null,

    PRIMARY KEY (payout_block_id, baker_id)
);