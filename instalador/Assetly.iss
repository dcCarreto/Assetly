; Instalador do Assetly (Inno Setup 6).
;
; Compilação (na raiz do repositório, depois de publicar o executável em "publicar\"):
;   ISCC.exe /DVersao=2026.11.23 instalador\Assetly.iss
; Saída: publicar\instalador\Assetly-Setup-2026.11.23.exe
;
; O Assetly não usa número de versão: cada release é identificada pela data de publicação
; (AAAA.MM.DD, ou AAAA.MM.DD.N quando sai mais de uma no mesmo dia). O workflow release.yml calcula a
; data, publica o executável com essa versão e chama este script.
;
; Instalação por usuário (sem pedir administrador), em %LOCALAPPDATA%\Programs\Assetly: é isso que permite
; ao próprio Assetly baixar uma versão nova e atualizar-se em silêncio. Os dados do usuário (%USERPROFILE%\.assetly)
; nunca são tocados pelo instalador nem pelo desinstalador.

#ifndef Versao
  #define Versao "0.0.0"
#endif
#ifndef PastaPublicacao
  #define PastaPublicacao "..\publicar"
#endif

#define NomeApp "Assetly"
#define ExeApp "Assetly.exe"
; Mesmo nome usado pelo programa (App.NomeTravaInstalador): enquanto existir, o Assetly está aberto.
#define TravaApp "AssetlyAplicacaoAberta"

[Setup]
AppId={{7B3E2A91-5C4D-4F6E-9A8B-1D2C3E4F5A60}
AppName={#NomeApp}
AppVersion={#Versao}
AppVerName={#NomeApp} ({#Versao})
AppPublisher=Denis Carreto
AppPublisherURL=https://github.com/dcCarreto/Assetly
AppSupportURL=https://github.com/dcCarreto/Assetly/issues
AppUpdatesURL=https://github.com/dcCarreto/Assetly/releases
AppCopyright=© Denis Carreto
VersionInfoVersion={#Versao}
VersionInfoProductName={#NomeApp}
VersionInfoDescription=Instalador do {#NomeApp}
DefaultDirName={autopf}\{#NomeApp}
DefaultGroupName={#NomeApp}
DisableProgramGroupPage=yes
DisableDirPage=auto
DisableReadyPage=no
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19041
OutputDir={#PastaPublicacao}\instalador
OutputBaseFilename=Assetly-Setup-{#Versao}
SetupIconFile=..\src\Assetly.Apresentacao\Recursos\Imagens\assetly.ico
UninstallDisplayIcon={app}\{#ExeApp}
UninstallDisplayName={#NomeApp}
WizardStyle=modern
WizardImageFile=imagens\instalador-lateral-100.bmp,imagens\instalador-lateral.bmp
WizardSmallImageFile=imagens\instalador-pequena-100.bmp,imagens\instalador-pequena.bmp
Compression=lzma2/ultra64
SolidCompression=yes
; O próprio script espera o Assetly fechar (ver InitializeSetup); o Gerenciador de Reinicialização não é usado.
CloseApplications=no
RestartApplications=no
ShowLanguageDialog=no
#ifdef Assinar
SignTool=assetly
SignedUninstaller=yes
#endif

[Languages]
Name: "ptbr"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[CustomMessages]
ptbr.AtalhoAreaTrabalho=Criar atalho na área de trabalho
ptbr.AbrirAssetly=Abrir o Assetly agora
ptbr.AssetlyAberto=O Assetly está aberto. Feche o programa para continuar a instalação.%n%nSeus dados não são afetados pela instalação.

[Tasks]
Name: "atalhodesktop"; Description: "{cm:AtalhoAreaTrabalho}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PastaPublicacao}\{#ExeApp}"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#NomeApp}"; Filename: "{app}\{#ExeApp}"; Comment: "Seu patrimônio, organizado."
Name: "{autoprograms}\{#NomeApp} (ambiente de teste)"; Filename: "{app}\{#ExeApp}"; Parameters: "--teste"; Comment: "Abre o Assetly com dados fictícios, separados dos seus dados reais."
Name: "{autodesktop}\{#NomeApp}"; Filename: "{app}\{#ExeApp}"; Tasks: atalhodesktop

[Run]
; Instalação normal: oferece abrir o programa no fim.
Filename: "{app}\{#ExeApp}"; Description: "{cm:AbrirAssetly}"; Flags: nowait postinstall skipifsilent
; Atualização automática (silenciosa): o Assetly pede para ser reaberto com /REABRIR.
Filename: "{app}\{#ExeApp}"; Flags: nowait; Check: DeveReabrir

[Code]
function AssetlyAberto(): Boolean;
begin
  Result := CheckForMutexes('{#TravaApp}');
end;

{ O Assetly inicia o instalador e fecha em seguida: espera até 30 s ele terminar de fechar. Numa instalação
  manual com o programa aberto, pede para fechar (Repetir/Cancelar). Em modo silencioso, desiste sem alterar nada. }
function InitializeSetup(): Boolean;
var
  Tentativas: Integer;
begin
  Tentativas := 0;
  while AssetlyAberto() and (Tentativas < 60) do
  begin
    Sleep(500);
    Tentativas := Tentativas + 1;
  end;

  while AssetlyAberto() do
  begin
    if WizardSilent() then
    begin
      Log('O Assetly continua aberto; atualização cancelada.');
      Result := False;
      exit;
    end;
    if SuppressibleMsgBox(CustomMessage('AssetlyAberto'), mbError, MB_RETRYCANCEL, IDCANCEL) = IDCANCEL then
    begin
      Result := False;
      exit;
    end;
  end;

  Result := True;
end;

function DeveReabrir(): Boolean;
var
  I: Integer;
begin
  Result := False;
  for I := 1 to ParamCount do
    if CompareText(ParamStr(I), '/REABRIR') = 0 then
      Result := True;
end;

function InitializeUninstall(): Boolean;
begin
  Result := True;
  while CheckForMutexes('{#TravaApp}') do
  begin
    if UninstallSilent() then
    begin
      Result := False;
      exit;
    end;
    if MsgBox(CustomMessage('AssetlyAberto'), mbError, MB_RETRYCANCEL) = IDCANCEL then
    begin
      Result := False;
      exit;
    end;
  end;
end;
