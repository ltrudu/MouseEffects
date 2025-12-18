using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Glitch;

/// <summary>
/// Glitch effect that creates digital corruption and distortion artifacts around the mouse cursor.
/// Simulates a broken screen with RGB split, scan lines, block displacement, and noise.
/// </summary>
public sealed class GlitchEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "glitch",
        Name = "Glitch",
        Description = "Creates digital corruption and distortion artifacts around the mouse cursor like a broken screen",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Digital
    };

    // GPU resources
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _paramsBuffer;
    private ISamplerState? _linearSampler;

    // Effect parameters
    private float _radius = 300.0f;
    private float _intensity = 1.0f;
    private float _rgbSplitAmount = 0.015f;
    private float _scanLineFrequency = 8.0f;
    private float _blockSize = 20.0f;
    private float _noiseAmount = 0.3f;
    private float _glitchFrequency = 5.0f;
    private Vector2 _mousePosition;
    private float _totalTime;

    public override EffectMetadata Metadata => _metadata;

    /// <summary>
    /// Glitch effect requires continuous screen capture to distort screen content.
    /// </summary>
    public override bool RequiresContinuousScreenCapture => true;

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("GlitchShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        var paramsDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<GlitchParams>(),
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

        if (Configuration.TryGet("intensity", out float intensity))
            _intensity = intensity;

        if (Configuration.TryGet("rgbSplitAmount", out float rgbSplit))
            _rgbSplitAmount = rgbSplit;

        if (Configuration.TryGet("scanLineFrequency", out float scanLine))
            _scanLineFrequency = scanLine;

        if (Configuration.TryGet("blockSize", out float blockSize))
            _blockSize = blockSize;

        if (Configuration.TryGet("noiseAmount", out float noise))
            _noiseAmount = noise;

        if (Configuration.TryGet("glitchFrequency", out float frequency))
            _glitchFrequency = frequency;
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
        var glitchParams = new GlitchParams
        {
            MousePosition = _mousePosition,
            ViewportSize = context.ViewportSize,
            Radius = _radius,
            Intensity = _intensity,
            RgbSplitAmount = _rgbSplitAmount,
            ScanLineFrequency = _scanLineFrequency,
            BlockSize = _blockSize,
            NoiseAmount = _noiseAmount,
            GlitchFrequency = _glitchFrequency,
            Time = _totalTime,
            HdrMultiplier = context.HdrPeakBrightness,
            Padding = default
        };

        context.UpdateBuffer(_paramsBuffer!, glitchParams);

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
        var assembly = typeof(GlitchEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.Glitch.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #region Shader Structures

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct GlitchParams
    {
        // Must match HLSL cbuffer layout exactly!
        // Total size: 64 bytes (4 * 16), must be multiple of 16 for constant buffers

        public Vector2 MousePosition;      // 8 bytes, offset 0
        public Vector2 ViewportSize;       // 8 bytes, offset 8
        public float Radius;               // 4 bytes, offset 16
        public float Intensity;            // 4 bytes, offset 20
        public float RgbSplitAmount;       // 4 bytes, offset 24
        public float ScanLineFrequency;    // 4 bytes, offset 28
        public float BlockSize;            // 4 bytes, offset 32
        public float NoiseAmount;          // 4 bytes, offset 36
        public float GlitchFrequency;      // 4 bytes, offset 40
        public float Time;                 // 4 bytes, offset 44
        public float HdrMultiplier;        // 4 bytes, offset 48
        public float Padding;              // 4 bytes, offset 52
        public Vector2 Padding2;           // 8 bytes, offset 56
    }

    #endregion
}
