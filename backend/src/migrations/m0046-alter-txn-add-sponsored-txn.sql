-- Alter transaction table to add sponsored transaction fields
ALTER TABLE transactions
ADD COLUMN IF NOT EXISTS sponsor_index BIGINT REFERENCES accounts(index),
ADD COLUMN IF NOT EXISTS sponsored_ccd_cost BIGINT;

-- Add SponsoredTransactionFee to account_statement_entry_type enum
ALTER TYPE account_statement_entry_type ADD VALUE IF NOT EXISTS 'SponsoredTransactionFee';

-- Create index on sponsor_index for efficient lookups of sponsored transactions
-- Similar to sender_index usage in JOINs and filtering
CREATE INDEX IF NOT EXISTS transactions_sponsor_index_idx ON transactions (sponsor_index) WHERE sponsor_index IS NOT NULL;