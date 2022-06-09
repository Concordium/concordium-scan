create materialized view graphql_baker_statistics as
select id        as baker_id,
       (case active_pool_total_stake is not null
            when true then row_number() over (order by active_pool_total_stake desc nulls last)::int
           end)  as active_baker_pool_rank_by_total_stake,
       (case active_pool_total_stake is not null
            when true then (select count(active_pool_total_stake) from graphql_bakers)::int
           end)  as active_baker_pool_count,
       round(active_pool_total_stake::decimal /
             (select bal_stats_total_amount from graphql_blocks order by id desc limit 1)::decimal,
             10) as active_pool_total_stake_percentage
from graphql_bakers;

create unique index graphql_baker_statistics_baker_id_index
    on graphql_baker_statistics (baker_id);
