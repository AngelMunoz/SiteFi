echo "Running pnpm install" 

pushd src/Hosted
pnpm install
popd

echo "Running dotnet build"

dotnet build SiteFi.sln