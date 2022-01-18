create table graphql_finalization_rewards
(
    block_id bigint not null,
    index    int    not null,
    address  text   not null,
    amount   bigint not null,
    PRIMARY KEY (block_id, index)
);
