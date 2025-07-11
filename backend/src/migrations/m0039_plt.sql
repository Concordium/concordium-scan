-- Create TokenEvent table
CREATE TYPE event_type AS ENUM ('Mint', 'Burn', 'Transfer', 'TokenModule');

-- Create TokenModule Type
CREATE TYPE token_module_type AS ENUM (
    'AddAllowList',
    'RemoveAllowList',
    'AddDenyList',
    'RemoveDenyList'
);

-- Create Plt table
CREATE TABLE
    plt_tokens (
        index BIGINT PRIMARY KEY,
        token_id TEXT NOT NULL UNIQUE,
        transaction_index BIGINT NOT NULL REFERENCES transactions,
        name TEXT,
        decimal INT,
        -- Index to the associated account index in the `accounts` table.
        issuer_index BIGINT NOT NULL REFERENCES accounts,
        issuer TEXT NOT NULL,
        -- Module reference of the module.
        module_reference CHAR(64),
        -- Metadata Object
        -- This is a JSON object that can contain any metadata related to the token.
        metadata JSONB,
        initial_supply NUMERIC DEFAULT 0,
        total_minted NUMERIC DEFAULT 0,
        total_burned NUMERIC DEFAULT 0,
        total_holders INT DEFAULT 0
    );

-- plt event table
CREATE TABLE
    plt_events (
        id BIGINT PRIMARY KEY,
        -- Index (row in the `transaction` table) of the transaction with the token event.
        transaction_index BIGINT NOT NULL REFERENCES transactions,
        event_type event_type,
        token_module_type token_module_type,
        token_index BIGINT NOT NULL REFERENCES plt_tokens,
        target_index BIGINT  REFERENCES accounts,
        to_index BIGINT REFERENCES accounts,
        from_index BIGINT  REFERENCES accounts,
        -- The plt token event. Only `Mint`, `Burn`, `Transfer` and `TokenModule` events can occure in the field
        token_event JSONB NOT NULL
    );


CREATE TABLE plt_accounts (
    account_index BIGINT NOT NULL REFERENCES accounts,
    token_index BIGINT NOT NULL REFERENCES plt_tokens,
    amount NUMERIC DEFAULT 0,
    decimal INT DEFAULT 0,
    PRIMARY KEY (account_index, token_index)
);

