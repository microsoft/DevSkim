FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS builder

COPY src /tmp/DevSkim/src
COPY rules /tmp/DevSkim/rules

RUN cd /tmp/DevSkim/src/Microsoft.DevSkim/Microsoft.DevSkim.CLI && \
    echo Initial build... && \
    dotnet build && \
    echo Packing rules ... && \
    dotnet run -- pack /tmp/DevSkim/rules Resources/devskim-rules.json --indent && \
    echo Publishing... && \
    mkdir /tmp/binary && \
    dotnet publish --configuration Release --runtime linux-x64 --output /tmp/binary && \
    ls /tmp/binary/devskim && \
    chmod +x /tmp/binary/devskim

FROM ubuntu:18.04

RUN apt-get update && \
    apt-get install -y \
        libunwind8 \
        icu-devtools && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

COPY --from=builder /tmp/binary /usr/share/devskim

RUN ln -s /usr/share/devskim/devskim /usr/bin/devskim

ENTRYPOINT [ "devskim" ]
CMD [ "--help" ]
