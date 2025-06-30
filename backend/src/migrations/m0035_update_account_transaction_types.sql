

ALTER TYPE account_transaction_type
    ADD VALUE IF NOT EXISTS 'TokenUpdate';



ALTER TYPE update_transaction_type 
    ADD VALUE IF NOT EXISTS 'CreatePltUpdate';

