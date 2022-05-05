drop materialized view graphql_baker_statistics;

create materialized view graphql_baker_statistics as
select
    id as baker_id,
    row_number() over (order by active_staked_amount desc nulls last) as active_baker_rank_by_stake,
    (select count(active_staked_amount) from graphql_bakers) as active_baker_count,
    active_staked_amount::decimal / (select bal_stats_total_amount from graphql_blocks order by id desc limit 1)::decimal as active_stake_percentage
from graphql_bakers;

create unique index graphql_baker_statistics_baker_id_index
    on graphql_baker_statistics (baker_id);
