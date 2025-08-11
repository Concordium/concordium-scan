-- Migration script to alter plt_events table
-- Add new columns: event_timestamp, amount_value, amount_decimals
-- This reduces the plt_event_metrics query complexity 
ALTER TABLE plt_events
ADD COLUMN IF NOT EXISTS event_timestamp TIMESTAMPTZ;

ALTER TABLE plt_events
ADD COLUMN IF NOT EXISTS amount_value NUMERIC;

ALTER TABLE plt_events
ADD COLUMN IF NOT EXISTS amount_decimals INT;

-- Create indexes to optimize queries on the new columns (helps in index only scans)
CREATE INDEX  IF NOT EXISTS idx_plt_events_token_type_time
ON plt_events (event_type, event_timestamp, token_index)
INCLUDE (amount_value, amount_decimals);

--  Create TEMPORARY indexes to speed up the backfill process
CREATE INDEX IF NOT EXISTS idx_temp_plt_events_backfill
  ON plt_events (id);

-- Create temporary indexes on related tables to speed up joins during backfill
CREATE INDEX IF NOT EXISTS idx_temp_transactions_index
  ON transactions (index);
-- Create temporary index on blocks to speed up backfill
CREATE INDEX IF NOT EXISTS idx_temp_blocks_height
  ON blocks (height);

--  Backfill all columns (timestamp, amount_value, amount_decimals) in one loop
DO $$
DECLARE
  batch_size INTEGER := 100000; -- Adjust batch size for performance
  total_updated INTEGER := 0;
  min_id BIGINT := -1;
  max_id BIGINT;
  rows_updated INTEGER;
BEGIN
  -- Get the maximum ID to know when to stop
  SELECT COALESCE(MAX(id), 0) INTO max_id FROM plt_events;
  
  RAISE NOTICE 'Starting batched backfill of event_timestamp, amount_value, and amount_decimals. Processing IDs from % to %', min_id, max_id;

  WHILE min_id < max_id LOOP
    WITH updated AS (
      SELECT
        e.id AS event_id,
        b.slot_time AS event_timestamp,
        CASE 
          WHEN e.token_event ? 'amount' AND e.token_event->'amount' ? 'value' 
          THEN (e.token_event->'amount'->>'value')::numeric
          ELSE NULL
        END AS amount_value,
        CASE 
          WHEN e.token_event ? 'amount' AND e.token_event->'amount' ? 'decimals' 
          THEN (e.token_event->'amount'->>'decimals')::int
          ELSE NULL
        END AS amount_decimals
      FROM
        plt_events e
        JOIN transactions t ON e.transaction_index = t.index
        JOIN blocks b ON t.block_height = b.height
      WHERE
        e.id > min_id
        AND e.id <= min_id + batch_size
      ORDER BY e.id
    )
    UPDATE plt_events
    SET 
      event_timestamp = updated.event_timestamp,
      amount_value = updated.amount_value,
      amount_decimals = updated.amount_decimals
    FROM updated
    WHERE plt_events.id = updated.event_id;

    GET DIAGNOSTICS rows_updated = ROW_COUNT;
    total_updated := total_updated + rows_updated;
    min_id := min_id + batch_size;
    
    RAISE NOTICE 'Updated % rows in this batch. Total updated: %. Progress: %/%', rows_updated, total_updated, min_id, max_id;

    -- Optional: pause to reduce load
    -- PERFORM pg_sleep(0.1);
  END LOOP;

  RAISE NOTICE 'Finished backfilling all columns. Total rows updated: %', total_updated;
END $$;

--  Drop temporary indexes after backfill
DROP INDEX IF EXISTS idx_temp_plt_events_backfill;
DROP INDEX IF EXISTS idx_temp_transactions_index;
DROP INDEX IF EXISTS idx_temp_blocks_height;


-- Create index on plt_accounts for faster queries Top Account amount holders
CREATE INDEX IF NOT EXISTS idx_token_amount_desc
ON plt_accounts (token_index, amount DESC);



-- Create table for transfer metrics PLT with cumulative data
CREATE TABLE metrics_specific_plt_transfer (
id BIGSERIAL PRIMARY KEY,
event_timestamp TIMESTAMPTZ NOT NULL,
token_index BIGINT NOT NULL,
cumulative_transfer_count BIGINT NOT NULL DEFAULT 0,
cumulative_transfer_amount NUMERIC NOT NULL DEFAULT 0
);

-- Create index on metrics_specific_plt_transfer for faster queries
CREATE INDEX IF NOT EXISTS idx_metrics_plt_transfer_token_time
ON metrics_specific_plt_transfer (token_index, event_timestamp DESC)
INCLUDE (cumulative_transfer_count, cumulative_transfer_amount);

-- Backfill metrics_specific_plt_transfer table with cumulative data 
DO $$
DECLARE
    batch_size INTEGER := 50000; -- Larger batch for 
    total_processed INTEGER := 0;
    min_id BIGINT := -1;
    max_id BIGINT;
    rows_inserted INTEGER;
BEGIN
    -- Get the maximum ID to know when to stop
    SELECT COALESCE(MAX(id), 0) INTO max_id FROM plt_events WHERE event_type = 'Transfer';
    RAISE NOTICE 'Starting backfill of metrics_specific_plt_transfer. Processing IDs from % to %', min_id, max_id;

    WHILE min_id < max_id LOOP
        WITH transfer_events AS (
            SELECT
                e.id,
                b.slot_time AS event_timestamp,
                e.token_index,
                (e.token_event->'amount'->>'value')::numeric AS amount_value,
                ROW_NUMBER() OVER (PARTITION BY e.token_index ORDER BY b.slot_time, e.id) as row_num
            FROM
                plt_events e
                JOIN transactions t ON e.transaction_index = t.index
                JOIN blocks b ON t.block_height = b.height
            WHERE
                e.event_type = 'Transfer'
                AND e.id > min_id 
                AND e.id <= min_id + batch_size
                AND e.token_event ? 'amount'
                AND e.token_event->'amount' ? 'value'
        ),
        cumulative_data AS (
            SELECT
                te.event_timestamp,
                te.token_index,
                te.row_num AS cumulative_transfer_count,
                SUM(te.amount_value) OVER (
                    PARTITION BY te.token_index 
                    ORDER BY te.event_timestamp, te.id
                    ROWS UNBOUNDED PRECEDING
                ) AS cumulative_transfer_amount
            FROM transfer_events te
        ),
        -- Get previous cumulative values for each token
        token_offsets AS (
            SELECT DISTINCT
                cd.token_index,
                COALESCE(prev.cumulative_transfer_count, 0) AS prev_count,
                COALESCE(prev.cumulative_transfer_amount, 0) AS prev_amount
            FROM cumulative_data cd
            LEFT JOIN LATERAL (
                SELECT cumulative_transfer_count, cumulative_transfer_amount
                FROM metrics_specific_plt_transfer ptm
                WHERE ptm.token_index = cd.token_index
                ORDER BY ptm.event_timestamp DESC, ptm.id DESC
                LIMIT 1
            ) prev ON true
        )
        INSERT INTO metrics_specific_plt_transfer (
            event_timestamp, token_index, 
            cumulative_transfer_count, cumulative_transfer_amount
        )
        SELECT
            cd.event_timestamp,
            cd.token_index,
            cd.cumulative_transfer_count + toff.prev_count,
            cd.cumulative_transfer_amount + toff.prev_amount
        FROM cumulative_data cd
        JOIN token_offsets toff ON cd.token_index = toff.token_index;

        GET DIAGNOSTICS rows_inserted = ROW_COUNT;
        total_processed := total_processed + rows_inserted;
        min_id := min_id + batch_size;

        RAISE NOTICE 'Processed batch: % rows inserted. Total processed: %. Progress: %/%', 
                    rows_inserted, total_processed, min_id, max_id;

        PERFORM pg_sleep(0.1);
    END LOOP;

    RAISE NOTICE 'Finished backfilling metrics_specific_plt_transfer. Total rows processed: %', total_processed;
END $$;


-- Create unified PLT metrics table combining event counts and transfer amounts
CREATE TABLE IF NOT EXISTS metrics_plt (
    event_timestamp TIMESTAMPTZ PRIMARY KEY,
    cumulative_event_count BIGINT NOT NULL DEFAULT 0,
    cumulative_transfer_amount NUMERIC NOT NULL DEFAULT 0, -- Normalized value for transfer amounts (value/10^decimals)
    unique_account_count BIGINT NOT NULL DEFAULT 0 -- Count of unique accounts involved in PLT activities
);

-- Create index on metrics_plt for faster queries
CREATE INDEX IF NOT EXISTS idx_metrics_plt_time
ON metrics_plt (event_timestamp DESC)
INCLUDE (cumulative_event_count, cumulative_transfer_amount, unique_account_count);

-- Backfill metrics_plt with combined event counts and transfer amounts
DO $$
DECLARE
    batch_size INTEGER := 50000; -- Process events in batches
    total_processed INTEGER := 0;
    min_id BIGINT := -1;
    max_id BIGINT;
    rows_inserted INTEGER;
BEGIN
    -- Get the maximum ID to know when to stop
    SELECT COALESCE(MAX(id), 0) INTO max_id FROM plt_events;
    RAISE NOTICE 'Starting backfill of metrics_plt. Processing IDs from % to %', min_id, max_id;

    WHILE min_id < max_id LOOP
        WITH all_events_data AS (
            SELECT
                e.id,
                b.slot_time AS event_timestamp,
                e.event_type,
                e.token_event,
                CASE 
                    WHEN e.event_type = 'Transfer' AND e.token_event ? 'amount' AND e.token_event->'amount' ? 'value' AND e.token_event->'amount' ? 'decimals'
                    THEN (e.token_event->'amount'->>'value')::numeric / POWER(10, (e.token_event->'amount'->>'decimals')::numeric)
                    ELSE 0
                END AS normalized_amount,
                ROW_NUMBER() OVER (ORDER BY b.slot_time, e.id) as event_row_num
            FROM
                plt_events e
                JOIN transactions t ON e.transaction_index = t.index
                JOIN blocks b ON t.block_height = b.height
            WHERE
                e.id > min_id 
                AND e.id <= min_id + batch_size
        ),
        account_extraction AS (
            SELECT
                aed.*,
                CASE 
                    WHEN aed.event_type = 'Transfer' AND aed.token_event ? 'from' AND aed.token_event->'from' ? 'address'
                    THEN aed.token_event->'from'->>'address'
                    WHEN aed.event_type IN ('Mint', 'Burn') AND aed.token_event ? 'target' AND aed.token_event->'target' ? 'address'
                    THEN aed.token_event->'target'->>'address'
                    ELSE NULL
                END AS from_address,
                CASE 
                    WHEN aed.event_type = 'Transfer' AND aed.token_event ? 'to' AND aed.token_event->'to' ? 'address'
                    THEN aed.token_event->'to'->>'address'
                    ELSE NULL
                END AS to_address
            FROM all_events_data aed
        ),
        running_totals AS (
            SELECT
                aed.event_timestamp,
                aed.event_row_num,
                aed.normalized_amount,
                SUM(aed.normalized_amount) OVER (
                    ORDER BY aed.event_timestamp, aed.id
                    ROWS UNBOUNDED PRECEDING
                ) AS running_transfer_amount
            FROM all_events_data aed
        ),
        aggregated_data AS (
            SELECT
                rt.event_timestamp,
                MAX(rt.event_row_num) AS cumulative_event_count,
                MAX(rt.running_transfer_amount) AS cumulative_transfer_amount_in_batch
            FROM running_totals rt
            GROUP BY rt.event_timestamp
        ),
        -- Get previous cumulative values
        offsets AS (
            SELECT 
                COALESCE(prev.cumulative_event_count, 0) AS prev_event_count,
                COALESCE(prev.cumulative_transfer_amount, 0) AS prev_transfer_amount,
                COALESCE(prev.unique_account_count, 0) AS prev_unique_accounts
            FROM (SELECT 1) dummy
            LEFT JOIN LATERAL (
                SELECT cumulative_event_count, cumulative_transfer_amount, unique_account_count
                FROM metrics_plt mp
                ORDER BY mp.event_timestamp DESC
                LIMIT 1
            ) prev ON true
        ),
        -- Calculate cumulative unique accounts across all events processed so far
        cumulative_unique_accounts AS (
            SELECT COUNT(DISTINCT all_account_addresses.account_address) AS total_unique_accounts
            FROM (
                SELECT DISTINCT ae.from_address AS account_address
                FROM account_extraction ae
                WHERE ae.from_address IS NOT NULL
                UNION
                SELECT DISTINCT ae.to_address AS account_address
                FROM account_extraction ae
                WHERE ae.to_address IS NOT NULL
                UNION
                -- Include all previously processed accounts from earlier batches
                SELECT DISTINCT 
                    CASE 
                        WHEN e_prev.event_type = 'Transfer' AND e_prev.token_event ? 'from' AND e_prev.token_event->'from' ? 'address'
                        THEN e_prev.token_event->'from'->>'address'
                        WHEN e_prev.event_type IN ('Mint', 'Burn') AND e_prev.token_event ? 'target' AND e_prev.token_event->'target' ? 'address'
                        THEN e_prev.token_event->'target'->>'address'
                        ELSE NULL
                    END AS account_address
                FROM plt_events e_prev
                WHERE e_prev.id <= min_id
                AND (
                    (e_prev.event_type = 'Transfer' AND e_prev.token_event ? 'from' AND e_prev.token_event->'from' ? 'address') OR
                    (e_prev.event_type IN ('Mint', 'Burn') AND e_prev.token_event ? 'target' AND e_prev.token_event->'target' ? 'address')
                )
                UNION
                SELECT DISTINCT 
                    e_prev.token_event->'to'->>'address' AS account_address
                FROM plt_events e_prev
                WHERE e_prev.id <= min_id
                AND e_prev.event_type = 'Transfer' 
                AND e_prev.token_event ? 'to' 
                AND e_prev.token_event->'to' ? 'address'
            ) all_account_addresses
            WHERE all_account_addresses.account_address IS NOT NULL
        )
        INSERT INTO metrics_plt (
            event_timestamp, cumulative_event_count, cumulative_transfer_amount, unique_account_count
        )
        SELECT
            ad.event_timestamp,
            ad.cumulative_event_count + off.prev_event_count,
            ad.cumulative_transfer_amount_in_batch + off.prev_transfer_amount,
            cua.total_unique_accounts
        FROM aggregated_data ad
        CROSS JOIN offsets off
        CROSS JOIN cumulative_unique_accounts cua
        ON CONFLICT (event_timestamp) DO UPDATE 
        SET 
            cumulative_event_count = GREATEST(metrics_plt.cumulative_event_count, EXCLUDED.cumulative_event_count),
            cumulative_transfer_amount = GREATEST(metrics_plt.cumulative_transfer_amount, EXCLUDED.cumulative_transfer_amount),
            unique_account_count = GREATEST(metrics_plt.unique_account_count, EXCLUDED.unique_account_count);

        GET DIAGNOSTICS rows_inserted = ROW_COUNT;
        total_processed := total_processed + rows_inserted;
        min_id := min_id + batch_size;

        RAISE NOTICE 'Processed batch: % rows inserted. Total processed: %. Progress: %/%', 
                    rows_inserted, total_processed, min_id, max_id;

        PERFORM pg_sleep(0.1);
    END LOOP;

    RAISE NOTICE 'Finished backfilling metrics_plt. Total rows processed: %', total_processed;
END $$;
