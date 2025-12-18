using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.GravityWell;

public sealed class GravityWellEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "gravitywell",
        Name = "Gravity Well",
        Description = "Particles attracted to or repelled from the mouse cursor, simulating gravitational physics",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    private struct FrameConstants
    {
        public Vector2 ViewportSize;
        public float Time;
        public float HdrMultiplier;
        public Vector4 Padding;
    }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct ParticleInstance
    {
        public Vector2 Position;          // 8 bytes
        public Vector2 Velocity;          // 8 bytes
        public Vector4 Color;             // 16 bytes = 32
        public float Size;                // 4 bytes
        public float Mass;                // 4 bytes
        public float TrailAlpha;          // 4 bytes
        public float Lifetime;            // 4 bytes = 48
        public float RotationAngle;       // 4 bytes
        public float AngularVelocity;     // 4 bytes
        public float Padding1;            // 4 bytes
        public float Padding2;            // 4 bytes = 64
    }

    // Constants
    private const int MaxParticles = 500;
    private const float SofteningFactor = 100f; // Prevents singularity at r=0

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _particleBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Particle management (CPU side)
    private readonly ParticleInstance[] _particles = new ParticleInstance[MaxParticles];
    private readonly ParticleInstance[] _gpuParticles = new ParticleInstance[MaxParticles];
    private int _activeParticleCount;

    // Configuration fields (gw_ prefix for gravity well)
    private int _particleCount = 100;
    private float _particleSize = 8f;
    private float _gravityStrength = 50000f;
    private GravityMode _gravityMode = GravityMode.Attract;
    private float _orbitSpeed = 200f;
    private float _damping = 0.98f;
    private Vector4 _particleColor = new(0.2f, 0.8f, 1.0f, 1f);
    private bool _trailEnabled = true;
    private float _trailLength = 0.3f;
    private bool _randomColors = false;
    private float _hdrMultiplier = 1.0f;

    // Public properties for UI binding
    public int ParticleCount { get => _particleCount; set => _particleCount = Math.Clamp(value, 50, MaxParticles); }
    public float ParticleSize { get => _particleSize; set => _particleSize = value; }
    public float GravityStrength { get => _gravityStrength; set => _gravityStrength = value; }
    public GravityMode GravityMode { get => _gravityMode; set => _gravityMode = value; }
    public float OrbitSpeed { get => _orbitSpeed; set => _orbitSpeed = value; }
    public float Damping { get => _damping; set => _damping = value; }
    public Vector4 ParticleColor { get => _particleColor; set => _particleColor = value; }
    public bool TrailEnabled { get => _trailEnabled; set => _trailEnabled = value; }
    public float TrailLength { get => _trailLength; set => _trailLength = value; }
    public bool RandomColors { get => _randomColors; set => _randomColors = value; }
    public float HdrMultiplier { get => _hdrMultiplier; set => _hdrMultiplier = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("GravityWellShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FrameConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create particle structured buffer
        _particleBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<ParticleInstance>() * MaxParticles,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<ParticleInstance>()
        });

        // Initialize particles
        InitializeParticles(context.ViewportSize);
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("gw_particleCount", out int count))
            _particleCount = count;
        if (Configuration.TryGet("gw_particleSize", out float size))
            _particleSize = size;
        if (Configuration.TryGet("gw_gravityStrength", out float strength))
            _gravityStrength = strength;
        if (Configuration.TryGet("gw_gravityMode", out int mode))
            _gravityMode = (GravityMode)mode;
        if (Configuration.TryGet("gw_orbitSpeed", out float orbit))
            _orbitSpeed = orbit;
        if (Configuration.TryGet("gw_damping", out float damp))
            _damping = damp;
        if (Configuration.TryGet("gw_particleColor", out Vector4 color))
            _particleColor = color;
        if (Configuration.TryGet("gw_trailEnabled", out bool trail))
            _trailEnabled = trail;
        if (Configuration.TryGet("gw_trailLength", out float trailLen))
            _trailLength = trailLen;
        if (Configuration.TryGet("gw_randomColors", out bool randomCol))
            _randomColors = randomCol;
        if (Configuration.TryGet("gw_hdrMultiplier", out float hdr))
            _hdrMultiplier = hdr;

        // Adjust particle count
        if (_activeParticleCount < _particleCount)
        {
            // Spawn more particles
            while (_activeParticleCount < _particleCount)
            {
                SpawnParticle(ViewportSize / 2f);
            }
        }
        else if (_activeParticleCount > _particleCount)
        {
            // Remove excess particles
            _activeParticleCount = _particleCount;
        }
    }

    private void InitializeParticles(Vector2 viewportSize)
    {
        _activeParticleCount = _particleCount;
        Vector2 center = viewportSize / 2f;

        for (int i = 0; i < _particleCount; i++)
        {
            // Random position around center
            float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
            float radius = 100f + Random.Shared.NextSingle() * 300f;
            Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

            ref var p = ref _particles[i];
            p.Position = center + offset;
            p.Velocity = Vector2.Zero;
            p.Size = _particleSize * (0.7f + Random.Shared.NextSingle() * 0.6f);
            p.Mass = 0.5f + Random.Shared.NextSingle() * 0.5f;
            p.Color = GetParticleColor();
            p.TrailAlpha = 0f;
            p.Lifetime = 1f;
            p.RotationAngle = Random.Shared.NextSingle() * MathF.PI * 2f;
            p.AngularVelocity = (Random.Shared.NextSingle() - 0.5f) * 2f;
            p.Padding1 = 0f;
            p.Padding2 = 0f;
        }
    }

    private void SpawnParticle(Vector2 center)
    {
        if (_activeParticleCount >= MaxParticles)
            return;

        ref var p = ref _particles[_activeParticleCount];
        _activeParticleCount++;

        // Random position around center
        float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
        float radius = 100f + Random.Shared.NextSingle() * 300f;
        Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

        p.Position = center + offset;
        p.Velocity = Vector2.Zero;
        p.Size = _particleSize * (0.7f + Random.Shared.NextSingle() * 0.6f);
        p.Mass = 0.5f + Random.Shared.NextSingle() * 0.5f;
        p.Color = GetParticleColor();
        p.TrailAlpha = 0f;
        p.Lifetime = 1f;
        p.RotationAngle = Random.Shared.NextSingle() * MathF.PI * 2f;
        p.AngularVelocity = (Random.Shared.NextSingle() - 0.5f) * 2f;
        p.Padding1 = 0f;
        p.Padding2 = 0f;
    }

    private Vector4 GetParticleColor()
    {
        if (_randomColors)
        {
            float hue = Random.Shared.NextSingle();
            return HueToRgb(hue);
        }
        return _particleColor;
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

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        Vector2 cursorPos = mouseState.Position;

        // Update all particles
        for (int i = 0; i < _activeParticleCount; i++)
        {
            UpdateParticle(ref _particles[i], cursorPos, deltaTime);
        }
    }

    private void UpdateParticle(ref ParticleInstance particle, Vector2 cursorPos, float deltaTime)
    {
        // Calculate direction to cursor
        Vector2 toCursor = cursorPos - particle.Position;
        float distance = toCursor.Length();

        // Prevent division by zero
        if (distance < 1f)
            distance = 1f;

        // Calculate gravitational force: F = G * m / (r^2 + softening)
        float forceMagnitude = _gravityStrength * particle.Mass / (distance * distance + SofteningFactor);

        // Apply force based on mode
        Vector2 acceleration = Vector2.Zero;

        switch (_gravityMode)
        {
            case GravityMode.Attract:
                // Pull toward cursor
                acceleration = Vector2.Normalize(toCursor) * forceMagnitude;
                break;

            case GravityMode.Repel:
                // Push away from cursor
                acceleration = -Vector2.Normalize(toCursor) * forceMagnitude;
                break;

            case GravityMode.Orbit:
                // Attract to cursor
                acceleration = Vector2.Normalize(toCursor) * forceMagnitude;
                // Add perpendicular component for orbital motion
                Vector2 perpendicular = new Vector2(-toCursor.Y, toCursor.X);
                if (perpendicular.Length() > 0.001f)
                {
                    acceleration += Vector2.Normalize(perpendicular) * _orbitSpeed;
                }
                break;
        }

        // Apply acceleration
        particle.Velocity += acceleration * deltaTime;

        // Apply damping (energy loss)
        particle.Velocity *= _damping;

        // Update position
        particle.Position += particle.Velocity * deltaTime;

        // Update rotation
        particle.RotationAngle += particle.AngularVelocity * deltaTime;

        // Calculate trail alpha based on velocity
        float speed = particle.Velocity.Length();
        particle.TrailAlpha = _trailEnabled ? MathF.Min(speed / 500f, 1f) * _trailLength : 0f;

        // Wrap around screen edges
        if (particle.Position.X < -100f) particle.Position.X = ViewportSize.X + 100f;
        if (particle.Position.X > ViewportSize.X + 100f) particle.Position.X = -100f;
        if (particle.Position.Y < -100f) particle.Position.Y = ViewportSize.Y + 100f;
        if (particle.Position.Y > ViewportSize.Y + 100f) particle.Position.Y = -100f;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeParticleCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Copy particles to GPU buffer
        for (int i = 0; i < MaxParticles; i++)
        {
            if (i < _activeParticleCount)
            {
                _gpuParticles[i] = _particles[i];
            }
            else
            {
                _gpuParticles[i] = default;
            }
        }
        context.UpdateBuffer(_particleBuffer!, (ReadOnlySpan<ParticleInstance>)_gpuParticles.AsSpan());

        // Update constant buffer
        var constants = new FrameConstants
        {
            ViewportSize = context.ViewportSize,
            Time = currentTime,
            HdrMultiplier = context.HdrPeakBrightness * _hdrMultiplier,
            Padding = Vector4.Zero
        };
        context.UpdateBuffer(_constantBuffer!, constants);

        // Set up rendering state
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _particleBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _particleBuffer!);
        context.SetBlendState(BlendMode.Additive);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced particles (6 vertices per quad, one instance per particle)
        context.DrawInstanced(6, MaxParticles, 0, 0);

        // Restore blend state
        context.SetBlendState(BlendMode.Alpha);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _particleBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.GravityWell.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

public enum GravityMode
{
    Attract = 0,
    Repel = 1,
    Orbit = 2
}
