#!/bin/bash
# Preview the generated site at http://localhost:4300
cd "$(dirname "$0")"
dotnet tool restore
dotnet dotnet-serve -d build -p:4300 --default-extensions:.html
