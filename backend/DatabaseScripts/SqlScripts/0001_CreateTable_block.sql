create table block
(
    id                      bigint    primary key generated always as identity,
    block_height            bigint    not null,
    block_hash              bytea     not null,
    parent_block            bytea     not null,
    block_last_finalized    bytea     not null,
    genesis_index           int       not null,
    era_block_height        int       not null,
    block_receive_time      timestamp not null,
    block_arrive_time       timestamp not null,
    block_slot              int       not null,
    block_slot_time         timestamp not null,
    block_baker             int       null,
    finalized               bool      not null, 
    transaction_count       int       not null,
    transaction_energy_cost int       not null,
    transaction_size        int       not null,
    block_state_hash        bytea     not null,
    block_summary           jsonb     not null
);
