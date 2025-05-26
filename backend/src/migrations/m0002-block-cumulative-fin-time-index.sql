-- Used when updating the cumulative finalization time, by efficiently finding blocks which have
-- finalization_time, but no cumulative_finalization_time.
CREATE INDEX blocks_height_null_cumulative_finalization_time ON blocks (height)
    WHERE blocks.cumulative_finalization_time IS NULL
    AND blocks.finalization_time IS NOT NULL;
-- blocks_hash_gin_trgm_idx index does not support char
ALTER TABLE blocks ALTER COLUMN hash SET DATA TYPE VARCHAR(64);
-- Used to efficiently perform partial string matching on the `hash` column,
-- allowing fast lookups when searching for blocks by their hash prefix using `LIKE`.
CREATE INDEX blocks_hash_gin_trgm_idx ON blocks USING gin(hash gin_trgm_ops);
-- Important for quickly calculating the delegated stake to a baker pool.
CREATE INDEX delegated_target_baker_id_index ON accounts(delegated_target_baker_id);
-- Function for generating a table where each row is a bucket.
-- Used by metrics queries.
-- This is replacing the current `date_bin_series` in a backwards compatible way, fixing issue where
-- a future bucket always got included.
CREATE OR REPLACE FUNCTION date_bin_series(bucket_size interval, starting TIMESTAMPTZ, ending TIMESTAMPTZ)
RETURNS TABLE(bucket_start TIMESTAMPTZ, bucket_end TIMESTAMPTZ) AS $$
    SELECT
        bucket_start,
        bucket_start + bucket_size
    FROM generate_series(
        date_bin(bucket_size, starting, TIMESTAMPTZ '2001-01-01'),
        date_bin(bucket_size, ending, TIMESTAMPTZ '2001-01-01'),
        bucket_size
    ) as bucket_start;
$$ LANGUAGE sql;
