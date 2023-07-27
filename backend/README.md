# Introduction 
This is the backend of Concordium Scan.

# Getting Started
## Prerequisites
The following prerequisites must be met in order to run the test suite and/or run the backend locally:

* **C# IDE** 
    * Jetbrains Rider was used during development.
    * The IDE should also install the .NET 6 SDK

* **PostgreSQL 14 with extension TimescaleDB (community edition)** 
    * The TimescaleDB docker image (latest-pg14) was used during development.
    * Install with this configuration *(If other configuration values are used, then configuration in code must be changed accordingly)*
        * Port: 5432
        * User: postgres
        * Password: password

* **Concordium Node**
    * During development the Concordium nodes in the development environment (in Azure) were used from local development setup

## Creating and upgrading the PostgreSQL database(s)
At startup, the backend will check if the databases exists and if all migrations have been applied. If not, the backend will create its own databases and/or apply all missing migrations automatically, thus making sure that the databases specified in the connection strings exist and has the correct schema.

Separate databases are created for the unit test suite. These databases are automatically created and migrated when running a unit test that accesses the database (ie. a test class using the DatabaseFixture type).

# Schema
Schema is validated using snapshot testing in test `Tests.Api.GraphQL.GivenGraphqlSchemaChanges_WhenBuild_ThenFailDueToSnapshotsNotMatched`.

# Run the backend locally
Once the prerequisites are met you can run the backend either from within the IDE or via a shell by issuing the "dotnet run" command in the Application directory.

## Configuring the application for local execution
The configuration file ``appsettings.Development.json`` contains the configuration values used when executing the application locally.

* **PostgresDatabase -> ConnectionString**: The connection string to the main database for the backend
* **PostgresDatabase -> ConnectionStringNodeCache**: The connection string to the Concordium Node cache
* **ConcordiumNodeGrpc**: The configuration values that determine which Concordium Node is used when importing data
* **NonCirculatingAccounts**: The foundation accounts which do not circulate the CCDs. This is primarily used in the calculation of Total Unlocked CCDs.  

# Run the tests locally
Once the prerequisites are met you can run the backend test suite either from within the IDE or via a shell by issuing the "dotnet test" command in the backend root directory.

## Configuring the unit test suite
A lot of unit tests require access to PostgreSQL. The connection strings used in the test suite are found in the class ``Tests.TestUtilities.DatabaseFixture``.

There is a single unit test class the runs test directly against a Concordium Node. These tests are all ignored by default and is only intended to do manual experimental tests against a Concordium Node. These tests are found in the class ``Tests.ConcordiumSdk.NodeApi.GrpcNodeClientTest``. To change which Concordium node is used in the tests, the values in the constructor of that class must simply be changed.

# Build a Docker image
There is a Dockerfile in the backend root directory. Build the image via a shell by issuing a "docker build ." command in the backend root directory.
Note: Docker needs to be installed for this task.

## Configuring for the docker image
When the backend is run in a docker container it uses the configuration values defined in the configuration file ``appsettings.json`` as default the configuration values. Each value in this file can be overridden via an environment variable.

For example, to override the PostgreSQL connection string for the main data:

```
{
  "PostgresDatabase": {
    "ConnectionString" : "Host=postgres;Port=5432;Database=ccdscan;User ID=postgres;Password=my-secret-password",
    ...
  },
  ...
}
```

You will need to set the environment variable named ``PostgresDatabase:ConnectionString``
