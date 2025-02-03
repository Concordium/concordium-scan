CREATE EXTENSION IF NOT EXISTS pg_trgm;

CREATE TYPE account_transaction_type AS ENUM (
    'InitializeSmartContractInstance',
    'UpdateSmartContractInstance',
    'SimpleTransfer',
    'EncryptedTransfer',
    'SimpleTransferWithMemo',
    'EncryptedTransferWithMemo',
    'TransferWithScheduleWithMemo',
    'DeployModule',
    'AddBaker',
    'RemoveBaker',
    'UpdateBakerStake',
    'UpdateBakerRestakeEarnings',
    'UpdateBakerKeys',
    'UpdateCredentialKeys',
    'TransferToEncrypted',
    'TransferToPublic',
    'TransferWithSchedule',
    'UpdateCredentials',
    'RegisterData',
    'ConfigureBaker',
    'ConfigureDelegation'
);

CREATE TYPE credential_deployment_transaction_type AS ENUM (
    'Initial',
    'Normal'
);

CREATE TYPE update_transaction_type AS ENUM (
    'UpdateProtocol',
    'UpdateElectionDifficulty',
    'UpdateEuroPerEnergy',
    'UpdateMicroGtuPerEuro',
    'UpdateFoundationAccount',
    'UpdateMintDistribution',
    'UpdateTransactionFeeDistribution',
    'UpdateGasRewards',
    'UpdateBakerStakeThreshold',
    'UpdateAddAnonymityRevoker',
    'UpdateAddIdentityProvider',
    'UpdateRootKeys',
    'UpdateLevel1Keys',
    'UpdateLevel2Keys',
    'UpdatePoolParameters',
    'UpdateCooldownParameters',
    'UpdateTimeParameters',
    'MintDistributionCpv1Update',
    'GasRewardsCpv2Update',
    'TimeoutParametersUpdate',
    'MinBlockTimeUpdate',
    'BlockEnergyLimitUpdate',
    'FinalizationCommitteeParametersUpdate',
    'ValidatorScoreParametersUpdate'
);

CREATE TYPE transaction_type AS ENUM (
    'Account',
    'CredentialDeployment',
    'Update'
);

CREATE TYPE pool_open_status AS ENUM (
    'OpenForAll',
    'ClosedForNew',
    'ClosedForAll'
);

CREATE TYPE account_statement_entry_type AS ENUM (
    'TransferIn',
    'TransferOut',
    'AmountDecrypted',
    'AmountEncrypted',
    'TransactionFee',
    'FinalizationReward',
    'FoundationReward',
    'BakerReward',
    'TransactionFeeReward'
);

CREATE TYPE module_reference_contract_link_action AS ENUM (
    'Added',
    'Removed'
);

-- Chain and consensus parameters queried from a caught-up node. A check that the node is on the protocol
-- version 7 or above by the indexer before inserting these values is done. This ensures that the values are
-- queried from the most recent consensus algorithm.
CREATE TABLE current_chain_parameters(
    -- This field is always `true` and a primary key to constrain the table to have a single row.
    id BOOL PRIMARY KEY DEFAULT true CHECK (id),
    -- Duration of an epoch in milliseconds of the current consensus algorithm.
    -- E.g. This value is 1 hour for testnet in protocol version 7 or above.
    epoch_duration
        BIGINT
        NOT NULL,
    -- Number of epochs between reward payouts.
    -- E.g. This value is 24 for testnet in protocol version 7 or above. This means after 24 hours
    -- a new payday block is happening on testnet with reward payouts.
    reward_period_length
        BIGINT
        NOT NULL,
    -- This field is only NULL when no payday block has been observed yet which is the case at the beginning of indexing the chain.
    last_payday_block_height
        BIGINT
);

-- Every block on chain.
CREATE TABLE blocks(
    -- The absolute height of the block.
    height
        BIGINT
        PRIMARY KEY,
    -- Block hash encoded using HEX.
    hash
        CHAR(64)
        UNIQUE
        NOT NULL,
    -- Timestamp for when the block was baked.
    slot_time
        TIMESTAMPTZ
        NOT NULL,
    -- Milliseconds between the slot_time of this block and the block below (height - 1).
    -- For the genesis block it will be 0.
    block_time
        INTEGER
        NOT NULL,
    -- Milliseconds between the slot_time of this block and the first block above where this was
    -- recorded as finalized.
    -- This is NULL until the indexer has processed the block marking this block as finalized.
    finalization_time
        INTEGER,
    -- Block where this block was first recorded as finalized.
    -- This is NULL until the indexer has processed the block marking this block as finalized.
    finalized_by
        BIGINT
        REFERENCES blocks(height),
    -- Total finalization time in milliseconds for every block prior to this block including its own finalization
    -- time.
    -- This is NULL until the indexer has processed the block marking this block as finalized.
    cumulative_finalization_time
        BIGINT,
    -- Index of the account which baked the block.
    -- For non-genesis blocks this should always be defined.
    -- Foreign key constraint added later, since account table is not defined yet.
    baker_id
        BIGINT,
    -- The total amount of CCD in existence at the time of this block was created in micro CCD.
    total_amount
        BIGINT
        NOT NULL,
    -- The total staked amount of CCD at the time of this block was created in micro CCD.
    total_staked
        BIGINT
        NOT NULL,
    -- Number of transactions in all blocks up to and including this one.
    -- This is a denormalized value used to quickly calculate transaction counts
    -- without having to scan through the transactions table.
    cumulative_num_txs
        BIGINT
        NOT NULL
        CONSTRAINT cumulative_num_txs_non_negative CHECK (0 <= cumulative_num_txs)
);

-- Important for quickly filtering blocks by slot time, such as is done in the transactions metrics query.
CREATE INDEX blocks_slot_time_idx ON blocks (slot_time);
-- Used when updating the finalization time for a block to efficiently find blocks which are not yet
-- finalized during indexing.
CREATE INDEX blocks_height_null_fin_time ON blocks (height) WHERE finalization_time IS NULL;

-- Every transaction on chain.
CREATE TABLE transactions(
    -- Global index of the transaction.
    index
        BIGINT
        PRIMARY KEY,
    -- Absolute height of the block containing the transaction.
    block_height
        BIGINT
        NOT NULL
        REFERENCES blocks(height),
    -- Transaction hash encoded using HEX.
    hash
        CHAR(64)
        UNIQUE
        NOT NULL,
    -- The cost of the transaction in terms of CCD.
    ccd_cost
        BIGINT
        NOT NULL,
    -- The energy cost of the transaction.
    energy_cost
        BIGINT
        NOT NULL,
    -- The account index used for sending of the transaction.
    -- NULL for chain update and account creation transactions.
    -- Foreign key constraint added later, since account table is not defined yet.
    sender_index
        BIGINT,
    -- The type of transaction.
    type
        transaction_type
        NOT NULL,
    -- NULL if the transaction type is not an account transaction or an account transaction which
    -- got rejected due to deserialization failure.
    type_account
        account_transaction_type,
    -- NULL if the transaction type is not credential deployment.
    type_credential_deployment
        credential_deployment_transaction_type,
    -- NULL if the transaction type is not update.
    type_update
        update_transaction_type,
    -- Whether the transaction was accepted or rejected.
    success
        BOOLEAN
        NOT NULL,
    -- Transaction details. Events if success is true.
    events
        JSONB,
    -- Transaction details. Reject reason if success is false.
    reject
        JSONB
);

-- Important for quickly filtering transactions related to a baker_id.
CREATE INDEX baker_related_tx_idx ON transactions (sender_index, type_account, index) WHERE type_account IN ('AddBaker', 'RemoveBaker', 'UpdateBakerStake', 'UpdateBakerRestakeEarnings', 'UpdateBakerKeys', 'ConfigureBaker');

-- Important for quickly filtering transactions in a given block.
CREATE INDEX transactions_block_idx ON transactions (block_height, index);

-- Every account on chain.
CREATE TABLE accounts(
    -- Index of the account.
    index
        BIGINT
        PRIMARY KEY,
    -- Account address bytes encoded using base58check.
    address
        VARCHAR(50)
        UNIQUE
        NOT NULL,
    -- Index of the transaction creating this account.
    -- Only NULL for genesis accounts
    transaction_index
        BIGINT
        REFERENCES transactions,
    -- The total balance of this account in micro CCD.
    -- TODO: Actually populate this in the indexer.
    amount
        BIGINT
        NOT NULL
        DEFAULT 0,
    -- The total number of transactions this account has been involved in or affected by.
    -- This is a denormalized value that should correspond to a count over the affected_accounts table,
    -- but we don't want to scan that table every time to calculate this.
    num_txs
        BIGINT
        NOT NULL
        -- Starting at 1 to count the transaction that made the account.
        DEFAULT 1,
    -- The total delegated stake of this account in micro CCD.
    -- An account can delegate stake to at most one baker pool.
    delegated_stake
        BIGINT
        NOT NULL
        DEFAULT 0,
    -- Whether we are re-staking earnings. Null means we are not using delegation.
    delegated_restake_earnings
        BOOLEAN
        NULL,
    -- Target id of the baker When this is null it means that we are using passive delegation.
    -- An account can delegate stake to at most one baker pool.
    delegated_target_baker_id
        BIGINT
        NULL
);

-- These are important for the sorting options on the accounts query.
CREATE INDEX accounts_amount_idx ON accounts (amount);
CREATE INDEX accounts_delegated_stake_idx ON accounts (delegated_stake);
CREATE INDEX accounts_num_txs_idx ON accounts (num_txs);
CREATE INDEX accounts_address_trgm_idx ON accounts USING gin (address gin_trgm_ops);

-- Add foreign key constraint now that the account table is created.
ALTER TABLE transactions
    ADD CONSTRAINT fk_transaction_sender
    FOREIGN KEY (sender_index)
    REFERENCES accounts;

-- Add foreign key constraint now that the account table is created.
ALTER TABLE blocks
    ADD CONSTRAINT fk_block_baker_id
    FOREIGN KEY (baker_id)
    REFERENCES accounts;

-- All the accounts that are affected by transactions are logged in this table.
-- This is used to decide which transactions to display under an account.
CREATE TABLE affected_accounts (
    -- The transaction in question.
    transaction_index
        BIGINT
        NOT NULL
        REFERENCES transactions,
    -- An account affected by this transaction.
    account_index
        BIGINT
        NOT NULL
        REFERENCES accounts,

    -- A transaction can only affect an account once.
    PRIMARY KEY (transaction_index, account_index)
);

-- Current active bakers
CREATE TABLE bakers(
    -- Baker/validator ID, corresponding to the account index.
    id
        BIGINT
        PRIMARY KEY
        REFERENCES accounts,
    -- Amount staked at present in this baker pool by the baker (no delegated stake included).
    staked
        BIGINT
        NOT NULL,
    -- Flag indicating whether rewards paid to the baker are automatically restaked.
    restake_earnings
        BOOLEAN
        NOT NULL,
    -- Delegation open status of this pool.
    -- This was introduced as part of P4.
    open_status
        pool_open_status,
    -- URL for pool metadata.
    -- This was introduced as part of P4.
    metadata_url
        VARCHAR(2048), -- The official max length is 2048 bytes, this however is a limit in characters.
    -- Fraction of transaction rewards charged by the pool owner.
    -- Stored as a fraction of an amount with a precision of `1/100_000`.
    transaction_commission
        BIGINT,
    -- Fraction of baking rewards charged by the pool owner.
    -- Stored as a fraction of an amount with a precision of `1/100_000`.
    baking_commission
        BIGINT,
    -- Fraction of finalization rewards charged by the pool owner.
    -- Stored as a fraction of an amount with a precision of `1/100_000`.
    finalization_commission
        BIGINT,
    -- Transaction used for self-suspending.
    -- This is not null only when a baker is suspended due to sending the transaction for
    -- self-suspending.
    -- This should always be null, when `inactive_suspended` column is not null.
    self_suspended
        BIGINT
        REFERENCES transactions,
    -- Block which suspended this baker due to inactivity.
    -- This is not null only when a baker is suspended due to inactivity.
    -- This should always be null, when `self_suspended` or `primed_for_suspension` column is not
    -- null.
    inactive_suspended
        BIGINT
        REFERENCES blocks,
    -- Block which is primed for suspension this baker due to inactivity.
    -- This is not null only when a baker got primed for suspension due to inactivity.
    -- This should always be null, `inactive_suspended` column is not null.
    primed_for_suspension
        BIGINT
        REFERENCES blocks
);

-- This index allows for efficiently updating bakers currently primed for suspension.
CREATE INDEX bakers_primed_for_suspension_index ON bakers (id) WHERE primed_for_suspension IS NOT NULL;

-- Every module on chain.
CREATE TABLE smart_contract_modules(
    -- Module reference of the module.
    module_reference
        CHAR(64)
        UNIQUE
        PRIMARY KEY,
    -- Index of the transaction deploying the module.
    transaction_index
        BIGINT
        NOT NULL
        REFERENCES transactions,
    -- Embedded schema in the module if present.
    schema BYTEA
);

-- Indexing of rejected transactions for a deployed smart contract module, such as redeploying a
-- module or a failed initialization.
CREATE TABLE rejected_smart_contract_module_transactions (
    -- Gapless incrementing index for each module reference, used for efficiently skipping in the
    -- query for this collection.
    index
        BIGINT
        NOT NULL,
    -- The transaction in question.
    transaction_index
        BIGINT
        NOT NULL
        REFERENCES transactions,
    -- A smart contract module affected by this transaction.
    module_reference
        CHAR(64)
        NOT NULL
        REFERENCES smart_contract_modules,
    PRIMARY KEY (module_reference, index)
);

-- Every contract instance on chain.
CREATE TABLE contracts(
    -- Index of the contract.
    index
        BIGINT
        NOT NULL,
    -- Sub index of the contract.
    sub_index
        BIGINT
        NOT NULL,
    -- Note: It might be better to use `module_reference_index` which would save storage space but would require more work in inserting/querying by the indexer.
    -- Module reference of the module.
    module_reference
        CHAR(64)
        NOT NULL,
    -- The contract name.
    name
        VARCHAR(100)
        NOT NULL,
    -- The total balance of the contract in micro CCD.
    amount
        BIGINT
        NOT NULL,
    -- The index of the transaction initializing the contract.
    transaction_index
        BIGINT
        NOT NULL
        REFERENCES transactions,
    -- The index of the most recent transaction which upgraded this contract.
    -- Is NULL for contracts which have never upgraded.
    last_upgrade_transaction_index
        BIGINT
        NULL
        REFERENCES transactions,
    -- Make the contract index and subindex the primary key.
    PRIMARY KEY (index, sub_index)
);

-- This index allows for efficiently listing every contract currently linked to a specific smart
-- contract module.
CREATE INDEX contracts_module_reference_index ON contracts (module_reference);

-- Every successful event associated to a contract.
CREATE TABLE contract_events (
    -- An index/id for this event (row number).
    index
        BIGINT GENERATED ALWAYS AS IDENTITY
        PRIMARY KEY,
    -- Transaction index including the event.
    transaction_index
        BIGINT
        NOT NULL,
    -- Trace element index of the event traces from above transaction.
    trace_element_index
        BIGINT
        NOT NULL,
    -- The absolute block height of the block that includes the event.
    block_height
        BIGINT
        NOT NULL,
    -- Contract index that the event is associated with.
    contract_index
        BIGINT
        NOT NULL,
    -- Contract subindex that the event is associated with.
    contract_sub_index
        BIGINT
        NOT NULL,
    -- Every time an event is associated with a contract, this index is incremented for that contract.
    -- This value is used to quickly filter/sort events by the order they were emitted by a contract.
    event_index_per_contract
        BIGINT
        NOT NULL
);

-- Important for quickly filtering/sorting events by the order they were emitted by a contract.
CREATE INDEX event_index_per_contract_idx ON contract_events (contract_index, contract_sub_index, event_index_per_contract);

-- Table indexing the rejected update transactions for each contract instance, tracking an incrementing
-- index allowing for efficient offset pagination.
CREATE TABLE contract_reject_transactions (
    -- Index of the contract rejecting the transaction.
    contract_index
        BIGINT
        NOT NULL,
    -- Sub index of the contract rejecting the transaction.
    contract_sub_index
        BIGINT
        NOT NULL,
    -- Every time a new transactions is rejected by a contract, this index is incremented for that contract.
    -- This value is used to quickly filter/sort transactions by the order they were rejected by a contract.
    transaction_index_per_contract
        BIGINT
        NOT NULL,
    -- Transaction index including the event.
    transaction_index
        BIGINT
        NOT NULL,
    PRIMARY KEY (contract_index, contract_sub_index, transaction_index_per_contract)
);

-- Indexing of transactions linking smart contract modules to a smart contract instance.
-- Such as init contract or contract upgrades.
CREATE TABLE link_smart_contract_module_transactions (
    -- Gapless incrementing index for each module reference, used for efficiently skipping in the
    -- query for this collection.
    index
        BIGINT
        NOT NULL,
    -- The transaction in question.
    transaction_index
        BIGINT
        NOT NULL
        REFERENCES transactions,
    -- A smart contract module affected by this transaction.
    module_reference
        CHAR(64)
        NOT NULL
        REFERENCES smart_contract_modules,
    -- Contract index that the event is associated with.
    contract_index
        BIGINT
        NOT NULL,
    -- Contract subindex that the event is associated with.
    contract_sub_index
        BIGINT
        NOT NULL,
    -- Whether the relevant smart contract instance is linking or unlinking from the module
    -- reference.
    link_action
        module_reference_contract_link_action
        NOT NULL,
    PRIMARY KEY (module_reference, index),
    FOREIGN KEY (contract_index, contract_sub_index) REFERENCES contracts(index, sub_index)
);

-- Every scheduled release on chain.
CREATE TABLE scheduled_releases (
    -- An index/id for this scheduled release (row number).
    index
        BIGINT GENERATED ALWAYS AS IDENTITY
        PRIMARY KEY,
    -- The index of the transaction creating the scheduled transfer.
    transaction_index
        BIGINT
        NOT NULL
        REFERENCES transactions,
    -- The account receiving the scheduled transfer.
    account_index
        BIGINT
        NOT NULL
        REFERENCES accounts,
    -- The scheduled release time.
    release_time
        TIMESTAMPTZ
        NOT NULL,
    -- The amount locked in the scheduled release.
    amount
        BIGINT
        NOT NULL
);

-- We typically want to find all scheduled releases for a specific account after a specific time.
-- This index is useful for that.
CREATE INDEX scheduled_releases_idx ON scheduled_releases (account_index, release_time);

-- All CIS2 tokens. A token is added to this table whenever a CIS2 `MintEvent`, `BurnEvent`
-- or `TokenMetadataEvent` is logged for the first time by a contract claiming
-- to follow the `CIS2 standard`.
CREATE TABLE tokens (
    -- An index/id for the token (row number).
    index
        BIGINT
        PRIMARY KEY,
    -- Every time a token is associated with a contract, this index is incremented for that contract.
    -- This value is used to quickly filter/sort tokens by the order they were created by a contract.
    token_index_per_contract
        BIGINT
        NOT NULL,
    -- Contract index that the token is associated with.
    contract_index
        BIGINT
        NOT NULL,
    -- Contract subindex that the token is associated with.
    contract_sub_index
        BIGINT
        NOT NULL,
    -- Unique token address to identify tokens across all smart contracts.
    -- The token address is generated by using a `version byte 2` and concatenating it
    -- with the leb128 byte encoding of the contract index and the leb128 byte
    -- encoding of the contract subindex concatenated with the token id in bytes.
    -- Finally the whole byte array is base 58 check encoded.
    -- https://proposals.concordium.software/CIS/cis-2.html#token-address
    token_address
        TEXT
        UNIQUE
        NOT NULL,
    -- Metadata url (this value only stores the url string and not the hash from the `MetadataUrl` type
    -- https://docs.rs/concordium-rust-sdk/latest/concordium_rust_sdk/cis2/struct.MetadataUrl.html).
    -- The value is `NULL` from the first `Mint/Burn` event until the first `TokenMetadata` event is
    -- observed for a given token.
    metadata_url
        TEXT,
    -- Token id of the token.
    token_id
        TEXT
        NOT NULL,
    -- Accumulated total supply of the token calculated by considering all `MintEvents` and `BurnEvents` associated
    -- to the token. If no total supply is specified when inserting a new token in the table,
    -- the default total supply 0 is used.
    total_supply
        NUMERIC
        NOT NULL
        DEFAULT 0,
    -- Index of the transaction with the first CIS2 `MintEvent`, `BurnEvent` or `TokenMetadataEvent` logged for the token.
    init_transaction_index
       BIGINT
       NOT NULL
);

-- We want to find a specific token (this index should be removed once the front-end queries a token by `token_address`).
CREATE INDEX token_idx ON tokens (contract_index, contract_sub_index, token_id);

-- Important for quickly filtering/sorting tokens by the order they were created by a contract.
CREATE INDEX token_index_per_contract_idx ON tokens (contract_index, contract_sub_index, token_index_per_contract);

-- This sequence is used to sort/filter the newest tokens transferred to an account address.
CREATE SEQUENCE account_tokens_update_seq;

-- Relations between accounts and CIS2 tokens. Rows are added or updated in this table whenever a CIS2 `MintEvent`, `BurnEvent`
-- or `TransferEvent` is logged by a contract claiming to follow the `CIS2 standard`.
CREATE TABLE account_tokens (
    -- An index/id for the row (row number).
    index
        BIGINT
        PRIMARY KEY,
  -- An account index (row in the `accounts` table) that owns a token balance.
    account_index
        BIGINT
        NOT NULL,
    -- The token index (row in the `tokens` table) of the associated token.
    token_index
        BIGINT
        NOT NULL,
    -- The accumulated balance of the token by the above account calculated by considering all `MintEvents`, `BurnEvents` and
    -- `TransferEvents` associated to the token and account. If no balance is specified when inserting a new row in the table,
    -- the default balance 0 is used.
    balance
        NUMERIC
        NOT NULL
        DEFAULT 0,
    -- Every time an `account_token` is inserted or updated in this table, a sequential index is assigned
    -- to the operation and tracked in this sequence.
    -- This sequence is used to sort/filter the newest tokens transferred to an account address.
    change_seq BIGINT DEFAULT nextval('account_tokens_update_seq'),

    -- Ensure that each token_index and account_index pair is unique.
    CONSTRAINT unique_token_account_relationship UNIQUE (token_index, account_index)
);

CREATE INDEX non_zero_account_token_idx ON account_tokens (account_index, change_seq) WHERE balance != 0;

-- Table to collect CIS2 token events (`Mint`, `Burn`, `Transfer` and `TokenMetadata` events).
CREATE TABLE cis2_token_events (
    -- An index/id for the event (row number).
    index
        BIGINT GENERATED ALWAYS AS IDENTITY
        PRIMARY KEY,
    -- Every time an event is associated with a token, this index is incremented for that token.
    -- This value is used to quickly filter/sort events associated with a given token.
    index_per_token
        BIGINT
        NOT NULL,
    -- Index (row in the `transaction` table) of the transaction with the token event.
    transaction_index
        BIGINT
        NOT NULL,
    -- The token index (row in the `tokens` table) of the associated token.
    token_index
        BIGINT
        NOT NULL,
    -- The cis2 token event. Only `Mint`, `Burn`, `Transfer` and `TokenMetadata` events can occure in the field
    -- (no `UpdateOperator` event because the event cannot be linked to a specific token).
    cis2_token_event
        JSONB
        NOT NULL
);

CREATE INDEX cis2_token_events_idx ON cis2_token_events (token_index, index_per_token);

CREATE OR REPLACE FUNCTION block_added_notify_trigger_function() RETURNS trigger AS $trigger$
DECLARE
  rec blocks;
  payload TEXT;
BEGIN
  CASE TG_OP
       WHEN 'INSERT' THEN
            payload := NEW.height;
            PERFORM pg_notify('block_added', payload);
       ELSE NULL;
  END CASE;
  RETURN NEW;
END;
$trigger$ LANGUAGE plpgsql;

CREATE TRIGGER block_added_notify_trigger AFTER INSERT
ON blocks
FOR EACH ROW EXECUTE PROCEDURE block_added_notify_trigger_function();

CREATE OR REPLACE FUNCTION account_updated_notify_trigger_function() RETURNS trigger AS $trigger$
DECLARE
  rec affected_accounts;
  lookup_result TEXT;
BEGIN
  CASE TG_OP
       WHEN 'INSERT' THEN
            -- Lookup the account address associated with the account index.
            SELECT address
            INTO lookup_result
            FROM accounts
            WHERE index = NEW.account_index;
            -- Include the lookup result in the payload
            PERFORM pg_notify('account_updated', lookup_result);
       ELSE NULL;
  END CASE;
  RETURN NEW;
END;
$trigger$ LANGUAGE plpgsql;

CREATE TRIGGER account_updated_notify_trigger AFTER INSERT
ON affected_accounts
FOR EACH ROW EXECUTE PROCEDURE account_updated_notify_trigger_function();

-- Function to update the change_seq column with the next sequence value
CREATE OR REPLACE FUNCTION update_change_seq()
RETURNS trigger AS $trigger$
BEGIN
    -- Fetch the next value from the sequence and assign it to change_seq
    NEW.change_seq := nextval('account_tokens_update_seq');
    RETURN NEW;
END;
$trigger$ LANGUAGE plpgsql;

-- Create a trigger for INSERT and UPDATE events
CREATE TRIGGER set_change_seq BEFORE INSERT OR UPDATE ON account_tokens
FOR EACH ROW EXECUTE PROCEDURE update_change_seq();

-- Table for logging all account-related activities on-chain.
-- This table tracks individual entries related to changes in account balances.
CREATE TABLE account_statements (
    -- Unique identifier for each account statement entry.
    id
        BIGINT
        GENERATED ALWAYS AS IDENTITY
        PRIMARY KEY,
    -- Index of the account associated with this entry.
    account_index
        BIGINT
        NOT NULL
        REFERENCES accounts(index),
    -- Type of the account statement entry.
    entry_type
        account_statement_entry_type
        NOT NULL,
    -- Amount associated with the entry in micro CCD.
    -- This represents the change in balance caused by the transaction or activity.
    -- Will be negative when an amount is being subtracted from the account.
    amount
        BIGINT
        NOT NULL,
    -- The resulting balance of the account after applying this entry.
    -- This is used to track the account's total CCD at the time of the transaction.
    account_balance
        BIGINT
        NOT NULL,
    -- Block height at which the entry occurred.
    -- Links to the blocks table to associate the entry with a specific block.
    block_height
        BIGINT
        NOT NULL
        REFERENCES blocks(height),
    -- Used as reference for all account statements not of type reward type
    transaction_id
        BIGINT
        NULL
);

CREATE INDEX account_statements_entry_type_idx ON account_statements (id, account_index, entry_type);

-- Type of a special transaction outcome in a block.
CREATE TYPE special_transaction_outcome_type AS ENUM (
    'BakingRewards',
    'Mint',
    'FinalizationRewards',
    'BlockRewards',
    'PaydayFoundationReward',
    'PaydayAccountReward',
    'BlockAccrueReward',
    'PaydayPoolReward',
    'ValidatorSuspended',
    'ValidatorPrimedForSuspension'
);

-- Table indexing special transaction outcomes in blocks.
CREATE TABLE block_special_transaction_outcomes (
    -- Height of the block containing the special transaction outcome.
    block_height
        BIGINT
        NOT NULL
        REFERENCES blocks,
    -- Index of the outcome within the special transaction outcomes of a block.
    block_outcome_index
        BIGINT
        NOT NULL,
    -- The type of the special transaction outcome.
    outcome_type
        special_transaction_outcome_type
        NOT NULL,
    -- The special transaction outcome stored as JSON.
    outcome
        JSONB
        NOT NULL
);

-- Index allowing for efficiently finding particular type of outcomes for a particular block.
CREATE INDEX block_special_transaction_outcomes_idx
    ON block_special_transaction_outcomes (block_height, outcome_type, block_outcome_index);

-- Function for generating a table where each row is a bucket.
-- Used by metrics queries.
CREATE OR REPLACE FUNCTION date_bin_series(bucket_size interval, starting TIMESTAMPTZ, ending TIMESTAMPTZ)
RETURNS TABLE(bucket_start TIMESTAMPTZ, bucket_end TIMESTAMPTZ) AS $$
    SELECT
        bucket_start,
        bucket_start + bucket_size
    FROM generate_series(
        date_bin(bucket_size, starting, TIMESTAMPTZ '2001-01-01'),
        date_bin(bucket_size, ending + bucket_size, TIMESTAMPTZ '2001-01-01'),
        bucket_size
    ) as bucket_start;
$$ LANGUAGE sql;

