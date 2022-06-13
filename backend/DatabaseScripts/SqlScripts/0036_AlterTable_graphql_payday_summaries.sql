alter table graphql_payday_summaries
    add column payday_time             timestamp null,
    add column payday_duration_seconds bigint    null;
