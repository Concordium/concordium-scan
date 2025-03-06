CREATE TABLE metrics_bakers
(
    block_height      BIGINT            REFERENCES blocks(height),
    total_baker_count INT               NOT NULL,
    bakers_added      INT               NOT NULL,
    bakers_removed    INT               NOT NULL
);


--SELECT create_hypertable('metrics_bakers', 'time');
