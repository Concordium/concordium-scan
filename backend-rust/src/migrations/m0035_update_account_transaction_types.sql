-- Migration to update the account_transaction_type enum to include new transaction types

ALTER TYPE account_transaction_type
    ADD VALUE IF NOT EXISTS 'TokenHolder';

ALTER TYPE account_transaction_type
    ADD VALUE IF NOT EXISTS 'TokenGovernance';



ALTER TYPE update_transaction_type 
    ADD VALUE IF NOT EXISTS 'CreatePltUpdate';

