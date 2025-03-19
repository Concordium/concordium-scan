SELECT
    bucket_time.bucket_start AS "bucket_time!",
    (SELECT COALESCE(SUM(amount), 0)
                    FROM metrics_rewards
                    WHERE block_slot_time BETWEEN bucket_time.bucket_start AND bucket_time.bucket_end
                    AND ($4::BIGINT IS NULL OR account_index = $4::BIGINT))::BIGINT AS "accumulated_amount!"
FROM
    date_bin_series(
        $3::interval,
        $2,
        $1
    ) AS bucket_time
