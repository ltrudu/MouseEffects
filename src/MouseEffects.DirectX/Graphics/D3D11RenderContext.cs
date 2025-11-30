using System.Numerics;
using System.Runtime.InteropServices;
using Vortice.D3DCompiler;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using MouseEffects.Core.Rendering;
using MouseEffects.DirectX.Resources;
using MouseEffects.DirectX.Capture;
using CoreBlendMode = MouseEffects.Core.Rendering.BlendMode;
using CoreCullMode = MouseEffects.Core.Rendering.CullMode;
using CoreFillMode = MouseEffects.Core.Rendering.FillMode;
using CoreShaderStage = MouseEffects.Core.Rendering.ShaderStage;
using CoreIndexFormat = MouseEffects.Core.Rendering.IndexFormat;
using CorePrimitiveTopology = MouseEffects.Core.Rendering.PrimitiveTopology;
using CoreBufferDescription = MouseEffects.Core.Rendering.BufferDescription;
using CoreSamplerDescription = MouseEffects.Core.Rendering.SamplerDescription;

namespace MouseEffects.DirectX.Graphics;

/// <summary>
/// Direct3D 11 implementation of IRenderContext.
/// </summary>
public sealed class D3D11RenderContext : IRenderContext
{
    private readonly D3D11GraphicsDevice _graphicsDevice;
    private readonly Dictionary<CoreBlendMode, ID3D11BlendState> _blendStates = new();
    private readonly Dictionary<(CoreCullMode, CoreFillMode), ID3D11RasterizerState> _rasterizerStates = new();
    private readonly Dictionary<(bool, bool), ID3D11DepthStencilState> _depthStencilStates = new();
    private readonly ScreenCapture _screenCapture;
    private ScreenCaptureTexture? _screenTexture;
    private bool _disposed;

    public nint DeviceHandle => _graphicsDevice.Device.NativePointer;
    public Vector2 ViewportSize { get; private set; }
    public ITexture? ScreenTexture => _screenTexture;

    public ID3D11Device Device => _graphicsDevice.Device;
    public ID3D11DeviceContext Context => _graphicsDevice.Context;

    public D3D11RenderContext(D3D11GraphicsDevice graphicsDevice, int viewportWidth, int viewportHeight)
    {
        _graphicsDevice = graphicsDevice;
        ViewportSize = new Vector2(viewportWidth, viewportHeight);
        _screenCapture = new ScreenCapture(graphicsDevice);
        InitializeCommonStates();
        InitializeScreenCapture();
    }

    private void InitializeScreenCapture()
    {
        if (_screenCapture.Initialize())
        {
            if (_screenCapture.ShaderResourceView != null)
            {
                _screenTexture = new ScreenCaptureTexture(
                    _screenCapture.ShaderResourceView,
                    _screenCapture.Width,
                    _screenCapture.Height);
            }
        }
    }

    /// <summary>
    /// When true, screen capture waits for new frames instead of returning immediately.
    /// Enable this when effects need continuously updated screen content.
    /// </summary>
    public bool ContinuousCaptureMode
    {
        get => _screenCapture.ContinuousCaptureMode;
        set => _screenCapture.ContinuousCaptureMode = value;
    }

    /// <summary>
    /// Capture the current screen frame. Call this before rendering effects that need screen data.
    /// </summary>
    public void CaptureScreen()
    {
        if (_screenCapture.IsAvailable)
        {
            _screenCapture.CaptureFrame();
        }
    }

    public void UpdateViewportSize(int width, int height)
    {
        ViewportSize = new Vector2(width, height);
    }

    private void InitializeCommonStates()
    {
        // Blend states
        _blendStates[CoreBlendMode.Opaque] = CreateBlendState(false, Blend.One, Blend.Zero);
        _blendStates[CoreBlendMode.Alpha] = CreateBlendState(true, Blend.SourceAlpha, Blend.InverseSourceAlpha);
        _blendStates[CoreBlendMode.Additive] = CreateBlendState(true, Blend.SourceAlpha, Blend.One);
        _blendStates[CoreBlendMode.Multiply] = CreateBlendState(true, Blend.DestinationColor, Blend.Zero);

        // Rasterizer states
        foreach (CoreCullMode cull in Enum.GetValues<CoreCullMode>())
        foreach (CoreFillMode fill in Enum.GetValues<CoreFillMode>())
        {
            _rasterizerStates[(cull, fill)] = CreateRasterizerState(cull, fill);
        }

        // Depth stencil states
        _depthStencilStates[(false, false)] = CreateDepthStencilState(false, false);
        _depthStencilStates[(true, false)] = CreateDepthStencilState(true, false);
        _depthStencilStates[(false, true)] = CreateDepthStencilState(false, true);
        _depthStencilStates[(true, true)] = CreateDepthStencilState(true, true);
    }

    private ID3D11BlendState CreateBlendState(bool blendEnable, Blend srcBlend, Blend destBlend)
    {
        var desc = new BlendDescription
        {
            AlphaToCoverageEnable = false,
            IndependentBlendEnable = false
        };
        desc.RenderTarget[0] = new RenderTargetBlendDescription
        {
            BlendEnable = blendEnable,
            SourceBlend = srcBlend,
            DestinationBlend = destBlend,
            BlendOperation = BlendOperation.Add,
            SourceBlendAlpha = Blend.One,
            DestinationBlendAlpha = Blend.InverseSourceAlpha,
            BlendOperationAlpha = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteEnable.All
        };
        return Device.CreateBlendState(desc);
    }

    private ID3D11RasterizerState CreateRasterizerState(CoreCullMode cullMode, CoreFillMode fillMode)
    {
        var desc = new RasterizerDescription
        {
            FillMode = fillMode == CoreFillMode.Solid ? Vortice.Direct3D11.FillMode.Solid : Vortice.Direct3D11.FillMode.Wireframe,
            CullMode = cullMode switch
            {
                CoreCullMode.None => Vortice.Direct3D11.CullMode.None,
                CoreCullMode.Front => Vortice.Direct3D11.CullMode.Front,
                CoreCullMode.Back => Vortice.Direct3D11.CullMode.Back,
                _ => Vortice.Direct3D11.CullMode.None
            },
            FrontCounterClockwise = false,
            DepthBias = 0,
            DepthBiasClamp = 0,
            SlopeScaledDepthBias = 0,
            DepthClipEnable = true,
            ScissorEnable = false,
            MultisampleEnable = false,
            AntialiasedLineEnable = false
        };
        return Device.CreateRasterizerState(desc);
    }

    private ID3D11DepthStencilState CreateDepthStencilState(bool depthEnable, bool stencilEnable)
    {
        var stencilOp = new DepthStencilOperationDescription
        {
            StencilFailOp = StencilOperation.Keep,
            StencilDepthFailOp = StencilOperation.Keep,
            StencilPassOp = StencilOperation.Keep,
            StencilFunc = ComparisonFunction.Always
        };

        var desc = new DepthStencilDescription
        {
            DepthEnable = depthEnable,
            DepthWriteMask = depthEnable ? DepthWriteMask.All : DepthWriteMask.Zero,
            DepthFunc = ComparisonFunction.Less,
            StencilEnable = stencilEnable,
            StencilReadMask = 0xFF,
            StencilWriteMask = 0xFF,
            FrontFace = stencilOp,
            BackFace = stencilOp
        };
        return Device.CreateDepthStencilState(desc);
    }

    public void SetBlendState(CoreBlendMode mode)
    {
        Context.OMSetBlendState(_blendStates[mode]);
    }

    public void SetRasterizerState(CoreCullMode cullMode, CoreFillMode fillMode)
    {
        Context.RSSetState(_rasterizerStates[(cullMode, fillMode)]);
    }

    public void SetDepthStencilState(bool depthEnabled, bool stencilEnabled)
    {
        Context.OMSetDepthStencilState(_depthStencilStates[(depthEnabled, stencilEnabled)]);
    }

    public void SetVertexShader(IShader shader)
    {
        var d3dShader = (D3D11Shader)shader;
        Context.VSSetShader((ID3D11VertexShader)d3dShader.Shader);
    }

    public void SetPixelShader(IShader shader)
    {
        var d3dShader = (D3D11Shader)shader;
        Context.PSSetShader((ID3D11PixelShader)d3dShader.Shader);
    }

    public void SetGeometryShader(IShader? shader)
    {
        if (shader == null)
        {
            Context.GSSetShader(null);
            return;
        }
        var d3dShader = (D3D11Shader)shader;
        Context.GSSetShader((ID3D11GeometryShader)d3dShader.Shader);
    }

    public void SetComputeShader(IShader shader)
    {
        var d3dShader = (D3D11Shader)shader;
        Context.CSSetShader((ID3D11ComputeShader)d3dShader.Shader);
    }

    public void SetConstantBuffer(CoreShaderStage stage, int slot, IBuffer buffer)
    {
        var d3dBuffer = (D3D11Buffer)buffer;
        switch (stage)
        {
            case CoreShaderStage.Vertex:
                Context.VSSetConstantBuffer((uint)slot, d3dBuffer.Buffer);
                break;
            case CoreShaderStage.Pixel:
                Context.PSSetConstantBuffer((uint)slot, d3dBuffer.Buffer);
                break;
            case CoreShaderStage.Geometry:
                Context.GSSetConstantBuffer((uint)slot, d3dBuffer.Buffer);
                break;
            case CoreShaderStage.Compute:
                Context.CSSetConstantBuffer((uint)slot, d3dBuffer.Buffer);
                break;
        }
    }

    public void SetShaderResource(CoreShaderStage stage, int slot, ITexture? texture)
    {
        if (texture == null)
        {
            SetShaderResourceView(stage, slot, null);
        }
        else if (texture is D3D11Texture d3dTexture)
        {
            SetShaderResourceView(stage, slot, d3dTexture.ShaderResourceView);
        }
        else if (texture is ScreenCaptureTexture screenTexture)
        {
            SetShaderResourceView(stage, slot, screenTexture.ShaderResourceView);
        }
        else
        {
            throw new ArgumentException($"Unsupported texture type: {texture.GetType().Name}", nameof(texture));
        }
    }

    public void SetShaderResource(CoreShaderStage stage, int slot, IBuffer buffer)
    {
        var d3dBuffer = (D3D11Buffer)buffer;
        SetShaderResourceView(stage, slot, d3dBuffer.ShaderResourceView);
    }

    private void SetShaderResourceView(CoreShaderStage stage, int slot, ID3D11ShaderResourceView? srv)
    {
        switch (stage)
        {
            case CoreShaderStage.Vertex:
                Context.VSSetShaderResource((uint)slot, srv);
                break;
            case CoreShaderStage.Pixel:
                Context.PSSetShaderResource((uint)slot, srv);
                break;
            case CoreShaderStage.Geometry:
                Context.GSSetShaderResource((uint)slot, srv);
                break;
            case CoreShaderStage.Compute:
                Context.CSSetShaderResource((uint)slot, srv);
                break;
        }
    }

    public void SetUnorderedAccessView(int slot, ITexture? texture)
    {
        if (texture == null)
        {
            Context.CSSetUnorderedAccessView((uint)slot, null);
        }
        else
        {
            var d3dTexture = (D3D11Texture)texture;
            Context.CSSetUnorderedAccessView((uint)slot, d3dTexture.UnorderedAccessView);
        }
    }

    public void SetSampler(CoreShaderStage stage, int slot, ISamplerState sampler)
    {
        var d3dSampler = (D3D11SamplerState)sampler;
        switch (stage)
        {
            case CoreShaderStage.Vertex:
                Context.VSSetSampler((uint)slot, d3dSampler.SamplerState);
                break;
            case CoreShaderStage.Pixel:
                Context.PSSetSampler((uint)slot, d3dSampler.SamplerState);
                break;
            case CoreShaderStage.Geometry:
                Context.GSSetSampler((uint)slot, d3dSampler.SamplerState);
                break;
            case CoreShaderStage.Compute:
                Context.CSSetSampler((uint)slot, d3dSampler.SamplerState);
                break;
        }
    }

    public void SetVertexBuffer(IBuffer buffer, int stride)
    {
        var d3dBuffer = (D3D11Buffer)buffer;
        Context.IASetVertexBuffer(0, d3dBuffer.Buffer, (uint)stride);
    }

    public void SetIndexBuffer(IBuffer buffer, CoreIndexFormat format)
    {
        var d3dBuffer = (D3D11Buffer)buffer;
        var dxgiFormat = format == CoreIndexFormat.UInt16 ? Format.R16_UInt : Format.R32_UInt;
        Context.IASetIndexBuffer(d3dBuffer.Buffer, dxgiFormat, 0);
    }

    public void SetPrimitiveTopology(CorePrimitiveTopology topology)
    {
        Vortice.Direct3D.PrimitiveTopology d3dTopology = topology switch
        {
            CorePrimitiveTopology.PointList => Vortice.Direct3D.PrimitiveTopology.PointList,
            CorePrimitiveTopology.LineList => Vortice.Direct3D.PrimitiveTopology.LineList,
            CorePrimitiveTopology.LineStrip => Vortice.Direct3D.PrimitiveTopology.LineStrip,
            CorePrimitiveTopology.TriangleList => Vortice.Direct3D.PrimitiveTopology.TriangleList,
            CorePrimitiveTopology.TriangleStrip => Vortice.Direct3D.PrimitiveTopology.TriangleStrip,
            _ => Vortice.Direct3D.PrimitiveTopology.TriangleList
        };
        Context.IASetPrimitiveTopology(d3dTopology);
    }

    public void Draw(int vertexCount, int startVertex = 0)
    {
        Context.Draw((uint)vertexCount, (uint)startVertex);
    }

    public void DrawIndexed(int indexCount, int startIndex = 0, int baseVertex = 0)
    {
        Context.DrawIndexed((uint)indexCount, (uint)startIndex, baseVertex);
    }

    public void DrawInstanced(int vertexCountPerInstance, int instanceCount, int startVertex = 0, int startInstance = 0)
    {
        Context.DrawInstanced((uint)vertexCountPerInstance, (uint)instanceCount, (uint)startVertex, (uint)startInstance);
    }

    public void Dispatch(int threadGroupCountX, int threadGroupCountY, int threadGroupCountZ)
    {
        Context.Dispatch((uint)threadGroupCountX, (uint)threadGroupCountY, (uint)threadGroupCountZ);
    }

    public IBuffer CreateBuffer(CoreBufferDescription description, ReadOnlySpan<byte> initialData = default)
    {
        var bindFlags = description.Type switch
        {
            BufferType.Vertex => BindFlags.VertexBuffer,
            BufferType.Index => BindFlags.IndexBuffer,
            BufferType.Constant => BindFlags.ConstantBuffer,
            BufferType.Structured => BindFlags.ShaderResource,
            _ => BindFlags.None
        };

        var usage = description.Dynamic ? ResourceUsage.Dynamic : ResourceUsage.Default;
        var cpuAccess = description.Dynamic ? CpuAccessFlags.Write : CpuAccessFlags.None;

        var desc = new Vortice.Direct3D11.BufferDescription
        {
            ByteWidth = (uint)description.Size,
            Usage = usage,
            BindFlags = bindFlags,
            CPUAccessFlags = cpuAccess,
            MiscFlags = description.Type == BufferType.Structured ? ResourceOptionFlags.BufferStructured : ResourceOptionFlags.None,
            StructureByteStride = (uint)description.StructureStride
        };

        ID3D11Buffer buffer;
        if (initialData.IsEmpty)
        {
            buffer = Device.CreateBuffer(desc);
        }
        else
        {
            unsafe
            {
                fixed (byte* dataPtr = initialData)
                {
                    var subresource = new SubresourceData((nint)dataPtr);
                    buffer = Device.CreateBuffer(desc, subresource);
                }
            }
        }

        ID3D11ShaderResourceView? srv = null;
        if (description.Type == BufferType.Structured)
        {
            var srvDesc = new ShaderResourceViewDescription
            {
                Format = Format.Unknown,
                ViewDimension = ShaderResourceViewDimension.Buffer
            };
            srvDesc.Buffer.FirstElement = 0;
            srvDesc.Buffer.NumElements = (uint)(description.Size / description.StructureStride);
            srv = Device.CreateShaderResourceView(buffer, srvDesc);
        }

        return new D3D11Buffer(buffer, description.Size, description.Type, srv);
    }

    public ITexture CreateTexture(TextureDescription description, ReadOnlySpan<byte> initialData = default)
    {
        var format = FormatConverter.ToVorticeFormat(description.Format);

        var bindFlags = BindFlags.None;
        if (description.ShaderResource) bindFlags |= BindFlags.ShaderResource;
        if (description.RenderTarget) bindFlags |= BindFlags.RenderTarget;
        if (description.UnorderedAccess) bindFlags |= BindFlags.UnorderedAccess;

        var desc = new Texture2DDescription
        {
            Width = (uint)description.Width,
            Height = (uint)description.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = format,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = bindFlags,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None
        };

        ID3D11Texture2D texture;
        if (initialData.IsEmpty)
        {
            texture = Device.CreateTexture2D(desc);
        }
        else
        {
            int rowPitch = description.Width * GetBytesPerPixel(description.Format);
            unsafe
            {
                fixed (byte* dataPtr = initialData)
                {
                    var subresource = new SubresourceData((nint)dataPtr, (uint)rowPitch);
                    texture = Device.CreateTexture2D(desc, new[] { subresource });
                }
            }
        }

        ID3D11ShaderResourceView? srv = null;
        if (description.ShaderResource)
        {
            srv = Device.CreateShaderResourceView(texture);
        }

        ID3D11RenderTargetView? rtv = null;
        if (description.RenderTarget)
        {
            rtv = Device.CreateRenderTargetView(texture);
        }

        ID3D11UnorderedAccessView? uav = null;
        if (description.UnorderedAccess)
        {
            uav = Device.CreateUnorderedAccessView(texture);
        }

        return new D3D11Texture(texture, description.Width, description.Height, description.Format, srv, rtv, uav);
    }

    private static int GetBytesPerPixel(TextureFormat format) => format switch
    {
        TextureFormat.R8G8B8A8_UNorm or TextureFormat.R8G8B8A8_UNorm_SRgb or TextureFormat.B8G8R8A8_UNorm => 4,
        TextureFormat.R32_Float => 4,
        TextureFormat.R32G32_Float => 8,
        TextureFormat.R32G32B32A32_Float => 16,
        TextureFormat.R16G16B16A16_Float => 8,
        _ => 4
    };

    public IShader CompileShader(string source, string entryPoint, CoreShaderStage stage)
    {
        var target = stage switch
        {
            CoreShaderStage.Vertex => "vs_5_0",
            CoreShaderStage.Pixel => "ps_5_0",
            CoreShaderStage.Geometry => "gs_5_0",
            CoreShaderStage.Compute => "cs_5_0",
            _ => throw new ArgumentException($"Unknown shader stage: {stage}")
        };

        Compiler.Compile(source, entryPoint, "shader", target, out var shaderBlob, out var errorBlob);

        if (shaderBlob == null || shaderBlob.BufferSize == 0)
        {
            var error = errorBlob != null ? System.Text.Encoding.UTF8.GetString(errorBlob.AsSpan()) : "Unknown error";
            throw new InvalidOperationException($"Shader compilation failed: {error}");
        }

        var bytecode = shaderBlob.AsSpan().ToArray();

        ID3D11DeviceChild shader = stage switch
        {
            CoreShaderStage.Vertex => Device.CreateVertexShader(bytecode),
            CoreShaderStage.Pixel => Device.CreatePixelShader(bytecode),
            CoreShaderStage.Geometry => Device.CreateGeometryShader(bytecode),
            CoreShaderStage.Compute => Device.CreateComputeShader(bytecode),
            _ => throw new ArgumentException($"Unknown shader stage: {stage}")
        };

        return new D3D11Shader(shader, bytecode, stage);
    }

    public ISamplerState CreateSamplerState(CoreSamplerDescription description)
    {
        var filter = description.Filter switch
        {
            SamplerFilter.Point => Filter.MinMagMipPoint,
            SamplerFilter.Linear => Filter.MinMagMipLinear,
            SamplerFilter.Anisotropic => Filter.Anisotropic,
            _ => Filter.MinMagMipLinear
        };

        var addressMode = (SamplerAddressMode mode) => mode switch
        {
            SamplerAddressMode.Wrap => TextureAddressMode.Wrap,
            SamplerAddressMode.Clamp => TextureAddressMode.Clamp,
            SamplerAddressMode.Mirror => TextureAddressMode.Mirror,
            SamplerAddressMode.Border => TextureAddressMode.Border,
            _ => TextureAddressMode.Clamp
        };

        var desc = new Vortice.Direct3D11.SamplerDescription
        {
            Filter = filter,
            AddressU = addressMode(description.AddressU),
            AddressV = addressMode(description.AddressV),
            AddressW = TextureAddressMode.Clamp,
            MipLODBias = 0,
            MaxAnisotropy = 16,
                        MinLOD = 0,
            MaxLOD = float.MaxValue
        };

        return new D3D11SamplerState(Device.CreateSamplerState(desc));
    }

    public void UpdateBuffer<T>(IBuffer buffer, T data) where T : unmanaged
    {
        var d3dBuffer = (D3D11Buffer)buffer;
        var mapped = Context.Map(d3dBuffer.Buffer, MapMode.WriteDiscard);
        try
        {
            unsafe
            {
                *(T*)mapped.DataPointer = data;
            }
        }
        finally
        {
            Context.Unmap(d3dBuffer.Buffer);
        }
    }

    public void UpdateBuffer<T>(IBuffer buffer, ReadOnlySpan<T> data) where T : unmanaged
    {
        var d3dBuffer = (D3D11Buffer)buffer;
        var mapped = Context.Map(d3dBuffer.Buffer, MapMode.WriteDiscard);
        try
        {
            unsafe
            {
                fixed (T* dataPtr = data)
                {
                    Buffer.MemoryCopy(dataPtr, (void*)mapped.DataPointer, d3dBuffer.Size, data.Length * sizeof(T));
                }
            }
        }
        finally
        {
            Context.Unmap(d3dBuffer.Buffer);
        }
    }

    public void ClearRenderTarget(Vector4 color)
    {
        // Get current render target and clear it
        var rtvs = new ID3D11RenderTargetView[1];
        Context.OMGetRenderTargets(1, rtvs, out _);
        if (rtvs[0] != null)
        {
            Context.ClearRenderTargetView(rtvs[0], new Color4(color.X, color.Y, color.Z, color.W));
        }
    }

    public void SetRenderTarget(IRenderTarget target)
    {
        var d3dTarget = (D3D11RenderTarget)target;
        Context.OMSetRenderTargets(d3dTarget.RenderTargetView);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _screenCapture.Dispose();

        foreach (var state in _blendStates.Values) state.Dispose();
        foreach (var state in _rasterizerStates.Values) state.Dispose();
        foreach (var state in _depthStencilStates.Values) state.Dispose();

        _blendStates.Clear();
        _rasterizerStates.Clear();
        _depthStencilStates.Clear();
    }
}
