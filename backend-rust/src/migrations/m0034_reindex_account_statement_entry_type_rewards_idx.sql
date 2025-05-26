-- Replace existing index with two separate indexes
DROP INDEX IF EXISTS account_statements_entry_type_idx;

-- Efficient lookup of account_statements for a specific account.
CREATE INDEX account_statements_idx
  ON account_statements (account_index, id);

-- Efficient lookup of rewards from account_statements for a specific account.
CREATE INDEX account_statements_entry_type_rewards_idx
  ON account_statements (account_index, id, entry_type)
  -- Range covers entry types related to rewards: 'FinalizationReward', 'FoundationReward', 'BakerReward', 'TransactionFeeReward'
  WHERE entry_type BETWEEN 'FinalizationReward' AND 'TransactionFeeReward';
