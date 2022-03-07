-- TODO: block time should be non-null
alter table graphql_blocks
    add column block_stats_block_time_secs        float null,
    add column block_stats_finalization_time_secs float null;

