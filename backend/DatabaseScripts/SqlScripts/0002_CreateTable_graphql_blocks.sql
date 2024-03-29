create table graphql_blocks
(
    id                                          bigint primary key generated always as identity,
    block_height                                bigint    not null,
    block_hash                                  text      not null,
    block_slot_time                             timestamp not null,
    baker_id                                    int       null,
    finalized                                   bool      not null,
    transaction_count                           int       not null,
    finalization_data_block_pointer             text      null,
    finalization_data_index                     bigint    null,
    finalization_data_delay                     bigint    null,
    bal_stats_total_amount                      bigint    not null,
    bal_stats_total_amount_released             bigint    null,
    bal_stats_total_amount_encrypted            bigint    not null,
    bal_stats_total_amount_locked_in_schedules  bigint    not null,
    bal_stats_total_amount_staked               bigint    not null,
    bal_stats_total_amount_staked_by_bakers     bigint    not null,
    bal_stats_total_amount_staked_by_delegation bigint    not null,
    bal_stats_baking_reward_account             bigint    not null,
    bal_stats_finalization_reward_account       bigint    not null,
    bal_stats_gas_account                       bigint    not null,
    block_stats_block_time_secs                 float     not null,
    block_stats_finalization_time_secs          float     null,
    chain_parameters_id                         int       not null
);

create index graphql_blocks_block_hash_index
    on graphql_blocks (block_hash text_pattern_ops);

create index graphql_blocks_block_height_index
    on graphql_blocks (block_height);