alter table graphql_passive_delegation
    alter column id drop identity;

update graphql_passive_delegation set id = -1