# Contributing

## Build and try it

```powershell
.\build.ps1
.\dist\BatteryWidget.exe --debug --always-visible   # log: %APPDATA%\BatteryWidget\debug.log
```

`--shots=<folder>` renders the real UI (widget, menus, tooltip, size popover) to PNG files, handy for reviewing visual changes.

## Adding a device

1. Add a class implementing `ISource` in `src/Sources.cs`. `Poll()` returns the devices that are **connected right now** (`Key` must be stable, `Percent` 0–100 or -1 if unknown). Return `null` if the read failed, so the previous value is kept for a moment instead of blinking out.
2. Register it in `src/Program.cs`.
3. Keep it cheap: sources are polled every 20–30 s on a background thread; open and close handles on each poll.
4. Note what the device needs (software running, firmware, etc.) in the README table.

## Code style

C# 5 (the compiler that ships with Windows). No external dependencies. UI text is Spanish in code with English in `src/Loc.cs`.
