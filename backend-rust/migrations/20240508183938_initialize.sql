CREATE TABLE blocks(
    height    BIGINT    PRIMARY KEY NOT NULL,
    hash      CHAR(64)  UNIQUE      NOT NULL,
    slot_time TIMESTAMP             NOT NULL,
    finalized BOOLEAN               NOT NULL,
    baker_id  BIGINT                -- For non-genesis blocks this should always be defined
);

CREATE TABLE transactions(
    index       BIGINT               NOT NULL,
    block       BIGINT   REFERENCES blocks(height) NOT NULL,
    hash        CHAR(64) UNIQUE NOT NULL,
    ccd_cost    BIGINT               NOT NULL,
    energy_cost BIGINT               NOT NULL,
    -- transaction_type: TransactionType,
    sender      BIGINT, -- NULL for chain update and account creation transactions. Reference added later.
    PRIMARY KEY (block, index)
);

CREATE TABLE accounts(
    index         BIGINT   PRIMARY KEY NOT NULL,
    address       CHAR(50) UNIQUE NOT NULL,
    created_block BIGINT   NOT NULL,
    created_index BIGINT   NOT NULL,
    -- credential_registration_id
    FOREIGN KEY (created_block, created_index) REFERENCES transactions(block, index)
);

ALTER TABLE transactions
    ADD CONSTRAINT fk_transaction_sender
    FOREIGN KEY (sender)
    REFERENCES accounts(index);

