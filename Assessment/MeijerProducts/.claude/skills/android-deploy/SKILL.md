---
name: android-deploy
description: Build and deploy the MeijerProducts .NET MAUI app to an attached Android device or emulator. Use when asked to deploy/run/install MeijerProducts on Android, or on a physical/USB Android device specifically.
---

MeijerProducts targets `net10.0-android` alongside its Windows head (see repo `CLAUDE.md`). This
skill builds that target and deploys it via `adb`/MSBuild's Android run target, choosing the right
device when more than one is attached.

All paths below are relative to `Assessment/` (repo-root-relative), matching `CLAUDE.md`'s command
conventions.

## Prerequisites

`adb` is not on `PATH` in this environment and `ANDROID_HOME`/`ANDROID_SDK_ROOT` aren't set either.
The SDK lives at:

```
C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe
```

Verified present in this environment. Use the full path (or set `$adb = "...\adb.exe"` once per
session) rather than assuming `adb` resolves.

## Step 1: enumerate attached devices

```powershell
$adb = "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe"
& $adb devices -l
```

Each line is `<serial> device product:... model:... device:... transport_id:...` (ignore any in
`unauthorized`/`offline` state — those aren't deployable yet, see Gotchas). Classify each serial:

- Matches `^emulator-\d+$` → **emulator**.
- Anything else (e.g. `R5CW419CEPR`) → **physical device**.

## Step 2: pick the target

- **No devices listed** → stop and tell the user: attach/authorize a device or start an emulator.
- **Exactly one device** (physical or emulator) → use it, no need to ask.
- **Two or more devices** → ask the user which one to deploy to via `AskUserQuestion`. List
  physical devices first and mark the first physical device "(Recommended)"; list emulators after.
  Always ask in this case — don't silently pick even when a physical device is obviously preferred.

## Step 3: build and deploy to the chosen serial

Run from `Assessment/`:

```powershell
dotnet build MeijerProducts/MeijerProducts.csproj -t:Run -f net10.0-android -p:AdbTarget="-s <serial>"
```

`AdbTarget` is a real MSBuild property consumed by the Android SDK's build targets (confirmed in
`Xamarin.Android.Common.Debugging.targets` — it's spliced directly into every `adb $(AdbTarget) ...`
invocation for install/run/uninstall). Passing `-p:AdbTarget="-s <serial>"` is the correct way to
target one device when several are attached; don't rely on `$env:ANDROID_SERIAL` alone as the
mechanism to document, even though setting it alongside doesn't hurt.

## Step 4: verify it launched

```powershell
& $adb -s <serial> shell dumpsys activity activities | Select-String -Pattern "topResumedActivity"
& $adb -s <serial> shell dumpsys package com.companyname.meijerproducts | Select-String -Pattern "lastUpdateTime|versionName"
```

`topResumedActivity` should show `com.companyname.meijerproducts/crc642251b06823ae3698.MainActivity`
in the foreground, and `lastUpdateTime` should match the time of this deploy (not a stale install).

## Gotchas

- **An unauthorized USB device does not appear in `adb devices` at all** — it doesn't show up as
  `unauthorized`, it's simply absent from the list. If a physical device the user says is plugged in
  doesn't show, the fix is usually on the phone: accept the "Allow USB debugging?" RSA fingerprint
  prompt. Re-run `adb devices -l` after they confirm, don't assume a driver problem first.
- **Two devices attached with no `-s`/`AdbTarget` selector errors out** ("more than one
  device/emulator") rather than picking one — always resolve to a single serial before running
  Step 3.
- **Build output doesn't print install/deploy progress** — `dotnet build ... -t:Run` for Android
  prints only the compile summary even when it's also installing and launching. Don't take silence
  as failure; confirm via Step 4 instead of trusting console output.
