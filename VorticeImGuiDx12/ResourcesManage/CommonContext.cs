using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Vortice.Dxc;
using VorticeImGuiDx12.Graphics;
using System.Collections.Concurrent;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace VorticeImGuiDx12.ResourcesManage
{
    public class CommonContext : IDisposable
    {
        public GraphicsDevice device = new GraphicsDevice();
        public GraphicsContext graphicsContext = new GraphicsContext();
        public Dictionary<string, Shader> VertexShaders = new Dictionary<string, Shader>();
        public Dictionary<string, Shader> PixelShaders = new Dictionary<string, Shader>();
        public Dictionary<string, RootSignature> rootSignatures = new Dictionary<string, RootSignature>();
        public Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
        public Dictionary<string, Texture2D> renderTargets = new Dictionary<string, Texture2D>();
        public Dictionary<string, PipelineStateObject> pipelineStateObjects = new Dictionary<string, PipelineStateObject>();
        public ConcurrentQueue<GPUUpload> uploadQueue = new ConcurrentQueue<GPUUpload>();
        public RingUploadBuffer uploadBuffer = new RingUploadBuffer();

        public IntPtr imguiContext;
        public ImGuiInputHandler imguiInputHandler;

        public void LoadDefaultResource()
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(RenderResource));
            string resourcePath = "Data";
            RenderResource renderResource = (RenderResource)xmlSerializer.Deserialize(File.OpenRead(Path.Combine(resourcePath, "DefaultResource.xml")));

            foreach (var vs in renderResource.VertexShaders)
            {
                VertexShaders[vs.Name] = new Shader() { compiledCode = LoadShader(DxcShaderStage.Vertex, Path.Combine(resourcePath, vs.Path), "main"), Name = vs.Name };
            }
            foreach (var ps in renderResource.PixelShaders)
            {
                PixelShaders[ps.Name] = new Shader() { compiledCode = LoadShader(DxcShaderStage.Pixel, Path.Combine(resourcePath, ps.Path), "main"), Name = ps.Name };
            }
            foreach (var ps in renderResource.PipelineStates)
            {
                pipelineStateObjects[ps.Name] = new PipelineStateObject(VertexShaders[ps.VertexShader], PixelShaders[ps.PixelShader]); ;
            }
            RegisterInputLayouts();
        }

        byte[] LoadShader(DxcShaderStage shaderStage, string path, string entryPoint)
        {
            var result = DxcCompiler.Compile(shaderStage, File.ReadAllText(path), entryPoint);
            if (result.GetStatus() != SharpGen.Runtime.Result.Ok)
                throw new Exception(result.GetErrors());
            return result.GetResult().ToArray();
        }

        void RegisterInputLayouts()
        {
            device.inputLayouts["ImGui"] = new InputLayoutDescription(
                     new InputElementDescription("POSITION", 0, Format.R32G32_Float, 0),
                     new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, 0),
                     new InputElementDescription("COLOR", 0, Format.R8G8B8A8_UNorm, 0)
                     );
        }

        //get or create mesh
        public Mesh GetMesh(string name)
        {
            if(meshes.TryGetValue(name,out Mesh mesh))
            {
                return mesh;
            }
            else
            {
                mesh = new Mesh();
                meshes[name] = mesh;
                return mesh;
            }
        }

        public void GPUUploadDatas()
        {
            while (uploadQueue.TryDequeue(out var upload))
            {
                if (upload.mesh != null)
                {
                    graphicsContext.UploadMesh(upload.mesh, upload.vertexData, upload.indexData, upload.stride, upload.format);
                }
                if (upload.texture2D != null)
                {
                    graphicsContext.UploadTexture(upload.texture2D, upload.textureData);
                }
            }
        }

        public void Dispose()
        {
            uploadBuffer?.Dispose();
            foreach (var pair in rootSignatures)
                pair.Value.Dispose();
            foreach (var pair in renderTargets)
                pair.Value.Dispose();
            foreach (var pair in pipelineStateObjects)
                pair.Value.Dispose();
            foreach (var pair in meshes)
                pair.Value.Dispose();
            graphicsContext.Dispose();
            device.Dispose();
        }
    }
}
