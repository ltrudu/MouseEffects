using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.MagneticField;

public sealed class MagneticFieldEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "magneticfield",
        Name = "Magnetic Field",
        Description = "Visualization of magnetic field lines emanating from the mouse cursor with dipole pattern",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Resources
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _constantBuffer;

    // Effect Parameters (mf_ prefix for MagneticField)
    private int _lineCount = 16;
    private float _fieldStrength = 1.0f;
    private float _animationSpeed = 1.0f;
    private float _lineThickness = 2.0f;
    private float _glowIntensity = 1.5f;
    private float _effectRadius = 300f;
    private bool _dualPoleMode = false;
    private float _poleSeparation = 200f;
    private int _colorMode = 0; // 0=NorthSouth, 1=Unified, 2=Custom
    private Vector4 _northColor = new(0.255f, 0.412f, 0.882f, 1f); // #4169E1 Blue
    private Vector4 _southColor = new(0.863f, 0.078f, 0.235f, 1f); // #DC143C Red
    private Vector4 _unifiedColor = new(0f, 1f, 1f, 1f); // #00FFFF Cyan
    private float _fieldCurvature = 1.5f;
    private float _flowScale = 0.05f;
    private float _flowSpeed = 1.0f;

    // Animation
    private float _elapsedTime;
    private Vector2 _mousePosition;

    [StructLayout(LayoutKind.Sequential, Size = 112)]
    private struct MagneticFieldConstants
    {
        public Vector2 ViewportSize;
        public Vector2 MousePosition;

        public float Time;
        public int LineCount;
        public float FieldStrength;
        public float AnimationSpeed;

        public float LineThickness;
        public float GlowIntensity;
        public float EffectRadius;
        public float DualPoleMode;

        public float PoleSeparation;
        public int ColorMode;
        public float FieldCurvature;
        public float FlowScale;

        public float FlowSpeed;
        public float Padding1;
        public float Padding2;
        public float Padding3;

        public Vector4 NorthColor;
        public Vector4 SouthColor;
        public Vector4 UnifiedColor;
    }

    // Public properties for UI binding
    public int LineCount
    {
        get => _lineCount;
        set => _lineCount = Math.Clamp(value, 8, 32);
    }

    public float FieldStrength
    {
        get => _fieldStrength;
        set => _fieldStrength = value;
    }

    public float AnimationSpeed
    {
        get => _animationSpeed;
        set => _animationSpeed = value;
    }

    public float LineThickness
    {
        get => _lineThickness;
        set => _lineThickness = value;
    }

    public float GlowIntensity
    {
        get => _glowIntensity;
        set => _glowIntensity = value;
    }

    public float EffectRadius
    {
        get => _effectRadius;
        set => _effectRadius = value;
    }

    public bool DualPoleMode
    {
        get => _dualPoleMode;
        set => _dualPoleMode = value;
    }

    public float PoleSeparation
    {
        get => _poleSeparation;
        set => _poleSeparation = value;
    }

    public int ColorMode
    {
        get => _colorMode;
        set => _colorMode = Math.Clamp(value, 0, 2);
    }

    public Vector4 NorthColor
    {
        get => _northColor;
        set => _northColor = value;
    }

    public Vector4 SouthColor
    {
        get => _southColor;
        set => _southColor = value;
    }

    public Vector4 UnifiedColor
    {
        get => _unifiedColor;
        set => _unifiedColor = value;
    }

    public float FieldCurvature
    {
        get => _fieldCurvature;
        set => _fieldCurvature = value;
    }

    public float FlowScale
    {
        get => _flowScale;
        set => _flowScale = value;
    }

    public float FlowSpeed
    {
        get => _flowSpeed;
        set => _flowSpeed = value;
    }

    protected override void OnInitialize(IRenderContext context)
    {
        // Create constant buffer
        var bufferDesc = new BufferDescription
        {
            Size = 112, // Size of MagneticFieldConstants
            Type = BufferType.Constant,
            Dynamic = true
        };
        _constantBuffer = context.CreateBuffer(bufferDesc);

        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("MagneticFieldShader.hlsl");
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
        Configuration.TryGet("mf_lineCount", out _lineCount);
        Configuration.TryGet("mf_fieldStrength", out _fieldStrength);
        Configuration.TryGet("mf_animationSpeed", out _animationSpeed);
        Configuration.TryGet("mf_lineThickness", out _lineThickness);
        Configuration.TryGet("mf_glowIntensity", out _glowIntensity);
        Configuration.TryGet("mf_effectRadius", out _effectRadius);
        Configuration.TryGet("mf_dualPoleMode", out _dualPoleMode);
        Configuration.TryGet("mf_poleSeparation", out _poleSeparation);
        Configuration.TryGet("mf_colorMode", out _colorMode);
        Configuration.TryGet("mf_northColor", out _northColor);
        Configuration.TryGet("mf_southColor", out _southColor);
        Configuration.TryGet("mf_unifiedColor", out _unifiedColor);
        Configuration.TryGet("mf_fieldCurvature", out _fieldCurvature);
        Configuration.TryGet("mf_flowScale", out _flowScale);
        Configuration.TryGet("mf_flowSpeed", out _flowSpeed);
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        _elapsedTime += gameTime.DeltaSeconds * _animationSpeed;
        _mousePosition = mouseState.Position;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null || _constantBuffer == null)
            return;

        // Update constant buffer
        var constants = new MagneticFieldConstants
        {
            ViewportSize = context.ViewportSize,
            MousePosition = _mousePosition,
            Time = _elapsedTime,
            LineCount = _lineCount,
            FieldStrength = _fieldStrength,
            AnimationSpeed = _animationSpeed,
            LineThickness = _lineThickness,
            GlowIntensity = _glowIntensity,
            EffectRadius = _effectRadius,
            DualPoleMode = _dualPoleMode ? 1.0f : 0.0f,
            PoleSeparation = _poleSeparation,
            ColorMode = _colorMode,
            FieldCurvature = _fieldCurvature,
            FlowScale = _flowScale,
            FlowSpeed = _flowSpeed,
            Padding1 = 0,
            Padding2 = 0,
            Padding3 = 0,
            NorthColor = _northColor,
            SouthColor = _southColor,
            UnifiedColor = _unifiedColor
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
        var assembly = typeof(MagneticFieldEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.MagneticField.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
