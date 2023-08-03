/*
Recreates graphql_pool_mean_apys from migration script 0031_CreateMaterializedView_graphql_pool_mean_apys.sql.
APY's values from view was mapped to postgres type 'numeric' which couldn't be casted to C# 'double' or 'decimal' 
used by Entity Framework when mapping.
Changes below cast calculated values to PostgreSQL 'double' values such that they can be mapped.

See link for PostgreSQL numeric datatypes
https://www.postgresql.org/docs/current/datatype-numeric.html
*/

DROP MATERIALIZED VIEW IF EXISTS graphql_pool_mean_apys;

create materialized view graphql_pool_mean_apys as
with fixed_timestamp (fixed_now) as (select current_timestamp at time zone ('utc')),

     data_30_days (payday_time, pool_id, total_apy, baker_apy, delegators_apy, total_stake, baker_stake, delegated_stake)
         as (select ps.payday_time,
                    pps.pool_id,
                    coalesce(total_apy, 0) + 1            as total_apy,
                    coalesce(baker_apy, 0) + 1            as baker_apy,
                    coalesce(delegators_apy, 0) + 1       as delegators_apy,
                    pps.baker_stake + pps.delegated_stake as total_stake,
                    pps.baker_stake,
                    pps.delegated_stake
             from graphql_payday_summaries ps
                      left join graphql_pool_payday_stakes pps on pps.payout_block_id = ps.block_id
                      left join metrics_payday_pool_rewards r on r.block_id = ps.block_id and r.pool_id = pps.pool_id
             where ps.payday_time between (select fixed_now - interval '30 days' from fixed_timestamp) and (select fixed_now from fixed_timestamp)),

     data_7_days (payday_time, pool_id, total_apy, baker_apy, delegators_apy, total_stake, baker_stake, delegated_stake)
         as (select payday_time, pool_id, total_apy, baker_apy, delegators_apy, total_stake, baker_stake, delegated_stake
             from data_30_days
             where payday_time > (select fixed_now - interval '7 days' from fixed_timestamp))

select a.pool_id,

       (select (exp(avg(ln(total_apy))) - 1)::double precision
        from data_30_days
        where pool_id = a.pool_id
          and total_stake > 0)     as total_apy_geom_mean_30_days,

       (select (exp(avg(ln(baker_apy))) - 1)::double precision
        from data_30_days
        where pool_id = a.pool_id
          and baker_stake > 0)     as baker_apy_geom_mean_30_days,

       (select (exp(avg(ln(delegators_apy))) - 1)::double precision
        from data_30_days
        where pool_id = a.pool_id
          and delegated_stake > 0) as delegators_apy_geom_mean_30_days,

       (select (exp(avg(ln(total_apy))) - 1)::double precision
        from data_7_days
        where pool_id = a.pool_id
          and total_stake > 0)     as total_apy_geom_mean_7_days,

       (select (exp(avg(ln(baker_apy))) - 1)::double precision
        from data_7_days
        where pool_id = a.pool_id
          and baker_stake > 0)     as baker_apy_geom_mean_mean_7_days,

       (select (exp(avg(ln(delegators_apy))) - 1)::double precision
        from data_7_days
        where pool_id = a.pool_id
          and delegated_stake > 0) as delegators_apy_geom_mean_mean_7_days

from (select distinct pool_id from data_30_days) a
order by pool_id;

create unique index graphql_pool_mean_apys_pool_id_index
    on graphql_pool_mean_apys (pool_id);

refresh materialized view graphql_pool_mean_apys