CREATE TABLE bakers_ranking_by_total_stake(
    -- Baker/validator ID, corresponding to the account index.
    id
        BIGINT
        PRIMARY KEY,
    -- Ranking by total stake staring with 1 (baker with highest total stake).
    ranking_by_total_stake
        NUMERIC
        NOT NULL
);

-- Used when determining the total number of bakers in the above ranking.
CREATE INDEX ranking_by_total_stake_idx ON bakers_ranking_by_total_stake (ranking_by_total_stake);
