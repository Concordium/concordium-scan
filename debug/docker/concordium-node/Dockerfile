FROM concordium/dev-node:latest
ENV GENESIS_DATA_PATH="/genesis-data/genesis-1-bakers"
ENV BAKER_ID="0"
WORKDIR /
COPY node-entrypoint.sh node-entrypoint.sh
RUN chmod +x node-entrypoint.sh
ENTRYPOINT [ "sh", "-c", "./node-entrypoint.sh" ]