-- Counts accounts in buckets by counting the cumulative total number of
-- accounts (i.e. the account index) at or before (i.e. <) the start of the
-- bucket and the same number just before (i.e. <) the next bucket. The
-- difference between the two numbers should give the total number of accounts
-- created within the bucket.
WITH
  buckets AS (
    SELECT generate_series(
      now() - $1::interval,
      now() - $2::interval,
      $2::interval
    ) AS bucket_time
  ),
  thresholds AS (
    SELECT
      b.bucket_time,
      -- Find the latest transaction index before the start of the bucket
      (
        SELECT t.index
        FROM transactions t
        JOIN blocks     bl ON bl.height = t.block_height
        WHERE bl.slot_time < b.bucket_time
        ORDER BY bl.slot_time DESC
        LIMIT 1
      ) AS tx_start,
      -- Find the latest transaction index before the end of the bucket
      (
        SELECT t.index
        FROM transactions t
        JOIN blocks     bl ON bl.height = t.block_height
        WHERE bl.slot_time < b.bucket_time + $2::interval
        ORDER BY bl.slot_time DESC
        LIMIT 1
      ) AS tx_end
    FROM buckets b
  )
SELECT
  th.bucket_time                              AS "bucket_time!",
  COALESCE(
    (
      SELECT MAX(a.index)
      FROM accounts a
      WHERE a.transaction_index <= th.tx_start
    ), 0
  ) AS "start_index!",
  COALESCE(
    (
      SELECT MAX(a.index)
      FROM accounts a
      WHERE a.transaction_index <= th.tx_end
    ), 0
  ) AS "end_index!"
FROM thresholds th
ORDER BY th.bucket_time;
