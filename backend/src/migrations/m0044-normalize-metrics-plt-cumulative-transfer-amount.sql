-- Migration m0044: Normalize metrics_plt cumulative transfer amount
--
-- PURPOSE:
-- This migration recalculates the cumulative_transfer_amount field in the metrics_plt table
-- by properly normalizing PLT (Protocol Level Token) transfer amounts across all tokens.
-- 
-- BACKGROUND:
-- Different PLT tokens can have different decimal places (e.g., 6, 8, 18 decimals).
-- Raw transfer amounts are stored as integers but represent different actual values
-- depending on each token's decimal configuration. To get meaningful aggregate
-- volume metrics, we need to normalize all amounts to their actual decimal values.
--
-- PROCESS:
-- 1. Calculate per-token deltas: For each token, compute the change in cumulative
--    transfer amount since the last event for that specific token
-- 2. Normalize amounts: Divide each delta by 10^(token_decimals) to get actual values
-- 3. Aggregate by timestamp: Sum all normalized deltas occurring at the same time
-- 4. Build cumulative series: Create running totals over time of normalized amounts
-- 5. Fill timeline gaps: Ensure every timestamp has a value by forward-filling
-- 6. Update metrics_plt: Insert/update the properly normalized cumulative amounts
--
-- EXAMPLE:
-- Token A (6 decimals): raw_amount=1000000 → normalized=1.0
-- Token B (18 decimals): raw_amount=1000000000000000000 → normalized=1.0  
-- Combined normalized volume: 2.0 (instead of 1000001000000000000000 raw)


-- 1) Compute per-token deltas (change since last event for that token)
WITH per_token_deltas AS (
    SELECT
        mpt.token_index,
        mpt.event_timestamp,
        pt.decimal,
        COALESCE(
            mpt.cumulative_transfer_amount
          - LAG(mpt.cumulative_transfer_amount) OVER (
                PARTITION BY mpt.token_index ORDER BY mpt.event_timestamp
            ),
            mpt.cumulative_transfer_amount
        ) AS delta_raw
    FROM metrics_plt_transfer mpt
    JOIN plt_tokens pt ON mpt.token_index = pt.index
),

-- 2) Normalize and aggregate across tokens per timestamp
normalized_deltas AS (
    SELECT
        event_timestamp,
        SUM(GREATEST(delta_raw,0) / POWER(10::numeric, decimal)) AS normalized_amount
    FROM per_token_deltas
    GROUP BY event_timestamp
),

-- 3) Compute cumulative total across time
cumulative_series AS (
    SELECT
        event_timestamp,
        SUM(normalized_amount) OVER (
            ORDER BY event_timestamp
            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
        ) AS cumulative_amount
    FROM normalized_deltas
),

-- 4) Build full timeline and carry forward last known cumulative
timeline AS (
    SELECT DISTINCT event_timestamp
    FROM (
        SELECT event_timestamp FROM normalized_deltas
        UNION
        SELECT event_timestamp FROM metrics_plt
    ) t
),
filled_series AS (
    SELECT
        ts.event_timestamp,
        -- forward-fill using max() over preceding rows
        MAX(cs.cumulative_amount) OVER (
            ORDER BY ts.event_timestamp
            ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
        ) AS cumulative_amount
    FROM timeline ts
    LEFT JOIN cumulative_series cs USING (event_timestamp)
)

-- 5) Upsert into metrics_plt
INSERT INTO metrics_plt (event_timestamp, cumulative_transfer_amount)
SELECT event_timestamp, COALESCE(cumulative_amount, 0)  -- guarantee non-null
FROM filled_series
ON CONFLICT (event_timestamp)
DO UPDATE
SET cumulative_transfer_amount = EXCLUDED.cumulative_transfer_amount;