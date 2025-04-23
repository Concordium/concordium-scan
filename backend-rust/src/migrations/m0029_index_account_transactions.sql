-- Used for account metrics when we have to make a mapping from accounts to transaction
CREATE INDEX accounts_transaction_index
  ON accounts(transaction_index);
