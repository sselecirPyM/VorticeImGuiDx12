using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Text;
using Vortice.Mathematics;
using VorticeImGuiDx12.Graphics;
using VorticeImGuiDx12.Interoperation;
using VorticeImGuiDx12.RenderPipeline;
using VorticeImGuiDx12.ResourcesManage;

namespace VorticeImGuiDx12
{
    class AppWindow : IDisposable
    {
        CommonContext context = new CommonContext();
        CommonRenderPipeline commonRenderPipeline = new CommonRenderPipeline();
        ImGuiRender imGuiRender = new ImGuiRender();
        DateTime current;
        public Win32Window Win32Window;
        public AppWindow(Win32Window Win32Window)
        {
            this.Win32Window = Win32Window;
        }
        public void Initialize()
        {
            commonRenderPipeline.context = context;
            imGuiRender.context = context;
            context.LoadDefaultResource();
            commonRenderPipeline.Initialize();

            context.device.Initialize();
            context.uploadBuffer.Initialize(context.device, 67108864);//64 MB


            //GPUUpload uploadTest = new GPUUpload();//just test
            //uploadTest.mesh = context.GetMesh("quad");
            //uploadTest.Quad();
            //context.uploadQueue.Enqueue(uploadTest);

            imGuiRender.Init();
            context.imguiInputHandler = new ImGuiInputHandler();
            context.imguiInputHandler.hwnd = Win32Window.Handle;

            context.graphicsContext.Initialize(context.device);
            context.device.SetupSwapChain((IntPtr)Win32Window.Handle);
        }
        public void Show()
        {
            User32.ShowWindow(Win32Window.Handle, ShowWindowCommand.Normal);
            Initialize();
        }
        public void UpdateAndDraw()
        {
            if (Win32Window.Width != context.device.width || Win32Window.Height != context.device.height)
            {
                context.device.Resize(Win32Window.Width, Win32Window.Height);
                ImGui.GetIO().DisplaySize = new System.Numerics.Vector2(context.device.width, context.device.height);
            }

            var graphicsContext = context.graphicsContext;
            context.device.Begin();
            graphicsContext.BeginCommand();
            context.GPUUploadDatas(graphicsContext);
            graphicsContext.SetDescriptorHeapDefault();
            graphicsContext.ScreenBeginRender();
            graphicsContext.SetRenderTargetScreen();
            graphicsContext.ClearRenderTargetScreen(new Color4(0.5f, 0.5f, 1, 1));

            commonRenderPipeline.Prepare();
            commonRenderPipeline.Render();
            ImGui.SetCurrentContext(context.imguiContext);
            var previous = current;
            current = DateTime.Now;
            float delta = (float)(current - previous).TotalSeconds;
            ImGui.GetIO().DeltaTime = delta;
            context.imguiInputHandler.Update();
            imGuiRender.Render();
            graphicsContext.ScreenEndRender();
            graphicsContext.EndCommand();
            graphicsContext.Execute();
            context.device.Present(true);
        }

        public virtual bool ProcessMessage(uint msg, UIntPtr wParam, IntPtr lParam)
        {
            if (context.imguiInputHandler != null && context.imguiInputHandler.ProcessMessage((WindowMessage)msg, wParam, lParam))
                return true;

            return Win32Window.ProcessMessage(msg, wParam, lParam);
        }

        public void Dispose()
        {
            context.Dispose();
        }
    }
}
