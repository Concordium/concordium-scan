-- Migration fixing some failed indexing, where the delegators did not get moved the passive pool as
-- their target validator pool got closed or removed.

-- Move delegators targeting an unknown baker id to the passive pool.
UPDATE accounts
    SET delegated_target_baker_id = NULL
    WHERE NOT EXISTS (SELECT FROM bakers WHERE bakers.id = accounts.delegated_target_baker_id);

-- Move delegators currently targeting a close for all pool.
UPDATE accounts
    SET delegated_target_baker_id = NULL
    FROM bakers
    WHERE
        bakers.id = accounts.delegated_target_baker_id
        AND bakers.open_status = 'ClosedForAll';

-- Add foreign key constraint to capture missing handling of removed bakers.
ALTER TABLE accounts
    ADD CONSTRAINT fk_delegated_target_baker_id
        FOREIGN KEY (delegated_target_baker_id)
        REFERENCES bakers (id);

-- Migration fixing invalid data for table `account_statements`. Here the changed amount (`amount`)
-- got accounted for twice in `account_balance`.

UPDATE account_statements
    SET account_balance = account_balance - amount;
