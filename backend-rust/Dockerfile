ARG build_image=rust:1.83-bookworm
ARG base_image=debian:bookworm-slim
ARG target_binary
FROM ${build_image} AS build

WORKDIR /usr/app

COPY Cargo.toml Cargo.lock ./
COPY src ./src
COPY concordium-rust-sdk /usr/app/concordium-rust-sdk
COPY .sqlx .sqlx
RUN cargo build --release --locked


FROM ${base_image}

WORKDIR /usr/app
COPY stablecoin.json transaction.json transfers.json walletholdings.json ./ 
RUN apt-get update && \
    apt-get -y install ca-certificates

COPY --from=build /usr/app/target/release/${target_binary} /usr/bin/

RUN chmod +x /usr/bin/${target_binary}
CMD ["/usr/bin/${target_binary}"]
