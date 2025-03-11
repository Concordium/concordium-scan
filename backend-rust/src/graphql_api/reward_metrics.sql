SELECT
    bucket_time.bucket_start as "bucket_time!",
    COALESCE(after_bucket.total_amount, 0) - COALESCE(before_bucket.total_amount, 0) as "bucket_rewards_total!"
FROM
    date_bin_series(
        $3::interval,
        $2,
        $1
    ) AS bucket_time
LEFT JOIN LATERAL (
    SELECT
        total_amount
    FROM metrics_bakers
    LEFT JOIN blocks ON metrics_rewards.block_height = blocks.height
    WHERE slot_time <= bucket_time.bucket_start
    ORDER BY slot_time DESC
    LIMIT 1
) before_bucket ON true
LEFT JOIN LATERAL (
    SELECT
        total_amount
    FROM metrics_bakers
    LEFT JOIN blocks ON metrics_rewards.block_height = blocks.height
    WHERE slot_time < bucket_time.bucket_end
    ORDER BY slot_time DESC
    LIMIT 1
) after_bucket ON true
