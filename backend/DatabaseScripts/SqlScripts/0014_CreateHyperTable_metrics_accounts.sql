create table metrics_accounts
(
    time                        timestamp not null,
    cumulative_accounts_created bigint    not null,
    accounts_created            int       not null
);

SELECT create_hypertable('metrics_accounts','time');
