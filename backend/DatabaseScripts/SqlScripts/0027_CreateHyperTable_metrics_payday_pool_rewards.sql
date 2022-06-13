create table metrics_payday_pool_rewards
(
    time                                 timestamp not null,
    index                                bigint generated always as identity,
    pool_id                              bigint    not null,
    transaction_fees_total_amount        bigint    not null,
    transaction_fees_baker_amount        bigint    not null,
    transaction_fees_delegator_amount    bigint    not null,
    baker_reward_total_amount            bigint    not null,
    baker_reward_baker_amount            bigint    not null,
    baker_reward_delegator_amount        bigint    not null,
    finalization_reward_total_amount     bigint    not null,
    finalization_reward_baker_amount     bigint    not null,
    finalization_reward_delegator_amount bigint    not null,
    sum_total_amount                     bigint    not null,
    sum_baker_amount                     bigint    not null,
    sum_delegator_amount                 bigint    not null,
    block_id                             bigint    not null
);

create index on metrics_payday_pool_rewards (pool_id, time DESC);
create index on metrics_payday_pool_rewards (pool_id, index DESC);

SELECT create_hypertable('metrics_payday_pool_rewards', 'time');
