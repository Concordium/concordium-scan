# Introduction 
This is the backend of Concordium Scan.

# Getting Started
## Prerequisites
The following prerequisites must be met in order to run the test suite and/or run the backend locally:

* **C# IDE** 
    * Jetbrains Rider was used during development.

* **PostgreSQL 14 with extension TimescaleDB (community edition)** 
    * The TimescaleDB docker image (latest-pg14) was used during development.
    * Install with this configuration *(If other configuration values are used, then configuration in code must be changed accordingly)*
        * Port: 5432
        * User: postgres
        * Password: lingo-bingo

* **Concordium Node**
    * During development the Concordium nodes in the development environment (in Azure) were used from local development setup

## Configuring the application for local execution
The configuration file ``appsettings.Development.json`` contains the configuration values used when executing the application locally.

* **PostgresDatabase -> ConnectionString**: The connection string to the main database for the backend
* **PostgresDatabase -> ConnectionStringNodeCache**: The connection string to the Concordium Node cache
* **ConcordiumNodeGrpc**: The configuration values that determine which Concordium Node is used when importing data

## Configuring the unit test suite
A lot of unit tests require access to PostgreSQL. The connection strings used in the test suite are found in the class ``Tests.TestUtilities.DatabaseFixture``.

There is a single unit test class the runs test directly against a Concordium Node. These tests are all ignored by default and is only intended to do manual experimental tests against a Concordium Node. These tests are found in the class ``Tests.ConcordiumSdk.NodeApi.GrpcNodeClientTest``. To change which Concordium node is used in the tests, the values in the constructor of that class must simply be changed.

## Creating and upgrading the PostgreSQL database(s)
At startup, the backend will check if the databases exists and if all migrations have been applied. If not, the backend will create its own databases and/or apply all missing migrations automatically, thus making sure that the databases specified in the connection strings exist and have the correct schema.

Separate databases are created to run unit tests against. These databases are automatically created and migrated when running a unit test that accesses the database (ie. a test class using the DatabaseFixture type).

# Build and Test
TODO: Describe and show how to build your code and run the tests. 
