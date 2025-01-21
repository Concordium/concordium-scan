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
Note that the connected Concordium node needs to be caught-up to protocol 7 or above.
Note that only one instance of the indexer may run at any one time, as multiple instances would conflict with each other.

For instructions on how to use the indexer run:

```
ccdscan-indexer --help
```

<!-- TODO When service become stable: add documentation of arguments and environment variables. -->

## Database schema setup and migrations

To set up the database schema either from an empty database or migration from an older release of `ccdscan-indexer` run:

```
ccdscan-indexer --migrate
```

which will first make sure to have the database migrated before running the service.

In production it is recommended to run migrations with elevated privileges and then the indexer service with more restricted privileges, use:

```
ccdscan-indexer --migrate-only
```

which will run the migrations and then exit.

## Run the Indexer Service during development

Examples:

```
cargo run --bin ccdscan-indexer -- --node http://localhost:20001 --max-parallel-block-preprocessors 20 --max-processing-batch 20
cargo run --bin ccdscan-indexer -- --node https://grpc.testnet.concordium.com:20000 --max-parallel-block-preprocessors 20 --max-processing-batch 20
```

Note: Since the indexer puts a lot of load on the node, use your own local node whenever possible.
If using the public nodes, run the indexer as short as possible.

Both binaries read variables from a `.env` file if present in the directory, use `.env.template` in this project as the starting point.

## Run the GraphQL API Service

For instructions on how to use the API service run:

```
ccdscan-api --help
```

Running the service will first verify the database schema version is supported.
See `ccdscan-indexer` for how to update the database schema version.

## Run the GraphQL API Service during development

Example:

```
cargo run --bin ccdscan-api
```

<!-- TODO When service become stable: add documentation of arguments and environment variables. -->

### GraphiQL IDE

Starting the GraphQL API Service above will provide you an interface
(defaults to [127.0.0.1:8000](http://127.0.0.1:8000)) to execute GraphQL queries.

An example is shown below:

Query:

```
query ($after: String, $before: String, $first: Int, $last: Int) {
  blocks(after: $after, before: $before, first: $first, last: $last) {
    nodes {
      id
      bakerId
      blockHash
      blockHeight
      blockSlotTime
      finalized
      transactionCount
      __typename
    }
    pageInfo {
      startCursor
      endCursor
      hasPreviousPage
      hasNextPage
      __typename
    }
    __typename
  }
}

```

Variables:

```
{"first": 5}
```

![ExampleQuery](./ExampleQuery.png)

## Setup for development

To develop this service the following tools are required, besides the dependencies listed above:

- [Rust and cargo](https://rustup.rs/)
- [sqlx-cli](https://crates.io/crates/sqlx-cli)

This project has some dependencies tracked as Git submodules, so make sure to initialize these:

```
git submodule update --init --recursive
```

### Initialize External Dependencies

To set up the external dependencies required for development, including initializing the database schema, you can choose one of the following options:

#### Option 1: Start from Fresh

```bash
make setup && make
```

#### Option 2: Reuse an Already Initialized Database

```bash
make setup-env-with-password && make
```

* `make setup`: Performs a one-time setup to generate the password and store it in the .env file
* `make setup-env-with-password`: Asks for the password to the database and store it in the .env file
* `make`: Starts the database service, and inserts the required SQL structure.

Given that one wants follow the logs of the database:

```
docker logs -f postgres_db
```

### Running database migrations

To setup the database schema when developing the service run:

```
cargo run --bin ccdscan-indexer -- --migrate-only
```

NOTE: Having compile-time checked queries will cause issues, since the queries are invalid until the database have been properly migrated. This is done by _not_ having the `DATABASE_URL` environment variable set until after running the migrations.

### Introducing a new migration

Database migrations are tracked in the `src/migrations.rs` file and every version of the database schema are represented by the `SchemaVersion` enum in this file.

To introduce a new database schema version:

#. Extend the `SchemaVersion` enum with a variant representing changes since previous version.
#. Extend functions found in `impl SchemaVersion`.
#. Enable by setting `SchemaVersion::LATEST` to this new variant.

### Compile-time checked queries feature

Database queries can be checked at compile-time against the database. This ensures they are both syntactically
correct and type-safe. To enable this the set the `DATABASE_URL` environment variable (can be done in a `.env` file) which causes the queries to be validated against the provided database schema.

In order for the CI to verify the queries, developers must provide cached results of the checks.
These must be generated every time there is a change in a query or a new one is introduced.
This is done by running a live database with the latest database schema, having the `DATABASE_URL` environment variable set and execute the command (requires `sqlx-cli` installed):

```
cargo sqlx prepare
```

This will update the cache for the queries in the `.sqlx` folder.
