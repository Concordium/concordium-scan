CREATE TABLE payday_baker_pool_stakes(
    payday_block BIGINT NOT NULL REFERENCES blocks,
    baker BIGINT NOT NULL REFERENCES accounts,
    baker_stake BIGINT NOT NULL,
    delegators_stake BIGINT NOT NULL,
    PRIMARY KEY(payday_block, baker)
);

CREATE TABLE payday_passive_pool_stakes(
    payday_block BIGINT PRIMARY KEY REFERENCES blocks,
    delegators_stake BIGINT NOT NULL
);
