-- Replace existing index with a composite index to optimize queries filtering on account_index
DROP INDEX IF EXISTS account_statements_entry_type_idx;
CREATE INDEX account_statements_entry_type_idx
  ON account_statements (account_index, id, entry_type);

