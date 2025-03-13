SELECT
    bucket_time.bucket_start as "bucket_time!",
    COALESCE(SUM(period.account_period_max), 0) - COALESCE(SUM(period.account_period_min), 0) as "bucket_rewards_total!"
FROM
    date_bin_series(
        $3::interval,
        $2,
        $1
    ) AS bucket_time
LEFT JOIN LATERAL (
    SELECT
        MAX(accumulated_amount) as account_period_max,
        MIN(accumulated_amount) as account_period_min
    FROM metrics_rewards
    LEFT JOIN blocks ON metrics_rewards.block_height = blocks.height
    GROUP BY account_id
    WHERE slot_time < bucket_time.bucket_end AND slot_time >= bucket_time.bucket_start AND ($1 IS NULL OR account_id = $1)
) period ON true
