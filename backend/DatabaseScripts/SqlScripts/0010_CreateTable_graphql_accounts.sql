create table graphql_accounts
(
    id                          bigint primary key,
    base_address                text      not null,
    canonical_address           text      not null,
    ccd_amount                  bigint    not null,
    transaction_count           int       not null,
    created_at                  timestamp not null,
    delegation_restake_earnings bool      null,
    delegation_target_baker_id  bigint    null,
    delegation_pending_change   json      null
);

create unique index graphql_accounts_base_address_uindex
    on graphql_accounts (base_address);

create unique index graphql_accounts_canonical_address_uindex
    on graphql_accounts (canonical_address text_pattern_ops);

create index graphql_accounts_ccd_amount_index
    on graphql_accounts (ccd_amount);

create index graphql_accounts_transaction_count_index
    on graphql_accounts (transaction_count)
