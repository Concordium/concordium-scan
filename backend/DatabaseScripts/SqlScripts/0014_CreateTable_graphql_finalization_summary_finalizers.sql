create table graphql_finalization_summary_finalizers
(
    block_id bigint not null,
    index    int    not null,
    baker_id bigint not null,
    weight   bigint not null,
    signed   bool   not null,
    PRIMARY KEY (block_id, index)
);
