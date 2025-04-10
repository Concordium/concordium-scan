ALTER TABLE metrics_rewards
DROP CONSTRAINT metrics_rewards_pkey;

ALTER TABLE metrics_rewards
ADD CONSTRAINT metrics_rewards_pkey
PRIMARY KEY (block_slot_time, account_index);