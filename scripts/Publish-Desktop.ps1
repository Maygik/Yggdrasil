param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$OutputDir = "",
    [switch]$SmokeTest
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "Yggdrasil.Presentation\Yggdrasil.Presentation.csproj"

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "The desktop project was not found at '$projectPath'."
}

$platform = switch ($Runtime) {
    "win-x64" { "x64" }
    "win-x86" { "x86" }
    "win-arm64" { "ARM64" }
    default { throw "Unsupported runtime '$Runtime'. Expected win-x64, win-x86, or win-arm64." }
}

if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $repoRoot "artifacts\portable\desktop"
}

[xml]$projectXml = Get-Content -LiteralPath $projectPath
$assemblyName = @(
    $projectXml.Project.PropertyGroup |
        ForEach-Object {
            $assemblyNameProperty = $_.PSObject.Properties["AssemblyName"]
            if ($null -ne $assemblyNameProperty -and -not [string]::IsNullOrWhiteSpace($assemblyNameProperty.Value)) {
                $assemblyNameProperty.Value
            }
        }
) | Select-Object -First 1

if ([string]::IsNullOrWhiteSpace($assemblyName)) {
    $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($projectPath)
}

$packageFolderName = "$assemblyName-$Runtime"
$packageDir = Join-Path $OutputDir $packageFolderName
$smokeTestDir = Join-Path $OutputDir "smoke-test"
$publishReadyToRun = if ($Configuration -ieq "Debug") { "false" } else { "true" }

foreach ($path in @($packageDir, $smokeTestDir)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force
    }
}

New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

$publishArgs = @(
    "publish", $projectPath,
    "-c", $Configuration,
    "-r", $Runtime,
    "--self-contained", "true",
    "-p:Platform=$platform",
    "-p:WindowsPackageType=None",
    "-p:WindowsAppSDKSelfContained=true",
    "-p:PublishSingleFile=false",
    "-p:PublishTrimmed=false",
    "-p:PublishReadyToRun=$publishReadyToRun",
    "-o", $packageDir
)

Write-Host "Publishing Yggdrasil desktop app..."
Write-Host "  Project: $projectPath"
Write-Host "  Config:  $Configuration"
Write-Host "  Runtime: $Runtime"
Write-Host "  Output:  $OutputDir"
Write-Host ""

& dotnet @publishArgs

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$exePath = Join-Path $packageDir "$assemblyName.exe"
if (-not (Test-Path -LiteralPath $exePath)) {
    throw "Publish completed, but the expected executable was not found at '$exePath'."
}

if ($SmokeTest) {
    New-Item -ItemType Directory -Path $smokeTestDir -Force | Out-Null
    Get-ChildItem -LiteralPath $packageDir -Force | Copy-Item -Destination $smokeTestDir -Recurse -Force

    $smokeTestExe = Join-Path $smokeTestDir "$assemblyName.exe"
    if (-not (Test-Path -LiteralPath $smokeTestExe)) {
        throw "Smoke test failed because '$smokeTestExe' was not found in the portable output."
    }

    $process = Start-Process -FilePath $smokeTestExe -PassThru

    try {
        Start-Sleep -Seconds 8
        $process.Refresh()

        if ($process.HasExited) {
            throw "Smoke test failed because the desktop app exited immediately with code $($process.ExitCode)."
        }

        if ($process.MainWindowHandle -eq 0) {
            throw "Smoke test failed because no main window was detected."
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
Write-Host "Portable folder: $packageDir"
Write-Host "Launch this file: $exePath"
