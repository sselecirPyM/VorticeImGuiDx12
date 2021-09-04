using System;
using System.Collections.Generic;
using System.Text;
using ImGuiNET;
using VorticeImGuiDx12.ResourcesManage;
using VorticeImGuiDx12.Graphics;
using Vortice.Direct3D12;
using Vortice.DXGI;
using ImDrawIdx = System.UInt16;
using System.Numerics;

namespace VorticeImGuiDx12.RenderPipeline
{
    unsafe public class ImGuiRender
    {
        public CommonContext context;
        public InputLayoutDescription inputLayoutDescription;
        public Texture2D fontTexture;
        public Mesh imguiMesh;

        PSODesc psoDesc = new PSODesc
        {
            CullMode = CullMode.None,
            RenderTargetFormat = Format.R8G8B8A8_UNorm,
            RenderTargetCount = 1,
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
            InputLayout = "ImGui",
            BlendState = "Alpha",
        };

        public void Init()
        {
            context.imguiContext = ImGui.CreateContext();
            ImGui.SetCurrentContext(context.imguiContext);
            var io = ImGui.GetIO();
            io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
            fontTexture = new Texture2D();
            context.renderTargets["imgui_font"] = fontTexture;

            //ImFontPtr font = io.Fonts.AddFontFromFileTTF("c:\\Windows\\Fonts\\SIMHEI.ttf", 14, null, io.Fonts.GetGlyphRangesChineseFull());

            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);
            io.Fonts.TexID = context.GetStringId("imgui_font");

            fontTexture.width = width;
            fontTexture.height = height;
            fontTexture.mipLevels = 1;
            fontTexture.format = Format.R8G8B8A8_UNorm;
            imguiMesh = context.GetMesh("imgui_mesh");

            GPUUpload gpuUpload = new GPUUpload();
            gpuUpload.texture2D = fontTexture;
            gpuUpload.format = Format.R8G8B8A8_UNorm;
            gpuUpload.textureData = new byte[width * height * bytesPerPixel];
            new Span<byte>(pixels, gpuUpload.textureData.Length).CopyTo(gpuUpload.textureData);

            context.uploadQueue.Enqueue(gpuUpload);

        }
        public void Render()
        {
            ImGui.NewFrame();
            ImGui.ShowDemoWindow();
            ImGui.Render();
            var data = ImGui.GetDrawData();
            GraphicsContext graphicsContext = context.graphicsContext;
            float L = data.DisplayPos.X;
            float R = data.DisplayPos.X + data.DisplaySize.X;
            float T = data.DisplayPos.Y;
            float B = data.DisplayPos.Y + data.DisplaySize.Y;
            float[] mvp =
            {
                    2.0f/(R-L),   0.0f,           0.0f,       0.0f,
                    0.0f,         2.0f/(T-B),     0.0f,       0.0f,
                    0.0f,         0.0f,           0.5f,       0.0f,
                    (R+L)/(L-R),  (T+B)/(B-T),    0.5f,       1.0f,
            };
            int index1 = context.uploadBuffer.Upload<float>(mvp);
            graphicsContext.SetRootSignature(Pipeline12Util.FromString(context, "Cssss"));
            graphicsContext.SetPipelineState(context.pipelineStateObjects["ImGui"], psoDesc);
            context.uploadBuffer.SetCBV(graphicsContext, index1, 0);
            graphicsContext.commandList.IASetPrimitiveTopology(Vortice.Direct3D.PrimitiveTopology.TriangleList);

            Vector2 clip_off = data.DisplayPos;
            for (int i = 0; i < data.CmdListsCount; i++)
            {
                var cmdList = data.CmdListsRange[i];
                var vertBytes = cmdList.VtxBuffer.Size * sizeof(ImDrawVert);
                var indexBytes = cmdList.IdxBuffer.Size * sizeof(ImDrawIdx);

                context.uploadBuffer.UploadMeshIndex(graphicsContext, imguiMesh, new Span<byte>(cmdList.IdxBuffer.Data.ToPointer(), indexBytes), Format.R16_UInt);
                context.uploadBuffer.UploadVertexBuffer(graphicsContext,ref imguiMesh._vertex, new Span<byte>(cmdList.VtxBuffer.Data.ToPointer(), vertBytes));
                imguiMesh.vertices["POSITION"] = new _VertexBuffer() { offset = 0, resource = imguiMesh._vertex, sizeInByte = vertBytes, stride = sizeof(ImDrawVert) };
                imguiMesh.vertices["TEXCOORD"] = new _VertexBuffer() { offset = 8, resource = imguiMesh._vertex, sizeInByte = vertBytes, stride = sizeof(ImDrawVert) };
                imguiMesh.vertices["COLOR"] = new _VertexBuffer() { offset = 16, resource = imguiMesh._vertex, sizeInByte = vertBytes, stride = sizeof(ImDrawVert) };

                graphicsContext.SetMesh(imguiMesh);

                for (int j = 0; j < cmdList.CmdBuffer.Size; j++)
                {
                    var cmd = cmdList.CmdBuffer[j];
                    if (cmd.UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException("user callbacks not implemented");
                    }
                    else
                    {
                        graphicsContext.SetSRV(context.GetTexByStrId(cmd.TextureId), 0);
                        var rect = new Vortice.RawRect((int)(cmd.ClipRect.X - clip_off.X), (int)(cmd.ClipRect.Y - clip_off.Y), (int)(cmd.ClipRect.Z - clip_off.X), (int)(cmd.ClipRect.W - clip_off.Y));
                        graphicsContext.commandList.RSSetScissorRects(new[] { rect });

                        graphicsContext.DrawIndexedInstanced((int)cmd.ElemCount, 1, (int)(cmd.IdxOffset), (int)(cmd.VtxOffset), 0);
                    }
                }
            }
        }
    }
}
