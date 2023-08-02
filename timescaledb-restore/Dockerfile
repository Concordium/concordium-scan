# This dockerfile contains scripts and configurations which can be used to restore a postgres database using pgbackrest.
# The image should only be use locally to restore CCD scan from backups.
ARG timescale_image=timescale/timescaledb-ha:pg14.3-ts2.7.0-p0
FROM ${timescale_image}

USER root

RUN apt-get update

COPY ./input/keep_alive.sh /keep_alive.sh
COPY ./input/restore.sh /etc/restore.sh
COPY ./input/pg_hba.conf /etc/pg_hba.conf
COPY ./input/postgresql.conf /etc/postgresql.conf

RUN chmod +x /keep_alive.sh
RUN chmod +x /etc/restore.sh
RUN chmod +x /etc/postgresql.conf
RUN chmod +x /etc/pg_hba.conf

USER postgres

ENTRYPOINT [ "/keep_alive.sh", "restore" ]
