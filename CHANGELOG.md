# Changelog

## 1.0.4 (1.0.3 was published by mistake with the 1.0.2 code)
- New: right-click -> "Save diagnostics" writes `%APPDATA%\BatteryWidget\diagnostic.txt` (Bluetooth devices, USB/HID collections of the supported brands, what Corsair and PlayStation devices answer). Attach it to an issue to get a device supported.

## 1.0.2
- Corsair: the Katar / Slipstream family talks to its dongle with an unnumbered report; the widget now tries both framings and every vendor HID collection (with a back-off so it stays cheap). `--debug` lists the Corsair collections it sees.

## 1.0.1
- Fixed: the widget no longer vanishes and reappears when a device connects or disconnects (removal is confirmed before it is applied; Windows device events are handled at once).
- Fixed: with an auto-hiding taskbar the widget now shows and hides together with the taskbar, without the ~2 s delay.
- Fixed: a charging cable no longer makes devices read 100 %; the last real level is kept and the charge is estimated until the device is trustworthy again.
- New: Corsair mice/headsets over the "Bragi" protocol (Katar, Slipstream dongle, Virtuoso, HS80) and PlayStation DualShock 4 (Bluetooth/USB).
- New: crash/error log in `%APPDATA%\BatteryWidget\error.log`; the installer verifies the widget actually started; the "no devices" icon is easier to see.

## 1.0.0
- Live battery of wireless devices on the Windows 11 taskbar, only while they are connected.
- Logitech (via G HUB), HyperX Cloud II Wireless, GameSir 2.4 GHz, Bluetooth devices that report battery, wireless XInput pads.
- Green / amber / red by level, per device. Drag to move, double-click to resize, multi-monitor aware.
- Per-user installer that enables start with Windows; uninstaller registered in "Installed apps".
- Line icons; hides while any app is full screen on the same monitor; stays above the taskbar when you use Start/tray.
- More brands: Logitech HID++ (no G HUB needed), Razer, Corsair and more HyperX headsets, Bluetooth LE devices (ASUS, Redragon, ...).

