$ErrorActionPreference = 'Stop'

$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '..\.dotnet-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:MSBUILDDISABLENODEREUSE = '1'

dotnet restore (Join-Path $PSScriptRoot '..\Halisaha.sln') --disable-build-servers -m:1
dotnet build (Join-Path $PSScriptRoot '..\Halisaha.sln') --no-restore --disable-build-servers -m:1
dotnet test (Join-Path $PSScriptRoot '..\Halisaha.sln') --no-restore --disable-build-servers -m:1

Push-Location (Join-Path $PSScriptRoot '..\frontend')
try {
    npm ci
    npm run lint
    npm run type-check
    npm test
    npm run build
}
finally {
    Pop-Location
}
