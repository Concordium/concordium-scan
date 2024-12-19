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
    'FinalizationCommitteeParametersUpdate'
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

CREATE TYPE module_reference_contract_link_action AS ENUM (
    'Added',
    'Removed'
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
    -- This is NULL until the indexer have processed the block marking this a finalized.
    finalization_time
        INTEGER,
    -- Block where this block was first recorded as finalized.
    -- This is NULL until the indexer have processed the block marking this a finalized.
    finalized_by
        BIGINT
        REFERENCES blocks(height),
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

-- Every transaction on chain.
CREATE TABLE transactions(
    -- Global index of the transaction.
    index
        BIGINT
        PRIMARY KEY,
    -- Absolute height of the block containing the transaction.
    block_height
        BIGINT
        REFERENCES blocks(height)
        NOT NULL,
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
    -- The account used for sending of the transaction.
    -- NULL for chain update and account creation transactions.
    -- Foreign key constraint added later, since account table is not defined yet.
    sender
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

-- Every account on chain.
CREATE TABLE accounts(
    -- Index of the account.
    index
        BIGINT
        PRIMARY KEY,
    -- Account address bytes encoded using base58check.
    address
        CHAR(50)
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
    delegated_stake
        BIGINT
        NOT NULL
        DEFAULT 0,
    -- Whether we are re-staking earnings. Null means we are not using delegation.
    delegated_restake_earnings
        BOOLEAN
        NULL,
    -- Target id of the baker When this is null it means that we are using passive delegation.
    delegated_target_baker_id
        BIGINT
        NULL
);

-- These are important for the sorting options on the accounts query.
CREATE INDEX accounts_amount_idx ON accounts (amount);
CREATE INDEX accounts_delegated_stake_idx ON accounts (delegated_stake);
CREATE INDEX accounts_num_txs_idx ON accounts (num_txs);

-- Add foreign key constraint now that the account table is created.
ALTER TABLE transactions
    ADD CONSTRAINT fk_transaction_sender
    FOREIGN KEY (sender)
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
    -- Amount staked at present.
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
        BIGINT
);

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

    -- Make the contract index and subindex the primary key.
    PRIMARY KEY (index, sub_index)
);

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
CREATE INDEX event_index_per_contract_idx ON contract_events (event_index_per_contract);

-- Important for quickly filtering contract events by a specific contract.
CREATE INDEX contract_events_idx ON contract_events (contract_index, contract_sub_index);

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

-- All CIS2 tokens. A token is added to this table whenever a `CIS2 mint event` is logged for the first
-- time by a contract claiming to follow the `CIS2 standard` or a `CIS2 tokenMetadataUpdated event` is logged
-- for the first time by a contract claiming to follow the `CIS2 standard`.
CREATE TABLE tokens (
    -- An index/id for the token (row number).
    index
        BIGINT GENERATED ALWAYS AS IDENTITY
        PRIMARY KEY,
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
    -- Metadata url type serialized as bytes. The value is `NULL` from the first `Mint/Burn` event until the first `TokenMetadata` event is observed for a given token.
    metadata_url
        TEXT,
    -- Accumulated total supply of the token calculated by considering all `mint/burn` events associated
    -- to the token. If no total supply is specified when inserting a new token in the table,
    -- the default total supply 0 is used.
    total_supply
        NUMERIC
        NOT NULL
        DEFAULT 0,
    -- Index of the transaction with the first `CIS2 mint event`, `CIS2 burn event` or `CIS2 tokenMetadata event` logged for the token.
    init_transaction_index
       BIGINT
       NOT NULL
);

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
