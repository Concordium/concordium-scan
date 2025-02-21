-- Add new columns:
ALTER TABLE current_chain_parameters
    -- The maximum leverage bound (also called leverage factor in the node API) that a baker can have as a ratio of the total stake in the protocol (from all bakers) 
    -- to the equity capital of one baker (only the baker's own stake, but not including delegated stake to the baker). 
    -- The value is 1 or greater (1 <= leverage_bound).
    -- The `leverage_bound` helps maintain network decentralization by preventing a single baker from gaining excessive power in the consensus protocol.
    ADD COLUMN leverage_bound_numerator
        BIGINT
        NOT NULL
        DEFAULT 1, -- The default value is only used for adding the column to the table and later overwritten in the migration script.
    ADD COLUMN leverage_bound_denominator
        BIGINT
        NOT NULL
        DEFAULT 1, -- The default value is only used for adding the column to the table and later overwritten in the migration script.
    -- A maximum capital bound that a baker can have as a ratio of the baker's own stake (only the baker's own stake, 
    -- not including the delegated stake to the baker) to the total staked capital of that baker (including the baker's own stake 
    -- and the delegated stake to the baker).
    -- This value is always greater than 0  (capital_bound > 0).
    -- The value is stored as a fraction with precision of `1/100_000`. For example, a capital bound of 0.05 is stored as 50000.
    -- The `capital_bound` ensures that each baker has skin in the game by providing some of the CCD staked from its own funds.  
    ADD COLUMN capital_bound 
        BIGINT
        NOT NULL
        DEFAULT 1; -- The default value is only used for adding the column to the table and later overwritten in the migration script.