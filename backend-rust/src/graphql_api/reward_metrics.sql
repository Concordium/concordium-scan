SELECT
    bucket_time.bucket_start as "bucket_time!",
    COALESCE(after_bucket.accumulated_amount, 0) as "after_bucket_rewards!",
    COALESCE(before_bucket.accumulated_amount, 0) as "before_bucket_rewards!"
FROM
    date_bin_series(
        $3::interval,
        $2,
        $1
    ) AS bucket_time
LEFT JOIN LATERAL (
    SELECT
        CASE
            WHEN $4::BIGINT IS NULL THEN total_accumulated_amount
            ELSE account_accumulated_amount
        END AS accumulated_amount
    FROM metrics_rewards
    LEFT JOIN blocks ON metrics_rewards.block_height = blocks.height
    WHERE slot_time < bucket_time.bucket_end AND ($4::BIGINT IS NULL OR account_id = $4::BIGINT)
    LIMIT 1
) before_bucket ON true
LEFT JOIN LATERAL (
    SELECT
        CASE
            WHEN $4::BIGINT IS NULL THEN total_accumulated_amount
            ELSE account_accumulated_amount
        END AS accumulated_amount
    FROM metrics_rewards
    LEFT JOIN blocks ON metrics_rewards.block_height = blocks.height
    WHERE slot_time < bucket_time.bucket_start AND ($4::BIGINT IS NULL OR account_id = $4::BIGINT)
    LIMIT 1
) after_bucket ON true
