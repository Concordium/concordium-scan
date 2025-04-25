-- Replace existing primary key with a new one to optimize queries filtering on account_index
ALTER TABLE affected_accounts
  DROP CONSTRAINT affected_accounts_pkey;

ALTER TABLE affected_accounts
  ADD CONSTRAINT affected_accounts_pkey PRIMARY KEY (account_index, transaction_index);
