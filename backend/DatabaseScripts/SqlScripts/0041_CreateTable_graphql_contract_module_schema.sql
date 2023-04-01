create table graphql_contract_module_schema
(
    id          bigint primary key generated always as identity,
    module_ref  text not null,
    schema_hex  text null,
    schema_name text null
);
