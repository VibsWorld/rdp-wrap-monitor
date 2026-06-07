# RDPWrap Monitor Service Installation Script
# Run as Administrator

param(
    [switch]$Uninstall
)

$serviceName = "RDPWrap Monitor"
$serviceDisplayName = "RDPWrap Monitor Service"
$serviceDescription = "Monitors for rdpwrap.ini updates and automatically updates RDP Wrapper configuration"

# Paths
$publishDir = ".\publish"
$serviceExe = "$publishDir\RdpWrapMonitor.Service.exe"
$configDir = "$env:APPDATA\RdpWrapMonitor"
$logDir = "$configDir\logs"

function Write-Info { Write-Host $args -ForegroundColor Cyan }
function Write-Success { Write-Host $args -ForegroundColor Green }
function Write-Error { Write-Host $args -ForegroundColor Red }
function Write-Warning { Write-Host $args -ForegroundColor Yellow }

if ($Uninstall) {
    Write-Info "Uninstalling $serviceName..."

    # Stop and remove service
    $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
    if ($service) {
        Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        sc.exe delete $serviceName
        Write-Success "Service removed"
    } else {
        Write-Warning "Service not found"
    }

    # Remove files
    if (Test-Path $publishDir) {
        Remove-Item $publishDir -Recurse -Force
        Write-Success "Publish directory removed"
    }

    Write-Success "Uninstallation complete!"
    return
}

# Check if running as admin
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator"
    Write-Info "Right-click and select 'Run as Administrator'"
    exit 1
}

Write-Info "Building RDPWrap Monitor Service..."

# Build the service
dotnet publish ".\src\RdpWrapMonitor.Service\RdpWrapMonitor.Service.csproj" `
    -c Release `
    -o $publishDir `
    -r win-x64 `
    --self-contained `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) {
    Write-Error "Build failed!"
    exit 1
}

Write-Success "Build successful!"

# Create config and log directories
Write-Info "Creating directories..."
if (-not (Test-Path $configDir)) {
    New-Item -ItemType Directory -Path $configDir | Out-Null
}
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir | Out-Null
}

# Check if service already exists
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Warning "Service '$serviceName' already exists. Removing first..."
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    sc.exe delete $serviceName
    Start-Sleep -Seconds 1
}

# Install the service
Write-Info "Installing $serviceName..."
sc.exe create $serviceName binPath= "\"$serviceExe\"" DisplayName= "$serviceDisplayName" start= auto

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create service!"
    exit 1
}

# Set description
sc.exe description $serviceName "$serviceDescription"

# Start the service
Write-Info "Starting $serviceName..."
Start-Service -Name $serviceName

# Verify
Start-Sleep -Seconds 2
$service = Get-Service -Name $serviceName
if ($service.Status -eq "Running") {
    Write-Success "============================================="
    Write-Success "  Installation successful!"
    Write-Success "============================================="
    Write-Info "Service Name: $serviceName"
    Write-Info "Service Status: $($service.Status)"
    Write-Info "Log file: $logDir\service.log"
    Write-Info ""
    Write-Info "If you haven't run the setup utility yet, run:"
    Write-Info "  dotnet run --project .\src\RdpWrapMonitor.Setup\RdpWrapMonitor.Setup.csproj"
} else {
    Write-Warning "Service installed but not running. Check Event Viewer for errors."
}
