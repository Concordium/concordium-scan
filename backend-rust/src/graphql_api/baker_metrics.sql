SELECT
    bucket_time.bucket_start as "bucket_time!",
    COALESCE((SELECT
        total_bakers_added
     FROM metrics_bakers
     WHERE block_height < before_bucket.height
     ORDER BY block_height DESC
     LIMIT 1), 0) as "added_before!",
    COALESCE((SELECT
        total_bakers_removed
     FROM metrics_bakers
     WHERE block_height < before_bucket.height
     ORDER BY block_height DESC
     LIMIT 1), 0) as "removed_before!",
    COALESCE((SELECT
        total_bakers_added
     FROM metrics_bakers
     WHERE block_height < after_bucket.height
     ORDER BY block_height DESC
     LIMIT 1), 0) as "added_after!",
    COALESCE((SELECT
        total_bakers_removed
     FROM metrics_bakers
     WHERE block_height < after_bucket.height
     ORDER BY block_height DESC
     LIMIT 1), 0) as "removed_after!"
FROM
    date_bin_series(
        $3::interval,
        $2,
        $1
    ) AS bucket_time
LEFT JOIN LATERAL (
    SELECT
        height
    FROM blocks
    WHERE slot_time < bucket_time.bucket_start
    ORDER BY slot_time DESC
    LIMIT 1
) before_bucket ON true
LEFT JOIN LATERAL (
    SELECT
        height
    FROM blocks
    WHERE slot_time < bucket_time.bucket_end
    ORDER BY slot_time DESC
    LIMIT 1
) after_bucket ON true
