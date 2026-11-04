using System.Runtime.InteropServices;
using System.Windows;

namespace SmartViewCompanion.Core;

public sealed record DisplayMonitorInfo(string DeviceName, Rect Bounds, bool IsPrimary)
{
    public string DisplayName => $"{DeviceName} — {Bounds.Width:0} × {Bounds.Height:0}{(IsPrimary ? " (Primary)" : "")}";
    public override string ToString() => DisplayName;
}

/// <summary>Enumerates physical desktop monitors using Win32 monitor coordinates.</summary>
public static class DisplayMonitorService
{
    private const uint MonitorInfoPrimary = 1;

    public static IReadOnlyList<DisplayMonitorInfo> GetMonitors()
    {
        var monitors = new List<DisplayMonitorInfo>();
        MonitorEnumProc callback = (handle, _, _, _) =>
        {
            var info = new MonitorInfoEx { Size = Marshal.SizeOf<MonitorInfoEx>(), DeviceName = string.Empty };
            if (GetMonitorInfo(handle, ref info))
            {
                var bounds = new Rect(
                    info.Monitor.Left,
                    info.Monitor.Top,
                    info.Monitor.Right - info.Monitor.Left,
                    info.Monitor.Bottom - info.Monitor.Top);

                monitors.Add(new DisplayMonitorInfo(
                    info.DeviceName ?? "Display",
                    bounds,
                    (info.Flags & MonitorInfoPrimary) != 0));
            }

            return true;
        };

        if (!EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero))
            throw new InvalidOperationException("Windows could not enumerate desktop monitors.");

        return monitors
            .OrderByDescending(m => m.IsPrimary)
            .ThenBy(m => m.DeviceName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string? DeviceName;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EnumDisplayMonitors(
        IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfoEx info);
}
