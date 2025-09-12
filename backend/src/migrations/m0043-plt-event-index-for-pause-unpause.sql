-- This index is designed to optimize queries that retrieve the latest Pause and Unpause status changes
-- for a specific token. It orders the results by transaction_index in descending order to ensure
-- that the most recent events are prioritized. The index includes additional columns to cover
-- common query patterns, reducing the need for lookups in the main table.
CREATE INDEX idx_pause_unpause_latest
ON plt_events (token_index, transaction_index DESC)
INCLUDE (id, event_type, token_module_type, token_event)
WHERE token_module_type IN ('Pause','Unpause');
