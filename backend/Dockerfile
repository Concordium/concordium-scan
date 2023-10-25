FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5000

FROM rust:1.65 AS rust-builder
WORKDIR /src
COPY ../concordium-base ../concordium-base
COPY rust-bindings/ rust-bindings/
RUN cargo build  --manifest-path=./rust-bindings/Cargo.toml --release

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS restore
WORKDIR /src
COPY --from=rust-builder /src/rust-bindings/target rust-bindings/target
COPY Application/ Application/
COPY DatabaseScripts/ DatabaseScripts/
RUN dotnet restore "Application/Application.csproj" 
WORKDIR "/src/Application"

FROM restore AS publish
RUN dotnet publish "Application.csproj" -c Release -o /app/publish

FROM base AS final
# Install 'ca-certificates' for supporting HTTPS.
RUN apt-get update && apt-get install ca-certificates && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "Application.dll"]
