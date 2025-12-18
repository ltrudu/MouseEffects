using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Kaleidoscope;

/// <summary>
/// Kaleidoscope effect that creates real-time kaleidoscopic mirroring of the screen around the mouse cursor.
/// Creates radial symmetry by dividing the screen into segments and mirroring content.
/// </summary>
public sealed class KaleidoscopeEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "kaleidoscope",
        Name = "Kaleidoscope",
        Description = "Creates real-time kaleidoscopic mirroring of the screen around the mouse cursor with radial symmetry",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Artistic
    };

    // GPU resources
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _paramsBuffer;
    private ISamplerState? _linearSampler;

    // Effect parameters
    private float _radius = 300.0f;
    private int _segments = 8;
    private float _rotationSpeed = 0.5f;
    private float _rotationOffset = 0.0f;
    private float _edgeSoftness = 0.2f;
    private float _zoomFactor = 1.0f;
    private Vector2 _mousePosition;
    private float _totalTime;

    public override EffectMetadata Metadata => _metadata;

    /// <summary>
    /// Kaleidoscope requires continuous screen capture to mirror screen content.
    /// </summary>
    public override bool RequiresContinuousScreenCapture => true;

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("KaleidoscopeShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        var paramsDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<KaleidoscopeParams>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _paramsBuffer = context.CreateBuffer(paramsDesc);

        // Create linear sampler for texture sampling
        _linearSampler = context.CreateSamplerState(SamplerDescription.LinearClamp);
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("radius", out float radius))
            _radius = radius;

        if (Configuration.TryGet("segments", out int segments))
            _segments = segments;

        if (Configuration.TryGet("rotationSpeed", out float rotationSpeed))
            _rotationSpeed = rotationSpeed;

        if (Configuration.TryGet("rotationOffset", out float rotationOffset))
            _rotationOffset = rotationOffset;

        if (Configuration.TryGet("edgeSoftness", out float edgeSoftness))
            _edgeSoftness = edgeSoftness;

        if (Configuration.TryGet("zoomFactor", out float zoomFactor))
            _zoomFactor = zoomFactor;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        _mousePosition = mouseState.Position;
        _totalTime = (float)gameTime.TotalTime.TotalSeconds;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null) return;

        // Get the screen texture from context
        var screenTexture = context.ScreenTexture;
        if (screenTexture == null) return;

        // Update parameters
        var kaleidoscopeParams = new KaleidoscopeParams
        {
            MousePosition = _mousePosition,
            ViewportSize = context.ViewportSize,
            Radius = _radius,
            Segments = _segments,
            RotationSpeed = _rotationSpeed,
            RotationOffset = _rotationOffset,
            EdgeSoftness = _edgeSoftness,
            ZoomFactor = _zoomFactor,
            Time = _totalTime,
            HdrMultiplier = context.HdrPeakBrightness
        };

        context.UpdateBuffer(_paramsBuffer!, kaleidoscopeParams);

        // Set shaders
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _paramsBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);
        context.SetSampler(ShaderStage.Pixel, 0, _linearSampler!);

        // Use alpha blending to composite the effect
        context.SetBlendState(BlendMode.Alpha);

        // Draw fullscreen quad (vertices generated procedurally in shader)
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Unbind screen texture
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);

        // Restore blend state
        context.SetBlendState(BlendMode.Opaque);
    }

    protected override void OnViewportSizeChanged(Vector2 newSize)
    {
        // No texture recreation needed - we use the screen capture
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _paramsBuffer?.Dispose();
        _linearSampler?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(KaleidoscopeEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.Kaleidoscope.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #region Shader Structures

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct KaleidoscopeParams
    {
        // Must match HLSL cbuffer layout exactly!
        // Total size: 64 bytes (4 * 16), must be multiple of 16 for constant buffers

        public Vector2 MousePosition;      // 8 bytes, offset 0
        public Vector2 ViewportSize;       // 8 bytes, offset 8
        public float Radius;               // 4 bytes, offset 16
        public int Segments;               // 4 bytes, offset 20
        public float RotationSpeed;        // 4 bytes, offset 24
        public float RotationOffset;       // 4 bytes, offset 28
        public float EdgeSoftness;         // 4 bytes, offset 32
        public float ZoomFactor;           // 4 bytes, offset 36
        public float Time;                 // 4 bytes, offset 40
        public float HdrMultiplier;        // 4 bytes, offset 44
        private Vector4 _padding;          // 16 bytes, offset 48
    }

    #endregion
}
