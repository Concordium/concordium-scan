-- Gin index is too slow as compared to text pattern ops
DROP INDEX blocks_hash_gin_trgm_idx;
CREATE INDEX blocks_hash_idx ON blocks USING btree (hash text_pattern_ops);

DROP INDEX accounts_address_trgm_idx;
CREATE INDEX accounts_address_idx ON accounts USING btree (address text_pattern_ops);

CREATE INDEX tokens_token_address_idx ON tokens USING btree (token_address text_pattern_ops);
