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

        public Dictionary<IntPtr, string> ptr2String = new Dictionary<IntPtr, string>();
        public Dictionary<string, IntPtr> string2Ptr = new Dictionary<string, IntPtr>();

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
        }

        int a = 65536;
        public IntPtr GetStringId(string s)
        {
            if (string2Ptr.TryGetValue(s, out IntPtr ptr))
                return ptr;
            ptr = new IntPtr(a);
            string2Ptr[s] = ptr;
            ptr2String[ptr] = s;
            a++;
            return ptr;
        }
        public string IdToString(IntPtr ptr) => ptr2String[ptr];
        public Texture2D GetTexByStrId(IntPtr ptr) => renderTargets[ptr2String[ptr]];


        byte[] LoadShader(DxcShaderStage shaderStage, string path, string entryPoint)
        {
            var result = DxcCompiler.Compile(shaderStage, File.ReadAllText(path), entryPoint);
            if (result.GetStatus() != SharpGen.Runtime.Result.Ok)
                throw new Exception(result.GetErrors());
            return result.GetResult().ToArray();
        }

        //get or create mesh
        public Mesh GetMesh(string name)
        {
            if (meshes.TryGetValue(name, out Mesh mesh))
            {
                return mesh;
            }
            else
            {
                mesh = new Mesh();
                mesh.unnamedInputLayout = new UnnamedInputLayout()
                {
                    inputElementDescriptions = new[]
                    {
                     new InputElementDescription("POSITION", 0, Format.R32G32_Float, 0),
                     new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, 1),
                     new InputElementDescription("COLOR", 0, Format.R8G8B8A8_UNorm, 2)
                     }
                };
                meshes[name] = mesh;
                return mesh;
            }
        }

        public void GPUUploadDatas(GraphicsContext graphicsContext1)
        {
            while (uploadQueue.TryDequeue(out var upload))
            {
                //if (upload.mesh != null)
                //{
                //    graphicsContext1.UploadMesh(upload.mesh, upload.vertexData, upload.indexData, upload.stride, upload.format);
                //}
                if (upload.texture2D != null)
                {
                    graphicsContext1.UploadTexture(upload.texture2D, upload.textureData);
                }
            }
        }

        public void Dispose()
        {
            uploadBuffer?.Dispose();
            DisposeDictionaryItems(rootSignatures);
            DisposeDictionaryItems(renderTargets);
            DisposeDictionaryItems(pipelineStateObjects);
            DisposeDictionaryItems(meshes);

            graphicsContext.Dispose();
            device.Dispose();
        }
        void DisposeDictionaryItems<T1, T2>(Dictionary<T1, T2> dict) where T2 : IDisposable
        {
            foreach (var pair in dict)
                pair.Value.Dispose();
            dict.Clear();
        }
    }
}
