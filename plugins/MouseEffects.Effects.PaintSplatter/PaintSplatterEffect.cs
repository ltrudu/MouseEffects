using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

using CoreMouseButtons = MouseEffects.Core.Input.MouseButtons;

namespace MouseEffects.Effects.PaintSplatter;

public sealed class PaintSplatterEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "paintsplatter",
        Name = "Paint Splatter",
        Description = "Artistic paint drops that splatter on clicks like Jackson Pollock",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Artistic
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct FrameConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public float Time;                // 4 bytes
        public float HdrMultiplier;       // 4 bytes = 16
        public float EdgeNoisiness;       // 4 bytes
        public float Opacity;             // 4 bytes
        public int EnableDrips;           // 4 bytes
        public float Padding1;            // 4 bytes = 32
        public Vector4 Padding2;          // 16 bytes = 48
        public Vector4 Padding3;          // 16 bytes = 64
    }

    [StructLayout(LayoutKind.Sequential, Size = 96)]
    private struct SplatParticle
    {
        public Vector2 Position;          // 8 bytes - Splat center
        public float MainRadius;          // 4 bytes - Main splat size
        public float Lifetime;            // 4 bytes = 16 - Current life remaining
        public float MaxLifetime;         // 4 bytes - Total lifetime
        public float BirthTime;           // 4 bytes - When spawned
        public float Seed;                // 4 bytes - Random seed for noise
        public int DropletCount;          // 4 bytes = 32 - Number of droplets
        public Vector4 Color;             // 16 bytes = 48 - Paint color
        public float DripLength;          // 4 bytes - Drip trail length
        public float DripSpeed;           // 4 bytes - How fast drip extends
        public float CurrentDripLength;   // 4 bytes - Current drip extension
        public float Padding1;            // 4 bytes = 64
        public Vector4 Padding2;          // 16 bytes = 80
        public Vector4 Padding3;          // 16 bytes = 96
    }

    // Constants
    private const int MaxSplatsLimit = 200;

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _splatBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Particle management
    private readonly SplatParticle[] _splats = new SplatParticle[MaxSplatsLimit];
    private readonly SplatParticle[] _gpuSplats = new SplatParticle[MaxSplatsLimit];
    private int _nextSplatIndex;
    private int _activeSplatCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;

    // Configuration fields
    private float _splatSize = 60f;
    private int _dropletCount = 15;
    private bool _enableDrips = true;
    private float _dripLength = 40f;
    private float _opacity = 0.85f;
    private float _lifetime = 5.0f;
    private float _spreadRadius = 25f;
    private int _colorMode = 0; // 0=Single, 1=Random, 2=Palette
    private int _paletteIndex = 0; // 0=Primary, 1=Neon, 2=Earth, 3=Pastel
    private Vector4 _singleColor = new(0.9f, 0.1f, 0.1f, 1f); // Bright red
    private bool _clickEnabled = true;
    private int _maxSplats = 50;
    private float _edgeNoisiness = 0.3f;

    // Color palettes
    private static readonly Vector4[][] ColorPalettes =
    [
        // Primary
        [
            new Vector4(0.9f, 0.1f, 0.1f, 1f),   // Red
            new Vector4(0.1f, 0.1f, 0.9f, 1f),   // Blue
            new Vector4(0.95f, 0.9f, 0.1f, 1f)   // Yellow
        ],
        // Neon
        [
            new Vector4(1.0f, 0.0f, 0.5f, 1f),   // Hot Pink
            new Vector4(0.0f, 0.9f, 0.9f, 1f),   // Cyan
            new Vector4(0.5f, 1.0f, 0.0f, 1f)    // Lime Green
        ],
        // Earth
        [
            new Vector4(0.4f, 0.25f, 0.1f, 1f),  // Brown
            new Vector4(0.9f, 0.5f, 0.1f, 1f),   // Orange
            new Vector4(0.4f, 0.5f, 0.2f, 1f)    // Olive
        ],
        // Pastel
        [
            new Vector4(1.0f, 0.8f, 0.85f, 1f),  // Light Pink
            new Vector4(0.7f, 0.85f, 1.0f, 1f),  // Baby Blue
            new Vector4(0.7f, 1.0f, 0.85f, 1f)   // Mint
        ]
    ];

    // Public properties for UI binding
    public float SplatSize { get => _splatSize; set => _splatSize = value; }
    public int DropletCount { get => _dropletCount; set => _dropletCount = value; }
    public bool EnableDrips { get => _enableDrips; set => _enableDrips = value; }
    public float DripLength { get => _dripLength; set => _dripLength = value; }
    public float Opacity { get => _opacity; set => _opacity = value; }
    public float Lifetime { get => _lifetime; set => _lifetime = value; }
    public float SpreadRadius { get => _spreadRadius; set => _spreadRadius = value; }
    public int ColorMode { get => _colorMode; set => _colorMode = value; }
    public int PaletteIndex { get => _paletteIndex; set => _paletteIndex = value; }
    public Vector4 SingleColor { get => _singleColor; set => _singleColor = value; }
    public bool ClickEnabled { get => _clickEnabled; set => _clickEnabled = value; }
    public int MaxSplats { get => _maxSplats; set => _maxSplats = value; }
    public float EdgeNoisiness { get => _edgeNoisiness; set => _edgeNoisiness = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("PaintSplatterShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FrameConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create splat structured buffer
        _splatBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<SplatParticle>() * MaxSplatsLimit,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<SplatParticle>()
        });

        // Initialize all splats as inactive
        for (int i = 0; i < MaxSplatsLimit; i++)
        {
            _splats[i].Lifetime = 0f;
        }
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("ps_splatSize", out float size))
            _splatSize = size;
        if (Configuration.TryGet("ps_dropletCount", out int count))
            _dropletCount = count;
        if (Configuration.TryGet("ps_enableDrips", out bool drips))
            _enableDrips = drips;
        if (Configuration.TryGet("ps_dripLength", out float dripLen))
            _dripLength = dripLen;
        if (Configuration.TryGet("ps_opacity", out float opacity))
            _opacity = opacity;
        if (Configuration.TryGet("ps_lifetime", out float lifetime))
            _lifetime = lifetime;
        if (Configuration.TryGet("ps_spreadRadius", out float spread))
            _spreadRadius = spread;
        if (Configuration.TryGet("ps_colorMode", out int colorMode))
            _colorMode = colorMode;
        if (Configuration.TryGet("ps_paletteIndex", out int palette))
            _paletteIndex = palette;
        if (Configuration.TryGet("ps_singleColor", out Vector4 color))
            _singleColor = color;
        if (Configuration.TryGet("ps_clickEnabled", out bool click))
            _clickEnabled = click;
        if (Configuration.TryGet("ps_maxSplats", out int maxSplats))
            _maxSplats = maxSplats;
        if (Configuration.TryGet("ps_edgeNoisiness", out float noise))
            _edgeNoisiness = noise;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        float totalTime = gameTime.TotalSeconds;

        // Update existing splats
        UpdateSplats(deltaTime);

        // Handle click trigger
        bool leftPressed = mouseState.IsButtonPressed(CoreMouseButtons.Left);
        bool rightPressed = mouseState.IsButtonPressed(CoreMouseButtons.Right);

        if (_clickEnabled && (leftPressed && !_wasLeftPressed || rightPressed && !_wasRightPressed))
        {
            SpawnSplat(mouseState.Position, totalTime);
        }

        _wasLeftPressed = leftPressed;
        _wasRightPressed = rightPressed;
        _lastMousePos = mouseState.Position;
    }

    private void UpdateSplats(float deltaTime)
    {
        _activeSplatCount = 0;

        for (int i = 0; i < MaxSplatsLimit; i++)
        {
            ref var splat = ref _splats[i];
            if (splat.Lifetime <= 0) continue; // Inactive

            // Age the splat
            splat.Lifetime -= deltaTime;

            if (splat.Lifetime > 0)
            {
                // Extend drip if enabled
                if (_enableDrips && splat.CurrentDripLength < splat.DripLength)
                {
                    splat.CurrentDripLength += splat.DripSpeed * deltaTime;
                    splat.CurrentDripLength = Math.Min(splat.CurrentDripLength, splat.DripLength);
                }

                _activeSplatCount++;
            }
        }
    }

    private void SpawnSplat(Vector2 position, float totalTime)
    {
        // Check if we've reached the max active splats
        if (_activeSplatCount >= _maxSplats)
        {
            // Find oldest splat and replace it
            int oldestIndex = -1;
            float oldestTime = float.MaxValue;
            for (int i = 0; i < MaxSplatsLimit; i++)
            {
                if (_splats[i].Lifetime > 0 && _splats[i].BirthTime < oldestTime)
                {
                    oldestTime = _splats[i].BirthTime;
                    oldestIndex = i;
                }
            }
            if (oldestIndex >= 0)
            {
                _nextSplatIndex = oldestIndex;
            }
            else
            {
                return; // No slot available
            }
        }
        else
        {
            // Find next free slot
            int attempts = 0;
            while (_splats[_nextSplatIndex].Lifetime > 0 && attempts < MaxSplatsLimit)
            {
                _nextSplatIndex = (_nextSplatIndex + 1) % MaxSplatsLimit;
                attempts++;
            }
            if (attempts >= MaxSplatsLimit) return; // No free slot
        }

        ref var splat = ref _splats[_nextSplatIndex];
        _nextSplatIndex = (_nextSplatIndex + 1) % MaxSplatsLimit;

        // Create the splat
        splat.Position = position;
        splat.MainRadius = _splatSize * (0.8f + Random.Shared.NextSingle() * 0.4f);
        splat.Lifetime = _lifetime;
        splat.MaxLifetime = _lifetime;
        splat.BirthTime = totalTime;
        splat.Seed = Random.Shared.NextSingle() * 1000f;
        splat.DropletCount = _dropletCount;
        splat.Color = GetPaintColor();
        splat.DripLength = _enableDrips ? _dripLength * (0.7f + Random.Shared.NextSingle() * 0.6f) : 0f;
        splat.DripSpeed = 50f + Random.Shared.NextSingle() * 30f;
        splat.CurrentDripLength = 0f;
        splat.Padding1 = 0f;
        splat.Padding2 = Vector4.Zero;
        splat.Padding3 = Vector4.Zero;
    }

    private Vector4 GetPaintColor()
    {
        if (_colorMode == 0)
        {
            return _singleColor;
        }
        else if (_colorMode == 1)
        {
            // Random from all palettes
            var allColors = ColorPalettes.SelectMany(p => p).ToArray();
            return allColors[Random.Shared.Next(allColors.Length)];
        }
        else if (_colorMode == 2)
        {
            // Random from selected palette
            var palette = ColorPalettes[Math.Clamp(_paletteIndex, 0, ColorPalettes.Length - 1)];
            return palette[Random.Shared.Next(palette.Length)];
        }
        else
        {
            return _singleColor;
        }
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeSplatCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU splat buffer - only include alive splats
        int gpuIndex = 0;
        for (int i = 0; i < MaxSplatsLimit && gpuIndex < MaxSplatsLimit; i++)
        {
            if (_splats[i].Lifetime > 0)
            {
                _gpuSplats[gpuIndex++] = _splats[i];
            }
        }

        // Fill remaining with zeroed splats
        for (int i = gpuIndex; i < MaxSplatsLimit; i++)
        {
            _gpuSplats[i] = default;
        }

        // Update splat buffer
        context.UpdateBuffer(_splatBuffer!, (ReadOnlySpan<SplatParticle>)_gpuSplats.AsSpan());

        // Update constant buffer
        var constants = new FrameConstants
        {
            ViewportSize = context.ViewportSize,
            Time = currentTime,
            HdrMultiplier = context.HdrPeakBrightness,
            EdgeNoisiness = _edgeNoisiness,
            Opacity = _opacity,
            EnableDrips = _enableDrips ? 1 : 0,
            Padding1 = 0f,
            Padding2 = Vector4.Zero,
            Padding3 = Vector4.Zero
        };
        context.UpdateBuffer(_constantBuffer!, constants);

        // Set up rendering state
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _splatBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _splatBuffer!);
        context.SetBlendState(BlendMode.Alpha);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced splats (6 vertices per quad, one instance per splat)
        context.DrawInstanced(6, MaxSplatsLimit, 0, 0);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _splatBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.PaintSplatter.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
