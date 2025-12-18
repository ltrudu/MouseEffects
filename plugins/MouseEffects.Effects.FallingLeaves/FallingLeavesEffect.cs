using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.FallingLeaves;

public sealed class FallingLeavesEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "fallingleaves",
        Name = "Falling Leaves",
        Description = "Autumn leaves drifting down from the mouse cursor with natural tumbling motion",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
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
    private struct LeafInstance
    {
        public Vector2 Position;          // 8 bytes - Current position
        public Vector2 Velocity;          // 8 bytes - Movement velocity (fall + wind)
        public Vector4 Color;             // 16 bytes = 32
        public float Size;                // 4 bytes - Leaf size
        public float Lifetime;            // 4 bytes - Current life remaining
        public float MaxLifetime;         // 4 bytes - Total lifetime
        public float RotationAngle;       // 4 bytes - Current rotation = 48
        public float RotationSpeed;       // 4 bytes - Rotation speed
        public float WindPhase;           // 4 bytes - Phase offset for wind oscillation
        public float TumblePhase;         // 4 bytes - Phase for tumbling animation
        public float TumbleSpeed;         // 4 bytes - Speed of tumbling = 64
        public int LeafVariant;           // 4 bytes - Leaf shape variant (0-2)
        public float SwayAmplitude;       // 4 bytes - Horizontal sway amount
        public float FallSpeedMod;        // 4 bytes - Individual fall speed multiplier
        public float Padding;             // 4 bytes = 80
    }

    // Constants
    private const int MaxLeaves = 500;

    // Autumn colors
    private static readonly Vector4[] AutumnColors = new Vector4[]
    {
        new Vector4(1.0f, 0.42f, 0.21f, 1f),    // Orange (#FF6B35)
        new Vector4(0.77f, 0.12f, 0.23f, 1f),   // Red (#C41E3A)
        new Vector4(1.0f, 0.84f, 0f, 1f),       // Yellow (#FFD700)
        new Vector4(0.55f, 0.27f, 0.07f, 1f),   // Brown (#8B4513)
        new Vector4(0.85f, 0.65f, 0.13f, 1f),   // Gold (#DAA520)
        new Vector4(0.8f, 0.36f, 0f, 1f),       // Dark Orange
        new Vector4(0.65f, 0.16f, 0.16f, 1f),   // Dark Red
    };

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _leafBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Leaf management (CPU side)
    private readonly LeafInstance[] _leaves = new LeafInstance[MaxLeaves];
    private readonly LeafInstance[] _gpuLeaves = new LeafInstance[MaxLeaves];
    private int _nextLeafIndex;
    private int _activeLeafCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _spawnAccumulator;

    // Configuration fields
    private int _leafCount = 30;
    private float _fallSpeed = 50f;
    private float _windStrength = 25f;
    private float _windFrequency = 0.3f;
    private float _minSize = 12f;
    private float _maxSize = 28f;
    private float _tumbleSpeed = 2.0f;
    private float _swayAmount = 40f;
    private float _spawnRadius = 120f;
    private float _leafLifetime = 10f;
    private float _colorVariety = 0.8f;

    // Public properties for UI binding
    public int LeafCount { get => _leafCount; set => _leafCount = value; }
    public float FallSpeed { get => _fallSpeed; set => _fallSpeed = value; }
    public float WindStrength { get => _windStrength; set => _windStrength = value; }
    public float WindFrequency { get => _windFrequency; set => _windFrequency = value; }
    public float MinSize { get => _minSize; set => _minSize = value; }
    public float MaxSize { get => _maxSize; set => _maxSize = value; }
    public float TumbleSpeed { get => _tumbleSpeed; set => _tumbleSpeed = value; }
    public float SwayAmount { get => _swayAmount; set => _swayAmount = value; }
    public float SpawnRadius { get => _spawnRadius; set => _spawnRadius = value; }
    public float LeafLifetime { get => _leafLifetime; set => _leafLifetime = value; }
    public float ColorVariety { get => _colorVariety; set => _colorVariety = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("FallingLeavesShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FrameConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create leaf structured buffer
        _leafBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<LeafInstance>() * MaxLeaves,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<LeafInstance>()
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("fl_leafCount", out int count))
            _leafCount = count;
        if (Configuration.TryGet("fl_fallSpeed", out float fall))
            _fallSpeed = fall;
        if (Configuration.TryGet("fl_windStrength", out float wind))
            _windStrength = wind;
        if (Configuration.TryGet("fl_windFrequency", out float freq))
            _windFrequency = freq;
        if (Configuration.TryGet("fl_minSize", out float minSize))
            _minSize = minSize;
        if (Configuration.TryGet("fl_maxSize", out float maxSize))
            _maxSize = maxSize;
        if (Configuration.TryGet("fl_tumbleSpeed", out float tumble))
            _tumbleSpeed = tumble;
        if (Configuration.TryGet("fl_swayAmount", out float sway))
            _swayAmount = sway;
        if (Configuration.TryGet("fl_spawnRadius", out float radius))
            _spawnRadius = radius;
        if (Configuration.TryGet("fl_lifetime", out float lifetime))
            _leafLifetime = lifetime;
        if (Configuration.TryGet("fl_colorVariety", out float variety))
            _colorVariety = variety;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        float totalTime = gameTime.TotalSeconds;

        // Update existing leaves
        UpdateLeaves(deltaTime, totalTime);

        // Calculate distance moved this frame
        float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);

        // Spawn leaves continuously when mouse moves
        if (distanceFromLast > 0.1f)
        {
            // Spawn rate based on leaf count setting
            float spawnRate = _leafCount * 1.5f; // Leaves per second
            _spawnAccumulator += deltaTime * spawnRate;

            while (_spawnAccumulator >= 1f)
            {
                SpawnLeaf(mouseState.Position, totalTime);
                _spawnAccumulator -= 1f;
            }
        }

        // Update last mouse position
        _lastMousePos = mouseState.Position;
    }

    private void UpdateLeaves(float deltaTime, float totalTime)
    {
        _activeLeafCount = 0;
        for (int i = 0; i < MaxLeaves; i++)
        {
            if (_leaves[i].Lifetime > 0)
            {
                ref var leaf = ref _leaves[i];

                // Age leaf
                leaf.Lifetime -= deltaTime;

                if (leaf.Lifetime > 0)
                {
                    // Apply gravity (downward fall) with individual variation
                    leaf.Velocity.Y = _fallSpeed * leaf.FallSpeedMod;

                    // Apply wind effect (oscillating horizontal movement)
                    float windEffect = MathF.Sin(totalTime * _windFrequency + leaf.WindPhase) * _windStrength;

                    // Add swaying motion (slower oscillation for natural drift)
                    float swayEffect = MathF.Sin(totalTime * 0.5f + leaf.WindPhase * 1.3f) * leaf.SwayAmplitude;

                    leaf.Velocity.X = windEffect + swayEffect;

                    // Update position
                    leaf.Position += leaf.Velocity * deltaTime;

                    // Update rotation (continuous spin)
                    leaf.RotationAngle += leaf.RotationSpeed * deltaTime;

                    // Update tumble animation (creates 3D flip effect)
                    leaf.TumblePhase += leaf.TumbleSpeed * deltaTime;

                    // Respawn at top if fallen below screen
                    if (leaf.Position.Y > 1080f)
                    {
                        leaf.Position.Y = -50f;
                        leaf.Position.X += (Random.Shared.NextSingle() - 0.5f) * 200f;
                    }

                    _activeLeafCount++;
                }
            }
        }
    }

    private void SpawnLeaf(Vector2 position, float time)
    {
        ref var leaf = ref _leaves[_nextLeafIndex];
        _nextLeafIndex = (_nextLeafIndex + 1) % MaxLeaves;

        // Random offset around cursor (spawn above cursor in a radius)
        float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
        float radius = Random.Shared.NextSingle() * _spawnRadius;
        Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

        // Bias spawn position upward
        offset.Y -= _spawnRadius * 0.6f;

        leaf.Position = position + offset;
        leaf.Lifetime = _leafLifetime * (0.8f + Random.Shared.NextSingle() * 0.4f);
        leaf.MaxLifetime = leaf.Lifetime;

        // Initial velocity
        leaf.Velocity = new Vector2(0, _fallSpeed);

        // Random size
        leaf.Size = _minSize + Random.Shared.NextSingle() * (_maxSize - _minSize);

        // Random rotation and rotation speed
        leaf.RotationAngle = Random.Shared.NextSingle() * MathF.PI * 2f;
        leaf.RotationSpeed = (Random.Shared.NextSingle() - 0.5f) * 2f; // Gentle rotation

        // Random wind and tumble phases
        leaf.WindPhase = Random.Shared.NextSingle() * MathF.PI * 2f;
        leaf.TumblePhase = Random.Shared.NextSingle() * MathF.PI * 2f;
        leaf.TumbleSpeed = _tumbleSpeed * (0.8f + Random.Shared.NextSingle() * 0.4f);

        // Random sway amplitude
        leaf.SwayAmplitude = _swayAmount * (0.7f + Random.Shared.NextSingle() * 0.6f);

        // Random fall speed variation
        leaf.FallSpeedMod = 0.7f + Random.Shared.NextSingle() * 0.6f;

        // Random leaf variant (0-2 for different shapes)
        leaf.LeafVariant = Random.Shared.Next(0, 3);

        // Random autumn color based on variety setting
        float varietyRoll = Random.Shared.NextSingle();
        if (varietyRoll < _colorVariety)
        {
            // Use one of the predefined autumn colors
            leaf.Color = AutumnColors[Random.Shared.Next(0, AutumnColors.Length)];
        }
        else
        {
            // Use a default warm orange
            leaf.Color = new Vector4(1.0f, 0.6f, 0.2f, 1f);
        }

        // Add slight color variation
        float colorVar = 0.9f + Random.Shared.NextSingle() * 0.2f;
        leaf.Color = new Vector4(
            leaf.Color.X * colorVar,
            leaf.Color.Y * colorVar,
            leaf.Color.Z * colorVar,
            1f
        );

        leaf.Padding = 0f;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeLeafCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU leaf buffer - only include alive leaves
        int gpuIndex = 0;
        for (int i = 0; i < MaxLeaves && gpuIndex < MaxLeaves; i++)
        {
            if (_leaves[i].Lifetime > 0)
            {
                _gpuLeaves[gpuIndex++] = _leaves[i];
            }
        }

        // Fill remaining with zeroed leaves
        for (int i = gpuIndex; i < MaxLeaves; i++)
        {
            _gpuLeaves[i] = default;
        }

        // Update leaf buffer
        context.UpdateBuffer(_leafBuffer!, (ReadOnlySpan<LeafInstance>)_gpuLeaves.AsSpan());

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
        context.SetShaderResource(ShaderStage.Vertex, 0, _leafBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _leafBuffer!);
        context.SetBlendState(BlendMode.Alpha);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced leaves (6 vertices per quad, one instance per leaf)
        context.DrawInstanced(6, MaxLeaves, 0, 0);

        // Restore blend state
        context.SetBlendState(BlendMode.Opaque);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _leafBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.FallingLeaves.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
