create index account_statement_entries_account_id_index
    on graphql_account_statement_entries (account_id);
create index account_statement_entries_entry_type_index
    on graphql_account_statement_entries (entry_type);
create index account_statement_entries_timestamp_index
    on graphql_account_statement_entries (time);
