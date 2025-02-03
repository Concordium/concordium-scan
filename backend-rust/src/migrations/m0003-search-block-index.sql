-- Used to efficiently perform partial string matching on the `hash` column,
-- allowing fast lookups when searching for blocks by their hash prefix using `LIKE`.
CREATE INDEX blocks_hash_gin_trgm_idx ON blocks USING gin(hash gin_trgm_ops);