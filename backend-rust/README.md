# CCDScan Backend

This is the backend for the [CCDScan](https://ccdscan.io/) Blockchain explorer for the [Concordium blockchain](https://concordium.com/).

The backend consists of two binaries:

- `ccdscan-indexer`: Traversing the chain indexing events into the database.
- `ccdscan-api`: Providing a GraphQL API for querying the database.

The service is split to allow for running several instances of the GraphQL API and while having a single instance of the indexer.

## Dependencies

To run the services, the following dependencies are required to be available on the system:

- PostgreSQL server 16

## Run the Indexer Service

For instructions how to use the indexer run:

```
cargo run --bin ccdscan-indexer -- --help
```

Example:

```
cargo run --bin ccdscan-indexer -- --node http://node.testnet.concordium.com:20000
cargo run --bin ccdscan-indexer -- --node https://grpc.testnet.concordium.com:20000
```

<!-- TODO When service become stable: add documentation of arguments and environment variables. -->

## Run the GraphQL API Service

For instructions how to use the API service run:

```
cargo run --bin ccdscan-api -- --help
```


Example:

```
cargo run --bin ccdscan-api
```

<!-- TODO When service become stable: add documentation of arguments and environment variables. -->


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

### Database migrations

Database migrations are tracked in the `migrations` directory. To introduce a new one run:

```
sqlx migrate add '<description>'
```

where `<description>` is replaced by a short description of the nature of the migration.

This will create two files in the directory:

- `<database-version>_<description>.up.sql` for the SQL code to bring the database up from the previous version.
- `<database-version>_<description>.down.sql` for the SQL code reverting back to the previous version.

Note: if you want to restart the database with fresh data, delete the data in the `data` folder:

```
sudo rm -r data/
```