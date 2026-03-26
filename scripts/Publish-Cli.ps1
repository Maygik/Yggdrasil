param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "Yggdrasil.Cli\Yggdrasil.Cli.csproj"

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $repoRoot "artifacts\publish\cli-single"
}

if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$publishArgs = @(
    "publish", $projectPath,
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "-p:PublishSingleFile=true",
    "-p:IncludeNativeLibrariesForSelfExtract=true",
    "-p:EnableCompressionInSingleFile=true",
    "-p:PublishTrimmed=false",
    "-p:DebugSymbols=false",
    "-p:DebugType=None",
    "-o", $OutputDir
)

Write-Host "Publishing Yggdrasil CLI..."
Write-Host "  Project: $projectPath"
Write-Host "  Config:  $Configuration"
Write-Host "  Runtime: $Runtime"
Write-Host "  Output:  $OutputDir"
Write-Host ""

& dotnet @publishArgs

$exePath = Join-Path $OutputDir "Yggdrasil.Cli.exe"
if (-not (Test-Path $exePath)) {
    throw "Publish completed, but the expected executable was not found at '$exePath'."
}

Write-Host ""
Write-Host "Publish complete."
Write-Host "Executable: $exePath"
