ARG DOTNET_VERSION
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-bookworm-slim AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-bookworm-slim AS publish
RUN apt-get update && apt-get install -y build-essential
# Installing Rust to build Concordium.Sdk.
RUN curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- --default-toolchain 1.80 --profile minimal -y
ENV PATH="/root/.cargo/bin:${PATH}"
WORKDIR /src
COPY . .
RUN dotnet publish Application -c Release -o /app/publish

FROM base AS final
# Install 'ca-certificates' for supporting HTTPS.
RUN apt-get update && apt-get install ca-certificates && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
LABEL dotnet_version=${DOTNET_VERSION}
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "Application.dll"]
