alter table metrics_pool_rewards
    add column total_staked_amount bigint null,
    add column baker_staked_amount bigint null,
    add column delegated_staked_amount bigint null;

update metrics_pool_rewards
set total_staked_amount = 0, baker_staked_amount = 0, delegated_staked_amount = 0 
where total_staked_amount is null;
