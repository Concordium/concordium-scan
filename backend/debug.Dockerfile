FROM mcr.microsoft.com/dotnet/sdk:6.0-focal AS run
WORKDIR /src
ENTRYPOINT [ "sleep", "infinity" ]