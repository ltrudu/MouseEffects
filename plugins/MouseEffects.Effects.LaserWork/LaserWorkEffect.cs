using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.LaserWork;

/// <summary>
/// Effect that shoots glowing lasers from the mouse pointer in multiple directions.
/// Lasers bounce off screen edges and fade out over their lifespan.
/// Lasers can collide with each other and explode into multiple new lasers.
/// </summary>
public sealed class LaserWorkEffect : EffectBase
{
    private const int MaxLasers = 500;
    private const float MinMovementThreshold = 5f;
    private const float CollisionCooldown = 0.1f; // Prevent rapid chain reactions

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "laser-work",
        Name = "Laser Work",
        Description = "Shoots glowing lasers from the mouse pointer that bounce off screen edges",
        Version = new Version(1, 0, 0),
        Author = "MouseEffects",
        Category = EffectCategory.Visual
    };

    // GPU Resources
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _laserBuffer;
    private IBuffer? _frameDataBuffer;

    // Laser data
    private readonly Laser[] _lasers = new Laser[MaxLasers];
    private int _activeLaserCount;
    private float _timeSinceLastEmission;

    // Explosion queue (to avoid modifying array while iterating)
    private readonly List<ExplosionRequest> _explosionQueue = new();

    // Rainbow state
    private float _rainbowHue;
    private float _rainbowDirection = 1f;

    // Previous mouse state for direction calculation
    private Vector2 _previousMousePos;
    private Vector2 _lastMovementDirection;
    private bool _hasValidDirection;

    // Configuration values (cached for performance)
    private float _lasersPerSecond = 20f;
    private float _minLaserLength = 30f;
    private float _maxLaserLength = 70f;
    private float _minLaserWidth = 2f;
    private float _maxLaserWidth = 6f;
    private bool _autoShrink;
    private float _laserSpeed = 400f;
    private float _laserLifespan = 3f;
    private float _minAlpha = 0.1f;
    private float _maxAlpha = 1.0f;
    private float _glowIntensity = 0.5f;
    private Vector4 _laserColor = new(1f, 0.2f, 0.2f, 1f);
    private bool _rainbowMode;
    private float _rainbowSpeed = 1f;
    private bool _shootForward = true;
    private bool _shootBackward = true;
    private bool _shootLeft = true;
    private bool _shootRight = true;

    // Collision explosion settings
    private bool _enableCollisionExplosion;
    private int _explosionLaserCount = 8;
    private float _explosionLifespanMultiplier = 0.5f;
    private bool _explosionLasersCanCollide;
    private int _maxCollisionCount = 3;

    public override EffectMetadata Metadata => _metadata;

    protected override void OnInitialize(IRenderContext context)
    {
        // Create laser buffer (structured buffer)
        var laserDesc = new BufferDescription
        {
            Size = MaxLasers * Marshal.SizeOf<LaserGPU>(),
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<LaserGPU>()
        };
        _laserBuffer = context.CreateBuffer(laserDesc);

        // Create frame data buffer
        var frameDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<FrameData>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _frameDataBuffer = context.CreateBuffer(frameDesc);

        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("LaserShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Initialize lasers
        for (int i = 0; i < MaxLasers; i++)
        {
            _lasers[i] = new Laser { Life = 0 };
        }
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("lasersPerSecond", out float lps))
            _lasersPerSecond = MathF.Max(1f, lps); // Minimum 1 laser per second
        if (Configuration.TryGet("minLaserLength", out float minLen))
            _minLaserLength = minLen;
        if (Configuration.TryGet("maxLaserLength", out float maxLen))
            _maxLaserLength = maxLen;
        if (Configuration.TryGet("minLaserWidth", out float minW))
            _minLaserWidth = minW;
        if (Configuration.TryGet("maxLaserWidth", out float maxW))
            _maxLaserWidth = maxW;
        if (Configuration.TryGet("autoShrink", out bool shrink))
            _autoShrink = shrink;
        if (Configuration.TryGet("laserSpeed", out float speed))
            _laserSpeed = speed;
        if (Configuration.TryGet("laserLifespan", out float life))
            _laserLifespan = life;
        if (Configuration.TryGet("minAlpha", out float minA))
            _minAlpha = minA;
        if (Configuration.TryGet("maxAlpha", out float maxA))
            _maxAlpha = maxA;
        if (Configuration.TryGet("glowIntensity", out float glow))
            _glowIntensity = glow;
        if (Configuration.TryGet("laserColor", out Vector4 color))
            _laserColor = color;
        if (Configuration.TryGet("rainbowMode", out bool rainbow))
            _rainbowMode = rainbow;
        if (Configuration.TryGet("rainbowSpeed", out float rainbowSpd))
            _rainbowSpeed = rainbowSpd;
        if (Configuration.TryGet("shootForward", out bool fwd))
            _shootForward = fwd;
        if (Configuration.TryGet("shootBackward", out bool bwd))
            _shootBackward = bwd;
        if (Configuration.TryGet("shootLeft", out bool left))
            _shootLeft = left;
        if (Configuration.TryGet("shootRight", out bool right))
            _shootRight = right;

        // Collision explosion settings
        if (Configuration.TryGet("enableCollisionExplosion", out bool explode))
            _enableCollisionExplosion = explode;
        if (Configuration.TryGet("explosionLaserCount", out float explodeCount))
            _explosionLaserCount = (int)explodeCount;
        if (Configuration.TryGet("explosionLifespanMultiplier", out float lifeMult))
            _explosionLifespanMultiplier = lifeMult;
        if (Configuration.TryGet("explosionLasersCanCollide", out bool canCollide))
            _explosionLasersCanCollide = canCollide;
        if (Configuration.TryGet("maxCollisionCount", out float maxColl))
            _maxCollisionCount = (int)maxColl;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        var dt = (float)gameTime.DeltaTime.TotalSeconds;

        // Update rainbow hue if in rainbow mode
        if (_rainbowMode)
        {
            _rainbowHue += _rainbowSpeed * dt * _rainbowDirection;

            if (_rainbowHue >= 1f)
            {
                _rainbowHue = 1f;
                _rainbowDirection = -1f;
            }
            else if (_rainbowHue <= 0f)
            {
                _rainbowHue = 0f;
                _rainbowDirection = 1f;
            }
        }

        // Calculate mouse movement direction
        Vector2 currentPos = mouseState.Position;
        Vector2 delta = currentPos - _previousMousePos;
        float movementMagnitude = delta.Length();

        if (movementMagnitude > MinMovementThreshold)
        {
            _lastMovementDirection = Vector2.Normalize(delta);
            _hasValidDirection = true;
        }

        _previousMousePos = currentPos;

        // Update emission timer
        _timeSinceLastEmission += dt;

        // Emit new lasers if mouse is moving
        float emissionCooldown = 1f / _lasersPerSecond;
        if (_hasValidDirection && movementMagnitude > MinMovementThreshold && _timeSinceLastEmission >= emissionCooldown)
        {
            EmitLasers(currentPos);
            _timeSinceLastEmission = 0f;
        }

        // Update existing lasers
        UpdateLasers(dt);

        // Check for collisions if enabled
        if (_enableCollisionExplosion)
        {
            CheckCollisions();
            ProcessExplosions();
        }
    }

    private void EmitLasers(Vector2 position)
    {
        Vector2 forward = _lastMovementDirection;
        Vector2 backward = -forward;
        Vector2 left = new(-forward.Y, forward.X);
        Vector2 right = new(forward.Y, -forward.X);

        Vector4 color = _rainbowMode ? HueToRgb(_rainbowHue) : _laserColor;

        if (_shootForward) SpawnLaser(position, forward, color, false);
        if (_shootBackward) SpawnLaser(position, backward, color, false);
        if (_shootLeft) SpawnLaser(position, left, color, false);
        if (_shootRight) SpawnLaser(position, right, color, false);
    }

    private void SpawnLaser(Vector2 position, Vector2 direction, Vector4 color, bool isExplosion, float lifespanOverride = -1f)
    {
        // Find an inactive laser slot
        for (int i = 0; i < MaxLasers; i++)
        {
            if (_lasers[i].Life <= 0)
            {
                ref var laser = ref _lasers[i];
                laser.Position = position;
                laser.Direction = direction;
                laser.Color = color;
                float lifespan = lifespanOverride > 0 ? lifespanOverride : _laserLifespan;
                laser.Life = lifespan;
                laser.MaxLife = lifespan;
                laser.Length = RandomRange(_minLaserLength, _maxLaserLength);
                laser.Width = RandomRange(_minLaserWidth, _maxLaserWidth);
                laser.IsExplosion = isExplosion;
                laser.CollisionCooldown = isExplosion ? CollisionCooldown : 0f;

                // Set collision counter:
                // - Regular lasers always get max collisions
                // - Explosion lasers get 0 if "Collide Always" is unchecked, otherwise max collisions
                if (isExplosion)
                    laser.RemainingCollisions = _explosionLasersCanCollide ? _maxCollisionCount : 0;
                else
                    laser.RemainingCollisions = _maxCollisionCount;

                return;
            }
        }
    }

    private float RandomRange(float min, float max)
    {
        if (MathF.Abs(max - min) < 0.001f) return min;
        return min + Random.Shared.NextSingle() * (max - min);
    }

    private void UpdateLasers(float dt)
    {
        _activeLaserCount = 0;

        for (int i = 0; i < MaxLasers; i++)
        {
            ref var laser = ref _lasers[i];
            if (laser.Life <= 0) continue;

            // Update life
            laser.Life -= dt;
            if (laser.Life <= 0) continue;

            // Update collision cooldown
            if (laser.CollisionCooldown > 0)
                laser.CollisionCooldown -= dt;

            // Move laser
            laser.Position += laser.Direction * _laserSpeed * dt;

            _activeLaserCount++;
        }
    }

    private void CheckCollisions()
    {
        _explosionQueue.Clear();

        // Check each pair of active lasers for collision
        for (int i = 0; i < MaxLasers; i++)
        {
            ref var laserA = ref _lasers[i];
            // Skip if dead, on cooldown, or no remaining collisions
            if (laserA.Life <= 0 || laserA.CollisionCooldown > 0 || laserA.RemainingCollisions <= 0) continue;

            Vector2 a1 = laserA.Position;
            Vector2 a2 = laserA.Position + laserA.Direction * laserA.Length;

            for (int j = i + 1; j < MaxLasers; j++)
            {
                ref var laserB = ref _lasers[j];
                // Skip if dead, on cooldown, or no remaining collisions
                if (laserB.Life <= 0 || laserB.CollisionCooldown > 0 || laserB.RemainingCollisions <= 0) continue;

                Vector2 b1 = laserB.Position;
                Vector2 b2 = laserB.Position + laserB.Direction * laserB.Length;

                // Check line segment intersection
                if (LineSegmentsIntersect(a1, a2, b1, b2, out Vector2 intersection))
                {
                    // Decrement collision counters
                    laserA.RemainingCollisions--;
                    laserB.RemainingCollisions--;

                    // Mix colors of colliding lasers
                    Vector4 mixedColor = (laserA.Color + laserB.Color) * 0.5f;
                    mixedColor.W = 1f;

                    // Queue explosion at intersection point
                    _explosionQueue.Add(new ExplosionRequest
                    {
                        Position = intersection,
                        Color = mixedColor
                    });

                    // Kill both lasers
                    laserA.Life = 0;
                    laserB.Life = 0;

                    break; // LaserA is dead, move to next
                }
            }
        }
    }

    private void ProcessExplosions()
    {
        foreach (var explosion in _explosionQueue)
        {
            SpawnExplosion(explosion.Position, explosion.Color);
        }
    }

    private void SpawnExplosion(Vector2 position, Vector4 color)
    {
        if (_explosionLaserCount <= 0) return;

        float angleStep = MathF.PI * 2f / _explosionLaserCount;
        float startAngle = Random.Shared.NextSingle() * MathF.PI * 2f; // Random rotation

        for (int i = 0; i < _explosionLaserCount; i++)
        {
            float angle = startAngle + i * angleStep;
            Vector2 direction = new(MathF.Cos(angle), MathF.Sin(angle));

            // Use rainbow color for explosion if in rainbow mode, otherwise use mixed color
            Vector4 explosionColor = _rainbowMode ? HueToRgb(_rainbowHue + i * 0.1f) : color;

            // Explosion lasers have shorter lifespan
            float explosionLifespan = _laserLifespan * _explosionLifespanMultiplier;

            SpawnLaser(position, direction, explosionColor, true, explosionLifespan);
        }
    }

    /// <summary>
    /// Check if two line segments intersect and return the intersection point.
    /// </summary>
    private static bool LineSegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, out Vector2 intersection)
    {
        intersection = Vector2.Zero;

        float d1x = p2.X - p1.X;
        float d1y = p2.Y - p1.Y;
        float d2x = p4.X - p3.X;
        float d2y = p4.Y - p3.Y;

        float cross = d1x * d2y - d1y * d2x;

        // Lines are parallel
        if (MathF.Abs(cross) < 0.0001f)
            return false;

        float dx = p3.X - p1.X;
        float dy = p3.Y - p1.Y;

        float t = (dx * d2y - dy * d2x) / cross;
        float u = (dx * d1y - dy * d1x) / cross;

        // Check if intersection is within both segments
        if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
        {
            intersection = new Vector2(p1.X + t * d1x, p1.Y + t * d1y);
            return true;
        }

        return false;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null) return;

        var viewportSize = context.ViewportSize;

        // Apply bouncing now that we have viewport size
        for (int i = 0; i < MaxLasers; i++)
        {
            ref var laser = ref _lasers[i];
            if (laser.Life <= 0) continue;

            // Bounce off screen edges
            if (laser.Position.X < 0)
            {
                laser.Position.X = 0;
                laser.Direction.X = -laser.Direction.X;
            }
            else if (laser.Position.X > viewportSize.X)
            {
                laser.Position.X = viewportSize.X;
                laser.Direction.X = -laser.Direction.X;
            }

            if (laser.Position.Y < 0)
            {
                laser.Position.Y = 0;
                laser.Direction.Y = -laser.Direction.Y;
            }
            else if (laser.Position.Y > viewportSize.Y)
            {
                laser.Position.Y = viewportSize.Y;
                laser.Direction.Y = -laser.Direction.Y;
            }

            // Check laser end point too
            Vector2 laserEnd = laser.Position + laser.Direction * laser.Length;
            if (laserEnd.X < 0 || laserEnd.X > viewportSize.X)
            {
                laser.Direction.X = -laser.Direction.X;
            }
            if (laserEnd.Y < 0 || laserEnd.Y > viewportSize.Y)
            {
                laser.Direction.Y = -laser.Direction.Y;
            }
        }

        // Update frame data
        var frameData = new FrameData
        {
            ViewportSize = viewportSize,
            GlowIntensity = _glowIntensity,
            MinAlpha = _minAlpha,
            MaxAlpha = _maxAlpha,
            Padding = 0
        };
        context.UpdateBuffer(_frameDataBuffer!, frameData);

        // Convert lasers to GPU format
        var gpuLasers = new LaserGPU[MaxLasers];
        for (int i = 0; i < MaxLasers; i++)
        {
            ref var laser = ref _lasers[i];
            float lifeRatio = laser.MaxLife > 0 ? laser.Life / laser.MaxLife : 0;
            float alpha = MathF.Max(_minAlpha, _maxAlpha * lifeRatio);

            // Calculate size - shrink from original to 1 pixel if autoShrink is enabled
            float length = laser.Length;
            float width = laser.Width;
            if (_autoShrink)
            {
                // Lerp from original size to 1 pixel based on remaining life
                length = 1f + (laser.Length - 1f) * lifeRatio;
                width = 1f + (laser.Width - 1f) * lifeRatio;
            }

            gpuLasers[i] = new LaserGPU
            {
                Position = laser.Position,
                Direction = laser.Direction,
                Color = laser.Color with { W = alpha },
                Length = length,
                Width = width,
                Life = laser.Life,
                MaxLife = laser.MaxLife
            };
        }
        context.UpdateBuffer(_laserBuffer!, (ReadOnlySpan<LaserGPU>)gpuLasers);

        // Set shaders
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _frameDataBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _frameDataBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _laserBuffer!);

        // Enable additive blending for glow effect
        context.SetBlendState(BlendMode.Additive);

        // Draw instanced lasers (6 vertices per laser quad)
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.DrawInstanced(6, MaxLasers, 0, 0);

        context.SetBlendState(BlendMode.Opaque);
    }

    protected override void OnDispose()
    {
        _laserBuffer?.Dispose();
        _frameDataBuffer?.Dispose();
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
    }

    private static Vector4 HueToRgb(float hue)
    {
        // Wrap hue to 0-1 range
        hue = hue - MathF.Floor(hue);
        float h = hue * 6f;
        float x = 1f - MathF.Abs(h % 2f - 1f);

        Vector3 rgb = (int)h switch
        {
            0 => new Vector3(1, x, 0),
            1 => new Vector3(x, 1, 0),
            2 => new Vector3(0, 1, x),
            3 => new Vector3(0, x, 1),
            4 => new Vector3(x, 0, 1),
            _ => new Vector3(1, 0, x)
        };

        return new Vector4(rgb.X, rgb.Y, rgb.Z, 1f);
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(LaserWorkEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.LaserWork.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #region Data Structures

    private struct Laser
    {
        public Vector2 Position;
        public Vector2 Direction;
        public Vector4 Color;
        public float Life;
        public float MaxLife;
        public float Length;
        public float Width;
        public bool IsExplosion;
        public float CollisionCooldown;
        public int RemainingCollisions;
    }

    private struct ExplosionRequest
    {
        public Vector2 Position;
        public Vector4 Color;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LaserGPU
    {
        public Vector2 Position;
        public Vector2 Direction;
        public Vector4 Color;
        public float Length;
        public float Width;
        public float Life;
        public float MaxLife;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FrameData
    {
        public Vector2 ViewportSize;
        public float GlowIntensity;
        public float MinAlpha;
        public float MaxAlpha;
        public float Padding;
        public float Padding2;
        public float Padding3;
    }

    #endregion
}
