using IronOcr;
using RSPS_Color_Bot;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;


//Look! Top-level entry point!
//This is where we begin
Execute execute = new Execute();

//Here we start the main loop!
execute.MainLoop();


[StructLayout(LayoutKind.Sequential)]
public struct Rect
{
    public int left;
    public int top;
    public int right;
    public int bottom;
}

[StructLayout(LayoutKind.Sequential)]
struct INPUT
{
    public SendInputEventType type;
    public MouseKeybdhardwareInputUnion mkhi;
}
[StructLayout(LayoutKind.Explicit)]
struct MouseKeybdhardwareInputUnion
{
    [FieldOffset(0)]
    public MouseInputData mi;

    [FieldOffset(0)]
    public KEYBDINPUT ki;

    [FieldOffset(0)]
    public HARDWAREINPUT hi;
}
[StructLayout(LayoutKind.Sequential)]
struct KEYBDINPUT
{
    public ushort wVk;
    public ushort wScan;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
}
[StructLayout(LayoutKind.Sequential)]
struct HARDWAREINPUT
{
    public int uMsg;
    public short wParamL;
    public short wParamH;
}
struct MouseInputData
{
    public int dx;
    public int dy;
    public uint mouseData;
    public MouseEventFlags dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
}
[Flags]
enum MouseEventFlags : uint
{
    MOUSEEVENTF_MOVE = 0x0001,
    MOUSEEVENTF_LEFTDOWN = 0x0002,
    MOUSEEVENTF_LEFTUP = 0x0004,
    MOUSEEVENTF_RIGHTDOWN = 0x0008,
    MOUSEEVENTF_RIGHTUP = 0x0010,
    MOUSEEVENTF_MIDDLEDOWN = 0x0020,
    MOUSEEVENTF_MIDDLEUP = 0x0040,
    MOUSEEVENTF_XDOWN = 0x0080,
    MOUSEEVENTF_XUP = 0x0100,
    MOUSEEVENTF_WHEEL = 0x0800,
    MOUSEEVENTF_VIRTUALDESK = 0x4000,
    MOUSEEVENTF_ABSOLUTE = 0x8000
}
enum SendInputEventType : int
{
    InputMouse,
    InputKeyboard,
    InputHardware
}

public struct RECT
{
    public int Left;       // Specifies the x-coordinate of the upper-left corner of the rectangle.
    public int Top;        // Specifies the y-coordinate of the upper-left corner of the rectangle.
    public int Right;      // Specifies the x-coordinate of the lower-right corner of the rectangle.
    public int Bottom;     // Specifies the y-coordinate of the lower-right corner of the rectangle.

}

[StructLayout(LayoutKind.Sequential)]
public struct WINDOWINFO
{
    public uint cbSize;
    public RECT rcWindow;
    public RECT rcClient;
    public uint dwStyle;
    public uint dwExStyle;
    public uint dwWindowStatus;
    public uint cxWindowBorders;
    public uint cyWindowBorders;
    public ushort atomWindowType;
    public ushort wCreatorVersion;

    public WINDOWINFO(Boolean? filler)
     : this()   // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
    {
        cbSize = (UInt32)(Marshal.SizeOf(typeof(WINDOWINFO)));
    }

}