create table graphql_accounts
(
    id         bigint primary key generated always as identity,
    address    text      not null,
    created_at timestamp not null
);

create unique index graphql_accounts_address_uindex
    on graphql_accounts (address);