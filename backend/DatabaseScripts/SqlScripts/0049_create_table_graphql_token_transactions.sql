create table graphql_token_events (
    id                          bigint  primary key generated always as identity,
    contract_address_index      bigint  not null,
    contract_address_subindex   bigint  not null,
    token_id                    text    not null,
    event                       json    not null
);
create index graphql_token_transactions_token_id on graphql_token_events(contract_address_index, contract_address_subindex, token_id);
