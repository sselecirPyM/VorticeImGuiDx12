using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.Mathematics;
using VorticeImGuiDx12.Interoperation;

namespace VorticeImGuiDx12
{
    public class Window
    {
        private const int CW_USEDEFAULT = unchecked((int)0x80000000);

        public string Title { get; private set; }
        public int Width;
        public int Height;
        public IntPtr Handle { get; private set; }
        public bool IsMinimized;

        public ResourcesManage.CommonContext context;

        public Window(string title, int width, int height, ResourcesManage.CommonContext context)
        {
            Title = title;
            Width = width;
            Height = height;
            this.context = context;
            CreateWindowInternal(WNDPROC);
        }

        private void CreateWindowInternal(WNDPROC proc)
        {
            var x = 0;
            var y = 0;
            WindowStyles style = 0;
            WindowExStyles styleEx = 0;
            const bool resizable = true;

            // Setup the screen settings depending on whether it is running in full screen or in windowed mode.
            //if (fullscreen)
            //{
            //style = User32.WindowStyles.WS_POPUP | User32.WindowStyles.WS_VISIBLE;
            //styleEx = User32.WindowStyles.WS_EX_APPWINDOW;

            //width = screenWidth;
            //height = screenHeight;
            //}
            //else
            {
                if (Width > 0 && Height > 0)
                {
                    var screenWidth = User32.GetSystemMetrics(SystemMetrics.SM_CXSCREEN);
                    var screenHeight = User32.GetSystemMetrics(SystemMetrics.SM_CYSCREEN);

                    // Place the window in the middle of the screen.WS_EX_APPWINDOW
                    x = (screenWidth - Width) / 2;
                    y = (screenHeight - Height) / 2;
                }

                if (resizable)
                {
                    style = WindowStyles.WS_OVERLAPPEDWINDOW;
                }
                else
                {
                    style = WindowStyles.WS_POPUP | WindowStyles.WS_BORDER | WindowStyles.WS_CAPTION | WindowStyles.WS_SYSMENU;
                }

                styleEx = WindowExStyles.WS_EX_APPWINDOW | WindowExStyles.WS_EX_WINDOWEDGE;
            }
            style |= WindowStyles.WS_CLIPCHILDREN | WindowStyles.WS_CLIPSIBLINGS;

            int windowWidth;
            int windowHeight;

            if (Width > 0 && Height > 0)
            {
                var rect = new Rectangle(0, 0, Width, Height);

                // Adjust according to window styles
                User32.AdjustWindowRectEx(
                    ref rect,
                    style,
                    false,
                    styleEx);

                windowWidth = rect.Right - rect.Left;
                windowHeight = rect.Bottom - rect.Top;
            }
            else
            {
                x = y = windowWidth = windowHeight = CW_USEDEFAULT;
            }
            WNDCLASSEX windowClass = new WNDCLASSEX();
            windowClass.ClassExtraBytes = Marshal.SizeOf<WNDCLASSEX>();
            windowClass.Styles = WindowClassStyles.CS_HREDRAW | WindowClassStyles.CS_VREDRAW;
            windowClass.InstanceHandle = Kernel32.GetModuleHandle(null);
            windowClass.CursorHandle = User32.LoadCursor(IntPtr.Zero, SystemCursor.IDC_ARROW);
            windowClass.ClassName = "Window";
            windowClass.Size = Marshal.SizeOf<WNDCLASSEX>();
            windowClass.WindowProc = proc;

            User32.RegisterClassEx(ref windowClass);
            var hwnd = User32.CreateWindowEx(
                (int)styleEx,
                windowClass.ClassName,
                Title,
                (int)style,
                x,
                y,
                windowWidth,
                windowHeight,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero);

            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            User32.ShowWindow(hwnd, ShowWindowCommand.Normal);
            Handle = hwnd;
            Width = windowWidth;
            Height = windowHeight;
        }

        public virtual IntPtr WNDPROC(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam)
        {
            if (context.imguiInputHandler != null && context.imguiInputHandler.ProcessMessage((WindowMessage)msg, wParam, lParam))
                return IntPtr.Zero;

            switch ((WindowMessage)msg)
            {
                case WindowMessage.Destroy:
                    User32.PostQuitMessage(0);
                    break;
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

            return User32.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        public void Destroy()
        {
            var hwnd = (IntPtr)Handle;
            if (hwnd != IntPtr.Zero)
            {
                var destroyHandle = hwnd;
                Handle = IntPtr.Zero;

                User32.DestroyWindow(destroyHandle);
            }
        }
    }
}
