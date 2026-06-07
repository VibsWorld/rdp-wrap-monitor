#!/usr/bin/env pwsh
# Build script for creating release packages
# Run this to create ZIP packages for release

param(
    [string]$Version = "1.0.0",
    [switch]$NoPublish,
    [switch]$CreateInstaller
)

$ErrorActionPreference = "Stop"

Write-Host "=============================================" -ForegroundColor Cyan
Write-Host "  RDPWrap Monitor - Release Build" -ForegroundColor Cyan
Write-Host "  Version: $Version" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan
Write-Host ""

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$publishDir = Join-Path $projectRoot "publish"
$outputDir = Join-Path $projectRoot "release"

# Clean previous builds
if (Test-Path $publishDir) {
    Write-Host "Cleaning previous build..." -ForegroundColor Yellow
    Remove-Item $publishDir -Recurse -Force
}
if (Test-Path $outputDir) {
    Remove-Item $outputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $outputDir | Out-Null

if (-not $NoPublish) {
    Write-Host ""
    Write-Host "Building Service (win-x64)..." -ForegroundColor Green
    dotnet publish "$projectRoot/src/RdpWrapMonitor.Service/RdpWrapMonitor.Service.csproj" `
        -c Release `
        -r win-x64 `
        --self-contained true `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:PublishDir="$publishDir/service/win-x64" `
        /p:Version=$Version

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to build Service!"
        exit 1
    }

    Write-Host "Building Setup (win-x64)..." -ForegroundColor Green
    dotnet publish "$projectRoot/src/RdpWrapMonitor.Setup/RdpWrapMonitor.Setup.csproj" `
        -c Release `
        -r win-x64 `
        --self-contained true `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:PublishDir="$publishDir/setup/win-x64" `
        /p:Version=$Version

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to build Setup!"
        exit 1
    }

    Write-Host "Building Service (win-arm64)..." -ForegroundColor Green
    dotnet publish "$projectRoot/src/RdpWrapMonitor.Service/RdpWrapMonitor.Service.csproj" `
        -c Release `
        -r win-arm64 `
        --self-contained true `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:PublishDir="$publishDir/service/win-arm64" `
        /p:Version=$Version

    Write-Host "Building Setup (win-arm64)..." -ForegroundColor Green
    dotnet publish "$projectRoot/src/RdpWrapMonitor.Setup/RdpWrapMonitor.Setup.csproj" `
        -c Release `
        -r win-arm64 `
        --self-contained true `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:PublishDir="$publishDir/setup/win-arm64" `
        /p:Version=$Version
}

# Create ZIP packages
Write-Host ""
Write-Host "Creating ZIP packages..." -ForegroundColor Green

# Full package (x64)
Compress-Archive -Path "$publishDir/*" -DestinationPath "$outputDir/RDPWrapMonitor-$Version.zip" -Force
Write-Host "  Created: RDPWrapMonitor-$Version.zip" -ForegroundColor Gray

# Service only (x64)
Compress-Archive -Path "$publishDir/service/win-x64/*" -DestinationPath "$outputDir/RDPWrapMonitor.Service-$Version.zip" -Force
Write-Host "  Created: RDPWrapMonitor.Service-$Version.zip" -ForegroundColor Gray

# Setup only (x64)
Compress-Archive -Path "$publishDir/setup/win-x64/*" -DestinationPath "$outputDir/RDPWrapMonitor.Setup-$Version.zip" -Force
Write-Host "  Created: RDPWrapMonitor.Setup-$Version.zip" -ForegroundColor Gray

# ARM64 package
Compress-Archive -Path "$publishDir/service/win-arm64/*" -DestinationPath "$outputDir/RDPWrapMonitor.Service-$Version-arm64.zip" -Force
Compress-Archive -Path "$publishDir/setup/win-arm64/*" -DestinationPath "$outputDir/RDPWrapMonitor.Setup-$Version-arm64.zip" -Force
Write-Host "  Created: *-arm64.zip packages" -ForegroundColor Gray

# Create installer with Inno Setup (if available)
if ($CreateInstaller) {
    Write-Host ""
    Write-Host "Creating Inno Setup installer..." -ForegroundColor Green

    $innoPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (Test-Path $innoPath) {
        Push-Location "$projectRoot/installer"
        & $innoPath rdpwrap-monitor.iss
        Pop-Location

        if (Test-Path "$projectRoot/installer/Output/*.exe") {
            Copy-Item "$projectRoot/installer/Output/*.exe" -Destination "$outputDir/RDPWrapMonitor-Setup-$Version.exe" -Force
            Write-Host "  Created: RDPWrapMonitor-Setup-$Version.exe" -ForegroundColor Gray
        }
    } else {
        Write-Host "  Inno Setup not found. Skipping installer creation." -ForegroundColor Yellow
        Write-Host "  Download from: https://jrsoftware.org/isinfo.php" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "=============================================" -ForegroundColor Green
Write-Host "  Build Complete!" -ForegroundColor Green
Write-Host "=============================================" -ForegroundColor Green
Write-Host ""
Write-Host "Release packages created in: $outputDir" -ForegroundColor Cyan
Write-Host ""
Write-Host "Files:" -ForegroundColor Cyan
Get-ChildItem $outputDir | ForEach-Object {
    Write-Host "  - $($_.Name) ($([math]::Round($_.Length / 1MB, 2)) MB)" -ForegroundColor Gray
}
Write-Host ""
Write-Host "To create a GitHub release:" -ForegroundColor Cyan
Write-Host "  1. Create a tag: git tag v$Version" -ForegroundColor Gray
Write-Host "  2. Push tag: git push origin v$Version" -ForegroundColor Gray
Write-Host "  3. GitHub Actions will automatically build and attach these files" -ForegroundColor Gray
