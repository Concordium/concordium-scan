create table graphql_accounts
(
    id                bigint primary key,
    base_address      text      not null,
    canonical_address text      not null,
    ccd_amount        bigint    not null,
    created_at        timestamp not null
);

create unique index graphql_accounts_base_address_uindex
    on graphql_accounts (base_address);

create unique index graphql_accounts_canonical_address_uindex
    on graphql_accounts (canonical_address);

