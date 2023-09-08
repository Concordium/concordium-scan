/*
Tables relevant for contract visualization. 
*/
create table graphql_contracts
(
    block_height                bigint        not null,
    transaction_hash            text          not null,
    transaction_index           bigint        not null,
    event_index                 bigint        not null,
    contract_address_index      bigint        not null,
    contract_address_sub_index  bigint        not null,
    creator                     text          not null,
    source                      int           not null,
    block_slot_time             TIMESTAMPTZ   not null,    
    created_at                  TIMESTAMPTZ   not null,
    PRIMARY KEY (
                 contract_address_index,
                 contract_address_sub_index
        )
);

create table graphql_contract_events
(
    block_height                bigint        not null,
    transaction_hash            text          not null,
    transaction_index           bigint        not null,
    event_index                 bigint        not null,
    contract_address_index      bigint        not null,
    contract_address_sub_index  bigint        not null,
    sender                      text          not null,    
    event                       json          not null,
    source                      int           not null,
    block_slot_time             TIMESTAMPTZ   not null,        
    created_at                  TIMESTAMPTZ   not null,
    PRIMARY KEY (
                 block_height,
                 transaction_index,
                 event_index,
                 contract_address_index,
                 contract_address_sub_index
                )
);

create table graphql_contract_reject_events
(
    block_height                bigint        not null,
    transaction_hash            text          not null,
    transaction_index           bigint        not null,
    event_index                 bigint        not null,
    contract_address_index      bigint        not null,
    contract_address_sub_index  bigint        not null,
    sender                      text          not null,
    reject_event                json          not null,
    source                      int           not null,
    block_slot_time             TIMESTAMPTZ   not null,
    created_at                  TIMESTAMPTZ   not null,
    PRIMARY KEY (
                 block_height,
                 transaction_index,
                 contract_address_index,
                 contract_address_sub_index
        )
);

create table graphql_contract_events
(
    block_height                bigint        not null,
    transaction_hash            text          not null,
    transaction_index           bigint        not null,
    event_index                 bigint        not null,
    contract_address_index      bigint        not null,
    contract_address_sub_index  bigint        not null,
    sender                      text          not null,    
    event                       json          not null,
    source                      int           not null,
    block_slot_time             TIMESTAMPTZ   not null,
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
    sender                      text          not null,    
    source                      int           not null,
    block_slot_time             TIMESTAMPTZ   not null,    
    created_at                  TIMESTAMPTZ   not null,    
    PRIMARY KEY (
                 module_reference
        )
);

create table graphql_module_reference_reject_events
(
    block_height                bigint        not null,
    transaction_hash            text          not null,
    transaction_index           bigint        not null,
    event_index                 bigint        not null,
    module_reference            text          not null,
    sender                      text          not null,
    reject_event                json          not null,    
    source                      int           not null,
    block_slot_time             TIMESTAMPTZ   not null,
    created_at                  TIMESTAMPTZ   not null,
    PRIMARY KEY (
                 block_height,
                 transaction_index,
                 module_reference
        )
);

create table graphql_module_reference_contract_link_events
(
    block_height                bigint      not null,
    transaction_hash            text        not null,
    transaction_index           bigint      not null,
    event_index                 bigint      not null,
    module_reference            text        not null,    
    contract_address_index      bigint      not null,
    contract_address_sub_index  bigint      not null,
    sender                      text        not null,    
    source                      int         not null,
    link_action                 int         not null,
    block_slot_time             TIMESTAMPTZ not null,
    created_at                  TIMESTAMPTZ not null,
    PRIMARY KEY (
                 block_height,
                 transaction_index,
                 event_index,
                 module_reference,
                 contract_address_index,
                 contract_address_sub_index,
                 link_action
        )
);

create table graphql_contract_read_heights
(
    id                          bigint      primary key generated always as identity,
    block_height                bigint      not null UNIQUE,
    source                      int         not null,
    created_at                  TIMESTAMPTZ not null
);

create table graphql_contract_jobs
(
    job                         text        primary key,
    created_at                  TIMESTAMPTZ not null
);