/*
Create table to keep track of migration jobs for main import flow. 
*/
create table graphql_main_migration_jobs
(
    job                         text        primary key,
    created_at                  TIMESTAMPTZ not null
);
