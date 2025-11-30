using System.Numerics;

namespace MouseEffects.Core.Rendering;

/// <summary>GPU buffer resource.</summary>
public interface IBuffer : IDisposable
{
    int Size { get; }
    BufferType Type { get; }
}

/// <summary>GPU texture resource.</summary>
public interface ITexture : IDisposable
{
    int Width { get; }
    int Height { get; }
    TextureFormat Format { get; }
}

/// <summary>Compiled shader.</summary>
public interface IShader : IDisposable
{
    ShaderStage Stage { get; }
}

/// <summary>Sampler state.</summary>
public interface ISamplerState : IDisposable
{
}

/// <summary>Render target.</summary>
public interface IRenderTarget : IDisposable
{
    int Width { get; }
    int Height { get; }
    ITexture Texture { get; }
}

/// <summary>Buffer types.</summary>
public enum BufferType
{
    Vertex,
    Index,
    Constant,
    Structured
}

/// <summary>Texture formats.</summary>
public enum TextureFormat
{
    R8G8B8A8_UNorm,
    R8G8B8A8_UNorm_SRgb,
    B8G8R8A8_UNorm,
    R32_Float,
    R32G32_Float,
    R32G32B32A32_Float,
    R16G16B16A16_Float
}

/// <summary>Buffer description.</summary>
public record struct BufferDescription
{
    public int Size { get; init; }
    public BufferType Type { get; init; }
    public bool Dynamic { get; init; }
    public int StructureStride { get; init; }
}

/// <summary>Texture description.</summary>
public record struct TextureDescription
{
    public int Width { get; init; }
    public int Height { get; init; }
    public TextureFormat Format { get; init; }
    public bool RenderTarget { get; init; }
    public bool ShaderResource { get; init; }
    public bool UnorderedAccess { get; init; }
}

/// <summary>Sampler description.</summary>
public record struct SamplerDescription
{
    public SamplerFilter Filter { get; init; }
    public SamplerAddressMode AddressU { get; init; }
    public SamplerAddressMode AddressV { get; init; }

    public static SamplerDescription LinearClamp => new()
    {
        Filter = SamplerFilter.Linear,
        AddressU = SamplerAddressMode.Clamp,
        AddressV = SamplerAddressMode.Clamp
    };

    public static SamplerDescription LinearWrap => new()
    {
        Filter = SamplerFilter.Linear,
        AddressU = SamplerAddressMode.Wrap,
        AddressV = SamplerAddressMode.Wrap
    };

    public static SamplerDescription PointClamp => new()
    {
        Filter = SamplerFilter.Point,
        AddressU = SamplerAddressMode.Clamp,
        AddressV = SamplerAddressMode.Clamp
    };
}

/// <summary>Sampler filter modes.</summary>
public enum SamplerFilter
{
    Point,
    Linear,
    Anisotropic
}

/// <summary>Sampler address modes.</summary>
public enum SamplerAddressMode
{
    Wrap,
    Clamp,
    Mirror,
    Border
}
