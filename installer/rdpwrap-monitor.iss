; Inno Setup Script for RDPWrap Monitor Service
; Requires Inno Setup Compiler (ISCC) - https://jrsoftware.org/isinfo.php

#define MyAppName "RDPWrap Monitor Service"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "VibsWorld"
#define MyAppExeName "RdpWrapMonitor.Service.exe"
#define MyAppSetupName "RdpWrapMonitor.Setup.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL=https://github.com/VibsWorld/rdp-wrap-monitor
AppSupportURL=https://github.com/VibsWorld/rdp-wrap-monitor/issues
AppUpdatesURL=https://github.com/VibsWorld/rdp-wrap-monitor/releases
DefaultDirName={autopf}\RDPWrap Monitor
DefaultGroupName=RDPWrap Monitor
LicenseFile=LICENSE.rtf
OutputDir=Output
OutputBaseFilename=RDPWrapMonitor-Setup-{#MyAppVersion}
; SetupIconFile=setup.ico  ; Optional: add your own icon
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startservice"; Description: "Start the service after installation"; GroupDescription: "Post-installation tasks"; Flags: checkedonce

[Files]
; Service files - supports both local build and GitHub Actions paths
; Check for GitHub Actions publish path first
#if FileExists("..\publish\service\win-x64\RdpWrapMonitor.Service.exe")
Source: "..\publish\service\win-x64\*"; DestDir: "{app}\Service"; Flags: recursesubdirs ignoreversion
Source: "..\publish\setup\win-x64\*"; DestDir: "{app}\Setup"; Flags: recursesubdirs ignoreversion
#else
; Fall back to local build path
#if FileExists("..\src\RdpWrapMonitor.Service\bin\Release\net8.0-windows\win-x64\publish\RdpWrapMonitor.Service.exe")
Source: "..\src\RdpWrapMonitor.Service\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}\Service"; Flags: recursesubdirs ignoreversion
Source: "..\src\RdpWrapMonitor.Setup\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}\Setup"; Flags: recursesubdirs ignoreversion
#endif
#endif
#if FileExists("..\README.md")
Source: "..\README.md"; DestDir: "{app}"; Flags: isreadme
#endif
Source: "install-service.ps1"; DestDir: "{app}"; Flags: skipifsourcedoesntexist

[Icons]
Name: "{group}\Configure RDPWrap Monitor"; Filename: "{app}\Setup\RdpWrapMonitor.Setup.exe"
Name: "{group}\Open Log Folder"; Filename: "{appdata}\RdpWrapMonitor\logs"
Name: "{group}\Open Config Folder"; Filename: "{appdata}\RdpWrapMonitor"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\Setup\RdpWrapMonitor.Setup.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Service\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
Filename: "powershell.exe"; Parameters: "-ExecutionPolicy Bypass -File ""{app}\install-service.ps1"""; Description: "Install Windows Service"; Flags: runhidden waituntilterminated postinstall

[Code]
var
  ServicePage: TWizardPage;
  ServiceCheckBox: TCheckBox;

procedure InitializeWizard;
begin
  ServicePage := CreateCustomPage(wpInstalling, 'Service Installation', 'Configure the Windows Service installation.');
  ServiceCheckBox := TCheckBox.Create(WizardForm);
  ServiceCheckBox.Parent := ServicePage.Surface;
  ServiceCheckBox.Left := 10;
  ServiceCheckBox.Top := 10;
  ServiceCheckBox.Width := 400;
  ServiceCheckBox.Caption := 'Install RDPWrap Monitor as a Windows Service (requires Administrator)';
  ServiceCheckBox.Checked := True;
end

function NextButtonClicked: Boolean;
begin
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    if ServiceCheckBox.Checked then
    begin
      // Install and start the service
      if not Exec('sc', 'create "RDPWrap Monitor" binPath= "' + ExpandConstant('{app}') + '\Service\RdpWrapMonitor.Service.exe" DisplayName= "RDPWrap Monitor Service" start= auto', '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
      begin
        MsgBox('Failed to create Windows Service. Error code: ' + IntToStr(ResultCode), mbError, MB_OK);
      end
      else
      begin
        // Set service description
        Exec('sc', 'description "RDPWrap Monitor" "Monitors for rdpwrap.ini updates and automatically updates RDP Wrapper configuration"', '', SW_HIDE, ewNoWait, ResultCode);
        // Start the service
        Exec('sc', 'start "RDPWrap Monitor"', '', SW_HIDE, ewNoWait, ResultCode);
      end;
    end;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    // Stop and remove the service
    Exec('sc', 'stop "RDPWrap Monitor"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(2000);
    Exec('sc', 'delete "RDPWrap Monitor"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;
