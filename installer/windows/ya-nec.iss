; YA-NEC - installatore Windows (Inno Setup 6). Richiesta di IC8TEM del 16/11/2026 15:55 ("mi devi fare l'installatore per ya-nec");
; scelte approvate alle 16:00: cartella C:\YA-NEC modificabile (niente Programmi, niente AppData: impostazioni e modelli restano in
; C:\YA-NEC\data come con lo zip, cosi' l'aggiornamento automatico continua a funzionare), collegamenti nel menu Start e sul desktop.
; Compilato da .github/workflows/build.yml: iscc /DVer=1.0.5 /DArch=x64 /DSrc=<cartella dello zip> /DOut=<uscita> ya-nec.iss
#ifndef YVer
  #define YVer "0.0.0"
#endif
#ifndef YArch
  #define YArch "x64"
#endif
#ifndef YNome
  #define YNome "win-" + YArch
#endif

[Setup]
AppId={{6B1F4C2E-3A8D-4E6B-9C1A-5E3D7A0C1E05}
AppName=YA-NEC
AppVersion={#YVer}
AppVerName=YA-NEC {#YVer}
AppPublisher=Costantino Cerrotta IC8TEM
AppPublisherURL=https://github.com/ic8tem/YA-NEC
AppSupportURL=https://github.com/ic8tem/YA-NEC/wiki
AppUpdatesURL=https://github.com/ic8tem/YA-NEC/releases
DefaultDirName={sd}\YA-NEC
DefaultGroupName=YA-NEC
DisableProgramGroupPage=yes
LicenseFile={#YSrc}\LICENSE
SetupIconFile={#YIcona}
UninstallDisplayIcon={app}\YA-NEC.exe
OutputDir={#YOut}
OutputBaseFilename=YA-NEC-{#YVer}-{#YNome}-setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
; niente diritti di amministratore: la cartella resta dell'utente e scrivibile
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
UsePreviousAppDir=yes
#if YArch == "x64"
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#elif YArch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#endif
#ifdef YWin7
MinVersion=6.1sp1
#else
MinVersion=10.0
#endif

[Languages]
Name: "it"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "en"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktop"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; i file di lavoro dell'utente (data\config.ini, modelli salvati, registri) non vengono toccati: si installano solo i file del pacchetto
Source: "{#YSrc}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\YA-NEC"; Filename: "{app}\YA-NEC.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\YA-NEC"; Filename: "{app}\YA-NEC.exe"; WorkingDir: "{app}"; Tasks: desktop

[Run]
Filename: "{app}\YA-NEC.exe"; Description: "{cm:LaunchProgram,YA-NEC}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; si tolgono solo i file temporanei; impostazioni e modelli dell'utente in data\ restano (l'utente li cancella se vuole)
Type: filesandordirs; Name: "{app}\data\tmp"
