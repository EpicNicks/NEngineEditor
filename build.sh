#!/bin/bash

dotnet build --configuration Release
if [ $? -ne 0 ]; then
    echo "Build failed."
    exit 1
fi

OUTPUT_DIR="./NEngineEditor/bin/Release/net8.0-linux"
if [ ! -d "$OUTPUT_DIR" ]; then
    OUTPUT_DIR="./NEngineEditor/bin/Release/net8.0"
fi

"$OUTPUT_DIR/NEngineEditor" &

exit 0