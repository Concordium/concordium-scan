create table graphql_payday_status
(
    id                int primary key generated always as identity,
    payday_start_time timestamp not null,
    next_payday_time  timestamp not null
)