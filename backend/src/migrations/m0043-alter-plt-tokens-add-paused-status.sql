-- 1. Create a partial covering index to quickly locate the latest Pause/Unpause
--    for each token. The DESC on transaction_index ensures the newest event comes first.
CREATE INDEX IF NOT EXISTS idx_pause_unpause_latest
ON plt_events (token_index, transaction_index DESC)
INCLUDE (token_module_type)
WHERE token_module_type IN ('Pause','Unpause');

-- 2. Add the paused column with a default value.
ALTER TABLE plt_tokens ADD COLUMN IF NOT EXISTS paused BOOLEAN NOT NULL DEFAULT FALSE;

-- 3. Backfill the paused column using the latest Pause/Unpause event per token.
--    DISTINCT ON combined with the index ensures efficient retrieval.
WITH latest_events AS (
    SELECT DISTINCT ON (pe.token_index)
        pe.token_index,
        pe.token_module_type
    FROM plt_events pe
    WHERE pe.token_module_type IN ('Pause','Unpause')
    ORDER BY pe.token_index, pe.transaction_index DESC
)
UPDATE plt_tokens pt
SET paused = (le.token_module_type = 'Pause')  -- TRUE if Pause, FALSE if Unpause
FROM latest_events le
WHERE pt.index = le.token_index
  AND pt.paused <> (le.token_module_type = 'Pause'); -- avoid unnecessary updates

-- 4. Drop the index if it's not useful for future queries.
DROP INDEX IF EXISTS idx_pause_unpause_latest;
