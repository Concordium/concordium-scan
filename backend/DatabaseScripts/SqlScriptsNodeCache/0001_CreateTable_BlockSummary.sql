create table block_summary
(
    block_hash      text primary key,
    compressed_data bytea not null
)