#!/bin/bash

# This script can be called when the container is running.
# It restores the postgres database using pgbackrest and the configuration
# file located at /etc/pgbackrest.conf. This file should be mounted when the 
# container is created.

PGDATA=/home/postgres/pgdata/data

pg_ctl -D $PGDATA stop -mf

echo "*** Start restore ***"

pgbackrest restore --stanza=poddb --delta --log-level-console=detail

echo "*** Done restore ***"

cp /etc/pg_hba.conf /home/postgres/pgdata/data/pg_hba.conf
cp /etc/postgresql.conf /home/postgres/pgdata/data/postgresql.conf

pg_ctl -D $PGDATA start -o --archive-command=/bin/false

while true; do
    MAX_BACKUP_WAL="$(pgbackrest info --output=json | python3 -c "import json,sys;obj=json.load(sys.stdin); print(obj[0]['archive'][0]['max']);")"
    echo "Testing whether WAL file ${MAX_BACKUP_WAL} has been restored ..."
    [ -f "${PGDATA}/pg_wal/${MAX_BACKUP_WAL}" ] && break
    sleep 10;
done

pg_ctl -D $PGDATA stop -mf

sleep 10

pg_ctl -D $PGDATA start

sleep 25

# Set password to 'password' for user 'postgres'.
psql -U postgres -d postgres -c "UPDATE pg_authid SET rolpassword = 'SCRAM-SHA-256\$4096:NK71fpsjRyAUUxSvFiDlGg==\$sBwig8n1Srb90HuK+jGb+hSWoQk9jVbvKLTX8mG/EIE=:2URiLDrB9cqgzyjKv8gt+LKwIuQ55nSm6S6wX8RgN20=' WHERE rolname='postgres';"
