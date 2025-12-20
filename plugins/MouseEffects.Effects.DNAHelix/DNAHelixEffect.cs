using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.DNAHelix;

public sealed class DNAHelixEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "dnahelix",
        Name = "DNA Helix",
        Description = "Animated double helix DNA structure around the mouse cursor with base pairs and 3D rotation",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Physics
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 160)]
    private struct Constants
    {
        public Vector2 ViewportSize;
        public Vector2 MousePosition;
        public float Time;
        public float HelixHeight;
        public float HelixRadius;
        public float TwistRate;
        public float RotationSpeed;
        public float StrandThickness;
        public float GlowIntensity;
        public int BasePairCount;
        public Vector3 Strand1Color;
        public float Padding1;
        public Vector3 Strand2Color;
        public float Padding2;
        public Vector3 BasePairColor1;
        public float Padding3;
        public Vector3 BasePairColor2;
        public float Padding4;
    }

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Mouse tracking
    private Vector2 _currentMousePosition;
    private float _time;

    // Configuration properties
    private float _helixHeight = 400f;
    private float _rotationSpeed = 1.0f;
    private float _strandThickness = 4.0f;
    private int _basePairCount = 12;
    private float _glowIntensity = 0.8f;
    private float _helixRadius = 50f;
    private float _twistRate = 0.03f;
    private Vector3 _strand1Color = new Vector3(0.255f, 0.412f, 0.882f); // Blue #4169E1
    private Vector3 _strand2Color = new Vector3(0.863f, 0.078f, 0.235f); // Red #DC143C
    private Vector3 _basePairColor1 = new Vector3(0.196f, 0.804f, 0.196f); // Green #32CD32
    private Vector3 _basePairColor2 = new Vector3(1.0f, 0.843f, 0.0f); // Yellow #FFD700

    // Public properties for UI binding (don't call OnConfigurationChanged - it reads FROM config)
    public float HelixHeight
    {
        get => _helixHeight;
        set => _helixHeight = value;
    }

    public float RotationSpeed
    {
        get => _rotationSpeed;
        set => _rotationSpeed = value;
    }

    public float StrandThickness
    {
        get => _strandThickness;
        set => _strandThickness = value;
    }

    public int BasePairCount
    {
        get => _basePairCount;
        set => _basePairCount = Math.Max(1, Math.Min(50, value));
    }

    public float GlowIntensity
    {
        get => _glowIntensity;
        set => _glowIntensity = value;
    }

    public float HelixRadius
    {
        get => _helixRadius;
        set => _helixRadius = value;
    }

    public float TwistRate
    {
        get => _twistRate;
        set => _twistRate = value;
    }

    public Vector3 Strand1Color
    {
        get => _strand1Color;
        set => _strand1Color = value;
    }

    public Vector3 Strand2Color
    {
        get => _strand2Color;
        set => _strand2Color = value;
    }

    public Vector3 BasePairColor1
    {
        get => _basePairColor1;
        set => _basePairColor1 = value;
    }

    public Vector3 BasePairColor2
    {
        get => _basePairColor2;
        set => _basePairColor2 = value;
    }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("DNAHelixShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VS", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PS", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<Constants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });
    }

    protected override void OnConfigurationChanged()
    {
        // Only update fields if the key exists in configuration (preserve defaults otherwise)
        if (Configuration.TryGet("helixHeight", out float helixHeight))
            _helixHeight = helixHeight;
        if (Configuration.TryGet("rotationSpeed", out float rotationSpeed))
            _rotationSpeed = rotationSpeed;
        if (Configuration.TryGet("strandThickness", out float strandThickness))
            _strandThickness = strandThickness;
        if (Configuration.TryGet("basePairCount", out int basePairCount))
            _basePairCount = basePairCount;
        if (Configuration.TryGet("glowIntensity", out float glowIntensity))
            _glowIntensity = glowIntensity;
        if (Configuration.TryGet("helixRadius", out float helixRadius))
            _helixRadius = helixRadius;
        if (Configuration.TryGet("twistRate", out float twistRate))
            _twistRate = twistRate;

        if (Configuration.TryGet("strand1ColorR", out float s1r) &&
            Configuration.TryGet("strand1ColorG", out float s1g) &&
            Configuration.TryGet("strand1ColorB", out float s1b))
        {
            _strand1Color = new Vector3(s1r, s1g, s1b);
        }

        if (Configuration.TryGet("strand2ColorR", out float s2r) &&
            Configuration.TryGet("strand2ColorG", out float s2g) &&
            Configuration.TryGet("strand2ColorB", out float s2b))
        {
            _strand2Color = new Vector3(s2r, s2g, s2b);
        }

        if (Configuration.TryGet("basePair1ColorR", out float bp1r) &&
            Configuration.TryGet("basePair1ColorG", out float bp1g) &&
            Configuration.TryGet("basePair1ColorB", out float bp1b))
        {
            _basePairColor1 = new Vector3(bp1r, bp1g, bp1b);
        }

        if (Configuration.TryGet("basePair2ColorR", out float bp2r) &&
            Configuration.TryGet("basePair2ColorG", out float bp2g) &&
            Configuration.TryGet("basePair2ColorB", out float bp2b))
        {
            _basePairColor2 = new Vector3(bp2r, bp2g, bp2b);
        }
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        _time += gameTime.DeltaSeconds;
        _currentMousePosition = mouseState.Position;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null || _constantBuffer == null)
            return;

        var constants = new Constants
        {
            ViewportSize = context.ViewportSize,
            MousePosition = _currentMousePosition,
            Time = _time,
            HelixHeight = _helixHeight,
            HelixRadius = _helixRadius,
            TwistRate = _twistRate,
            RotationSpeed = _rotationSpeed,
            StrandThickness = _strandThickness,
            GlowIntensity = _glowIntensity,
            BasePairCount = _basePairCount,
            Strand1Color = _strand1Color,
            Strand2Color = _strand2Color,
            BasePairColor1 = _basePairColor1,
            BasePairColor2 = _basePairColor2
        };

        context.UpdateBuffer(_constantBuffer, constants);

        // Set up rendering state
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer);
        context.SetBlendState(BlendMode.Alpha);
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
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.DNAHelix.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
