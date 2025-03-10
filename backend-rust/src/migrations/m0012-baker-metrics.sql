CREATE TABLE metrics_bakers
(
    index
        BIGINT GENERATED ALWAYS AS IDENTITY
        PRIMARY KEY,
    block_height            BIGINT            REFERENCES blocks(height),
    total_bakers_added      BIGINT            NOT NULL,
    total_bakers_removed    BIGINT            NOT NULL,
    total_bakers_resumed    BIGINT            NOT NULL,
    total_bakers_suspended  BIGINT            NOT NULL
);
