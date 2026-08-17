#!/bin/bash
# Build the static site into ./build
set -e
cd "$(dirname "$0")"
dotnet run --project src/Blogo -- build
