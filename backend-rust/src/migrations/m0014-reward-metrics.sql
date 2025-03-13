CREATE TABLE metrics_rewards
(
    block_height            BIGINT            PRIMARY KEY REFERENCES blocks(height),
    account_id              BIGINT    NOT NULL,
    accumulated_amount      BIGINT    NOT NULL
);

CREATE INDEX ON metrics_rewards (account_id, block_height);
