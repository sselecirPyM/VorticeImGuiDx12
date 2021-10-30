using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace VorticeImGuiDx12.Graphics
{
    unsafe struct D3D12_MEMCPY_DEST
    {
        public void* pData;
        public ulong RowPitch;
        public ulong SlicePitch;
    }
    public class GraphicsContext : IDisposable
    {
        const int D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING = 5768;

        public void Initialize(GraphicsDevice graphicsDevice)
        {
            ThrowIfFailed(graphicsDevice.device.CreateCommandList(0, CommandListType.Direct, graphicsDevice.GetCommandAllocator(), null, out commandList));
            commandList.Close();
            this.graphicsDevice = graphicsDevice;
        }

        public void DrawIndexedInstanced(int indexCountPerInstance, int instanceCount, int startIndexLocation, int baseVertexLocation, int startInstanceLocation)
        {
            commandList.SetPipelineState(pipelineStateObject.GetState(graphicsDevice, psoDesc, currentRootSignature, unnamedInputLayout));
            commandList.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
        }

        public void SetDescriptorHeapDefault()
        {
            commandList.SetDescriptorHeaps(1, new[] { graphicsDevice.cbvsrvuavHeap.heap });
        }

        public void SetRootSignature(RootSignature rootSignature)
        {
            currentRootSignature = rootSignature;
            commandList.SetGraphicsRootSignature(rootSignature.rootSignature);
        }

        public void SetComputeRootSignature(RootSignature rootSignature)
        {
            currentRootSignature = rootSignature;
            commandList.SetComputeRootSignature(rootSignature.rootSignature);
        }

        public void SetSRV(Texture2D texture, int slot)
        {
            ShaderResourceViewDescription shaderResourceViewDescription = new ShaderResourceViewDescription();
            shaderResourceViewDescription.ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.Texture2D;
            shaderResourceViewDescription.Format = texture.format;
            shaderResourceViewDescription.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
            shaderResourceViewDescription.Texture2D.MipLevels = texture.mipLevels;

            texture.StateChange(commandList, ResourceStates.GenericRead);
            graphicsDevice.cbvsrvuavHeap.GetTempHandle(out CpuDescriptorHandle cpuHandle, out GpuDescriptorHandle gpuHandle);
            graphicsDevice.device.CreateShaderResourceView(texture.resource, shaderResourceViewDescription, cpuHandle);
            commandList.SetGraphicsRootDescriptorTable(currentRootSignature.srv[slot], gpuHandle);
        }

        public void SetDSVRTV(Texture2D dsv, Texture2D[] rtvs, bool clearDSV, bool clearRTV)
        {
            dsv?.StateChange(commandList, ResourceStates.DepthWrite);
            CpuDescriptorHandle[] rtvHandles = null;
            if (rtvs != null)
            {
                rtvHandles = new CpuDescriptorHandle[rtvs.Length];
                for (int i = 0; i < rtvs.Length; i++)
                {
                    Texture2D rtv = rtvs[i];
                    rtv.StateChange(commandList, ResourceStates.RenderTarget);
                    rtvHandles[i] = rtv.renderTargetView.GetCPUDescriptorHandleForHeapStart();
                }
            }
            if (clearDSV && dsv != null)
                commandList.ClearDepthStencilView(dsv.depthStencilView.GetCPUDescriptorHandleForHeapStart(), ClearFlags.Depth | ClearFlags.Stencil, 1.0f, 0);
            if (clearRTV && rtvs != null)
            {
                foreach (var rtv in rtvs)
                    commandList.ClearRenderTargetView(rtv.renderTargetView.GetCPUDescriptorHandleForHeapStart(), new Color4());
            }

            commandList.OMSetRenderTargets(rtvHandles, dsv.depthStencilView.GetCPUDescriptorHandleForHeapStart());
        }

        public void SetRenderTargetScreen()
        {
            commandList.RSSetViewport(new Viewport(0, 0, graphicsDevice.width, graphicsDevice.height, 0.0f, 1.0f));
            commandList.RSSetScissorRect(new Rectangle(0, 0, graphicsDevice.width, graphicsDevice.height));
            commandList.OMSetRenderTargets(graphicsDevice.GetRenderTargetScreen());
        }

        public void SetMesh(Mesh mesh)
        {
            commandList.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

            int c = -1;
            foreach (var desc in mesh.unnamedInputLayout.inputElementDescriptions)
            {
                if (desc.Slot != c)
                {
                    if (mesh.vertices != null && mesh.vertices.TryGetValue(desc.SemanticName, out var vertex))
                    {
                        commandList.IASetVertexBuffers(desc.Slot, new VertexBufferView(vertex.resource.GPUVirtualAddress + (ulong)vertex.offset, vertex.sizeInByte - vertex.offset, vertex.stride));
                    }
                    c = desc.Slot;
                }
            }

            if (mesh.index != null)
                commandList.IASetIndexBuffer(new IndexBufferView(mesh.index.GPUVirtualAddress, mesh.indexSizeInByte, mesh.indexFormat));
            unnamedInputLayout = mesh.unnamedInputLayout;
        }

        public void UploadTexture(Texture2D texture, byte[] data)
        {
            ID3D12Resource resourceUpload1 = graphicsDevice.device.CreateCommittedResource<ID3D12Resource>(
                new HeapProperties(HeapType.Upload),
                HeapFlags.None,
                ResourceDescription.Buffer((ulong)data.Length),
                ResourceStates.GenericRead);
            graphicsDevice.DestroyResource(resourceUpload1);
            graphicsDevice.DestroyResource(texture.resource);
            texture.resource = graphicsDevice.device.CreateCommittedResource<ID3D12Resource>(
                HeapProperties.DefaultHeapProperties,
                HeapFlags.None,
                ResourceDescription.Texture2D(texture.format, (ulong)texture.width, texture.height, 1, 1),
                ResourceStates.CopyDestination);

            uint bitsPerPixel = GraphicsDevice.BitsPerPixel(texture.format);
            int width = texture.width;
            int height = texture.height;
            GCHandle gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            SubresourceData subresourcedata = new SubresourceData();
            subresourcedata.DataPointer = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0);
            subresourcedata.RowPitch = (IntPtr)(width * bitsPerPixel / 8);
            subresourcedata.SlicePitch = (IntPtr)(width * height * bitsPerPixel / 8);
            UpdateSubresources(commandList, texture.resource, resourceUpload1, 0, 0, 1, new SubresourceData[] { subresourcedata });
            gcHandle.Free();
            commandList.ResourceBarrierTransition(texture.resource, ResourceStates.CopyDestination, ResourceStates.GenericRead);
            texture.resourceStates = ResourceStates.GenericRead;
        }

        public void SetCBV(UploadBuffer uploadBuffer, int offset, int slot)
        {
            commandList.SetGraphicsRootConstantBufferView(currentRootSignature.cbv[slot], uploadBuffer.resource.GPUVirtualAddress + (ulong)offset);
        }

        public void SetPipelineState(PipelineStateObject pipelineStateObject, PSODesc psoDesc)
        {
            this.pipelineStateObject = pipelineStateObject;
            this.psoDesc = psoDesc;
        }

        public void ClearRenderTarget(Texture2D texture2D)
        {
            commandList.ClearRenderTargetView(texture2D.renderTargetView.GetCPUDescriptorHandleForHeapStart(), new Color4(0, 0, 0, 0));
        }

        public void ClearRenderTargetScreen(Color4 color)
        {
            commandList.ClearRenderTargetView(graphicsDevice.GetRenderTargetScreen(), color);
        }

        public void ScreenBeginRender()
        {
            commandList.ResourceBarrierTransition(graphicsDevice.GetScreenResource(), ResourceStates.Present, ResourceStates.RenderTarget);
        }

        public void ScreenEndRender()
        {
            commandList.ResourceBarrierTransition(graphicsDevice.GetScreenResource(), ResourceStates.RenderTarget, ResourceStates.Present);
        }

        public void BeginCommand()
        {
            commandList.Reset(graphicsDevice.GetCommandAllocator());
        }

        public void EndCommand()
        {
            commandList.Close();
        }

        public void Execute()
        {
            graphicsDevice.commandQueue.ExecuteCommandList(commandList);
        }

        unsafe void MemcpySubresource(
            D3D12_MEMCPY_DEST* pDest,
            SubresourceData pSrc,
            int RowSizeInBytes,
            int NumRows,
            int NumSlices)
        {
            for (uint z = 0; z < NumSlices; ++z)
            {
                byte* pDestSlice = (byte*)(pDest->pData) + pDest->SlicePitch * z;
                byte* pSrcSlice = (byte*)(pSrc.DataPointer) + (long)pSrc.SlicePitch * z;
                for (int y = 0; y < NumRows; ++y)
                {
                    new Span<byte>(pSrcSlice + ((long)pSrc.RowPitch * y), RowSizeInBytes).CopyTo(new Span<byte>(pDestSlice + (long)pDest->RowPitch * y, RowSizeInBytes));
                }
            }
        }
        unsafe ulong UpdateSubresources(
            ID3D12GraphicsCommandList pCmdList,
            ID3D12Resource pDestinationResource,
            ID3D12Resource pIntermediate,
            int FirstSubresource,
            int NumSubresources,
            ulong RequiredSize,
            PlacedSubresourceFootPrint[] pLayouts,
            int[] pNumRows,
            ulong[] pRowSizesInBytes,
            SubresourceData[] pSrcData)
        {
            var IntermediateDesc = pIntermediate.Description;
            var DestinationDesc = pDestinationResource.Description;
            if (IntermediateDesc.Dimension != ResourceDimension.Buffer ||
                IntermediateDesc.Width < RequiredSize + pLayouts[0].Offset ||
                (DestinationDesc.Dimension == ResourceDimension.Buffer &&
                    (FirstSubresource != 0 || NumSubresources != 1)))
            {
                return 0;
            }

            byte* pData;
            IntPtr data1 = pIntermediate.Map(0, null);
            pData = (byte*)data1;

            for (uint i = 0; i < NumSubresources; ++i)
            {
                D3D12_MEMCPY_DEST DestData = new D3D12_MEMCPY_DEST { pData = pData + pLayouts[i].Offset, RowPitch = (ulong)pLayouts[i].Footprint.RowPitch, SlicePitch = (uint)(pLayouts[i].Footprint.RowPitch) * (uint)(pNumRows[i]) };
                MemcpySubresource(&DestData, pSrcData[i], (int)(pRowSizesInBytes[i]), pNumRows[i], pLayouts[i].Footprint.Depth);
            }
            pIntermediate.Unmap(0, null);

            if (DestinationDesc.Dimension == ResourceDimension.Buffer)
            {
                pCmdList.CopyBufferRegion(
                    pDestinationResource, 0, pIntermediate, pLayouts[0].Offset, (ulong)pLayouts[0].Footprint.Width);
            }
            else
            {
                for (int i = 0; i < NumSubresources; ++i)
                {
                    TextureCopyLocation Dst = new TextureCopyLocation(pDestinationResource, i + FirstSubresource);
                    TextureCopyLocation Src = new TextureCopyLocation(pIntermediate, pLayouts[i]);
                    pCmdList.CopyTextureRegion(Dst, 0, 0, 0, Src, null);
                }
            }
            return RequiredSize;
        }

        ulong UpdateSubresources(
            ID3D12GraphicsCommandList pCmdList,
            ID3D12Resource pDestinationResource,
            ID3D12Resource pIntermediate,
            ulong IntermediateOffset,
            int FirstSubresource,
            int NumSubresources,
            SubresourceData[] pSrcData)
        {
            PlacedSubresourceFootPrint[] pLayouts = new PlacedSubresourceFootPrint[NumSubresources];
            ulong[] pRowSizesInBytes = new ulong[NumSubresources];
            int[] pNumRows = new int[NumSubresources];

            var Desc = pDestinationResource.Description;
            ID3D12Device pDevice = null;
            pDestinationResource.GetDevice(out pDevice);
            pDevice.GetCopyableFootprints(Desc, (int)FirstSubresource, (int)NumSubresources, IntermediateOffset, pLayouts, pNumRows, pRowSizesInBytes, out ulong RequiredSize);
            pDevice.Release();

            ulong Result = UpdateSubresources(pCmdList, pDestinationResource, pIntermediate, FirstSubresource, NumSubresources, RequiredSize, pLayouts, pNumRows, pRowSizesInBytes, pSrcData);
            return Result;
        }

        public ID3D12GraphicsCommandList5 commandList;
        public GraphicsDevice graphicsDevice;
        private void ThrowIfFailed(SharpGen.Runtime.Result hr)
        {
            if (hr != SharpGen.Runtime.Result.Ok)
            {
                throw new NotImplementedException();
            }
        }
        public RootSignature currentRootSignature;
        public PipelineStateObject pipelineStateObject;
        public PSODesc psoDesc;
        public UnnamedInputLayout unnamedInputLayout;

        public void Dispose()
        {
            commandList?.Dispose();
            commandList = null;
        }
    }
}
