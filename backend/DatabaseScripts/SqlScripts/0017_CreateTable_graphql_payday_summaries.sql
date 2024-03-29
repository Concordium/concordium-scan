﻿create table graphql_payday_summaries
(
    block_id                bigint primary key,
    payday_time             timestamp not null,
    payday_duration_seconds bigint    not null
);

create index on graphql_payday_summaries (payday_time);