create table graphql_payday_status
(
    id               int primary key generated always as identity,
    next_payday_time timestamp not null
)