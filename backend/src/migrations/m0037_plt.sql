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
        token_id TEXT PRIMARY KEY,
        name TEXT,
        decimal INT,
        -- Index to the associated account index in the `accounts` table.
        issuer_index BIGINT NOT NULL REFERENCES accounts,
        -- Module reference of the module.
        module_reference CHAR(64),
        -- Metadata url (this value only stores the url string and not the hash from the `MetadataUrl` type
        -- https://docs.rs/concordium-rust-sdk/latest/concordium_rust_sdk/cis2/struct.MetadataUrl.html).
        metadata_url TEXT,
        -- Maybe we want to track moduleState or maybe not
        module_state TEXT,
        total_supply NUMERIC DEFAULT 0,
        total_minted NUMERIC DEFAULT 0,
        total_burned NUMERIC DEFAULT 0,
        total_holders INT DEFAULT 0,
        last_updated TIMESTAMPTZ DEFAULT now ()
    );

-- plt event table
CREATE TABLE
    plt_events (
        id SERIAL PRIMARY KEY,
        -- Index (row in the `transaction` table) of the transaction with the token event.
        transaction_index BIGINT NOT NULL,
        event_type event_type NOT NULL,
        token_module_type token_module_type,
        token_id TEXT, --  REFERENCES plt_tokens(token_id)
        -- The plt token event. Only `Mint`, `Burn`, `Transfer` and `TokenModule` events can occure in the field
        token_event JSONB NOT NULL
    );