#!/bin/bash
TEST_FAILURES=0

# test with issues
if docker run --volume "`pwd`/resources/issues":/code $1 analyze /code ; then
    echo "FAILED: Test with issues - expected a non-zero exit code"
    TEST_FAILURES=1
else
    echo "PASSED: Test with issues"
fi

# test with no issues
if docker run --volume "`pwd`/resources/no_issues":/code $1 analyze /code ; then
    echo "PASSED: Test with issues"
else
    echo "FAILED: Test with issues - expected a zero exit code"
    TEST_FAILURES=1
fi

exit $TEST_FAILURES