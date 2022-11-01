# CCDScan

[CCDScan](https://ccdscan.io) is a full blockchain explorer on [Concordium](https://www.concordium.com/).

It consists of two main parts:

- **[backend/](./backend/README.md)**  
  Backend built with [.NET](https://dotnet.microsoft.com/en-us/). It reads data directly from the blockchain via gRPC, ingests it into a [PostgreSQL database](https://www.postgresql.org/), then serves it to any consumer from a [GraphQL](https://graphql.org/) API.
- **[frontend/](./frontend/README.md)**  
  A server-side rendered single page app written in [Vue](https://vuejs.org/) and [TypeScript](https://www.typescriptlang.org/), which consumes data from the [GraphQL](https://graphql.org/) endpoint exposed by the backend.

## Docker Compose

The project ships a Docker Compose spec for deploying a CCDScan Backend with a TimescaleDB (v14) instance.

*Parameters*

- `CCDSCAN_BACKEND_IMAGE` (default `concordium/ccdscan:test`):
  Image to use for the backend. The default value is not an existing public image: Using it will make Compose build the image from local sources.
  Note that to use an existing public image, the image must already have been pulled (using e.g. `docker-compose pull`) before running `up`.
  Otherwise, Compose will proceed to build the image without first checking if the image can be pulled.
- `CCDSCAN_NODE_GRPC_ADDRESS` (default: `http://172.17.0.1:10000`):
  URL of the gRPC (APIv1) interface of a Concordium Node. The default value is the default address of a Node running on the host.
- `CCDSCAN_DOMAIN` (default: `testnet.concordium.com`):
  URL of the network's domain (`mainnet.concordium.software` for mainnet and `<network>.concordium.com` for the other official networks).
  Used as part of the URL for fetching data from the public network dashboard.

*Example*

Run backend from public image `concordium/ccdscan:1.3.0-0` against a local mainnet node:

```shell
export CCDSCAN_BACKEND_IMAGE=concordium/ccdscan:1.3.0-0
export CCDSCAN_DOMAIN=mainnet.concordium.software
docker-compose pull
docker-compose up
```