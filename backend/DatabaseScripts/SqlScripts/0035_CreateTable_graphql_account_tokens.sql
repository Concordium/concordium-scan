create table graphql_account_tokens (
    contract_index      bigint not null,
    contract_sub_index  bigint not null,
    token_id            text not null,
    account_id          bigint not null,
    balance             numeric not null,
    CONSTRAINT graphql_account_tokens_pk PRIMARY KEY (contract_index, contract_sub_index, token_id, account_id)
);