create table graphql_passive_delegation
(
    id              int primary key generated always as identity,
    delegator_count int not null
)