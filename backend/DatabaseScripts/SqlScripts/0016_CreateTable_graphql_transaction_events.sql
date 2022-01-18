create table graphql_transaction_events
(
    transaction_id bigint not null,
    index          int    not null,
    event          json   not null,
    PRIMARY KEY (transaction_id, index)
);
