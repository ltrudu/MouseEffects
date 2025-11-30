using System.Numerics;

namespace MouseEffects.Core.Rendering;

/// <summary>
/// Abstraction over the graphics rendering context.
/// </summary>
public interface IRenderContext : IDisposable
{
    /// <summary>Native device handle.</summary>
    nint DeviceHandle { get; }

    /// <summary>Current viewport size.</summary>
    Vector2 ViewportSize { get; }

    /// <summary>
    /// Captured screen texture for effects that need to sample the background.
    /// May be null if screen capture is not available.
    /// </summary>
    ITexture? ScreenTexture { get; }

    /// <summary>
    /// When true, screen capture waits for new frames instead of returning immediately.
    /// Enable this when effects need continuously updated screen content.
    /// </summary>
    bool ContinuousCaptureMode { get; set; }

    /// <summary>Set blend state.</summary>
    void SetBlendState(BlendMode mode);

    /// <summary>Set rasterizer state.</summary>
    void SetRasterizerState(CullMode cullMode, FillMode fillMode);

    /// <summary>Set depth/stencil state.</summary>
    void SetDepthStencilState(bool depthEnabled, bool stencilEnabled);

    /// <summary>Set vertex shader.</summary>
    void SetVertexShader(IShader shader);

    /// <summary>Set pixel shader.</summary>
    void SetPixelShader(IShader shader);

    /// <summary>Set geometry shader.</summary>
    void SetGeometryShader(IShader? shader);

    /// <summary>Set compute shader.</summary>
    void SetComputeShader(IShader shader);

    /// <summary>Set constant buffer.</summary>
    void SetConstantBuffer(ShaderStage stage, int slot, IBuffer buffer);

    /// <summary>Set shader resource (texture). Pass null to unbind.</summary>
    void SetShaderResource(ShaderStage stage, int slot, ITexture? texture);

    /// <summary>Set shader resource (buffer).</summary>
    void SetShaderResource(ShaderStage stage, int slot, IBuffer buffer);

    /// <summary>Set unordered access view. Pass null to unbind.</summary>
    void SetUnorderedAccessView(int slot, ITexture? texture);

    /// <summary>Set sampler state.</summary>
    void SetSampler(ShaderStage stage, int slot, ISamplerState sampler);

    /// <summary>Set vertex buffer.</summary>
    void SetVertexBuffer(IBuffer buffer, int stride);

    /// <summary>Set index buffer.</summary>
    void SetIndexBuffer(IBuffer buffer, IndexFormat format);

    /// <summary>Set primitive topology.</summary>
    void SetPrimitiveTopology(PrimitiveTopology topology);

    /// <summary>Draw vertices.</summary>
    void Draw(int vertexCount, int startVertex = 0);

    /// <summary>Draw indexed vertices.</summary>
    void DrawIndexed(int indexCount, int startIndex = 0, int baseVertex = 0);

    /// <summary>Draw instanced vertices.</summary>
    void DrawInstanced(int vertexCountPerInstance, int instanceCount, int startVertex = 0, int startInstance = 0);

    /// <summary>Dispatch compute shader.</summary>
    void Dispatch(int threadGroupCountX, int threadGroupCountY, int threadGroupCountZ);

    /// <summary>Create a buffer.</summary>
    IBuffer CreateBuffer(BufferDescription description, ReadOnlySpan<byte> initialData = default);

    /// <summary>Create a texture.</summary>
    ITexture CreateTexture(TextureDescription description, ReadOnlySpan<byte> initialData = default);

    /// <summary>Compile a shader from source.</summary>
    IShader CompileShader(string source, string entryPoint, ShaderStage stage);

    /// <summary>Create a sampler state.</summary>
    ISamplerState CreateSamplerState(SamplerDescription description);

    /// <summary>Update buffer data.</summary>
    void UpdateBuffer<T>(IBuffer buffer, T data) where T : unmanaged;

    /// <summary>Update buffer data from span.</summary>
    void UpdateBuffer<T>(IBuffer buffer, ReadOnlySpan<T> data) where T : unmanaged;

    /// <summary>Clear render target.</summary>
    void ClearRenderTarget(Vector4 color);

    /// <summary>Set render target.</summary>
    void SetRenderTarget(IRenderTarget target);
}

/// <summary>Blend modes.</summary>
public enum BlendMode
{
    Opaque,
    Alpha,
    Additive,
    Multiply
}

/// <summary>Cull modes.</summary>
public enum CullMode
{
    None,
    Front,
    Back
}

/// <summary>Fill modes.</summary>
public enum FillMode
{
    Solid,
    Wireframe
}

/// <summary>Shader stages.</summary>
public enum ShaderStage
{
    Vertex,
    Pixel,
    Geometry,
    Compute
}

/// <summary>Index buffer formats.</summary>
public enum IndexFormat
{
    UInt16,
    UInt32
}

/// <summary>Primitive topologies.</summary>
public enum PrimitiveTopology
{
    PointList,
    LineList,
    LineStrip,
    TriangleList,
    TriangleStrip
}
