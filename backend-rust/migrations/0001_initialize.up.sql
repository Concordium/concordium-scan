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

-- Every block on chain.
CREATE TABLE blocks(
    -- The absolute height of the block.
    height
        BIGINT
        PRIMARY KEY
        NOT NULL,
    -- Block hash encoded using HEX.
    hash
        CHAR(64)
        UNIQUE
        NOT NULL,
    -- Timestamp for when the block was baked.
    slot_time
        TIMESTAMP
        NOT NULL,
    -- Milliseconds between the slot_time of this block and the block below (height - 1).
    -- For the genesis block it will be 0.
    block_time
        INTEGER
        NOT NULL,
    -- Milliseconds between the slot_time of this block and the block above causing this block to be finalized.
    -- This is NULL until the indexer have processed the block marking this a finalized.
    finalization_time
        INTEGER,
    -- Block causing this block to become finalized.
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
        NOT NULL
);

-- Every transaction on chain.
CREATE TABLE transactions(
    -- Index of the transaction within the block.
    index
        BIGINT
        NOT NULL,
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
        JSONB,

    -- Within a single block, two transactions cannot share the same index.
    PRIMARY KEY (block_height, index)
);

-- Every account on chain.
CREATE TABLE accounts(
    -- Index of the account.
    index
        BIGINT
        PRIMARY KEY
        NOT NULL,
    -- Account address bytes encoded using base58check.
    address
        CHAR(50)
        UNIQUE
        NOT NULL,
    -- Block height where this account was created.
    created_block
        BIGINT
        NOT NULL,
    -- Index of the transaction in the block creating this account.
    -- Only NULL for genesis accounts
    created_index
        BIGINT,
    -- The total balance of this account in micro CCD.
    amount
        BIGINT
        NOT NULL,
    -- Connect the account with the transaction creating it.
    FOREIGN KEY (created_block, created_index) REFERENCES transactions (block_height, index)
    -- credential_registration_id
);

-- Add foreign key constraint now that the account table is created.
ALTER TABLE transactions
    ADD CONSTRAINT fk_transaction_sender
    FOREIGN KEY (sender)
    REFERENCES accounts(index);

-- Add foreign key constraint now that the account table is created.
ALTER TABLE blocks
    ADD CONSTRAINT fk_block_baker_id
    FOREIGN KEY (baker_id)
    REFERENCES accounts(index);

-- Current active bakers
CREATE TABLE bakers(
    -- Baker/validator ID, corresponding to the account index.
    id
        BIGINT
        PRIMARY KEY
        NOT NULL
        REFERENCES accounts(index),
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
