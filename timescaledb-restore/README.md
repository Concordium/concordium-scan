# Restore database from backups

Below is instructions how to spin up CCD scan database locally which is restored from Stage- or Testnet backups, as specified by `./init/pgbackrest.conf`.

Before following below instructions make sure you have read access to the S3 buckets where the backups are located.

Commands should be run from current directory.

## Database restore

In the `init` directory make a `pgbackrest.conf` file like `pgbackrest.conf.example` replacing `$AWS_ACCESS_KEY` and `$AWS_SECRET_KEY` with your access credentials.

Start docker-compose
```
docker-compose up
```

Database is now available at `localhost:15432` with user `postgres` and password `password`.

### Refenreces
- https://bun.uptrace.dev/postgres/pgbackrest-s3-backups.html#incremental-backup
- https://pgbackrest.org/user-guide.html#restore/option-delta
