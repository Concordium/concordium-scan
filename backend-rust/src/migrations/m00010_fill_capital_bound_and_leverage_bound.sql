-- Add new columns:
ALTER TABLE current_chain_parameters
    -- The leverage bound (also called leverage factor in the node API) is the maximum proportion of total stake 
    -- of a baker (including the baker's own stake and the delegated stake to the baker) to the baker's own stake (excluding delegated stake to the baker)
    -- that a baker can achieve where the total stake of the baker is considered for calculating the lottery power or finalizer weight 
    -- in the consensus (effective stake).
    -- Once this bound is passed, some of the baker's total stake no longer contribute 
    -- to lottery power or finalizer weight in the consensus algorithm, meaning that part of the baker's total stake will
    -- no longer be considered as effective stake.
    -- The value is 1 or greater (1 <= leverage_bound).
    -- The value's numerator and denominator is stored.
    -- The `leverage bound` ensures that each baker has skin in the game with respect to its delegators by providing some of the CCD staked from its own funds.  
   ADD COLUMN leverage_bound_numerator
        BIGINT
        NOT NULL
        DEFAULT 1, -- The default value is only used for adding the column to the table and later overwritten in the migration script.
    ADD COLUMN leverage_bound_denominator
        BIGINT
        NOT NULL
        DEFAULT 1, -- The default value is only used for adding the column to the table and later overwritten in the migration script.
    -- The capital bound is the maximum proportion of the total stake in the protocol (from all bakers including passive delegation) 
    -- to the total stake of a baker (including the baker's own stake and the delegated stake to the baker) that a baker can 
    -- achieve where the total stake of the baker is considered for calculating the lottery power or finalizer weight 
    -- in the consensus (effective stake).
    -- Once this bound is passed, some of the baker's total stake no longer contribute 
    -- to lottery power or finalizer weight in the consensus algorithm, meaning that part of the baker's total stake will
    -- no longer be considered as effective stake.
    -- The capital bound is always greater than 0 (capital_bound > 0).
    -- The value is stored as a fraction with precision of `1/100_000`. For example, a capital bound of 0.05 is stored as 5000.
    -- The `capital_bound` helps maintain network decentralization by preventing a single baker from gaining excessive power in the consensus protocol.
    ADD COLUMN capital_bound 
        BIGINT
        NOT NULL
        DEFAULT 1; -- The default value is only used for adding the column to the table and later overwritten in the migration script.