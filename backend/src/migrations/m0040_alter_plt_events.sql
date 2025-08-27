
-- Create index on plt_accounts for faster queries Top Account amount holders
CREATE INDEX IF NOT EXISTS idx_token_amount_desc
ON plt_accounts (token_index, amount DESC);



-- Create table for transfer metrics PLT with cumulative data
CREATE TABLE metrics_plt_transfer (
id BIGSERIAL PRIMARY KEY,
event_timestamp TIMESTAMPTZ NOT NULL,
token_index BIGINT NOT NULL,
cumulative_transfer_count BIGINT NOT NULL DEFAULT 0,
cumulative_transfer_amount NUMERIC NOT NULL DEFAULT 0
);

-- Create index on metrics_plt_transfer for faster queries (plt_transfer_metrics.sql) and for query pattern
--- `ORDER BY event_timestamp DESC` 
CREATE INDEX IF NOT EXISTS idx_metrics_plt_transfer_token_time
ON metrics_plt_transfer (token_index, event_timestamp DESC)
INCLUDE (cumulative_transfer_count, cumulative_transfer_amount);



-- Create unified PLT metrics table combining event counts and transfer amounts
CREATE TABLE IF NOT EXISTS metrics_plt (
    event_timestamp TIMESTAMPTZ PRIMARY KEY,
    cumulative_event_count BIGINT NOT NULL DEFAULT 0,
    cumulative_transfer_amount NUMERIC NOT NULL DEFAULT 0, -- Normalized value for transfer amounts (value/10^decimals)
    unique_account_count BIGINT NOT NULL DEFAULT 0 -- Count of unique accounts involved in PLT activities
);

-- Create index on metrics_plt for faster queries for pattern where event_timestamp DESC
CREATE INDEX IF NOT EXISTS idx_metrics_plt_time
ON metrics_plt (event_timestamp DESC)
INCLUDE (cumulative_event_count, cumulative_transfer_amount, unique_account_count);
