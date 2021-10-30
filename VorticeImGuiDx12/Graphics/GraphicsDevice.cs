using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Vortice.Direct3D12;
using Vortice.Direct3D12.Debug;
using Vortice.Dxc;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace VorticeImGuiDx12.Graphics
{
    public struct ResourceDelayDestroy
    {
        public ulong destroyFrame;
        public ID3D12Object resource;
    }
    public class GraphicsDevice : IDisposable
    {
        public static uint BitsPerPixel(Format format)
        {
            switch (format)
            {
                case Format.R32G32B32A32_Typeless:
                case Format.R32G32B32A32_Float:
                case Format.R32G32B32A32_UInt:
                case Format.R32G32B32A32_SInt:
                    return 128;

                case Format.R32G32B32_Typeless:
                case Format.R32G32B32_Float:
                case Format.R32G32B32_UInt:
                case Format.R32G32B32_SInt:
                    return 96;

                case Format.R16G16B16A16_Typeless:
                case Format.R16G16B16A16_Float:
                case Format.R16G16B16A16_UNorm:
                case Format.R16G16B16A16_UInt:
                case Format.R16G16B16A16_SNorm:
                case Format.R16G16B16A16_SInt:
                case Format.R32G32_Typeless:
                case Format.R32G32_Float:
                case Format.R32G32_UInt:
                case Format.R32G32_SInt:
                case Format.R32G8X24_Typeless:
                case Format.D32_Float_S8X24_UInt:
                case Format.R32_Float_X8X24_Typeless:
                case Format.X32_Typeless_G8X24_UInt:
                case Format.Y416:
                case Format.Y210:
                case Format.Y216:
                    return 64;

                case Format.R10G10B10A2_Typeless:
                case Format.R10G10B10A2_UNorm:
                case Format.R10G10B10A2_UInt:
                case Format.R11G11B10_Float:
                case Format.R8G8B8A8_Typeless:
                case Format.R8G8B8A8_UNorm:
                case Format.R8G8B8A8_UNorm_SRgb:
                case Format.R8G8B8A8_UInt:
                case Format.R8G8B8A8_SNorm:
                case Format.R8G8B8A8_SInt:
                case Format.R16G16_Typeless:
                case Format.R16G16_Float:
                case Format.R16G16_UNorm:
                case Format.R16G16_UInt:
                case Format.R16G16_SNorm:
                case Format.R16G16_SInt:
                case Format.R32_Typeless:
                case Format.D32_Float:
                case Format.R32_Float:
                case Format.R32_UInt:
                case Format.R32_SInt:
                case Format.R24G8_Typeless:
                case Format.D24_UNorm_S8_UInt:
                case Format.R24_UNorm_X8_Typeless:
                case Format.X24_Typeless_G8_UInt:
                case Format.R9G9B9E5_SharedExp:
                case Format.R8G8_B8G8_UNorm:
                case Format.G8R8_G8B8_UNorm:
                case Format.B8G8R8A8_UNorm:
                case Format.B8G8R8X8_UNorm:
                case Format.R10G10B10_Xr_Bias_A2_UNorm:
                case Format.B8G8R8A8_Typeless:
                case Format.B8G8R8A8_UNorm_SRgb:
                case Format.B8G8R8X8_Typeless:
                case Format.B8G8R8X8_UNorm_SRgb:
                case Format.AYUV:
                case Format.Y410:
                case Format.YUY2:
                    return 32;

                case Format.P010:
                case Format.P016:
                    return 24;

                case Format.R8G8_Typeless:
                case Format.R8G8_UNorm:
                case Format.R8G8_UInt:
                case Format.R8G8_SNorm:
                case Format.R8G8_SInt:
                case Format.R16_Typeless:
                case Format.R16_Float:
                case Format.D16_UNorm:
                case Format.R16_UNorm:
                case Format.R16_UInt:
                case Format.R16_SNorm:
                case Format.R16_SInt:
                case Format.B5G6R5_UNorm:
                case Format.B5G5R5A1_UNorm:
                case Format.A8P8:
                case Format.B4G4R4A4_UNorm:
                    return 16;

                case Format.NV12:
                //case Format.420_OPAQUE:
                case Format.Opaque420:
                case Format.NV11:
                    return 12;

                case Format.R8_Typeless:
                case Format.R8_UNorm:
                case Format.R8_UInt:
                case Format.R8_SNorm:
                case Format.R8_SInt:
                case Format.A8_UNorm:
                case Format.AI44:
                case Format.IA44:
                case Format.P8:
                    return 8;

                case Format.R1_UNorm:
                    return 1;

                case Format.BC1_Typeless:
                case Format.BC1_UNorm:
                case Format.BC1_UNorm_SRgb:
                case Format.BC4_Typeless:
                case Format.BC4_UNorm:
                case Format.BC4_SNorm:
                    return 4;

                case Format.BC2_Typeless:
                case Format.BC2_UNorm:
                case Format.BC2_UNorm_SRgb:
                case Format.BC3_Typeless:
                case Format.BC3_UNorm:
                case Format.BC3_UNorm_SRgb:
                case Format.BC5_Typeless:
                case Format.BC5_UNorm:
                case Format.BC5_SNorm:
                case Format.BC6H_Typeless:
                case Format.BC6H_Uf16:
                case Format.BC6H_Sf16:
                case Format.BC7_Typeless:
                case Format.BC7_UNorm:
                case Format.BC7_UNorm_SRgb:
                    return 8;

                default:
                    return 0;
            }
        }
        public void Initialize()
        {
#if DEBUG
            if (D3D12.D3D12GetDebugInterface<ID3D12Debug>(out var pDx12Debug).Success)
            {
                pDx12Debug.EnableDebugLayer();
            }
#endif
            ThrowIfFailed(DXGI.CreateDXGIFactory1(out dxgiFactory));
            int index1 = 0;
            while (true)
            {
                var hr = dxgiFactory.EnumAdapterByGpuPreference(index1, GpuPreference.HighPerformance, out adapter);
                if (hr == SharpGen.Runtime.Result.Ok)
                {
                    break;
                }
                index1++;
            }
            ThrowIfFailed(D3D12.D3D12CreateDevice(this.adapter, out device));
            CommandQueueDescription description;
            description.Flags = CommandQueueFlags.None;
            description.Type = CommandListType.Direct;
            description.NodeMask = 0;
            description.Priority = 0;
            ThrowIfFailed(device.CreateCommandQueue(description, out commandQueue));
            DescriptorHeapDescription descriptorHeapDescription;
            descriptorHeapDescription.DescriptorCount = CBVSRVUAVDescriptorCount;
            descriptorHeapDescription.Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView;
            descriptorHeapDescription.Flags = DescriptorHeapFlags.ShaderVisible;
            descriptorHeapDescription.NodeMask = 0;
            cbvsrvuavHeap.Initialize(this, descriptorHeapDescription);

            descriptorHeapDescription.DescriptorCount = 64;
            descriptorHeapDescription.Type = DescriptorHeapType.DepthStencilView;
            descriptorHeapDescription.Flags = DescriptorHeapFlags.None;
            dsvHeap.Initialize(this, descriptorHeapDescription);

            descriptorHeapDescription.DescriptorCount = 64;
            descriptorHeapDescription.Type = DescriptorHeapType.RenderTargetView;
            descriptorHeapDescription.Flags = DescriptorHeapFlags.None;
            rtvHeap.Initialize(this, descriptorHeapDescription);
            waitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

            commandAllocators = new List<ID3D12CommandAllocator>();
            for (int i = 0; i < bufferCount; i++)
            {
                ThrowIfFailed(device.CreateCommandAllocator(CommandListType.Direct, out ID3D12CommandAllocator commandAllocator));
                commandAllocators.Add(commandAllocator);
            }
            ThrowIfFailed(device.CreateFence(executeCount, FenceFlags.None, out fence));
            executeCount++;
        }

        public void SetupSwapChain(IntPtr hwnd)
        {
            this.hwnd = hwnd;
        }

        public void Resize(int width, int height)
        {
            WaitForGpu();
            this.width = Math.Max(width, 1);
            this.height = Math.Max(height, 1);
            if (swapChain == null)
            {
                SwapChainDescription1 swapChainDescription;
                swapChainDescription.Width = width;
                swapChainDescription.Height = height;
                swapChainDescription.Format = swapChainFormat;
                swapChainDescription.Stereo = false;
                swapChainDescription.SampleDescription.Count = 1;
                swapChainDescription.SampleDescription.Quality = 0;
                swapChainDescription.Usage = Usage.RenderTargetOutput;
                swapChainDescription.BufferCount = bufferCount;
                swapChainDescription.SwapEffect = SwapEffect.FlipDiscard;
                swapChainDescription.Flags = SwapChainFlags.AllowTearing;
                swapChainDescription.Scaling = Scaling.Stretch;
                swapChainDescription.AlphaMode = AlphaMode.Ignore;
                IDXGISwapChain1 swapChain1 = dxgiFactory.CreateSwapChainForHwnd(commandQueue, hwnd, swapChainDescription);
                swapChain = swapChain1.QueryInterface<IDXGISwapChain3>();
                swapChain1.Dispose();
            }
            else
            {
                foreach (var screenResource in screenResources)
                    screenResource.Dispose();
                ThrowIfFailed(swapChain.ResizeBuffers(bufferCount, width, height, swapChainFormat, SwapChainFlags.AllowTearing));
            }
            screenResources = new List<ID3D12Resource>();
            for (int i = 0; i < bufferCount; i++)
            {
                ThrowIfFailed(swapChain.GetBuffer(i, out ID3D12Resource res));
                screenResources.Add(res);
            }
        }

        public ID3D12CommandAllocator GetCommandAllocator()
        {
            return commandAllocators[executeIndex];
        }

        public CpuDescriptorHandle GetRenderTargetScreen()
        {
            CpuDescriptorHandle handle = rtvHeap.GetTempCpuHandle();
            var res = screenResources[swapChain.GetCurrentBackBufferIndex()];
            device.CreateRenderTargetView(res, null, handle);
            return handle;
        }

        public ID3D12Resource GetScreenResource()
        {
            return screenResources[swapChain.GetCurrentBackBufferIndex()];
        }

        public void Begin()
        {
            GetCommandAllocator().Reset();
        }

        public void Present(bool vsync)
        {
            if (vsync)
                ThrowIfFailed(swapChain.Present(1, PresentFlags.None));
            else
                ThrowIfFailed(swapChain.Present(0, PresentFlags.AllowTearing));
            commandQueue.Signal(fence, executeCount);
            executeIndex = (executeIndex + 1) % bufferCount;
            if (fence.CompletedValue < executeCount - (uint)bufferCount + 1)
            {
                fence.SetEventOnCompletion(executeCount - (uint)bufferCount + 1, waitHandle);
                waitHandle.WaitOne();
            }
            DestroyResourceInternal(fence.CompletedValue);
            executeCount++;
        }

        public void WaitForGpu()
        {
            commandQueue.Signal(fence, executeCount);
            fence.SetEventOnCompletion(executeCount, waitHandle);
            waitHandle.WaitOne();
            DestroyResourceInternal(fence.CompletedValue);
            executeCount++;
        }

        public void CreateRootSignature(RootSignature rootSignature, IList<RootSignatureParamP> types)
        {
            //static samplers
            StaticSamplerDescription[] samplerDescription = new StaticSamplerDescription[4];
            samplerDescription[0] = new StaticSamplerDescription(ShaderVisibility.All, 0, 0)
            {
                AddressU = TextureAddressMode.Clamp,
                AddressV = TextureAddressMode.Clamp,
                AddressW = TextureAddressMode.Clamp,
                BorderColor = StaticBorderColor.OpaqueBlack,
                ComparisonFunction = ComparisonFunction.Never,
                Filter = Filter.MinMagMipLinear,
                MipLODBias = 0,
                MaxAnisotropy = 0,
                MinLOD = 0,
                MaxLOD = float.MaxValue,
                ShaderVisibility = ShaderVisibility.All,
                RegisterSpace = 0,
                ShaderRegister = 0,
            };
            samplerDescription[1] = samplerDescription[0];
            samplerDescription[2] = samplerDescription[0];
            samplerDescription[3] = samplerDescription[0];

            samplerDescription[1].ShaderRegister = 1;
            samplerDescription[2].ShaderRegister = 2;
            samplerDescription[3].ShaderRegister = 3;
            samplerDescription[1].MaxAnisotropy = 16;
            samplerDescription[1].Filter = Filter.Anisotropic;
            samplerDescription[2].ComparisonFunction = ComparisonFunction.Less;
            samplerDescription[2].Filter = Filter.ComparisonMinMagMipLinear;
            samplerDescription[3].Filter = Filter.MinMagMipPoint;

            RootParameter1[] rootParameters = new RootParameter1[types.Count];

            int cbvCount = 0;
            int srvCount = 0;
            int uavCount = 0;
            rootSignature.cbv.Clear();
            rootSignature.srv.Clear();
            rootSignature.uav.Clear();

            for (int i = 0; i < types.Count; i++)
            {
                RootSignatureParamP t = types[i];
                switch (t)
                {
                    case RootSignatureParamP.CBV:
                        rootParameters[i] = new RootParameter1(RootParameterType.ConstantBufferView, new RootDescriptor1(cbvCount, 0), ShaderVisibility.All);
                        rootSignature.cbv[cbvCount] = i;
                        cbvCount++;
                        break;
                    case RootSignatureParamP.SRV:
                        rootParameters[i] = new RootParameter1(RootParameterType.ShaderResourceView, new RootDescriptor1(srvCount, 0), ShaderVisibility.All);
                        rootSignature.srv[srvCount] = i;
                        srvCount++;
                        break;
                    case RootSignatureParamP.UAV:
                        rootParameters[i] = new RootParameter1(RootParameterType.UnorderedAccessView, new RootDescriptor1(uavCount, 0), ShaderVisibility.All);
                        rootSignature.uav[uavCount] = i;
                        uavCount++;
                        break;
                    case RootSignatureParamP.CBVTable:
                        rootParameters[i] = new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ConstantBufferView, 1, cbvCount)), ShaderVisibility.All);
                        rootSignature.cbv[cbvCount] = i;
                        cbvCount++;
                        break;
                    case RootSignatureParamP.SRVTable:
                        rootParameters[i] = new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ShaderResourceView, 1, srvCount)), ShaderVisibility.All);
                        rootSignature.srv[srvCount] = i;
                        srvCount++;
                        break;
                    case RootSignatureParamP.UAVTable:
                        rootParameters[i] = new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.UnorderedAccessView, 1, uavCount)), ShaderVisibility.All);
                        rootSignature.uav[uavCount] = i;
                        uavCount++;
                        break;
                }
            }

            RootSignatureDescription1 rootSignatureDescription = new RootSignatureDescription1();
            rootSignatureDescription.StaticSamplers = samplerDescription;
            rootSignatureDescription.Flags = RootSignatureFlags.AllowInputAssemblerInputLayout;
            rootSignatureDescription.Parameters = rootParameters;

            rootSignature.rootSignature = device.CreateRootSignature<ID3D12RootSignature>(0, rootSignatureDescription);
        }

        public void RenderTexture(Texture2D texture)
        {
            ResourceDescription resourceDescription = new ResourceDescription
            {
                Width = (ulong)texture.width,
                Height = texture.height,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Dimension = ResourceDimension.Texture2D,
                DepthOrArraySize = 1,
                Format = texture.format,
            };
            if (texture.dsvFormat != 0)
            {
                DestroyResource(texture.resource);
                resourceDescription.Flags = ResourceFlags.AllowDepthStencil;
                ThrowIfFailed(device.CreateCommittedResource<ID3D12Resource>(HeapProperties.DefaultHeapProperties,
                     HeapFlags.None,
                     resourceDescription,
                     ResourceStates.GenericRead,
                     new ClearValue(texture.dsvFormat, new DepthStencilValue(1.0f, 0)), out texture.resource));
                if (texture.depthStencilView == null)
                {
                    DescriptorHeapDescription descriptorHeapDescription;
                    descriptorHeapDescription.DescriptorCount = 1;
                    descriptorHeapDescription.Type = DescriptorHeapType.DepthStencilView;
                    descriptorHeapDescription.Flags = DescriptorHeapFlags.None;
                    descriptorHeapDescription.NodeMask = 0;
                    ThrowIfFailed(device.CreateDescriptorHeap(descriptorHeapDescription, out texture.depthStencilView));
                }

                device.CreateDepthStencilView(texture.resource, null, texture.depthStencilView.GetCPUDescriptorHandleForHeapStart());
            }
            else if (texture.rtvFormat != 0)
            {
                DestroyResource(texture.resource);
                resourceDescription.Flags = ResourceFlags.AllowRenderTarget | ResourceFlags.AllowUnorderedAccess;
                ThrowIfFailed(device.CreateCommittedResource<ID3D12Resource>(HeapProperties.DefaultHeapProperties,
                     HeapFlags.None,
                     resourceDescription,
                     ResourceStates.GenericRead,
                     new ClearValue(texture.dsvFormat, new Color4(0, 0, 0, 0)), out texture.resource));
                if (texture.renderTargetView == null)
                {
                    DescriptorHeapDescription descriptorHeapDescription;
                    descriptorHeapDescription.DescriptorCount = 1;
                    descriptorHeapDescription.Type = DescriptorHeapType.RenderTargetView;
                    descriptorHeapDescription.Flags = DescriptorHeapFlags.None;
                    descriptorHeapDescription.NodeMask = 0;
                    ThrowIfFailed(device.CreateDescriptorHeap(descriptorHeapDescription, out texture.renderTargetView));
                }

                device.CreateRenderTargetView(texture.resource, null, texture.renderTargetView.GetCPUDescriptorHandleForHeapStart());
            }
            else
            {
                throw new NotImplementedException();
            }
            texture.resourceStates = ResourceStates.GenericRead;
        }

        public void CreateUploadBuffer(UploadBuffer uploadBuffer, int size)
        {
            DestroyResource(uploadBuffer.resource);
            uploadBuffer.resource = device.CreateCommittedResource<ID3D12Resource>(
                HeapProperties.UploadHeapProperties,
                HeapFlags.None,
                ResourceDescription.Buffer(new ResourceAllocationInfo((ulong)size, 0)),
                ResourceStates.GenericRead);
            uploadBuffer.size = size;
        }

        public void DestroyResource(ID3D12Object res)
        {
            if (res != null)
                delayDestroy.Enqueue(new ResourceDelayDestroy { resource = res, destroyFrame = executeCount });
        }

        private void DestroyResourceInternal(ulong completedFrame)
        {
            while (delayDestroy.Count > 0)
                if (delayDestroy.Peek().destroyFrame <= completedFrame)
                {
                    var p = delayDestroy.Dequeue();
                    p.resource?.Dispose();
                }
                else
                    break;
        }

        public ID3D12Device2 device;
        public IDXGIAdapter adapter;
        public IDXGIFactory7 dxgiFactory;
        public ID3D12CommandQueue commandQueue;
        public DescriptorHeapX cbvsrvuavHeap = new DescriptorHeapX();
        public DescriptorHeapX dsvHeap = new DescriptorHeapX();
        public DescriptorHeapX rtvHeap = new DescriptorHeapX();
        public IDXGISwapChain3 swapChain;
        public List<ID3D12CommandAllocator> commandAllocators;
        public EventWaitHandle waitHandle;
        public ID3D12Fence fence;

        public Queue<ResourceDelayDestroy> delayDestroy = new Queue<ResourceDelayDestroy>();

        public int executeIndex = 0;
        public ulong executeCount = 3;//greater equal than 'bufferCount'

        public int width;
        public int height;
        public IntPtr hwnd;

        public Format swapChainFormat = Format.R8G8B8A8_UNorm;
        public List<ID3D12Resource> screenResources;
        public int bufferCount = 3;
        public int CBVSRVUAVDescriptorCount = 65536;

        public static void ThrowIfFailed(SharpGen.Runtime.Result hr)
        {
            if (hr != SharpGen.Runtime.Result.Ok)
            {
                throw new NotImplementedException(hr.ToString());
            }
        }

        public void Dispose()
        {
            WaitForGpu();
            while (delayDestroy.Count > 0)
            {
                var p = delayDestroy.Dequeue();
                p.resource?.Dispose();
            }
            foreach (var commandAllocator in commandAllocators)
                commandAllocator.Dispose();
            if (screenResources != null)
                foreach (var screenResource in screenResources)
                    screenResource.Dispose();
            dxgiFactory?.Dispose();
            commandQueue?.Dispose();
            cbvsrvuavHeap?.Dispose();
            dsvHeap?.Dispose();
            rtvHeap?.Dispose();
            swapChain?.Dispose();
            fence?.Dispose();
            device?.Dispose();
            adapter?.Dispose();
        }
    }
}
