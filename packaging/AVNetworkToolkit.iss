#ifndef AppVersion
  #error AppVersion is required
#endif
#ifndef PayloadDir
  #error PayloadDir is required
#endif
#ifndef OutputDir
  #error OutputDir is required
#endif

[Setup]
AppId={{02E72C36-F6DD-4AB0-92EC-A66017AA227A}
AppName=AV Network Toolkit
AppVersion={#AppVersion}
AppPublisher=AV Network Toolkit contributors
DefaultDirName={autopf}\AV Network Toolkit
DefaultGroupName=AV Network Toolkit
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=AVNetworkToolkit-{#AppVersion}-win-x64-setup
ArchitecturesAllowed=x64os
ArchitecturesInstallIn64BitMode=x64os
MinVersion=10.0
PrivilegesRequired=admin
LicenseFile=..\LICENSE
SetupIconFile=..\app\Assets\icon.ico
UninstallDisplayIcon={app}\NetworkProfileManager.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=no
RestartApplications=no
RestartIfNeededByRun=no
Uninstallable=yes

[Files]
Source: "{#PayloadDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\AV Network Toolkit"; Filename: "{app}\NetworkProfileManager.exe"; WorkingDir: "{app}"

; No automatic launch, startup, file associations, services or user-data deletion.
; LocalAppData and user-chosen project files are outside this installer payload.

[Code]
function AppFilesAvailable: Boolean;
var
  Executable: String;
  Handle: Integer;
begin
  Result := True;
  Executable := ExpandConstant('{app}\NetworkProfileManager.exe');
  if FileExists(Executable) then
  begin
    Handle := FileOpen(Executable, fmOpenReadWrite or fmShareExclusive);
    if Handle = -1 then
      Result := False
    else
      FileClose(Handle);
  end;
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if not AppFilesAvailable then
    Result := 'Close AV Network Toolkit and wait for its active operation to finish before updating.';
end;

function InitializeUninstall: Boolean;
begin
  Result := AppFilesAvailable;
  if not Result then
    SuppressibleMsgBox('Close AV Network Toolkit and wait for its active operation to finish before uninstalling.', mbError, MB_OK, IDOK);
end;
