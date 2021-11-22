# Introduction 
This is the backend of Concordium Scan.

# Getting Started
## Prerequisites
The following software must be installed:
* PostgreSQL (version?)


## Creating and upgrading the PostgreSQL database(s)
At startup, the backend will check if the database exists and if it has had all migrations applied. If not, startup will be interrupted to ensure that the backend is not unintentionally run with an incompatible database.

The backend will create its own database and/or apply all missing migrations if the application is run with the command line argument migrate-db:

    Application.exe migrate-db

Database migration can also be initiated from within the Rider IDE by running the configuration `'Application: MigrateDb'`.

A separate database is created to run unit tests against. This database is automatically created and migrated when running a unit test that accesses the database (ie. a test class using the DatabaseFixture type).

# Build and Test
TODO: Describe and show how to build your code and run the tests. 
