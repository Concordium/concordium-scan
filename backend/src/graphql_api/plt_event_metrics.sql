-- Inputs:
-- $1::interval - total period (e.g., '7 days')
-- $2::interval - bucket width (e.g., '6 hours')
-- $3::text     - optional token_id (NULL means include all tokens)

SELECT
    bucket_time.bucket_start AS "bucket_time!",

    -- Event counts
    COALESCE(before_bucket.cumulative_event_count, 0) AS "start_cumulative_event_count!",
    COALESCE(after_bucket.cumulative_event_count, 0) AS "end_cumulative_event_count!",
    COALESCE(after_bucket.cumulative_event_count, 0) - COALESCE(before_bucket.cumulative_event_count, 0) AS "delta_event_count!",

    -- Total supply
    COALESCE(supply_start.total_supply, 0) AS "start_total_supply!",
    COALESCE(supply_end.total_supply, 0) AS "end_total_supply!",
    COALESCE(supply_end.total_supply, 0) - COALESCE(supply_start.total_supply, 0) AS "delta_total_supply!",

    -- Unique holders
    COALESCE(holders_start.total_unique_holders, 0) AS "start_total_unique_holders!",
    COALESCE(holders_end.total_unique_holders, 0) AS "end_total_unique_holders!",
    COALESCE(holders_end.total_unique_holders, 0) - COALESCE(holders_start.total_unique_holders, 0) AS "delta_unique_holders!"

FROM date_bin_series(
    $2::interval,
    now() - $1::interval,
    now()
) AS bucket_time

-- Event count before bucket start
LEFT JOIN LATERAL (
    SELECT COUNT(*) AS cumulative_event_count
    FROM plt_events e
    JOIN plt_tokens pt ON pt.index = e.token_index
    JOIN transactions tx ON e.transaction_index = tx.index
    JOIN blocks b ON tx.block_height = b.height
    WHERE b.slot_time < bucket_time.bucket_start
      AND ($3::TEXT IS NULL OR pt.token_id = $3::TEXT)

) before_bucket ON true

-- Event count before bucket end
LEFT JOIN LATERAL (
    SELECT COUNT(*) AS cumulative_event_count
    FROM plt_events e
    JOIN plt_tokens pt ON pt.index = e.token_index
    JOIN transactions tx ON e.transaction_index = tx.index
    JOIN blocks b ON tx.block_height = b.height
    WHERE b.slot_time < bucket_time.bucket_start + $2::interval
      AND ($3::TEXT IS NULL OR pt.token_id = $3::TEXT)

) after_bucket ON true

-- Total supply before bucket start
LEFT JOIN LATERAL (
    SELECT SUM(pt.total_minted - pt.total_burned)  AS total_supply
    FROM plt_tokens pt
    JOIN transactions tx ON pt.transaction_index = tx.index
    JOIN blocks b ON tx.block_height = b.height
    WHERE b.slot_time < bucket_time.bucket_start
      AND ($3::TEXT IS NULL OR pt.token_id = $3::TEXT)

) supply_start ON true

-- Total supply before bucket end
LEFT JOIN LATERAL (
    SELECT SUM(pt.total_minted - pt.total_burned) AS total_supply
    FROM plt_tokens pt
    JOIN transactions tx ON pt.transaction_index = tx.index
    JOIN blocks b ON tx.block_height = b.height
    WHERE b.slot_time < bucket_time.bucket_start + $2::interval
      AND ($3::TEXT IS NULL OR pt.token_id = $3::TEXT)

) supply_end ON true

-- Unique holders before bucket start
LEFT JOIN LATERAL (
    SELECT COUNT(DISTINCT paa.account_index) AS total_unique_holders
    FROM plt_accounts paa
    JOIN plt_tokens pt ON paa.token_index = pt.index
    JOIN transactions tx ON pt.transaction_index = tx.index
    JOIN blocks b ON tx.block_height = b.height
    WHERE b.slot_time < bucket_time.bucket_start
      AND paa.amount > 0
      AND ($3::TEXT IS NULL OR pt.token_id = $3::TEXT)

) holders_start ON true

-- Unique holders before bucket end
LEFT JOIN LATERAL (
    SELECT COUNT(DISTINCT paa.account_index) AS total_unique_holders
    FROM plt_accounts paa
    JOIN plt_tokens pt ON paa.token_index = pt.index
    JOIN transactions tx ON pt.transaction_index = tx.index
    JOIN blocks b ON tx.block_height = b.height
    WHERE b.slot_time < bucket_time.bucket_start + $2::interval
      AND paa.amount > 0
      AND ($3::TEXT IS NULL OR pt.token_id = $3::TEXT)

) holders_end ON true

ORDER BY bucket_time.bucket_start;
