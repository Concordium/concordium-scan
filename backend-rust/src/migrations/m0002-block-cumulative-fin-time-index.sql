-- Used when updating the cumulative finalization time, by efficiently finding blocks which have
-- finalization_time, but no cumulative_finalization_time.
CREATE INDEX blocks_height_null_cumulative_finalization_time ON blocks (height)
    WHERE blocks.cumulative_finalization_time IS NULL
    AND blocks.finalization_time IS NOT NULL;

-- Important for quickly calculating the delegated stake to a baker pool.
CREATE INDEX delegated_target_baker_id_index ON accounts(delegated_target_baker_id);
