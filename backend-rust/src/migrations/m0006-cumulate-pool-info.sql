-- Migration adding accumulated pool state to bakers table, for faster filtering based on these
-- values.

-- Add new columns as nullable:
ALTER TABLE bakers
    -- Total stake in the pool, so delegated staked + bakers stake.
    ADD COLUMN pool_total_staked BIGINT,
    -- Number of delegators targeting this pool.
    ADD COLUMN pool_delegator_count BIGINT;

-- Update new columns for bakers with delegators.
WITH pool_delegators AS (
     SELECT
         delegated_target_baker_id,
         SUM(delegated_stake) as total_delegated_stake,
         COUNT(*) as delegator_count
     FROM accounts
     WHERE delegated_target_baker_id IS NOT NULL
     GROUP BY delegated_target_baker_id
)
UPDATE bakers
    SET pool_total_staked = bakers.staked + pool_delegators.total_delegated_stake,
        pool_delegator_count = pool_delegators.delegator_count
    FROM pool_delegators
    WHERE bakers.id = pool_delegators.delegated_target_baker_id;

-- Update new columns for bakers without delegators.
UPDATE bakers
    SET pool_total_staked = bakers.staked,
        pool_delegator_count = 0
    WHERE
        pool_delegator_count IS NULL
        AND pool_total_staked IS NULL;

-- Mark new columns as NOT NULL
ALTER TABLE bakers
    ALTER COLUMN pool_total_staked SET NOT NULL,
    ALTER COLUMN pool_delegator_count SET NOT NULL;

-- Revert changes, useful for testing.

-- ALTER TABLE bakers
--     DROP COLUMN pool_total_staked,
--     DROP COLUMN pool_delegator_count;
