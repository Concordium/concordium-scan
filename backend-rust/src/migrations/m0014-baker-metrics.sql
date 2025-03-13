-- Table used for showing graphs on amount of bakers
-- Each row represents a change in any value for this particular block

CREATE TABLE metrics_bakers
(
    block_height            BIGINT            PRIMARY KEY REFERENCES blocks(height),
    -- total amount of added accounts at this particular block
    total_bakers_added      BIGINT            NOT NULL,
    -- total amount of removed accounts at this particular block
    total_bakers_removed    BIGINT            NOT NULL
);
