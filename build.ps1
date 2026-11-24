# Compiles dist\BatteryWidget.exe with the .NET Framework compiler that already ships with Windows (nothing to install).
param([string]$Version = "1.0.0")
$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$fw   = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
$gac  = 'C:\Windows\Microsoft.NET\assembly\GAC_MSIL'
$out  = Join-Path $here 'dist'
New-Item -ItemType Directory -Force $out | Out-Null
function Find-Dll($name) {
  foreach ($p in @("$fw\$name.dll", "$fw\WPF\$name.dll")) { if (Test-Path $p) { return $p } }
  $f = Get-ChildItem $gac -Recurse -Filter "$name.dll" -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($f) { return $f.FullName }
  throw "Could not find $name.dll"
}
$refs = 'PresentationFramework','PresentationCore','WindowsBase','System.Xaml','UIAutomationClient','UIAutomationTypes','System.Web.Extensions','System','System.Core' | % { Find-Dll $_ }

# version info (shown in file properties)
$info = Join-Path $out 'AssemblyInfo.generated.cs'
@"
using System.Reflection;
[assembly: AssemblyTitle("Battery Widget")]
[assembly: AssemblyProduct("Battery Widget")]
[assembly: AssemblyDescription("Live battery level of your wireless devices, on the Windows taskbar")]
[assembly: AssemblyVersion("$Version.0")]
[assembly: AssemblyFileVersion("$Version.0")]
"@ | Set-Content $info -Encoding UTF8

$icon = Join-Path $here 'assets\icon.ico'
$csc = @('-nologo', '-target:winexe', '-optimize+', "-out:$out\BatteryWidget.exe", "-win32manifest:$here\app.manifest") +
       $(if (Test-Path $icon) { "-win32icon:$icon" } else { @() }) +
       ($refs | % { "-r:$_" }) + (Get-ChildItem "$here\src\*.cs" | % FullName) + $info
& "$fw\csc.exe" @csc
if ($LASTEXITCODE -ne 0) { throw "Build failed" }
Write-Host "OK -> $out\BatteryWidget.exe"
