CREATE TABLE bakers_payday_lottery_powers(
    -- Baker/validator ID, corresponding to the account index.
    id
        BIGINT
        PRIMARY KEY,
    -- Fraction of transaction rewards rewarded at payday to this baker pool.
    -- Stored as a fraction of an amount with a precision of `1/100_000`.
    payday_lottery_power
        NUMERIC
        NOT NULL
);
