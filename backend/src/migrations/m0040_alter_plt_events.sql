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

