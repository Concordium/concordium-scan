-- Inputs:
-- $1::interval - total period
-- $2::interval - bucket width 
-- $3::text     - optional token_id (NULL means include all tokens)
-- This is temprary, might have to modify later
WITH bucket_time AS (
  SELECT generate_series(
    now() - $1::interval,
    now(),
    $2::interval
  ) AS bucket_start
),

events_with_block AS (
  SELECT
    e.*,
    pt.token_id,
    b.slot_time
  FROM plt_events e
  JOIN plt_tokens pt ON pt.index = e.token_index
  JOIN transactions tx ON e.transaction_index = tx.index
  JOIN blocks b ON tx.block_height = b.height
),

supply_with_block AS (
  SELECT
    pt.token_id,
    b.slot_time,
    (pt.total_minted - pt.total_burned) / POWER(10, pt.decimal) AS total_supply
  FROM plt_tokens pt
  JOIN transactions tx ON pt.transaction_index = tx.index
  JOIN blocks b ON tx.block_height = b.height
),

holders_with_block AS (
  SELECT
    paa.account_index,
    pt.token_id,
    paa.amount,
    b.slot_time
  FROM plt_accounts paa
  JOIN plt_tokens pt ON paa.token_index = pt.index
  JOIN transactions tx ON pt.transaction_index = tx.index
  JOIN blocks b ON tx.block_height = b.height
  WHERE paa.amount > 0
),

aggregated AS (
  SELECT
    bt.bucket_start,

    -- Event counts
    (
      SELECT COUNT(*) FROM events_with_block e
      WHERE e.slot_time < bt.bucket_start
        AND ($3::TEXT IS NULL OR e.token_id = $3::TEXT)
    ) AS start_cumulative_event_count,

    (
      SELECT COUNT(*) FROM events_with_block e
      WHERE e.slot_time < bt.bucket_start + $2::interval
        AND ($3::TEXT IS NULL OR e.token_id = $3::TEXT)
    ) AS end_cumulative_event_count,

    -- Total supply
    (
      SELECT COALESCE(SUM(s.total_supply), 0) FROM supply_with_block s
      WHERE s.slot_time < bt.bucket_start
        AND ($3::TEXT IS NULL OR s.token_id = $3::TEXT)
    ) AS start_total_supply,

    (
      SELECT COALESCE(SUM(s.total_supply), 0) FROM supply_with_block s
      WHERE s.slot_time < bt.bucket_start + $2::interval
        AND ($3::TEXT IS NULL OR s.token_id = $3::TEXT)
    ) AS end_total_supply,

    -- Unique holders
    (
      SELECT COUNT(DISTINCT h.account_index) FROM holders_with_block h
      WHERE h.slot_time < bt.bucket_start
        AND ($3::TEXT IS NULL OR h.token_id = $3::TEXT)
    ) AS start_total_unique_holders,

    (
      SELECT COUNT(DISTINCT h.account_index) FROM holders_with_block h
      WHERE h.slot_time < bt.bucket_start + $2::interval
        AND ($3::TEXT IS NULL OR h.token_id = $3::TEXT)
    ) AS end_total_unique_holders

  FROM bucket_time bt
)

SELECT
  aggregated.bucket_start AS "bucket_time!",

  aggregated.start_cumulative_event_count AS "start_cumulative_event_count!",
  aggregated.end_cumulative_event_count AS "end_cumulative_event_count!",
  aggregated.end_cumulative_event_count - aggregated.start_cumulative_event_count AS "delta_event_count!",

  aggregated.start_total_supply AS "start_total_supply!",
  aggregated.end_total_supply AS "end_total_supply!",
  aggregated.end_total_supply - aggregated.start_total_supply AS "delta_total_supply!",

  aggregated.start_total_unique_holders AS "start_total_unique_holders!",
  aggregated.end_total_unique_holders AS "end_total_unique_holders!",
  aggregated.end_total_unique_holders - aggregated.start_total_unique_holders AS "delta_unique_holders!"

FROM aggregated
ORDER BY aggregated.bucket_start;
