# SmartView Companion

A Windows companion application for a future USB-powered auxiliary display. The first milestone is a software simulator: capture a region of the desktop, track the mouse or text caret, transform the image, and show it in a virtual display window.

## Project status

**Prototype 0.4 / early development.** The app now has a GDI BitBlt capture provider, a live preview, a drag-to-select fixed capture rectangle, and live mouse-pointer tracking. Fixed-region crops use the selected rectangle; mouse tracking follows the pointer on the primary display. Automatic currently uses GDI BitBlt because it is the only implemented backend. Windows Graphics Capture, DXGI, and caret tracking are not implemented yet.

## Goals

- The future hardware is a USB peripheral, **not** a Windows secondary monitor.
- Capture API and tracking mode are independent settings.
- Capture APIs planned: Windows Graphics Capture, DXGI Desktop Duplication, and GDI BitBlt, plus Automatic selection.
- Tracking modes planned: Fixed Region, Mouse, Caret, and Automatic.
- Preview output supports crop, zoom, rotation, output dimensions, and target frame rate.

## Requirements

- Windows 10/11
- .NET 8 SDK
- Visual Studio 2022 with the .NET desktop development workload, or `dotnet` CLI

## Build

```powershell
dotnet build .\SmartViewCompanion.sln
```

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
- [ ] Windows Graphics Capture provider
- [ ] DXGI Desktop Duplication provider
- [ ] Caret tracking (Win32 first, UI Automation fallback where feasible)
- [x] Basic crop, zoom, aspect-ratio fit, and output resize
- [ ] Performance telemetry and saved profiles
- [ ] USB device protocol simulator

## License

TBD. No license has been selected yet.
