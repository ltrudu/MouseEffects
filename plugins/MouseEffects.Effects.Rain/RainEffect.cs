using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Rain;

public sealed class RainEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "rain",
        Name = "Rain",
        Description = "Realistic raindrops falling around the mouse cursor with splash effects",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Nature
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    private struct FrameConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public float Time;                // 4 bytes
        public float HdrMultiplier;       // 4 bytes = 16
        public Vector4 Padding;           // 16 bytes = 32
    }

    [StructLayout(LayoutKind.Sequential, Size = 80)]
    private struct RaindropInstance
    {
        public Vector2 Position;          // 8 bytes - Current position
        public Vector2 Velocity;          // 8 bytes - Movement velocity (fall + wind)
        public Vector4 Color;             // 16 bytes = 32
        public float Size;                // 4 bytes - Raindrop width
        public float Length;              // 4 bytes - Raindrop streak length
        public float Lifetime;            // 4 bytes - Current life remaining
        public float MaxLifetime;         // 4 bytes - Total lifetime = 48
        public float Intensity;           // 4 bytes - Brightness/opacity
        public float IsSplash;            // 4 bytes - 0=raindrop, 1=splash
        public float SplashRadius;        // 4 bytes - Current splash radius
        public float SplashAge;           // 4 bytes - How long splash has existed = 64
        public Vector4 Padding;           // 16 bytes = 80
    }

    // Constants
    private const int MaxRaindrops = 1000;
    private const float SplashLifetime = 0.3f; // Splashes last 0.3 seconds

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _raindropBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Raindrop management (CPU side)
    private readonly RaindropInstance[] _raindrops = new RaindropInstance[MaxRaindrops];
    private readonly RaindropInstance[] _gpuRaindrops = new RaindropInstance[MaxRaindrops];
    private int _nextRaindropIndex;
    private int _activeRaindropCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _spawnAccumulator;

    // Configuration fields
    private int _rainIntensity = 50;          // Drops per second
    private float _fallSpeed = 800f;          // Fast falling (pixels/sec)
    private float _windAngle = 15f;           // Wind angle in degrees
    private float _minLength = 15f;           // Min streak length
    private float _maxLength = 30f;           // Max streak length
    private float _minSize = 1.5f;            // Min width
    private float _maxSize = 3f;              // Max width
    private bool _splashEnabled = true;       // Enable splash effects
    private float _splashSize = 20f;          // Max splash radius
    private float _spawnRadius = 200f;        // Area around cursor
    private bool _fullScreenMode = false;     // Full screen or cursor-follow
    private float _raindropLifetime = 3f;     // Max raindrop lifetime

    // Public properties for UI binding
    public int RainIntensity { get => _rainIntensity; set => _rainIntensity = value; }
    public float FallSpeed { get => _fallSpeed; set => _fallSpeed = value; }
    public float WindAngle { get => _windAngle; set => _windAngle = value; }
    public float MinLength { get => _minLength; set => _minLength = value; }
    public float MaxLength { get => _maxLength; set => _maxLength = value; }
    public float MinSize { get => _minSize; set => _minSize = value; }
    public float MaxSize { get => _maxSize; set => _maxSize = value; }
    public bool SplashEnabled { get => _splashEnabled; set => _splashEnabled = value; }
    public float SplashSize { get => _splashSize; set => _splashSize = value; }
    public float SpawnRadius { get => _spawnRadius; set => _spawnRadius = value; }
    public bool FullScreenMode { get => _fullScreenMode; set => _fullScreenMode = value; }
    public float RaindropLifetime { get => _raindropLifetime; set => _raindropLifetime = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("RainShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FrameConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create raindrop structured buffer
        _raindropBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<RaindropInstance>() * MaxRaindrops,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<RaindropInstance>()
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("rain_intensity", out int intensity))
            _rainIntensity = intensity;
        if (Configuration.TryGet("rain_fallSpeed", out float fall))
            _fallSpeed = fall;
        if (Configuration.TryGet("rain_windAngle", out float wind))
            _windAngle = wind;
        if (Configuration.TryGet("rain_minLength", out float minLen))
            _minLength = minLen;
        if (Configuration.TryGet("rain_maxLength", out float maxLen))
            _maxLength = maxLen;
        if (Configuration.TryGet("rain_minSize", out float minSize))
            _minSize = minSize;
        if (Configuration.TryGet("rain_maxSize", out float maxSize))
            _maxSize = maxSize;
        if (Configuration.TryGet("rain_splashEnabled", out bool splash))
            _splashEnabled = splash;
        if (Configuration.TryGet("rain_splashSize", out float splashSize))
            _splashSize = splashSize;
        if (Configuration.TryGet("rain_spawnRadius", out float radius))
            _spawnRadius = radius;
        if (Configuration.TryGet("rain_fullScreen", out bool fullScreen))
            _fullScreenMode = fullScreen;
        if (Configuration.TryGet("rain_lifetime", out float lifetime))
            _raindropLifetime = lifetime;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        float totalTime = gameTime.TotalSeconds;

        // Update existing raindrops and splashes
        UpdateRaindrops(deltaTime, totalTime);

        // Spawn raindrops
        if (_fullScreenMode)
        {
            // Full screen rain - spawn across entire screen
            float spawnRate = _rainIntensity;
            _spawnAccumulator += deltaTime * spawnRate;

            while (_spawnAccumulator >= 1f)
            {
                SpawnRaindrop(null, totalTime); // null = random screen position
                _spawnAccumulator -= 1f;
            }
        }
        else
        {
            // Cursor-follow mode - spawn around cursor when it moves
            float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);

            if (distanceFromLast > 0.1f)
            {
                float spawnRate = _rainIntensity * 1.5f; // Slightly faster spawn around cursor
                _spawnAccumulator += deltaTime * spawnRate;

                while (_spawnAccumulator >= 1f)
                {
                    SpawnRaindrop(mouseState.Position, totalTime);
                    _spawnAccumulator -= 1f;
                }
            }
        }

        // Update last mouse position
        _lastMousePos = mouseState.Position;
    }

    private void UpdateRaindrops(float deltaTime, float totalTime)
    {
        _activeRaindropCount = 0;

        // Convert wind angle to radians
        float windRad = _windAngle * MathF.PI / 180f;

        for (int i = 0; i < MaxRaindrops; i++)
        {
            if (_raindrops[i].Lifetime > 0)
            {
                ref var rd = ref _raindrops[i];

                // Age the raindrop/splash
                rd.Lifetime -= deltaTime;

                if (rd.Lifetime > 0)
                {
                    if (rd.IsSplash > 0.5f)
                    {
                        // Update splash
                        rd.SplashAge += deltaTime;
                        float splashProgress = rd.SplashAge / SplashLifetime;
                        rd.SplashRadius = _splashSize * splashProgress;
                        rd.Intensity = 1f - splashProgress; // Fade out
                    }
                    else
                    {
                        // Update raindrop position
                        rd.Position += rd.Velocity * deltaTime;

                        // Check if raindrop hit "ground" (bottom of spawn area or screen)
                        float groundY = _fullScreenMode ? 1080f : _lastMousePos.Y + _spawnRadius;

                        if (rd.Position.Y >= groundY && _splashEnabled)
                        {
                            // Convert to splash
                            rd.IsSplash = 1f;
                            rd.SplashAge = 0f;
                            rd.SplashRadius = 0f;
                            rd.Lifetime = SplashLifetime;
                            rd.Intensity = 1f;
                        }
                    }

                    _activeRaindropCount++;
                }
            }
        }
    }

    private void SpawnRaindrop(Vector2? centerPosition, float time)
    {
        ref var rd = ref _raindrops[_nextRaindropIndex];
        _nextRaindropIndex = (_nextRaindropIndex + 1) % MaxRaindrops;

        Vector2 spawnPos;

        if (centerPosition.HasValue && !_fullScreenMode)
        {
            // Spawn around cursor
            float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
            float radius = Random.Shared.NextSingle() * _spawnRadius;
            Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

            // Bias spawn upward
            offset.Y -= _spawnRadius * 0.7f;
            spawnPos = centerPosition.Value + offset;
        }
        else
        {
            // Full screen - random position across top
            spawnPos = new Vector2(
                Random.Shared.NextSingle() * 1920f, // Assume screen width
                -50f // Start above screen
            );
        }

        rd.Position = spawnPos;
        rd.Lifetime = _raindropLifetime * (0.8f + Random.Shared.NextSingle() * 0.4f);
        rd.MaxLifetime = rd.Lifetime;

        // Calculate velocity based on fall speed and wind angle
        float windRad = _windAngle * MathF.PI / 180f;
        float speedVariation = 0.8f + Random.Shared.NextSingle() * 0.4f;
        rd.Velocity = new Vector2(
            MathF.Sin(windRad) * _fallSpeed * speedVariation,
            MathF.Cos(windRad) * _fallSpeed * speedVariation
        );

        // Random size and length
        rd.Size = _minSize + Random.Shared.NextSingle() * (_maxSize - _minSize);
        rd.Length = _minLength + Random.Shared.NextSingle() * (_maxLength - _minLength);

        // Initial intensity
        rd.Intensity = 0.7f + Random.Shared.NextSingle() * 0.3f;

        // It's a raindrop (not a splash)
        rd.IsSplash = 0f;
        rd.SplashRadius = 0f;
        rd.SplashAge = 0f;

        // Light blue/white color
        float colorVariation = 0.9f + Random.Shared.NextSingle() * 0.1f;
        rd.Color = new Vector4(colorVariation * 0.8f, colorVariation * 0.9f, colorVariation, 1f);
        rd.Padding = Vector4.Zero;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeRaindropCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU raindrop buffer - only include alive raindrops/splashes
        int gpuIndex = 0;
        for (int i = 0; i < MaxRaindrops && gpuIndex < MaxRaindrops; i++)
        {
            if (_raindrops[i].Lifetime > 0)
            {
                _gpuRaindrops[gpuIndex++] = _raindrops[i];
            }
        }

        // Fill remaining with zeroed raindrops
        for (int i = gpuIndex; i < MaxRaindrops; i++)
        {
            _gpuRaindrops[i] = default;
        }

        // Update raindrop buffer
        context.UpdateBuffer(_raindropBuffer!, (ReadOnlySpan<RaindropInstance>)_gpuRaindrops.AsSpan());

        // Update constant buffer
        var constants = new FrameConstants
        {
            ViewportSize = context.ViewportSize,
            Time = currentTime,
            HdrMultiplier = context.HdrPeakBrightness,
            Padding = Vector4.Zero
        };
        context.UpdateBuffer(_constantBuffer!, constants);

        // Set up rendering state
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _raindropBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _raindropBuffer!);
        context.SetBlendState(BlendMode.Alpha);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced raindrops/splashes (6 vertices per quad, one instance per raindrop)
        context.DrawInstanced(6, MaxRaindrops, 0, 0);

        // Restore blend state
        context.SetBlendState(BlendMode.Opaque);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _raindropBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.Rain.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
