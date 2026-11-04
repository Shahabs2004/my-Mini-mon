using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SmartViewCompanion.Capture;

/// <summary>
/// Basic desktop capture provider based on GDI BitBlt.
/// Captured frames are copied into a frozen WPF BitmapSource before returning.
/// </summary>
public sealed class GdiBitBltCaptureProvider : IFrameCaptureProvider
{
    private const uint Srccopy = 0x00CC0020;
    private const uint CaptureBlt = 0x40000000;
    private const uint DibRgbColors = 0;
    private const int BiRgb = 0;

    public string Name => "GDI BitBlt";

    public Task<CaptureBackendStatus> CheckAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var screenDc = GetDC(IntPtr.Zero);
        if (screenDc == IntPtr.Zero)
            return Task.FromResult(new CaptureBackendStatus(false, "Windows could not provide a desktop device context."));

        ReleaseDC(IntPtr.Zero, screenDc);
        return Task.FromResult(new CaptureBackendStatus(true, "GDI desktop capture is available."));
    }

    public Task<BitmapSource?> CaptureAsync(Rect desktopRegion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int x = (int)Math.Floor(desktopRegion.X);
        int y = (int)Math.Floor(desktopRegion.Y);
        int width = Math.Max(1, (int)Math.Ceiling(desktopRegion.Width));
        int height = Math.Max(1, (int)Math.Ceiling(desktopRegion.Height));

        IntPtr screenDc = IntPtr.Zero;
        IntPtr memoryDc = IntPtr.Zero;
        IntPtr bitmap = IntPtr.Zero;
        IntPtr oldBitmap = IntPtr.Zero;
        IntPtr bits = IntPtr.Zero;

        try
        {
            screenDc = GetDC(IntPtr.Zero);
            if (screenDc == IntPtr.Zero)
                throw new InvalidOperationException("GetDC failed.");

            memoryDc = CreateCompatibleDC(screenDc);
            if (memoryDc == IntPtr.Zero)
                throw new InvalidOperationException("CreateCompatibleDC failed.");

            var info = new BitmapInfo
            {
                Header = new BitmapInfoHeader
                {
                    Size = (uint)Marshal.SizeOf<BitmapInfoHeader>(),
                    Width = width,
                    Height = -height, // top-down DIB: first row is the top row
                    Planes = 1,
                    BitCount = 32,
                    Compression = BiRgb,
                    SizeImage = (uint)(width * height * 4)
                }
            };

            bitmap = CreateDIBSection(screenDc, ref info, DibRgbColors, out bits, IntPtr.Zero, 0);
            if (bitmap == IntPtr.Zero || bits == IntPtr.Zero)
                throw new InvalidOperationException("CreateDIBSection failed.");

            oldBitmap = SelectObject(memoryDc, bitmap);
            if (oldBitmap == IntPtr.Zero || oldBitmap == new IntPtr(-1))
                throw new InvalidOperationException("SelectObject failed.");

            if (!BitBlt(memoryDc, 0, 0, width, height, screenDc, x, y, Srccopy | CaptureBlt))
                throw new InvalidOperationException("BitBlt failed.");

            cancellationToken.ThrowIfCancellationRequested();

            int stride = checked(width * 4);
            byte[] pixels = new byte[checked(stride * height)];
            Marshal.Copy(bits, pixels, 0, pixels.Length);

            BitmapSource frame = BitmapSource.Create(
                width, height, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
            frame.Freeze();
            return Task.FromResult<BitmapSource?>(frame);
        }
        finally
        {
            if (oldBitmap != IntPtr.Zero && oldBitmap != new IntPtr(-1) && memoryDc != IntPtr.Zero)
                SelectObject(memoryDc, oldBitmap);
            if (bitmap != IntPtr.Zero)
                DeleteObject(bitmap);
            if (memoryDc != IntPtr.Zero)
                DeleteDC(memoryDc);
            if (screenDc != IntPtr.Zero)
                ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public static Rect GetPrimaryScreenBounds()
    {
        int width = GetSystemMetrics(0);  // SM_CXSCREEN
        int height = GetSystemMetrics(1); // SM_CYSCREEN
        return new Rect(0, 0, Math.Max(1, width), Math.Max(1, height));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width;
        public int Height;
        public ushort Planes;
        public ushort BitCount;
        public uint Compression;
        public uint SizeImage;
        public int XPelsPerMeter;
        public int YPelsPerMeter;
        public uint ClrUsed;
        public uint ClrImportant;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfo
    {
        public BitmapInfoHeader Header;
        public uint Colors;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetDC(IntPtr window);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int ReleaseDC(IntPtr window, IntPtr dc);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateCompatibleDC(IntPtr dc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteDC(IntPtr dc);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr CreateDIBSection(
        IntPtr dc, ref BitmapInfo bitmapInfo, uint usage, out IntPtr bits, IntPtr section, uint offset);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern IntPtr SelectObject(IntPtr dc, IntPtr item);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr item);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool BitBlt(
        IntPtr destinationDc, int x, int y, int width, int height,
        IntPtr sourceDc, int sourceX, int sourceY, uint rasterOperation);
}
