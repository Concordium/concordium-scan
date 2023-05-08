create table graphql_token_transactions (
    id bigint primary key generated always as identity,
    contract_index bigint not null,
    contract_subindex bigint not null,
    transaction_id bigint not null,
    token_id text not null,
    data jsonb not null
);
create index graphql_token_transactions_token_id on graphql_token_transactions(contract_index, contract_subindex, token_id);