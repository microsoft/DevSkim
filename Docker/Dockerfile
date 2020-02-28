FROM ubuntu:18.04

COPY DevSkim-Linux /DevSkim-Linux

RUN apt update

RUN apt install libicu60 -y

RUN ln -s /DevSkim-Linux/devskim /bin/devskim

ENTRYPOINT ["devskim"]
CMD ["--help"]