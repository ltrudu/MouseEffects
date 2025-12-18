using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Hologram;

/// <summary>
/// Hologram effect that creates sci-fi holographic projection with scan lines, flickering,
/// and chromatic aberration around the mouse cursor.
/// </summary>
public sealed class HologramEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "hologram",
        Name = "Hologram",
        Description = "Sci-fi holographic projection effect with scan lines and flickering around the mouse cursor",
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
    private float _radius = 250.0f;
    private float _scanLineDensity = 150.0f;
    private float _scanLineSpeed = 2.0f;
    private float _flickerIntensity = 0.15f;
    private int _colorTint = 0; // 0=Cyan, 1=Blue, 2=Green, 3=Purple
    private float _edgeGlowStrength = 0.8f;
    private float _noiseAmount = 0.2f;
    private float _chromaticAberration = 0.008f;
    private float _tintStrength = 0.6f;
    private Vector2 _mousePosition;
    private float _totalTime;

    public override EffectMetadata Metadata => _metadata;

    /// <summary>
    /// Hologram effect requires continuous screen capture to transform screen content.
    /// </summary>
    public override bool RequiresContinuousScreenCapture => true;

    // Public properties for UI binding
    public float Radius
    {
        get => _radius;
        set => _radius = value;
    }

    public float ScanLineDensity
    {
        get => _scanLineDensity;
        set => _scanLineDensity = value;
    }

    public float ScanLineSpeed
    {
        get => _scanLineSpeed;
        set => _scanLineSpeed = value;
    }

    public float FlickerIntensity
    {
        get => _flickerIntensity;
        set => _flickerIntensity = value;
    }

    public int ColorTint
    {
        get => _colorTint;
        set => _colorTint = Math.Clamp(value, 0, 3);
    }

    public float EdgeGlowStrength
    {
        get => _edgeGlowStrength;
        set => _edgeGlowStrength = value;
    }

    public float NoiseAmount
    {
        get => _noiseAmount;
        set => _noiseAmount = value;
    }

    public float ChromaticAberration
    {
        get => _chromaticAberration;
        set => _chromaticAberration = value;
    }

    public float TintStrength
    {
        get => _tintStrength;
        set => _tintStrength = value;
    }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("HologramShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        var paramsDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<HologramParams>(),
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

        if (Configuration.TryGet("scanLineDensity", out float density))
            _scanLineDensity = density;

        if (Configuration.TryGet("scanLineSpeed", out float speed))
            _scanLineSpeed = speed;

        if (Configuration.TryGet("flickerIntensity", out float flicker))
            _flickerIntensity = flicker;

        if (Configuration.TryGet("colorTint", out int tint))
            _colorTint = tint;

        if (Configuration.TryGet("edgeGlowStrength", out float glow))
            _edgeGlowStrength = glow;

        if (Configuration.TryGet("noiseAmount", out float noise))
            _noiseAmount = noise;

        if (Configuration.TryGet("chromaticAberration", out float aberration))
            _chromaticAberration = aberration;

        if (Configuration.TryGet("tintStrength", out float tintStr))
            _tintStrength = tintStr;
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
        var hologramParams = new HologramParams
        {
            MousePosition = _mousePosition,
            ViewportSize = context.ViewportSize,
            Radius = _radius,
            ScanLineDensity = _scanLineDensity,
            ScanLineSpeed = _scanLineSpeed,
            FlickerIntensity = _flickerIntensity,
            ColorTint = _colorTint,
            EdgeGlowStrength = _edgeGlowStrength,
            NoiseAmount = _noiseAmount,
            ChromaticAberration = _chromaticAberration,
            TintStrength = _tintStrength,
            Time = _totalTime,
            HdrMultiplier = context.HdrPeakBrightness,
            Padding = default
        };

        context.UpdateBuffer(_paramsBuffer!, hologramParams);

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
        var assembly = typeof(HologramEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.Hologram.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #region Shader Structures

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct HologramParams
    {
        // Must match HLSL cbuffer layout exactly!
        // Total size: 64 bytes (4 * 16), must be multiple of 16 for constant buffers

        public Vector2 MousePosition;      // 8 bytes, offset 0
        public Vector2 ViewportSize;       // 8 bytes, offset 8
        public float Radius;               // 4 bytes, offset 16
        public float ScanLineDensity;      // 4 bytes, offset 20
        public float ScanLineSpeed;        // 4 bytes, offset 24
        public float FlickerIntensity;     // 4 bytes, offset 28
        public int ColorTint;              // 4 bytes, offset 32
        public float EdgeGlowStrength;     // 4 bytes, offset 36
        public float NoiseAmount;          // 4 bytes, offset 40
        public float ChromaticAberration;  // 4 bytes, offset 44
        public float TintStrength;         // 4 bytes, offset 48
        public float Time;                 // 4 bytes, offset 52
        public float HdrMultiplier;        // 4 bytes, offset 56
        public float Padding;              // 4 bytes, offset 60
    }

    #endregion
}
