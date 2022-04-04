create table graphql_bakers
(
    id                      bigint primary key,
    active_staked_amount    bigint    null,
    active_restake_earnings bool      null,
    active_pending_change   json      null,
    removed_timestamp       timestamp null
);
