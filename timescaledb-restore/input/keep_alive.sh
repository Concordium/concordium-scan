#!/bin/sh
/usr/local/bin/docker-entrypoint.sh postgres &

if [ "$1" = "restore" ]; then
    sleep 15
    /etc/restore.sh
fi

sleep infinity
