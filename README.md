# CleanShotX

Fork of ShareX that shows a floating thumbnail stack after you capture region/window/full screen—similar UX to CleanShot's preview strip, implemented as WinForms in the main ShareX process. Everything else (hotkeys, editor, destinations, settings) behaves like upstream.

The patched code lives in [`upstream/ShareX`](./upstream/ShareX). This repo root is thin glue: npm scripts kick off `dotnet build` and launch ShareX.exe. The old Electron/React stuff under [`src`](./src) is dead weight unless you resurrect it yourself.

## Behavior (read this if something feels wrong)

Completion flow in [`TaskManager`](./upstream/ShareX/ShareX/TaskManager.cs) checks `CanShowCaptureOverlay`: image **Job**, file saved, path resolves to something ShareX considers an image. When that fires, **`CaptureOverlayForm`** opens and **`task.KeepImage` stays true** so the thumbnail has bytes in memory—you need that because the toast path would otherwise reclaim the bitmap.

Overlay and the classic toast are **alternative branches**. If you're eligible for the overlay, you don't get that completion toast block for that task; tweak the predicate in `CanShowCaptureOverlay` if you want both.

Stack details are all constants in [`CaptureOverlayStackLayout`](./upstream/ShareX/ShareX/Forms/CaptureOverlayStackLayout.cs) (`MaxCards` = 4, card pixel sizes, offset). Idle timeout lives in [`CaptureOverlayDismissPolicy.DismissDelayMilliseconds`](./upstream/ShareX/ShareX/Forms/CaptureOverlayDismissPolicy.cs) (20 seconds). Hovering pauses dismissal; dragging out uses `ThumbnailDragCompleted` on [`TaskThumbnailPanel`](./upstream/ShareX/ShareX/Controls/TaskThumbnailPanel.cs).

Unit tests covering layout math and dismiss timing are in [`ShareX.Tests`](./upstream/ShareX/ShareX.Tests/CaptureOverlayStackLayoutTests.cs).

## Build

Needs Windows and the [.NET 9 SDK](https://dotnet.microsoft.com/download). Project target is wired in [`ShareX.csproj`](./upstream/ShareX/ShareX/ShareX.csproj) (`net9.0-windows`; min OS aligns with upstream).

From repo root:

```powershell
npm run dev              # dotnet Release win-x64 + start ShareX.exe
npm run build            # build only

dotnet test upstream/ShareX/ShareX.Tests/ShareX.Tests.csproj -c Release
```

Output exe: `upstream/ShareX/ShareX/bin/Release/win-x64/ShareX.exe`. If `dotnet` isn't on PATH, fix that first—the npm scripts call it directly via [`scripts/run-sharex-overlay.ps1`](./scripts/run-sharex-overlay.ps1).

## License

ShareX-derived code stays **GPLv3** — [`upstream/ShareX/LICENSE.txt`](./upstream/ShareX/LICENSE.txt). Upstream docs and binaries: [getsharex.com](https://getsharex.com) · [github.com/ShareX/ShareX](https://github.com/ShareX/ShareX).

`package.json` at the repo root is tooling metadata only; distributing a build you made from `upstream/ShareX` is still GPLv3 territory.
