echo "Positional Parameters"
echo '$0 = ' $0
echo '$1 = ' $1
echo '$2 = ' $2
echo '$3 = ' $3

if [ "$1" == "GITHUB_WORKSPACE" ]
    $Directory = $GITHUB_WORKSPACE
else
    $Directory = $1
fi

if [ "$2" == "true" ]
    devskim analyze $Directory -f sarif -c > $GITHUB_WORKSPACE/$3
else
    devskim analyze $Directory -f sarif > $GITHUB_WORKSPACE/$3
fi