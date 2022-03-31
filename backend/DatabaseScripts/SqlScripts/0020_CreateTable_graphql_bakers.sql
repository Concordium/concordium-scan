create table graphql_bakers
(
    id             bigint primary key,
    status         int  not null,
    pending_change json null
);
