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
    -- Whether the block is finalized.
    finalized
        BOOLEAN
        NOT NULL,
    -- Index of the account which baked the block.
    -- For non-genesis blocks this should always be defined.
    -- Foreign key constraint added later, since account table is not defined yet.
    baker_id
        BIGINT
);

-- Every transaction on chain.
CREATE TABLE transactions(
    -- Index of the transaction within the block.
    index
        BIGINT
        NOT NULL,
    -- Absolute height of the block containing the transaction.
    block
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
    -- NULL if the transaction type is not account or the account transaction have no effect on chain.
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
    -- Transaction details. Events if success otherwise the reject reason.
    details
        JSONB,

    -- Make the block height and transaction index the primary key.
    PRIMARY KEY (block, index)
    -- transaction_type: TransactionType,
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
    -- The total balance of this account.
    amount
        BIGINT
        NOT NULL,
    -- Connect the account with the transaction creating it.
    FOREIGN KEY (created_block, created_index) REFERENCES transactions(block, index)
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

