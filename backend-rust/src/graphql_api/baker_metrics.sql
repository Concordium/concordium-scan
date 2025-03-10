SELECT
    bucket_time.bucket_start as "bucket_time!",
    after_bucket.total_bakers_added - before_bucket.total_bakers_added as "bucket_bakers_added!",
    after_bucket.total_bakers_removed - before_bucket.total_bakers_removed as "bucket_bakers_removed!"
FROM
    date_bin_series(
        $3::interval,
        $2,
        $1
    ) AS bucket_time
LEFT JOIN LATERAL (
    SELECT
        COALESCE(metrics_bakers.total_bakers_added, 0) as total_bakers_added,
        COALESCE(metrics_bakers.total_bakers_removed, 0) as total_bakers_removed
    FROM metrics_bakers
    LEFT JOIN blocks ON metrics_bakers.block_height = blocks.height
    WHERE slot_time <= bucket_time.bucket_start
    ORDER BY slot_time DESC
    LIMIT 1
) before_bucket ON true
LEFT JOIN LATERAL (
    SELECT
        COALESCE(metrics_bakers.total_bakers_added, 0) as total_bakers_added,
        COALESCE(metrics_bakers.total_bakers_removed, 0) as total_bakers_removed
    FROM metrics_bakers
    LEFT JOIN blocks ON metrics_bakers.block_height = blocks.height
    WHERE slot_time < bucket_time.bucket_end
    ORDER BY slot_time DESC
    LIMIT 1
) after_bucket ON true
