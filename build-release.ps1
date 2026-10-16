param([string]$CompilerPath, [string]$InnoCompiler = "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe")
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $InnoCompiler)) { throw 'Specify -InnoCompiler with the path to ISCC.exe (Inno Setup 6.5+).' }
$staging = Join-Path ([IO.Path]::GetTempPath()) ('FolderPeek-build-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $staging, "$PSScriptRoot/dist" | Out-Null
foreach ($language in @('zh-CN','en-US')) {
    & "$PSScriptRoot/build.ps1" -CompilerPath $CompilerPath -Language $language
    & $InnoCompiler "/O$staging" "/DAppLanguage=$language" "$PSScriptRoot/installer/FolderPeek.iss"
    if ($LASTEXITCODE -ne 0) { throw "Installer build failed: $language" }
}
Copy-Item "$staging/*.exe" "$PSScriptRoot/dist" -Force
Get-ChildItem "$PSScriptRoot/dist/*.exe" | ForEach-Object {
    '{0}  {1}' -f (Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.Name
} | Set-Content "$PSScriptRoot/dist/SHA256SUMS.txt" -Encoding ascii
