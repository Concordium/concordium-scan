-- Ranking all bakers by lottery powers, staring with rank 1 for the baker with the highest lottery power.
ALTER TABLE bakers_payday_lottery_powers 
    ADD COLUMN payday_ranking_by_lottery_powers 
        BIGINT 
        NOT NULL
        DEFAULT 0; -- The default value is only used for adding the column to the table and will be overwritten at the next payday block.

-- Used when determining the total number of bakers in the above ranking.
CREATE INDEX payday_ranking_by_lottery_powers_idx ON bakers_payday_lottery_powers (payday_ranking_by_lottery_powers);
