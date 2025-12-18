using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.StarfieldWarp;

public sealed class StarfieldWarpEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "starfieldwarp",
        Name = "Starfield Warp",
        Description = "Hyperspace/warp speed effect with stars streaking past, centered on mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Resources
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _constantBuffer;

    // Effect Parameters
    private int _starCount = 500;
    private float _warpSpeed = 1.0f;
    private float _streakLength = 0.5f;
    private float _effectRadius = 800f;
    private float _starBrightness = 1.2f;
    private bool _colorTintEnabled = true;
    private Vector4 _colorTint = new(0.6f, 0.8f, 1f, 1f);
    private bool _tunnelEffect = true;
    private float _tunnelDarkness = 0.3f;
    private float _starSize = 2.0f;
    private int _depthLayers = 3;
    private bool _pulseEffect = true;
    private float _pulseSpeed = 1.0f;

    // Animation
    private float _elapsedTime;
    private Vector2 _mousePosition;

    [StructLayout(LayoutKind.Sequential, Size = 128)]
    private struct StarfieldConstants
    {
        public Vector2 ViewportSize;
        public Vector2 MousePosition;

        public float Time;
        public int StarCount;
        public float WarpSpeed;
        public float StreakLength;

        public float EffectRadius;
        public float StarBrightness;
        public float ColorTintEnabled;
        public float TunnelEffect;

        public Vector4 ColorTint;

        public float TunnelDarkness;
        public float StarSize;
        public int DepthLayers;
        public float PulseEffect;

        public float PulseSpeed;
        public float Padding1;
        public float Padding2;
        public float Padding3;
    }

    // Public properties for UI binding
    public int StarCount
    {
        get => _starCount;
        set => _starCount = Math.Clamp(value, 100, 1000);
    }

    public float WarpSpeed
    {
        get => _warpSpeed;
        set => _warpSpeed = value;
    }

    public float StreakLength
    {
        get => _streakLength;
        set => _streakLength = value;
    }

    public float EffectRadius
    {
        get => _effectRadius;
        set => _effectRadius = value;
    }

    public float StarBrightness
    {
        get => _starBrightness;
        set => _starBrightness = value;
    }

    public bool ColorTintEnabled
    {
        get => _colorTintEnabled;
        set => _colorTintEnabled = value;
    }

    public Vector4 ColorTint
    {
        get => _colorTint;
        set => _colorTint = value;
    }

    public bool TunnelEffect
    {
        get => _tunnelEffect;
        set => _tunnelEffect = value;
    }

    public float TunnelDarkness
    {
        get => _tunnelDarkness;
        set => _tunnelDarkness = value;
    }

    public float StarSize
    {
        get => _starSize;
        set => _starSize = value;
    }

    public int DepthLayers
    {
        get => _depthLayers;
        set => _depthLayers = Math.Clamp(value, 1, 5);
    }

    public bool PulseEffect
    {
        get => _pulseEffect;
        set => _pulseEffect = value;
    }

    public float PulseSpeed
    {
        get => _pulseSpeed;
        set => _pulseSpeed = value;
    }

    protected override void OnInitialize(IRenderContext context)
    {
        // Create constant buffer
        var bufferDesc = new BufferDescription
        {
            Size = 128,
            Type = BufferType.Constant,
            Dynamic = true
        };
        _constantBuffer = context.CreateBuffer(bufferDesc);

        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("StarfieldWarpShader.hlsl");
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
        Configuration.TryGet("sw_starCount", out _starCount);
        Configuration.TryGet("sw_warpSpeed", out _warpSpeed);
        Configuration.TryGet("sw_streakLength", out _streakLength);
        Configuration.TryGet("sw_effectRadius", out _effectRadius);
        Configuration.TryGet("sw_starBrightness", out _starBrightness);
        Configuration.TryGet("sw_colorTintEnabled", out _colorTintEnabled);
        Configuration.TryGet("sw_colorTint", out _colorTint);
        Configuration.TryGet("sw_tunnelEffect", out _tunnelEffect);
        Configuration.TryGet("sw_tunnelDarkness", out _tunnelDarkness);
        Configuration.TryGet("sw_starSize", out _starSize);
        Configuration.TryGet("sw_depthLayers", out _depthLayers);
        Configuration.TryGet("sw_pulseEffect", out _pulseEffect);
        Configuration.TryGet("sw_pulseSpeed", out _pulseSpeed);
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
        var constants = new StarfieldConstants
        {
            ViewportSize = context.ViewportSize,
            MousePosition = _mousePosition,
            Time = _elapsedTime,
            StarCount = _starCount,
            WarpSpeed = _warpSpeed,
            StreakLength = _streakLength,
            EffectRadius = _effectRadius,
            StarBrightness = _starBrightness,
            ColorTintEnabled = _colorTintEnabled ? 1.0f : 0.0f,
            TunnelEffect = _tunnelEffect ? 1.0f : 0.0f,
            ColorTint = _colorTint,
            TunnelDarkness = _tunnelDarkness,
            StarSize = _starSize,
            DepthLayers = _depthLayers,
            PulseEffect = _pulseEffect ? 1.0f : 0.0f,
            PulseSpeed = _pulseSpeed,
            Padding1 = 0,
            Padding2 = 0,
            Padding3 = 0
        };

        context.UpdateBuffer(_constantBuffer, constants);

        // Set pipeline state
        context.SetBlendState(BlendMode.Additive);
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw fullscreen triangle
        context.Draw(3, 0);

        // Restore blend state
        context.SetBlendState(BlendMode.Alpha);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(StarfieldWarpEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.StarfieldWarp.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
