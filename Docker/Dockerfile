FROM ubuntu:18.04

COPY DevSkim-Linux /DevSkim-Linux

RUN ln -s /DevSkim-Linux/devskim /bin/devskim

ENTRYPOINT ["devskim"]
CMD ["--help"]