create table transaction_summary
(
    id                   bigint primary key generated always as identity,
    block_id             bigint not null,
    block_height         bigint not null,
    block_hash           bytea  not null,
    transaction_index    int    not null,
    sender               bytea  null,
    transaction_hash     bytea  not null,
    cost                 bigint not null,
    energy_cost          bigint not null,
    transaction_type     int    not null,
    transaction_sub_type int    null,
    success_events       jsonb  null,
    reject_reason_type   text   null
);