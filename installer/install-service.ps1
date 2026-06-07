# Post-install script to register and start the Windows Service
# Called by Inno Setup installer

param(
    [string]$InstallPath = "${env:ProgramFiles}\RDPWrap Monitor"
)

$serviceName = "RDPWrap Monitor"
$serviceExe = "$InstallPath\Service\RdpWrapMonitor.Service.exe"

Write-Host "Installing RDPWrap Monitor Service..."

# Check if running as admin
if (-not ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "This script must be run as Administrator"
    exit 1
}

# Check if service already exists
$existingService = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "Service already exists. Updating..."
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    sc.exe delete $serviceName
    Start-Sleep -Seconds 1
}

# Create config directory
$configDir = "$env:APPDATA\RdpWrapMonitor"
$logDir = "$configDir\logs"
if (-not (Test-Path $configDir)) {
    New-Item -ItemType Directory -Path $configDir | Out-Null
    Write-Host "Created config directory: $configDir"
}
if (-not (Test-Path $logDir)) {
    New-Item -ItemType Directory -Path $logDir | Out-Null
    Write-Host "Created log directory: $logDir"
}

# Install the service
Write-Host "Creating Windows Service..."
sc.exe create $serviceName binPath= "\"$serviceExe\"" DisplayName= "RDPWrap Monitor Service" start= auto

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to create service!"
    exit 1
}

# Set description
sc.exe description $serviceName "Monitors for rdpwrap.ini updates and automatically updates RDP Wrapper configuration"

# Start the service
Write-Host "Starting service..."
Start-Service -Name $serviceName

# Verify
Start-Sleep -Seconds 2
$service = Get-Service -Name $serviceName
if ($service.Status -eq "Running") {
    Write-Host "Service installed and started successfully!" -ForegroundColor Green
} else {
    Write-Warning "Service installed but not running. Check Event Viewer for errors."
}
