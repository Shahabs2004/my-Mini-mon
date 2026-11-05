## Continuous integration

GitHub Actions runs on every push to `main`, pull request targeting `main`, or manual dispatch. The Windows runner restores the solution, builds the full solution in Release configuration, and then runs the automated tests. On success, the compiled application output is uploaded as the `SmartViewCompanion-windows-net8` artifact; test results are uploaded separately when available. Open the repository's **Actions** tab to inspect compiler errors and download artifacts.

# SmartView Companion

A Windows companion application for a future USB-powered auxiliary display. The current milestone is a software simulator: capture a region of the desktop, track the mouse or a supported text caret, transform the image, and show it in a preview window.

## Project status

See [doc/PROJECT_ROADMAP.md](doc/PROJECT_ROADMAP.md) for the full feature checklist, milestone status, known limitations, and development order.


**Prototype 0.9 / early development.** The app includes a GDI BitBlt capture provider, live preview, drag-to-select fixed capture rectangle, mouse-pointer tracking, basic Win32 text-caret tracking, and a simple automatic mode (caret-first with mouse fallback). Caret tracking uses the foreground GUI thread's `GetGUIThreadInfo` caret rectangle and converts it to desktop coordinates. It works only when the target application exposes a standard Win32 caret; some applications (including some custom-rendered editors, browsers, and elevated apps) may not expose usable caret information. When no caret is found, the preview remains at the last known position (or the primary-screen center before the first successful detection).

Automatic capture currently selects GDI BitBlt because it is the only implemented backend. A Win32 monitor selector now offers individual physical monitors and a combined Virtual desktop (all monitors) source. Selecting the virtual desktop allows the tracking crop to follow a mouse pointer or supported Win32 caret across monitor coordinates. The captured rectangle is the bounding rectangle of all displays, so gaps between monitors may appear blank. Windows Graphics Capture, DXGI Desktop Duplication, mixed-DPI normalization/validation, and UI Automation caret fallback are not implemented yet.

## Idle Photo Frame

The prototype includes a separate Photo Frame window for an idle-time slideshow. Add multiple image files or one or more folders (searched recursively), choose the inactivity delay and slide duration, and select Fit or Fill scaling. The slideshow pauses as soon as system keyboard/mouse activity resumes. The source list and preferences are stored under `%LocalAppData%\\SmartViewCompanion\\photo-frame.json`.

This first version is a standalone simulator window; it is not yet routed through the same frame-composition/output pipeline as desktop capture, and it does not send images to physical USB hardware.

## Goals

- The future hardware is a USB peripheral, **not** a Windows secondary monitor.
- Capture API and tracking mode are independent settings.
- Capture APIs planned: Windows Graphics Capture, DXGI Desktop Duplication, and GDI BitBlt, plus Automatic selection.
- Tracking modes: Fixed Region, Mouse, Caret, and Automatic (caret-first, mouse fallback).
- Preview output supports crop, zoom, aspect-ratio fit, output dimensions, and target frame rate.

## Requirements

- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 with the .NET desktop development workload, or `dotnet` CLI

## Build

```powershell
dotnet test .\SmartViewCompanion.sln --configuration Release
```

The solution now includes an initial xUnit test project for desktop geometry and crop calculations. The Windows GitHub Actions workflow runs `dotnet test`; check the Actions tab for the result of the latest run.

## Current limitations

- The primary monitor is selected by default; users can select another physical monitor or the combined Virtual desktop source.
- Virtual-desktop capture uses the bounding rectangle of all monitors; gaps in the desktop layout may be included as blank pixels.
- Mixed-DPI positioning still needs normalization and Windows validation.
- Caret tracking is based on the standard Win32 GUI caret. It is not a universal text-caret detector and does not yet use UI Automation.
- GDI BitBlt may fail to capture protected content, hardware overlays, or some accelerated surfaces.
- `WDA_EXCLUDEFROMCAPTURE` is requested for the app window as a best-effort measure; actual exclusion must be verified on the target Windows version and capture path.
- The preview is a software simulator. No USB device protocol, firmware, or physical display output exists yet.

## Architecture principles

1. `CaptureMethod` selects *how pixels are acquired*.
2. `TrackingMode` selects *which screen location is followed*.
3. `CaptureSource` selects *what desktop surface is captured* (monitor/window/region).
4. A capability check must run before a backend is activated. Automatic mode may fall back only to a backend that passed its capability check.
5. Unsupported caret APIs and protected content must be reported honestly; no silent false-positive tracking.

## Roadmap

- [x] Initial repository and WPF application shell
- [x] Working GDI BitBlt capture provider and live fixed-center preview
- [x] Fixed-region selection overlay (primary display)
- [x] Mouse tracking (primary display)
- [x] Basic Win32 caret tracking (foreground thread; app-dependent)
- [x] Simple automatic tracking (caret-first, mouse fallback)
- [ ] UI Automation caret fallback where feasible
- [ ] Windows Graphics Capture provider
- [ ] DXGI Desktop Duplication provider
- [x] Per-monitor enumeration and selection (source implemented; Windows validation pending)
- [x] Virtual-desktop bounding rectangle source for cross-monitor tracking (source implemented; Windows validation pending)
- [ ] Mixed-DPI normalization and validation
- [ ] Performance telemetry and saved profiles
- [ ] USB device protocol simulator
- [ ] Hardware/firmware integration and physical-device testing

## License

TBD. No license has been selected yet.
