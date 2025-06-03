CREATE TABLE bakers_payday_lottery_powers(
    -- Baker/validator ID, corresponding to the account index.
    id
        BIGINT
        PRIMARY KEY,
    -- Lottery power in the consensus algorithm at the last payday period of the above baker.
    payday_lottery_power
        NUMERIC
        NOT NULL
);
