-- Alter plt_accounts to add NOT NULL constraint on amount and decimal
ALTER TABLE plt_accounts
ALTER COLUMN amount SET NOT NULL,
ALTER COLUMN decimal SET NOT NULL; 
