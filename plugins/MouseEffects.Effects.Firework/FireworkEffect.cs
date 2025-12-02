using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

using MouseButtons = MouseEffects.Core.Input.MouseButtons;

namespace MouseEffects.Effects.Firework;

public sealed class FireworkEffect : EffectBase
{
    private struct FireworkParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector4 Color;
        public float Size;
        public float Life;
        public float MaxLife;
        public bool CanExplode;
        public bool HasExploded;
    }

    private struct FireworkRocket
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector4 Color;
        public float Size;
        public float Age;
        public float TargetY;
        public bool IsActive;
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

    [StructLayout(LayoutKind.Sequential, Size = 48)]
    private struct FrameData
    {
        public Vector2 ViewportSize;
        public float Time;
        public float GlowIntensity;
        public float EnableTrails;
        public float TrailLength;
        public float Padding1;
        public float Padding2;
        public float Padding3;
        public float Padding4;
        public float Padding5;
        public float Padding6;
    }

    private const int MaxParticlesLimit = 15000;
    private const int MaxRockets = 200;

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "firework",
        Name = "Firework",
        Description = "Creates stunning firework explosions with colorful particles and trails",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    private IBuffer? _particleBuffer;
    private IBuffer? _frameDataBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    private readonly FireworkParticle[] _particles = new FireworkParticle[MaxParticlesLimit];
    private readonly ParticleGPU[] _gpuParticles = new ParticleGPU[MaxParticlesLimit];
    private int _nextParticle;
    private int _activeParticleCount;

    private readonly FireworkRocket[] _rockets = new FireworkRocket[MaxRockets];
    private Vector2 _lastMousePos;
    private float _lastSpawnDistance;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;
    private float _rainbowHue;
    private float _rocketRainbowHue;
    private float _viewportHeight = 1080f;

    // Configuration values
    private int _maxParticles = 5000;
    private int _maxFireworks = 50;
    private float _particleLifespan = 2.5f;
    private bool _spawnOnLeftClick = true;
    private bool _spawnOnRightClick;
    private int _minParticlesPerFirework = 50;
    private int _maxParticlesPerFirework = 150;
    private float _clickExplosionForce = 300f;
    private bool _spawnOnMove;
    private float _moveSpawnDistance = 100f;
    private float _moveExplosionForce = 150f;
    private float _minParticleSize = 3f;
    private float _maxParticleSize = 8f;
    private float _glowIntensity = 0.8f;
    private bool _enableTrails = true;
    private float _trailLength = 0.3f;
    private float _gravity = 150f;
    private float _drag = 0.98f;
    private float _spreadAngle = 360f;
    private bool _rainbowMode = true;
    private float _rainbowSpeed = 0.5f;
    private Vector4 _primaryColor = new(1f, 0.3f, 0.1f, 1f);
    private Vector4 _secondaryColor = new(1f, 0.8f, 0.2f, 1f);
    private bool _useRandomColors = true;
    private bool _enableSecondaryExplosion = true;
    private float _secondaryExplosionDelay = 0.8f;
    private int _secondaryParticleCount = 20;
    private float _secondaryExplosionForce = 100f;

    // Rocket settings
    private bool _enableRocketMode;
    private float _rocketSpeed = 500f;
    private float _rocketMinAltitude = 0.1f;
    private float _rocketMaxAltitude = 0.3f;
    private float _rocketMaxFuseTime = 3.0f;
    private float _rocketSize = 12f;
    private bool _rocketRainbowMode = true;
    private float _rocketRainbowSpeed = 0.5f;
    private Vector4 _rocketPrimaryColor = new(1f, 0.8f, 0.2f, 1f);
    private Vector4 _rocketSecondaryColor = new(1f, 0.4f, 0.1f, 1f);
    private bool _rocketUseRandomColors = true;

    public override EffectMetadata Metadata => _metadata;

    protected override void OnInitialize(IRenderContext context)
    {
        var particleDesc = new BufferDescription
        {
            Size = MaxParticlesLimit * Marshal.SizeOf<ParticleGPU>(),
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<ParticleGPU>()
        };
        _particleBuffer = context.CreateBuffer(particleDesc, default);

        var frameDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<FrameData>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _frameDataBuffer = context.CreateBuffer(frameDesc, default);

        string shaderSource = LoadEmbeddedShader("FireworkShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        for (int i = 0; i < MaxParticlesLimit; i++)
            _particles[i] = new FireworkParticle { Life = 0f };

        for (int j = 0; j < MaxRockets; j++)
            _rockets[j] = new FireworkRocket { IsActive = false };
    }

    protected override void OnConfigurationChanged()
    {
        // General settings
        if (Configuration.TryGet("maxParticles", out int maxPart))
            _maxParticles = Math.Clamp(maxPart, 1000, MaxParticlesLimit);

        if (Configuration.TryGet("maxFireworks", out int maxFw))
            _maxFireworks = Math.Clamp(maxFw, 1, MaxRockets);

        if (Configuration.TryGet("particleLifespan", out float lifespan))
            _particleLifespan = lifespan;

        if (Configuration.TryGet("spawnOnLeftClick", out bool leftClick))
            _spawnOnLeftClick = leftClick;

        if (Configuration.TryGet("spawnOnRightClick", out bool rightClick))
            _spawnOnRightClick = rightClick;

        if (Configuration.TryGet("minParticlesPerFirework", out int minPart))
            _minParticlesPerFirework = minPart;

        if (Configuration.TryGet("maxParticlesPerFirework", out int maxPartFw))
            _maxParticlesPerFirework = maxPartFw;

        if (Configuration.TryGet("clickExplosionForce", out float clickForce))
            _clickExplosionForce = clickForce;

        if (Configuration.TryGet("spawnOnMove", out bool spawnMove))
            _spawnOnMove = spawnMove;

        if (Configuration.TryGet("moveSpawnDistance", out float moveDist))
            _moveSpawnDistance = moveDist;

        if (Configuration.TryGet("moveExplosionForce", out float moveForce))
            _moveExplosionForce = moveForce;

        if (Configuration.TryGet("minParticleSize", out float minSize))
            _minParticleSize = minSize;

        if (Configuration.TryGet("maxParticleSize", out float maxSize))
            _maxParticleSize = maxSize;

        if (Configuration.TryGet("glowIntensity", out float glow))
            _glowIntensity = glow;

        if (Configuration.TryGet("enableTrails", out bool trails))
            _enableTrails = trails;

        if (Configuration.TryGet("trailLength", out float trailLen))
            _trailLength = trailLen;

        if (Configuration.TryGet("gravity", out float gravity))
            _gravity = gravity;

        if (Configuration.TryGet("drag", out float drag))
            _drag = drag;

        if (Configuration.TryGet("spreadAngle", out float spread))
            _spreadAngle = spread;

        // Firework color settings
        if (Configuration.TryGet("rainbowMode", out bool rainbow))
            _rainbowMode = rainbow;

        if (Configuration.TryGet("rainbowSpeed", out float rainbowSpd))
            _rainbowSpeed = rainbowSpd;

        if (Configuration.TryGet("primaryColor", out Vector4 primary))
            _primaryColor = primary;

        if (Configuration.TryGet("secondaryColor", out Vector4 secondary))
            _secondaryColor = secondary;

        if (Configuration.TryGet("useRandomColors", out bool randomColors))
            _useRandomColors = randomColors;

        // Secondary explosion
        if (Configuration.TryGet("enableSecondaryExplosion", out bool secondaryExp))
            _enableSecondaryExplosion = secondaryExp;

        if (Configuration.TryGet("secondaryExplosionDelay", out float secondaryDelay))
            _secondaryExplosionDelay = secondaryDelay;

        if (Configuration.TryGet("secondaryParticleCount", out int secondaryCount))
            _secondaryParticleCount = secondaryCount;

        if (Configuration.TryGet("secondaryExplosionForce", out float secondaryForce))
            _secondaryExplosionForce = secondaryForce;

        // Rocket settings
        if (Configuration.TryGet("enableRocketMode", out bool rocketMode))
            _enableRocketMode = rocketMode;

        if (Configuration.TryGet("rocketSpeed", out float rocketSpd))
            _rocketSpeed = rocketSpd;

        if (Configuration.TryGet("rocketMinAltitude", out float minAlt))
            _rocketMinAltitude = minAlt;

        if (Configuration.TryGet("rocketMaxAltitude", out float maxAlt))
            _rocketMaxAltitude = maxAlt;

        if (Configuration.TryGet("rocketMaxFuseTime", out float maxFuse))
            _rocketMaxFuseTime = maxFuse;

        if (Configuration.TryGet("rocketSize", out float rocketSize))
            _rocketSize = rocketSize;

        if (Configuration.TryGet("rocketRainbowMode", out bool rocketRainbow))
            _rocketRainbowMode = rocketRainbow;

        if (Configuration.TryGet("rocketRainbowSpeed", out float rocketRainbowSpd))
            _rocketRainbowSpeed = rocketRainbowSpd;

        if (Configuration.TryGet("rocketPrimaryColor", out Vector4 rocketPrimary))
            _rocketPrimaryColor = rocketPrimary;

        if (Configuration.TryGet("rocketSecondaryColor", out Vector4 rocketSecondary))
            _rocketSecondaryColor = rocketSecondary;

        if (Configuration.TryGet("rocketUseRandomColors", out bool rocketRandomColors))
            _rocketUseRandomColors = rocketRandomColors;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float dt = (float)gameTime.DeltaTime.TotalSeconds;
        float totalTime = (float)gameTime.TotalTime.TotalSeconds;

        if (_rainbowMode)
        {
            _rainbowHue += _rainbowSpeed * dt;
            if (_rainbowHue > 1f) _rainbowHue -= 1f;
        }

        if (_rocketRainbowMode)
        {
            _rocketRainbowHue += _rocketRainbowSpeed * dt;
            if (_rocketRainbowHue > 1f) _rocketRainbowHue -= 1f;
        }

        UpdateRockets(dt, totalTime);
        UpdateParticles(dt, totalTime);

        bool leftPressed = mouseState.IsButtonPressed(MouseButtons.Left);
        bool rightPressed = mouseState.IsButtonPressed(MouseButtons.Right);

        if (_spawnOnLeftClick && leftPressed && !_wasLeftPressed)
        {
            int particleCount = Random.Shared.Next(_minParticlesPerFirework, _maxParticlesPerFirework + 1);
            SpawnFirework(mouseState.Position, particleCount, _clickExplosionForce, totalTime);
        }

        if (_spawnOnRightClick && rightPressed && !_wasRightPressed)
        {
            int particleCount = Random.Shared.Next(_minParticlesPerFirework, _maxParticlesPerFirework + 1);
            SpawnFirework(mouseState.Position, particleCount, _clickExplosionForce, totalTime);
        }

        _wasLeftPressed = leftPressed;
        _wasRightPressed = rightPressed;

        if (_spawnOnMove)
        {
            float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);
            _lastSpawnDistance += distanceFromLast;
            if (_lastSpawnDistance >= _moveSpawnDistance)
            {
                int moveParticleCount = Random.Shared.Next(_minParticlesPerFirework / 3, _maxParticlesPerFirework / 3 + 1);
                SpawnFirework(mouseState.Position, Math.Max(5, moveParticleCount), _moveExplosionForce, totalTime);
                _lastSpawnDistance = 0f;
            }
        }

        _lastMousePos = mouseState.Position;
    }

    private void UpdateRockets(float dt, float totalTime)
    {
        for (int i = 0; i < MaxRockets; i++)
        {
            ref FireworkRocket rocket = ref _rockets[i];
            if (!rocket.IsActive) continue;

            rocket.Age += dt;
            rocket.Position += rocket.Velocity * dt;
            rocket.Velocity.Y += _gravity * 0.3f * dt;

            if (_enableTrails)
                SpawnRocketTrail(rocket.Position, rocket.Color);

            // Check explosion conditions:
            // 1. Reached target altitude (Y position)
            // 2. OR exceeded max fuse time (safety fallback)
            bool reachedTarget = rocket.Position.Y <= rocket.TargetY;
            bool timedOut = rocket.Age >= _rocketMaxFuseTime;

            if (reachedTarget || timedOut)
            {
                int particleCount = Random.Shared.Next(_minParticlesPerFirework, _maxParticlesPerFirework + 1);
                SpawnExplosion(rocket.Position, particleCount, _clickExplosionForce, rocket.Color, totalTime, isSecondary: false);
                rocket.IsActive = false;
            }
        }
    }

    private void UpdateParticles(float dt, float totalTime)
    {
        _activeParticleCount = 0;
        for (int i = 0; i < _maxParticles; i++)
        {
            ref FireworkParticle p = ref _particles[i];
            if (p.Life <= 0f) continue;

            p.Life -= dt;
            if (p.Life <= 0f) continue;

            p.Position += p.Velocity * dt;
            p.Velocity.Y += _gravity * dt;
            p.Velocity *= _drag;

            if (_enableSecondaryExplosion && !p.HasExploded && p.CanExplode &&
                p.Life / p.MaxLife < 1f - _secondaryExplosionDelay / p.MaxLife)
            {
                p.HasExploded = true;
                SpawnExplosion(p.Position, _secondaryParticleCount, _secondaryExplosionForce, p.Color, totalTime, isSecondary: true);
            }

            _activeParticleCount++;
        }
    }

    private void SpawnFirework(Vector2 position, int particleCount, float force, float totalTime)
    {
        if (_enableRocketMode)
        {
            SpawnRocket(position, totalTime);
            return;
        }

        Vector4 color = GetFireworkColor();
        SpawnExplosion(position, particleCount, force, color, totalTime, isSecondary: false);
    }

    private void SpawnRocket(Vector2 position, float totalTime)
    {
        for (int i = 0; i < _maxFireworks; i++)
        {
            if (!_rockets[i].IsActive)
            {
                ref FireworkRocket rocket = ref _rockets[i];
                rocket.Position = position;
                rocket.Velocity = new Vector2((Random.Shared.NextSingle() - 0.5f) * 100f, -_rocketSpeed);
                rocket.Color = GetRocketColor();
                rocket.Size = _rocketSize;
                rocket.Age = 0f;

                // Calculate target Y position based on altitude settings (% from top)
                // Random value between minAltitude and maxAltitude
                float altitudeRange = _rocketMaxAltitude - _rocketMinAltitude;
                float randomAltitude = _rocketMinAltitude + Random.Shared.NextSingle() * altitudeRange;
                rocket.TargetY = _viewportHeight * randomAltitude;

                // If rocket is launched above the explosion zone, set minimal target
                // so it explodes almost immediately
                if (position.Y <= rocket.TargetY)
                {
                    rocket.TargetY = position.Y - 10f; // Explode after minimal travel
                }

                rocket.IsActive = true;
                break;
            }
        }
    }

    private void SpawnRocketTrail(Vector2 position, Vector4 color)
    {
        for (int i = 0; i < 2; i++)
        {
            SpawnParticle(
                position,
                new Vector2((Random.Shared.NextSingle() - 0.5f) * 20f, Random.Shared.NextSingle() * 30f + 10f),
                color * 0.5f,
                0.2f,
                _minParticleSize * 0.4f,
                canExplode: false);
        }
    }

    private void SpawnExplosion(Vector2 position, int count, float force, Vector4 baseColor, float totalTime, bool isSecondary)
    {
        float spreadRad = _spreadAngle * MathF.PI / 180f;
        float startAngle = Random.Shared.NextSingle() * MathF.PI * 2f;

        for (int i = 0; i < count; i++)
        {
            float angle = _spreadAngle >= 360f
                ? startAngle + (float)i / count * MathF.PI * 2f
                : startAngle - spreadRad / 2f + Random.Shared.NextSingle() * spreadRad;

            float particleForce = force * (0.5f + Random.Shared.NextSingle() * 0.5f);
            Vector2 velocity = new(MathF.Cos(angle) * particleForce, MathF.Sin(angle) * particleForce);

            Vector4 color = baseColor;
            if (_useRandomColors && !isSecondary)
            {
                float mixFactor = Random.Shared.NextSingle() * 0.5f;
                color = Vector4.Lerp(baseColor, _secondaryColor, mixFactor);
            }

            color.X = MathF.Max(0f, MathF.Min(1f, color.X + (Random.Shared.NextSingle() - 0.5f) * 0.2f));
            color.Y = MathF.Max(0f, MathF.Min(1f, color.Y + (Random.Shared.NextSingle() - 0.5f) * 0.2f));
            color.Z = MathF.Max(0f, MathF.Min(1f, color.Z + (Random.Shared.NextSingle() - 0.5f) * 0.2f));

            float size = _minParticleSize + Random.Shared.NextSingle() * (_maxParticleSize - _minParticleSize);
            float lifespan = _particleLifespan * (0.7f + Random.Shared.NextSingle() * 0.3f);

            if (isSecondary)
            {
                size *= 0.6f;
                lifespan *= 0.5f;
            }

            SpawnParticle(position, velocity, color, lifespan, size, !isSecondary && _enableSecondaryExplosion);
        }
    }

    private void SpawnParticle(Vector2 position, Vector2 velocity, Vector4 color, float lifespan, float size, bool canExplode)
    {
        int startIndex = _nextParticle;
        do
        {
            ref FireworkParticle p = ref _particles[_nextParticle];
            _nextParticle = (_nextParticle + 1) % _maxParticles;

            if (p.Life <= 0f)
            {
                p.Position = position;
                p.Velocity = velocity;
                p.Color = color;
                p.Life = lifespan;
                p.MaxLife = lifespan;
                p.Size = size;
                p.CanExplode = canExplode;
                p.HasExploded = false;
                break;
            }
        } while (_nextParticle != startIndex);
    }

    private Vector4 GetFireworkColor()
    {
        if (_rainbowMode)
            return HueToRgb(_rainbowHue + Random.Shared.NextSingle() * 0.1f);

        if (_useRandomColors)
            return HueToRgb(Random.Shared.NextSingle());

        return _primaryColor;
    }

    private Vector4 GetRocketColor()
    {
        if (_rocketRainbowMode)
            return HueToRgb(_rocketRainbowHue + Random.Shared.NextSingle() * 0.1f);

        if (_rocketUseRandomColors)
            return HueToRgb(Random.Shared.NextSingle());

        return _rocketPrimaryColor;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null)
            return;

        // Store viewport height for rocket altitude calculations
        _viewportHeight = context.ViewportSize.Y;

        // Count active rockets
        int activeRocketCount = 0;
        for (int i = 0; i < MaxRockets; i++)
        {
            if (_rockets[i].IsActive)
                activeRocketCount++;
        }

        if (_activeParticleCount == 0 && activeRocketCount == 0)
            return;

        float totalTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        var frameData = new FrameData
        {
            ViewportSize = context.ViewportSize,
            Time = totalTime,
            GlowIntensity = _glowIntensity,
            EnableTrails = _enableTrails ? 1f : 0f,
            TrailLength = _trailLength
        };
        context.UpdateBuffer(_frameDataBuffer!, frameData);

        int activeIndex = 0;

        // Add regular particles to GPU buffer
        for (int i = 0; i < _maxParticles && activeIndex < _maxParticles; i++)
        {
            ref FireworkParticle p = ref _particles[i];
            if (p.Life <= 0f) continue;

            _gpuParticles[activeIndex] = new ParticleGPU
            {
                Position = p.Position,
                Velocity = p.Velocity,
                Color = p.Color,
                Size = p.Size,
                Life = p.Life,
                MaxLife = p.MaxLife
            };
            activeIndex++;
        }

        // Add rockets to GPU buffer
        for (int i = 0; i < MaxRockets && activeIndex < _maxParticles; i++)
        {
            ref FireworkRocket rocket = ref _rockets[i];
            if (!rocket.IsActive) continue;

            // Calculate life based on distance to target (for visual fade effect)
            float distanceToTarget = rocket.Position.Y - rocket.TargetY;
            float totalDistance = _viewportHeight * (_rocketMaxAltitude - _rocketMinAltitude);
            float rocketLife = Math.Max(0.1f, distanceToTarget / Math.Max(1f, totalDistance));

            _gpuParticles[activeIndex] = new ParticleGPU
            {
                Position = rocket.Position,
                Velocity = rocket.Velocity,
                Color = rocket.Color,
                Size = rocket.Size,
                Life = rocketLife,
                MaxLife = 1f
            };
            activeIndex++;
        }

        // Clear remaining slots
        for (int j = activeIndex; j < _maxParticles; j++)
        {
            _gpuParticles[j] = default;
        }

        context.UpdateBuffer(_particleBuffer!, (ReadOnlySpan<ParticleGPU>)_gpuParticles.AsSpan(0, _maxParticles));
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _frameDataBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _frameDataBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _particleBuffer!);
        context.SetBlendState(BlendMode.Additive);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.DrawInstanced(6, _maxParticles, 0, 0);
        context.SetBlendState(BlendMode.Alpha);
    }

    protected override void OnDispose()
    {
        _particleBuffer?.Dispose();
        _frameDataBuffer?.Dispose();
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
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

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(FireworkEffect).Assembly;
        string resourceName = $"MouseEffects.Effects.Firework.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
