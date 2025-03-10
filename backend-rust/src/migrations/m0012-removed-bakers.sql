-- Migration adding the `bakers_removed` table.


-- Table with a row for each account which have been a baker/validator in the past.
CREATE TABLE bakers_removed (
    -- Baker/validator ID, corresponding to the account index.
    id
        BIGINT
        PRIMARY KEY
        REFERENCES accounts,
    -- Transaction index configuring the validator/baker to be removed.
    removed_by_tx_index
        BIGINT
        NOT NULL
        REFERENCES transactions
);

-- Find last remove baker transaction for each account, ignoring the current bakers.
INSERT INTO bakers_removed
SELECT
    sender_index as id,
    MAX(index) as removed_by_tx_index
FROM transactions
WHERE
    -- Ensure the sender is not currently baking.
    sender_index NOT IN (SELECT id FROM bakers)
    AND (
        type_account = 'RemoveBaker'
        OR (
            type_account IN ('ConfigureBaker', 'ConfigureDelegation')
            AND events @> '[{"BakerRemoved": {}}]'::JSONB
        )
    )
GROUP BY sender_index ORDER BY sender_index;

-- To undo the migration changes:

-- DROP TABLE bakers_removed;
