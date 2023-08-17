-- TODO : Provide summary
create table graphql_smart_contract_events
(
    block_height                bigint        not null,
    transaction_hash            text          not null,
    transaction_index           bigint        not null,
    event_index                 bigint        not null,
    contract_address_index      bigint        not null,
    contract_address_sub_index  bigint        not null,
    event                       json          not null,
    created_at                  TIMESTAMPTZ   not null,
    PRIMARY KEY (
                 block_height,
                 transaction_index,
                 event_index,
                 contract_address_index,
                 contract_address_sub_index
                )
);

create table graphql_module_reference_events
(
    block_height                bigint        not null,
    transaction_hash            text          not null,
    transaction_index           bigint        not null,
    event_index                 bigint        not null,
    module_reference            text          not null,
    created_at                  TIMESTAMPTZ   not null,    
    PRIMARY KEY (
                 block_height,
                 transaction_index,
                 event_index,
                 module_reference
        )
);

create table graphql_module_reference_smart_contract_link_events
(
    block_height                bigint      not null,
    transaction_hash            text        not null,
    transaction_index           bigint      not null,
    event_index                 bigint      not null,
    module_reference            text        not null,    
    contract_address_index      bigint      not null,
    contract_address_sub_index  bigint      not null,
    created_at                  TIMESTAMPTZ not null,
    PRIMARY KEY (
                 block_height,
                 transaction_index,
                 event_index,
                 module_reference,
                 contract_address_index,
                 contract_address_sub_index
        )
);

create table graphql_smart_contract_read_heights
(
    id                          bigint      primary key generated always as identity,
    block_height                bigint      not null,
    created_at                  TIMESTAMPTZ not null
)