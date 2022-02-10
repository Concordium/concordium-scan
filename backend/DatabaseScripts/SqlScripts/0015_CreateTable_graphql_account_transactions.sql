create table graphql_account_transactions
(
    account_id     bigint not null,
    index          bigint generated always as identity,
    transaction_id bigint not null,
    PRIMARY KEY (account_id, index)
);
