create table metrics_bakers
(
    time              timestamp not null,
    total_baker_count int       not null,
    bakers_added      int       not null,
    bakers_removed    int       not null
);

SELECT create_hypertable('metrics_bakers', 'time');
