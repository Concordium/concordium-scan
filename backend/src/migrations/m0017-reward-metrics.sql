CREATE TABLE metrics_rewards (
    block_height        BIGINT NOT NULL REFERENCES blocks,
    block_slot_time     TIMESTAMPTZ NOT NULL,
    account_index       BIGINT NOT NULL REFERENCES accounts,
    amount              BIGINT NOT NULL,
    PRIMARY KEY (account_index, block_slot_time)
);

INSERT INTO metrics_rewards
WITH per_block AS (
  SELECT
    block_height,
    account_index,
    (SELECT slot_time FROM blocks WHERE height = block_height) AS block_slot_time,
    SUM(amount) AS rewards
  FROM account_statements
  WHERE entry_type IN (
    'FinalizationReward',
    'FoundationReward',
    'BakerReward',
    'TransactionFeeReward'
  )
  GROUP BY block_height, account_index
)
SELECT
  block_height,
  block_slot_time,
  account_index,
  rewards
FROM per_block;
