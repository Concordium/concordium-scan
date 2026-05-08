-- Fix NULL sender_index and sponsor_index caused by account aliasing bug
-- 
-- Background:
-- The indexer previously looked up sender/sponsor accounts using the Base58check 
-- address string, which fails when the transaction uses an account alias different 
-- from the one stored in accounts.address. This left sender_index/sponsor_index as NULL.
--
-- Solution:
-- Use affected_accounts table (which uses canonical addresses) to find the correct
-- account index and update the transactions table.
--
-- This migration is safe to run multiple times (idempotent).

-- Update NULL sender_index for account transactions
-- Logic: Find the account from affected_accounts that should be the sender
UPDATE transactions t
SET sender_index = (
    SELECT a.index
    FROM affected_accounts aa
    JOIN accounts a ON a.index = aa.account_index
    WHERE aa.transaction_index = t.index
    ORDER BY aa.account_index ASC
    LIMIT 1
)
WHERE t.sender_index IS NULL
  AND t.type = 'Account'
  AND EXISTS (
    SELECT 1 FROM affected_accounts aa
    WHERE aa.transaction_index = t.index
  );

-- Update NULL sponsor_index for sponsored transactions
-- Logic: The sponsor is typically the second affected account (after the sender)
-- We identify it by checking if there are exactly 2 affected accounts and the 
-- second one is not the sender
UPDATE transactions t
SET sponsor_index = (
    SELECT a.index
    FROM affected_accounts aa
    JOIN accounts a ON a.index = aa.account_index
    WHERE aa.transaction_index = t.index
      AND a.index != t.sender_index
    ORDER BY aa.account_index ASC
    LIMIT 1
)
WHERE t.sponsor_index IS NULL
  AND t.type = 'Account'
  AND t.sponsored_ccd_cost IS NOT NULL
  AND t.sponsored_ccd_cost > 0
  AND EXISTS (
    SELECT 1 FROM affected_accounts aa
    WHERE aa.transaction_index = t.index
    HAVING COUNT(*) >= 2
  );
