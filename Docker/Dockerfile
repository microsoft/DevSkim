FROM ubuntu

RUN apt-get update \
  && apt-get install -y wget libunwind8 icu-devtools curl apt-transport-https gnupg \
  && rm -rf /var/lib/apt/lists/*

RUN curl https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > microsoft.gpg

RUN mv microsoft.gpg /etc/apt/trusted.gpg.d/microsoft.gpg

RUN sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-ubuntu-xenial-prod xenial main" > /etc/apt/sources.list.d/microsoft.list'

RUN apt-get update \
  && apt-get install devskim

ENTRYPOINT [ "/usr/bin/devskim" ]