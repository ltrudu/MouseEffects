using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.CometTrail;

public sealed class CometTrailEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "comettrail",
        Name = "Comet Trail",
        Description = "A blazing comet with fiery tail and sparks following the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 96)]
    private struct CometConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public float Time;                // 4 bytes
        public float HeadSize;            // 4 bytes = 16
        public float TrailWidth;          // 4 bytes
        public float GlowIntensity;       // 4 bytes
        public float ColorTemperature;    // 4 bytes (0 = cooler, 1 = hotter)
        public float FadeSpeed;           // 4 bytes = 32
        public int SparkCount;            // 4 bytes
        public float SparkSize;           // 4 bytes
        public float SmoothingFactor;     // 4 bytes
        public float HdrMultiplier;       // 4 bytes = 48
        public Vector2 MousePosition;     // 8 bytes
        public float Padding1;            // 4 bytes
        public float Padding2;            // 4 bytes = 64
        public Vector4 CoreColor;         // 16 bytes = 80 (white)
        public Vector4 EdgeColor;         // 16 bytes = 96 (dark red)
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    private struct TrailPoint
    {
        public Vector2 Position;          // 8 bytes
        public float Age;                 // 4 bytes
        public float MaxAge;              // 4 bytes = 16
        public Vector4 Color;             // 16 bytes = 32
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    private struct Spark
    {
        public Vector2 Position;          // 8 bytes
        public Vector2 Velocity;          // 8 bytes = 16
        public float Age;                 // 4 bytes
        public float MaxAge;              // 4 bytes
        public float Size;                // 4 bytes
        public float Brightness;          // 4 bytes = 32
    }

    // Constants
    private const int MaxTrailPoints = 512;
    private const int MaxSparks = 256;

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _trailPointBuffer;
    private IBuffer? _sparkBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Trail management (CPU side)
    private readonly TrailPoint[] _trailPoints = new TrailPoint[MaxTrailPoints];
    private readonly TrailPoint[] _gpuTrailPoints = new TrailPoint[MaxTrailPoints];
    private int _trailHeadIndex;
    private int _trailPointCount;
    private float _trailAccumulatedDistance;

    // Spark management (CPU side)
    private readonly Spark[] _sparks = new Spark[MaxSparks];
    private readonly Spark[] _gpuSparks = new Spark[MaxSparks];
    private float _sparkSpawnTimer;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private bool _isFirstUpdate = true;

    // Random for spark variations
    private readonly Random _random = new();

    // Configuration fields (ct_ prefix for CometTrail)
    private int _maxTrailPoints = 250;
    private float _trailSpacing = 6f;
    private float _headSize = 20f;
    private float _trailWidth = 8f;
    private int _sparkCount = 5;
    private float _sparkSize = 3f;
    private float _colorTemperature = 0.7f;  // 0-1 (cooler to hotter)
    private float _glowIntensity = 2.0f;
    private float _fadeSpeed = 1.0f;
    private float _smoothingFactor = 0.2f;

    // Public properties for UI binding
    public int MaxTrailLength { get => _maxTrailPoints; set => _maxTrailPoints = value; }
    public float TrailSpacing { get => _trailSpacing; set => _trailSpacing = value; }
    public float HeadSize { get => _headSize; set => _headSize = value; }
    public float TrailWidth { get => _trailWidth; set => _trailWidth = value; }
    public int SparkCount { get => _sparkCount; set => _sparkCount = value; }
    public float SparkSize { get => _sparkSize; set => _sparkSize = value; }
    public float ColorTemperature { get => _colorTemperature; set => _colorTemperature = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public float FadeSpeed { get => _fadeSpeed; set => _fadeSpeed = value; }
    public float SmoothingFactor { get => _smoothingFactor; set => _smoothingFactor = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("CometTrailShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<CometConstants>(),
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

        // Create spark structured buffer
        _sparkBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<Spark>() * MaxSparks,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<Spark>()
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("ct_maxTrailPoints", out int maxPoints))
            _maxTrailPoints = maxPoints;
        if (Configuration.TryGet("ct_trailSpacing", out float spacing))
            _trailSpacing = spacing;
        if (Configuration.TryGet("ct_headSize", out float headSize))
            _headSize = headSize;
        if (Configuration.TryGet("ct_trailWidth", out float width))
            _trailWidth = width;
        if (Configuration.TryGet("ct_sparkCount", out int sparkCount))
            _sparkCount = sparkCount;
        if (Configuration.TryGet("ct_sparkSize", out float sparkSize))
            _sparkSize = sparkSize;
        if (Configuration.TryGet("ct_colorTemperature", out float temp))
            _colorTemperature = temp;
        if (Configuration.TryGet("ct_glowIntensity", out float intensity))
            _glowIntensity = intensity;
        if (Configuration.TryGet("ct_fadeSpeed", out float fadeSpd))
            _fadeSpeed = fadeSpd;
        if (Configuration.TryGet("ct_smoothingFactor", out float smooth))
            _smoothingFactor = smooth;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;

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

        // Spawn sparks periodically if mouse is moving
        if (distanceFromLast > 0.5f)
        {
            _sparkSpawnTimer += deltaTime;
            float sparkRate = _sparkCount / 10f; // Sparks per second
            if (_sparkSpawnTimer >= 1f / sparkRate)
            {
                SpawnSpark(mouseState.Position);
                _sparkSpawnTimer = 0f;
            }
        }

        // Update sparks
        UpdateSparks(deltaTime);
    }

    private void SpawnTrailPoint(Vector2 position)
    {
        // Get color based on temperature (hot to cool gradient along trail)
        Vector4 color = GetFireColor(0f); // Hottest at spawn

        // Create trail point
        ref TrailPoint point = ref _trailPoints[_trailHeadIndex];
        point.Position = position;
        point.Age = 0f;
        point.MaxAge = _maxTrailPoints * _trailSpacing / 80f; // Lifetime based on trail length
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

    private void SpawnSpark(Vector2 position)
    {
        if (_sparkCount >= MaxSparks) return;

        // Find first inactive spark
        for (int i = 0; i < MaxSparks; i++)
        {
            if (_sparks[i].Age >= _sparks[i].MaxAge || _sparks[i].MaxAge == 0)
            {
                // Random velocity (sideways and backward from movement)
                float angle = (float)(_random.NextDouble() * Math.PI * 2);
                float speed = (float)(_random.NextDouble() * 50 + 30);

                _sparks[i].Position = position;
                _sparks[i].Velocity = new Vector2(
                    MathF.Cos(angle) * speed,
                    MathF.Sin(angle) * speed
                );
                _sparks[i].Age = 0f;
                _sparks[i].MaxAge = (float)(_random.NextDouble() * 0.5 + 0.3); // 0.3-0.8 seconds
                _sparks[i].Size = _sparkSize * (float)(_random.NextDouble() * 0.5 + 0.75); // 75-125% of base size
                _sparks[i].Brightness = (float)(_random.NextDouble() * 0.3 + 0.7); // 70-100% brightness
                break;
            }
        }
    }

    private void UpdateSparks(float deltaTime)
    {
        for (int i = 0; i < MaxSparks; i++)
        {
            if (_sparks[i].Age < _sparks[i].MaxAge && _sparks[i].MaxAge > 0)
            {
                // Update position with gravity
                _sparks[i].Position += _sparks[i].Velocity * deltaTime;
                _sparks[i].Velocity.Y += 150f * deltaTime; // Gravity
                _sparks[i].Velocity *= 0.98f; // Air resistance
                _sparks[i].Age += deltaTime;
            }
        }
    }

    private Vector4 GetFireColor(float t)
    {
        // Fire gradient: White -> Yellow -> Orange -> Red -> Dark Red
        // t = 0 is hottest (white), t = 1 is coolest (dark red)

        // Adjust based on temperature setting
        t = MathF.Pow(t, 1f - _colorTemperature * 0.5f);

        if (t < 0.2f) // White to Yellow
        {
            float s = t / 0.2f;
            return new Vector4(1f, 1f, 1f - s * 0.5f, 1f);
        }
        else if (t < 0.4f) // Yellow to Orange
        {
            float s = (t - 0.2f) / 0.2f;
            return new Vector4(1f, 1f - s * 0.45f, 0.5f - s * 0.5f, 1f);
        }
        else if (t < 0.6f) // Orange to Red
        {
            float s = (t - 0.4f) / 0.2f;
            return new Vector4(1f, 0.55f - s * 0.27f, 0f, 1f);
        }
        else if (t < 0.8f) // Red to Dark Red
        {
            float s = (t - 0.6f) / 0.2f;
            return new Vector4(1f - s * 0.45f, 0.28f - s * 0.28f, 0f, 1f);
        }
        else // Dark Red fade
        {
            float s = (t - 0.8f) / 0.2f;
            return new Vector4(0.55f - s * 0.01f, 0f, 0f, 1f);
        }
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_trailPointCount < 2) return; // Need at least 2 points to draw a trail

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU trail point buffer
        for (int i = 0; i < MaxTrailPoints; i++)
        {
            _gpuTrailPoints[i] = _trailPoints[i];
        }

        // Build GPU spark buffer
        for (int i = 0; i < MaxSparks; i++)
        {
            _gpuSparks[i] = _sparks[i];
        }

        // Update buffers
        context.UpdateBuffer(_trailPointBuffer!, (ReadOnlySpan<TrailPoint>)_gpuTrailPoints.AsSpan());
        context.UpdateBuffer(_sparkBuffer!, (ReadOnlySpan<Spark>)_gpuSparks.AsSpan());

        // Get fire colors based on temperature
        Vector4 coreColor = GetFireColor(0f);  // White core
        Vector4 edgeColor = GetFireColor(1f);  // Dark red edge

        // Update constant buffer
        var constants = new CometConstants
        {
            ViewportSize = context.ViewportSize,
            Time = currentTime,
            HeadSize = _headSize,
            TrailWidth = _trailWidth,
            GlowIntensity = _glowIntensity,
            ColorTemperature = _colorTemperature,
            FadeSpeed = _fadeSpeed,
            SparkCount = _sparkCount,
            SparkSize = _sparkSize,
            SmoothingFactor = _smoothingFactor,
            HdrMultiplier = context.HdrPeakBrightness,
            MousePosition = _lastMousePos,
            Padding1 = 0f,
            Padding2 = 0f,
            CoreColor = coreColor,
            EdgeColor = edgeColor
        };
        context.UpdateBuffer(_constantBuffer!, constants);

        // Set up rendering state
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _trailPointBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 1, _sparkBuffer!);
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
        _sparkBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.CometTrail.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
