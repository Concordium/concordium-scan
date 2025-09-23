ALTER TABLE plt_tokens
ADD COLUMN normalized_current_supply NUMERIC
GENERATED ALWAYS AS (
    (COALESCE(total_minted, 0) - COALESCE(total_burned, 0)) 
    / NULLIF(POWER(10::NUMERIC, decimal), 0)
) STORED;


CREATE INDEX idx_tokens_normalized_current_supply_desc
    ON plt_tokens (normalized_current_supply DESC);




CREATE OR REPLACE FUNCTION update_total_holders_incremental()
RETURNS TRIGGER AS $$
BEGIN
    -- INSERT: new account becomes a holder
    IF TG_OP = 'INSERT' THEN
        IF NEW.amount > 0 THEN
            UPDATE plt_tokens
            SET total_holders = total_holders + 1
            WHERE index = NEW.token_index;
        END IF;

    -- UPDATE: account changes amount
    ELSIF TG_OP = 'UPDATE' THEN
        -- crossed from non-holder to holder
        IF NEW.amount > 0 AND COALESCE(OLD.amount,0) <= 0 THEN
            UPDATE plt_tokens
            SET total_holders = total_holders + 1
            WHERE index = NEW.token_index;
        -- crossed from holder to non-holder
        ELSIF NEW.amount <= 0 AND OLD.amount > 0 THEN
            UPDATE plt_tokens
            SET total_holders = total_holders - 1
            WHERE index = NEW.token_index;
        END IF;

    -- DELETE: remove holder if amount > 0
    ELSIF TG_OP = 'DELETE' THEN
        IF OLD.amount > 0 THEN
            UPDATE plt_tokens
            SET total_holders = total_holders - 1
            WHERE index = OLD.token_index;
        END IF;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;



DROP TRIGGER IF EXISTS trg_update_holders ON plt_accounts;

CREATE TRIGGER trg_update_holders
AFTER INSERT OR UPDATE OR DELETE ON plt_accounts
FOR EACH ROW
EXECUTE FUNCTION update_total_holders_incremental();





UPDATE plt_tokens t
SET total_holders = sub.holders
FROM (
    SELECT token_index, COUNT(*) AS holders
    FROM plt_accounts
    WHERE amount > 0
    GROUP BY token_index
) sub
WHERE t.index = sub.token_index;
