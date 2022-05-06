create table graphql_account_statement_entries
(
    account_id      bigint    not null,
    index           bigint generated always as identity,
    time            timestamp not null,
    entry_type      int       not null,
    amount          bigint    not null,
    account_balance bigint    not null,
    block_id        bigint    not null,
    transaction_id  bigint    null,
    PRIMARY KEY (account_id, index)
);
