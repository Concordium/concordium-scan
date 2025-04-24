# CCDScan

## Component Interaction Diagram

![Component Interaction Diagram](docs/diagrams/CCDScan.drawio.png)

[CCDScan](https://ccdscan.io) is a full blockchain explorer on [Concordium](https://www.concordium.com/).

It consists of two main parts:

- **[backend/](./backend/README.md)**  
  Backend built with [.NET](https://dotnet.microsoft.com/en-us/). It reads data directly from the blockchain via gRPC, ingests it into a [PostgreSQL database](https://www.postgresql.org/), then serves it to any consumer from a [GraphQL](https://graphql.org/) API.
- **[frontend/](./frontend/README.md)**  
  A server-side rendered single page app written in [Vue](https://vuejs.org/) and [TypeScript](https://www.typescriptlang.org/), which consumes data from the [GraphQL](https://graphql.org/) endpoint exposed by the backend.

## Docker Compose

The project ships a Docker Compose spec for deploying a CCDScan Backend with a TimescaleDB (v14) instance.

*Parameters*

- `CCDSCAN_BACKEND_IMAGE` (default: `concordium/ccdscan:test`):
  Image to use for the backend. The default value is not an existing public image: Using it will make Compose build the image from local sources.
  Note that to use an existing public image, the image must already have been pulled (using e.g. `docker-compose pull`) before running `up`.
  Otherwise, Compose will proceed to build the image without first checking if the image can be pulled.
- `CCDSCAN_BACKEND_PORT` (default: `5000`):
  Port on which to expose the backend.
  Mac users should set this to another value as the port might already be used by the system.
- `CCDSCAN_NODE_GRPC_ADDRESS` (default: `http://172.17.0.1:10000`):
  URL of the gRPC (APIv1) interface of a Concordium Node. The default value is the default address of a Node running on the host.
- `CCDSCAN_DOMAIN` (default: `testnet.concordium.com`):
  URL of the network's domain (`mainnet.concordium.software` for mainnet and `<network>.concordium.com` for the other official networks).
  Used as part of the URL for fetching data from the public network dashboard.

*Example*

Run backend on port 5001 from public image `concordium/ccdscan:<tag>` against a local mainnet node:

```shell
export CCDSCAN_BACKEND_IMAGE=concordium/ccdscan:<tag>
export CCDSCAN_BACKEND_PORT=5001
export CCDSCAN_DOMAIN=mainnet.concordium.software
docker-compose pull
docker-compose up
```

See the description of `CCDSCAN_BACKEND_PORT` for an explanation of why Mac users in particular might want to set this value.

## Database restore

In `./timescaledb-restore` a description is given how to spin up and restore CCD scan database locally from Stage- or Testnet backups.

This is only needed to be done once. They will be recompiled on later changes as part of `dotnet build`.

## Unstable features
- GraphQL endpoints for contracts and modules should currently be seen as unstable, indicating that the query models are likely to undergo changes.
