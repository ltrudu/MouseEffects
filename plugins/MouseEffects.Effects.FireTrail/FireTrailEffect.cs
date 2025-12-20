using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.FireTrail;

/// <summary>
/// Fire Trail effect that creates realistic flames trailing behind the mouse cursor.
/// Features particle system with rising flames, flickering, heat distortion, smoke, and embers.
/// </summary>
public sealed class FireTrailEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "firetrail",
        Name = "Fire Trail",
        Description = "Creates realistic fire and flames that trail behind the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Trail
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 80)]
    private struct FireConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public Vector2 MousePosition;     // 8 bytes = 16
        public float Time;                // 4 bytes
        public float Intensity;           // 4 bytes
        public float FlameHeight;         // 4 bytes
        public float FlameWidth;          // 4 bytes = 32
        public float TurbulenceAmount;    // 4 bytes
        public float SmokeAmount;         // 4 bytes
        public float EmberAmount;         // 4 bytes
        public float GlowIntensity;       // 4 bytes = 48
        public float HdrMultiplier;       // 4 bytes - HDR peak brightness
        public float FireStyle;           // 4 bytes - 0=Campfire, 1=Torch, 2=Inferno
        public float ColorSaturation;     // 4 bytes
        public float FlickerSpeed;        // 4 bytes = 64
        public int ParticleCount;         // 4 bytes - Active particle count
        public Vector3 Padding;           // 12 bytes = 80
    }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct FireParticle
    {
        public Vector2 Position;          // 8 bytes - Current position
        public Vector2 Velocity;          // 8 bytes - Movement vector = 16
        public float Lifetime;            // 4 bytes - Current life remaining
        public float MaxLifetime;         // 4 bytes - Total lifetime
        public float Size;                // 4 bytes - Particle size
        public float Temperature;         // 4 bytes - Heat (affects color) = 32
        public Vector4 Color;             // 16 bytes - Base color = 48
        public float Rotation;            // 4 bytes - Rotation angle
        public float RotationSpeed;       // 4 bytes - Angular velocity
        public float ParticleType;        // 4 bytes - 0=Fire, 1=Smoke, 2=Ember
        public float Brightness;          // 4 bytes - Intensity multiplier = 64
    }

    // Constants
    private const int MaxParticles = 2048;
    private const float ParticleSpawnRate = 60f; // Particles per second at max speed

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _particleBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Particle management (CPU side)
    private readonly FireParticle[] _particles = new FireParticle[MaxParticles];
    private readonly FireParticle[] _gpuParticles = new FireParticle[MaxParticles];
    private int _nextParticleIndex;
    private int _activeParticleCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _accumulatedDistance;
    private float _lastSpawnTime;

    // Random number generator
    private readonly Random _random = new();

    // Configuration fields (ft_ prefix)
    private bool _enabled = true;
    private float _intensity = 1.0f;
    private float _flameHeight = 80f;
    private float _flameWidth = 40f;
    private float _turbulenceAmount = 0.5f;
    private float _smokeAmount = 0.3f;
    private float _emberAmount = 0.2f;
    private float _glowIntensity = 1.2f;
    private float _flickerSpeed = 15f;
    private int _fireStyle = 0; // 0=Campfire, 1=Torch, 2=Inferno
    private float _colorSaturation = 1.0f;
    private float _particleLifetime = 1.5f;
    private float _minSpeed = 20f;
    private float _maxSpeed = 60f;

    // Public properties for UI binding
    public bool Enabled { get => _enabled; set => _enabled = value; }
    public float Intensity { get => _intensity; set => _intensity = Math.Clamp(value, 0f, 2f); }
    public float FlameHeight { get => _flameHeight; set => _flameHeight = Math.Clamp(value, 20f, 200f); }
    public float FlameWidth { get => _flameWidth; set => _flameWidth = Math.Clamp(value, 10f, 100f); }
    public float TurbulenceAmount { get => _turbulenceAmount; set => _turbulenceAmount = Math.Clamp(value, 0f, 1f); }
    public float SmokeAmount { get => _smokeAmount; set => _smokeAmount = Math.Clamp(value, 0f, 1f); }
    public float EmberAmount { get => _emberAmount; set => _emberAmount = Math.Clamp(value, 0f, 1f); }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = Math.Clamp(value, 0f, 3f); }
    public float FlickerSpeed { get => _flickerSpeed; set => _flickerSpeed = Math.Clamp(value, 1f, 30f); }
    public int FireStyle { get => _fireStyle; set => _fireStyle = Math.Clamp(value, 0, 2); }
    public float ColorSaturation { get => _colorSaturation; set => _colorSaturation = Math.Clamp(value, 0f, 2f); }
    public float ParticleLifetime { get => _particleLifetime; set => _particleLifetime = Math.Clamp(value, 0.5f, 3f); }
    public float MinSpeed { get => _minSpeed; set => _minSpeed = Math.Clamp(value, 10f, 100f); }
    public float MaxSpeed { get => _maxSpeed; set => _maxSpeed = Math.Clamp(value, 20f, 150f); }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        var shaderSource = LoadEmbeddedShader("FireTrailShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FireConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create particle buffer (structured buffer for shader access)
        _particleBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FireParticle>() * MaxParticles,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<FireParticle>()
        });

        // Initialize particles array
        for (int i = 0; i < MaxParticles; i++)
        {
            _particles[i].Lifetime = 0f; // Dead particles
        }

        _lastMousePos = Vector2.Zero;
    }

    private string LoadEmbeddedShader(string fileName)
    {
        var assembly = typeof(FireTrailEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.FireTrail.Shaders.{fileName}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("ft_enabled", out bool enabled))
            _enabled = enabled;
        if (Configuration.TryGet("ft_intensity", out float intensity))
            _intensity = intensity;
        if (Configuration.TryGet("ft_flameHeight", out float flameHeight))
            _flameHeight = flameHeight;
        if (Configuration.TryGet("ft_flameWidth", out float flameWidth))
            _flameWidth = flameWidth;
        if (Configuration.TryGet("ft_turbulenceAmount", out float turbulenceAmount))
            _turbulenceAmount = turbulenceAmount;
        if (Configuration.TryGet("ft_smokeAmount", out float smokeAmount))
            _smokeAmount = smokeAmount;
        if (Configuration.TryGet("ft_emberAmount", out float emberAmount))
            _emberAmount = emberAmount;
        if (Configuration.TryGet("ft_glowIntensity", out float glowIntensity))
            _glowIntensity = glowIntensity;
        if (Configuration.TryGet("ft_flickerSpeed", out float flickerSpeed))
            _flickerSpeed = flickerSpeed;
        if (Configuration.TryGet("ft_fireStyle", out int fireStyle))
            _fireStyle = fireStyle;
        if (Configuration.TryGet("ft_colorSaturation", out float colorSaturation))
            _colorSaturation = colorSaturation;
        if (Configuration.TryGet("ft_particleLifetime", out float particleLifetime))
            _particleLifetime = particleLifetime;
        if (Configuration.TryGet("ft_minSpeed", out float minSpeed))
            _minSpeed = minSpeed;
        if (Configuration.TryGet("ft_maxSpeed", out float maxSpeed))
            _maxSpeed = maxSpeed;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        if (!_enabled) return;

        var mousePos = mouseState.Position;
        var deltaTime = (float)gameTime.DeltaTime.TotalSeconds;

        // Calculate mouse movement
        var mouseDelta = mousePos - _lastMousePos;
        var mouseSpeed = mouseDelta.Length() / (deltaTime > 0 ? deltaTime : 0.016f);

        // Accumulate distance for spawning
        if (_lastMousePos != Vector2.Zero)
        {
            _accumulatedDistance += mouseDelta.Length();
        }

        // Spawn new particles based on mouse movement
        var spawnInterval = 1f / (ParticleSpawnRate * _intensity);
        if (gameTime.TotalTime.TotalSeconds - _lastSpawnTime >= spawnInterval && mouseSpeed > 5f)
        {
            SpawnFireParticles(mousePos, mouseDelta, mouseSpeed, deltaTime);
            _lastSpawnTime = (float)gameTime.TotalTime.TotalSeconds;
        }

        // Update existing particles
        UpdateParticles(deltaTime, (float)gameTime.TotalTime.TotalSeconds);

        _lastMousePos = mousePos;
    }

    private void SpawnFireParticles(Vector2 position, Vector2 velocity, float speed, float deltaTime)
    {
        var normalizedVelocity = velocity.Length() > 0 ? Vector2.Normalize(velocity) : Vector2.Zero;
        var perpendicular = new Vector2(-normalizedVelocity.Y, normalizedVelocity.X);

        // Spawn multiple particles per frame based on intensity
        int particlesToSpawn = (int)(1 + _intensity * 3);

        for (int i = 0; i < particlesToSpawn; i++)
        {
            // Fire particle
            if (_random.NextDouble() < (1.0 - _smokeAmount))
            {
                SpawnParticle(position, perpendicular, 0); // Fire
            }

            // Smoke particle
            if (_random.NextDouble() < _smokeAmount)
            {
                SpawnParticle(position, perpendicular, 1); // Smoke
            }

            // Ember particle
            if (_random.NextDouble() < _emberAmount)
            {
                SpawnParticle(position, perpendicular, 2); // Ember
            }
        }
    }

    private void SpawnParticle(Vector2 position, Vector2 perpendicular, int type)
    {
        var idx = _nextParticleIndex;
        _nextParticleIndex = (_nextParticleIndex + 1) % MaxParticles;

        // Spread particles across trail width
        var offset = perpendicular * ((float)_random.NextDouble() * 2 - 1) * _flameWidth;
        position += offset;

        var particle = new FireParticle
        {
            Position = position,
            Lifetime = _particleLifetime,
            MaxLifetime = _particleLifetime,
            ParticleType = type
        };

        switch (type)
        {
            case 0: // Fire
                particle.Velocity = new Vector2(
                    ((float)_random.NextDouble() * 2 - 1) * _turbulenceAmount * 20f,
                    -_flameHeight * (0.5f + (float)_random.NextDouble() * 0.5f)
                );
                particle.Size = 8f + (float)_random.NextDouble() * 12f;
                particle.Temperature = 0.8f + (float)_random.NextDouble() * 0.2f;
                particle.Color = new Vector4(1f, 0.8f, 0.2f, 1f);
                particle.Brightness = 1.0f + (float)_random.NextDouble() * 0.5f;
                particle.Rotation = (float)_random.NextDouble() * MathF.PI * 2;
                particle.RotationSpeed = ((float)_random.NextDouble() * 2 - 1) * 3f;
                break;

            case 1: // Smoke
                particle.Velocity = new Vector2(
                    ((float)_random.NextDouble() * 2 - 1) * _turbulenceAmount * 15f,
                    -_flameHeight * 0.3f * (0.8f + (float)_random.NextDouble() * 0.4f)
                );
                particle.Size = 12f + (float)_random.NextDouble() * 20f;
                particle.Temperature = 0.2f + (float)_random.NextDouble() * 0.1f;
                particle.Color = new Vector4(0.3f, 0.3f, 0.3f, 0.6f);
                particle.Brightness = 0.3f + (float)_random.NextDouble() * 0.3f;
                particle.Rotation = (float)_random.NextDouble() * MathF.PI * 2;
                particle.RotationSpeed = ((float)_random.NextDouble() * 2 - 1) * 2f;
                break;

            case 2: // Ember
                particle.Velocity = new Vector2(
                    ((float)_random.NextDouble() * 2 - 1) * _turbulenceAmount * 25f,
                    -_flameHeight * (0.3f + (float)_random.NextDouble() * 0.7f)
                );
                particle.Size = 2f + (float)_random.NextDouble() * 4f;
                particle.Temperature = 1.0f;
                particle.Color = new Vector4(1f, 0.5f, 0.1f, 1f);
                particle.Brightness = 1.5f + (float)_random.NextDouble() * 1f;
                particle.Rotation = (float)_random.NextDouble() * MathF.PI * 2;
                particle.RotationSpeed = ((float)_random.NextDouble() * 2 - 1) * 8f;
                break;
        }

        _particles[idx] = particle;
        _activeParticleCount = Math.Min(_activeParticleCount + 1, MaxParticles);
    }

    private void UpdateParticles(float deltaTime, float totalTime)
    {
        _activeParticleCount = 0;

        for (int i = 0; i < MaxParticles; i++)
        {
            ref var particle = ref _particles[i];
            if (particle.Lifetime <= 0) continue;

            // Update lifetime
            particle.Lifetime -= deltaTime;
            if (particle.Lifetime <= 0) continue;

            // Update position
            particle.Position += particle.Velocity * deltaTime;

            // Apply turbulence (noise-based movement)
            var noiseX = MathF.Sin(totalTime * 3f + particle.Position.X * 0.01f) * _turbulenceAmount * 10f;
            var noiseY = MathF.Cos(totalTime * 2f + particle.Position.Y * 0.01f) * _turbulenceAmount * 5f;
            particle.Position += new Vector2(noiseX, noiseY) * deltaTime;

            // Update rotation
            particle.Rotation += particle.RotationSpeed * deltaTime;

            // Fade out particles near end of life
            var lifeFactor = particle.Lifetime / particle.MaxLifetime;
            particle.Color.W = lifeFactor;

            // Cool down fire particles over time
            if (particle.ParticleType == 0) // Fire
            {
                particle.Temperature *= 0.98f;
            }

            // Expand smoke particles
            if (particle.ParticleType == 1) // Smoke
            {
                particle.Size += deltaTime * 10f;
            }

            _activeParticleCount++;
        }
    }

    protected override void OnRender(IRenderContext context)
    {
        if (!_enabled || _constantBuffer == null || _particleBuffer == null) return;
        if (_activeParticleCount == 0) return;

        // Copy particles to GPU array (only active particles)
        int gpuIndex = 0;
        for (int i = 0; i < MaxParticles && gpuIndex < _activeParticleCount; i++)
        {
            if (_particles[i].Lifetime > 0)
            {
                _gpuParticles[gpuIndex++] = _particles[i];
            }
        }

        // Update constant buffer
        var constants = new FireConstants
        {
            ViewportSize = context.ViewportSize,
            MousePosition = _lastMousePos,
            Time = (float)DateTime.Now.TimeOfDay.TotalSeconds,
            Intensity = _intensity,
            FlameHeight = _flameHeight,
            FlameWidth = _flameWidth,
            TurbulenceAmount = _turbulenceAmount,
            SmokeAmount = _smokeAmount,
            EmberAmount = _emberAmount,
            GlowIntensity = _glowIntensity,
            HdrMultiplier = context.HdrPeakBrightness,
            FireStyle = _fireStyle,
            ColorSaturation = _colorSaturation,
            FlickerSpeed = _flickerSpeed,
            ParticleCount = gpuIndex,
            Padding = Vector3.Zero
        };

        context.UpdateBuffer(_constantBuffer, constants);

        // Update particle buffer
        if (gpuIndex > 0)
        {
            context.UpdateBuffer(_particleBuffer!, (ReadOnlySpan<FireParticle>)_gpuParticles.AsSpan(0, gpuIndex));
        }

        // Set shaders
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer);
        context.SetShaderResource(ShaderStage.Pixel, 0, _particleBuffer!);

        // Draw fullscreen quad with additive blending
        context.SetBlendState(BlendMode.Additive);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _particleBuffer?.Dispose();
    }
}
