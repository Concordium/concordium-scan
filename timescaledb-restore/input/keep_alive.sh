#!/bin/sh
/usr/local/bin/docker-entrypoint.sh postgres &

sleep 15

/etc/restore.sh

sleep infinity
