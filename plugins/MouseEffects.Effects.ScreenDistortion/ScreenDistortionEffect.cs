using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.ScreenDistortion;

/// <summary>
/// Screen distortion effect that creates a lens/ripple distortion around the mouse cursor.
/// Captures the screen and applies distortion shader in real-time.
/// </summary>
public sealed class ScreenDistortionEffect : EffectBase
{
    private const float DefaultDistortionRadius = 150.0f;
    private const float DefaultDistortionStrength = 0.3f;
    private const float DefaultRippleFrequency = 8.0f;
    private const float DefaultRippleSpeed = 3.0f;
    private const float DefaultWaveHeight = 0.5f;
    private const float DefaultWaveWidth = 1.0f;
    private const bool DefaultEnableChromatic = true;
    private const bool DefaultEnableGlow = true;
    private const float DefaultGlowIntensity = 0.2f;
    private const bool DefaultEnableWireframe = false;
    private const float DefaultWireframeSpacing = 30.0f;
    private const float DefaultWireframeThickness = 1.5f;

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "screen-distortion",
        Name = "Screen Distortion",
        Description = "Creates a lens/ripple distortion effect around the mouse cursor by distorting the screen content",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    // GPU resources
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _paramsBuffer;
    private ISamplerState? _linearSampler;

    // Effect parameters
    private float _distortionRadius = DefaultDistortionRadius;
    private float _distortionStrength = DefaultDistortionStrength;
    private float _rippleFrequency = DefaultRippleFrequency;
    private float _rippleSpeed = DefaultRippleSpeed;
    private float _waveHeight = DefaultWaveHeight;
    private float _waveWidth = DefaultWaveWidth;
    private bool _enableChromatic = DefaultEnableChromatic;
    private bool _enableGlow = DefaultEnableGlow;
    private float _glowIntensity = DefaultGlowIntensity;
    private Vector4 _glowColor = new(0.3f, 0.5f, 1.0f, 1.0f); // Default blue
    private bool _enableWireframe = DefaultEnableWireframe;
    private float _wireframeSpacing = DefaultWireframeSpacing;
    private float _wireframeThickness = DefaultWireframeThickness;
    private Vector4 _wireframeColor = new(0.0f, 1.0f, 0.5f, 0.8f); // Default cyan/green
    private float _time;
    private Vector2 _mousePosition;

    public override EffectMetadata Metadata => _metadata;

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("ScreenDistortion.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        var paramsDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<DistortionParams>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _paramsBuffer = context.CreateBuffer(paramsDesc);

        // Create linear sampler for texture sampling
        _linearSampler = context.CreateSamplerState(SamplerDescription.LinearClamp);
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("distortionRadius", out float radius))
            _distortionRadius = radius;

        if (Configuration.TryGet("distortionStrength", out float strength))
            _distortionStrength = strength;

        if (Configuration.TryGet("rippleFrequency", out float frequency))
            _rippleFrequency = frequency;

        if (Configuration.TryGet("rippleSpeed", out float speed))
            _rippleSpeed = speed;

        if (Configuration.TryGet("waveHeight", out float waveHeight))
            _waveHeight = waveHeight;

        if (Configuration.TryGet("waveWidth", out float waveWidth))
            _waveWidth = waveWidth;

        if (Configuration.TryGet("enableChromatic", out bool chromatic))
            _enableChromatic = chromatic;

        if (Configuration.TryGet("enableGlow", out bool glow))
            _enableGlow = glow;

        if (Configuration.TryGet("glowIntensity", out float intensity))
            _glowIntensity = intensity;

        if (Configuration.TryGet("glowColor", out Vector4 color))
            _glowColor = color;

        if (Configuration.TryGet("enableWireframe", out bool wireframe))
            _enableWireframe = wireframe;

        if (Configuration.TryGet("wireframeSpacing", out float spacing))
            _wireframeSpacing = spacing;

        if (Configuration.TryGet("wireframeThickness", out float thickness))
            _wireframeThickness = thickness;

        if (Configuration.TryGet("wireframeColor", out Vector4 wireColor))
            _wireframeColor = wireColor;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        _time += (float)gameTime.DeltaTime.TotalSeconds;
        _mousePosition = mouseState.Position;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null) return;

        // Get the screen texture from context
        var screenTexture = context.ScreenTexture;
        if (screenTexture == null)
        {
            // Screen capture not available - render debug indicator
            RenderDebugIndicator(context);
            return;
        }

        // Update parameters
        var distortParams = new DistortionParams
        {
            MousePosition = _mousePosition,
            ViewportSize = context.ViewportSize,
            DistortionRadius = _distortionRadius,
            DistortionStrength = _distortionStrength,
            RippleFrequency = _rippleFrequency,
            RippleSpeed = _rippleSpeed,
            Time = _time,
            WaveHeight = _waveHeight,
            WaveWidth = _waveWidth,
            EnableChromatic = _enableChromatic ? 1.0f : 0.0f,
            EnableGlow = _enableGlow ? 1.0f : 0.0f,
            GlowIntensity = _glowIntensity,
            GlowColor = _glowColor,
            EnableWireframe = _enableWireframe ? 1.0f : 0.0f,
            WireframeSpacing = _wireframeSpacing,
            WireframeThickness = _wireframeThickness,
            WireframePadding = 0,
            WireframeColor = _wireframeColor
        };

        context.UpdateBuffer(_paramsBuffer!, distortParams);

        // Set shaders
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _paramsBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);
        context.SetSampler(ShaderStage.Pixel, 0, _linearSampler!);

        // Enable alpha blending
        context.SetBlendState(BlendMode.Alpha);

        // Draw fullscreen quad (vertices generated procedurally in shader)
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Unbind screen texture
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);

        // Restore blend state
        context.SetBlendState(BlendMode.Opaque);
    }

    private void RenderDebugIndicator(IRenderContext context)
    {
        // Could render a simple indicator that screen capture isn't available
        // For now, just skip rendering
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
        var assembly = typeof(ScreenDistortionEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.ScreenDistortion.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #region Shader Structures

    [StructLayout(LayoutKind.Sequential, Size = 112)]
    private struct DistortionParams
    {
        // Must match HLSL cbuffer layout exactly!
        // HLSL float4 requires 16-byte alignment
        // Total size: 112 bytes (7 * 16), must be multiple of 16 for constant buffers

        public Vector2 MousePosition;      // 8 bytes, offset 0
        public Vector2 ViewportSize;       // 8 bytes, offset 8
        public float DistortionRadius;     // 4 bytes, offset 16
        public float DistortionStrength;   // 4 bytes, offset 20
        public float RippleFrequency;      // 4 bytes, offset 24
        public float RippleSpeed;          // 4 bytes, offset 28
        public float Time;                 // 4 bytes, offset 32
        public float WaveHeight;           // 4 bytes, offset 36
        public float WaveWidth;            // 4 bytes, offset 40
        public float EnableChromatic;      // 4 bytes, offset 44
        public float EnableGlow;           // 4 bytes, offset 48
        public float GlowIntensity;        // 4 bytes, offset 52
        private float _padding1;           // 4 bytes, offset 56 (padding for float4 alignment)
        private float _padding2;           // 4 bytes, offset 60 (padding for float4 alignment)
        public Vector4 GlowColor;          // 16 bytes, offset 64 (must be 16-byte aligned)
        public float EnableWireframe;      // 4 bytes, offset 80
        public float WireframeSpacing;     // 4 bytes, offset 84
        public float WireframeThickness;   // 4 bytes, offset 88
        public float WireframePadding;     // 4 bytes, offset 92
        public Vector4 WireframeColor;     // 16 bytes, offset 96 (already 16-byte aligned)
    }

    #endregion
}
