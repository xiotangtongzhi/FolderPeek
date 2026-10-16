param([Parameter(Mandatory=$true)][string]$CompilerPath, [switch]$Installers)
$ErrorActionPreference='Stop'
$out=Join-Path $PSScriptRoot 'build/tests'
New-Item -ItemType Directory -Force $out | Out-Null
$sources=Get-ChildItem "$PSScriptRoot/src/*.cs" | ForEach-Object FullName
foreach($language in @('zh-CN','en-US')) {
 $define=if($language -eq 'en-US'){'/define:ENGLISH'}else{'/define:CHINESE'}
 $dir=Join-Path $out $language
 New-Item -ItemType Directory -Force $dir | Out-Null
 & $CompilerPath /nologo /target:exe /langversion:latest $define /main:Smoke "/out:$dir/Smoke.exe" "/win32manifest:$PSScriptRoot/app.manifest" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:Microsoft.CSharp.dll $sources "$PSScriptRoot/tests/Smoke.cs"
 if($LASTEXITCODE -ne 0){throw 'Smoke build failed'}
 Copy-Item "$PSScriptRoot/App.config" "$dir/Smoke.exe.config" -Force
 & "$dir/Smoke.exe" "$dir/day.png"
 if($LASTEXITCODE -ne 0){throw "GUI checks failed: $language"}
}
& $CompilerPath /nologo /target:exe /langversion:latest /main:FollowTest "/out:$out/FollowTest.exe" "/win32manifest:$PSScriptRoot/app.manifest" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:Microsoft.CSharp.dll $sources "$PSScriptRoot/tests/FollowTest.cs"
if($LASTEXITCODE -ne 0){throw 'Follow test build failed'}
Copy-Item "$PSScriptRoot/App.config" "$out/FollowTest.exe.config" -Force
& "$out/FollowTest.exe"
if($LASTEXITCODE -ne 0){throw 'Explorer follow checks failed'}
if($Installers){
 & $CompilerPath /nologo /target:exe /langversion:latest "/out:$out/InstalledProbe.exe" /r:System.Windows.Forms.dll "$PSScriptRoot/tests/InstalledProbe.cs"
 if($LASTEXITCODE -ne 0){throw 'Installed probe build failed'}
 Copy-Item "$PSScriptRoot/App.config" "$out/InstalledProbe.exe.config" -Force
 & "$PSScriptRoot/tests/InstallTest.ps1" -Probe "$out/InstalledProbe.exe"
}
