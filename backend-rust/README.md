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

For instructions on how to use the indexer run:

```
ccdscan-indexer --help
```

## Run the Indexer Service during development

Examples:

```
cargo run --bin ccdscan-indexer -- --node http://localhost:20001 --max-parallel-block-preprocessors 20 --max-processing-batch 20
cargo run --bin ccdscan-indexer -- --node https://grpc.testnet.concordium.com:20000 --max-parallel-block-preprocessors 20 --max-processing-batch 20
```

Note: Since the indexer puts a lot of load on the node, use your own local node whenever possible.
If using the public nodes, run the indexer as short as possible.

<!-- TODO When service become stable: add documentation of arguments and environment variables. -->

## Run the GraphQL API Service

For instructions on how to use the API service run:

```
ccdscan-api --help
```

## Run the GraphQL API Service during development

Example:

```
cargo run --bin ccdscan-api
```

<!-- TODO When service become stable: add documentation of arguments and environment variables. -->

### GraphiQL IDE

Starting the GraphQL API Service above will provide you an interface
(usually at 127.0.0.1:8000) to execute GraphQL queries.

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

### `sqlx` features

- Feature 1:

The tool validates database queries at compile-time, ensuring they are both syntactically
correct and type-safe. To take advantage of this feature, you should run the following
command every time you update the database schema.
Run a live database with the new schema and execute the command:

```
cargo sqlx prepare
```

This will generate type metadata for the queries in the `.sqlx` folder.

- Feature 2:

If you want to drop the entire database and start with an empty database that uses the current schema, execute the command:

```
sqlx database reset --force
```
