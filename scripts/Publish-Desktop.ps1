param(
    [string]$Configuration = "Release",
    [string]$Version = "",
    [string]$OutputDir = "",
    [switch]$SmokeTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "Yggdrasil.Presentation\Yggdrasil.Presentation.csproj"
$runtime = "win-x64"
$platform = "x64"
$packageFolderName = "Yggdrasil-win-x64"

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $repoRoot "artifacts\release\desktop"
}

$publishDir = Join-Path $OutputDir "publish-temp"
$packageDir = Join-Path $OutputDir $packageFolderName
$zipName = if ([string]::IsNullOrWhiteSpace($Version)) {
    "$packageFolderName.zip"
} else {
    "$packageFolderName-v$Version.zip"
}
$zipPath = Join-Path $OutputDir $zipName
$smokeTestDir = Join-Path $OutputDir "smoke-test"

foreach ($path in @($publishDir, $packageDir, $zipPath, $smokeTestDir)) {
    if (Test-Path $path) {
        Remove-Item -Path $path -Recurse -Force
    }
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$publishArgs = @(
    "publish", $projectPath,
    "-c", $Configuration,
    "-r", $runtime,
    "--self-contained", "true",
    "-p:Platform=$platform",
    "-p:WindowsPackageType=None",
    "-p:WindowsAppSDKSelfContained=true",
    "-p:PublishSingleFile=false",
    "-p:PublishTrimmed=false",
    "-p:PublishReadyToRun=true",
    "-o", $publishDir
)

Write-Host "Publishing Yggdrasil desktop app..."
Write-Host "  Project: $projectPath"
Write-Host "  Config:  $Configuration"
Write-Host "  Runtime: $runtime"
Write-Host "  Output:  $OutputDir"
Write-Host ""

& dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$exePath = Join-Path $publishDir "Yggdrasil.Presentation.exe"
if (-not (Test-Path $exePath)) {
    throw "Publish completed, but the expected executable was not found at '$exePath'."
}

New-Item -ItemType Directory -Path $packageDir -Force | Out-Null
Copy-Item -Path (Join-Path $publishDir "*") -Destination $packageDir -Recurse -Force

Compress-Archive -Path $packageDir -DestinationPath $zipPath -CompressionLevel Optimal

if ($SmokeTest) {
    Expand-Archive -Path $zipPath -DestinationPath $smokeTestDir -Force

    $smokeTestExe = Join-Path $smokeTestDir $packageFolderName
    $smokeTestExe = Join-Path $smokeTestExe "Yggdrasil.Presentation.exe"

    if (-not (Test-Path $smokeTestExe)) {
        throw "Smoke test failed because '$smokeTestExe' was not found after extracting the zip."
    }

    $process = Start-Process -FilePath $smokeTestExe -PassThru

    try {
        Start-Sleep -Seconds 8
        $process.Refresh()

        if ($process.HasExited) {
            throw "Smoke test failed because the desktop app exited immediately with code $($process.ExitCode)."
        }
    }
    finally {
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id -Force
        }
    }
}

Write-Host ""
Write-Host "Desktop publish complete."
Write-Host "Package folder: $packageDir"
Write-Host "Zip package:    $zipPath"
