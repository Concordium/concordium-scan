-- Migration loosening the constraint enforcing `pool_total_staked > 0` to `pool_total_staked >= 0`,
-- since this is the case for the genesis validator with id 8 on Concordium Mainnet.
ALTER TABLE bakers
    DROP CONSTRAINT check_pool_total_staked_positive,
    ADD CONSTRAINT check_pool_total_staked_positive CHECK (pool_total_staked >= 0);
