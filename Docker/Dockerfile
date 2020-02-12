FROM ubuntu:18.04

COPY publish /publish

RUN ln -s /publish/devskim /bin/devskim

ENTRYPOINT ["devskim"]
CMD ["--help"]