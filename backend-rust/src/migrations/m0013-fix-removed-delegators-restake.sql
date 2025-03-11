-- Migration fixing the delegated_restake_earnings column for removed delegators.
-- This column must be NULL when an account is removed as delegator.

-- First figure out which delegators are currently removed from the transactions table.
WITH delegators AS (
    SELECT
        sender_index as index,
        -- Compute the number of added delegation transactions subtracted from the number of
        -- removed delegation transactions.
        -- If the account is currently delegating this would result to 1 otherwise 0.
        SUM(CASE
                WHEN events @> '[{"DelegationRemoved": {}}]'::JSONB THEN -1
                ELSE 1
            END
        ) as is_delegating
    FROM transactions
    WHERE
        type_account = 'ConfigureDelegation'
        AND (
            events @> '[{"DelegationRemoved": {}}]'::JSONB
            OR events @> '[{"DelegationAdded": {}}]'::JSONB
        )
    GROUP BY sender_index
)
UPDATE accounts
    SET delegated_restake_earnings = NULL
FROM delegators
WHERE
    delegated_restake_earnings <> NULL
    AND accounts.index = delegators.index
    AND delegators.is_delegating = 0;
