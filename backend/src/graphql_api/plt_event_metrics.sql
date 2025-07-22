-- Inputs:
-- $1::interval - total period
-- $2::interval - bucket width
-- $3::text     - optional token_id (NULL means include all tokens)

WITH bucket_time AS (
  SELECT generate_series(
    now() - $1::interval,
    now(),
    $2::interval
  ) AS bucket_start
),

events_with_transfer_amounts AS (
  SELECT
    e.token_index,
    pt.token_id,
    b.slot_time,
    e.event_type,
    CASE 
      WHEN e.event_type = 'Transfer' THEN
        COALESCE(((e.token_event->'amount'->>'value')::numeric / POWER(10, COALESCE(pt.decimal, 0))), 0)
      ELSE 0
    END AS transfer_amount
  FROM plt_events e
  JOIN plt_tokens pt ON pt.index = e.token_index
  JOIN transactions tx ON e.transaction_index = tx.index
  JOIN blocks b ON tx.block_height = b.height
  WHERE ($3::TEXT IS NULL OR pt.token_id = $3::TEXT)
),

events_cumulative AS (
  SELECT
    slot_time,
    COUNT(*) OVER (ORDER BY slot_time) AS cumulative_event_count,
    COUNT(*) FILTER (WHERE event_type = 'Transfer') OVER (ORDER BY slot_time) AS cumulative_transfer_count,
    SUM(CASE WHEN event_type = 'Transfer' THEN transfer_amount ELSE 0 END) OVER (ORDER BY slot_time) AS cumulative_transfer_volume
  FROM events_with_transfer_amounts
),

supply_snapshots AS (
  SELECT
    pt.token_id,
    b.slot_time,
    (COALESCE(pt.total_minted, 0) - COALESCE(pt.total_burned, 0)) / POWER(10, COALESCE(pt.decimal, 0)) AS total_supply
  FROM plt_tokens pt
  JOIN transactions tx ON pt.transaction_index = tx.index
  JOIN blocks b ON tx.block_height = b.height
  WHERE ($3::TEXT IS NULL OR pt.token_id = $3::TEXT)
),

holder_balances AS (
  SELECT
    paa.account_index,
    pt.token_id,
    COALESCE(paa.amount, 0) as amount,
    b.slot_time
  FROM plt_accounts paa
  JOIN plt_tokens pt ON paa.token_index = pt.index
  JOIN transactions tx ON pt.transaction_index = tx.index
  JOIN blocks b ON tx.block_height = b.height
  WHERE COALESCE(paa.amount, 0) > 0 AND ($3::TEXT IS NULL OR pt.token_id = $3::TEXT)
),

aggregated AS (
  SELECT
    bt.bucket_start,

    COALESCE(
      (SELECT cumulative_event_count FROM events_cumulative WHERE slot_time < bt.bucket_start ORDER BY slot_time DESC LIMIT 1), 0
    ) AS start_cumulative_event_count,
    COALESCE(
      (SELECT cumulative_event_count FROM events_cumulative WHERE slot_time < bt.bucket_start + $2::interval ORDER BY slot_time DESC LIMIT 1), 0
    ) AS end_cumulative_event_count,

    COALESCE(
      (SELECT cumulative_transfer_count FROM events_cumulative WHERE slot_time < bt.bucket_start ORDER BY slot_time DESC LIMIT 1), 0
    ) AS start_cumulative_transfer_count,
    COALESCE(
      (SELECT cumulative_transfer_count FROM events_cumulative WHERE slot_time < bt.bucket_start + $2::interval ORDER BY slot_time DESC LIMIT 1), 0
    ) AS end_cumulative_transfer_count,

    COALESCE(
      (SELECT cumulative_transfer_volume FROM events_cumulative WHERE slot_time < bt.bucket_start ORDER BY slot_time DESC LIMIT 1), 0
    ) AS start_cumulative_transfer_volume,
    COALESCE(
      (SELECT cumulative_transfer_volume FROM events_cumulative WHERE slot_time < bt.bucket_start + $2::interval ORDER BY slot_time DESC LIMIT 1), 0
    ) AS end_cumulative_transfer_volume,

    COALESCE((
      SELECT s.total_supply
      FROM supply_snapshots s
      WHERE s.slot_time < bt.bucket_start
      ORDER BY s.slot_time DESC
      LIMIT 1
    ), 0) AS start_total_supply,
    COALESCE((
      SELECT s.total_supply
      FROM supply_snapshots s
      WHERE s.slot_time < bt.bucket_start + $2::interval
      ORDER BY s.slot_time DESC
      LIMIT 1
    ), 0) AS end_total_supply,

    COALESCE((
      SELECT COUNT(DISTINCT h.account_index)
      FROM holder_balances h
      WHERE h.slot_time < bt.bucket_start AND h.amount > 0
    ), 0) AS start_total_unique_holders,
    COALESCE((
      SELECT COUNT(DISTINCT h.account_index)
      FROM holder_balances h
      WHERE h.slot_time < bt.bucket_start + $2::interval AND h.amount > 0
    ), 0) AS end_total_unique_holders

  FROM bucket_time bt
)

SELECT
  aggregated.bucket_start AS "bucket_time!",

  aggregated.start_cumulative_event_count AS "start_cumulative_event_count!",
  aggregated.end_cumulative_event_count AS "end_cumulative_event_count!",
  aggregated.end_cumulative_event_count - aggregated.start_cumulative_event_count AS "delta_event_count!",

  aggregated.start_cumulative_transfer_count AS "start_cumulative_transfer_count!",
  aggregated.end_cumulative_transfer_count AS "end_cumulative_transfer_count!",
  aggregated.end_cumulative_transfer_count - aggregated.start_cumulative_transfer_count AS "delta_transfer_count!",

  aggregated.start_cumulative_transfer_volume AS "start_cumulative_transfer_volume!",
  aggregated.end_cumulative_transfer_volume AS "end_cumulative_transfer_volume!",
  aggregated.end_cumulative_transfer_volume - aggregated.start_cumulative_transfer_volume AS "delta_transfer_volume!",

  aggregated.start_total_supply AS "start_total_supply!",
  aggregated.end_total_supply AS "end_total_supply!",
  aggregated.end_total_supply - aggregated.start_total_supply AS "delta_total_supply!",

  aggregated.start_total_unique_holders AS "start_total_unique_holders!",
  aggregated.end_total_unique_holders AS "end_total_unique_holders!",
  aggregated.end_total_unique_holders - aggregated.start_total_unique_holders AS "delta_unique_holders!"

FROM aggregated
ORDER BY aggregated.bucket_start;