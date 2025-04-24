ALTER TABLE affected_accounts
  DROP CONSTRAINT affected_accounts_pkey;

ALTER TABLE affected_accounts
  ADD CONSTRAINT affected_accounts_pkey PRIMARY KEY (account_index, transaction_index);