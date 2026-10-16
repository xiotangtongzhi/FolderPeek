#ifndef AppLanguage
  #define AppLanguage "zh-CN"
#endif
#if AppLanguage == "en-US"
  #define ProductName "Folder Peek"
  #define LangFile "compiler:Default.isl"
  #define DesktopLabel "Create a &desktop shortcut"
  #define LaunchLabel "Launch Folder Peek"
  #define RuntimeMessage "Folder Peek requires .NET Framework 4.8 or later. Install it from Microsoft, then run this setup again."
#else
  #define ProductName "目录浮窗 Folder Peek"
  #define LangFile "ChineseSimplified.isl"
  #define DesktopLabel "创建桌面快捷方式(&D)"
  #define LaunchLabel "启动目录浮窗"
  #define RuntimeMessage "目录浮窗需要 .NET Framework 4.8 或更高版本。请从 Microsoft 安装后重新运行本安装程序。"
#endif

[Setup]
AppId={{B937CC16-F84B-4C74-953E-3DDA7F821A50}
AppName={#ProductName}
AppVersion=1.5
AppVerName={#ProductName} 1.5
VersionInfoVersion=1.5.0.0
DefaultDirName={localappdata}\Programs\FolderPeek
DefaultGroupName=Folder Peek
PrivilegesRequired=lowest
MinVersion=10.0
DisableProgramGroupPage=yes
AllowNoIcons=yes
OutputDir=..\dist
OutputBaseFilename=FolderPeek-v1.5-Setup-{#AppLanguage}
SetupIconFile=..\assets\app.ico
UninstallDisplayIcon={app}\FolderPeek.exe
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
ShowLanguageDialog=no
UsePreviousLanguage=no

[Languages]
Name: "default"; MessagesFile: "{#LangFile}"

[Tasks]
Name: "desktopicon"; Description: "{#DesktopLabel}"; Flags: unchecked

[Files]
Source: "..\build\{#AppLanguage}\FolderPeek.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\build\{#AppLanguage}\FolderPeek.exe.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\docs\UserGuide-{#AppLanguage}.txt"; DestDir: "{app}"; DestName: "UserGuide.txt"; Flags: ignoreversion

[Icons]
Name: "{group}\Folder Peek"; Filename: "{app}\FolderPeek.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\Folder Peek"; Filename: "{app}\FolderPeek.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\FolderPeek.exe"; Description: "{#LaunchLabel}"; Flags: nowait postinstall skipifsilent

[Code]
function InitializeSetup(): Boolean;
var Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040);
  if not Result then
    MsgBox('{#RuntimeMessage}' + #13#10 + 'https://dotnet.microsoft.com/download/dotnet-framework/net48', mbError, MB_OK);
end;
