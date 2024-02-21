/*
 Creates an index for rewards on graphql_account_statement_entries which is used by view graphql_account_rewards
 */
create index account_statement_entries_account_id_index_rewards_index on graphql_account_statement_entries (account_id, index) where entry_type >= 6;
