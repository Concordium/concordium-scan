# CCDScan Backend

This is the backend for the [CCDScan](https://ccdscan.io/) Blockchain explorer for the [Concordium blockchain](https://concordium.com/).

The backend consists of two binaries:

- `ccdscan-indexer`: Traversing the chain, inserting data into the database as it is generated in the blockchain.
- `ccdscan-api`: Providing a GraphQL API for querying the database.

The service is split to allow for running several instances of the GraphQL API and while having a single instance of the indexer.

## Dependencies

To run the services, the following dependencies are required to be available on the system:

- PostgreSQL server 16 or higher

## Deploying new versions into production

A new database schema is either `destructive` (e.g. removing existing columns/tables) or `non-destructive` (e.g. adding new columns/tables).

If the new version of the services introduced a new database schema that is `non-destructive`, update the `indexer` service to the new version and run the database migration, then update all `api` services. 
If the new version of the services introduced a new database schema that is `destructive`, update all `api` services to the new version, then update the `indexer` service to the new version and run the database migration.

## Run the Indexer Service

The indexer talks to a Concordium node in order to gather data about the chain, which it then inserts into a PostgreSQL database.
Note that the connected Concordium node needs to be caught-up to protocol 7 or above.
Note that only one instance of the indexer may run at any one time, as multiple instances would conflict with each other.

### Configuring

The indexer service have several options for configuration, and these can be provided as command-line arguments or/and environment variables, where the command-line arguments take precedence.
By default the service also reads environment variables a `.env` directory if present in the current directory of the running process.

The file `.env.template` in this project repository can be used as a starting point.

The required configurations are:

- `--database-url <url>` (env `CCDSCAN_INDEXER_DATABASE_URL=<url>`): where `<url>` is the database connection in the format of a URL ex. `postgres://user:password@localhost/ccdscan`.
- `--node <url>` (env `CCDSCAN_INDEXER_GRPC_ENDPOINTS=<url>`): where `<url>` is the gRPC endpoint of a Concordium node on the relevant network.
  Multiple nodes can be provided by providing multiple `--node <url>` arguments (environment variable take a comma separated list of URLs).

For full list of configuration options for the indexer service run:

```
ccdscan-indexer --help
```

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

## Run the GraphQL API Service

The GraphQL API service is designed to be run independently of the indexer service, and provides the API directly from the database.
The service support running several instances at once, to allow for load-balancing and zero down time.

During startup the GraphQL API service will verify the compatibility of the current database schema version.
See `ccdscan-indexer` for how to update the database schema version.

### Configuring

The GraphQL API service have several options for configuration, and these can be provided as command-line arguments or/and environment variables, where the command-line arguments take precedence.
By default the service also reads environment variables a `.env` directory if present in the current directory of the running process.

The file `.env.template` in this project repository can be used as a starting point.

The required configurations are:

- `--database-url <url>` (env `CCDSCAN_API_DATABASE_URL=<url>`): where `<url>` is the database connection in the format of a URL ex. `postgres://user:password@localhost/ccdscan`.
  Only require read privileges for the database.

For full list of configuration options for the API service run:

```
ccdscan-api --help
```

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

## Contributing

### Setup

To develop this service the following tools are required, besides the dependencies listed above:

- [Rust and cargo](https://rustup.rs/)
- [sqlx-cli](https://crates.io/crates/sqlx-cli)

This project has some dependencies tracked as Git submodules, so make sure to initialize these:

```
git submodule update --init --recursive
```

The file `.env.template` in this project repository can be used as a starting point for the `.env`.
Make sure to enable compile-time checked quries by uncommenting the `DATABASE_URL` see section on this feature further below.

To quickly get a PostgreSQL database instance running using [docker.io](https://www.docker.com/) the following command can be used:

```
docker run -p 5432:5432 -e POSTGRES_PASSWORD=password -e POSTGRES_DB="ccdscan" --rm postgres
```

### Running database migrations

The `sqlx-cli` tool provides a handy command for setting up the empty database, make sure the `DATABASE_URL` points to the location of the database and run:

```
sqlx database create
```

To setup the database schema when developing the service run:

```
env SQLX_OFFLINE=true cargo run --bin ccdscan-indexer -- --migrate-only
```

NOTE: Having compile-time checked queries causes issues, since the queries are invalid until the database have been properly migrated, hence we disable these checks against the live database by setting `SQLX_OFFLINE=TRUE` during bootstrapping of the database.

### Run the Indexer Service during development

Assuming the `.env` file contains the required arguments and after the database has been migrated, the API service can be build and run using:

```
cargo run --bin ccdscan-indexer
```

Note: Since the indexer puts a lot of load on the gRPC of the Concordium node, use your own local node whenever possible and never use a node which is also used for block validation.
If using the public nodes, run the indexer as short as possible.

### Run the GraphQL API Service during development

Assuming the `.env` file contains the required arguments and after the database has been migrated, the API service can be build and run using:

```
cargo run --bin ccdscan-api
```

### Introducing a new migration

Database migrations are tracked in the `src/migrations.rs` file and every version of the database schema are represented by the `SchemaVersion` enum in this file.

To introduce a new database schema version:

- Extend the `SchemaVersion` enum with a variant representing changes since previous version.
- Extend functions found in `impl SchemaVersion`. Every required function should produce compile-time errors for the now missing variant.
- Enable the new schema by setting `SchemaVersion::LATEST` to the new variant.
- Evaluate if the minimum `SchemaVersion::API_SUPPORTED_SCHEMA_VERSION` needs to be set to the new variant (update only if the API service was updated and is not compatible with older database schemas anymore).

### Compile-time checked queries feature

Database queries are checked at compile-time against the database. This ensures they are both syntactically
correct and type-safe. To enable this the set the `DATABASE_URL` environment variable (can be done in a `.env` file) which causes the queries to be validated against the schema of the provided database.

In order for the CI to verify the queries, developers must provide cached results of the checks.
These must be generated every time there is a change in a query or a new one is introduced.
This is done by running a live database with the latest database schema, having the `DATABASE_URL` environment variable set and execute the command (requires `sqlx-cli` installed):

```
cargo sqlx prepare
```

This will update the cache for the queries in the `.sqlx` folder.
