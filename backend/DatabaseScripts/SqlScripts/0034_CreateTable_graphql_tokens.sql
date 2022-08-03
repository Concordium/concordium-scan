create table graphql_tokens (
    contract_index      bigint not null,
    contract_sub_index  bigint not null,
    token_id            text not null,
    metadata_url        text,
    total_supply        numeric not null,
    primary key (contract_index, contract_sub_index, token_id)
);