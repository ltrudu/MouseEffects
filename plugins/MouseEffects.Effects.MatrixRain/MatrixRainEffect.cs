using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

using CoreMouseButtons = MouseEffects.Core.Input.MouseButtons;

namespace MouseEffects.Effects.MatrixRain;

public sealed class MatrixRainEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "matrixrain",
        Name = "Matrix Rain",
        Description = "Iconic falling green code effect from The Matrix centered around the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Digital
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct MatrixConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public Vector2 MousePosition;     // 8 bytes = 16
        public float Time;                // 4 bytes
        public float ColumnDensity;       // 4 bytes
        public float FallSpeed;           // 4 bytes
        public float CharChangeRate;      // 4 bytes = 32
        public float GlowIntensity;       // 4 bytes
        public float TrailLength;         // 4 bytes
        public float EffectRadius;        // 4 bytes
        public float HdrMultiplier;       // 4 bytes = 48
        public Vector4 Color;             // 16 bytes = 64
    }

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Mouse tracking
    private Vector2 _lastMousePos;

    // Configuration fields
    private float _columnDensity = 0.04f;      // Columns per pixel (0.04 = ~25px spacing)
    private float _minFallSpeed = 100f;
    private float _maxFallSpeed = 300f;
    private float _charChangeRate = 8f;
    private float _glowIntensity = 1.2f;
    private float _trailLength = 0.7f;
    private float _effectRadius = 300f;
    private Vector4 _color = new(0.2f, 1f, 0.3f, 1f);  // Matrix green

    // Public properties for UI binding
    public float ColumnDensity { get => _columnDensity; set => _columnDensity = value; }
    public float MinFallSpeed { get => _minFallSpeed; set => _minFallSpeed = value; }
    public float MaxFallSpeed { get => _maxFallSpeed; set => _maxFallSpeed = value; }
    public float CharChangeRate { get => _charChangeRate; set => _charChangeRate = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public float TrailLength { get => _trailLength; set => _trailLength = value; }
    public float EffectRadius { get => _effectRadius; set => _effectRadius = value; }
    public Vector4 Color { get => _color; set => _color = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("MatrixRainShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<MatrixConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        _lastMousePos = new Vector2(context.ViewportSize.X / 2, context.ViewportSize.Y / 2);
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("mr_columnDensity", out float colDensity))
            _columnDensity = colDensity;
        if (Configuration.TryGet("mr_minFallSpeed", out float minSpeed))
            _minFallSpeed = minSpeed;
        if (Configuration.TryGet("mr_maxFallSpeed", out float maxSpeed))
            _maxFallSpeed = maxSpeed;
        if (Configuration.TryGet("mr_charChangeRate", out float changeRate))
            _charChangeRate = changeRate;
        if (Configuration.TryGet("mr_glowIntensity", out float glow))
            _glowIntensity = glow;
        if (Configuration.TryGet("mr_trailLength", out float trail))
            _trailLength = trail;
        if (Configuration.TryGet("mr_effectRadius", out float radius))
            _effectRadius = radius;
        if (Configuration.TryGet("mr_color", out Vector4 col))
            _color = col;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        _lastMousePos = mouseState.Position;
    }

    protected override void OnRender(IRenderContext context)
    {
        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Update constant buffer
        var constants = new MatrixConstants
        {
            ViewportSize = context.ViewportSize,
            MousePosition = _lastMousePos,
            Time = currentTime,
            ColumnDensity = _columnDensity,
            FallSpeed = (_minFallSpeed + _maxFallSpeed) / 2f,
            CharChangeRate = _charChangeRate,
            GlowIntensity = _glowIntensity,
            TrailLength = _trailLength,
            EffectRadius = _effectRadius,
            HdrMultiplier = context.HdrPeakBrightness,
            Color = _color
        };
        context.UpdateBuffer(_constantBuffer!, constants);

        // Set up rendering state
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetBlendState(BlendMode.Additive);
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
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.MatrixRain.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
