// Copyright (c) Amer Koleci and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Vortice.Mathematics;
using VorticeImGuiDx12.Interoperation;
using static VorticeImGuiDx12.Interoperation.User32;

namespace VorticeImGuiDx12
{
    public class Win32Window
    {
        public string Title;
        public int Width;
        public int Height;
        public IntPtr Handle;
        public bool IsMinimized;

        public Win32Window(string wndClass, string title, int width, int height)
        {
            Title = title;
            Width = width;
            Height = height;

            var screenWidth = GetSystemMetrics(SystemMetrics.SM_CXSCREEN);
            var screenHeight = GetSystemMetrics(SystemMetrics.SM_CYSCREEN);
            var x = (screenWidth - Width) / 2;
            var y = (screenHeight - Height) / 2;

            var style = WindowStyles.WS_OVERLAPPEDWINDOW;
            var styleEx = WindowExStyles.WS_EX_APPWINDOW | WindowExStyles.WS_EX_WINDOWEDGE;

            var windowRect = new Vortice.Mathematics.Rectangle(0, 0, Width, Height);
            AdjustWindowRectEx(ref windowRect, style, false, styleEx);

            var windowWidth = windowRect.Right - windowRect.Left;
            var windowHeight = windowRect.Bottom - windowRect.Top;

            var hwnd = CreateWindowEx(
                (int)styleEx, wndClass, Title, (int)style,
                x, y, windowWidth, windowHeight,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            Handle = hwnd;
        }

        public virtual bool ProcessMessage(uint msg, UIntPtr wParam, IntPtr lParam)
        {
            switch ((WindowMessage)msg)
            {
                case WindowMessage.Size:
                    switch ((SizeMessage)wParam)
                    {
                        case SizeMessage.SIZE_RESTORED:
                        case SizeMessage.SIZE_MAXIMIZED:
                            IsMinimized = false;

                            var lp = (int)lParam;
                            Width = Utils.Loword(lp);
                            Height = Utils.Hiword(lp);
                            break;
                        case SizeMessage.SIZE_MINIMIZED:
                            IsMinimized = true;
                            break;
                        default:
                            break;
                    }
                    break;
            }
            return false;
        }

        public void Destroy()
        {
            if (Handle != IntPtr.Zero)
            {
                DestroyWindow(Handle);
                Handle = IntPtr.Zero;
            }
        }
    }
}