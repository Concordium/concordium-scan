# CCDScan

[CCDScan](https://ccdscan.io) is a full blockchain explorer on [Concordium](https://www.concordium.com/).

It consists of two main parts:

- **[backend/](./backend/README.md)**  
  Backend built with [.NET](https://dotnet.microsoft.com/en-us/). It reads data directly from the blockchain via gRPC, ingests it into a [PostgreSQL database](https://www.postgresql.org/), then serves it to any consumer from a [GraphQL](https://graphql.org/) API.
- **[frontend/](./frontend/README.md)**  
  A server-side rendered single page app written in [Vue](https://vuejs.org/) and [TypeScript](https://www.typescriptlang.org/), which consumes data from the [GraphQL](https://graphql.org/) endpoint exposed by the backend.

## Docker Compose

The project ships a Docker Compose spec for deploying a CCDScan Backend with a TimescaleDB instance.

Parameters:

- `CCDSCAN_BACKEND_IMAGE` (default `concordium/ccdscan:test`):
  Image to use for the backend. The default value is not an existing public image, using it will make Compose build the image from local source.
- `CCDSCAN_NODE_GRPC_ADDRESS` (default: `http://172.17.0.1:10000`):
  URL of the gRPC (APIv1) interface of a Concordium Node. The default value is the default address of a Node running on the host.
- `CCDSCAN_DOMAIN` (default: `testnet.concordium.com`):
  URL of the network's domain (`mainnet.concordium.software` for mainnet and `<network>.concordium.com` for the other official networks).
  Used as part of the URL for fetching data from the public network dashboard.
