create table graphql_account_release_schedule
(
    account_id     bigint not null,
    transaction_id bigint not null,
    schedule_index int not null,
    timestamp      timestamp not null,
    amount         bigint not null,
    PRIMARY KEY (account_id, timestamp, transaction_id, schedule_index)
);

create index graphql_account_release_schedule_timestamp_amount
    on graphql_account_release_schedule (timestamp, amount);