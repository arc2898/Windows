[CmdletBinding()]
param([string]$Configuration = 'Release', [string]$Runtime = 'win-x64', [string]$Version = '0.2.0')
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\AeroDesk\AeroDesk.csproj'
$artifacts = Join-Path $root 'artifacts'
$publish = Join-Path $artifacts 'publish'
$media = Join-Path $artifacts 'media'
New-Item -ItemType Directory -Force -Path $publish, $media | Out-Null

dotnet publish $project -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o $publish
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

$iscc = Get-Command ISCC.exe -ErrorAction SilentlyContinue
if ($iscc) {
  & $iscc.Source "/DAppVersion=$Version" "/DSourceDir=$publish" (Join-Path $PSScriptRoot 'AeroDesk.iss')
  if ($LASTEXITCODE -ne 0) { throw 'Inno Setup compiler failed.' }
} else {
  Write-Warning 'Inno Setup was not found. Install it to create the installer and uninstaller.'
}
