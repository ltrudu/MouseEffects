using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Aurora;

public sealed class AuroraEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "aurora",
        Name = "Aurora",
        Description = "Beautiful northern lights ribbons following the mouse cursor with flowing colors and organic motion",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 144)]
    private struct AuroraConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public Vector2 MousePosition;     // 8 bytes = 16
        public float Time;                // 4 bytes
        public float Height;              // 4 bytes
        public float HorizontalSpread;    // 4 bytes
        public float WaveSpeed;           // 4 bytes = 32
        public float WaveFrequency;       // 4 bytes
        public int NumLayers;             // 4 bytes
        public float ColorIntensity;      // 4 bytes
        public float GlowStrength;        // 4 bytes = 48
        public float NoiseScale;          // 4 bytes
        public float NoiseStrength;       // 4 bytes
        public float VerticalFlow;        // 4 bytes
        public float HdrMultiplier;       // 4 bytes = 64
        public Vector4 PrimaryColor;      // 16 bytes = 80
        public Vector4 SecondaryColor;    // 16 bytes = 96
        public Vector4 TertiaryColor;     // 16 bytes = 112
        public Vector4 AccentColor;       // 16 bytes = 128
        public Vector4 Padding;           // 16 bytes = 144
    }

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Mouse tracking
    private Vector2 _currentMousePosition;

    // Configuration fields (au_ prefix for Aurora)
    private float _height = 400f;
    private float _horizontalSpread = 300f;
    private float _waveSpeed = 1.0f;
    private float _waveFrequency = 2.0f;
    private int _numLayers = 3;
    private float _colorIntensity = 1.5f;
    private float _glowStrength = 2.0f;
    private Vector4 _primaryColor = new(0f, 1f, 0.5f, 1f);
    private Vector4 _secondaryColor = new(0f, 1f, 1f, 1f);
    private Vector4 _tertiaryColor = new(0.545f, 0f, 1f, 1f);
    private Vector4 _accentColor = new(1f, 0.078f, 0.576f, 1f);
    private float _noiseScale = 1.5f;
    private float _noiseStrength = 0.3f;
    private float _verticalFlow = 0.5f;

    // Public properties for UI binding
    public float Height { get => _height; set => _height = value; }
    public float HorizontalSpread { get => _horizontalSpread; set => _horizontalSpread = value; }
    public float WaveSpeed { get => _waveSpeed; set => _waveSpeed = value; }
    public float WaveFrequency { get => _waveFrequency; set => _waveFrequency = value; }
    public int NumLayers { get => _numLayers; set => _numLayers = value; }
    public float ColorIntensity { get => _colorIntensity; set => _colorIntensity = value; }
    public float GlowStrength { get => _glowStrength; set => _glowStrength = value; }
    public Vector4 PrimaryColor { get => _primaryColor; set => _primaryColor = value; }
    public Vector4 SecondaryColor { get => _secondaryColor; set => _secondaryColor = value; }
    public Vector4 TertiaryColor { get => _tertiaryColor; set => _tertiaryColor = value; }
    public Vector4 AccentColor { get => _accentColor; set => _accentColor = value; }
    public float NoiseScale { get => _noiseScale; set => _noiseScale = value; }
    public float NoiseStrength { get => _noiseStrength; set => _noiseStrength = value; }
    public float VerticalFlow { get => _verticalFlow; set => _verticalFlow = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("AuroraShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<AuroraConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("au_height", out float height))
            _height = height;
        if (Configuration.TryGet("au_horizontalSpread", out float spread))
            _horizontalSpread = spread;
        if (Configuration.TryGet("au_waveSpeed", out float waveSpeed))
            _waveSpeed = waveSpeed;
        if (Configuration.TryGet("au_waveFrequency", out float waveFreq))
            _waveFrequency = waveFreq;
        if (Configuration.TryGet("au_numLayers", out int numLayers))
            _numLayers = numLayers;
        if (Configuration.TryGet("au_colorIntensity", out float colorInt))
            _colorIntensity = colorInt;
        if (Configuration.TryGet("au_glowStrength", out float glowStr))
            _glowStrength = glowStr;
        if (Configuration.TryGet("au_primaryColor", out Vector4 primCol))
            _primaryColor = primCol;
        if (Configuration.TryGet("au_secondaryColor", out Vector4 secCol))
            _secondaryColor = secCol;
        if (Configuration.TryGet("au_tertiaryColor", out Vector4 tertCol))
            _tertiaryColor = tertCol;
        if (Configuration.TryGet("au_accentColor", out Vector4 accentCol))
            _accentColor = accentCol;
        if (Configuration.TryGet("au_noiseScale", out float noiseScale))
            _noiseScale = noiseScale;
        if (Configuration.TryGet("au_noiseStrength", out float noiseStr))
            _noiseStrength = noiseStr;
        if (Configuration.TryGet("au_verticalFlow", out float vertFlow))
            _verticalFlow = vertFlow;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        // Store current mouse position for rendering
        _currentMousePosition = mouseState.Position;
    }

    protected override void OnRender(IRenderContext context)
    {
        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Update constant buffer
        var constants = new AuroraConstants
        {
            ViewportSize = context.ViewportSize,
            MousePosition = _currentMousePosition,
            Time = currentTime,
            Height = _height,
            HorizontalSpread = _horizontalSpread,
            WaveSpeed = _waveSpeed,
            WaveFrequency = _waveFrequency,
            NumLayers = _numLayers,
            ColorIntensity = _colorIntensity,
            GlowStrength = _glowStrength,
            NoiseScale = _noiseScale,
            NoiseStrength = _noiseStrength,
            VerticalFlow = _verticalFlow,
            HdrMultiplier = context.HdrPeakBrightness,
            PrimaryColor = _primaryColor,
            SecondaryColor = _secondaryColor,
            TertiaryColor = _tertiaryColor,
            AccentColor = _accentColor,
            Padding = Vector4.Zero
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
        var resourceName = $"MouseEffects.Effects.Aurora.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
