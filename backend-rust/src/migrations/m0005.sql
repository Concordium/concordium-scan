-- Used to efficiently perform partial string matching on the hash column,
-- allowing fast lookups when searching for transactions by their hash prefix using LIKE.
CREATE INDEX transactions_hash_idx ON transactions USING gin (hash text_pattern_ops);
