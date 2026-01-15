-- Add composite indexes for PLT tokens pagination and sorting queries
-- These indexes significantly improve performance for all sorting/pagination scenarios

-- PRIMARY INDEXES FOR PAGINATION QUERIES
-- ======================================

-- 1. Index for AGE_DESC pagination (newest first)
--    Query pattern: ORDER BY index DESC WHERE paused = ? (optional)
--    Includes paused in index for filtered and unfiltered queries
CREATE INDEX IF NOT EXISTS idx_plt_tokens_index_desc_paused
    ON plt_tokens (index DESC, paused);

-- 2. Index for AGE_ASC pagination (oldest first)
--    Query pattern: ORDER BY index ASC WHERE paused = ? (optional)
CREATE INDEX IF NOT EXISTS idx_plt_tokens_index_asc_paused
    ON plt_tokens (index ASC, paused);

-- 3. Index for SUPPLY_DESC pagination (highest supply first)
--    Query pattern: ORDER BY normalized_current_supply DESC, index DESC WHERE paused = ? (optional)
--    Includes index as tie-breaker and paused for filtering
CREATE INDEX IF NOT EXISTS idx_plt_tokens_supply_desc_index_desc_paused
    ON plt_tokens (normalized_current_supply DESC, index DESC, paused);

-- 4. Index for SUPPLY_ASC pagination (lowest supply first)
--    Query pattern: ORDER BY normalized_current_supply ASC, index ASC WHERE paused = ? (optional)
CREATE INDEX IF NOT EXISTS idx_plt_tokens_supply_asc_index_asc_paused
    ON plt_tokens (normalized_current_supply ASC, index ASC, paused);

-- BOUNDS QUERY INDEXES (for has_previous_page/has_next_page)
-- ==========================================================

-- 5. Partial indexes for paused-only filtering
--    Query pattern: WHERE paused = true/false
--    Used when querying bounds with paused filter
CREATE INDEX IF NOT EXISTS idx_plt_tokens_paused_true
    ON plt_tokens (paused, index DESC)
    WHERE paused = true;

CREATE INDEX IF NOT EXISTS idx_plt_tokens_paused_false
    ON plt_tokens (paused, index DESC)
    WHERE paused = false;

-- NOTES
-- =====

-- 6. Token ID lookups
--    The UNIQUE constraint on token_id already creates an index automatically
--    No additional index needed for: WHERE token_id = ?

-- 7. Index from m0045 (idx_tokens_normalized_current_supply_desc)
--    The existing single-column index on normalized_current_supply DESC from m0045
--    is superseded by the composite indexes above (#3 and #4) which are more efficient
--    for pagination queries that need both supply sorting and index tie-breaking.
--    Consider dropping it if storage is a concern: 
--    DROP INDEX IF EXISTS idx_tokens_normalized_current_supply_desc;

-- PERFORMANCE IMPACT
-- ==================
-- - Without composite indexes: Sequential scan or multiple index lookups + sort operation
-- - With composite indexes: Direct index scan in the correct order, no separate sort needed
-- - Reduces query time complexity from O(n log n) to O(log n) for pagination
-- - Critical for large token datasets (thousands to millions of tokens)
-- - Each index typically 1-2% of table size, minimal storage overhead
-- - Significantly faster bounds queries with partial indexes on paused status
