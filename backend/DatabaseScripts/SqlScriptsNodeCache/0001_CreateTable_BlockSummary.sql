create table block_summary
(
    block_hash text primary key,
    data       json not null
)