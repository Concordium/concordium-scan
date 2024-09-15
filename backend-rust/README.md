# CCDScan Backend

This is the backend for [CCDScan](https://ccdscan.io/) Blockchain explorer for the [Concordium blockchain](https://concordium.com/).


## Setup for development

Install PostgreSQL server 16 or run `docker-compose up`.

Install `sqlx-cli`

```
cargo install sqlx-cli
```

Create a `.env` file in this directory:

```
# Postgres database connection used by sqlx-cli and this service.
DATABASE_URL=postgres://postgres:example@localhost/ccd-scan
```

Create the database

```
sqlx database create
```

Setup tables:

```
sqlx migrate run
```


## Run the backend



TODO
