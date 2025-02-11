ALTER TABLE bakers
-- Fraction of transaction rewards rewarded at payday to this baker pool.
-- Stored as a fraction of an amount with a precision of `1/100_000`.
ADD COLUMN payday_transaction_commission BIGINT,
-- Fraction of baking rewards rewarded at payday to this baker pool.
-- Stored as a fraction of an amount with a precision of `1/100_000`.
ADD COLUMN payday_baking_commission BIGINT,
-- Fraction of finalization rewards rewarded at payday to this baker pool.
-- Stored as a fraction of an amount with a precision of `1/100_000`.
ADD COLUMN payday_finalization_commission BIGINT;
