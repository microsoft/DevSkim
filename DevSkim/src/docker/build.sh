#!/bin/bash
if docker build --tag=devskim -f Dockerfile ../.. ; then
    echo Build succeeded
    cd tests
    if ./tests.sh devskim ; then
        echo All tests passed
    else
        echo There were test failures
        exit 1
    fi
else
    echo Build failed
fi
