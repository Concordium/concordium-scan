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
    baker_stake
        BIGINT
        NOT NULL,
    -- The total stake of the accounts delegating to this pool.
    delegators_stake
        BIGINT
        NOT NULL,
    PRIMARY KEY(payday_block, baker)
);

-- Passive delegation stake in effect for the reward period at some payday.
CREATE TABLE payday_passive_pool_stakes(
    -- Block with the payday indicating the start of this reward period.
    payday_block
        BIGINT
        PRIMARY KEY
        REFERENCES blocks,
    -- The total passive stake.
    delegators_stake
        BIGINT
        NOT NULL
);

-- Compute the Annual Percentage Yield (APY).
CREATE FUNCTION apy(rewards FLOAT8, stake FLOAT8, paydays_per_year FLOAT8) RETURNS FLOAT8 AS $$
BEGIN
    RETURN POWER(
        1 + (rewards / stake),
        paydays_per_year
    ) - 1;
END;
$$ LANGUAGE plpgsql;

-- Accumulator for the aggregate function `geometric_mean`.
-- UPDATE: This function gets modified as part of `m0024-baker-apy-materialized-view.sql`.
CREATE FUNCTION geometric_mean_accum(accum FLOAT8[], item FLOAT8) RETURNS FLOAT8[] AS $$
BEGIN
    RETURN ARRAY[
        -- Index 0: holds the sum of ln for every item
        accum[1] + LN(item),
        -- Index 1: Items count
        accum[2] + 1.0
    ];
END;
$$ LANGUAGE plpgsql;

-- Finalizer function for the aggregate function `geometric_mean`.
-- UPDATE: This function gets modified as part of `m0024-baker-apy-materialized-view.sql`.
CREATE FUNCTION geometric_mean_finalize(accum FLOAT8[]) RETURNS FLOAT8 AS $$
BEGIN
    RETURN EXP(accum[1] / accum[2]);
END;
$$ LANGUAGE plpgsql;

-- Compute the geometric mean of a series of FLOAT8.
CREATE AGGREGATE geometric_mean(FLOAT8) (
    sfunc = geometric_mean_accum,
    stype = FLOAT8[],
    finalfunc = geometric_mean_finalize,
    initcond = '{0.0, 0.0}'
);
