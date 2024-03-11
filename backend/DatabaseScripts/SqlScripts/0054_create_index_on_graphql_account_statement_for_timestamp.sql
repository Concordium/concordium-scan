/*
 Creates an index on time on graphql_account_statement_entries which is used by account statement export
 */
create index account_statement_entries_account_id_index_rewards_timestamp on graphql_account_statement_entries (account_id, time);
