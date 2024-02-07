/*
Drop columns related to finalization summary in graphql_blocks and remove table. Currently finalization summaries are
queried from the block but one can just look at the latest finalized block hash on the block info returned from get
block info node query.
Changes are needed since finalization summaries isn't present in protocol 6. Using latest finalization block hash in
block info is a robust calculation in both old (before protocol 6) and new consensus protocol.  
 */

ALTER TABLE graphql_blocks
    DROP COLUMN finalization_data_block_pointer,
    DROP COLUMN finalization_data_index,
    DROP COLUMN finalization_data_delay;

DROP TABLE graphql_finalization_summary_finalizers;

CREATE INDEX metrics_blocks_block_height_index
    ON metrics_blocks (block_height);
