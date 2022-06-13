create table metrics_pool_rewards
(
    time             timestamp not null,
    index            bigint generated always as identity,
    pool_id          bigint    not null,
    total_amount     bigint    not null,
    baker_amount     bigint    not null,
    delegator_amount bigint    not null,
    reward_type      int       not null,
    block_id         bigint    not null
);

create index on metrics_pool_rewards (pool_id, time DESC);
create index on metrics_pool_rewards (pool_id, index DESC);

SELECT create_hypertable('metrics_pool_rewards', 'time');
