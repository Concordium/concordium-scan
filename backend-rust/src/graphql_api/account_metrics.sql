-- Counts accounts in buckets by counting the cumulative total number of
-- accounts (i.e. the account index) at or before (i.e. <=) the start of the
-- bucket and the same number just before (i.e. <) the next bucket. The
-- difference between the two numbers should give the total number of accounts
-- created within the bucket.
SELECT
    -- The bucket time is the starting time of the bucket.
    bucket_time,
    -- Number of accounts at or before the bucket.
    COALESCE(before_bucket.index, 0) as start_index,
    -- Number of accounts at the end of the bucket.
    COALESCE(after_bucket.index, 0) as end_index
FROM
    -- We generate a time series of all the buckets where accounts will be counted.
    -- $1 is the full period, $2 is the bucket interval.
    -- For the rest of the comments, let's go with the example of a full period of 7 days with 6 hour buckets.
    generate_series(
        -- The first bucket starts 7 days ago.
        now() - $1::interval,
        -- The final bucket starts 6 hours ago, since the bucket time is the start of the bucket.
        now() - $2::interval,
        -- Each bucket is seperated by 6 hours.
        $2::interval
    ) AS bucket_time
LEFT JOIN LATERAL (
    -- Selects the index at or before the start of the bucket.
    SELECT accounts.index
    FROM accounts
    LEFT JOIN transactions on transaction_index = transactions.index
    LEFT JOIN blocks ON transactions.block_height = height
    WHERE slot_time <= bucket_time
    ORDER BY slot_time DESC
    LIMIT 1
) before_bucket ON true
LEFT JOIN LATERAL (
    -- Selects the index at the end of the bucket.
    SELECT accounts.index
    FROM accounts
    LEFT JOIN transactions on transaction_index = transactions.index
    LEFT JOIN blocks ON transactions.block_height = height
    WHERE slot_time < bucket_time + $2::interval
    ORDER BY slot_time DESC
    LIMIT 1
) after_bucket ON true
