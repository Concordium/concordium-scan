create table graphql_baker_transactions
(
    baker_id       bigint not null,
    index          bigint generated always as identity,
    transaction_id bigint not null,
    PRIMARY KEY (baker_id, index)
);
