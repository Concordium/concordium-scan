create table metrics_block
(
    time                     timestamp not null,
    block_height             bigint    not null,
    block_time_secs          float     not null,
    total_microccd           bigint    not null,
    total_encrypted_microccd bigint    not null
);

SELECT create_hypertable('metrics_block','time');
