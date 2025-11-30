using Vortice.Direct3D11;
using Vortice.DXGI;
using MouseEffects.Core.Rendering;
using CoreTextureFormat = MouseEffects.Core.Rendering.TextureFormat;
using CoreBufferType = MouseEffects.Core.Rendering.BufferType;

namespace MouseEffects.DirectX.Resources;

/// <summary>D3D11 buffer wrapper.</summary>
public sealed class D3D11Buffer : IBuffer
{
    public ID3D11Buffer Buffer { get; }
    public ID3D11ShaderResourceView? ShaderResourceView { get; }
    public int Size { get; }
    public CoreBufferType Type { get; }

    public D3D11Buffer(ID3D11Buffer buffer, int size, CoreBufferType type, ID3D11ShaderResourceView? srv = null)
    {
        Buffer = buffer;
        Size = size;
        Type = type;
        ShaderResourceView = srv;
    }

    public void Dispose()
    {
        ShaderResourceView?.Dispose();
        Buffer.Dispose();
    }
}

/// <summary>D3D11 texture wrapper.</summary>
public sealed class D3D11Texture : ITexture
{
    public ID3D11Texture2D Texture { get; }
    public ID3D11ShaderResourceView? ShaderResourceView { get; }
    public ID3D11RenderTargetView? RenderTargetView { get; }
    public ID3D11UnorderedAccessView? UnorderedAccessView { get; }
    public int Width { get; }
    public int Height { get; }
    public CoreTextureFormat Format { get; }

    public D3D11Texture(
        ID3D11Texture2D texture,
        int width,
        int height,
        CoreTextureFormat format,
        ID3D11ShaderResourceView? srv = null,
        ID3D11RenderTargetView? rtv = null,
        ID3D11UnorderedAccessView? uav = null)
    {
        Texture = texture;
        Width = width;
        Height = height;
        Format = format;
        ShaderResourceView = srv;
        RenderTargetView = rtv;
        UnorderedAccessView = uav;
    }

    public void Dispose()
    {
        UnorderedAccessView?.Dispose();
        RenderTargetView?.Dispose();
        ShaderResourceView?.Dispose();
        Texture.Dispose();
    }
}

/// <summary>D3D11 shader wrapper.</summary>
public sealed class D3D11Shader : IShader
{
    public ID3D11DeviceChild Shader { get; }
    public byte[] Bytecode { get; }
    public ShaderStage Stage { get; }

    public D3D11Shader(ID3D11DeviceChild shader, byte[] bytecode, ShaderStage stage)
    {
        Shader = shader;
        Bytecode = bytecode;
        Stage = stage;
    }

    public void Dispose()
    {
        Shader.Dispose();
    }
}

/// <summary>D3D11 sampler state wrapper.</summary>
public sealed class D3D11SamplerState : ISamplerState
{
    public ID3D11SamplerState SamplerState { get; }

    public D3D11SamplerState(ID3D11SamplerState samplerState)
    {
        SamplerState = samplerState;
    }

    public void Dispose()
    {
        SamplerState.Dispose();
    }
}

/// <summary>D3D11 render target wrapper.</summary>
public sealed class D3D11RenderTarget : IRenderTarget
{
    public ID3D11RenderTargetView RenderTargetView { get; }
    public D3D11Texture TextureWrapper { get; }
    public int Width { get; }
    public int Height { get; }
    public ITexture Texture => TextureWrapper;

    public D3D11RenderTarget(ID3D11RenderTargetView rtv, D3D11Texture texture, int width, int height)
    {
        RenderTargetView = rtv;
        TextureWrapper = texture;
        Width = width;
        Height = height;
    }

    public void Dispose()
    {
        RenderTargetView.Dispose();
        TextureWrapper.Dispose();
    }
}

/// <summary>Screen capture texture wrapper - wraps external SRV from ScreenCapture.</summary>
public sealed class ScreenCaptureTexture : ITexture
{
    private readonly Vortice.Direct3D11.ID3D11ShaderResourceView _srv;

    public int Width { get; }
    public int Height { get; }
    public CoreTextureFormat Format => CoreTextureFormat.B8G8R8A8_UNorm;
    public ID3D11ShaderResourceView? ShaderResourceView => _srv;
    public ID3D11RenderTargetView? RenderTargetView => null;
    public ID3D11UnorderedAccessView? UnorderedAccessView => null;

    public ScreenCaptureTexture(Vortice.Direct3D11.ID3D11ShaderResourceView srv, int width, int height)
    {
        _srv = srv;
        Width = width;
        Height = height;
    }

    public void Dispose()
    {
        // Don't dispose - owned by ScreenCapture
    }
}

/// <summary>Helper for converting formats.</summary>
public static class FormatConverter
{
    public static Format ToVorticeFormat(CoreTextureFormat format) => format switch
    {
        CoreTextureFormat.R8G8B8A8_UNorm => Format.R8G8B8A8_UNorm,
        CoreTextureFormat.R8G8B8A8_UNorm_SRgb => Format.R8G8B8A8_UNorm_SRgb,
        CoreTextureFormat.B8G8R8A8_UNorm => Format.B8G8R8A8_UNorm,
        CoreTextureFormat.R32_Float => Format.R32_Float,
        CoreTextureFormat.R32G32_Float => Format.R32G32_Float,
        CoreTextureFormat.R32G32B32A32_Float => Format.R32G32B32A32_Float,
        CoreTextureFormat.R16G16B16A16_Float => Format.R16G16B16A16_Float,
        _ => throw new ArgumentException($"Unknown texture format: {format}")
    };

    public static CoreTextureFormat FromVorticeFormat(Format format) => format switch
    {
        Format.R8G8B8A8_UNorm => CoreTextureFormat.R8G8B8A8_UNorm,
        Format.R8G8B8A8_UNorm_SRgb => CoreTextureFormat.R8G8B8A8_UNorm_SRgb,
        Format.B8G8R8A8_UNorm => CoreTextureFormat.B8G8R8A8_UNorm,
        Format.R32_Float => CoreTextureFormat.R32_Float,
        Format.R32G32_Float => CoreTextureFormat.R32G32_Float,
        Format.R32G32B32A32_Float => CoreTextureFormat.R32G32B32A32_Float,
        Format.R16G16B16A16_Float => CoreTextureFormat.R16G16B16A16_Float,
        _ => throw new ArgumentException($"Unknown Vortice format: {format}")
    };
}
