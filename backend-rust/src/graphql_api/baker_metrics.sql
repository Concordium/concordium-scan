SELECT
    bucket_time.bucket_start as "bucket_time!",
    COALESCE(after_bucket.total_bakers_added, 0) - COALESCE(before_bucket.total_bakers_added, 0) as "bucket_bakers_added!",
    COALESCE(after_bucket.total_bakers_removed, 0) - COALESCE(before_bucket.total_bakers_removed, 0) as "bucket_bakers_removed!"
FROM
    date_bin_series(
        $3::interval,
        $2,
        $1
    ) AS bucket_time
LEFT JOIN LATERAL (
    SELECT
        total_bakers_added,
        total_bakers_removed
    FROM metrics_bakers
    LEFT JOIN blocks ON metrics_bakers.block_height = blocks.height
    WHERE slot_time <= bucket_time.bucket_start
    ORDER BY slot_time DESC
    LIMIT 1
) before_bucket ON true
LEFT JOIN LATERAL (
    SELECT
        total_bakers_added,
        total_bakers_removed
    FROM metrics_bakers
    LEFT JOIN blocks ON metrics_bakers.block_height = blocks.height
    WHERE slot_time <= bucket_time.bucket_end
    ORDER BY slot_time DESC
    LIMIT 1
) after_bucket ON true
