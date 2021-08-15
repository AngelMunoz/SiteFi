#!/bin/bash

echo "Installing dotnet-serve"

dotnet tool install dotnet-serve --tool-path .tools

echo "Running pnpm install"

pushd src/Hosted
pnpm install
popd

echo "Running dotnet build"

dotnet build SiteFi.sln

