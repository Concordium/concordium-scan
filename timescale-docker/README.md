# Restore database from backups

Below is instructions how to spin up CCD scan database locally which is restored from Stage- or Testnet backups.

Before following below instructions make sure you have read access to the S3 buckets where the backups are located.

## Database restore

Build custom docker image by running this
```
cd ./docker
docker build -t timescale-restore:1 .
cd ..
```

Make a `pgbackrest.conf` file like `pgbackrest.conf.example` replacing `$AWS_ACCESS_KEY` and `$AWS_SECRET_KEY` with your access credentials.

Start docker-compose
```
docker-compose up
```

Exec into the container.
```
docker container ls | grep timescale | awk '{print $1}' | head -n 1 | xargs -o -I {} docker exec -it {} bash
```

Then run
````
/etc/restore.sh
````

Database is now available at `localhost:15432` with user `postgres` and password `password`.

### Refenreces
- https://bun.uptrace.dev/postgres/pgbackrest-s3-backups.html#incremental-backup
- https://pgbackrest.org/user-guide.html#restore/option-delta
