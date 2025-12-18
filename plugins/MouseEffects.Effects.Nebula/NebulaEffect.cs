using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Nebula;

public sealed class NebulaEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "nebula",
        Name = "Nebula",
        Description = "Colorful cosmic gas clouds trailing the mouse cursor with volumetric feel and twinkling stars",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Cosmic
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 192)]
    private struct NebulaConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public Vector2 MousePosition;     // 8 bytes = 16
        public float Time;                // 4 bytes
        public float CloudDensity;        // 4 bytes
        public float SwirlSpeed;          // 4 bytes
        public int LayerCount;            // 4 bytes = 32
        public float GlowIntensity;       // 4 bytes
        public float StarDensity;         // 4 bytes
        public float EffectRadius;        // 4 bytes
        public float NoiseScale;          // 4 bytes = 48
        public float ColorVariation;      // 4 bytes
        public float HdrMultiplier;       // 4 bytes
        public int ColorPalette;          // 4 bytes (0=Orion, 1=Carina, 2=Eagle, 3=Custom)
        public float CloudSpeed;          // 4 bytes = 64
        public Vector4 CustomColor1;      // 16 bytes = 80
        public Vector4 CustomColor2;      // 16 bytes = 96
        public Vector4 CustomColor3;      // 16 bytes = 112
        public Vector4 PaletteColor1;     // 16 bytes = 128 (computed from palette)
        public Vector4 PaletteColor2;     // 16 bytes = 144
        public Vector4 PaletteColor3;     // 16 bytes = 160
        public Vector4 Padding1;          // 16 bytes = 176
        public Vector4 Padding2;          // 16 bytes = 192
    }

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Mouse tracking
    private Vector2 _currentMousePosition;

    // Configuration fields (nb_ prefix for Nebula)
    private float _cloudDensity = 0.7f;
    private float _swirlSpeed = 0.5f;
    private int _layerCount = 4;
    private float _glowIntensity = 1.5f;
    private float _starDensity = 0.3f;
    private float _effectRadius = 400f;
    private float _noiseScale = 1.2f;
    private float _colorVariation = 0.5f;
    private float _cloudSpeed = 0.3f;
    private int _colorPalette = 0; // 0=Orion, 1=Carina, 2=Eagle, 3=Custom
    private Vector4 _customColor1 = new(0.545f, 0f, 0.545f, 1f); // Purple
    private Vector4 _customColor2 = new(0.25f, 0.41f, 0.88f, 1f); // Blue
    private Vector4 _customColor3 = new(1f, 0.41f, 0.71f, 1f);    // Pink

    // Public properties for UI binding
    public float CloudDensity { get => _cloudDensity; set => _cloudDensity = value; }
    public float SwirlSpeed { get => _swirlSpeed; set => _swirlSpeed = value; }
    public int LayerCount { get => _layerCount; set => _layerCount = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public float StarDensity { get => _starDensity; set => _starDensity = value; }
    public float EffectRadius { get => _effectRadius; set => _effectRadius = value; }
    public float NoiseScale { get => _noiseScale; set => _noiseScale = value; }
    public float ColorVariation { get => _colorVariation; set => _colorVariation = value; }
    public float CloudSpeed { get => _cloudSpeed; set => _cloudSpeed = value; }
    public int ColorPalette { get => _colorPalette; set => _colorPalette = value; }
    public Vector4 CustomColor1 { get => _customColor1; set => _customColor1 = value; }
    public Vector4 CustomColor2 { get => _customColor2; set => _customColor2 = value; }
    public Vector4 CustomColor3 { get => _customColor3; set => _customColor3 = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("NebulaShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<NebulaConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("nb_cloudDensity", out float density))
            _cloudDensity = density;
        if (Configuration.TryGet("nb_swirlSpeed", out float swirl))
            _swirlSpeed = swirl;
        if (Configuration.TryGet("nb_layerCount", out int layers))
            _layerCount = layers;
        if (Configuration.TryGet("nb_glowIntensity", out float glow))
            _glowIntensity = glow;
        if (Configuration.TryGet("nb_starDensity", out float stars))
            _starDensity = stars;
        if (Configuration.TryGet("nb_effectRadius", out float radius))
            _effectRadius = radius;
        if (Configuration.TryGet("nb_noiseScale", out float noiseScale))
            _noiseScale = noiseScale;
        if (Configuration.TryGet("nb_colorVariation", out float colorVar))
            _colorVariation = colorVar;
        if (Configuration.TryGet("nb_cloudSpeed", out float speed))
            _cloudSpeed = speed;
        if (Configuration.TryGet("nb_colorPalette", out int palette))
            _colorPalette = palette;
        if (Configuration.TryGet("nb_customColor1", out Vector4 c1))
            _customColor1 = c1;
        if (Configuration.TryGet("nb_customColor2", out Vector4 c2))
            _customColor2 = c2;
        if (Configuration.TryGet("nb_customColor3", out Vector4 c3))
            _customColor3 = c3;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        // Store current mouse position for rendering
        _currentMousePosition = mouseState.Position;
    }

    protected override void OnRender(IRenderContext context)
    {
        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Get palette colors based on selection
        var (color1, color2, color3) = GetPaletteColors();

        // Update constant buffer
        var constants = new NebulaConstants
        {
            ViewportSize = context.ViewportSize,
            MousePosition = _currentMousePosition,
            Time = currentTime,
            CloudDensity = _cloudDensity,
            SwirlSpeed = _swirlSpeed,
            LayerCount = _layerCount,
            GlowIntensity = _glowIntensity,
            StarDensity = _starDensity,
            EffectRadius = _effectRadius,
            NoiseScale = _noiseScale,
            ColorVariation = _colorVariation,
            HdrMultiplier = context.HdrPeakBrightness,
            ColorPalette = _colorPalette,
            CloudSpeed = _cloudSpeed,
            CustomColor1 = _customColor1,
            CustomColor2 = _customColor2,
            CustomColor3 = _customColor3,
            PaletteColor1 = color1,
            PaletteColor2 = color2,
            PaletteColor3 = color3,
            Padding1 = Vector4.Zero,
            Padding2 = Vector4.Zero
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

    private (Vector4, Vector4, Vector4) GetPaletteColors()
    {
        return _colorPalette switch
        {
            0 => ( // Orion Nebula - Purple, Blue, Pink
                new Vector4(0.545f, 0f, 0.545f, 1f),    // Purple #8B008B
                new Vector4(0.25f, 0.41f, 0.88f, 1f),   // Blue #4169E1
                new Vector4(1f, 0.41f, 0.71f, 1f)       // Pink #FF69B4
            ),
            1 => ( // Carina Nebula - Orange, Yellow, Red
                new Vector4(1f, 0.27f, 0f, 1f),         // Orange #FF4500
                new Vector4(1f, 0.84f, 0f, 1f),         // Yellow #FFD700
                new Vector4(0.86f, 0.08f, 0.24f, 1f)    // Red #DC143C
            ),
            2 => ( // Eagle Nebula - Green, Teal, Blue
                new Vector4(0.13f, 0.545f, 0.13f, 1f),  // Green #228B22
                new Vector4(0f, 0.5f, 0.5f, 1f),        // Teal #008080
                new Vector4(0.275f, 0.51f, 0.71f, 1f)   // Blue #4682B4
            ),
            _ => ( // Custom
                _customColor1,
                _customColor2,
                _customColor3
            )
        };
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
        var resourceName = $"MouseEffects.Effects.Nebula.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
