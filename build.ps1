param([string]$CompilerPath, [ValidateSet('zh-CN','en-US')][string]$Language='zh-CN')
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$outputPath = Join-Path $projectRoot "build/$Language"
New-Item -ItemType Directory -Force $outputPath | Out-Null
if ($CompilerPath) {
    $sources = Get-ChildItem (Join-Path $projectRoot 'src/*.cs') | ForEach-Object FullName
    $define = if ($Language -eq 'en-US') { '/define:ENGLISH' } else { '/define:CHINESE' }
    & $CompilerPath /nologo /target:winexe /langversion:latest /optimize+ $define "/out:$outputPath/FolderPeek.exe" "/win32manifest:$projectRoot/app.manifest" "/win32icon:$projectRoot/assets/app.ico" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:Microsoft.CSharp.dll $sources
    if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
    Copy-Item (Join-Path $projectRoot 'App.config') (Join-Path $outputPath 'FolderPeek.exe.config') -Force
} else {
    $define = if ($Language -eq 'en-US') { 'ENGLISH' } else { 'CHINESE' }
    & dotnet build (Join-Path $projectRoot 'FolderPeek.csproj') -c Release -o $outputPath "-p:DefineConstants=$define"
    if ($LASTEXITCODE -ne 0) { throw 'Build failed; install .NET 8 SDK or specify -CompilerPath.' }
}
Write-Host "Build complete: $outputPath"
