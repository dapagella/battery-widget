# Builds the widget and runs the notification-rule tests (scripted battery readings + a fake clock) against the real code.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$fw = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319'
function Find-Dll($name) {
  foreach ($p in @("$fw\$name.dll", "$fw\WPF\$name.dll")) { if (Test-Path $p) { return $p } }
  (Get-ChildItem 'C:\Windows\Microsoft.NET\assembly\GAC_MSIL' -Recurse -Filter "$name.dll" | Select-Object -First 1).FullName
}
& "$root\build.ps1" | Out-Null
$out = Join-Path $root 'dist\tests'; New-Item -ItemType Directory -Force $out | Out-Null
Copy-Item "$root\dist\BatteryWidget.exe" "$out\BatteryWidget.exe" -Force
$refs = 'PresentationCore','PresentationFramework','WindowsBase','System' | % { Find-Dll $_ }
& "$fw\csc.exe" -nologo "-out:$out\AlertTests.exe" "-r:$out\BatteryWidget.exe" ($refs | % { "-r:$_" }) "$PSScriptRoot\AlertTests.cs"
if ($LASTEXITCODE -ne 0) { throw 'test build failed' }
& "$out\AlertTests.exe"
exit $LASTEXITCODE
