create table graphql_import_state
(
    id                                              int primary key generated always as identity,
    genesis_block_hash                              text      not null,
    max_imported_block_height                       bigint    not null,
    cumulative_accounts_created                     bigint    not null,
    cumulative_transaction_count                    bigint    not null,
    last_block_slot_time                            timestamp not null,
    max_block_height_with_updated_finalization_time bigint    not null
)