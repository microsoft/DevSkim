FROM mcr.microsoft.com/dotnet/core/sdk:3.1

RUN dotnet tool install -g Microsoft.CST.DevSkim.Cli

COPY entrypoint.sh /entrypoint.sh

ENTRYPOINT ["/entrypoint.sh"]