﻿drop index graphql_blocks_block_height_index;

create unique index graphql_blocks_block_height_index
    on graphql_blocks (block_height);