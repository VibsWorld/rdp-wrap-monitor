# RDPWrap Monitor Service

A Windows Service that automatically monitors and updates RDP Wrapper's `rdpwrap.ini` file.

## Features

- **Automatic Updates**: Checks every 6 hours (configurable) for new rdpwrap.ini versions
- **Email Notifications**: Sends Gmail notifications when updates are applied
- **Automatic Service Restart**: Stops TermService, updates INI, and restarts TermService
- **Error Handling**: Sends email alerts on failures
- **File Logging**: All operations logged to `%APPDATA%\RdpWrapMonitor\logs\service.log`

## Prerequisites

1. **.NET 8.0 SDK** - Download from https://dotnet.microsoft.com/download/dotnet/8.0
2. **Gmail App Password** - Required for email notifications
3. **RDP Wrapper** - Must be installed at `C:\Program Files\RDP Wrapper\`

## Gmail App Password Setup

Before configuring the service, you need to create a Gmail App Password:

1. Go to https://myaccount.google.com/apppasswords
2. If prompted, enable 2-Factor Authentication on your Google Account
3. Click "App passwords"
4. Under "App", select "Mail"
5. Under "Device", select "Windows Computer"
6. Click "Generate"
7. **Copy the 16-character password** (you won't see it again)

## Installation

### Step 1: Run Setup Utility

First, configure your Gmail credentials:

```powershell
cd C:\Vaibhav\PVR\RdpWrapMonitor
dotnet run --project .\src\RdpWrapMonitor.Setup\RdpWrapMonitor.Setup.csproj
```

Follow the prompts to enter:
- Gmail address
- Gmail App Password (16 characters)
- Recipient email (default: same as Gmail)
- Check interval in hours (default: 6)

### Step 2: Install the Service

Run the installation script **as Administrator**:

```powershell
# Right-click PowerShell and select "Run as Administrator"
cd C:\Vaibhav\PVR\RdpWrapMonitor
.\install.ps1
```

### Step 3: Verify Installation

1. Open **Services** (`services.msc`)
2. Find **RDPWrap Monitor** in the list
3. Status should be **Running**

## Uninstallation

```powershell
.\install.ps1 -Uninstall
```

## Configuration

Configuration is stored in: `%APPDATA%\RdpWrapMonitor\config.json`

The password is encrypted using Windows DPAPI (tied to your user account).

### Manual Configuration Edit

If you need to reconfigure without the setup utility:

1. Stop the service: `Stop-Service "RDPWrap Monitor"`
2. Delete `%APPDATA%\RdpWrapMonitor\config.json`
3. Run the setup utility again
4. Start the service: `Start-Service "RDPWrap Monitor"`

## Logs

- **Log file**: `%APPDATA%\RdpWrapMonitor\logs\service.log`
- **Windows Event Log**: Event Viewer → Applications and Services Logs → RDPWrap Monitor

## Troubleshooting

### Service won't start

1. Check the log file at `%APPDATA%\RdpWrapMonitor\logs\service.log`
2. Check Windows Event Viewer for errors
3. Ensure config.json exists and is valid JSON

### Not detecting updates

1. Verify the remote URL is accessible in your browser
2. Check that `C:\Program Files\RDP Wrapper\rdpwrap.ini` exists
3. Review logs for hash comparison details

### Email not sending

1. Verify Gmail App Password is correct (16 characters, no spaces)
2. Ensure 2FA is enabled on your Gmail account
3. Check that "Less secure app access" isn't required (App Password bypasses this)
4. Review logs for SMTP errors

### TermService fails to restart

1. Ensure you're running as a user with service control permissions
2. Check if TermService has any dependencies that might be failing
3. Review Windows Event Viewer → System log for TermService errors

## Project Structure

```
RdpWrapMonitor/
├── src/
│   ├── RdpWrapMonitor.Service/    # Main Windows Service
│   │   ├── Program.cs              # Entry point
│   │   ├── RdpWrapMonitorService.cs # Background service logic
│   │   ├── Config/
│   │   │   └── ServiceConfig.cs    # Configuration model
│   │   ├── Monitor/
│   │   │   └── IniMonitor.cs       # Remote/local file comparison
│   │   ├── Email/
│   │   │   └── EmailNotifier.cs    # Gmail SMTP sender
│   │   └── Utils/
│   │       └── TermServiceController.cs # Service control
│   └── RdpWrapMonitor.Setup/       # One-time config utility
│       └── Program.cs              # Setup wizard
├── install.ps1                      # Installation script
└── README.md
```

## License

This project is provided as-is for personal use.

## Support

For RDP Wrapper issues, visit: https://github.com/stascorp/rdpwrap
