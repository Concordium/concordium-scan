

## Setup

Install PostgreSQL server 16 or run `docker-compose up`.

Install `sqlx-cli`

Create database using connection defined in `.env`:

```
sqlx database create
```

Setup tables:

```
sqlx migrate run
```
