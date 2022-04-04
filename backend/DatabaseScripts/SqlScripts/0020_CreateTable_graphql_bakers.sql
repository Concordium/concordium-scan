create table graphql_bakers
(
    id                     bigint primary key,
    active_restake_rewards bool      null,
    active_pending_change  json      null,
    removed_timestamp      timestamp null
);
