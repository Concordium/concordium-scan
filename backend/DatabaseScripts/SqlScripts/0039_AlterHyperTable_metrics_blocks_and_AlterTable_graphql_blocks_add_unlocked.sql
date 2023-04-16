ALTER TABLE metrics_blocks 
    ADD COLUMN total_microccd_unlocked bigint NULL;
ALTER TABLE graphql_blocks 
    ADD COLUMN bal_stats_total_amount_unlocked bigint NULL;
