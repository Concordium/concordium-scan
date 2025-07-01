-- DROP the old materialized views if they exist, as we are replacing the function for them
DROP MATERIALIZED VIEW IF EXISTS latest_baker_apy_30_days;
DROP MATERIALIZED VIEW IF EXISTS latest_baker_apy_7_days;


-- Function for computing the APY for every baker for the provided interval ending at the latest
-- processed block.
-- The WITH statment computes the paydays_per_year based on the epoch duration and reward period length,
-- capping it to a maximum of 8760 (hourly). For lower testing environments the paydays are often more
-- regular, so we cap it to a maximum of 8760 to avoid too high APY values which can cause FLOAT8 overflow.
CREATE OR REPLACE FUNCTION compute_latest_baker_apys(INTERVAL)
RETURNS TABLE(id BIGINT, total_apy FLOAT8, delegators_apy FLOAT8, baker_apy FLOAT8) AS '
WITH
    chain_parameter AS (
        SELECT
            id,
            LEAST(
                (EXTRACT(''epoch'' FROM INTERVAL ''1 year'') * 1000)
                    / (epoch_duration * reward_period_length),
                8760.0
            )::FLOAT8 AS paydays_per_year
        FROM public.current_chain_parameters
        WHERE id = true
    )
SELECT
    payday_baker_pool_stakes.baker AS id,
    public.geometric_mean(1 + public.apy(
        (payday_total_transaction_rewards
          + payday_total_baking_rewards
          + payday_total_finalization_rewards)::FLOAT8,
        (baker_stake + delegators_stake)::FLOAT8,
        paydays_per_year
    )) - 1 AS total_apy,
    public.geometric_mean(1 + public.apy(
        (payday_delegators_transaction_rewards
          + payday_delegators_baking_rewards
          + payday_delegators_finalization_rewards)::FLOAT8,
        NULLIF(delegators_stake, 0)::FLOAT8,
        paydays_per_year
        )) - 1 AS delegators_apy,
    public.geometric_mean(1 + public.apy(
        (payday_total_transaction_rewards
           - payday_delegators_transaction_rewards
           + payday_total_baking_rewards
           - payday_delegators_baking_rewards
           + payday_total_finalization_rewards
           - payday_delegators_finalization_rewards)::FLOAT8,
        baker_stake::FLOAT8,
        paydays_per_year
    )) - 1 AS baker_apy
FROM public.payday_baker_pool_stakes
    JOIN public.blocks ON blocks.height = payday_baker_pool_stakes.payday_block
    JOIN public.bakers_payday_pool_rewards
        ON blocks.height = bakers_payday_pool_rewards.payday_block_height
        AND pool_owner_for_primary_key = payday_baker_pool_stakes.baker
    JOIN chain_parameter ON chain_parameter.id = true
WHERE
    blocks.slot_time > (SELECT slot_time FROM public.blocks ORDER BY height DESC LIMIT 1) - $1
GROUP BY payday_baker_pool_stakes.baker;
' LANGUAGE SQL;




-- Materialized view with APYs for pool, computed from the rewards and stake of the last 30 days.
CREATE MATERIALIZED VIEW latest_baker_apy_30_days AS
    SELECT id, baker_apy, delegators_apy, total_apy
    FROM compute_latest_baker_apys('30 days'::INTERVAL);

-- Index to lookup APY for a particular baker efficient.
CREATE UNIQUE INDEX latest_baker_apy_30_days_baker_index ON latest_baker_apy_30_days (id);
-- Index to make sorting by baker_apy and then baker_id more efficient.
CREATE INDEX latest_baker_apy_30_days_baker_apy_index ON latest_baker_apy_30_days (baker_apy, id);
-- Index to make sorting by delegators_apy and then baker_id more efficient.
CREATE INDEX latest_baker_apy_30_days_delegators_apy_index ON latest_baker_apy_30_days (delegators_apy, id);

-- Materialized view with APYs for pool, computed from the rewards and stake of the last 7 days.
CREATE MATERIALIZED VIEW latest_baker_apy_7_days AS
    SELECT id, baker_apy, delegators_apy, total_apy
    FROM compute_latest_baker_apys('7 days'::INTERVAL);

-- Index to lookup APY for a particular baker efficient.
CREATE UNIQUE INDEX latest_baker_apy_7_days_baker_index ON latest_baker_apy_7_days (id);
-- Index to make sorting by baker_apy and then baker_id more efficient.
CREATE INDEX latest_baker_apy_7_days_baker_apy_index ON latest_baker_apy_7_days (baker_apy, id);
-- Index to make sorting by delegators_apy and then baker_id more efficient.
CREATE INDEX latest_baker_apy_7_days_delegators_apy_index ON latest_baker_apy_7_days (delegators_apy, id);