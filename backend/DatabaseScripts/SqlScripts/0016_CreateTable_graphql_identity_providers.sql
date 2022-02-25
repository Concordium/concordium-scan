create table graphql_identity_providers
(
    ip_identity    int primary key,
    name           text      not null,
    url            text      not null,
    description    text      not null
);

