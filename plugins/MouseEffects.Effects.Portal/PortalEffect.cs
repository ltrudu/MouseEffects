using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Portal;

public sealed class PortalEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "portal",
        Name = "Portal",
        Description = "Swirling dimensional vortex/portal effect at the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Cosmic
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Resources
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _constantBuffer;

    // Effect Parameters
    private float _portalRadius = 150f;
    private float _rotationSpeed = 1.0f;
    private float _spiralTightness = 1.0f;
    private int _spiralArms = 4;
    private float _glowIntensity = 1.2f;
    private float _depthStrength = 0.7f;
    private bool _rimParticlesEnabled = true;
    private Vector4 _portalColor = new(0.4f, 0.7f, 1f, 1f);
    private Vector4 _rimColor = new(0.8f, 0.9f, 1f, 1f);
    private float _innerDarkness = 0.2f;
    private float _distortionStrength = 1.0f;
    private float _particleSpeed = 1.5f;
    private int _colorTheme = 0; // 0=Blue, 1=Purple, 2=Orange, 3=Rainbow

    // Animation
    private float _elapsedTime;
    private Vector2 _mousePosition;

    [StructLayout(LayoutKind.Sequential, Size = 128)]
    private struct PortalConstants
    {
        public Vector2 ViewportSize;
        public Vector2 MousePosition;

        public float Time;
        public float PortalRadius;
        public float RotationSpeed;
        public float SpiralTightness;

        public int SpiralArms;
        public float GlowIntensity;
        public float DepthStrength;
        public float RimParticlesEnabled;

        public Vector4 PortalColor;
        public Vector4 RimColor;

        public float InnerDarkness;
        public float DistortionStrength;
        public float HdrMultiplier;
        public float ParticleSpeed;

        public int ColorTheme;
        public float Padding1;
        public float Padding2;
        public float Padding3;
    }

    // Public properties for UI binding
    public float PortalRadius
    {
        get => _portalRadius;
        set => _portalRadius = value;
    }

    public float RotationSpeed
    {
        get => _rotationSpeed;
        set => _rotationSpeed = value;
    }

    public float SpiralTightness
    {
        get => _spiralTightness;
        set => _spiralTightness = value;
    }

    public int SpiralArms
    {
        get => _spiralArms;
        set => _spiralArms = Math.Clamp(value, 2, 8);
    }

    public float GlowIntensity
    {
        get => _glowIntensity;
        set => _glowIntensity = value;
    }

    public float DepthStrength
    {
        get => _depthStrength;
        set => _depthStrength = value;
    }

    public bool RimParticlesEnabled
    {
        get => _rimParticlesEnabled;
        set => _rimParticlesEnabled = value;
    }

    public Vector4 PortalColor
    {
        get => _portalColor;
        set => _portalColor = value;
    }

    public Vector4 RimColor
    {
        get => _rimColor;
        set => _rimColor = value;
    }

    public float InnerDarkness
    {
        get => _innerDarkness;
        set => _innerDarkness = value;
    }

    public float DistortionStrength
    {
        get => _distortionStrength;
        set => _distortionStrength = value;
    }

    public float ParticleSpeed
    {
        get => _particleSpeed;
        set => _particleSpeed = value;
    }

    public int ColorTheme
    {
        get => _colorTheme;
        set => _colorTheme = Math.Clamp(value, 0, 3);
    }

    protected override void OnInitialize(IRenderContext context)
    {
        // Create constant buffer
        var bufferDesc = new BufferDescription
        {
            Size = 128, // Size of PortalConstants
            Type = BufferType.Constant,
            Dynamic = true
        };
        _constantBuffer = context.CreateBuffer(bufferDesc);

        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("PortalShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Load configuration
        LoadConfiguration();
    }

    protected override void OnConfigurationChanged()
    {
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        if (Configuration.TryGet("portalRadius", out float portalRadius))
            _portalRadius = portalRadius;
        if (Configuration.TryGet("rotationSpeed", out float rotationSpeed))
            _rotationSpeed = rotationSpeed;
        if (Configuration.TryGet("spiralTightness", out float spiralTightness))
            _spiralTightness = spiralTightness;
        if (Configuration.TryGet("spiralArms", out int spiralArms))
            _spiralArms = spiralArms;
        if (Configuration.TryGet("glowIntensity", out float glowIntensity))
            _glowIntensity = glowIntensity;
        if (Configuration.TryGet("depthStrength", out float depthStrength))
            _depthStrength = depthStrength;
        if (Configuration.TryGet("rimParticlesEnabled", out bool rimParticlesEnabled))
            _rimParticlesEnabled = rimParticlesEnabled;
        if (Configuration.TryGet("portalColor", out Vector4 portalColor))
            _portalColor = portalColor;
        if (Configuration.TryGet("rimColor", out Vector4 rimColor))
            _rimColor = rimColor;
        if (Configuration.TryGet("innerDarkness", out float innerDarkness))
            _innerDarkness = innerDarkness;
        if (Configuration.TryGet("distortionStrength", out float distortionStrength))
            _distortionStrength = distortionStrength;
        if (Configuration.TryGet("particleSpeed", out float particleSpeed))
            _particleSpeed = particleSpeed;
        if (Configuration.TryGet("colorTheme", out int colorTheme))
            _colorTheme = colorTheme;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        _elapsedTime += gameTime.DeltaSeconds;
        _mousePosition = mouseState.Position;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null || _constantBuffer == null)
            return;

        // Update constant buffer
        var constants = new PortalConstants
        {
            ViewportSize = context.ViewportSize,
            MousePosition = _mousePosition,
            Time = _elapsedTime,
            PortalRadius = _portalRadius,
            RotationSpeed = _rotationSpeed,
            SpiralTightness = _spiralTightness,
            SpiralArms = _spiralArms,
            GlowIntensity = _glowIntensity,
            DepthStrength = _depthStrength,
            RimParticlesEnabled = _rimParticlesEnabled ? 1.0f : 0.0f,
            PortalColor = _portalColor,
            RimColor = _rimColor,
            InnerDarkness = _innerDarkness,
            DistortionStrength = _distortionStrength,
            HdrMultiplier = context.HdrPeakBrightness,
            ParticleSpeed = _particleSpeed,
            ColorTheme = _colorTheme,
            Padding1 = 0,
            Padding2 = 0,
            Padding3 = 0
        };

        context.UpdateBuffer(_constantBuffer, constants);

        // Set pipeline state - Additive for glowing effect
        context.SetBlendState(BlendMode.Additive);
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw fullscreen triangle
        context.Draw(3, 0);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(PortalEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.Portal.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
