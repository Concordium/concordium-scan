alter table graphql_passive_delegation
    add column delegated_stake_percentage decimal null;

update graphql_passive_delegation
set delegated_stake_percentage = 0;

alter table graphql_passive_delegation
    alter column delegated_stake_percentage set not null;
