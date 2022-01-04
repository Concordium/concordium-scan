alter table block
    add column finalization_data_block_pointer  bytea null,
    add column finalization_data_index          bigint null,
    add column finalization_data_delay          bigint null;