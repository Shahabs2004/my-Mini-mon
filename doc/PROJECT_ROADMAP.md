# SmartView Companion — Project Roadmap & Progress

> **Purpose:** This is the project's central, maintained checklist. Keep it synchronized with the actual source code and verified tests. Mark an item `[x]` only after its implementation exists in the repository and its acceptance criteria have been checked. A committed change is not automatically a tested or verified feature.
>
> **Current milestone:** Prototype 0.6 — software preview simulator (preview lifecycle hardening in progress)
> **Last roadmap review:** 2026-11-05
> **Repository:** https://github.com/Shahabs2004/my-Mini-mon

## 1. Project vision

Build a Windows application and, later, a compact USB-powered auxiliary display mounted above or near a keyboard. The device must behave as a **custom USB peripheral**, not as a Windows secondary monitor.

The PC application captures a selected part of the desktop, follows useful screen locations, transforms the captured image to the target panel's format, and eventually streams frames through a documented USB protocol to dedicated firmware.

The software simulator comes first. Hardware purchase, PCB design, and firmware integration should wait until capture, tracking, rendering, and the transport abstraction are stable.

## 2. Status legend

- [x] Implemented in source and reviewed against the listed acceptance criteria.
- [ ] Not implemented, incomplete, or not yet verified.
- **Implemented — unverified:** code exists but still needs a successful build/runtime test. Do not mark complete until verified.
- **Deferred:** intentionally postponed; it remains visible in this plan.

## 3. Current verified status

### 3.1 Repository and application foundation
- [x] Public GitHub repository and `main` branch established.
- [x] .NET 8 WPF solution and application project created.
- [x] Main application window and basic configuration controls.
- [x] GitHub Actions Windows build workflow file added.
- [ ] Confirm the latest GitHub Actions run is green.
- [ ] Establish a repeatable local build and test procedure on Windows 10/11.
- [ ] Add automated unit tests for geometry, crop calculations, tracking selection, and protocol framing.

### 3.2 Desktop capture and preview
- [x] Capture-provider abstraction introduced.
- [x] GDI BitBlt capture provider implemented.
- [x] Live preview loop with cancellation and target-FPS setting.
- [x] Output resolution presets (320×240, 480×320, 480×800).
- [x] Portrait output option.
- [x] Zoom control.
- [x] Crop fitted to output aspect ratio.
- [x] Best-effort request to exclude the app window from capture using `WDA_EXCLUDEFROMCAPTURE`.
- [ ] Verify capture exclusion on supported Windows versions; current behavior is best-effort only.
- [ ] Windows Graphics Capture provider.
- [ ] DXGI Desktop Duplication provider.
- [ ] Backend capability checks and reliable Automatic backend selection/fallback.
- [ ] Protected-content and unsupported-surface detection/reporting.
- [ ] Capture latency, dropped-frame, and frame-time telemetry.
- [ ] Configurable image scaling/filter quality.
- [ ] Optional cursor inclusion/exclusion in captured frames.

### 3.3 Region selection and tracking
- [x] Drag-to-select capture rectangle on the primary display.
- [x] Fixed-region tracking.
- [x] Mouse-pointer tracking on the primary display.
- [x] Basic Win32 text-caret tracking using foreground-thread `GetGUIThreadInfo`.
- [x] Basic Automatic tracking: try Win32 caret first, fall back to mouse.
- [ ] UI Automation caret-location fallback for applications that do not expose a standard Win32 caret.
- [ ] Track caret rectangle dimensions and use caret height/line bounds where reliable.
- [ ] Configurable tracking anchor (caret top, caret center, mouse pointer, custom offset).
- [ ] Smooth tracking / configurable follow delay to avoid jitter.
- [ ] Configurable edge behavior and crop clamping.
- [ ] Multi-monitor enumeration and monitor selection.
- [ ] Region selection across all monitors, including negative desktop coordinates.
- [ ] Mouse and caret tracking across monitors.
- [ ] DPI-awareness validation and coordinate normalization for mixed-DPI displays.
- [ ] Optional window-following mode.
- [ ] Optional application/window allowlist and ignore list.
- [ ] Keyboard shortcuts to start/stop capture and switch tracking modes.

### 3.4 User experience and settings
- [x] Capture API selector (Automatic, WGC, DXGI, GDI BitBlt).
- [x] Tracking mode selector (Fixed Region, Mouse, Caret, Automatic).
- [x] Resolution, FPS, zoom, and portrait controls.
- [x] Status text for unsupported capture/tracking modes.
- [ ] Validate every setting and show actionable error messages.
- [ ] Persist settings between application launches.
- [ ] Named profiles (e.g. Coding, Chat, Monitoring, Video).
- [ ] Import/export settings profiles.
- [ ] Per-profile monitor, region, output size, FPS, zoom, and tracking mode.
- [ ] Minimize-to-tray and optional start-with-Windows behavior.
- [ ] Compact/always-on-top preview mode.
- [ ] Accessibility, keyboard navigation, and high-DPI UI review.
- [ ] Localization structure (English and Persian) and RTL review.

### 3.5 Rendering and performance
- [x] Convert captured GDI frames into WPF bitmap frames.
- [x] Resize output frames to configured dimensions.
- [x] Freeze generated bitmap objects before cross-thread UI presentation.
- [ ] Avoid unnecessary per-frame allocations and bitmap copies.
- [ ] Measure CPU, memory, and GPU utilization under each capture backend.
- [ ] Add adaptive frame pacing and back-pressure when rendering falls behind.
- [ ] Add configurable frame format and color handling.
- [ ] Add optional rotation, mirror, brightness, contrast, and gamma controls.
- [ ] Add diagnostics view with actual FPS, capture time, render time, and dropped frames.
- [ ] Stress test long-running capture sessions and repeated start/stop cycles.

### 3.6 Software device simulator
- [ ] Define a display-device abstraction independent of WPF preview.
- [ ] Add a virtual device endpoint that receives the same frame packets planned for USB.
- [ ] Simulate connection, disconnection, device capabilities, and errors.
- [ ] Simulate target display dimensions, refresh rate, pixel format, and orientation.
- [ ] Add a packet/frame inspector and traffic counters.
- [ ] Add deterministic test mode with generated test patterns.
- [ ] Verify that the simulator and future USB transport consume the same interface.

### 3.7 USB transport and hardware integration
- [ ] Select a suitable USB device class and transport (evaluate vendor-specific bulk, HID, or another appropriate class).
- [ ] Document bandwidth, latency, power, and driver trade-offs before selecting the protocol.
- [ ] Define versioned packet framing, sequence numbers, frame boundaries, checksums, and error handling.
- [ ] Define device discovery, handshake, capability negotiation, and firmware version reporting.
- [ ] Implement transport abstraction in the PC application.
- [ ] Implement a loopback/mock transport for automated tests.
- [ ] Implement real USB transport only after protocol and hardware constraints are known.
- [ ] Design firmware architecture and frame-buffer strategy.
- [ ] Prototype display controller and panel interface.
- [ ] Verify USB power budget and cable/connector requirements.
- [ ] Measure real end-to-end latency, achievable FPS, and image quality.
- [ ] Add firmware update and recovery strategy.

### 3.8 Documentation, quality, and release
- [x] Initial README with project purpose, setup, limitations, and roadmap summary.
- [x] GitHub Actions build workflow definition.
- [ ] Keep this roadmap and README synchronized.
- [ ] Add architecture document and component/data-flow diagram.
- [ ] Add developer setup and troubleshooting guide.
- [ ] Add release packaging and versioning.
- [ ] Add changelog and release notes.
- [ ] Add crash logging with privacy-conscious diagnostics.
- [ ] Add license and third-party notices.
- [ ] Define supported Windows versions and minimum hardware requirements.
- [ ] Publish a tested prototype release.

## 4. Proposed architecture

Keep these concerns independent so hardware changes do not force a rewrite of capture and tracking:

1. **Capture** — obtains desktop pixels from a selected backend.
2. **Tracking** — determines the screen-space anchor or region to follow.
3. **Composition** — crops, scales, rotates, and converts frames to the device format.
4. **Output** — sends frames to WPF preview, simulator, or a future USB device.
5. **Configuration** — stores profiles and device/capture preferences.
6. **Diagnostics** — reports capabilities, errors, timing, and dropped frames.

Target flow:

`Desktop → Capture Provider → Tracking/Crop → Frame Composer → Output Sink → Preview / Simulator / USB Device`

Do not couple tracking logic to a particular capture API. Do not couple the preview window to the future USB protocol.

## 5. Milestones and exit criteria

### M0 — Software shell and first preview
**Status: Implemented in source; runtime/build verification pending.**
- [x] WPF application and solution.
- [x] GDI capture and preview loop.
- [x] Fixed-region and basic output controls.
- Exit criteria: successful Windows build and stable preview session. [ ]

### M1 — Tracking prototype
**Status: Implemented in source; runtime/build verification pending.**
- [x] Fixed region.
- [x] Mouse pointer.
- [x] Basic Win32 caret.
- [x] Caret-first Automatic with mouse fallback.
- [ ] Test in Notepad, Microsoft Word, a browser text field, and a code editor.
- [ ] Document unsupported applications and behavior.
- Exit criteria: tracking behavior validated in the target app matrix. [ ]

### M2 — Robust capture and multiple monitors
**Status: Not started.**
- [ ] WGC backend.
- [ ] DXGI backend.
- [ ] Multi-monitor and mixed-DPI support.
- [ ] Backend fallback and capability reporting.
- Exit criteria: stable capture on multiple Windows configurations. [ ]

### M3 — Polished desktop simulator
**Status: Not started.**
- [ ] Saved profiles and diagnostics.
- [ ] Frame composer and output sink abstraction.
- [ ] Virtual device simulator and test patterns.
- Exit criteria: preview and simulator use the same output pipeline. [ ]

### M4 — USB protocol and hardware prototype
**Status: Not started; requires hardware selection.**
- [ ] Protocol specification and mock transport.
- [ ] Hardware/panel/controller selection.
- [ ] Firmware and real USB transport.
- Exit criteria: stable real-device streaming with measured latency and power. [ ]

### M5 — Release quality
**Status: Not started.**
- [ ] Automated tests and green CI.
- [ ] Installer/portable packaging, documentation, license, changelog.
- Exit criteria: reproducible release build and tested user guide. [ ]

## 6. Immediate next tasks (execution order)

1. **Build correctness first:** inspect the current source and fix compile/runtime lifecycle issues; obtain a successful Windows CI build.
2. **Add automated tests:** isolate crop geometry and tracking-coordinate logic from WPF UI.
3. **Improve preview lifecycle:** await cancellation/cleanup, prevent overlapping preview loops, and report errors without stale UI updates.
4. **Implement multi-monitor support:** enumerate monitors, use virtual desktop coordinates, and make region selection monitor-aware.
5. **Implement WGC capture:** add a real provider with capability detection; preserve GDI as a fallback.
6. **Implement UI Automation caret fallback:** test carefully against supported applications and keep Win32 caret as the first choice.
7. **Add frame telemetry and saved profiles.**
8. **Create simulator/output abstraction, then USB protocol mock.**

## 7. Development rules

- Update this file in the same change that materially changes project capability.
- Never mark a feature complete solely because code was committed.
- Record whether a feature is implemented, built, runtime-tested, or hardware-tested.
- Keep known limitations visible.
- Prefer small, reviewable commits with one coherent purpose.
- Do not start PCB/hardware work until the software simulator and transport contract are sufficiently stable.
- Preserve the core requirement: the final device is a USB peripheral, not a Windows secondary monitor.

## 8. Recent implementation notes

### 2026-11-05 — Preview lifecycle hardening (source committed; verification pending)
- Capture shutdown now cancels and awaits the active preview task before disposing the provider.
- Start/stop and region-selection flows await the previous preview session, preventing overlapping capture loops.
- Window closing now waits for preview cleanup before closing.
- Capture-backend initialization exceptions are reported in the status area and the Start control is restored.
- These changes are **not yet marked complete**: Windows build and repeated start/stop runtime tests are still required.

## 9. Progress snapshot

Progress is tracked by checked checklist items, not by an optimistic percentage. A percentage should be calculated only after defining weighted milestones; until then, the milestone status and checklists above are the authoritative progress report.
