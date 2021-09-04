using System;
using System.Collections.Generic;
using System.Text;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace VorticeImGuiDx12.Graphics
{
    public class Mesh : IDisposable
    {
        public ID3D12Resource _vertex;
        public ID3D12Resource index;
        public UnnamedInputLayout unnamedInputLayout;
        public Dictionary<string, _VertexBuffer> vertices = new Dictionary<string, _VertexBuffer>();

        public int indexCount;
        public int indexSizeInByte;
        public string Name;
        public Format indexFormat;

        public void Dispose()
        {
            _vertex?.Dispose();
            _vertex = null;
            if (vertices != null)
                foreach (var pair in vertices)
                {
                    pair.Value.Dispose();
                }
            vertices?.Clear();
            index?.Dispose();
            index = null;
        }
    }
    public class _VertexBuffer : IDisposable
    {
        public ID3D12Resource resource;
        public int offset;
        public int sizeInByte;
        public int stride;

        public void Dispose()
        {
            if (offset == 0)
                resource.Dispose();
        }
    }
}
