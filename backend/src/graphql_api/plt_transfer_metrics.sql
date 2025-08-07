-- Inputs:
-- $1::interval - e.g. '30 days'
-- $2::interval - e.g. '1 days'
-- $3::BIGINT   - mandatory token filter (token_index)

WITH buckets AS (
  SELECT bucket_start
  FROM date_bin_series(
    $2::interval,
    now() - $1::interval,
    now()
  ) AS bucket_start
),

latest_per_bucket AS (
  SELECT
    b.bucket_start AS bucket_interval_start,
    tm.token_index,
    tm.event_timestamp,
    tm.cumulative_transfer_count,
    tm.cumulative_transfer_amount
  FROM buckets b
  LEFT JOIN LATERAL (
    SELECT *
    FROM metrics_specific_plt_transfer tm
    WHERE tm.event_timestamp <= b.bucket_start
      AND tm.token_index = $3::BIGINT
    ORDER BY tm.event_timestamp DESC
    LIMIT 1
  ) tm ON true
),

delta AS (
  SELECT
    bucket_interval_start,
    token_index,
    cumulative_transfer_count,
    cumulative_transfer_amount,
    LAG(cumulative_transfer_count) OVER (PARTITION BY token_index ORDER BY bucket_interval_start) AS prev_cumulative_transfer_count,
    LAG(cumulative_transfer_amount) OVER (PARTITION BY token_index ORDER BY bucket_interval_start) AS prev_cumulative_transfer_amount
  FROM latest_per_bucket
)

SELECT
  bucket_interval_start AS "bucket_time!",
  COALESCE(token_index, $3::BIGINT) AS "token_index!",
  COALESCE(cumulative_transfer_count - COALESCE(prev_cumulative_transfer_count, 0), 0) AS transfer_count,
  COALESCE(cumulative_transfer_amount - COALESCE(prev_cumulative_transfer_amount, 0), 0) AS transfer_volume
FROM delta
ORDER BY bucket_interval_start;
