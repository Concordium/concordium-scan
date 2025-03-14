SELECT
    bucket_time.bucket_start as "bucket_time!",
    COALESCE(after_bucket.accumulated_amount, 0) - COALESCE(before_bucket.accumulated_amount, 0) as "bucket_rewards!"
FROM
    date_bin_series(
        $3::interval,
        $2,
        $1
    ) AS bucket_time
LEFT JOIN LATERAL (
    SELECT
        (CASE
            WHEN $1 IS NULL THEN total_accumulated_amount
            ELSE account_accumulated_amount
        END) AS accumulated_amount
    FROM metrics_rewards
    LEFT JOIN blocks ON metrics_rewards.block_height = blocks.height
    GROUP BY account_id
    WHERE slot_time < bucket_time.bucket_end AND ($1 IS NULL OR account_id = $1)
    LIMIT 1
) before_bucket ON true
LEFT JOIN LATERAL (
    SELECT
        (CASE
            WHEN $1 IS NULL THEN total_accumulated_amount
            ELSE account_accumulated_amount
        END) AS accumulated_amount
    FROM metrics_rewards
    LEFT JOIN blocks ON metrics_rewards.block_height = blocks.height
    GROUP BY account_id
    WHERE slot_time < bucket_time.bucket_start AND ($1 IS NULL OR account_id = $1)
    LIMIT 1
) after_bucket ON true
