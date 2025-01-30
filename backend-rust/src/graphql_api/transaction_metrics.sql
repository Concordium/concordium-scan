-- Counts transactions in buckets by counting the cumulative total number of
-- transactions at or before (i.e. <=) the start of the bucket and the same number just
-- before (i.e. <) the next bucket. The difference between the two numbers should
-- give the total number of transactions within the bucket.
SELECT
    -- The bucket time is the starting time of the bucket.
    bucket_time.bucket_start as bucket_time,
    -- Number of transactions at or before the bucket.
    COALESCE(before_bucket.cumulative_num_txs, 0) as start_cumulative_num_txs,
    -- Number of transactions at the end of the bucket.
    COALESCE(after_bucket.cumulative_num_txs, 0) as end_cumulative_num_txs
FROM
    -- We generate a time series of all the buckets where transactions will be counted.
    -- $1 is the full period, $2 is the bucket interval.
    -- For the rest of the comments, let's go with the example of a full period of 7 days with 6 hour buckets.
    date_bin_series(
        -- Size of the buckets.
        $2::interval,
        -- The first bucket should cover 7 days ago.
        now() - $1::interval,
        -- The final bucket should cover now.
        now()
    ) AS bucket_time
LEFT JOIN LATERAL (
    -- Selects the cumulative number of transactions at or before the start of the bucket.
    SELECT cumulative_num_txs
    FROM blocks
    WHERE slot_time <= bucket_time.bucket_start
    ORDER BY slot_time DESC
    LIMIT 1
) before_bucket ON true
LEFT JOIN LATERAL (
    -- Selects the cumulative number of transactions at the end of the bucket.
    SELECT cumulative_num_txs
    FROM blocks
    WHERE slot_time < bucket_time.bucket_end
    ORDER BY slot_time DESC
    LIMIT 1
) after_bucket ON true
