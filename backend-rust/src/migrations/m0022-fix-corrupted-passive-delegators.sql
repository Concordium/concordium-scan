-- Migration which fixes data due to a bug in the migration file `m0013-fix-removed-delegators-restake.sql`
-- where delegators which set their target to a pool pending for removal got their `passiveDelegation` state
-- in the database corrupted.

-- The approach is to first find the latest set restake earnings event for each account and if the
-- `delegated_restake_earnings` is not set but the account is still using passive delegation, 
-- set the `delegated_restake_earnings` value again.
WITH latest_set_restake_earnings AS (
    SELECT
        transactions.sender_index,
        -- Parse out the target baker of from the events of the latest transaction.
        JSONB_PATH_QUERY(
            events,
            '$[*].DelegationSetRestakeEarnings.restake_earnings'
        )::BOOL as restake_earnings
    FROM transactions
        INNER JOIN (
            -- Find the latest transaction index for each account containing a 'DelegationSetRestakeEarnings' event.
            SELECT
                MAX(index) as tx_index,
                sender_index
            FROM transactions
            WHERE
                type_account = 'ConfigureDelegation'
                AND events @> '[{"DelegationSetRestakeEarnings": {}}]'::JSONB
            GROUP BY sender_index
        ) latest ON latest.sender_index = transactions.sender_index
            AND latest.tx_index = transactions.index
)
UPDATE accounts
    SET delegated_restake_earnings = latest_set_restake_earnings.restake_earnings
FROM latest_set_restake_earnings
WHERE
    -- Only update accounts which are passive delegating at the moment.
    accounts.delegated_stake > 0
    AND accounts.delegated_target_baker_id IS NULL
    -- Only update accounts which have sent a 'DelegationSetRestakeEarnings' transaction.
    AND accounts.index = latest_set_restake_earnings.sender_index;

-- Migration which fixes data due to bug in indexer where delegators which set their target to a pools pending for removal did not get updated (only relevant for blocks prior to Protocol Version 7).
-- This migration was meant to be part of `m0013-fix-removed-delegators-restake.sql` but was not correctly executed due to a bug.

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
    SET delegated_target_baker_id = NULL
FROM latest_set_delegation
WHERE
    -- Only update accounts which are delegating
    accounts.delegated_restake_earnings IS NOT NULL
    -- Only update accounts which have sent a 'set delegation target' transaction.
    AND accounts.index = latest_set_delegation.sender_index
    -- Only update accounts where the latest target baker is not currently baking.
    AND NOT EXISTS(SELECT TRUE FROM bakers WHERE id = latest_set_delegation.target);

