-- Migration: Add partial index for nonzero PLT holders by token

-- This index will speed up queries on plt_accounts that filter out zero-amount holders, e.g. for token holder lists.
CREATE INDEX IF NOT EXISTS idx_token_amount_desc_nonzero
ON plt_accounts (token_index, amount DESC)
WHERE amount > 0;