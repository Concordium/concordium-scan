create table metrics_transactions
(
    time                         timestamp not null,
    cumulative_transaction_count bigint    not null,
    transaction_type             text      not null,
    micro_ccd_cost               bigint    not null,
    success                      bool      not null
);

SELECT create_hypertable('metrics_transactions','time');