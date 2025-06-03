CREATE INDEX passive_delegators_idx
ON accounts (delegated_target_baker_id, delegated_stake)
WHERE delegated_target_baker_id IS NULL AND delegated_stake != 0;

CREATE TABLE passive_delegation_payday_commission_rates(
    -- This field is always `true` and a primary key to constrain the table to have a single row.
    id BOOL PRIMARY KEY DEFAULT true CHECK (id),
    -- Fraction of transaction rewards rewarded at payday to passive delegators.
    -- Stored as a fraction of an amount with a precision of `1/100_000`.
    payday_transaction_commission 
        BIGINT
        NOT NULL,
    -- Fraction of baking rewards rewarded at payday to passive delegators.
    -- Stored as a fraction of an amount with a precision of `1/100_000`.
    payday_baking_commission 
        BIGINT
        NOT NULL,
    -- Fraction of finalization rewards rewarded at payday to passive delegators.
    -- Stored as a fraction of an amount with a precision of `1/100_000`.
    payday_finalization_commission 
        BIGINT
        NOT NULL
);
