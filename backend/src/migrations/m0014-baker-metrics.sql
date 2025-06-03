-- Table used for showing graphs on amount of bakers
-- Each row represents a change in any value for this particular block

CREATE TABLE metrics_bakers
(
    block_height            BIGINT            PRIMARY KEY REFERENCES blocks(height),
    -- Total amount of occurring `bakersAdded` events at the given block.
    total_bakers_added      BIGINT            NOT NULL,
    -- Total amount of occurring `bakersRemoved` events at the given block.
    total_bakers_removed    BIGINT            NOT NULL
);
