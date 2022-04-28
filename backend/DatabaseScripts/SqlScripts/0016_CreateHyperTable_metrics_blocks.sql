create table metrics_blocks
(
    time                       timestamp not null,
    block_height               bigint    not null,
    block_time_secs            float     not null,
    finalization_time_secs     float     null,
    total_microccd             bigint    not null,
    total_microccd_released    bigint    null,
    total_microccd_encrypted   bigint    not null,
    total_microccd_staked      bigint    not null,
    total_percentage_released  float     null,
    total_percentage_encrypted float     not null,
    total_percentage_staked    float     not null
);

SELECT create_hypertable('metrics_blocks', 'time');
