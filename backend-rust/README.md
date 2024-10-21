# CCDScan Backend

This is the backend for the [CCDScan](https://ccdscan.io/) Blockchain explorer for the [Concordium blockchain](https://concordium.com/).

The backend consists of two binaries:

- `ccdscan-indexer`: Traversing the chain, inserting data into the database as it is generated in the blockchain.
- `ccdscan-api`: Providing a GraphQL API for querying the database.

The service is split to allow for running several instances of the GraphQL API and while having a single instance of the indexer.

## Dependencies

To run the services, the following dependencies are required to be available on the system:

- PostgreSQL server 16

## Run the Indexer Service

The indexer talks to a Concordium node in order to gather data about the chain, which it then inserts into a PostgreSQL database.
Note that only one instance of the indexer may run at any one time, as multiple instances would conflict with each other.
The indexer purposefully performs insertions in a sequential manner, such that table indices can be strictly increasing without skipping any values.
Since no rows are ever deleted, this allows using the table indices to quickly calculate the number of rows in a table, without having to actually count all rows via a table scan.

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


## Setup for development

To develop this service the following tools are required, besides the dependencies listed above:

- [Rust and cargo](https://rustup.rs/)
- [sqlx-cli](https://crates.io/crates/sqlx-cli)

This project have some dependencies tracked as Git submodules, so make sure to initialize these:

```
git submodule update --init --recursive
```

### Running the database server

Both services depend on having a PostgreSQL server running, this can be done in several ways, but it can be done using [docker](https://www.docker.com/) with the command below:

```
docker run -p 5432:5432 -e 'POSTGRES_PASSWORD=example' -e 'POSTGRES_DB=ccd-scan' postgres:16
```

### Initializing a database

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

The project can now be built using `cargo build`

### Database migrations

Database migrations are tracked in the `migrations` directory.
To introduce a new one run:

```
sqlx migrate add '<description>'
```

where `<description>` is replaced by a short description of the nature of the migration.

This will create two files in the directory:

- `<database-version>_<description>.up.sql` for the SQL code to bring the database up from the previous version.
- `<database-version>_<description>.down.sql` for the SQL code reverting back to the previous version.
