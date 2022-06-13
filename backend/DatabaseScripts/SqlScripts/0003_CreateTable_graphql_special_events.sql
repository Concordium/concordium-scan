create table graphql_special_events
(
    block_id                   bigint   not null,
    index                      bigint generated always as identity,
    type_id                    int      not null,
    foundation_account_address text     null,
    account_address            text     null,
    account_addresses          text[]   null,
    amounts                    bigint[] null,
    remainder                  bigint   null,
    transaction_fees           bigint   null,
    baker_reward               bigint   null,
    finalization_reward        bigint   null,
    old_gas_account            bigint   null,
    new_gas_account            bigint   null,
    passive_reward             bigint   null,
    foundation_charge          bigint   null,
    baker_id                   bigint   null,
    pool_owner                 bigint   null,

    PRIMARY KEY (block_id, index)
);
