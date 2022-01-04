create unique index transaction_summary_block_id_transaction_index_uindex
    on transaction_summary (block_id, transaction_index);