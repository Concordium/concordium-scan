-- Migration script for adding the scheduled release table.

-- Every scheduled release on chain.
CREATE TABLE scheduled_releases (
    -- An index/id for this scheduled release (row number).
    index
        BIGINT GENERATED ALWAYS AS IDENTITY
        PRIMARY KEY,
    -- The index of the transaction creating the scheduled transfer.
    transaction_index
        BIGINT
        NOT NULL
        REFERENCES transactions,
    -- The account receiving the scheduled transfer.
    account_index
        BIGINT
        NOT NULL
        REFERENCES accounts,
    -- The scheduled release time.
    release_time
        TIMESTAMPTZ
        NOT NULL,
    -- The amount locked in the scheduled release.
    amount
        BIGINT
        NOT NULL
);

-- We typically want to find all scheduled releases for a specific account after a certain time.
-- This index is useful for that.
CREATE INDEX scheduled_releases_idx ON scheduled_releases (account_index, release_time);
