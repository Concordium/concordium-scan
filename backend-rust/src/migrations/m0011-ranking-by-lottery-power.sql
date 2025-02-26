-- Rename table and colum since the table is re-created with new data for each block and not only for each payday block now.
ALTER TABLE bakers_payday_lottery_powers 
RENAME TO bakers_lottery_powers;
ALTER TABLE bakers_lottery_powers
RENAME COLUMN payday_lottery_power TO lottery_power;

-- Ranking all bakers by lottery powers staring with rank 1 for the baker with the highest lottery power.
ALTER TABLE bakers_lottery_powers ADD COLUMN ranking_by_lottery_powers BIGINT NOT NULL;

-- Used when determining the total number of bakers in the above ranking.
CREATE INDEX ranking_by_lottery_powers_idx ON bakers_lottery_powers (ranking_by_lottery_powers);
