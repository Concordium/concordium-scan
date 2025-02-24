ALTER TABLE bakers
    DROP CONSTRAINT check_pool_total_staked_positive,
    ADD CONSTRAINT check_pool_total_staked_positive CHECK (pool_total_staked >= 0);
