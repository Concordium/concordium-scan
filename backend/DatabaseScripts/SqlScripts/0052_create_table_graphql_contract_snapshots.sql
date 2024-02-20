/*
 Table used for aggregates state of contracts.
 */
create table graphql_contract_snapshot (
    block_height                bigint        not null,
    contract_address_index      bigint        not null,
    contract_address_subindex   bigint        not null,
    contract_name               text          not null,
    module_reference            text          not null,
    amount                      bigint        not null,
    source                      int           not null,
    created_at                  TIMESTAMPTZ   not null,
    PRIMARY KEY (
                 block_height,
                 contract_address_index,
                 contract_address_subindex
        )    
);

