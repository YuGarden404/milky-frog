using System.Runtime.InteropServices;
using MilkyFrog.Core.Abstractions;
using MilkyFrog.Core.Models;

namespace MilkyFrog.Platform.Windows.Cursor;

public sealed class WindowsCursorPositionProvider
    : ICursorPositionProvider
{
    public bool TryGetPosition(out Point2D position)
    {
        if (!GetCursorPos(out NativePoint nativePoint))
        {
            position = default;
            return false;
        }

        position = new Point2D(
            nativePoint.X,
            nativePoint.Y);

        return true;
    }

    [DllImport(
        "user32.dll",
        SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(
        out NativePoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }
}