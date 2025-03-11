CREATE TABLE payday_bakers_pool_rewards(
    -- Height of the payday block.
    payday_block_height
        BIGINT
        NOT NULL,
    -- Baker/validator ID, corresponding to the account index.
    baker_id
        BIGINT
        NOT NULL,
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
    -- Make the `payday_block_height` and `baker_id` the primary key.
    PRIMARY KEY (payday_block_height, baker_id)
);
