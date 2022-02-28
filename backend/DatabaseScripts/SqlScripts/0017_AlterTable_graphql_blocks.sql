alter table graphql_blocks
    add column bal_stats_total_amount bigint null,
    add column bal_stats_total_encrypted_amount bigint null,
    add column bal_stats_baking_reward_account bigint null,
    add column bal_stats_finalization_reward_account bigint null,
    add column bal_stats_gas_account bigint null;