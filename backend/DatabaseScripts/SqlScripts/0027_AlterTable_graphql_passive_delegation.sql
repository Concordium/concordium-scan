alter table graphql_passive_delegation
    add column delegated_stake bigint null;

update graphql_passive_delegation
set delegated_stake = 0;

alter table graphql_passive_delegation
    alter column delegated_stake set not null;
