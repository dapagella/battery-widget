# Builds everything needed for a GitHub release:
#   dist\BatteryWidget.exe          the widget (portable)
#   dist\BatteryWidget-Setup.exe    installer (embeds the widget; sets up autostart)
#   dist\BatteryWidget-portable.zip
# Needs only Windows (uses the .NET Framework compiler that ships with it).
param([string]$Version = "1.0.0")
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$fw   = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$out  = Join-Path $here 'dist'

if (-not (Test-Path "$here\assets\icon.ico")) { & powershell -NoProfile -ExecutionPolicy Bypass -File "$here\tools\make-icon.ps1" }

& "$here\build.ps1" -Version $Version
if ($LASTEXITCODE -ne 0) { throw "widget build failed" }

# installer: single exe with the widget embedded as a resource
$setup = "$out\BatteryWidget-Setup.exe"
$refs = @("$fw\System.dll", "$fw\System.Windows.Forms.dll", "$fw\System.Core.dll", "$fw\Microsoft.CSharp.dll")
$csc = @('-nologo', '-target:winexe', '-optimize+', "-out:$setup", "-win32icon:$here\assets\icon.ico",
         "-win32manifest:$here\installer\setup.manifest", "-resource:$out\BatteryWidget.exe,BatteryWidget.exe") + ($refs | % { "-r:$_" }) + "$here\installer\Setup.cs" + "$here\src\AutoStartTask.cs"
& "$fw\csc.exe" @csc
if ($LASTEXITCODE -ne 0) { throw "installer build failed" }

Compress-Archive -Force -Path "$out\BatteryWidget.exe" -DestinationPath "$out\BatteryWidget-portable.zip"
Get-ChildItem $out -File | Select-Object Name, @{n='KB';e={[int]($_.Length/1KB)}} | Format-Table -AutoSize
Write-Host "OK -> $setup"
