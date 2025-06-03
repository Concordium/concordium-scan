-- Used for account metrics when we have to make a mapping from accounts to blocks
CREATE INDEX accounts_transaction_index
  ON accounts(transaction_index);
