
drop index graphql_blocks_block_hash_index;
create index graphql_blocks_block_hash_index
    on graphql_blocks (block_hash text_pattern_ops);

drop index graphql_transactions_transaction_hash_index;
create index graphql_transactions_transaction_hash_index
    on graphql_transactions (transaction_hash text_pattern_ops);

drop index graphql_accounts_canonical_address_uindex;
create unique index graphql_accounts_canonical_address_uindex
    on graphql_accounts (canonical_address text_pattern_ops);

