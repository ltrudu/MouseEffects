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
    private const int BufferMaxPetals = 2500; // Maximum buffer size
    private int _maxPetals = 500; // Configurable max petals

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _petalBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Petal management (CPU side)
    private readonly PetalInstance[] _petals = new PetalInstance[BufferMaxPetals];
    private readonly PetalInstance[] _gpuPetals = new PetalInstance[BufferMaxPetals];
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
    private float _petalLifetime = 60f;
    private int _colorPalette = 0; // 0=Cherry Blossom, 1=Autumn, 2=Winter, 3=Spring, 4=Summer, 5=Aquatic, 6=Green Pastel, 7=Fire, 8=Rainbow

    // Wind configuration
    private bool _windEnabled = false;
    private float _windStrength = 50f;
    private float _windDirection = 0f; // Degrees (0=right, 90=down, 180=left, 270=up)
    private bool _windRandomDirection = true;
    private float _windMinDirection = -45f;
    private float _windMaxDirection = 45f;
    private int _windTransitionMode = 0; // 0=Instant, 1=Linear, 2=EaseIn, 3=EaseOut, 4=SmoothStep
    private float _windTransitionDuration = 2f; // Seconds
    private float _windChangeFrequency = 5f; // Seconds between direction changes

    // Wind runtime state
    private float _currentWindDirection = 0f;
    private float _targetWindDirection = 0f;
    private float _windTransitionProgress = 1f; // 0-1, 1 = complete
    private float _windChangeTimer = 0f;
    private float _startWindDirection = 0f;

    // Cursor interaction configuration
    private int _cursorInteraction = 0; // 0=None, 1=Attract, 2=Repel
    private float _cursorForceStrength = 100f;
    private float _cursorFieldRadius = 150f;

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
    public int ColorPalette { get => _colorPalette; set => _colorPalette = value; }

    // Wind properties
    public bool WindEnabled { get => _windEnabled; set => _windEnabled = value; }
    public float WindStrength { get => _windStrength; set => _windStrength = value; }
    public float WindDirection { get => _windDirection; set => _windDirection = value; }
    public bool WindRandomDirection { get => _windRandomDirection; set => _windRandomDirection = value; }
    public float WindMinDirection { get => _windMinDirection; set => _windMinDirection = value; }
    public float WindMaxDirection { get => _windMaxDirection; set => _windMaxDirection = value; }
    public int WindTransitionMode { get => _windTransitionMode; set => _windTransitionMode = value; }
    public float WindTransitionDuration { get => _windTransitionDuration; set => _windTransitionDuration = value; }
    public float WindChangeFrequency { get => _windChangeFrequency; set => _windChangeFrequency = value; }

    // Cursor interaction properties
    public int CursorInteraction { get => _cursorInteraction; set => _cursorInteraction = value; }
    public float CursorForceStrength { get => _cursorForceStrength; set => _cursorForceStrength = value; }
    public float CursorFieldRadius { get => _cursorFieldRadius; set => _cursorFieldRadius = value; }

    // Max petals property
    public int MaxPetals { get => _maxPetals; set => _maxPetals = Math.Clamp(value, 1, BufferMaxPetals); }

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

        // Create petal structured buffer (use max buffer size)
        _petalBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<PetalInstance>() * BufferMaxPetals,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<PetalInstance>()
        });

        // Initialize buffer with zeros to prevent garbage data artifacts
        Array.Clear(_gpuPetals, 0, BufferMaxPetals);
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
        if (Configuration.TryGet("cb_colorPalette", out int palette))
            _colorPalette = palette;

        // Wind settings
        if (Configuration.TryGet("cb_windEnabled", out bool windEnabled))
            _windEnabled = windEnabled;
        if (Configuration.TryGet("cb_windStrength", out float windStrength))
            _windStrength = windStrength;
        if (Configuration.TryGet("cb_windDirection", out float windDir))
            _windDirection = windDir;
        if (Configuration.TryGet("cb_windRandomDirection", out bool windRandom))
            _windRandomDirection = windRandom;
        if (Configuration.TryGet("cb_windMinDirection", out float windMin))
            _windMinDirection = windMin;
        if (Configuration.TryGet("cb_windMaxDirection", out float windMax))
            _windMaxDirection = windMax;
        if (Configuration.TryGet("cb_windTransitionMode", out int windTransMode))
            _windTransitionMode = windTransMode;
        if (Configuration.TryGet("cb_windTransitionDuration", out float windTransDur))
            _windTransitionDuration = windTransDur;
        if (Configuration.TryGet("cb_windChangeFrequency", out float windChangeFreq))
            _windChangeFrequency = windChangeFreq;

        // Cursor interaction settings
        if (Configuration.TryGet("cb_cursorInteraction", out int cursorInt))
            _cursorInteraction = cursorInt;
        if (Configuration.TryGet("cb_cursorForceStrength", out float cursorForce))
            _cursorForceStrength = cursorForce;
        if (Configuration.TryGet("cb_cursorFieldRadius", out float cursorRadius))
            _cursorFieldRadius = cursorRadius;

        // Max petals
        if (Configuration.TryGet("cb_maxPetals", out int maxPetals))
            _maxPetals = Math.Clamp(maxPetals, 1, BufferMaxPetals);
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        float totalTime = gameTime.TotalSeconds;

        // Update wind
        if (_windEnabled)
        {
            UpdateWind(deltaTime);
        }

        // Update existing petals
        UpdatePetals(deltaTime, totalTime, mouseState.Position);

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

    private void UpdateWind(float deltaTime)
    {
        // Update direction change timer
        _windChangeTimer += deltaTime;

        // Check if it's time to change direction
        if (_windChangeTimer >= _windChangeFrequency)
        {
            _windChangeTimer = 0f;

            // Set new target direction
            if (_windRandomDirection)
            {
                _targetWindDirection = _windMinDirection + Random.Shared.NextSingle() * (_windMaxDirection - _windMinDirection);
            }
            else
            {
                _targetWindDirection = _windDirection;
            }

            // Start transition
            _startWindDirection = _currentWindDirection;
            _windTransitionProgress = 0f;
        }

        // Update transition progress
        if (_windTransitionProgress < 1f)
        {
            if (_windTransitionMode == 0) // Instant
            {
                _windTransitionProgress = 1f;
                _currentWindDirection = _targetWindDirection;
            }
            else
            {
                // Advance transition
                float transitionSpeed = _windTransitionDuration > 0 ? 1f / _windTransitionDuration : 100f;
                _windTransitionProgress = MathF.Min(1f, _windTransitionProgress + deltaTime * transitionSpeed);

                // Apply easing
                float easedProgress = ApplyWindEasing(_windTransitionProgress);

                // Interpolate direction
                _currentWindDirection = _startWindDirection + (_targetWindDirection - _startWindDirection) * easedProgress;
            }
        }
    }

    private float ApplyWindEasing(float t)
    {
        return _windTransitionMode switch
        {
            1 => t, // Linear
            2 => t * t, // EaseIn (quadratic)
            3 => 1f - (1f - t) * (1f - t), // EaseOut (quadratic)
            4 => t * t * (3f - 2f * t), // SmoothStep
            5 => t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f, // EaseInOut
            6 => MathF.Pow(2f, 10f * (t - 1f)), // Exponential
            7 => MathF.Log10(t * 9f + 1f), // Logarithmic
            _ => t
        };
    }

    private Vector2 GetWindVelocity()
    {
        if (!_windEnabled) return Vector2.Zero;

        // Convert direction from degrees to radians
        float radians = _currentWindDirection * MathF.PI / 180f;

        // Calculate wind vector (0 degrees = right, 90 = down)
        return new Vector2(MathF.Cos(radians), MathF.Sin(radians)) * _windStrength;
    }

    private void UpdatePetals(float deltaTime, float totalTime, Vector2 mousePosition)
    {
        _activePetalCount = 0;
        Vector2 windVelocity = GetWindVelocity();

        for (int i = 0; i < _maxPetals; i++)
        {
            ref var petal = ref _petals[i];

            // Skip inactive petals (Lifetime <= 0 means inactive)
            if (petal.Lifetime <= 0)
                continue;

            // Apply gravity (downward fall)
            petal.Velocity.Y = petal.FallSpeed;

            // Apply sway effect (gentle side-to-side oscillation)
            float swayEffect = MathF.Sin(totalTime * _swayFrequency + petal.SwayPhase) * petal.SwayAmplitude;
            petal.Velocity.X = swayEffect;

            // Apply wind
            petal.Velocity += windVelocity;

            // Apply cursor interaction (attract/repel)
            if (_cursorInteraction != 0)
            {
                Vector2 toCursor = mousePosition - petal.Position;
                float distance = toCursor.Length();

                if (distance < _cursorFieldRadius && distance > 1f)
                {
                    // Normalize direction
                    Vector2 direction = toCursor / distance;

                    // Calculate force with falloff (stronger when closer)
                    float falloff = 1f - (distance / _cursorFieldRadius);
                    float force = _cursorForceStrength * falloff * falloff;

                    // Apply attract (1) or repel (2)
                    if (_cursorInteraction == 1)
                        petal.Velocity += direction * force * deltaTime;
                    else
                        petal.Velocity -= direction * force * deltaTime;
                }
            }

            // Update position
            petal.Position += petal.Velocity * deltaTime;

            // Update rotation (gentle spinning as they fall)
            petal.RotationAngle += petal.SpinSpeed * deltaTime;

            // Check if petal is off-screen (mark for recycling)
            bool offScreen = petal.Position.Y > 1200f || // Below screen
                             petal.Position.Y < -100f || // Above screen (shouldn't happen normally)
                             petal.Position.X < -200f || // Left of screen
                             petal.Position.X > 2200f;   // Right of screen

            if (offScreen)
            {
                // Mark as inactive for recycling
                petal.Lifetime = 0;
            }
            else
            {
                _activePetalCount++;
            }
        }
    }

    private void SpawnPetal(Vector2 position, float time)
    {
        // Find an inactive petal to recycle
        int spawnIndex = -1;
        for (int i = 0; i < _maxPetals; i++)
        {
            int checkIndex = (_nextPetalIndex + i) % _maxPetals;
            if (_petals[checkIndex].Lifetime <= 0)
            {
                spawnIndex = checkIndex;
                _nextPetalIndex = (checkIndex + 1) % _maxPetals;
                break;
            }
        }

        // No inactive petal found, can't spawn
        if (spawnIndex < 0)
            return;

        ref var petal = ref _petals[spawnIndex];

        // Spawn at top of screen, using mouse X position with random horizontal offset
        float xOffset = (Random.Shared.NextSingle() - 0.5f) * _spawnRadius * 2f;
        float spawnX = position.X + xOffset;
        float spawnY = -Random.Shared.NextSingle() * 50f; // Spawn just above screen top

        petal.Position = new Vector2(spawnX, spawnY);
        petal.Lifetime = 1f; // Active flag (>0 means active)
        petal.MaxLifetime = 1f;

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

    private Vector4 GetPetalColor(float variant)
    {
        return _colorPalette switch
        {
            0 => GetCherryBlossomColor(variant),
            1 => GetAutumnColor(variant),
            2 => GetWinterColor(variant),
            3 => GetSpringColor(variant),
            4 => GetSummerColor(variant),
            5 => GetAquaticColor(variant),
            6 => GetGreenPastelColor(variant),
            7 => GetFireColor(variant),
            8 => GetRainbowColor(variant),
            _ => GetCherryBlossomColor(variant)
        };
    }

    private static Vector4 GetCherryBlossomColor(float variant)
    {
        if (variant < 0.4f)
            return new Vector4(1f, 0.55f, 0.65f, 1f);      // Light pink
        else if (variant < 0.7f)
            return new Vector4(1f, 0.4f, 0.55f, 1f);       // Medium pink
        else if (variant < 0.9f)
            return new Vector4(1f, 0.65f, 0.75f, 1f);      // Soft pink
        else
            return new Vector4(1f, 0.85f, 0.9f, 1f);       // Pale pink
    }

    private static Vector4 GetAutumnColor(float variant)
    {
        if (variant < 0.25f)
            return new Vector4(0.9f, 0.3f, 0.1f, 1f);      // Deep orange-red
        else if (variant < 0.5f)
            return new Vector4(1f, 0.55f, 0f, 1f);         // Bright orange
        else if (variant < 0.75f)
            return new Vector4(0.8f, 0.6f, 0.2f, 1f);      // Golden brown
        else
            return new Vector4(0.95f, 0.8f, 0.2f, 1f);     // Golden yellow
    }

    private static Vector4 GetWinterColor(float variant)
    {
        if (variant < 0.3f)
            return new Vector4(1f, 1f, 1f, 1f);            // Pure white (snow)
        else if (variant < 0.6f)
            return new Vector4(0.85f, 0.92f, 1f, 1f);      // Ice blue
        else if (variant < 0.85f)
            return new Vector4(0.7f, 0.85f, 0.95f, 1f);    // Frost blue
        else
            return new Vector4(0.9f, 0.9f, 0.95f, 1f);     // Silver white
    }

    private static Vector4 GetSpringColor(float variant)
    {
        if (variant < 0.25f)
            return new Vector4(0.2f, 0.9f, 0.3f, 1f);      // Spring green
        else if (variant < 0.5f)
            return new Vector4(1f, 0.9f, 0.2f, 1f);        // Dandelion yellow
        else if (variant < 0.75f)
            return new Vector4(1f, 0.5f, 0.7f, 1f);        // Cherry pink
        else
            return new Vector4(0.6f, 0.5f, 1f, 1f);        // Lavender
    }

    private static Vector4 GetSummerColor(float variant)
    {
        if (variant < 0.25f)
            return new Vector4(0.2f, 1f, 0.2f, 1f);        // Vibrant green
        else if (variant < 0.5f)
            return new Vector4(1f, 1f, 0.2f, 1f);          // Sunny yellow
        else if (variant < 0.75f)
            return new Vector4(1f, 0.5f, 0.1f, 1f);        // Warm orange
        else
            return new Vector4(0.3f, 0.9f, 0.3f, 1f);      // Fresh green
    }

    private static Vector4 GetAquaticColor(float variant)
    {
        if (variant < 0.25f)
            return new Vector4(0f, 0.8f, 0.95f, 1f);       // Bright cyan
        else if (variant < 0.5f)
            return new Vector4(0.2f, 0.6f, 0.9f, 1f);      // Ocean blue
        else if (variant < 0.75f)
            return new Vector4(0.4f, 0.9f, 0.85f, 1f);     // Turquoise
        else
            return new Vector4(0.1f, 0.5f, 0.7f, 1f);      // Deep teal
    }

    private static Vector4 GetGreenPastelColor(float variant)
    {
        if (variant < 0.25f)
            return new Vector4(0.2f, 1f, 0.4f, 1f);        // Mint green
        else if (variant < 0.5f)
            return new Vector4(0.1f, 0.9f, 0.3f, 1f);      // Fresh green
        else if (variant < 0.75f)
            return new Vector4(0.3f, 1f, 0.2f, 1f);        // Lime green
        else
            return new Vector4(0.15f, 0.8f, 0.25f, 1f);    // Forest green
    }

    private static Vector4 GetFireColor(float variant)
    {
        if (variant < 0.3f)
            return new Vector4(1f, 0.9f, 0.2f, 1f);        // Bright yellow (core)
        else if (variant < 0.6f)
            return new Vector4(1f, 0.5f, 0f, 1f);          // Orange flame
        else if (variant < 0.85f)
            return new Vector4(1f, 0.2f, 0f, 1f);          // Red-orange
        else
            return new Vector4(0.8f, 0.1f, 0f, 1f);        // Deep red
    }

    private static Vector4 GetRainbowColor(float variant)
    {
        // HSV to RGB conversion for smooth rainbow
        float hue = variant * 360f;
        float saturation = 0.8f;
        float value = 1f;

        float c = value * saturation;
        float x = c * (1f - MathF.Abs((hue / 60f) % 2f - 1f));
        float m = value - c;

        float r, g, b;
        if (hue < 60f) { r = c; g = x; b = 0; }
        else if (hue < 120f) { r = x; g = c; b = 0; }
        else if (hue < 180f) { r = 0; g = c; b = x; }
        else if (hue < 240f) { r = 0; g = x; b = c; }
        else if (hue < 300f) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return new Vector4(r + m, g + m, b + m, 1f);
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activePetalCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU petal buffer - only include alive petals
        int gpuIndex = 0;
        for (int i = 0; i < _maxPetals && gpuIndex < _maxPetals; i++)
        {
            if (_petals[i].Lifetime > 0)
            {
                _gpuPetals[gpuIndex++] = _petals[i];
            }
        }

        // Fill remaining with zeroed petals
        for (int i = gpuIndex; i < BufferMaxPetals; i++)
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
        context.DrawInstanced(6, _maxPetals, 0, 0);

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
