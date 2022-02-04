create table metrics_block
(
    time            timestamp not null,
    block_height    bigint    not null,
    block_time_secs int       not null
);

SELECT create_hypertable('metrics_block','time');
