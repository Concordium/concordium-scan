-- Inputs:
-- $1::interval - e.g. '90 days'
-- $2::interval - e.g. '3 days'
-- $3::BIGINT   - optional token filter

WITH pre_agg_events AS MATERIALIZED (
  SELECT
    date_bin($2::interval, event_timestamp, now() - $1::interval) AS bucket_start,
    token_index,
    event_type,
    CASE
      WHEN event_type IN ('Transfer', 'Mint', 'Burn') AND amount_value IS NOT NULL THEN
        amount_value / POWER(10, COALESCE(amount_decimals, 0))
      ELSE 0
    END AS amount
  FROM plt_events
  WHERE token_index IS NOT NULL
    AND event_timestamp >= now() - $1::interval
    AND event_timestamp <= now()
  AND ($3::BIGINT IS NULL OR token_index = $3::BIGINT)
),

aggregated AS MATERIALIZED (
  SELECT
    bucket_start,
    token_index,

    COUNT(token_index) FILTER (WHERE event_type = 'Transfer')    AS transfer_count,
    COUNT(token_index) FILTER (WHERE event_type = 'Mint')        AS mint_count,
    COUNT(token_index) FILTER (WHERE event_type = 'Burn')        AS burn_count,
    COUNT(token_index) FILTER (WHERE event_type = 'TokenModule') AS token_module_count,

    SUM(amount) FILTER (WHERE event_type = 'Transfer') AS transfer_volume,
    SUM(amount) FILTER (WHERE event_type = 'Mint')     AS mint_volume,
    SUM(amount) FILTER (WHERE event_type = 'Burn')     AS burn_volume,

    COUNT(*) AS total_event_count
  FROM pre_agg_events
  GROUP BY bucket_start, token_index
)

SELECT
  bucket_start AS "bucket_time!",
  token_index  AS "token_index!",
  transfer_count,
  transfer_volume,
  mint_count,
  mint_volume,
  burn_count,
  burn_volume,
  token_module_count,
  total_event_count
FROM aggregated
ORDER BY bucket_start, token_index;
