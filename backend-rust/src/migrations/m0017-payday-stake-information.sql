-- Baker pool stake in effect for the reward period at some payday.
CREATE TABLE payday_baker_pool_stakes(
    -- Block with the payday indicating the start of this reward period.
    payday_block
        BIGINT
        NOT NULL
        REFERENCES blocks,
    -- Account index/ baker id of the owner of the baker pool.
    baker
        BIGINT
        NOT NULL
        REFERENCES accounts,
    -- The stake of the pool owner / baker.
    baker_stake BIGINT NOT NULL,
    -- The total stake of the accounts delegating to this pool.
    delegators_stake BIGINT NOT NULL,
    PRIMARY KEY(payday_block, baker)
);

-- Passive delegation stake in effect for the reward period at some payday.
CREATE TABLE payday_passive_pool_stakes(
    -- Block with the payday indicating the start of this reward period.
    payday_block BIGINT PRIMARY KEY REFERENCES blocks,
    -- The total stake of the accounts delegating to this pool.
    delegators_stake BIGINT NOT NULL
);
