create table graphql_transactions
(
    id                   bigint primary key generated always as identity,
    block_id             bigint not null,
    index                int    not null,
    sender               text   null,
    transaction_hash     text   not null,
    micro_ccd_cost       bigint not null,
    energy_cost          bigint not null,
    transaction_type     text   not null,
    reject_reason        json   null
);

create index graphql_transactions_transaction_hash_index
    on graphql_transactions (transaction_hash text_pattern_ops);
