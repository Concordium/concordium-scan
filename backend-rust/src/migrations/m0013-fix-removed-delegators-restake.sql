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
    delegated_restake_earnings IS NOT NULL
    AND accounts.index = delegators.index
    AND delegators.is_delegating = 0;


-- Migration which fixes data due to bug in indexer where delegators which set their target to a pools pending for removal did not get updated (only relevant for blocks prior to Protocol Version 7).

-- The approach is to first find the latest set delegation target event for each account and ensure
-- the target is still a baker, if not we set the target to the passive pool.
WITH latest_set_delegation AS (
    SELECT
        transactions.sender_index,
        -- Parse out the target baker of from the events of the latest transaction.
        JSONB_PATH_QUERY(
            events,
            '$[*].DelegationSetDelegationTarget.delegation_target.BakerDelegationTarget.baker_id'
        )::BIGINT as target
    FROM transactions
        INNER JOIN (
            -- Find the latest transaction index for each account containing a 'DelegationSetDelegationTarget' event.
            SELECT
                MAX(index) as tx_index,
                sender_index
            FROM transactions
            WHERE
                type_account = 'ConfigureDelegation'
                AND events @> '[{"DelegationSetDelegationTarget": {}}]'::JSONB
            GROUP BY sender_index
        ) latest ON latest.sender_index = transactions.sender_index
            AND latest.tx_index = transactions.index
)
UPDATE accounts
    SET delegated_restake_earnings = NULL
FROM latest_set_delegation
WHERE
    -- Only update accounts which are delegating
    accounts.delegated_restake_earnings IS NOT NULL
    -- Only update accounts which have sent a 'set delegation target' transaction.
    AND accounts.index = latest_set_delegation.sender_index
    -- Only update accounts where the latest target baker is not currently baking.
    AND NOT EXISTS(SELECT TRUE FROM bakers WHERE id = latest_set_delegation.target);


-- Migration recomputing the pool_delegator_count and total_delegated_stake

-- First reset every baker to the state of having no delegators.
UPDATE bakers
    SET pool_total_staked = bakers.staked,
        pool_delegator_count = 0;

-- Then update bakers with delegators.
WITH pool_delegators AS (
     SELECT
         delegated_target_baker_id,
         SUM(delegated_stake) as total_delegated_stake,
         COUNT(*) as delegator_count
     FROM accounts
     WHERE delegated_target_baker_id IS NOT NULL
     GROUP BY delegated_target_baker_id
)
UPDATE bakers
    SET pool_total_staked = bakers.staked + pool_delegators.total_delegated_stake,
        pool_delegator_count = pool_delegators.delegator_count
    FROM pool_delegators
    WHERE bakers.id = pool_delegators.delegated_target_baker_id;
