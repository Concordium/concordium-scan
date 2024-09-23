# CCDScan Backend

This is the backend for [CCDScan](https://ccdscan.io/) Blockchain explorer for the [Concordium blockchain](https://concordium.com/).

The backend consists of two binaries:

- `ccdscan-indexer`: Traversing the chain indexing events into the database.
- `ccdscan-api`: Running a GraphQL API for querying the database.

The service is split to allow for running several instances of the GraphQL API and while having a single instance of the indexer.

## Dependencies

To run the service, the following dependencies are required to be available on the system:

- PostgreSQL server 16

## Setup for development

To develop this service the following tools are required, besides the dependencies listed above:

- [Rust and cargo](https://rustup.rs/)
- [sqlx-cli](https://crates.io/crates/sqlx-cli)

Then set the environment variable `DATABASE_URL` pointing to the location of the SQL database, this can be done by creating a `.env` file within this directory.
Example:

```
# Postgres database connection used by sqlx-cli and this service.
DATABASE_URL=postgres://postgres:example@localhost/ccd-scan
```

With the environment variable `DATABASE_URL` set, use the `sqlx` CLI to setup the database and tables and run all the migrations:

```
sqlx migrate run
```

The project can now be build using `cargo build`

## Run the Indexer Service

For instructions how to use the indexer run:

```
ccdscan-indexer --help
```

<!-- TODO When service become stable: add documentation of arguments and environment variables. -->

## Run the GraphQL API Service

For instructions how to use the API service run:

```
ccdscan-api --help
```

<!-- TODO When service become stable: add documentation of arguments and environment variables. -->
