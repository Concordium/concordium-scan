ARG build_image=rust:1.82-bookworm
ARG base_image=debian:bookworm-slim
FROM ${build_image} AS build

WORKDIR /usr/app

COPY Cargo.toml Cargo.lock ./
COPY src ./src
COPY concordium-rust-sdk /usr/app/concordium-rust-sdk
COPY .sqlx .sqlx
RUN cargo install sqlx-cli --no-default-features --features "postgres"
RUN cargo build --release --locked


FROM ${base_image}

WORKDIR /usr/app

COPY migrations /usr/app/migrations
COPY --from=build /usr/app/target/release/ccdscan-api /usr/app/target/release/ccdscan-indexer /usr/bin/
COPY --from=build /usr/local/cargo/bin/sqlx /usr/local/bin/sqlx

RUN chmod +x /usr/bin/ccdscan-api /usr/bin/ccdscan-indexer
