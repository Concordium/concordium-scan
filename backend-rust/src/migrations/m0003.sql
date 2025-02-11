CREATE TABLE bakers_payday_commission_rates(
    -- Baker/validator ID, corresponding to the account index.
    id
        BIGINT
        PRIMARY KEY,
    -- Fraction of transaction rewards rewarded at payday to this baker pool.
    -- Stored as a fraction of an amount with a precision of `1/100_000`.
    payday_transaction_commission 
        BIGINT
        NOT NULL,
    -- Fraction of baking rewards rewarded at payday to this baker pool.
    -- Stored as a fraction of an amount with a precision of `1/100_000`.
    payday_baking_commission 
        BIGINT
        NOT NULL,
    -- Fraction of finalization rewards rewarded at payday to this baker pool.
    -- Stored as a fraction of an amount with a precision of `1/100_000`.
    payday_finalization_commission 
        BIGINT
        NOT NULL
);
