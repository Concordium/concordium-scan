FROM timescale/timescaledb-ha:pg14.3-ts2.7.0-p0

USER root

RUN apt-get update && apt-get install -y vim

COPY sleep.sh /sleep.sh
RUN chmod +x /sleep.sh

USER postgres

ENTRYPOINT [ "/sleep.sh" ]
CMD [ "" ]