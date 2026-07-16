CREATE TABLE plt_locks (
    lock_id TEXT PRIMARY KEY,
    creator_account_index BIGINT REFERENCES accounts(index),
    created_transaction_index BIGINT REFERENCES transactions(index),
    created_block_height BIGINT,
    created_at TIMESTAMPTZ,
    expiry TIMESTAMPTZ,
    canceled_transaction_index BIGINT REFERENCES transactions(index),
    canceled_block_height BIGINT,
    canceled_at TIMESTAMPTZ,
    config JSONB,
    raw_config TEXT,
    metadata_name TEXT,
    metadata_description TEXT
);

CREATE INDEX plt_locks_created_at_idx
ON plt_locks(created_at DESC, lock_id);

CREATE INDEX plt_locks_canceled_at_idx
ON plt_locks(canceled_at)
WHERE canceled_at IS NOT NULL;

CREATE TABLE plt_lock_events (
    id BIGSERIAL PRIMARY KEY,
    transaction_index BIGINT NOT NULL REFERENCES transactions(index),
    block_height BIGINT NOT NULL,
    slot_time TIMESTAMPTZ NOT NULL,
    operation_order INT NOT NULL,
    event_type TEXT NOT NULL CHECK (
        event_type IN (
            'LockCreate',
            'LockFund',
            'LockSend',
            'LockReturn',
            'LockCancel',
            'LockDestroy'
        )
    ),
    lock_id TEXT NOT NULL REFERENCES plt_locks(lock_id),
    token_index BIGINT REFERENCES plt_tokens(index),
    account_index BIGINT REFERENCES accounts(index),
    source_account_index BIGINT REFERENCES accounts(index),
    recipient_account_index BIGINT REFERENCES accounts(index),
    amount NUMERIC,
    decimals INT,
    memo JSONB,
    event JSONB NOT NULL
);

CREATE INDEX plt_lock_events_lock_time_idx
ON plt_lock_events(lock_id, slot_time DESC, operation_order DESC);

CREATE INDEX plt_lock_events_transaction_idx
ON plt_lock_events(transaction_index, operation_order);

CREATE TABLE plt_lock_balances (
    lock_id TEXT NOT NULL REFERENCES plt_locks(lock_id),
    account_index BIGINT NOT NULL REFERENCES accounts(index),
    token_index BIGINT NOT NULL REFERENCES plt_tokens(index),
    amount NUMERIC NOT NULL DEFAULT 0,
    decimal INT NOT NULL DEFAULT 0,
    PRIMARY KEY (lock_id, account_index, token_index)
);

CREATE INDEX plt_lock_balances_account_idx
ON plt_lock_balances(account_index, token_index)
WHERE amount <> 0;

CREATE TABLE plt_lock_accounts (
    lock_id TEXT NOT NULL REFERENCES plt_locks(lock_id),
    account_index BIGINT NOT NULL REFERENCES accounts(index),
    relationship_type TEXT NOT NULL CHECK (
        relationship_type IN (
            'Creator',
            'BalanceHolder',
            'Recipient',
            'Controller',
            'Touched'
        )
    ),
    first_transaction_index BIGINT NOT NULL REFERENCES transactions(index),
    last_transaction_index BIGINT NOT NULL REFERENCES transactions(index),
    PRIMARY KEY (lock_id, account_index, relationship_type)
);

CREATE INDEX plt_lock_accounts_account_idx
ON plt_lock_accounts(account_index, lock_id);
