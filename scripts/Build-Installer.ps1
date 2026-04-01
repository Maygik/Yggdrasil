param(
    [Parameter(Mandatory = $true)]
    [string]$Version,
    [string]$Runtime = "win-x64",
    [string]$PublishDir = "",
    [string]$OutputDir = "",
    [string]$InstallerScript = "",
    [string]$InnoSetupCompilerPath = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot

if ([string]::IsNullOrWhiteSpace($PublishDir)) {
    $PublishDir = Join-Path $repoRoot "artifacts\release\Yggdrasil-$Runtime"
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $repoRoot "artifacts\release"
}

if ([string]::IsNullOrWhiteSpace($InstallerScript)) {
    $InstallerScript = Join-Path $repoRoot "installer\Yggdrasil.iss"
}

$normalizedVersion = $Version.Trim()
if ($normalizedVersion.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase)) {
    $normalizedVersion = $normalizedVersion.Substring(1)
}

if ([string]::IsNullOrWhiteSpace($normalizedVersion)) {
    throw "A non-empty version is required."
}

if (-not (Test-Path -LiteralPath $PublishDir)) {
    throw "The publish directory '$PublishDir' was not found. Run Publish-Desktop.ps1 first."
}

if (-not (Test-Path -LiteralPath $InstallerScript)) {
    throw "The installer script '$InstallerScript' was not found."
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$resolvedPublishDir = (Resolve-Path -LiteralPath $PublishDir).Path
$resolvedOutputDir = (Resolve-Path -LiteralPath $OutputDir).Path
$resolvedInstallerScript = (Resolve-Path -LiteralPath $InstallerScript).Path
$resolvedRepoRoot = (Resolve-Path -LiteralPath $repoRoot).Path

$compilerCandidates = [System.Collections.Generic.List[string]]::new()

if (-not [string]::IsNullOrWhiteSpace($InnoSetupCompilerPath)) {
    $compilerCandidates.Add($InnoSetupCompilerPath)
}

$command = Get-Command "ISCC.exe" -ErrorAction SilentlyContinue
if ($null -ne $command -and -not [string]::IsNullOrWhiteSpace($command.Source)) {
    $compilerCandidates.Add($command.Source)
}

foreach ($candidate in @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
)) {
    if (-not [string]::IsNullOrWhiteSpace($candidate)) {
        $compilerCandidates.Add($candidate)
    }
}

$resolvedCompilerPath = $compilerCandidates |
    Where-Object { Test-Path -LiteralPath $_ } |
    Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($resolvedCompilerPath)) {
    throw "ISCC.exe was not found. Install Inno Setup 6 or pass -InnoSetupCompilerPath."
}

$safeVersion = $normalizedVersion -replace "[^0-9A-Za-z._-]", "-"
$outputBaseFilename = "Yggdrasil-setup-$Runtime-$safeVersion"
$installerOutputPath = Join-Path $resolvedOutputDir "$outputBaseFilename.exe"

if (Test-Path -LiteralPath $installerOutputPath) {
    Remove-Item -LiteralPath $installerOutputPath -Force
}

$compilerArgs = @(
    "/DAppVersion=$normalizedVersion",
    "/DRuntimeIdentifier=$Runtime",
    "/DSourceDir=$resolvedPublishDir",
    "/DOutputDir=$resolvedOutputDir",
    "/DRepoRoot=$resolvedRepoRoot",
    "/DOutputBaseFilename=$outputBaseFilename",
    $resolvedInstallerScript
)

Write-Host "Building Yggdrasil installer..."
Write-Host "  Version:   $normalizedVersion"
Write-Host "  Runtime:   $Runtime"
Write-Host "  Publish:   $resolvedPublishDir"
Write-Host "  Output:    $resolvedOutputDir"
Write-Host "  Compiler:  $resolvedCompilerPath"
Write-Host ""

& $resolvedCompilerPath @compilerArgs

if ($LASTEXITCODE -ne 0) {
    throw "ISCC.exe failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path -LiteralPath $installerOutputPath)) {
    throw "The installer was not created at '$installerOutputPath'."
}

Write-Host ""
Write-Host "Installer build complete."
Write-Host "Installer path: $installerOutputPath"
