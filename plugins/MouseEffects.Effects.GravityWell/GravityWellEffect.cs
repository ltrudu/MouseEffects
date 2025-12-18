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
        Category = EffectCategory.Cosmic
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
    private const int TrailSegments = 16;  // Number of trail segments per particle
    private const float SofteningFactor = 100f; // Prevents singularity at r=0

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _particleBuffer;
    private IBuffer? _trailBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Particle management (CPU side)
    private readonly ParticleInstance[] _particles = new ParticleInstance[MaxParticles];
    private readonly ParticleInstance[] _gpuParticles = new ParticleInstance[MaxParticles];
    private readonly ParticleInstance[] _trailInstances = new ParticleInstance[MaxParticles * TrailSegments];
    private readonly Vector2[][] _trailHistory = new Vector2[MaxParticles][];
    private int _activeParticleCount;

    // Configuration fields (gw_ prefix for gravity well)
    private int _particleCount = 100;
    private float _particleSize = 8f;
    private float _gravityStrength = 50000f;
    private float _gravityRadius = 500f;
    private GravityMode _gravityMode = GravityMode.Attract;
    private float _orbitSpeed = 200f;
    private float _damping = 0.98f;
    private Vector4 _particleColor = new(0.2f, 0.8f, 1.0f, 1f);
    private bool _trailEnabled = true;
    private float _trailLength = 0.3f;
    private bool _randomColors = false;
    private bool _resetOnLeftClick = false;
    private bool _resetOnRightClick = false;
    private EdgeBehavior _edgeBehavior = EdgeBehavior.Teleport;

    // Trigger condition fields
    private bool _triggerAlwaysActive = true;
    private bool _triggerOnLeftMouseDown = false;
    private bool _triggerOnRightMouseDown = false;
    private bool _triggerOnMouseMove = false;

    // Drift settings (applies when gravity is inactive)
    private bool _driftEnabled = false;
    private float _driftAmount = 0.95f;

    // Time multiplier for mouse move trigger
    private float _mouseMoveTimeMultiplier = 1.0f;

    // Mouse state tracking for click detection
    private bool _wasLeftButtonDown = false;
    private bool _wasRightButtonDown = false;

    // Current cursor position (for Reset edge behavior)
    private Vector2 _currentCursorPos;

    // Previous cursor position (for mouse move detection)
    private Vector2 _previousCursorPos;
    private bool _isMouseMoving = false;
    private const float MouseMoveThreshold = 2f;

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
    public EdgeBehavior EdgeBehavior { get => _edgeBehavior; set => _edgeBehavior = value; }
    public bool TriggerAlwaysActive { get => _triggerAlwaysActive; set => _triggerAlwaysActive = value; }
    public bool TriggerOnLeftMouseDown { get => _triggerOnLeftMouseDown; set => _triggerOnLeftMouseDown = value; }
    public bool TriggerOnRightMouseDown { get => _triggerOnRightMouseDown; set => _triggerOnRightMouseDown = value; }
    public bool TriggerOnMouseMove { get => _triggerOnMouseMove; set => _triggerOnMouseMove = value; }
    public bool DriftEnabled { get => _driftEnabled; set => _driftEnabled = value; }
    public float DriftAmount { get => _driftAmount; set => _driftAmount = value; }
    public float MouseMoveTimeMultiplier { get => _mouseMoveTimeMultiplier; set => _mouseMoveTimeMultiplier = value; }

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

        // Create trail structured buffer
        _trailBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<ParticleInstance>() * MaxParticles * TrailSegments,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<ParticleInstance>()
        });

        // Initialize trail history arrays
        for (int i = 0; i < MaxParticles; i++)
        {
            _trailHistory[i] = new Vector2[TrailSegments];
        }

        // Initialize particles
        InitializeParticles(context.ViewportSize);
    }

    protected override void OnConfigurationChanged()
    {
        float oldParticleSize = _particleSize;
        bool oldRandomColors = _randomColors;

        if (Configuration.TryGet("gw_particleCount", out int count))
            _particleCount = count;
        if (Configuration.TryGet("gw_particleSize", out float size))
            _particleSize = size;
        if (Configuration.TryGet("gw_gravityStrength", out float strength))
            _gravityStrength = strength;
        if (Configuration.TryGet("gw_gravityRadius", out float radius))
            _gravityRadius = radius;
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
        if (Configuration.TryGet("gw_resetOnLeftClick", out bool resetLeft))
            _resetOnLeftClick = resetLeft;
        if (Configuration.TryGet("gw_resetOnRightClick", out bool resetRight))
            _resetOnRightClick = resetRight;
        if (Configuration.TryGet("gw_edgeBehavior", out int edgeBehavior))
            _edgeBehavior = (EdgeBehavior)edgeBehavior;

        // Trigger settings
        if (Configuration.TryGet("gw_triggerAlwaysActive", out bool triggerAlways))
            _triggerAlwaysActive = triggerAlways;
        if (Configuration.TryGet("gw_triggerOnLeftMouseDown", out bool triggerLeft))
            _triggerOnLeftMouseDown = triggerLeft;
        if (Configuration.TryGet("gw_triggerOnRightMouseDown", out bool triggerRight))
            _triggerOnRightMouseDown = triggerRight;
        if (Configuration.TryGet("gw_triggerOnMouseMove", out bool triggerMove))
            _triggerOnMouseMove = triggerMove;

        // Drift settings
        if (Configuration.TryGet("gw_driftEnabled", out bool driftEnabled))
            _driftEnabled = driftEnabled;
        if (Configuration.TryGet("gw_driftAmount", out float driftAmount))
            _driftAmount = driftAmount;

        // Time multiplier
        if (Configuration.TryGet("gw_mouseMoveTimeMultiplier", out float timeMultiplier))
            _mouseMoveTimeMultiplier = timeMultiplier;

        // Update existing particles with new size (scale proportionally)
        if (Math.Abs(oldParticleSize - _particleSize) > 0.01f && oldParticleSize > 0.01f)
        {
            float sizeRatio = _particleSize / oldParticleSize;
            for (int i = 0; i < _activeParticleCount; i++)
            {
                _particles[i].Size *= sizeRatio;
            }
        }

        // Update existing particle colors if randomColors changed or if using fixed color
        if (oldRandomColors != _randomColors || !_randomColors)
        {
            for (int i = 0; i < _activeParticleCount; i++)
            {
                _particles[i].Color = GetParticleColor();
            }
        }

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

            // Initialize trail history with starting position
            for (int j = 0; j < TrailSegments; j++)
            {
                _trailHistory[i][j] = p.Position;
            }
        }
    }

    /// <summary>
    /// Resets all particles to their initial state around the center of the viewport.
    /// </summary>
    public void ResetParticles()
    {
        InitializeParticles(ViewportSize);
    }

    private void SpawnParticle(Vector2 center)
    {
        if (_activeParticleCount >= MaxParticles)
            return;

        int particleIndex = _activeParticleCount;
        ref var p = ref _particles[particleIndex];
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

        // Initialize trail history with starting position
        for (int j = 0; j < TrailSegments; j++)
        {
            _trailHistory[particleIndex][j] = p.Position;
        }
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

        // Store cursor position for edge behavior Reset mode
        _currentCursorPos = cursorPos;

        // Detect mouse movement
        float cursorDelta = Vector2.Distance(cursorPos, _previousCursorPos);
        _isMouseMoving = cursorDelta > MouseMoveThreshold;
        _previousCursorPos = cursorPos;

        // Apply time multiplier when mouse is moving
        if (_isMouseMoving)
        {
            deltaTime *= _mouseMoveTimeMultiplier;
        }

        // Determine if gravity should be active this frame
        bool gravityActive = IsGravityActive(mouseState);

        // Check for mouse click reset triggers (on button down, not held)
        bool isLeftDown = mouseState.IsButtonDown(MouseButtons.Left);
        bool isRightDown = mouseState.IsButtonDown(MouseButtons.Right);

        if (_resetOnLeftClick && isLeftDown && !_wasLeftButtonDown)
        {
            ResetParticles();
        }

        if (_resetOnRightClick && isRightDown && !_wasRightButtonDown)
        {
            ResetParticles();
        }

        _wasLeftButtonDown = isLeftDown;
        _wasRightButtonDown = isRightDown;

        // Update all particles
        for (int i = 0; i < _activeParticleCount; i++)
        {
            UpdateParticle(i, ref _particles[i], cursorPos, deltaTime, gravityActive);
        }
    }

    private bool IsGravityActive(MouseState mouseState)
    {
        // If always active is checked, gravity is always on
        if (_triggerAlwaysActive)
            return true;

        // Check each trigger condition (OR logic - any trigger activates gravity)
        if (_triggerOnLeftMouseDown && mouseState.IsButtonDown(MouseButtons.Left))
            return true;
        if (_triggerOnRightMouseDown && mouseState.IsButtonDown(MouseButtons.Right))
            return true;
        if (_triggerOnMouseMove && _isMouseMoving)
            return true;

        // No triggers active
        return false;
    }

    private void UpdateParticle(int particleIndex, ref ParticleInstance particle, Vector2 cursorPos, float deltaTime, bool gravityActive)
    {
        if (gravityActive)
        {
            // Calculate direction to cursor
            Vector2 toCursor = cursorPos - particle.Position;
            float distance = toCursor.Length();

            // Prevent division by zero
            if (distance < 1f)
                distance = 1f;

            // Calculate radius falloff (smooth transition at edge of gravity field)
            float radiusFalloff = 1f;
            if (_gravityRadius > 0f)
            {
                // Smooth falloff starting at 70% of radius
                float falloffStart = _gravityRadius * 0.7f;
                if (distance > falloffStart)
                {
                    radiusFalloff = 1f - MathF.Min((distance - falloffStart) / (_gravityRadius - falloffStart), 1f);
                    radiusFalloff = radiusFalloff * radiusFalloff; // Quadratic falloff for smoother feel
                }
            }

            // Calculate gravitational force: F = G * m / (r^2 + softening)
            float forceMagnitude = _gravityStrength * particle.Mass / (distance * distance + SofteningFactor);
            forceMagnitude *= radiusFalloff;

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
                    // Add perpendicular component for orbital motion (also affected by radius)
                    Vector2 perpendicular = new Vector2(-toCursor.Y, toCursor.X);
                    if (perpendicular.Length() > 0.001f)
                    {
                        acceleration += Vector2.Normalize(perpendicular) * _orbitSpeed * radiusFalloff;
                    }
                    break;
            }

            // Apply acceleration
            particle.Velocity += acceleration * deltaTime;

            // Apply damping (energy loss)
            particle.Velocity *= _damping;
        }
        else if (_driftEnabled)
        {
            // Apply drift deceleration when gravity is off
            particle.Velocity *= _driftAmount;
        }
        // else: particle continues with current velocity (no change)

        // Update position
        particle.Position += particle.Velocity * deltaTime;

        // Update rotation
        particle.RotationAngle += particle.AngularVelocity * deltaTime;

        // Calculate trail alpha based on velocity (used for trail visibility)
        float speed = particle.Velocity.Length();
        particle.TrailAlpha = _trailEnabled ? MathF.Min(speed / 300f, 1f) * _trailLength : 0f;

        // Update trail history - shift history and record new position
        if (_trailEnabled && _trailHistory[particleIndex] != null)
        {
            var history = _trailHistory[particleIndex];
            // Shift positions backwards (oldest positions fall off)
            for (int i = TrailSegments - 1; i > 0; i--)
            {
                history[i] = history[i - 1];
            }
            // Record current position at front
            history[0] = particle.Position;
        }

        // Handle screen edge behavior
        float margin = particle.Size * 2f; // Use particle size as margin
        bool isOutOfBounds = particle.Position.X < -margin ||
                             particle.Position.X > ViewportSize.X + margin ||
                             particle.Position.Y < -margin ||
                             particle.Position.Y > ViewportSize.Y + margin;

        if (isOutOfBounds)
        {
            switch (_edgeBehavior)
            {
                case EdgeBehavior.Teleport:
                    // Wrap around to opposite side
                    if (particle.Position.X < -margin) particle.Position.X = ViewportSize.X + margin;
                    if (particle.Position.X > ViewportSize.X + margin) particle.Position.X = -margin;
                    if (particle.Position.Y < -margin) particle.Position.Y = ViewportSize.Y + margin;
                    if (particle.Position.Y > ViewportSize.Y + margin) particle.Position.Y = -margin;
                    break;

                case EdgeBehavior.Bounce:
                    // Bounce off the edge with some energy loss
                    const float bounceDamping = 0.8f;
                    if (particle.Position.X < -margin)
                    {
                        particle.Position.X = -margin;
                        particle.Velocity.X = -particle.Velocity.X * bounceDamping;
                    }
                    if (particle.Position.X > ViewportSize.X + margin)
                    {
                        particle.Position.X = ViewportSize.X + margin;
                        particle.Velocity.X = -particle.Velocity.X * bounceDamping;
                    }
                    if (particle.Position.Y < -margin)
                    {
                        particle.Position.Y = -margin;
                        particle.Velocity.Y = -particle.Velocity.Y * bounceDamping;
                    }
                    if (particle.Position.Y > ViewportSize.Y + margin)
                    {
                        particle.Position.Y = ViewportSize.Y + margin;
                        particle.Velocity.Y = -particle.Velocity.Y * bounceDamping;
                    }
                    break;

                case EdgeBehavior.Reset:
                    // Reset particle around the cursor position
                    float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
                    float resetRadius = 50f + Random.Shared.NextSingle() * 150f;
                    Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * resetRadius;
                    particle.Position = _currentCursorPos + offset;
                    particle.Velocity = Vector2.Zero;
                    particle.Color = GetParticleColor();
                    // Reset trail history for this particle
                    if (_trailHistory[particleIndex] != null)
                    {
                        for (int i = 0; i < TrailSegments; i++)
                        {
                            _trailHistory[particleIndex][i] = particle.Position;
                        }
                    }
                    break;
            }
        }
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeParticleCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

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
        context.SetBlendState(BlendMode.Additive);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw trails first (behind particles)
        if (_trailEnabled && _trailLength > 0.01f)
        {
            int trailInstanceCount = 0;

            for (int i = 0; i < _activeParticleCount; i++)
            {
                ref var particle = ref _particles[i];
                var history = _trailHistory[i];

                // Only draw trails if particle is moving fast enough
                if (particle.TrailAlpha < 0.01f)
                    continue;

                // Create trail segments from position history
                // Skip segment 0 (current position - that's the main particle)
                for (int j = 1; j < TrailSegments; j++)
                {
                    // Calculate segment falloff (older segments are smaller and more transparent)
                    float segmentT = (float)j / TrailSegments;
                    float alphaFalloff = (1f - segmentT) * (1f - segmentT); // Quadratic falloff
                    float sizeFalloff = 1f - segmentT * 0.7f; // Size decreases to 30% at end

                    // Scale by trail length setting
                    float trailScale = MathF.Min(_trailLength / 10f, 1f); // Normalize trail length effect

                    ref var trail = ref _trailInstances[trailInstanceCount];
                    trail.Position = history[j];
                    trail.Velocity = particle.Velocity;
                    trail.Color = particle.Color * alphaFalloff * trailScale;
                    trail.Size = particle.Size * sizeFalloff * 0.8f; // Slightly smaller than main particle
                    trail.Mass = particle.Mass;
                    trail.TrailAlpha = 0f; // Trail segments don't have their own trails
                    trail.Lifetime = alphaFalloff > 0.01f ? 1f : 0f;
                    trail.RotationAngle = particle.RotationAngle;
                    trail.AngularVelocity = 0f;
                    trail.Padding1 = 0f;
                    trail.Padding2 = 0f;

                    trailInstanceCount++;
                }
            }

            // Update and draw trail buffer
            if (trailInstanceCount > 0)
            {
                // Clear remaining trail instances
                for (int i = trailInstanceCount; i < _trailInstances.Length; i++)
                {
                    _trailInstances[i] = default;
                }

                context.UpdateBuffer(_trailBuffer!, (ReadOnlySpan<ParticleInstance>)_trailInstances.AsSpan());
                context.SetShaderResource(ShaderStage.Vertex, 0, _trailBuffer!);
                context.SetShaderResource(ShaderStage.Pixel, 0, _trailBuffer!);

                // Draw all trail segments
                context.DrawInstanced(6, MaxParticles * TrailSegments, 0, 0);
            }
        }

        // Copy main particles to GPU buffer
        for (int i = 0; i < MaxParticles; i++)
        {
            if (i < _activeParticleCount)
            {
                _gpuParticles[i] = _particles[i];
                // Clear TrailAlpha for main particles (trails are drawn separately)
                _gpuParticles[i].TrailAlpha = 0f;
            }
            else
            {
                _gpuParticles[i] = default;
            }
        }
        context.UpdateBuffer(_particleBuffer!, (ReadOnlySpan<ParticleInstance>)_gpuParticles.AsSpan());

        // Draw main particles on top
        context.SetShaderResource(ShaderStage.Vertex, 0, _particleBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _particleBuffer!);

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
        _trailBuffer?.Dispose();
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

public enum EdgeBehavior
{
    Teleport = 0,  // Wrap around to opposite side
    Bounce = 1,    // Bounce off the edge
    Reset = 2      // Reset particle around cursor
}
