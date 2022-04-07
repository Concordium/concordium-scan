create table metrics_rewards
(
    time       timestamp not null,
    account_id bigint    not null,
    amount     bigint    not null
);

create index on metrics_rewards (account_id, time DESC);

SELECT create_hypertable('metrics_rewards', 'time');
