drop materialized view graphql_baker_statistics;

create materialized view graphql_baker_statistics as
select id        as baker_id,
       (case active_staked_amount is not null
            when true then row_number() over (order by active_staked_amount desc nulls last)::int
           end)  as active_baker_rank_by_stake,
       (case active_staked_amount is not null
            when true then (select count(active_staked_amount) from graphql_bakers)::int
           end)  as active_baker_count,
       round(active_staked_amount::decimal /
             (select bal_stats_total_amount from graphql_blocks order by id desc limit 1)::decimal,
             10) as active_stake_percentage
from graphql_bakers;

create unique index graphql_baker_statistics_baker_id_index
    on graphql_baker_statistics (baker_id);
