FROM mcr.microsoft.com/dotnet/aspnet:6.0-bookworm-slim AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:6.0-bookworm-slim AS publish
WORKDIR /src
COPY Application/ Application/
COPY DatabaseScripts/ DatabaseScripts/
WORKDIR "/src/Application"
RUN dotnet publish "Application.csproj" -c Release -o /app/publish -p:RunRustBuild=false

FROM base AS final
# Install 'ca-certificates' for supporting HTTPS.
RUN apt-get update && apt-get install ca-certificates && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "Application.dll"]
