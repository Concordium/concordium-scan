-- The primary key has been changed to have block_slot_time as the leading column
-- because all queries on the metrics_rewards table reduce sums based on block_slot_time.
ALTER TABLE metrics_rewards
DROP CONSTRAINT metrics_rewards_pkey;

ALTER TABLE metrics_rewards
ADD CONSTRAINT metrics_rewards_pkey
PRIMARY KEY (block_slot_time, account_index);
