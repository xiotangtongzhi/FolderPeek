param([Parameter(Mandatory=$true)][string]$Probe)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot
$testRoot=Join-Path ([IO.Path]::GetTempPath()) ('FolderPeek-install-test-'+[guid]::NewGuid().ToString('N'))
$installPath=Join-Path $testRoot 'app'
New-Item -ItemType Directory -Force $testRoot | Out-Null
$key='HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{B937CC16-F84B-4C74-953E-3DDA7F821A50}_is1'
if(Test-Path $key){throw 'A real Folder Peek installation already exists; run installation tests in a clean Windows account.'}
try {
 foreach($lang in @('zh-CN','en-US')) {
  $args=@('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/NOICONS','/TASKS=!desktopicon',('/DIR="'+$installPath+'"'),('/LOG="'+$testRoot+'/'+$lang+'.log"'))
  $p=Start-Process "$root/dist/FolderPeek-v1.5-Setup-$lang.exe" -ArgumentList $args -WindowStyle Hidden -PassThru -Wait
  if($p.ExitCode -ne 0){throw "Install failed: $lang ($($p.ExitCode))"}
  $installed=Join-Path $installPath 'FolderPeek.exe'
  if((Get-FileHash $installed).Hash -ne (Get-FileHash "$root/build/$lang/FolderPeek.exe").Hash){throw 'Installed executable hash mismatch'}
  if(!(Test-Path $key)){throw 'Missing uninstall registration'}
  & $Probe $installed $lang
  if($LASTEXITCODE -ne 0){throw 'Installed executable probe failed'}
  Write-Host "PASS installation / language replacement / payload integrity: $lang"
 }
} finally {
 $uninstaller=Join-Path $installPath 'unins000.exe'
 if(Test-Path $uninstaller){$p=Start-Process $uninstaller -ArgumentList @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART') -WindowStyle Hidden -PassThru -Wait; if($p.ExitCode -ne 0){throw 'Uninstall failed'}}
}
if((Test-Path "$installPath/FolderPeek.exe") -or (Test-Path $key)){throw 'Uninstall did not remove executable/registration'}
Write-Host 'PASS uninstall removes application and registration'
Write-Host "Installer logs: $testRoot"
