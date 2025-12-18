using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.BlackHole;

/// <summary>
/// Black hole effect that creates gravitational lensing distortion around the mouse cursor.
/// Warps screen content toward/around the cursor creating a realistic black hole appearance.
/// </summary>
public sealed class BlackHoleEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "black-hole",
        Name = "Black Hole",
        Description = "Creates gravitational lensing distortion around the mouse cursor, warping the screen like a real black hole",
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
    private float _radius = 200.0f;
    private float _distortionStrength = 1.0f;
    private float _eventHorizonSize = 0.3f;
    private bool _accretionDiskEnabled = true;
    private Vector4 _accretionDiskColor = new(1.0f, 0.6f, 0.2f, 1.0f);
    private float _rotationSpeed = 0.5f;
    private float _glowIntensity = 1.0f;
    private Vector2 _mousePosition;
    private float _totalTime;

    public override EffectMetadata Metadata => _metadata;

    /// <summary>
    /// Black hole requires continuous screen capture to warp screen content.
    /// </summary>
    public override bool RequiresContinuousScreenCapture => true;

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("BlackHoleShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        var paramsDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<BlackHoleParams>(),
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

        if (Configuration.TryGet("distortionStrength", out float strength))
            _distortionStrength = strength;

        if (Configuration.TryGet("eventHorizonSize", out float eventHorizon))
            _eventHorizonSize = eventHorizon;

        if (Configuration.TryGet("accretionDiskEnabled", out bool diskEnabled))
            _accretionDiskEnabled = diskEnabled;

        if (Configuration.TryGet("accretionDiskColor", out Vector4 diskColor))
            _accretionDiskColor = diskColor;

        if (Configuration.TryGet("rotationSpeed", out float rotation))
            _rotationSpeed = rotation;

        if (Configuration.TryGet("glowIntensity", out float glow))
            _glowIntensity = glow;
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
        var blackHoleParams = new BlackHoleParams
        {
            MousePosition = _mousePosition,
            ViewportSize = context.ViewportSize,
            Radius = _radius,
            DistortionStrength = _distortionStrength,
            EventHorizonSize = _eventHorizonSize,
            AccretionDiskEnabled = _accretionDiskEnabled ? 1.0f : 0.0f,
            RotationSpeed = _rotationSpeed,
            GlowIntensity = _glowIntensity,
            Time = _totalTime,
            AccretionDiskColor = _accretionDiskColor,
            HdrMultiplier = context.HdrPeakBrightness
        };

        context.UpdateBuffer(_paramsBuffer!, blackHoleParams);

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
        var assembly = typeof(BlackHoleEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.BlackHole.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #region Shader Structures

    [StructLayout(LayoutKind.Sequential, Size = 80)]
    private struct BlackHoleParams
    {
        // Must match HLSL cbuffer layout exactly!
        // Total size: 80 bytes (5 * 16), must be multiple of 16 for constant buffers

        public Vector2 MousePosition;      // 8 bytes, offset 0
        public Vector2 ViewportSize;       // 8 bytes, offset 8
        public float Radius;               // 4 bytes, offset 16
        public float DistortionStrength;   // 4 bytes, offset 20
        public float EventHorizonSize;     // 4 bytes, offset 24
        public float AccretionDiskEnabled; // 4 bytes, offset 28
        public float RotationSpeed;        // 4 bytes, offset 32
        public float GlowIntensity;        // 4 bytes, offset 36
        public float Time;                 // 4 bytes, offset 40
        public float HdrMultiplier;        // 4 bytes, offset 44
        public Vector4 AccretionDiskColor; // 16 bytes, offset 48
        private Vector4 _padding;          // 16 bytes, offset 64
    }

    #endregion
}
