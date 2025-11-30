using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.ParticleTrail;

/// <summary>
/// Particle trail effect that spawns particles following the mouse cursor.
/// Uses GPU instancing for efficient rendering.
/// </summary>
public sealed class ParticleTrailEffect : EffectBase
{
    private const int MaxParticles = 1000;

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "particle-trail",
        Name = "Particle Trail",
        Description = "Creates colorful particle trails that follow the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    // GPU resources
    private IBuffer? _particleBuffer;
    private IBuffer? _frameDataBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Particle state
    private readonly Particle[] _particles = new Particle[MaxParticles];
    private readonly ParticleGPU[] _gpuParticles = new ParticleGPU[MaxParticles]; // Pooled to avoid allocation per frame
    private int _nextParticle;
    private Vector2 _lastMousePos;
    private float _spawnAccumulator;

    // Configuration
    private float _emissionRate = 100f;
    private float _particleLifetime = 1.5f;
    private float _particleSize = 8f;
    private float _spreadAngle = 0.5f;
    private float _initialSpeed = 50f;
    private Vector4 _startColor = new(1f, 0.6f, 0.2f, 1f);
    private Vector4 _endColor = new(1f, 0.2f, 0.8f, 0.5f);

    public override EffectMetadata Metadata => _metadata;

    protected override void OnInitialize(IRenderContext context)
    {
        // Create particle buffer (structured buffer)
        var particleDesc = new BufferDescription
        {
            Size = MaxParticles * Marshal.SizeOf<ParticleGPU>(),
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<ParticleGPU>()
        };
        _particleBuffer = context.CreateBuffer(particleDesc);

        // Create frame data buffer
        var frameDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<FrameData>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _frameDataBuffer = context.CreateBuffer(frameDesc);

        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("ParticleShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Initialize particles
        for (int i = 0; i < MaxParticles; i++)
        {
            _particles[i] = new Particle { Life = 0 };
        }
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("emissionRate", out float rate))
            _emissionRate = rate;
        if (Configuration.TryGet("particleLifetime", out float lifetime))
            _particleLifetime = lifetime;
        if (Configuration.TryGet("particleSize", out float size))
            _particleSize = size;
        if (Configuration.TryGet("spreadAngle", out float spread))
            _spreadAngle = spread;
        if (Configuration.TryGet("initialSpeed", out float speed))
            _initialSpeed = speed;
        if (Configuration.TryGet("startColor", out Vector4 start))
            _startColor = start;
        if (Configuration.TryGet("endColor", out Vector4 end))
            _endColor = end;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        var dt = (float)gameTime.DeltaTime.TotalSeconds;
        var totalTime = (float)gameTime.TotalTime.TotalSeconds;

        // Update existing particles
        for (int i = 0; i < MaxParticles; i++)
        {
            ref var p = ref _particles[i];
            if (p.Life <= 0) continue;

            p.Life -= dt;
            p.Position += p.Velocity * dt;
            p.Velocity *= 0.98f; // Drag
            p.Velocity.Y += 20f * dt; // Gravity
        }

        // Spawn new particles based on mouse movement
        var mouseDelta = mouseState.Position - _lastMousePos;
        var mouseSpeed = mouseDelta.Length();
        _lastMousePos = mouseState.Position;

        if (mouseSpeed > 0.5f)
        {
            _spawnAccumulator += dt * _emissionRate;
            while (_spawnAccumulator >= 1f)
            {
                SpawnParticle(mouseState.Position, mouseDelta, totalTime);
                _spawnAccumulator -= 1f;
            }
        }

        // Also spawn on click
        if (mouseState.IsButtonPressed(MouseButtons.Left))
        {
            for (int i = 0; i < 20; i++)
            {
                SpawnParticle(mouseState.Position, Vector2.Zero, totalTime);
            }
        }
    }

    private void SpawnParticle(Vector2 position, Vector2 direction, float time)
    {
        ref var p = ref _particles[_nextParticle];
        _nextParticle = (_nextParticle + 1) % MaxParticles;

        p.Position = position;
        p.Life = _particleLifetime;
        p.MaxLife = _particleLifetime;
        p.Size = _particleSize * (0.8f + 0.4f * Random.Shared.NextSingle());

        // Calculate velocity with spread
        var baseAngle = MathF.Atan2(direction.Y, direction.X) + MathF.PI; // Opposite direction
        var angle = baseAngle + (Random.Shared.NextSingle() - 0.5f) * _spreadAngle * 2;
        var speed = _initialSpeed * (0.5f + Random.Shared.NextSingle());

        p.Velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;

        // Interpolate color based on time
        var colorT = (MathF.Sin(time * 2) + 1) * 0.5f;
        p.Color = Vector4.Lerp(_startColor, _endColor, colorT);
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null) return;

        // Update frame data
        var frameData = new FrameData
        {
            ViewportSize = context.ViewportSize,
            Time = 0,
            Padding = 0
        };
        context.UpdateBuffer(_frameDataBuffer!, frameData);

        // Convert particles to GPU format (using pooled array - no allocation)
        for (int i = 0; i < MaxParticles; i++)
        {
            ref var p = ref _particles[i];
            _gpuParticles[i] = new ParticleGPU
            {
                Position = p.Position,
                Velocity = p.Velocity,
                Color = p.Color,
                Size = p.Size,
                Life = p.Life,
                MaxLife = p.MaxLife,
                Padding = 0
            };
        }
        context.UpdateBuffer(_particleBuffer!, (ReadOnlySpan<ParticleGPU>)_gpuParticles);

        // Set shaders
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _frameDataBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _particleBuffer!);

        // Enable alpha blending
        context.SetBlendState(BlendMode.Additive);

        // Draw instanced particles (6 vertices per particle quad)
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.DrawInstanced(6, MaxParticles, 0, 0);

        context.SetBlendState(BlendMode.Opaque);
    }

    protected override void OnDispose()
    {
        _particleBuffer?.Dispose();
        _frameDataBuffer?.Dispose();
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(ParticleTrailEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.ParticleTrail.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #region Data Structures

    private struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector4 Color;
        public float Size;
        public float Life;
        public float MaxLife;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ParticleGPU
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector4 Color;
        public float Size;
        public float Life;
        public float MaxLife;
        public float Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FrameData
    {
        public Vector2 ViewportSize;
        public float Time;
        public float Padding;
    }

    #endregion
}
