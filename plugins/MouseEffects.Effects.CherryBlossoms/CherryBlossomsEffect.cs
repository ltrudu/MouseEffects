using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.CherryBlossoms;

public sealed class CherryBlossomsEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "cherry-blossoms",
        Name = "Cherry Blossoms",
        Description = "Beautiful sakura petals floating gently around the mouse cursor",
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

    [StructLayout(LayoutKind.Sequential)]
    private struct PetalInstance
    {
        public Vector2 Position;          // 8 bytes, offset 0
        public Vector2 Velocity;          // 8 bytes, offset 8
        public Vector4 Color;             // 16 bytes, offset 16
        public float Size;                // 4 bytes, offset 32
        public float Lifetime;            // 4 bytes, offset 36
        public float MaxLifetime;         // 4 bytes, offset 40
        public float RotationAngle;       // 4 bytes, offset 44
        public float SpinSpeed;           // 4 bytes, offset 48
        public float SwayPhase;           // 4 bytes, offset 52
        public float SwayAmplitude;       // 4 bytes, offset 56
        public float GlowIntensity;       // 4 bytes, offset 60
        public float FallSpeed;           // 4 bytes, offset 64
        public float ColorVariant;        // 4 bytes, offset 68
        public float Padding1;            // 4 bytes, offset 72
        public float Padding2;            // 4 bytes, offset 76 = 80 bytes total
    }

    // Constants
    private const int MaxPetals = 500;

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _petalBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Petal management (CPU side)
    private readonly PetalInstance[] _petals = new PetalInstance[MaxPetals];
    private readonly PetalInstance[] _gpuPetals = new PetalInstance[MaxPetals];
    private int _nextPetalIndex;
    private int _activePetalCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _spawnAccumulator;

    // Configuration fields (cb_ prefix for config keys)
    private int _petalCount = 30;
    private float _fallSpeed = 60f;
    private float _swayAmount = 40f;
    private float _swayFrequency = 0.8f;
    private float _minSize = 10f;
    private float _maxSize = 18f;
    private float _spinSpeed = 1.5f;
    private float _glowIntensity = 0.8f;
    private float _spawnRadius = 180f;
    private float _petalLifetime = 10f;

    // Public properties for UI binding
    public int PetalCount { get => _petalCount; set => _petalCount = value; }
    public float FallSpeed { get => _fallSpeed; set => _fallSpeed = value; }
    public float SwayAmount { get => _swayAmount; set => _swayAmount = value; }
    public float SwayFrequency { get => _swayFrequency; set => _swayFrequency = value; }
    public float MinSize { get => _minSize; set => _minSize = value; }
    public float MaxSize { get => _maxSize; set => _maxSize = value; }
    public float SpinSpeed { get => _spinSpeed; set => _spinSpeed = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public float SpawnRadius { get => _spawnRadius; set => _spawnRadius = value; }
    public float PetalLifetime { get => _petalLifetime; set => _petalLifetime = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("CherryBlossomsShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FrameConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create petal structured buffer
        _petalBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<PetalInstance>() * MaxPetals,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<PetalInstance>()
        });

        // Initialize buffer with zeros to prevent garbage data artifacts
        Array.Clear(_gpuPetals, 0, MaxPetals);
        context.UpdateBuffer(_petalBuffer!, (ReadOnlySpan<PetalInstance>)_gpuPetals.AsSpan());
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("cb_petalCount", out int count))
            _petalCount = count;
        if (Configuration.TryGet("cb_fallSpeed", out float fall))
            _fallSpeed = fall;
        if (Configuration.TryGet("cb_swayAmount", out float sway))
            _swayAmount = sway;
        if (Configuration.TryGet("cb_swayFrequency", out float freq))
            _swayFrequency = freq;
        if (Configuration.TryGet("cb_minSize", out float minSize))
            _minSize = minSize;
        if (Configuration.TryGet("cb_maxSize", out float maxSize))
            _maxSize = maxSize;
        if (Configuration.TryGet("cb_spinSpeed", out float spin))
            _spinSpeed = spin;
        if (Configuration.TryGet("cb_glowIntensity", out float glow))
            _glowIntensity = glow;
        if (Configuration.TryGet("cb_spawnRadius", out float radius))
            _spawnRadius = radius;
        if (Configuration.TryGet("cb_lifetime", out float lifetime))
            _petalLifetime = lifetime;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        float totalTime = gameTime.TotalSeconds;

        // Update existing petals
        UpdatePetals(deltaTime, totalTime);

        // Calculate distance moved this frame
        float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);

        // Spawn petals continuously when mouse moves
        if (distanceFromLast > 0.1f)
        {
            // Spawn rate based on petal count setting
            float spawnRate = _petalCount * 1.5f; // Petals per second
            _spawnAccumulator += deltaTime * spawnRate;

            while (_spawnAccumulator >= 1f)
            {
                SpawnPetal(mouseState.Position, totalTime);
                _spawnAccumulator -= 1f;
            }
        }

        // Update last mouse position
        _lastMousePos = mouseState.Position;
    }

    private void UpdatePetals(float deltaTime, float totalTime)
    {
        _activePetalCount = 0;
        for (int i = 0; i < MaxPetals; i++)
        {
            if (_petals[i].Lifetime > 0)
            {
                ref var petal = ref _petals[i];

                // Age petal
                petal.Lifetime -= deltaTime;

                if (petal.Lifetime > 0)
                {
                    // Apply gravity (downward fall)
                    petal.Velocity.Y = petal.FallSpeed;

                    // Apply sway effect (gentle side-to-side oscillation)
                    float swayEffect = MathF.Sin(totalTime * _swayFrequency + petal.SwayPhase) * petal.SwayAmplitude;
                    petal.Velocity.X = swayEffect;

                    // Update position
                    petal.Position += petal.Velocity * deltaTime;

                    // Update rotation (gentle spinning as they fall)
                    petal.RotationAngle += petal.SpinSpeed * deltaTime;

                    // Respawn at top if fallen below screen
                    if (petal.Position.Y > 1080f)
                    {
                        petal.Position.Y = -50f;
                        petal.Position.X += (Random.Shared.NextSingle() - 0.5f) * 200f;
                    }

                    _activePetalCount++;
                }
            }
        }
    }

    private void SpawnPetal(Vector2 position, float time)
    {
        ref var petal = ref _petals[_nextPetalIndex];
        _nextPetalIndex = (_nextPetalIndex + 1) % MaxPetals;

        // Random offset around cursor (spawn above and around cursor)
        float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
        float radius = Random.Shared.NextSingle() * _spawnRadius;
        Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

        // Bias spawn position upward
        offset.Y -= _spawnRadius * 0.6f; // Spawn above cursor

        petal.Position = position + offset;
        petal.Lifetime = _petalLifetime * (0.8f + Random.Shared.NextSingle() * 0.4f);
        petal.MaxLifetime = petal.Lifetime;

        // Initial velocity (will be overridden in update)
        petal.Velocity = new Vector2(0, _fallSpeed);

        // Random size
        petal.Size = _minSize + Random.Shared.NextSingle() * (_maxSize - _minSize);

        // Random rotation and spin speed
        petal.RotationAngle = Random.Shared.NextSingle() * MathF.PI * 2f;
        petal.SpinSpeed = (Random.Shared.NextSingle() - 0.5f) * _spinSpeed * 2f;

        // Random sway phase for varied oscillation
        petal.SwayPhase = Random.Shared.NextSingle() * MathF.PI * 2f;
        petal.SwayAmplitude = _swayAmount * (0.7f + Random.Shared.NextSingle() * 0.6f);

        // Random fall speed variation
        petal.FallSpeed = _fallSpeed * (0.8f + Random.Shared.NextSingle() * 0.4f);

        // Random glow intensity
        petal.GlowIntensity = _glowIntensity * (0.8f + Random.Shared.NextSingle() * 0.4f);

        // Cherry blossom colors (soft pinks and whites)
        // Variant 0 = light pink, 1 = medium pink, 2 = white, 3 = soft peach
        float colorVariant = Random.Shared.NextSingle();
        petal.ColorVariant = colorVariant;
        petal.Color = GetPetalColor(colorVariant);
        petal.Padding1 = 0f;
        petal.Padding2 = 0f;
    }

    private static Vector4 GetPetalColor(float variant)
    {
        if (variant < 0.4f)
        {
            // Light pink (#FFB7C5)
            return new Vector4(1f, 0.72f, 0.77f, 1f);
        }
        else if (variant < 0.7f)
        {
            // Medium pink (#FF69B4 / Hot Pink but softer)
            return new Vector4(1f, 0.5f, 0.67f, 1f);
        }
        else if (variant < 0.9f)
        {
            // Soft white with pink tint (#FFC0CB / Pink)
            return new Vector4(1f, 0.82f, 0.86f, 1f);
        }
        else
        {
            // Pure white
            return new Vector4(1f, 1f, 1f, 1f);
        }
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activePetalCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU petal buffer - only include alive petals
        int gpuIndex = 0;
        for (int i = 0; i < MaxPetals && gpuIndex < MaxPetals; i++)
        {
            if (_petals[i].Lifetime > 0)
            {
                _gpuPetals[gpuIndex++] = _petals[i];
            }
        }

        // Fill remaining with zeroed petals
        for (int i = gpuIndex; i < MaxPetals; i++)
        {
            _gpuPetals[i] = default;
        }

        // Update petal buffer
        context.UpdateBuffer(_petalBuffer!, (ReadOnlySpan<PetalInstance>)_gpuPetals.AsSpan());

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
        context.SetShaderResource(ShaderStage.Vertex, 0, _petalBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _petalBuffer!);
        context.SetBlendState(BlendMode.Alpha);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced petals (6 vertices per quad, one instance per petal)
        context.DrawInstanced(6, MaxPetals, 0, 0);

        // Restore blend state
        context.SetBlendState(BlendMode.Opaque);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _petalBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.CherryBlossoms.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
