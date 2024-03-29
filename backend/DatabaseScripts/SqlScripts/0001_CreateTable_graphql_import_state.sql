﻿create table graphql_import_state
(
    id                                              int primary key generated always as identity,
    genesis_block_hash                              text      not null,
    max_imported_block_height                       bigint    not null,
    cumulative_accounts_created                     bigint    not null,
    cumulative_transaction_count                    bigint    not null,
    last_block_slot_time                            timestamp not null,
    max_block_height_with_updated_finalization_time bigint    not null,
    next_pending_baker_change_time                  timestamp null,
    last_genesis_index                              int       not null,
    total_baker_count                               int       not null,
    migration_to_baker_pools_completed              bool      not null,
    passive_delegation_added                        bool      not null
)