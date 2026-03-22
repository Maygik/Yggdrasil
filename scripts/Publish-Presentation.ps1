param(
    [string]$Configuration = "Release",
    [ValidateSet("win-x86", "win-x64", "win-arm64")]
    [string]$Runtime = "win-x64",
    [ValidateSet("FrameworkDependent", "SelfContained")]
    [string]$Deployment = "SelfContained",
    [ValidateSet("Trimmed", "Untrimmed")]
    [string]$SizeProfile = "Trimmed",
    [string]$OutputDir = "",
    [string]$ZipPath = "",
    [switch]$SkipZip,
    [switch]$ReadyToRun
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "Yggdrassil.Presentation\Yggdrassil.Presentation.csproj"

$platform = switch ($Runtime) {
    "win-x86" { "x86" }
    "win-x64" { "x64" }
    "win-arm64" { "ARM64" }
    default { throw "Unsupported runtime '$Runtime'." }
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $deploymentSuffix = if ($Deployment -eq "SelfContained") { "sc" } else { "fdd" }
    $profileSuffix = if ($SizeProfile -eq "Trimmed") { "trimmed" } else { "untrimmed" }
    $OutputDir = Join-Path $repoRoot "artifacts\publish\presentation\Yggdrassil.Presentation-$Configuration-$Runtime-$deploymentSuffix-$profileSuffix"
}

if ([string]::IsNullOrWhiteSpace($ZipPath)) {
    $ZipPath = "$OutputDir.zip"
}

if (Test-Path $OutputDir) {
    Remove-Item -Path $OutputDir -Recurse -Force
}

if ((-not $SkipZip) -and (Test-Path $ZipPath)) {
    Remove-Item -Path $ZipPath -Force
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$dotnetCliHome = Join-Path $repoRoot ".dotnet"
New-Item -ItemType Directory -Path $dotnetCliHome -Force | Out-Null

$env:DOTNET_CLI_HOME = $dotnetCliHome
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"
$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"

$selfContained = if ($Deployment -eq "SelfContained") { "true" } else { "false" }
$windowsAppSdkSelfContained = if ($Deployment -eq "SelfContained") { "true" } else { "false" }
$publishTrimmed = if ($SizeProfile -eq "Trimmed") { "true" } else { "false" }
$publishReadyToRun = if ($ReadyToRun.IsPresent) { "true" } else { "false" }

$publishArgs = @(
    "publish", $projectPath,
    "-c", $Configuration,
    "-r", $Runtime,
    "-p:Platform=$platform",
    "--self-contained", $selfContained,
    "-p:WindowsPackageType=None",
    "-p:WindowsAppSDKSelfContained=$windowsAppSdkSelfContained",
    "-p:PublishSingleFile=false",
    "-p:PublishTrimmed=$publishTrimmed",
    "-p:PublishReadyToRun=$publishReadyToRun",
    "-o", $OutputDir
)

if ($Configuration -ieq "Release") {
    $publishArgs += @(
        "-p:DebugSymbols=false",
        "-p:DebugType=None"
    )
}

Write-Host "Publishing Yggdrassil Presentation..."
Write-Host "  Project:  $projectPath"
Write-Host "  Config:   $Configuration"
Write-Host "  Runtime:  $Runtime"
Write-Host "  Platform: $platform"
Write-Host "  Deploy:   $Deployment"
Write-Host "  Profile:  $SizeProfile"
Write-Host "  Output:   $OutputDir"
if (-not $SkipZip) {
    Write-Host "  Zip:      $ZipPath"
}
Write-Host ""

& dotnet @publishArgs

$exePath = Join-Path $OutputDir "Yggdrassil.Presentation.exe"
if (-not (Test-Path $exePath)) {
    throw "Publish completed, but the expected executable was not found at '$exePath'."
}

if (-not $SkipZip) {
    Compress-Archive -Path $OutputDir -DestinationPath $ZipPath -CompressionLevel Optimal -Force

    if (-not (Test-Path $ZipPath)) {
        throw "Zip creation completed, but the expected archive was not found at '$ZipPath'."
    }
}

Write-Host ""
Write-Host "Publish complete."
Write-Host "Folder:      $OutputDir"
Write-Host "Executable:  $exePath"
if (-not $SkipZip) {
    Write-Host "Archive:     $ZipPath"
}
if ($Deployment -eq "FrameworkDependent") {
    Write-Host "Run:         Ensure .NET Desktop Runtime 8 and the Windows App Runtime are installed, then launch Yggdrassil.Presentation.exe"
}
else {
    Write-Host "Run:         Extract the zip and launch Yggdrassil.Presentation.exe"
}
if ($SizeProfile -eq "Trimmed") {
    Write-Host "Fallback:    If the presentation build hits a trim-related issue, rerun with -SizeProfile Untrimmed"
}
