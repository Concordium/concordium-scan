-- Add slot_time column account statements table

ALTER TABLE account_statements
ADD COLUMN slot_time TIMESTAMPTZ;

-- add the slot_time from the blocks table for each account statement
-- This will be done in batches to avoid locking the table for too long
DO $$
DECLARE
    batch_size INTEGER := 1000000;
    rows_updated INTEGER := 0;
BEGIN
    LOOP
        WITH to_update AS (
            SELECT a.ctid AS ctid, b.slot_time
            FROM account_statements a
            JOIN blocks b ON a.block_height = b.height
            WHERE a.slot_time IS NULL
            LIMIT batch_size
        )
        UPDATE account_statements a
        SET slot_time = to_update.slot_time
        FROM to_update
        WHERE a.ctid = to_update.ctid;

        GET DIAGNOSTICS rows_updated = ROW_COUNT;
        EXIT WHEN rows_updated = 0;

        RAISE NOTICE 'Updated % rows', rows_updated;
    END LOOP;
END $$;



-- Add index for efficient lookup of account_statements by slot_time descending order
CREATE INDEX idx_account_statements_covering_slot_time_desc ON account_statements(account_index, slot_time desc)
INCLUDE (amount, account_balance, entry_type, slot_time);

-- Ensure the slot_time column is set to NOT NULL after populating it
ALTER TABLE account_statements
ALTER COLUMN slot_time SET NOT NULL;