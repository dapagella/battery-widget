# Changelog

## 1.2.3
- Fixed: the right-click menu stayed open (blurry) until you clicked the widget. It now closes with a click anywhere outside it, or Esc.
- Fixed: notices, the hover card, the size box and the menu no longer overlap. A notice waits while a menu or the size box is open, the hover card stays away while a notice shows, and nothing is placed past the screen edge.

## 1.2.2
- Fixed: a Bluetooth device (gamepad, headset...) kept showing its battery for about two minutes after you switched Bluetooth off or it disconnected. Bluetooth and XInput devices now disappear as soon as Windows reports them gone; only USB receivers keep the short grace period that stops them blinking.

## 1.2.1
- Fixed: devices that are on and connected sometimes vanished for a moment. A device is now kept on screen with its last reading until it has been missing for 90 s or more, so a missed poll can no longer make it blink out.
- Fixed: DualShock 4 over Bluetooth was never found (only USB). Windows names Bluetooth HID paths differently (`vid&0002054c`); the matcher now understands both. Tests cover both path forms.

## 1.2.0
- Fixed: the widget sometimes crashed right after sign-in (Windows event: clr.dll, 0xC0000005). Overlapped USB/HID reads used memory the garbage collector could move while Windows was still finishing them; all HID I/O now uses fixed memory, never frees memory a cancelled request may still touch, runs one conversation at a time, and the first readings are staggered.
- Fixed: a safety net. The logon task now also relaunches the widget every 5 minutes if it is not running (it does nothing after you chose Exit yourself, until you sign in again).
- Fixed: settings. The newest of the settings file and its backup always wins, what is written is read back and verified, settings are also saved when Windows shuts down, and `startup.log` records every load and save.
- New: any Bluetooth LE device with the standard Battery Service is read directly (mice, keyboards, headsets, earbuds, trackers from any brand).
- Improved: DualShock 4 over Bluetooth asks for both calibration reports and listens longer; "Save diagnostics" shows exactly what the pad answers.

## 1.1.1
- Notifications are now stricter: a reading must hold for 30 s before it is announced (one glitchy reading can't fire an alert); estimates and coarse readings (4-step gamepads) never trigger; "fully charged" only counts a real climb to 100 %. 19 scripted scenarios in `tests/` run on every build.

## 1.1.0
- New: notifications. Low battery at 20 % and 10 %, and once when a charging device reaches 100 %. Cards rise from the widget, never take focus and wait while a full-screen app is running. Switch them (and the sound) off in right-click -> Alerts.
- New: time left. The tooltip shows an estimate such as "~ 14 h left", learned from the device's own battery history (kept locally for two weeks).
- New: updates. One daily check of the GitHub releases; a notification offers a one-click update (checksum verified). Right-click -> Updates to check now or turn it off.

## 1.0.5
- Fixed: start with Windows. It now registers a Task Scheduler logon task (the installer asks Windows for permission when it is needed) in addition to the Run key, and re-creates it on every launch if it went missing.
- Fixed: your look (style, background, shape, size, position) is always kept: settings are saved atomically with a backup copy, a locked or damaged file falls back to the backup, and a failed read can never overwrite your settings with defaults.
- Fixed: installer messages showed garbled accents in 1.0.4.

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

