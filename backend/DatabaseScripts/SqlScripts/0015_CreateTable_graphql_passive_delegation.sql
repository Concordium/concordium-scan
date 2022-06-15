create table graphql_passive_delegation
(
    id                             int primary key generated always as identity,
    delegator_count                int     not null,
    delegated_stake                bigint  not null,
    delegated_stake_percentage     decimal not null,
    current_payday_delegated_stake bigint  not null
)