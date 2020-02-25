using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace COMPort {
    [Flags]
    public enum MouseEventFlags {
        Move = 0x0001,              // mouse move
        LeftDown = 0x0002,          // left button down
        LeftUp = 0x0004,            // left button up
        RightDown = 0x0008,         // right button down
        RightUp = 0x0010,           // right button up
        MiddleDown = 0x0020,        // middle button down
        MiddleUp = 0x0040,          // middle button up
        XDown = 0x0080,             // x button down
        XUp = 0x0100,               // x button down
        Wheel = 0x0800,             // wheel button rolled
        HWheel = 0x01000,           // hwheel button rolled
        MoveNoCoalesce = 0x2000,   // do not coalesce mouse moves
        VirtualDesk = 0x4000,       // map to entire virtual desktop
        Absolute = 0x8000,          // absolute move
    }

    public struct POINT {
        public int X { get; set; }

        public int Y { get; set; }
    }

    public static class GlobalMouse {
        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out POINT lpMousePoint);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        public static Point Position {
            get {
                POINT pos;
                GetCursorPos(out pos);
                return new Point(pos.X, pos.Y);
            }
            set {
                SetCursorPos((int)value.X, (int)value.Y);
            }
        }

        public static void MouseAction(MouseEventFlags mouseEvent) {
            mouse_event((int)mouseEvent, (int)Position.X, (int)Position.Y, 0, 0);
        }

        public static void MouseWheel(int delta) {
            mouse_event((int)MouseEventFlags.Wheel, (int)Position.X, (int)Position.Y, delta * SystemParameters.WheelScrollLines * 40, 0);
        }
    }
}
