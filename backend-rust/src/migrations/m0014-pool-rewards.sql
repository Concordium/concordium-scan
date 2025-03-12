CREATE TABLE bakers_payday_pool_rewards(
    -- Height of the payday block.
    payday_block_height
        BIGINT
        NOT NULL,
    -- Baker/validator ID, corresponding to the account index.
    -- The pool owner is `NULL` if the pool rewards are for the passive delegators. 
    -- There is only one `PaydayPoolReward` row entry per payday block that goes to the passive delegators.
    pool_owner
        BIGINT,
    -- Primary keys cannot be `NULL`. Replace `NULL` with `-1` since we want to use the `pool_owner` as part of the primary key.
    pool_owner_for_primary_key BIGINT GENERATED ALWAYS AS (COALESCE(pool_owner, -1)) STORED,
    -- Total transaction rewards rewarded at payday to this baker pool.
    -- Some of the total transaction rewards go to the `baker` running the pool while the other part goes to its delegators.
    -- The value is represented in microCCD, where one unit equals 1/1,000,000 CCD.
    -- E.g. 0.05 CCDs are stored as 50000.
    payday_total_transaction_rewards
        BIGINT
        NOT NULL,
    -- The part of above `payday_total_transaction_rewards` that goes to pool's delgators.
    payday_delegators_transaction_rewards
        BIGINT
        NOT NULL,
    -- Total baking rewards rewarded at payday to this baker pool.
    -- Some of the total baking rewards go to the `baker` running the pool while the other part goes to its delegators.
    -- The value is represented in microCCD, where one unit equals 1/1,000,000 CCD.
    -- E.g. 0.05 CCDs are stored as 50000.
    payday_total_baking_rewards
        BIGINT
        NOT NULL,
    -- The part of above `payday_total_baking_rewards` that goes to pool's delgators.
    payday_delegators_baking_rewards
        BIGINT
        NOT NULL,
    -- Total finalization rewards rewarded at payday to this baker pool.
    -- Some of the total finalization rewards go to the `baker` running the pool while the other part goes to its delegators.
    -- The value is represented in microCCD, where one unit equals 1/1,000,000 CCD.
    -- E.g. 0.05 CCDs are stored as 50000.
    payday_total_finalization_rewards
        BIGINT
        NOT NULL,
    -- The part of above `payday_total_finalization_rewards` that goes to pool's delgators.
    payday_delegators_finalization_rewards
        BIGINT
        NOT NULL,
    -- Use the pair of `pool_owner` and `payday_block_height` as primary key. 
    -- This creates an index to efficiently query the `BakerPool::poolRewards` and `PassiveDelegation::poolRewards`.
    -- Because the `pool_owner` can be `NULL` the `pool_owner_for_primary_key` is used which replaces `NULL` with `-1`.
    PRIMARY KEY (pool_owner_for_primary_key, payday_block_height)  -- Treat NULL as -1 in the index
);
