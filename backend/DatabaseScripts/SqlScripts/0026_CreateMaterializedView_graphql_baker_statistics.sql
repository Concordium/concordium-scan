create materialized view graphql_baker_statistics as
select 
    id as baker_id, 
    row_number() over (order by active_staked_amount desc nulls last) as baker_rank_by_stake, 
    active_staked_amount::decimal / (select bal_stats_total_amount from graphql_blocks order by id desc limit 1)::decimal as stake_percentage 
from graphql_bakers
order by id limit 10;