create table block
(
    id            bigint primary key generated always as identity,
    block_height  bigint not null,
    block_hash    bytea  not null,
    block_summary json   not null
);
