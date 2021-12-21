create table baking_reward
(
    block_id bigint not null,
    index    int    not null,
    address  bytea  not null,
    amount   bigint not null,
    PRIMARY KEY (block_id, index)
);
