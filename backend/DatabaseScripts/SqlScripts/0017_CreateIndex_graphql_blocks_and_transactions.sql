create index graphql_blocks_block_hash_index
    on graphql_blocks (block_hash);

create index graphql_transactions_transaction_hash_index
    on graphql_transactions (transaction_hash);

