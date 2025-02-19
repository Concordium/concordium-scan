-- Initial setup of canonical address with placeholder data
ALTER TABLE accounts
ADD COLUMN canonical_address BYTEA

-- Used to efficiently perform partial string matching on the hash column,
-- allowing fast lookups when searching for transactions by their hash prefix using LIKE.
ALTER TABLE transactions ALTER COLUMN hash SET DATA TYPE VARCHAR(64);
CREATE INDEX transactions_hash_idx ON transactions USING btree (hash text_pattern_ops);
