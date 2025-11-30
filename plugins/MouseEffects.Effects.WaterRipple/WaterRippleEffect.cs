using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.WaterRipple;

/// <summary>
/// Water ripple effect that creates expanding ripples when the user clicks.
/// Uses wave superposition for natural interference patterns.
/// </summary>
public sealed class WaterRippleEffect : EffectBase
{
    private const int HardMaxRipples = 200; // Hard limit for buffer allocation

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "water-ripple",
        Name = "Water Ripple",
        Description = "Creates expanding water ripples on click that distort the screen with realistic wave interference",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    // GPU resources
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _paramsBuffer;
    private IBuffer? _rippleBuffer;
    private ISamplerState? _linearSampler;

    // Ripple state (CPU-side)
    private readonly Ripple[] _ripples = new Ripple[HardMaxRipples];
    private readonly RippleGPU[] _gpuRipples = new RippleGPU[HardMaxRipples]; // Pooled
    private int _nextRipple;
    private int _activeRippleCount; // Tracked incrementally to avoid O(n) counting
    private float _totalTime;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;
    private Vector2 _lastSpawnPosition;

    // General configuration
    private int _maxRipples = 50;
    private float _rippleLifespan = 3.0f;
    private float _waveSpeed = 200f;
    private float _wavelength = 30f;
    private float _damping = 2.0f;

    // Click configuration
    private bool _spawnOnLeftClick = true;
    private bool _spawnOnRightClick = false;
    private float _clickMinAmplitude = 5f;
    private float _clickMaxAmplitude = 20f;

    // Mouse move configuration
    private bool _spawnOnMove = false;
    private float _moveSpawnDistance = 50f;
    private float _moveMinAmplitude = 3f;
    private float _moveMaxAmplitude = 10f;
    private float _moveRippleLifespan = 2.0f;
    private float _moveWaveSpeed = 300f;
    private float _moveWavelength = 20f;
    private float _moveDamping = 3.0f;

    // Grid configuration
    private bool _enableGrid = false;
    private float _gridSpacing = 30f;
    private float _gridThickness = 1.5f;
    private Vector4 _gridColor = new(0.0f, 1.0f, 0.5f, 0.8f);

    public override EffectMetadata Metadata => _metadata;

    /// <summary>
    /// This effect requires continuous screen capture to distort the screen.
    /// </summary>
    public override bool RequiresContinuousScreenCapture => true;

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("WaterRipple.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer for parameters
        var paramsDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<RippleParams>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _paramsBuffer = context.CreateBuffer(paramsDesc);

        // Create structured buffer for ripples
        var rippleDesc = new BufferDescription
        {
            Size = HardMaxRipples * Marshal.SizeOf<RippleGPU>(),
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<RippleGPU>()
        };
        _rippleBuffer = context.CreateBuffer(rippleDesc);

        // Create sampler
        _linearSampler = context.CreateSamplerState(SamplerDescription.LinearClamp);

        // Initialize ripples
        for (int i = 0; i < HardMaxRipples; i++)
        {
            _ripples[i] = new Ripple { IsActive = false };
        }
    }

    protected override void OnConfigurationChanged()
    {
        // General settings
        if (Configuration.TryGet("maxRipples", out int maxRipples))
            _maxRipples = Math.Clamp(maxRipples, 1, HardMaxRipples);

        if (Configuration.TryGet("rippleLifespan", out float lifespan))
            _rippleLifespan = lifespan;

        if (Configuration.TryGet("waveSpeed", out float speed))
            _waveSpeed = speed;

        if (Configuration.TryGet("wavelength", out float wavelength))
            _wavelength = wavelength;

        if (Configuration.TryGet("damping", out float damping))
            _damping = damping;

        // Click settings
        if (Configuration.TryGet("spawnOnLeftClick", out bool leftClick))
            _spawnOnLeftClick = leftClick;

        if (Configuration.TryGet("spawnOnRightClick", out bool rightClick))
            _spawnOnRightClick = rightClick;

        if (Configuration.TryGet("clickMinAmplitude", out float clickMinAmp))
            _clickMinAmplitude = clickMinAmp;

        if (Configuration.TryGet("clickMaxAmplitude", out float clickMaxAmp))
            _clickMaxAmplitude = clickMaxAmp;

        // Mouse move settings
        if (Configuration.TryGet("spawnOnMove", out bool spawnOnMove))
            _spawnOnMove = spawnOnMove;

        if (Configuration.TryGet("moveSpawnDistance", out float moveDistance))
            _moveSpawnDistance = moveDistance;

        if (Configuration.TryGet("moveMinAmplitude", out float moveMinAmp))
            _moveMinAmplitude = moveMinAmp;

        if (Configuration.TryGet("moveMaxAmplitude", out float moveMaxAmp))
            _moveMaxAmplitude = moveMaxAmp;

        if (Configuration.TryGet("moveRippleLifespan", out float moveLifespan))
            _moveRippleLifespan = moveLifespan;

        if (Configuration.TryGet("moveWaveSpeed", out float moveSpeed))
            _moveWaveSpeed = moveSpeed;

        if (Configuration.TryGet("moveWavelength", out float moveWavelength))
            _moveWavelength = moveWavelength;

        if (Configuration.TryGet("moveDamping", out float moveDamping))
            _moveDamping = moveDamping;

        // Grid settings
        if (Configuration.TryGet("enableGrid", out bool enableGrid))
            _enableGrid = enableGrid;

        if (Configuration.TryGet("gridSpacing", out float gridSpacing))
            _gridSpacing = gridSpacing;

        if (Configuration.TryGet("gridThickness", out float gridThickness))
            _gridThickness = gridThickness;

        if (Configuration.TryGet("gridColor", out Vector4 gridColor))
            _gridColor = gridColor;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        var dt = (float)gameTime.DeltaTime.TotalSeconds;
        _totalTime = (float)gameTime.TotalTime.TotalSeconds;

        // Update existing ripples
        for (int i = 0; i < HardMaxRipples; i++)
        {
            ref var ripple = ref _ripples[i];
            if (!ripple.IsActive) continue;

            ripple.Age += dt;
            if (ripple.Age >= ripple.Lifetime)
            {
                ripple.IsActive = false;
                _activeRippleCount--;
            }
        }

        // Spawn ripple on click (detect press, not hold)
        bool leftPressed = mouseState.IsButtonPressed(Core.Input.MouseButtons.Left);
        bool rightPressed = mouseState.IsButtonPressed(Core.Input.MouseButtons.Right);

        if (_spawnOnLeftClick && leftPressed && !_wasLeftPressed)
        {
            SpawnRipple(mouseState.Position, _clickMinAmplitude, _clickMaxAmplitude,
                _rippleLifespan, _waveSpeed, _wavelength, _damping);
        }

        if (_spawnOnRightClick && rightPressed && !_wasRightPressed)
        {
            SpawnRipple(mouseState.Position, _clickMinAmplitude, _clickMaxAmplitude,
                _rippleLifespan, _waveSpeed, _wavelength, _damping);
        }

        _wasLeftPressed = leftPressed;
        _wasRightPressed = rightPressed;

        // Spawn ripple on mouse movement
        if (_spawnOnMove)
        {
            float distanceFromLast = Vector2.Distance(mouseState.Position, _lastSpawnPosition);
            if (distanceFromLast >= _moveSpawnDistance)
            {
                SpawnRipple(mouseState.Position, _moveMinAmplitude, _moveMaxAmplitude,
                    _moveRippleLifespan, _moveWaveSpeed, _moveWavelength, _moveDamping);
                _lastSpawnPosition = mouseState.Position;
            }
        }
    }

    private void SpawnRipple(Vector2 position, float minAmplitude, float maxAmplitude,
        float lifespan, float waveSpeed, float wavelength, float damping)
    {
        // Use tracked count instead of O(n) loop
        if (_activeRippleCount >= _maxRipples) return;

        // Find next available slot (may need to skip active ones)
        int attempts = 0;
        while (_ripples[_nextRipple].IsActive && attempts < HardMaxRipples)
        {
            _nextRipple = (_nextRipple + 1) % HardMaxRipples;
            attempts++;
        }

        if (attempts >= HardMaxRipples) return; // All slots full (shouldn't happen if count is accurate)

        ref var ripple = ref _ripples[_nextRipple];
        _nextRipple = (_nextRipple + 1) % HardMaxRipples;

        // Random amplitude between min and max
        float amplitude = minAmplitude + Random.Shared.NextSingle() * (maxAmplitude - minAmplitude);

        ripple.Position = position;
        ripple.BirthTime = _totalTime;
        ripple.Age = 0;
        ripple.Lifetime = lifespan;
        ripple.InitialAmplitude = amplitude;
        ripple.WaveSpeed = waveSpeed;
        ripple.Wavelength = wavelength;
        ripple.Damping = damping;
        ripple.IsActive = true;
        _activeRippleCount++;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null) return;

        // Early-out: skip entire render pass when no ripples active
        if (_activeRippleCount == 0) return;

        var screenTexture = context.ScreenTexture;
        if (screenTexture == null) return;

        // Prepare GPU data
        int activeCount = 0;
        for (int i = 0; i < HardMaxRipples; i++)
        {
            ref var ripple = ref _ripples[i];
            if (ripple.IsActive)
            {
                float normalizedAge = ripple.Age / ripple.Lifetime;
                // Amplitude decays over lifetime
                float currentAmplitude = ripple.InitialAmplitude * (1.0f - normalizedAge);
                // Current radius = wave has traveled this far (using per-ripple wave speed)
                float currentRadius = ripple.Age * ripple.WaveSpeed;

                _gpuRipples[activeCount] = new RippleGPU
                {
                    Position = ripple.Position,
                    Radius = currentRadius,
                    Amplitude = currentAmplitude,
                    Age = ripple.Age,
                    Lifetime = ripple.Lifetime,
                    WaveSpeed = ripple.WaveSpeed,
                    InvWavelength = MathF.Tau / ripple.Wavelength, // Precompute: 2*PI / wavelength
                    Damping = ripple.Damping,
                    Padding1 = 0,
                    Padding2 = 0,
                    Padding3 = 0
                };
                activeCount++;
            }
        }

        // Only upload active ripples (partial buffer update)
        if (activeCount > 0)
        {
            context.UpdateBuffer(_rippleBuffer!, ((ReadOnlySpan<RippleGPU>)_gpuRipples).Slice(0, activeCount));
        }

        // Update parameters
        var cbParams = new RippleParams
        {
            ViewportSize = context.ViewportSize,
            Time = _totalTime,
            RippleCount = activeCount,
            WaveSpeed = _waveSpeed,
            Wavelength = _wavelength,
            Damping = _damping,
            EnableGrid = _enableGrid ? 1.0f : 0.0f,
            GridSpacing = _gridSpacing,
            GridThickness = _gridThickness,
            GridColor = _gridColor
        };

        context.UpdateBuffer(_paramsBuffer!, cbParams);

        // Set shaders
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _paramsBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);
        context.SetShaderResource(ShaderStage.Pixel, 1, _rippleBuffer!);
        context.SetSampler(ShaderStage.Pixel, 0, _linearSampler!);

        // Opaque blend - we're rendering the full screen with distortion
        context.SetBlendState(BlendMode.Opaque);

        // Draw fullscreen quad
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Unbind
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);
    }

    protected override void OnViewportSizeChanged(Vector2 newSize)
    {
        // No texture recreation needed
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _paramsBuffer?.Dispose();
        _rippleBuffer?.Dispose();
        _linearSampler?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(WaterRippleEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.WaterRipple.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #region Data Structures

    private struct Ripple
    {
        public Vector2 Position;
        public float BirthTime;
        public float Age;
        public float Lifetime;
        public float InitialAmplitude;
        public float WaveSpeed;
        public float Wavelength;
        public float Damping;
        public bool IsActive;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RippleGPU
    {
        public Vector2 Position;    // Center of ripple
        public float Radius;        // Current outer radius
        public float Amplitude;     // Current max amplitude
        public float Age;           // Time since spawn
        public float Lifetime;      // Total lifetime
        public float WaveSpeed;     // Per-ripple wave speed
        public float InvWavelength; // Precomputed: TWO_PI / wavelength (avoids division in shader)
        public float Damping;       // Per-ripple damping
        public float Padding1;
        public float Padding2;
        public float Padding3;
    }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct RippleParams
    {
        public Vector2 ViewportSize;   // 8 bytes, offset 0
        public float Time;              // 4 bytes, offset 8
        public int RippleCount;         // 4 bytes, offset 12
        public float WaveSpeed;         // 4 bytes, offset 16
        public float Wavelength;        // 4 bytes, offset 20
        public float Damping;           // 4 bytes, offset 24
        public float EnableGrid;        // 4 bytes, offset 28
        public float GridSpacing;       // 4 bytes, offset 32
        public float GridThickness;     // 4 bytes, offset 36
        private float _gridPadding1;    // 4 bytes, offset 40
        private float _gridPadding2;    // 4 bytes, offset 44
        public Vector4 GridColor;       // 16 bytes, offset 48 (16-byte aligned)
    }

    #endregion
}
