-- plt_accounts_statement table for PLT transaction history
-- This table stores account-level statements for PLT token operations

-- enum for PLT account statement entry types
CREATE TYPE plt_account_statement_entry_type AS ENUM (
    'Mint',
    'Burn',
    'TransferIn',
    'TransferOut'
);

-- plt_accounts_statement table
CREATE TABLE plt_accounts_statement (
    -- Composite primary key: account_index + plt_event_id for uniqueness
    account_index BIGINT NOT NULL REFERENCES accounts(index),
    plt_event_id BIGINT NOT NULL REFERENCES plt_events(id),
    
    -- Token involved in this statement
    token_index BIGINT NOT NULL REFERENCES plt_tokens(index),
    
    -- Type of PLT operation
    entry_type plt_account_statement_entry_type NOT NULL,
    
    -- Amount involved in the operation (raw value from token_event JSON)
    amount NUMERIC NOT NULL,
    
    -- Token decimals at the time of operation
    decimals INTEGER NOT NULL,
    
    -- Normalized amount (amount / 10^decimals) for easy calculations
    normalized_amount NUMERIC NOT NULL,
    
    -- Denormalized timestamp for efficient querying
    slot_time TIMESTAMPTZ NOT NULL,
    
    -- Block height for reference
    block_height BIGINT NOT NULL REFERENCES blocks(height),
    
    -- Transaction that caused this statement
    transaction_index BIGINT NOT NULL REFERENCES transactions(index),
    
    -- Store the account balance after this operation (computed during backfill)
    account_balance NUMERIC NOT NULL DEFAULT 0,
    
    PRIMARY KEY (account_index, plt_event_id, entry_type)
);

-- Index for efficient queries by account and time
CREATE INDEX plt_accounts_statement_account_time_idx 
ON plt_accounts_statement(account_index, slot_time DESC);

-- Index for efficient queries by token
CREATE INDEX plt_accounts_statement_token_idx 
ON plt_accounts_statement(token_index, slot_time DESC);

-- Covering index for export queries (includes all needed columns)
CREATE INDEX plt_accounts_statement_export_idx 
ON plt_accounts_statement(account_index, token_index, slot_time DESC)
INCLUDE (entry_type, amount, decimals, normalized_amount, account_balance);

-- Backfill plt_accounts_statement from existing plt_events
-- Process ALL events in chronological order to maintain proper account balances
-- Using a single CTE to handle all event types with correct balance calculations

WITH all_plt_events AS (
    -- Union all PLT events with proper account extraction and amount calculations
    SELECT 
        pe.id AS plt_event_id,
        pe.token_index,
        a.index AS account_index,
        'Mint'::plt_account_statement_entry_type AS entry_type,
        (pe.token_event->'amount'->>'value')::NUMERIC AS raw_amount,
        (pe.token_event->'amount'->>'decimals')::INTEGER AS decimals,
        (pe.token_event->'amount'->>'value')::NUMERIC / POWER(10, (pe.token_event->'amount'->>'decimals')::INTEGER) AS normalized_amount,
        b.slot_time,
        b.height AS block_height,
        pe.transaction_index
    FROM plt_events pe
    JOIN transactions t ON pe.transaction_index = t.index
    JOIN blocks b ON t.block_height = b.height
    JOIN accounts a ON a.address = (pe.token_event->'target'->'address'->>'as_string')
    WHERE pe.event_type = 'Mint' AND pe.token_event->>'type' = 'Mint'
    
    UNION ALL
    
    -- Burn events (negative amounts)
    SELECT 
        pe.id AS plt_event_id,
        pe.token_index,
        a.index AS account_index,
        'Burn'::plt_account_statement_entry_type AS entry_type,
        -((pe.token_event->'amount'->>'value')::NUMERIC) AS raw_amount,
        (pe.token_event->'amount'->>'decimals')::INTEGER AS decimals,
        -((pe.token_event->'amount'->>'value')::NUMERIC / POWER(10, (pe.token_event->'amount'->>'decimals')::INTEGER)) AS normalized_amount,
        b.slot_time,
        b.height AS block_height,
        pe.transaction_index
    FROM plt_events pe
    JOIN transactions t ON pe.transaction_index = t.index
    JOIN blocks b ON t.block_height = b.height
    JOIN accounts a ON a.address = (pe.token_event->'target'->'address'->>'as_string')
    WHERE pe.event_type = 'Burn' AND pe.token_event->>'type' = 'Burn'
    
    UNION ALL
    
    -- Transfer events - sender (TransferOut, negative amounts)
    SELECT 
        pe.id AS plt_event_id,
        pe.token_index,
        sender.index AS account_index,
        'TransferOut'::plt_account_statement_entry_type AS entry_type,
        -((pe.token_event->'amount'->>'value')::NUMERIC) AS raw_amount,
        (pe.token_event->'amount'->>'decimals')::INTEGER AS decimals,
        -((pe.token_event->'amount'->>'value')::NUMERIC / POWER(10, (pe.token_event->'amount'->>'decimals')::INTEGER)) AS normalized_amount,
        b.slot_time,
        b.height AS block_height,
        pe.transaction_index
    FROM plt_events pe
    JOIN transactions t ON pe.transaction_index = t.index
    JOIN blocks b ON t.block_height = b.height
    JOIN accounts sender ON sender.address = (pe.token_event->'from'->'address'->>'as_string')
    WHERE pe.event_type = 'Transfer' AND pe.token_event->>'type' = 'Transfer'
    
    UNION ALL
    
    -- Transfer events - receiver (TransferIn, positive amounts)
    SELECT 
        pe.id AS plt_event_id,
        pe.token_index,
        receiver.index AS account_index,
        'TransferIn'::plt_account_statement_entry_type AS entry_type,
        (pe.token_event->'amount'->>'value')::NUMERIC AS raw_amount,
        (pe.token_event->'amount'->>'decimals')::INTEGER AS decimals,
        (pe.token_event->'amount'->>'value')::NUMERIC / POWER(10, (pe.token_event->'amount'->>'decimals')::INTEGER) AS normalized_amount,
        b.slot_time,
        b.height AS block_height,
        pe.transaction_index
    FROM plt_events pe
    JOIN transactions t ON pe.transaction_index = t.index
    JOIN blocks b ON t.block_height = b.height
    JOIN accounts receiver ON receiver.address = (pe.token_event->'to'->'address'->>'as_string')
    WHERE pe.event_type = 'Transfer' AND pe.token_event->>'type' = 'Transfer'
),
events_with_balance AS (
    -- Calculate running balance for each account+token combination using window function
    SELECT 
        *,
        SUM(raw_amount) OVER (
            PARTITION BY account_index, token_index 
            ORDER BY plt_event_id 
            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
        ) AS account_balance
    FROM all_plt_events
)
-- Insert all events with calculated balances
INSERT INTO plt_accounts_statement (
    account_index,
    plt_event_id,
    token_index,
    entry_type,
    amount,
    decimals,
    normalized_amount,
    slot_time,
    block_height,
    transaction_index,
    account_balance
)
SELECT 
    account_index,
    plt_event_id,
    token_index,
    entry_type,
    raw_amount AS amount,
    decimals,
    normalized_amount,
    slot_time,
    block_height,
    transaction_index,
    account_balance
FROM events_with_balance
ORDER BY plt_event_id;

-- Note: TokenModule events (AddAllowList, RemoveAllowList, etc.) are not included
-- as they don't affect account balances, only token permissions/settings