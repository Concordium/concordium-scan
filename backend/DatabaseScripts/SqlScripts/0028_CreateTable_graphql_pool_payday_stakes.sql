create table graphql_pool_payday_stakes
(
    payout_block_id bigint  not null,
    pool_id         bigint  not null,
    baker_stake     bigint  not null,
    delegated_stake bigint  not null,

    PRIMARY KEY (payout_block_id, pool_id)
);