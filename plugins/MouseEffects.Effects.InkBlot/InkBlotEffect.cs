using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

using CoreMouseButtons = MouseEffects.Core.Input.MouseButtons;

namespace MouseEffects.Effects.InkBlot;

public sealed class InkBlotEffect : EffectBase
{
    private const int MaxDrops = 128;

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "inkblot",
        Name = "Ink Blot",
        Description = "Animated metaball ink drops that drip and merge organically",
        Author = "MouseEffects",
        Version = new Version(1, 1, 0),
        Category = EffectCategory.Artistic
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 96)]
    private struct InkConstants
    {
        public Vector2 ViewportSize;       // 8 bytes
        public float Time;                 // 4 bytes
        public int ActiveDropCount;        // 4 bytes = 16

        public float MetaballThreshold;    // 4 bytes
        public float EdgeSoftness;         // 4 bytes
        public float HdrMultiplier;        // 4 bytes
        public float Opacity;              // 4 bytes = 32

        public Vector4 InkColor;           // 16 bytes = 48

        public float GlowIntensity;        // 4 bytes
        public float InnerDarkening;       // 4 bytes
        public int ColorMode;              // 4 bytes
        public float RainbowSpeed;         // 4 bytes = 64

        public int AnimateGlow;            // 4 bytes (bool as int)
        public float GlowMin;              // 4 bytes
        public float GlowMax;              // 4 bytes
        public float GlowAnimSpeed;        // 4 bytes = 80

        public Vector4 Padding;            // 16 bytes = 96
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    private struct InkDrop
    {
        public Vector2 Position;           // 8 bytes
        public Vector2 Velocity;           // 8 bytes = 16

        public float Radius;               // 4 bytes
        public float Age;                  // 4 bytes
        public float MaxAge;               // 4 bytes
        public float Seed;                 // 4 bytes = 32
    }

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _dropBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Drop management (CPU side)
    private readonly InkDrop[] _drops = new InkDrop[MaxDrops];
    private readonly InkDrop[] _gpuDrops = new InkDrop[MaxDrops];
    private int _activeDropCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _accumulatedDistance;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;

    // Viewport
    private Vector2 _viewportSize;

    // Rate limiting
    private int _spawnsThisSecond;
    private float _lastSecondStart;

    // Configuration fields
    private float _dropRadius = 25f;
    private float _gravity = 150f;
    private float _surfaceTension = 0.5f;
    private float _viscosity = 0.98f;
    private float _spawnSpread = 30f;
    private float _metaballThreshold = 1.0f;
    private float _edgeSoftness = 0.3f;
    private float _opacity = 0.85f;
    private float _lifetime = 4.0f;
    private float _glowIntensity = 0.2f;
    private float _innerDarkening = 0.3f;
    private bool _animateGlow = false;
    private float _glowMin = 0.1f;
    private float _glowMax = 0.5f;
    private float _glowAnimSpeed = 2.0f;
    private int _colorMode = 0; // 0=Black, 1=Blue, 2=Red, 3=Sepia, 4=Rainbow
    private Vector4 _customColor = new(0.1f, 0.1f, 0.1f, 1f);
    private float _rainbowSpeed = 1.0f;
    private bool _spawnOnClick = true;
    private bool _spawnOnMove = true;
    private float _moveDistance = 40f;
    private int _dropsPerSpawn = 3;
    private int _maxDropsPerSecond = 30;

    // Color palette
    private static readonly Vector4[] InkColors =
    [
        new(0.05f, 0.05f, 0.08f, 1f),      // Black ink
        new(0.08f, 0.15f, 0.45f, 1f),       // Blue ink
        new(0.5f, 0.08f, 0.08f, 1f),        // Red ink
        new(0.4f, 0.2f, 0.05f, 1f)          // Sepia
    ];

    // Public properties for UI binding
    public float DropRadius { get => _dropRadius; set => _dropRadius = value; }
    public float Gravity { get => _gravity; set => _gravity = value; }
    public float SurfaceTension { get => _surfaceTension; set => _surfaceTension = value; }
    public float Viscosity { get => _viscosity; set => _viscosity = value; }
    public float SpawnSpread { get => _spawnSpread; set => _spawnSpread = value; }
    public float MetaballThreshold { get => _metaballThreshold; set => _metaballThreshold = value; }
    public float EdgeSoftness { get => _edgeSoftness; set => _edgeSoftness = value; }
    public float Opacity { get => _opacity; set => _opacity = value; }
    public float Lifetime { get => _lifetime; set => _lifetime = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public float InnerDarkening { get => _innerDarkening; set => _innerDarkening = value; }
    public bool AnimateGlow { get => _animateGlow; set => _animateGlow = value; }
    public float GlowMin { get => _glowMin; set => _glowMin = value; }
    public float GlowMax { get => _glowMax; set => _glowMax = value; }
    public float GlowAnimSpeed { get => _glowAnimSpeed; set => _glowAnimSpeed = value; }
    public int ColorMode { get => _colorMode; set => _colorMode = value; }
    public Vector4 CustomColor { get => _customColor; set => _customColor = value; }
    public float RainbowSpeed { get => _rainbowSpeed; set => _rainbowSpeed = value; }
    public bool SpawnOnClick { get => _spawnOnClick; set => _spawnOnClick = value; }
    public bool SpawnOnMove { get => _spawnOnMove; set => _spawnOnMove = value; }
    public float MoveDistance { get => _moveDistance; set => _moveDistance = value; }
    public int DropsPerSpawn { get => _dropsPerSpawn; set => _dropsPerSpawn = value; }
    public int MaxDropsPerSecond { get => _maxDropsPerSecond; set => _maxDropsPerSecond = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        _viewportSize = context.ViewportSize;

        // Initialize drops as inactive
        for (int i = 0; i < MaxDrops; i++)
        {
            _drops[i].Age = -1f;
        }

        // Load and compile shaders
        var shaderCode = LoadShaderResource("Shaders.InkBlotShader.hlsl");
        _vertexShader = context.CompileShader(shaderCode, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderCode, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<InkConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create drop instance buffer (structured buffer)
        _dropBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<InkDrop>() * MaxDrops,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<InkDrop>()
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("dropRadius", out float dropRadius))
            _dropRadius = dropRadius;
        if (Configuration.TryGet("gravity", out float gravity))
            _gravity = gravity;
        if (Configuration.TryGet("surfaceTension", out float surfaceTension))
            _surfaceTension = surfaceTension;
        if (Configuration.TryGet("viscosity", out float viscosity))
            _viscosity = viscosity;
        if (Configuration.TryGet("spawnSpread", out float spawnSpread))
            _spawnSpread = spawnSpread;
        if (Configuration.TryGet("metaballThreshold", out float threshold))
            _metaballThreshold = threshold;
        if (Configuration.TryGet("edgeSoftness", out float softness))
            _edgeSoftness = softness;
        if (Configuration.TryGet("opacity", out float opacity))
            _opacity = opacity;
        if (Configuration.TryGet("lifetime", out float lifetime))
            _lifetime = lifetime;
        if (Configuration.TryGet("glowIntensity", out float glow))
            _glowIntensity = glow;
        if (Configuration.TryGet("innerDarkening", out float darkening))
            _innerDarkening = darkening;
        if (Configuration.TryGet("animateGlow", out bool animateGlow))
            _animateGlow = animateGlow;
        if (Configuration.TryGet("glowMin", out float glowMin))
            _glowMin = glowMin;
        if (Configuration.TryGet("glowMax", out float glowMax))
            _glowMax = glowMax;
        if (Configuration.TryGet("glowAnimSpeed", out float glowAnimSpeed))
            _glowAnimSpeed = glowAnimSpeed;
        if (Configuration.TryGet("colorMode", out int colorMode))
            _colorMode = colorMode;
        if (Configuration.TryGet("rainbowSpeed", out float rainbowSpeed))
            _rainbowSpeed = rainbowSpeed;
        if (Configuration.TryGet("spawnOnClick", out bool spawnOnClick))
            _spawnOnClick = spawnOnClick;
        if (Configuration.TryGet("spawnOnMove", out bool spawnOnMove))
            _spawnOnMove = spawnOnMove;
        if (Configuration.TryGet("moveDistance", out float moveDistance))
            _moveDistance = moveDistance;
        if (Configuration.TryGet("dropsPerSpawn", out int dropsPerSpawn))
            _dropsPerSpawn = dropsPerSpawn;
        if (Configuration.TryGet("maxDropsPerSecond", out int maxDropsPerSecond))
            _maxDropsPerSecond = maxDropsPerSecond;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        var dt = gameTime.DeltaSeconds;
        var totalTime = gameTime.TotalSeconds;

        // Reset rate limiting each second
        if (totalTime - _lastSecondStart >= 1f)
        {
            _lastSecondStart = totalTime;
            _spawnsThisSecond = 0;
        }

        // Track mouse movement
        var currentPos = mouseState.Position;
        var distanceFromLast = Vector2.Distance(currentPos, _lastMousePos);

        // Mouse move trigger
        if (_spawnOnMove && distanceFromLast > 0.1f)
        {
            _accumulatedDistance += distanceFromLast;
            if (_accumulatedDistance >= _moveDistance)
            {
                SpawnDrops(currentPos, totalTime);
                _accumulatedDistance = 0f;
            }
        }

        // Click triggers
        var leftPressed = mouseState.ButtonsDown.HasFlag(CoreMouseButtons.Left);
        var rightPressed = mouseState.ButtonsDown.HasFlag(CoreMouseButtons.Right);

        if (_spawnOnClick && leftPressed && !_wasLeftPressed)
        {
            SpawnDrops(currentPos, totalTime);
        }
        if (_spawnOnClick && rightPressed && !_wasRightPressed)
        {
            SpawnDrops(currentPos, totalTime);
        }

        _wasLeftPressed = leftPressed;
        _wasRightPressed = rightPressed;
        _lastMousePos = currentPos;

        // Update physics
        UpdatePhysics(dt);
    }

    private void UpdatePhysics(float dt)
    {
        // First pass: update velocities with surface tension (attraction to nearby drops)
        if (_surfaceTension > 0)
        {
            for (int i = 0; i < MaxDrops; i++)
            {
                ref var dropA = ref _drops[i];
                if (dropA.Age < 0) continue;

                Vector2 tensionForce = Vector2.Zero;

                for (int j = 0; j < MaxDrops; j++)
                {
                    if (i == j) continue;
                    ref var dropB = ref _drops[j];
                    if (dropB.Age < 0) continue;

                    var delta = dropB.Position - dropA.Position;
                    var dist = delta.Length();
                    var combinedRadius = (dropA.Radius + dropB.Radius) * 2f;

                    // Only attract within a certain range
                    if (dist > 0.1f && dist < combinedRadius)
                    {
                        var strength = (1f - dist / combinedRadius) * _surfaceTension * 50f;
                        tensionForce += Vector2.Normalize(delta) * strength;
                    }
                }

                dropA.Velocity += tensionForce * dt;
            }
        }

        // Second pass: update positions and apply gravity/viscosity
        _activeDropCount = 0;

        for (int i = 0; i < MaxDrops; i++)
        {
            ref var drop = ref _drops[i];
            if (drop.Age < 0) continue;

            // Update age
            drop.Age += dt;

            // Check if expired
            if (drop.Age >= drop.MaxAge)
            {
                drop.Age = -1f;
                continue;
            }

            // Apply gravity
            drop.Velocity.Y += _gravity * dt;

            // Apply viscosity (drag)
            drop.Velocity *= MathF.Pow(_viscosity, dt * 60f);

            // Update position
            drop.Position += drop.Velocity * dt;

            // Boundary check - keep drops on screen (with some margin)
            float margin = drop.Radius * 2;
            if (drop.Position.X < -margin || drop.Position.X > _viewportSize.X + margin ||
                drop.Position.Y < -margin || drop.Position.Y > _viewportSize.Y + margin)
            {
                drop.Age = -1f;
                continue;
            }

            // Copy to GPU buffer
            _gpuDrops[_activeDropCount++] = drop;
        }
    }

    private void SpawnDrops(Vector2 position, float totalTime)
    {
        for (int d = 0; d < _dropsPerSpawn; d++)
        {
            // Rate limiting
            if (_spawnsThisSecond >= _maxDropsPerSecond)
                return;

            // Find empty slot
            int slot = -1;
            for (int i = 0; i < MaxDrops; i++)
            {
                if (_drops[i].Age < 0)
                {
                    slot = i;
                    break;
                }
            }
            if (slot < 0) return;

            // Random offset for spread
            var offset = new Vector2(
                (Random.Shared.NextSingle() - 0.5f) * 2f * _spawnSpread,
                (Random.Shared.NextSingle() - 0.5f) * 2f * _spawnSpread
            );

            // Random initial velocity (slight downward bias)
            var velocity = new Vector2(
                (Random.Shared.NextSingle() - 0.5f) * 40f,
                Random.Shared.NextSingle() * 20f + 10f
            );

            // Vary radius slightly
            var radiusVariation = 0.7f + Random.Shared.NextSingle() * 0.6f;

            // Create drop
            _drops[slot] = new InkDrop
            {
                Position = position + offset,
                Velocity = velocity,
                Radius = _dropRadius * radiusVariation,
                Age = 0f,
                MaxAge = _lifetime * (0.8f + Random.Shared.NextSingle() * 0.4f),
                Seed = Random.Shared.NextSingle() * 1000f
            };

            _spawnsThisSecond++;
        }
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeDropCount == 0) return;

        var totalTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Get current ink color
        Vector4 inkColor = _colorMode < InkColors.Length ? InkColors[_colorMode] : _customColor;

        // Update constant buffer
        var constants = new InkConstants
        {
            ViewportSize = _viewportSize,
            Time = totalTime,
            ActiveDropCount = _activeDropCount,
            MetaballThreshold = _metaballThreshold,
            EdgeSoftness = _edgeSoftness,
            HdrMultiplier = context.HdrPeakBrightness,
            Opacity = _opacity,
            InkColor = inkColor,
            GlowIntensity = _glowIntensity,
            InnerDarkening = _innerDarkening,
            ColorMode = _colorMode,
            RainbowSpeed = _rainbowSpeed,
            AnimateGlow = _animateGlow ? 1 : 0,
            GlowMin = _glowMin,
            GlowMax = _glowMax,
            GlowAnimSpeed = _glowAnimSpeed
        };

        context.UpdateBuffer(_constantBuffer!, MemoryMarshal.CreateReadOnlySpan(ref constants, 1));

        // Update drop buffer
        context.UpdateBuffer(_dropBuffer!, new ReadOnlySpan<InkDrop>(_gpuDrops, 0, _activeDropCount));

        // Set render state
        context.SetBlendState(BlendMode.Alpha);
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _dropBuffer!);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw fullscreen triangle for metaball evaluation
        context.Draw(3, 0);
    }

    protected override void OnViewportSizeChanged(Vector2 newSize)
    {
        _viewportSize = newSize;
    }

    protected override void OnDispose()
    {
        _dropBuffer?.Dispose();
        _constantBuffer?.Dispose();
        _pixelShader?.Dispose();
        _vertexShader?.Dispose();
    }

    private string LoadShaderResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.InkBlot.{name}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Shader resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
