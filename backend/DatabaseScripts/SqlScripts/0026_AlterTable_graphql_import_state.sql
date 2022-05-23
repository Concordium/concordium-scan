alter table graphql_import_state
    add column passive_delegation_added bool null;

update graphql_import_state
set passive_delegation_added = false;