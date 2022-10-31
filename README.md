# CCDScan

[CCDScan](https://ccdscan.io) is a full blockchain explorer on [Concordium](https://www.concordium.com/).

It consists of two main parts:

- **[backend/](./backend/README.md)**  
  Backend built with [.NET](https://dotnet.microsoft.com/en-us/). It reads data directly from the blockchain via gRPC, ingests it into a [PostgreSQL database](https://www.postgresql.org/), then serves it to any consumer from a [GraphQL](https://graphql.org/) API.
- **[frontend/](./frontend/README.md)**  
  A server-side rendered single page app written in [Vue](https://vuejs.org/) and [TypeScript](https://www.typescriptlang.org/), which consumes data from the [GraphQL](https://graphql.org/) endpoint exposed by the backend.

## Docker Compose

The project ships a Docker Compose spec for spinning up a backend with a TimescaleDB instance. Parameters:

- `CCDSCAN_BACKEND_IMAGE` (default `concordium/ccdscan:test`): ...
- `NODE_GRPC_ADDRESS` (default: `http://172.17.0.1:10000`): ...
- `DOMAIN` (default: `testnet.concordium.com`): ...
