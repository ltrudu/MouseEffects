using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Spotlight;

/// <summary>
/// Spotlight effect that creates dramatic theater lighting centered on the mouse cursor.
/// Darkens the screen except for a bright spotlight area following the cursor.
/// </summary>
public sealed class SpotlightEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "spotlight",
        Name = "Spotlight",
        Description = "Creates a dramatic theater spotlight effect centered on the mouse cursor, darkening everything outside the lit area",
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
    private float _spotlightRadius = 200.0f;
    private float _edgeSoftness = 100.0f;
    private float _darknessLevel = 0.1f;
    private int _colorTemperature = 1; // 0=warm, 1=neutral, 2=cool
    private float _brightnessBoost = 1.2f;
    private bool _dustParticlesEnabled = true;
    private float _dustDensity = 0.5f;
    private Vector2 _mousePosition;
    private float _totalTime;

    public override EffectMetadata Metadata => _metadata;

    /// <summary>
    /// Spotlight requires continuous screen capture to darken screen content.
    /// </summary>
    public override bool RequiresContinuousScreenCapture => true;

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("SpotlightShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        var paramsDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<SpotlightParams>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _paramsBuffer = context.CreateBuffer(paramsDesc);

        // Create linear sampler for texture sampling
        _linearSampler = context.CreateSamplerState(SamplerDescription.LinearClamp);
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("spotlightRadius", out float radius))
            _spotlightRadius = radius;

        if (Configuration.TryGet("edgeSoftness", out float softness))
            _edgeSoftness = softness;

        if (Configuration.TryGet("darknessLevel", out float darkness))
            _darknessLevel = darkness;

        if (Configuration.TryGet("colorTemperature", out int temperature))
            _colorTemperature = temperature;

        if (Configuration.TryGet("brightnessBoost", out float brightness))
            _brightnessBoost = brightness;

        if (Configuration.TryGet("dustParticlesEnabled", out bool dustEnabled))
            _dustParticlesEnabled = dustEnabled;

        if (Configuration.TryGet("dustDensity", out float density))
            _dustDensity = density;
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
        var spotlightParams = new SpotlightParams
        {
            MousePosition = _mousePosition,
            ViewportSize = context.ViewportSize,
            SpotlightRadius = _spotlightRadius,
            EdgeSoftness = _edgeSoftness,
            DarknessLevel = _darknessLevel,
            ColorTemperature = _colorTemperature,
            BrightnessBoost = _brightnessBoost,
            DustParticlesEnabled = _dustParticlesEnabled ? 1.0f : 0.0f,
            DustDensity = _dustDensity,
            Time = _totalTime,
            HdrMultiplier = context.HdrPeakBrightness,
            Padding1 = 0.0f
        };

        context.UpdateBuffer(_paramsBuffer!, spotlightParams);

        // Set shaders
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _paramsBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);
        context.SetSampler(ShaderStage.Pixel, 0, _linearSampler!);

        // Use opaque blending since we're replacing the entire screen
        context.SetBlendState(BlendMode.Opaque);

        // Draw fullscreen quad (vertices generated procedurally in shader)
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Unbind screen texture
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);
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
        var assembly = typeof(SpotlightEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.Spotlight.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #region Shader Structures

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct SpotlightParams
    {
        // Must match HLSL cbuffer layout exactly!
        // Total size: 64 bytes (4 * 16), must be multiple of 16 for constant buffers

        public Vector2 MousePosition;      // 8 bytes, offset 0
        public Vector2 ViewportSize;       // 8 bytes, offset 8
        public float SpotlightRadius;      // 4 bytes, offset 16
        public float EdgeSoftness;         // 4 bytes, offset 20
        public float DarknessLevel;        // 4 bytes, offset 24
        public float BrightnessBoost;      // 4 bytes, offset 28
        public int ColorTemperature;       // 4 bytes, offset 32
        public float DustParticlesEnabled; // 4 bytes, offset 36
        public float DustDensity;          // 4 bytes, offset 40
        public float Time;                 // 4 bytes, offset 44
        public float HdrMultiplier;        // 4 bytes, offset 48
        public float Padding1;             // 4 bytes, offset 52
        private Vector2 _padding2;         // 8 bytes, offset 56
    }

    #endregion
}
