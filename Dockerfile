# https://hub.docker.com/_/microsoft-dotnet-core
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-bionic AS build

WORKDIR /source
COPY . .
RUN dotnet restore DevSkim-DotNet/Microsoft.DevSkim.CLI
RUN dotnet publish DevSkim-DotNet/Microsoft.DevSkim.CLI -c release -o /app -f netcoreapp3.1 -r linux-x64 /p:DebugType=None --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/core/runtime:3.1-bionic
WORKDIR /DevSkim
COPY --from=build /app .
ENTRYPOINT ["./devskim"]
CMD ["--help"]