param(
    [switch] $buildOnly
)

# Install pnpm packages
pushd src\Hosted
pnpm install
popd

if (!$buildOnly) {
    # Install local dotnet-serve
    dotnet tool install dotnet-serve --tool-path .tools
}
