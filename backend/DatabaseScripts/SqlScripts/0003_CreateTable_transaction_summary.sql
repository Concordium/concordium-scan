create table transaction_summary
(
    block_height         bigint not null,
    transaction_index    int    not null,
    sender               bytea  null,
    transaction_hash     bytea  not null,
    cost                 bigint not null,
    energy_cost          bigint not null,
    transaction_type     int    not null,
    transaction_sub_type int    not null,
    success_events       jsonb  null,
    reject_reason_type   text   null,
    primary key (block_height, transaction_index)
);