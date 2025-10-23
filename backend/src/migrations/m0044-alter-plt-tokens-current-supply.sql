ALTER TABLE plt_tokens
ADD COLUMN normalized_current_supply NUMERIC
GENERATED ALWAYS AS (
    (COALESCE(total_minted, 0) - COALESCE(total_burned, 0)) 
    / POWER(10::NUMERIC, decimal)
) STORED;


CREATE INDEX idx_tokens_normalized_current_supply_desc
    ON plt_tokens (normalized_current_supply DESC);

