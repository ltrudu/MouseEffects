using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.NeonGlow;

public sealed class NeonGlowEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "neonglow",
        Name = "Neon Glow",
        Description = "80s synthwave style neon trails with multilayer bloom following the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 80)]
    private struct NeonConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public float Time;                // 4 bytes
        public float LineThickness;       // 4 bytes = 16
        public float GlowIntensity;       // 4 bytes
        public int GlowLayers;            // 4 bytes
        public float FadeSpeed;           // 4 bytes
        public float SmoothingFactor;     // 4 bytes = 32
        public Vector4 PrimaryColor;      // 16 bytes = 48
        public Vector4 SecondaryColor;    // 16 bytes = 64
        public float HdrMultiplier;       // 4 bytes
        public int ColorMode;             // 4 bytes (0=fixed, 1=rainbow, 2=gradient)
        public float RainbowSpeed;        // 4 bytes
        public float Padding;             // 4 bytes = 80
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    private struct TrailPoint
    {
        public Vector2 Position;          // 8 bytes
        public float Age;                 // 4 bytes (0 = newest, increases over time)
        public float MaxAge;              // 4 bytes = 16
        public Vector4 Color;             // 16 bytes = 32
    }

    // Constants
    private const int MaxTrailPoints = 512;

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _trailPointBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Trail management (CPU side) - circular buffer
    private readonly TrailPoint[] _trailPoints = new TrailPoint[MaxTrailPoints];
    private readonly TrailPoint[] _gpuTrailPoints = new TrailPoint[MaxTrailPoints];
    private int _trailHeadIndex;
    private int _trailPointCount;
    private float _trailAccumulatedDistance;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private bool _isFirstUpdate = true;

    // Rainbow hue tracking
    private float _rainbowHue;

    // Configuration fields (ng_ prefix for NeonGlow)
    private int _maxTrailPoints = 200;
    private float _trailSpacing = 8f;
    private float _lineThickness = 4f;
    private int _glowLayers = 3;
    private float _glowIntensity = 1.5f;
    private float _fadeSpeed = 1.0f;
    private float _smoothingFactor = 0.3f;
    private int _colorMode = 1; // 0=fixed, 1=rainbow, 2=gradient
    private Vector4 _primaryColor = new(1f, 0.08f, 0.58f, 1f);  // Hot pink
    private Vector4 _secondaryColor = new(0f, 1f, 1f, 1f);      // Cyan
    private float _rainbowSpeed = 0.5f;

    // Public properties for UI binding
    public int MaxTrailLength { get => _maxTrailPoints; set => _maxTrailPoints = value; }
    public float TrailSpacing { get => _trailSpacing; set => _trailSpacing = value; }
    public float LineThickness { get => _lineThickness; set => _lineThickness = value; }
    public int GlowLayers { get => _glowLayers; set => _glowLayers = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public float FadeSpeed { get => _fadeSpeed; set => _fadeSpeed = value; }
    public float SmoothingFactor { get => _smoothingFactor; set => _smoothingFactor = value; }
    public int ColorMode { get => _colorMode; set => _colorMode = value; }
    public Vector4 PrimaryColor { get => _primaryColor; set => _primaryColor = value; }
    public Vector4 SecondaryColor { get => _secondaryColor; set => _secondaryColor = value; }
    public float RainbowSpeed { get => _rainbowSpeed; set => _rainbowSpeed = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("NeonGlowShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<NeonConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create trail point structured buffer
        _trailPointBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<TrailPoint>() * MaxTrailPoints,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<TrailPoint>()
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("ng_maxTrailPoints", out int maxPoints))
            _maxTrailPoints = maxPoints;
        if (Configuration.TryGet("ng_trailSpacing", out float spacing))
            _trailSpacing = spacing;
        if (Configuration.TryGet("ng_lineThickness", out float thickness))
            _lineThickness = thickness;
        if (Configuration.TryGet("ng_glowLayers", out int layers))
            _glowLayers = layers;
        if (Configuration.TryGet("ng_glowIntensity", out float intensity))
            _glowIntensity = intensity;
        if (Configuration.TryGet("ng_fadeSpeed", out float fadeSpd))
            _fadeSpeed = fadeSpd;
        if (Configuration.TryGet("ng_smoothingFactor", out float smooth))
            _smoothingFactor = smooth;
        if (Configuration.TryGet("ng_colorMode", out int mode))
            _colorMode = mode;
        if (Configuration.TryGet("ng_primaryColor", out Vector4 primCol))
            _primaryColor = primCol;
        if (Configuration.TryGet("ng_secondaryColor", out Vector4 secCol))
            _secondaryColor = secCol;
        if (Configuration.TryGet("ng_rainbowSpeed", out float rainbowSpd))
            _rainbowSpeed = rainbowSpd;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;

        // Update rainbow hue
        if (_colorMode == 1) // Rainbow mode
        {
            _rainbowHue += _rainbowSpeed * deltaTime;
            if (_rainbowHue > 1f) _rainbowHue -= 1f;
        }

        // Skip first frame to avoid huge line from (0,0)
        if (_isFirstUpdate)
        {
            _lastMousePos = mouseState.Position;
            _isFirstUpdate = false;
            return;
        }

        // Calculate distance moved this frame
        float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);
        _trailAccumulatedDistance += distanceFromLast;

        // Spawn trail points based on spacing
        while (_trailAccumulatedDistance >= _trailSpacing && _trailPointCount < _maxTrailPoints)
        {
            // Calculate position along the movement path
            float t = _trailSpacing / _trailAccumulatedDistance;
            Vector2 pointPos = Vector2.Lerp(_lastMousePos, mouseState.Position, t);

            // Apply smoothing to reduce jitter
            if (_trailPointCount > 0)
            {
                int prevIndex = (_trailHeadIndex - 1 + MaxTrailPoints) % MaxTrailPoints;
                pointPos = Vector2.Lerp(pointPos, _trailPoints[prevIndex].Position, _smoothingFactor);
            }

            SpawnTrailPoint(pointPos);
            _trailAccumulatedDistance -= _trailSpacing;
        }

        // Update last mouse position
        _lastMousePos = mouseState.Position;

        // Age existing trail points
        UpdateTrailPoints(deltaTime);
    }

    private void SpawnTrailPoint(Vector2 position)
    {
        // Get color based on mode
        Vector4 color = GetTrailColor();

        // Create trail point
        ref TrailPoint point = ref _trailPoints[_trailHeadIndex];
        point.Position = position;
        point.Age = 0f;
        point.MaxAge = _maxTrailPoints * _trailSpacing / 100f; // Lifetime based on trail length
        point.Color = color;

        _trailHeadIndex = (_trailHeadIndex + 1) % MaxTrailPoints;
        _trailPointCount = Math.Min(_trailPointCount + 1, _maxTrailPoints);
    }

    private void UpdateTrailPoints(float deltaTime)
    {
        // Age all points
        for (int i = 0; i < MaxTrailPoints; i++)
        {
            if (_trailPoints[i].MaxAge > 0)
            {
                _trailPoints[i].Age += deltaTime * _fadeSpeed;
            }
        }

        // Count active points
        _trailPointCount = 0;
        for (int i = 0; i < MaxTrailPoints; i++)
        {
            if (_trailPoints[i].Age < _trailPoints[i].MaxAge && _trailPoints[i].MaxAge > 0)
            {
                _trailPointCount++;
            }
        }
    }

    private Vector4 GetTrailColor()
    {
        return _colorMode switch
        {
            1 => HueToRgb(_rainbowHue), // Rainbow
            2 => Vector4.Lerp(_primaryColor, _secondaryColor, _rainbowHue), // Gradient
            _ => _primaryColor // Fixed color
        };
    }

    private static Vector4 HueToRgb(float hue)
    {
        hue -= MathF.Floor(hue);
        float h = hue * 6f;
        float x = 1f - MathF.Abs(h % 2f - 1f);

        Vector3 rgb = (int)h switch
        {
            0 => new Vector3(1f, x, 0f),
            1 => new Vector3(x, 1f, 0f),
            2 => new Vector3(0f, 1f, x),
            3 => new Vector3(0f, x, 1f),
            4 => new Vector3(x, 0f, 1f),
            _ => new Vector3(1f, 0f, x),
        };

        return new Vector4(rgb.X, rgb.Y, rgb.Z, 1f);
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_trailPointCount < 2) return; // Need at least 2 points to draw a trail

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU trail point buffer - copy all points
        for (int i = 0; i < MaxTrailPoints; i++)
        {
            _gpuTrailPoints[i] = _trailPoints[i];
        }

        // Update trail point buffer
        context.UpdateBuffer(_trailPointBuffer!, (ReadOnlySpan<TrailPoint>)_gpuTrailPoints.AsSpan());

        // Update constant buffer
        var constants = new NeonConstants
        {
            ViewportSize = context.ViewportSize,
            Time = currentTime,
            LineThickness = _lineThickness,
            GlowIntensity = _glowIntensity,
            GlowLayers = _glowLayers,
            FadeSpeed = _fadeSpeed,
            SmoothingFactor = _smoothingFactor,
            PrimaryColor = _primaryColor,
            SecondaryColor = _secondaryColor,
            HdrMultiplier = context.HdrPeakBrightness,
            ColorMode = _colorMode,
            RainbowSpeed = _rainbowSpeed,
            Padding = 0f
        };
        context.UpdateBuffer(_constantBuffer!, constants);

        // Set up rendering state
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _trailPointBuffer!);
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
        _trailPointBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.NeonGlow.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
