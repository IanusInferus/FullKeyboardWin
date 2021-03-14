using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using Microsoft.Windows.Sdk;
using WindowsInput.Native;

namespace FullKeyboardWin
{
    public static class PInvokeWrapper
    {
        public static void SetWindowNoFocus(IntPtr hWnd)
        {
            const int GWL_EXSTYLE = -20;
            const int WS_EX_NOACTIVATE = 0x08000000;

            var v = GetWindowLongPtr(hWnd, GWL_EXSTYLE).ToInt64();
            SetWindowLongPtr(hWnd, GWL_EXSTYLE, new IntPtr(v | WS_EX_NOACTIVATE));
        }
        public static void SetDragMove(IntPtr hWnd)
        {
            const uint WM_SYSCOMMAND = 0x0112;
            const uint WM_LBUTTONUP = 0x0202;
            PInvoke.SendMessage((HWND)hWnd, WM_SYSCOMMAND, (WPARAM)(UIntPtr)0xf012, (LPARAM)(IntPtr)0);
            PInvoke.SendMessage((HWND)hWnd, WM_LBUTTONUP, (WPARAM)(UIntPtr)0, (LPARAM)(IntPtr)0);
        }

        [Flags]
        public enum ModifierCode : uint
        {
            MOD_ALT = 0x0001,
            MOD_CONTROL = 0x0002,
            MOD_NOREPEAT = 0x4000,
            MOD_SHIFT = 0x0004,
            MOD_WIN = 0x0008
        }
        public static bool TryRegisterHotKey(HwndSource hwndSource, int HOTKEY_ID, ModifierCode modifiers, VirtualKeyCode key, Action OnHotKeyPressed)
        {
            IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
            {
                const int WM_HOTKEY = 0x0312;
                switch (msg)
                {
                    case WM_HOTKEY:
                        if (wParam.ToInt32() == HOTKEY_ID)
                        {
                            OnHotKeyPressed();
                            handled = true;
                        }
                        break;
                }
                return IntPtr.Zero;
            };
            hwndSource.AddHook(HwndHook);

            if (!PInvoke.RegisterHotKey((HWND)hwndSource.Handle, HOTKEY_ID, (uint)modifiers, (uint)key))
            {
                return false;
            }
            return true;
        }

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 4)
            {
                return GetWindowLong32(hWnd, nIndex);
            }
            return GetWindowLongPtr64(hWnd, nIndex);
        }

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", CharSet = CharSet.Auto)]
        private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Auto)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        // This static method is required because legacy OSes do not support
        // SetWindowLongPtr
        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);
    }
}
