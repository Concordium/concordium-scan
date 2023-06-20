#!/bin/sh

/usr/local/bin/docker-entrypoint.sh postgres || true

while true; do
    sleep 500
done