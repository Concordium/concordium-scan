CREATE TABLE metrics_bakers
(
    block_height            BIGINT            PRIMARY KEY REFERENCES blocks(height),
    total_bakers_added      BIGINT            NOT NULL,
    total_bakers_removed    BIGINT            NOT NULL,
    total_bakers_resumed    BIGINT            NOT NULL,
    total_bakers_suspended  BIGINT            NOT NULL
);
