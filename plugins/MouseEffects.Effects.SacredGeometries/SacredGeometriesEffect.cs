using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

using CoreMouseButtons = MouseEffects.Core.Input.MouseButtons;

namespace MouseEffects.Effects.SacredGeometries;

public sealed class SacredGeometriesEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "sacredgeometries",
        Name = "Sacred Geometries",
        Description = "Mystical mandala patterns with sacred geometry around the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 128)]
    private struct SacredGeometryConstants
    {
        public Vector2 ViewportSize;       // 8 bytes
        public float Time;                 // 4 bytes
        public float GlowIntensity;        // 4 bytes = 16

        public float HdrMultiplier;        // 4 bytes
        public float LineThickness;        // 4 bytes
        public int ActiveMandalaCount;     // 4 bytes
        public float TwinkleIntensity;     // 4 bytes = 32

        public Vector4 PrimaryColor;       // 16 bytes = 48
        public Vector4 SecondaryColor;     // 16 bytes = 64

        public float RainbowSpeed;         // 4 bytes
        public int RainbowEnabled;         // 4 bytes (bool as int)
        public float RainbowHue;           // 4 bytes - current hue offset
        public float Padding1;             // 4 bytes = 80

        public float FadeInDuration;       // 4 bytes
        public float FadeOutDuration;      // 4 bytes
        public float ScaleInDuration;      // 4 bytes
        public float ScaleOutDuration;     // 4 bytes = 96

        public Vector4 Padding2;           // 16 bytes = 112
        public Vector4 Padding3;           // 16 bytes = 128
    }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct MandalaInstance
    {
        public Vector2 Position;           // 8 bytes - Screen position
        public float Radius;               // 4 bytes - Current radius
        public float Rotation;             // 4 bytes = 16 - Current angle (radians)

        public float RotationSpeed;        // 4 bytes - Radians per second
        public float RotationDirection;    // 4 bytes - 1.0 or -1.0
        public float Lifetime;             // 4 bytes - Remaining lifetime
        public float MaxLifetime;          // 4 bytes = 32 - Total lifetime

        public float PatternIndex;         // 4 bytes - Pattern type (0-9, as float for shader)
        public float AppearMode;           // 4 bytes - 0=Fade, 1=Scale, 2=Both
        public float SpawnTime;            // 4 bytes - When spawned (for animations)
        public float PatternComplexity;    // 4 bytes = 48 - Detail level

        public Vector4 Color;              // 16 bytes = 64 - RGBA color
    }

    // Constants
    private const int MaxMandalas = 50;

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _mandalaBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Mandala management (CPU side)
    private readonly MandalaInstance[] _mandalas = new MandalaInstance[MaxMandalas];
    private readonly MandalaInstance[] _gpuMandalas = new MandalaInstance[MaxMandalas];
    private int _activeMandalaCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _accumulatedDistance;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;
    private bool _isMoving;
    private float _lastMoveTime;

    // Rainbow hue tracking
    private float _rainbowHue;

    // Viewport
    private Vector2 _viewportSize;

    // Rate limiting
    private float _lastSpawnTime;
    private int _spawnsThisSecond;
    private float _lastSecondStart;

    // ===== Configuration Fields =====

    // Pattern settings
    private PatternType _selectedPattern = PatternType.FlowerOfLife;
    private bool _randomPatternEnabled = true;
    private float _patternComplexity = 1.0f;

    // Radius settings
    private float _fixedRadius = 100f;
    private bool _animatedRadius = true;
    private float _radiusMin = 60f;
    private float _radiusMax = 140f;
    private float _radiusOscSpeed = 1.0f;

    // Rotation settings
    private float _rotationSpeed = 30f;  // degrees/second
    private int _rotationDirection = 2;  // 0=left, 1=right, 2=random
    private bool _randomRotationSpeed = true;
    private float _rotationSpeedMin = 15f;
    private float _rotationSpeedMax = 60f;

    // Color settings
    private Vector4 _primaryColor = new(0.8f, 0.5f, 1f, 1f);
    private Vector4 _secondaryColor = new(0.4f, 0.8f, 1f, 1f);
    private bool _rainbowMode = true;
    private float _rainbowSpeed = 0.5f;
    private bool _randomRainbowSpeed;
    private float _rainbowSpeedMin = 0.2f;
    private float _rainbowSpeedMax = 1.5f;

    // Glow settings
    private float _glowIntensity = 1.2f;
    private float _lineThickness = 2.0f;
    private float _twinkleIntensity = 0.3f;

    // Appearance settings
    private AppearanceMode _appearanceMode = AppearanceMode.Both;
    private bool _randomAppearanceMode;
    private float _fadeInDuration = 0.3f;
    private float _fadeOutDuration = 0.5f;
    private float _scaleInDuration = 0.3f;
    private float _scaleOutDuration = 0.4f;

    // Spawn settings
    private int _maxMandalaCount = 5;

    // Trigger settings
    private bool _mouseMoveEnabled = true;
    private float _moveDistanceThreshold = 80f;
    private bool _leftClickEnabled = true;
    private bool _rightClickEnabled;

    // Lifetime settings
    private float _lifetimeDuration = 3.0f;
    private bool _whileActiveMode;

    // Performance settings
    private int _maxActiveMandalas = 20;
    private int _maxSpawnsPerSecond = 10;

    // Public properties for UI binding
    public PatternType SelectedPattern { get => _selectedPattern; set => _selectedPattern = value; }
    public bool RandomPatternEnabled { get => _randomPatternEnabled; set => _randomPatternEnabled = value; }
    public float PatternComplexity { get => _patternComplexity; set => _patternComplexity = value; }
    public float FixedRadius { get => _fixedRadius; set => _fixedRadius = value; }
    public bool AnimatedRadius { get => _animatedRadius; set => _animatedRadius = value; }
    public float RadiusMin { get => _radiusMin; set => _radiusMin = value; }
    public float RadiusMax { get => _radiusMax; set => _radiusMax = value; }
    public float RadiusOscSpeed { get => _radiusOscSpeed; set => _radiusOscSpeed = value; }
    public float RotationSpeed { get => _rotationSpeed; set => _rotationSpeed = value; }
    public int RotationDirection { get => _rotationDirection; set => _rotationDirection = value; }
    public bool RandomRotationSpeed { get => _randomRotationSpeed; set => _randomRotationSpeed = value; }
    public float RotationSpeedMin { get => _rotationSpeedMin; set => _rotationSpeedMin = value; }
    public float RotationSpeedMax { get => _rotationSpeedMax; set => _rotationSpeedMax = value; }
    public Vector4 PrimaryColor { get => _primaryColor; set => _primaryColor = value; }
    public Vector4 SecondaryColor { get => _secondaryColor; set => _secondaryColor = value; }
    public bool RainbowMode { get => _rainbowMode; set => _rainbowMode = value; }
    public float RainbowSpeed { get => _rainbowSpeed; set => _rainbowSpeed = value; }
    public bool RandomRainbowSpeed { get => _randomRainbowSpeed; set => _randomRainbowSpeed = value; }
    public float RainbowSpeedMin { get => _rainbowSpeedMin; set => _rainbowSpeedMin = value; }
    public float RainbowSpeedMax { get => _rainbowSpeedMax; set => _rainbowSpeedMax = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public float LineThickness { get => _lineThickness; set => _lineThickness = value; }
    public float TwinkleIntensity { get => _twinkleIntensity; set => _twinkleIntensity = value; }
    public AppearanceMode AppearanceMode { get => _appearanceMode; set => _appearanceMode = value; }
    public bool RandomAppearanceMode { get => _randomAppearanceMode; set => _randomAppearanceMode = value; }
    public float FadeInDuration { get => _fadeInDuration; set => _fadeInDuration = value; }
    public float FadeOutDuration { get => _fadeOutDuration; set => _fadeOutDuration = value; }
    public float ScaleInDuration { get => _scaleInDuration; set => _scaleInDuration = value; }
    public float ScaleOutDuration { get => _scaleOutDuration; set => _scaleOutDuration = value; }
    public int MaxMandalaCount { get => _maxMandalaCount; set => _maxMandalaCount = value; }
    public bool MouseMoveEnabled { get => _mouseMoveEnabled; set => _mouseMoveEnabled = value; }
    public float MoveDistanceThreshold { get => _moveDistanceThreshold; set => _moveDistanceThreshold = value; }
    public bool LeftClickEnabled { get => _leftClickEnabled; set => _leftClickEnabled = value; }
    public bool RightClickEnabled { get => _rightClickEnabled; set => _rightClickEnabled = value; }
    public float LifetimeDuration { get => _lifetimeDuration; set => _lifetimeDuration = value; }
    public bool WhileActiveMode { get => _whileActiveMode; set => _whileActiveMode = value; }
    public int MaxActiveMandalas { get => _maxActiveMandalas; set => _maxActiveMandalas = value; }
    public int MaxSpawnsPerSecond { get => _maxSpawnsPerSecond; set => _maxSpawnsPerSecond = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        _viewportSize = context.ViewportSize;

        // Load and compile shaders
        var shaderCode = LoadShaderResource("Shaders.MandalaShader.hlsl");
        _vertexShader = context.CompileShader(shaderCode, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderCode, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<SacredGeometryConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create mandala instance buffer (structured buffer)
        _mandalaBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<MandalaInstance>() * MaxMandalas,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<MandalaInstance>()
        });
    }

    protected override void OnConfigurationChanged()
    {
        // Pattern settings
        if (Configuration.TryGet("sg_pat_selected", out int pattern))
            _selectedPattern = (PatternType)pattern;
        if (Configuration.TryGet("sg_pat_randomEnabled", out bool randomPattern))
            _randomPatternEnabled = randomPattern;
        if (Configuration.TryGet("sg_pat_complexity", out float complexity))
            _patternComplexity = complexity;

        // Radius settings
        if (Configuration.TryGet("sg_rad_fixed", out float fixedRadius))
            _fixedRadius = fixedRadius;
        if (Configuration.TryGet("sg_rad_animated", out bool animatedRadius))
            _animatedRadius = animatedRadius;
        if (Configuration.TryGet("sg_rad_min", out float radiusMin))
            _radiusMin = radiusMin;
        if (Configuration.TryGet("sg_rad_max", out float radiusMax))
            _radiusMax = radiusMax;
        if (Configuration.TryGet("sg_rad_oscSpeed", out float oscSpeed))
            _radiusOscSpeed = oscSpeed;

        // Rotation settings
        if (Configuration.TryGet("sg_rot_speed", out float rotSpeed))
            _rotationSpeed = rotSpeed;
        if (Configuration.TryGet("sg_rot_direction", out int rotDir))
            _rotationDirection = rotDir;
        if (Configuration.TryGet("sg_rot_randomSpeed", out bool randomRotSpeed))
            _randomRotationSpeed = randomRotSpeed;
        if (Configuration.TryGet("sg_rot_minSpeed", out float rotMinSpeed))
            _rotationSpeedMin = rotMinSpeed;
        if (Configuration.TryGet("sg_rot_maxSpeed", out float rotMaxSpeed))
            _rotationSpeedMax = rotMaxSpeed;

        // Color settings
        if (Configuration.TryGet("sg_col_primary", out Vector4 primary))
            _primaryColor = primary;
        if (Configuration.TryGet("sg_col_secondary", out Vector4 secondary))
            _secondaryColor = secondary;
        if (Configuration.TryGet("sg_col_rainbowMode", out bool rainbow))
            _rainbowMode = rainbow;
        if (Configuration.TryGet("sg_col_rainbowSpeed", out float rainbowSpeed))
            _rainbowSpeed = rainbowSpeed;
        if (Configuration.TryGet("sg_col_randomRainbowSpeed", out bool randomRainbow))
            _randomRainbowSpeed = randomRainbow;
        if (Configuration.TryGet("sg_col_rainbowSpeedMin", out float rainbowMin))
            _rainbowSpeedMin = rainbowMin;
        if (Configuration.TryGet("sg_col_rainbowSpeedMax", out float rainbowMax))
            _rainbowSpeedMax = rainbowMax;

        // Glow settings
        if (Configuration.TryGet("sg_glow_intensity", out float glowInt))
            _glowIntensity = glowInt;
        if (Configuration.TryGet("sg_glow_lineThickness", out float thickness))
            _lineThickness = thickness;
        if (Configuration.TryGet("sg_glow_twinkleIntensity", out float twinkle))
            _twinkleIntensity = twinkle;

        // Appearance settings
        if (Configuration.TryGet("sg_app_mode", out int appMode))
            _appearanceMode = (AppearanceMode)appMode;
        if (Configuration.TryGet("sg_app_randomMode", out bool randomApp))
            _randomAppearanceMode = randomApp;
        if (Configuration.TryGet("sg_app_fadeInDuration", out float fadeIn))
            _fadeInDuration = fadeIn;
        if (Configuration.TryGet("sg_app_fadeOutDuration", out float fadeOut))
            _fadeOutDuration = fadeOut;
        if (Configuration.TryGet("sg_app_scaleInDuration", out float scaleIn))
            _scaleInDuration = scaleIn;
        if (Configuration.TryGet("sg_app_scaleOutDuration", out float scaleOut))
            _scaleOutDuration = scaleOut;

        // Spawn settings
        if (Configuration.TryGet("sg_spawn_maxCount", out int maxCount))
            _maxMandalaCount = maxCount;

        // Trigger settings
        if (Configuration.TryGet("sg_trig_mouseMoveEnabled", out bool moveEnabled))
            _mouseMoveEnabled = moveEnabled;
        if (Configuration.TryGet("sg_trig_moveDistance", out float moveDist))
            _moveDistanceThreshold = moveDist;
        if (Configuration.TryGet("sg_trig_leftClickEnabled", out bool leftClick))
            _leftClickEnabled = leftClick;
        if (Configuration.TryGet("sg_trig_rightClickEnabled", out bool rightClick))
            _rightClickEnabled = rightClick;

        // Lifetime settings
        if (Configuration.TryGet("sg_life_duration", out float lifetime))
            _lifetimeDuration = lifetime;
        if (Configuration.TryGet("sg_life_whileActiveMode", out bool whileActive))
            _whileActiveMode = whileActive;

        // Performance settings
        if (Configuration.TryGet("sg_perf_maxActive", out int maxActive))
            _maxActiveMandalas = maxActive;
        if (Configuration.TryGet("sg_perf_maxSpawnsPerSecond", out int maxSpawns))
            _maxSpawnsPerSecond = maxSpawns;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        var dt = gameTime.DeltaSeconds;
        var totalTime = gameTime.TotalSeconds;

        // Update rainbow hue
        if (_rainbowMode)
        {
            _rainbowHue += _rainbowSpeed * dt;
            if (_rainbowHue > 1f) _rainbowHue -= 1f;
        }

        // Reset rate limiting each second
        if (totalTime - _lastSecondStart >= 1f)
        {
            _lastSecondStart = totalTime;
            _spawnsThisSecond = 0;
        }

        // Track mouse movement
        var currentPos = mouseState.Position;
        var distanceFromLast = Vector2.Distance(currentPos, _lastMousePos);
        _isMoving = distanceFromLast > 0.5f;
        if (_isMoving) _lastMoveTime = totalTime;

        // Mouse move trigger
        if (_mouseMoveEnabled && distanceFromLast > 0.1f)
        {
            _accumulatedDistance += distanceFromLast;
            if (_accumulatedDistance >= _moveDistanceThreshold)
            {
                SpawnMandala(currentPos, totalTime);
                _accumulatedDistance = 0f;
            }
        }

        // Click triggers
        var leftPressed = mouseState.ButtonsDown.HasFlag(CoreMouseButtons.Left);
        var rightPressed = mouseState.ButtonsDown.HasFlag(CoreMouseButtons.Right);

        if (_leftClickEnabled && leftPressed && !_wasLeftPressed)
        {
            SpawnMandala(currentPos, totalTime);
        }
        if (_rightClickEnabled && rightPressed && !_wasRightPressed)
        {
            SpawnMandala(currentPos, totalTime);
        }

        _wasLeftPressed = leftPressed;
        _wasRightPressed = rightPressed;
        _lastMousePos = currentPos;

        // Update existing mandalas
        UpdateMandalas(dt, totalTime, currentPos);
    }

    private void UpdateMandalas(float dt, float totalTime, Vector2 mousePos)
    {
        _activeMandalaCount = 0;

        for (int i = 0; i < MaxMandalas; i++)
        {
            ref var m = ref _mandalas[i];
            if (m.Lifetime <= 0) continue;

            // Update lifetime
            bool shouldFade = true;

            // While-active mode: don't fade while moving/clicking
            if (_whileActiveMode && (_isMoving || _wasLeftPressed || _wasRightPressed))
            {
                shouldFade = false;
                // Reset lifetime to keep it alive
                m.Lifetime = m.MaxLifetime;
            }

            if (shouldFade)
            {
                m.Lifetime -= dt;
            }

            if (m.Lifetime <= 0)
            {
                m.Lifetime = 0;
                continue;
            }

            // Update rotation
            m.Rotation += m.RotationSpeed * m.RotationDirection * dt;

            // Follow cursor mode (when max count is 1)
            if (_maxMandalaCount == 1)
            {
                m.Position = mousePos;
            }

            // Update animated radius
            if (_animatedRadius)
            {
                float t = (totalTime - m.SpawnTime) * _radiusOscSpeed;
                float oscillation = (MathF.Sin(t * MathF.PI * 2f) + 1f) * 0.5f;  // 0 to 1
                m.Radius = _radiusMin + oscillation * (_radiusMax - _radiusMin);
            }

            // Copy to GPU buffer
            _gpuMandalas[_activeMandalaCount++] = m;
        }
    }

    private void SpawnMandala(Vector2 position, float totalTime)
    {
        // Rate limiting
        if (_spawnsThisSecond >= _maxSpawnsPerSecond)
            return;

        // Active count limiting
        if (_activeMandalaCount >= _maxActiveMandalas)
            return;

        // Max mandala count limiting
        int currentCount = 0;
        for (int i = 0; i < MaxMandalas; i++)
        {
            if (_mandalas[i].Lifetime > 0) currentCount++;
        }
        if (currentCount >= _maxMandalaCount)
        {
            // In follow cursor mode, update position of existing mandala
            if (_maxMandalaCount == 1)
            {
                for (int i = 0; i < MaxMandalas; i++)
                {
                    if (_mandalas[i].Lifetime > 0)
                    {
                        _mandalas[i].Position = position;
                        return;
                    }
                }
            }
            return;
        }

        // Find empty slot
        int slot = -1;
        for (int i = 0; i < MaxMandalas; i++)
        {
            if (_mandalas[i].Lifetime <= 0)
            {
                slot = i;
                break;
            }
        }
        if (slot < 0) return;

        // Determine pattern
        var pattern = _randomPatternEnabled
            ? (PatternType)Random.Shared.Next(0, 10)
            : _selectedPattern;

        // Determine rotation speed
        float rotSpeed = _randomRotationSpeed
            ? _rotationSpeedMin + Random.Shared.NextSingle() * (_rotationSpeedMax - _rotationSpeedMin)
            : _rotationSpeed;
        rotSpeed *= MathF.PI / 180f;  // Convert to radians

        // Determine rotation direction
        float rotDir = _rotationDirection switch
        {
            0 => -1f,  // Left (counter-clockwise)
            1 => 1f,   // Right (clockwise)
            _ => Random.Shared.NextSingle() > 0.5f ? 1f : -1f  // Random
        };

        // Determine appearance mode
        var appMode = _randomAppearanceMode
            ? (AppearanceMode)Random.Shared.Next(0, 3)
            : _appearanceMode;

        // Determine color
        Vector4 color;
        if (_rainbowMode)
        {
            float hue = _rainbowHue + Random.Shared.NextSingle() * 0.1f;
            color = HueToRgb(hue);
        }
        else
        {
            color = _primaryColor;
        }

        // Determine initial radius
        float radius = _animatedRadius
            ? _radiusMin + Random.Shared.NextSingle() * (_radiusMax - _radiusMin)
            : _fixedRadius;

        // Create mandala
        _mandalas[slot] = new MandalaInstance
        {
            Position = position,
            Radius = radius,
            Rotation = Random.Shared.NextSingle() * MathF.PI * 2f,  // Random initial rotation
            RotationSpeed = rotSpeed,
            RotationDirection = rotDir,
            Lifetime = _lifetimeDuration,
            MaxLifetime = _lifetimeDuration,
            PatternIndex = (float)pattern,
            AppearMode = (float)appMode,
            SpawnTime = totalTime,
            PatternComplexity = _patternComplexity,
            Color = color
        };

        _spawnsThisSecond++;
        _lastSpawnTime = totalTime;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeMandalaCount == 0) return;

        var totalTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Update constant buffer
        var constants = new SacredGeometryConstants
        {
            ViewportSize = _viewportSize,
            Time = totalTime,
            GlowIntensity = _glowIntensity,
            HdrMultiplier = 1.5f,
            LineThickness = _lineThickness,
            ActiveMandalaCount = _activeMandalaCount,
            TwinkleIntensity = _twinkleIntensity,
            PrimaryColor = _primaryColor,
            SecondaryColor = _secondaryColor,
            RainbowSpeed = _rainbowSpeed,
            RainbowEnabled = _rainbowMode ? 1 : 0,
            RainbowHue = _rainbowHue,
            FadeInDuration = _fadeInDuration,
            FadeOutDuration = _fadeOutDuration,
            ScaleInDuration = _scaleInDuration,
            ScaleOutDuration = _scaleOutDuration
        };

        context.UpdateBuffer(_constantBuffer!, MemoryMarshal.CreateReadOnlySpan(ref constants, 1));

        // Update mandala buffer
        context.UpdateBuffer(_mandalaBuffer!, new ReadOnlySpan<MandalaInstance>(_gpuMandalas, 0, _activeMandalaCount));

        // Set render state
        context.SetBlendState(BlendMode.Additive);
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _mandalaBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _mandalaBuffer!);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced quads (6 vertices per quad, one per mandala)
        context.DrawInstanced(6, _activeMandalaCount, 0, 0);
    }

    protected override void OnViewportSizeChanged(Vector2 newSize)
    {
        _viewportSize = newSize;
    }

    protected override void OnDispose()
    {
        _mandalaBuffer?.Dispose();
        _constantBuffer?.Dispose();
        _pixelShader?.Dispose();
        _vertexShader?.Dispose();
    }

    private string LoadShaderResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.SacredGeometries.{name}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Shader resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Converts HSV hue (0-1) to RGB color with full saturation and value.
    /// </summary>
    private static Vector4 HueToRgb(float hue)
    {
        hue = hue - MathF.Floor(hue);  // Wrap to 0-1
        float r, g, b;

        float h = hue * 6f;
        int i = (int)h;
        float f = h - i;

        switch (i % 6)
        {
            case 0: r = 1; g = f; b = 0; break;
            case 1: r = 1 - f; g = 1; b = 0; break;
            case 2: r = 0; g = 1; b = f; break;
            case 3: r = 0; g = 1 - f; b = 1; break;
            case 4: r = f; g = 0; b = 1; break;
            default: r = 1; g = 0; b = 1 - f; break;
        }

        return new Vector4(r, g, b, 1f);
    }
}
