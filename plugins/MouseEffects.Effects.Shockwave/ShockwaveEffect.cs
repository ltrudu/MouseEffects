using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Shockwave;

/// <summary>
/// Shockwave effect that creates expanding circular rings/ripples emanating from mouse clicks or movement.
/// Creates sonic boom-like visual effects with optional screen distortion.
/// </summary>
public sealed class ShockwaveEffect : EffectBase
{
    private const int HardMaxShockwaves = 100; // Hard limit for buffer allocation

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "shockwave",
        Name = "Shockwave",
        Description = "Creates expanding circular shockwave rings on click with glow and optional distortion",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    // GPU resources
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _paramsBuffer;
    private IBuffer? _shockwaveBuffer;
    private ISamplerState? _linearSampler;

    // Shockwave state (CPU-side)
    private readonly Shockwave[] _shockwaves = new Shockwave[HardMaxShockwaves];
    private readonly ShockwaveGPU[] _gpuShockwaves = new ShockwaveGPU[HardMaxShockwaves]; // Pooled
    private int _nextShockwave;
    private int _activeShockwaveCount;
    private float _totalTime;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;
    private Vector2 _lastSpawnPosition;

    // General configuration
    private int _maxShockwaves = 20;
    private float _ringLifespan = 2.0f;
    private float _expansionSpeed = 600f;
    private float _maxRadius = 500f;
    private float _ringThickness = 15f;
    private float _glowIntensity = 1.5f;
    private bool _enableDistortion = true;
    private float _distortionStrength = 20f;
    private float _hdrBrightness = 1.0f;

    // Click configuration
    private bool _spawnOnLeftClick = true;
    private bool _spawnOnRightClick = false;

    // Mouse move configuration
    private bool _spawnOnMove = false;
    private float _moveSpawnDistance = 100f;
    private float _moveRingLifespan = 1.5f;
    private float _moveExpansionSpeed = 400f;

    // Color configuration
    private int _colorPreset = 0; // 0=Energy Blue, 1=Fire Red, 2=White, 3=Custom
    private Vector4 _customColor = new(0.0f, 0.5f, 1.0f, 1.0f);

    public override EffectMetadata Metadata => _metadata;

    /// <summary>
    /// This effect requires continuous screen capture only when distortion is enabled and there are active shockwaves.
    /// </summary>
    public override bool RequiresContinuousScreenCapture => _enableDistortion && _activeShockwaveCount > 0;

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("ShockwaveShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer for parameters
        var paramsDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<ShockwaveParams>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _paramsBuffer = context.CreateBuffer(paramsDesc);

        // Create structured buffer for shockwaves
        var shockwaveDesc = new BufferDescription
        {
            Size = HardMaxShockwaves * Marshal.SizeOf<ShockwaveGPU>(),
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<ShockwaveGPU>()
        };
        _shockwaveBuffer = context.CreateBuffer(shockwaveDesc);

        // Create sampler
        _linearSampler = context.CreateSamplerState(SamplerDescription.LinearClamp);

        // Initialize shockwaves
        for (int i = 0; i < HardMaxShockwaves; i++)
        {
            _shockwaves[i] = new Shockwave { IsActive = false };
        }
    }

    protected override void OnConfigurationChanged()
    {
        // General settings
        if (Configuration.TryGet("sw_maxShockwaves", out int maxShockwaves))
            _maxShockwaves = Math.Clamp(maxShockwaves, 1, HardMaxShockwaves);

        if (Configuration.TryGet("sw_ringLifespan", out float lifespan))
            _ringLifespan = lifespan;

        if (Configuration.TryGet("sw_expansionSpeed", out float speed))
            _expansionSpeed = speed;

        if (Configuration.TryGet("sw_maxRadius", out float maxRadius))
            _maxRadius = maxRadius;

        if (Configuration.TryGet("sw_ringThickness", out float thickness))
            _ringThickness = thickness;

        if (Configuration.TryGet("sw_glowIntensity", out float glowIntensity))
            _glowIntensity = glowIntensity;

        if (Configuration.TryGet("sw_enableDistortion", out bool enableDistortion))
            _enableDistortion = enableDistortion;

        if (Configuration.TryGet("sw_distortionStrength", out float distortionStrength))
            _distortionStrength = distortionStrength;

        if (Configuration.TryGet("sw_hdrBrightness", out float hdrBrightness))
            _hdrBrightness = hdrBrightness;

        // Click settings
        if (Configuration.TryGet("sw_spawnOnLeftClick", out bool leftClick))
            _spawnOnLeftClick = leftClick;

        if (Configuration.TryGet("sw_spawnOnRightClick", out bool rightClick))
            _spawnOnRightClick = rightClick;

        // Mouse move settings
        if (Configuration.TryGet("sw_spawnOnMove", out bool spawnOnMove))
            _spawnOnMove = spawnOnMove;

        if (Configuration.TryGet("sw_moveSpawnDistance", out float moveDistance))
            _moveSpawnDistance = moveDistance;

        if (Configuration.TryGet("sw_moveRingLifespan", out float moveLifespan))
            _moveRingLifespan = moveLifespan;

        if (Configuration.TryGet("sw_moveExpansionSpeed", out float moveSpeed))
            _moveExpansionSpeed = moveSpeed;

        // Color settings
        if (Configuration.TryGet("sw_colorPreset", out int colorPreset))
            _colorPreset = colorPreset;

        if (Configuration.TryGet("sw_customColor", out Vector4 customColor))
            _customColor = customColor;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        var dt = (float)gameTime.DeltaTime.TotalSeconds;
        _totalTime = (float)gameTime.TotalTime.TotalSeconds;

        // Update existing shockwaves
        for (int i = 0; i < HardMaxShockwaves; i++)
        {
            ref var shockwave = ref _shockwaves[i];
            if (!shockwave.IsActive) continue;

            shockwave.Age += dt;
            shockwave.CurrentRadius = shockwave.Age * shockwave.ExpansionSpeed;

            // Deactivate if expired or exceeded max radius
            if (shockwave.Age >= shockwave.Lifetime || shockwave.CurrentRadius >= _maxRadius)
            {
                shockwave.IsActive = false;
                _activeShockwaveCount--;
            }
        }

        // Spawn shockwave on click (detect press, not hold)
        bool leftPressed = mouseState.IsButtonPressed(Core.Input.MouseButtons.Left);
        bool rightPressed = mouseState.IsButtonPressed(Core.Input.MouseButtons.Right);

        if (_spawnOnLeftClick && leftPressed && !_wasLeftPressed)
        {
            SpawnShockwave(mouseState.Position, _ringLifespan, _expansionSpeed);
        }

        if (_spawnOnRightClick && rightPressed && !_wasRightPressed)
        {
            SpawnShockwave(mouseState.Position, _ringLifespan, _expansionSpeed);
        }

        _wasLeftPressed = leftPressed;
        _wasRightPressed = rightPressed;

        // Spawn shockwave on mouse movement
        if (_spawnOnMove)
        {
            float distanceFromLast = Vector2.Distance(mouseState.Position, _lastSpawnPosition);
            if (distanceFromLast >= _moveSpawnDistance)
            {
                SpawnShockwave(mouseState.Position, _moveRingLifespan, _moveExpansionSpeed);
                _lastSpawnPosition = mouseState.Position;
            }
        }
    }

    private void SpawnShockwave(Vector2 position, float lifespan, float expansionSpeed)
    {
        // Use tracked count instead of O(n) loop
        if (_activeShockwaveCount >= _maxShockwaves) return;

        // Find next available slot (may need to skip active ones)
        int attempts = 0;
        while (_shockwaves[_nextShockwave].IsActive && attempts < HardMaxShockwaves)
        {
            _nextShockwave = (_nextShockwave + 1) % HardMaxShockwaves;
            attempts++;
        }

        if (attempts >= HardMaxShockwaves) return; // All slots full

        ref var shockwave = ref _shockwaves[_nextShockwave];
        _nextShockwave = (_nextShockwave + 1) % HardMaxShockwaves;

        shockwave.Position = position;
        shockwave.BirthTime = _totalTime;
        shockwave.Age = 0;
        shockwave.Lifetime = lifespan;
        shockwave.ExpansionSpeed = expansionSpeed;
        shockwave.CurrentRadius = 0;
        shockwave.IsActive = true;
        _activeShockwaveCount++;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null) return;

        // Early-out: skip entire render pass when no shockwaves active
        if (_activeShockwaveCount == 0) return;

        // Prepare GPU data
        int activeCount = 0;
        for (int i = 0; i < HardMaxShockwaves; i++)
        {
            ref var shockwave = ref _shockwaves[i];
            if (shockwave.IsActive)
            {
                float normalizedAge = shockwave.Age / shockwave.Lifetime;

                _gpuShockwaves[activeCount] = new ShockwaveGPU
                {
                    Position = shockwave.Position,
                    Radius = shockwave.CurrentRadius,
                    Age = shockwave.Age,
                    Lifetime = shockwave.Lifetime,
                    Padding1 = 0,
                    Padding2 = 0,
                    Padding3 = 0,
                    Padding4 = 0
                };
                activeCount++;
            }
        }

        // Only upload active shockwaves (partial buffer update)
        if (activeCount > 0)
        {
            context.UpdateBuffer(_shockwaveBuffer!, ((ReadOnlySpan<ShockwaveGPU>)_gpuShockwaves).Slice(0, activeCount));
        }

        // Get color based on preset
        Vector4 ringColor = GetRingColor();

        // Update parameters
        var cbParams = new ShockwaveParams
        {
            ViewportSize = context.ViewportSize,
            Time = _totalTime,
            ShockwaveCount = activeCount,
            RingThickness = _ringThickness,
            GlowIntensity = _glowIntensity,
            EnableDistortion = _enableDistortion ? 1.0f : 0.0f,
            DistortionStrength = _distortionStrength,
            HdrBrightness = _hdrBrightness,
            RingColor = ringColor
        };

        context.UpdateBuffer(_paramsBuffer!, cbParams);

        // Set shaders
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _paramsBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 1, _shockwaveBuffer!);
        context.SetSampler(ShaderStage.Pixel, 0, _linearSampler!);

        // Set screen texture if distortion is enabled
        if (_enableDistortion)
        {
            var screenTexture = context.ScreenTexture;
            if (screenTexture != null)
            {
                context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);
                context.SetBlendState(BlendMode.Opaque); // Full replacement with distortion
            }
            else
            {
                // No screen texture available, render as additive
                context.SetBlendState(BlendMode.Additive);
            }
        }
        else
        {
            // No distortion - additive blend for rings
            context.SetBlendState(BlendMode.Additive);
        }

        // Draw fullscreen quad
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Unbind
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);
    }

    private Vector4 GetRingColor()
    {
        return _colorPreset switch
        {
            0 => new Vector4(0.0f, 0.5f, 1.0f, 1.0f), // Energy Blue
            1 => new Vector4(1.0f, 0.3f, 0.0f, 1.0f), // Fire Red
            2 => new Vector4(1.0f, 1.0f, 1.0f, 1.0f), // White
            3 => _customColor, // Custom
            _ => new Vector4(0.0f, 0.5f, 1.0f, 1.0f)  // Default to Energy Blue
        };
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
        _shockwaveBuffer?.Dispose();
        _linearSampler?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(ShockwaveEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.Shockwave.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #region Data Structures

    private struct Shockwave
    {
        public Vector2 Position;
        public float BirthTime;
        public float Age;
        public float Lifetime;
        public float ExpansionSpeed;
        public float CurrentRadius;
        public bool IsActive;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ShockwaveGPU
    {
        public Vector2 Position;    // Center of shockwave
        public float Radius;        // Current radius
        public float Age;           // Time since spawn
        public float Lifetime;      // Total lifetime
        public float Padding1;
        public float Padding2;
        public float Padding3;
        public float Padding4;      // Padding to 32 bytes (8 floats)
    }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct ShockwaveParams
    {
        public Vector2 ViewportSize;        // 8 bytes, offset 0
        public float Time;                   // 4 bytes, offset 8
        public int ShockwaveCount;          // 4 bytes, offset 12
        public float RingThickness;         // 4 bytes, offset 16
        public float GlowIntensity;         // 4 bytes, offset 20
        public float EnableDistortion;      // 4 bytes, offset 24
        public float DistortionStrength;    // 4 bytes, offset 28
        public float HdrBrightness;         // 4 bytes, offset 32
        private float _padding1;            // 4 bytes, offset 36
        private float _padding2;            // 4 bytes, offset 40
        private float _padding3;            // 4 bytes, offset 44
        public Vector4 RingColor;           // 16 bytes, offset 48 (16-byte aligned)
    }

    #endregion
}
