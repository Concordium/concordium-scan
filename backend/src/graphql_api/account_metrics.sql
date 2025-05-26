-- Counts accounts in buckets by counting the cumulative total number of
-- accounts (i.e. the account index) at or before (i.e. <) the start of the
-- bucket and the same number just before (i.e. <) the next bucket. The
-- difference between the two numbers should give the total number of accounts
-- created within the bucket.
WITH
  thresholds AS (
    SELECT
      bucket_time.bucket_start as bucket_time,
      -- Find the latest transaction index before the start of the bucket
      (
        SELECT transactions.index
        FROM transactions
        JOIN blocks ON height = transactions.block_height
        WHERE slot_time < bucket_time.bucket_start
        ORDER BY slot_time DESC
        LIMIT 1
      ) AS tx_start,
      -- Find the latest transaction index before the end of the bucket
      (
        SELECT transactions.index
        FROM transactions
        JOIN blocks ON height = transactions.block_height
        WHERE slot_time < bucket_time.bucket_start + $3::interval
        ORDER BY slot_time DESC
        LIMIT 1
      ) AS tx_end
    FROM date_bin_series(
        $3::interval,
        $2,
        $1
    ) AS bucket_time
  )
SELECT
  bucket_time AS "bucket_time!",
  COALESCE(
    (
      SELECT MAX(index) + 1
      FROM accounts
      WHERE transaction_index <= tx_start
    ), 0
  ) AS "start_index!",
  COALESCE(
    (
      SELECT MAX(index) + 1
      FROM accounts
      WHERE transaction_index <= tx_end
    ), 0
  ) AS "end_index!"
FROM thresholds
ORDER BY bucket_time;
