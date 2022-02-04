create table metrics_transaction
(
    time             timestamp not null,
    transaction_type text      not null,
    micro_ccd_cost   bigint    not null,
    success          bool      not null
);

SELECT create_hypertable('metrics_transaction','time');