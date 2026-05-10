# Jet Screenshot

Windows screenshot tool built as a ShareX fork with a CleanShot-style floating capture overlay. The real product code lives in [`upstream/ShareX`](./upstream/ShareX); the repo root is only thin wrapper tooling for building, branding, and packaging that native fork.

## What Changed From Upstream

- Added a floating post-capture overlay stack for screenshot tasks.
- Kept ShareX's native capture, editor, hotkey, tray, and settings flows intact.
- Rebranded the app assets around the custom logo in [`assets/app-logo.png`](./assets/app-logo.png).
- Defaulted the app to tray-first behavior:
  - starts with Windows
  - launches silently
  - stays in the system tray instead of opening the full main window on login

## Runtime Behavior

The overlay flow is driven from [`TaskManager`](./upstream/ShareX/ShareX/TaskManager.cs). When a completed task is an image capture with a resolved saved file path, the fork opens [`CaptureOverlayForm`](./upstream/ShareX/ShareX/Forms/CaptureOverlayForm.cs) instead of the standard completion path, and keeps the image in memory for thumbnail and drag behavior.

Layout and timing are isolated in:

- [`CaptureOverlayStackLayout`](./upstream/ShareX/ShareX/Forms/CaptureOverlayStackLayout.cs)
- [`CaptureOverlayDismissPolicy`](./upstream/ShareX/ShareX/Forms/CaptureOverlayDismissPolicy.cs)
- [`TaskThumbnailPanel`](./upstream/ShareX/ShareX/Controls/TaskThumbnailPanel.cs)

Startup and tray behavior are enforced in:

- [`Program.cs`](./upstream/ShareX/ShareX/Program.cs)
- [`ApplicationConfig.cs`](./upstream/ShareX/ShareX/ApplicationConfig.cs)
- [`StartupManager.cs`](./upstream/ShareX/ShareX/StartupManager.cs)

## Build And Run

Requirements:

- Windows
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- PowerShell

From repo root:

```powershell
npm run build
npm run dev
npm run release
```

Script behavior:

- `npm run build`: builds the native ShareX fork
- `npm run dev`: rebuilds and launches the native app
- `npm run release`: creates a runnable copy under [`release`](./release)

Primary outputs:

- build output: `upstream/ShareX/ShareX/bin/Release/win-x64/ShareX.exe`
- packaged local release: `release/ShareX.exe`

Wrapper scripts:

- [`scripts/run-sharex-overlay.ps1`](./scripts/run-sharex-overlay.ps1)
- [`scripts/deploy-sharex.ps1`](./scripts/deploy-sharex.ps1)

## Notes

- This repo no longer uses the older Electron prototype.
- Startup registration creates a Windows Startup shortcut that launches the app with `-silent`.
- The ShareX test project still contains some pre-existing failures unrelated to the startup, tray, and branding work in this repo state.

## License

ShareX-derived code remains GPLv3. See [`upstream/ShareX/LICENSE.txt`](./upstream/ShareX/LICENSE.txt).

Upstream references:

- [getsharex.com](https://getsharex.com)
- [github.com/ShareX/ShareX](https://github.com/ShareX/ShareX)
