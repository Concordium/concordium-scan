-- Alter plt_accounts to add NOT NULL constraint on amount and decimal
ALTER TABLE plt_accounts
ALTER COLUMN amount SET NOT NULL,
ALTER COLUMN decimal SET NOT NULL; 

-- Index to speed up queries filtering by account_index, amount > 0, and token_index range
-- Excludes rows with amount = 0 to reduce index size (partial index)
CREATE INDEX idx_pa_account_token
ON plt_accounts (account_index, token_index)
WHERE amount > 0;
