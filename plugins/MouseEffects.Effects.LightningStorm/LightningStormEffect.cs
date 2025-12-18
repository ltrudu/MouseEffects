using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

using CoreMouseButtons = MouseEffects.Core.Input.MouseButtons;

namespace MouseEffects.Effects.LightningStorm;

public sealed class LightningStormEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "lightningstorm",
        Name = "Lightning Storm",
        Description = "Creates dramatic lightning bolts that arc from or around the mouse cursor with electric effects",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct LightningConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public Vector2 MousePosition;     // 8 bytes = 16
        public float Time;                // 4 bytes
        public float BoltThickness;       // 4 bytes
        public float FlickerSpeed;        // 4 bytes
        public float FlashIntensity;      // 4 bytes = 32
        public float GlowIntensity;       // 4 bytes
        public float BranchIntensity;     // 4 bytes
        public float HdrMultiplier;       // 4 bytes - HDR peak brightness
        public float Padding1;            // 4 bytes = 48
        public Vector4 Padding2;          // 16 bytes = 64
    }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct LightningBolt
    {
        public Vector2 StartPos;          // 8 bytes - Start position
        public Vector2 EndPos;            // 8 bytes - End position = 16
        public Vector4 Color;             // 16 bytes = 32
        public float Lifetime;            // 4 bytes - Current life remaining
        public float MaxLifetime;         // 4 bytes - Total lifetime
        public float Intensity;           // 4 bytes - Brightness multiplier
        public float BranchCount;         // 4 bytes = 48
        public Vector4 Padding;           // 16 bytes = 64
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    private struct ImpactSpark
    {
        public Vector2 Position;          // 8 bytes
        public Vector2 Velocity;          // 8 bytes = 16
        public Vector4 Color;             // 16 bytes = 32
    }

    // Constants
    private const int MaxBolts = 256;
    private const int MaxSparks = 512;

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _boltBuffer;
    private IBuffer? _sparkBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Bolt management (CPU side)
    private readonly LightningBolt[] _bolts = new LightningBolt[MaxBolts];
    private readonly LightningBolt[] _gpuBolts = new LightningBolt[MaxBolts];
    private int _nextBoltIndex;
    private int _activeBoltCount;

    // Spark management (CPU side)
    private readonly ImpactSpark[] _sparks = new ImpactSpark[MaxSparks];
    private readonly ImpactSpark[] _gpuSparks = new ImpactSpark[MaxSparks];
    private int _nextSparkIndex;
    private int _activeSparkCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _accumulatedDistance;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;

    // Strike timing for random mode
    private float _nextStrikeTime;
    private float _strikeTimer;

    // Flash effect
    private float _flashIntensity;

    // Viewport
    private Vector2 _viewportSize;

    // Configuration fields with ls_ prefix
    private bool _isLoading = true;

    // Trigger settings
    private bool _onClickTrigger = true;
    private bool _onMoveTrigger;
    private float _moveDistance = 50f;
    private bool _randomTiming;
    private float _minStrikeInterval = 0.5f;
    private float _maxStrikeInterval = 2.0f;

    // Bolt settings
    private int _minBoltCount = 1;
    private int _maxBoltCount = 3;
    private float _boltThickness = 2.0f;
    private int _branchCount = 3;
    private float _branchProbability = 0.7f;

    // Direction and targeting
    private bool _strikeFromCursor = true;
    private bool _chainLightning;
    private float _minStrikeDistance = 100f;
    private float _maxStrikeDistance = 300f;

    // Visual settings
    private float _boltLifetime = 0.2f;
    private float _flickerSpeed = 25f;
    private float _flashIntensityConfig = 0.5f;
    private float _glowIntensity = 1.0f;
    private bool _persistenceEffect;
    private float _persistenceFade = 0.3f;

    // Colors
    private int _colorMode; // 0=White/Blue, 1=Purple, 2=Green, 3=Custom
    private Vector4 _customColor = new(0.4f, 0.6f, 1f, 1f);

    // Sparks
    private bool _enableSparks = true;
    private int _sparkCount = 8;
    private float _sparkLifetime = 0.5f;
    private float _sparkSpeed = 200f;

    // Public properties for UI binding
    public bool OnClickTrigger { get => _onClickTrigger; set => _onClickTrigger = value; }
    public bool OnMoveTrigger { get => _onMoveTrigger; set => _onMoveTrigger = value; }
    public float MoveDistance { get => _moveDistance; set => _moveDistance = value; }
    public bool RandomTiming { get => _randomTiming; set => _randomTiming = value; }
    public float MinStrikeInterval { get => _minStrikeInterval; set => _minStrikeInterval = value; }
    public float MaxStrikeInterval { get => _maxStrikeInterval; set => _maxStrikeInterval = value; }
    public int MinBoltCount { get => _minBoltCount; set => _minBoltCount = value; }
    public int MaxBoltCount { get => _maxBoltCount; set => _maxBoltCount = value; }
    public float BoltThickness { get => _boltThickness; set => _boltThickness = value; }
    public int BranchCount { get => _branchCount; set => _branchCount = value; }
    public float BranchProbability { get => _branchProbability; set => _branchProbability = value; }
    public bool StrikeFromCursor { get => _strikeFromCursor; set => _strikeFromCursor = value; }
    public bool ChainLightning { get => _chainLightning; set => _chainLightning = value; }
    public float MinStrikeDistance { get => _minStrikeDistance; set => _minStrikeDistance = value; }
    public float MaxStrikeDistance { get => _maxStrikeDistance; set => _maxStrikeDistance = value; }
    public float BoltLifetime { get => _boltLifetime; set => _boltLifetime = value; }
    public float FlickerSpeed { get => _flickerSpeed; set => _flickerSpeed = value; }
    public float FlashIntensity { get => _flashIntensityConfig; set => _flashIntensityConfig = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public bool PersistenceEffect { get => _persistenceEffect; set => _persistenceEffect = value; }
    public float PersistenceFade { get => _persistenceFade; set => _persistenceFade = value; }
    public int ColorMode { get => _colorMode; set => _colorMode = value; }
    public Vector4 CustomColor { get => _customColor; set => _customColor = value; }
    public bool EnableSparks { get => _enableSparks; set => _enableSparks = value; }
    public int SparkCount { get => _sparkCount; set => _sparkCount = value; }
    public float SparkLifetime { get => _sparkLifetime; set => _sparkLifetime = value; }
    public float SparkSpeed { get => _sparkSpeed; set => _sparkSpeed = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        _viewportSize = context.ViewportSize;

        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("LightningStormShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<LightningConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create bolt structured buffer
        _boltBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<LightningBolt>() * MaxBolts,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<LightningBolt>()
        });

        // Create spark structured buffer
        _sparkBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<ImpactSpark>() * MaxSparks,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<ImpactSpark>()
        });

        // Initialize random strike time
        _nextStrikeTime = Random.Shared.NextSingle() * (_maxStrikeInterval - _minStrikeInterval) + _minStrikeInterval;
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("ls_onClickTrigger", out bool onClick))
            _onClickTrigger = onClick;
        if (Configuration.TryGet("ls_onMoveTrigger", out bool onMove))
            _onMoveTrigger = onMove;
        if (Configuration.TryGet("ls_moveDistance", out float moveDist))
            _moveDistance = moveDist;
        if (Configuration.TryGet("ls_randomTiming", out bool randTime))
            _randomTiming = randTime;
        if (Configuration.TryGet("ls_minStrikeInterval", out float minInterval))
            _minStrikeInterval = minInterval;
        if (Configuration.TryGet("ls_maxStrikeInterval", out float maxInterval))
            _maxStrikeInterval = maxInterval;

        if (Configuration.TryGet("ls_minBoltCount", out int minBolts))
            _minBoltCount = minBolts;
        if (Configuration.TryGet("ls_maxBoltCount", out int maxBolts))
            _maxBoltCount = maxBolts;
        if (Configuration.TryGet("ls_boltThickness", out float thickness))
            _boltThickness = thickness;
        if (Configuration.TryGet("ls_branchCount", out int branches))
            _branchCount = branches;
        if (Configuration.TryGet("ls_branchProbability", out float branchProb))
            _branchProbability = branchProb;

        if (Configuration.TryGet("ls_strikeFromCursor", out bool fromCursor))
            _strikeFromCursor = fromCursor;
        if (Configuration.TryGet("ls_chainLightning", out bool chain))
            _chainLightning = chain;
        if (Configuration.TryGet("ls_minStrikeDistance", out float minDist))
            _minStrikeDistance = minDist;
        if (Configuration.TryGet("ls_maxStrikeDistance", out float maxDist))
            _maxStrikeDistance = maxDist;

        if (Configuration.TryGet("ls_boltLifetime", out float lifetime))
            _boltLifetime = lifetime;
        if (Configuration.TryGet("ls_flickerSpeed", out float flicker))
            _flickerSpeed = flicker;
        if (Configuration.TryGet("ls_flashIntensity", out float flash))
            _flashIntensityConfig = flash;
        if (Configuration.TryGet("ls_glowIntensity", out float glow))
            _glowIntensity = glow;
        if (Configuration.TryGet("ls_persistenceEffect", out bool persist))
            _persistenceEffect = persist;
        if (Configuration.TryGet("ls_persistenceFade", out float persistFade))
            _persistenceFade = persistFade;

        if (Configuration.TryGet("ls_colorMode", out int colorMode))
            _colorMode = colorMode;
        if (Configuration.TryGet("ls_customColor", out Vector4 customCol))
            _customColor = customCol;

        if (Configuration.TryGet("ls_enableSparks", out bool sparks))
            _enableSparks = sparks;
        if (Configuration.TryGet("ls_sparkCount", out int sparkCnt))
            _sparkCount = sparkCnt;
        if (Configuration.TryGet("ls_sparkLifetime", out float sparkLife))
            _sparkLifetime = sparkLife;
        if (Configuration.TryGet("ls_sparkSpeed", out float sparkSpd))
            _sparkSpeed = sparkSpd;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        float totalTime = gameTime.TotalSeconds;

        // Update flash effect (decay)
        if (_flashIntensity > 0)
        {
            _flashIntensity = MathF.Max(0, _flashIntensity - deltaTime * 5f);
        }

        // Handle random timing strikes
        if (_randomTiming)
        {
            _strikeTimer += deltaTime;
            if (_strikeTimer >= _nextStrikeTime)
            {
                SpawnLightningStrike(mouseState.Position);
                _strikeTimer = 0;
                _nextStrikeTime = Random.Shared.NextSingle() * (_maxStrikeInterval - _minStrikeInterval) + _minStrikeInterval;
            }
        }

        // Handle click trigger
        if (_onClickTrigger)
        {
            bool leftPressed = mouseState.IsButtonPressed(CoreMouseButtons.Left);
            if (leftPressed && !_wasLeftPressed)
            {
                SpawnLightningStrike(mouseState.Position);
            }
            _wasLeftPressed = leftPressed;

            bool rightPressed = mouseState.IsButtonPressed(CoreMouseButtons.Right);
            if (rightPressed && !_wasRightPressed)
            {
                SpawnLightningStrike(mouseState.Position);
            }
            _wasRightPressed = rightPressed;
        }

        // Handle move trigger
        if (_onMoveTrigger)
        {
            float distance = Vector2.Distance(mouseState.Position, _lastMousePos);
            _accumulatedDistance += distance;

            if (_accumulatedDistance >= _moveDistance)
            {
                SpawnLightningStrike(mouseState.Position);
                _accumulatedDistance = 0;
            }
        }

        _lastMousePos = mouseState.Position;

        // Update existing bolts
        UpdateBolts(deltaTime);

        // Update existing sparks
        UpdateSparks(deltaTime);
    }

    private void SpawnLightningStrike(Vector2 cursorPos)
    {
        int boltCount = Random.Shared.Next(_minBoltCount, _maxBoltCount + 1);
        Vector4 color = GetBoltColor();

        // Trigger flash effect
        _flashIntensity = _flashIntensityConfig;

        if (_chainLightning)
        {
            // Chain lightning: connect multiple random points
            Vector2 currentPos = cursorPos;
            for (int i = 0; i < boltCount; i++)
            {
                float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
                float distance = Random.Shared.NextSingle() * (_maxStrikeDistance - _minStrikeDistance) + _minStrikeDistance;
                Vector2 endPos = currentPos + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;

                // Clamp to viewport
                endPos.X = Math.Clamp(endPos.X, 0, _viewportSize.X);
                endPos.Y = Math.Clamp(endPos.Y, 0, _viewportSize.Y);

                CreateBolt(currentPos, endPos, color);

                if (_enableSparks)
                    SpawnSparks(endPos, color);

                currentPos = endPos;
            }
        }
        else
        {
            // Standard mode: bolts from/to cursor
            for (int i = 0; i < boltCount; i++)
            {
                float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
                float distance = Random.Shared.NextSingle() * (_maxStrikeDistance - _minStrikeDistance) + _minStrikeDistance;
                Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;

                Vector2 startPos, endPos;
                if (_strikeFromCursor)
                {
                    startPos = cursorPos;
                    endPos = cursorPos + offset;
                }
                else
                {
                    startPos = cursorPos + offset;
                    endPos = cursorPos;
                }

                // Clamp to viewport
                endPos.X = Math.Clamp(endPos.X, 0, _viewportSize.X);
                endPos.Y = Math.Clamp(endPos.Y, 0, _viewportSize.Y);

                CreateBolt(startPos, endPos, color);

                if (_enableSparks)
                    SpawnSparks(endPos, color);
            }
        }
    }

    private void CreateBolt(Vector2 start, Vector2 end, Vector4 color)
    {
        ref LightningBolt bolt = ref _bolts[_nextBoltIndex];
        bolt.StartPos = start;
        bolt.EndPos = end;
        bolt.Color = color;
        bolt.Lifetime = _persistenceEffect ? _boltLifetime + _persistenceFade : _boltLifetime;
        bolt.MaxLifetime = bolt.Lifetime;
        bolt.Intensity = 1.0f;
        bolt.BranchCount = Random.Shared.NextSingle() < _branchProbability ? _branchCount : 0;
        bolt.Padding = Vector4.Zero;

        _nextBoltIndex = (_nextBoltIndex + 1) % MaxBolts;
    }

    private void SpawnSparks(Vector2 position, Vector4 color)
    {
        for (int i = 0; i < _sparkCount; i++)
        {
            float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
            float speed = Random.Shared.NextSingle() * _sparkSpeed;
            Vector2 velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;

            ref ImpactSpark spark = ref _sparks[_nextSparkIndex];
            spark.Position = position;
            spark.Velocity = velocity;
            spark.Color = color;

            _nextSparkIndex = (_nextSparkIndex + 1) % MaxSparks;
        }
    }

    private Vector4 GetBoltColor()
    {
        return _colorMode switch
        {
            0 => new Vector4(0.8f, 0.9f, 1f, 1f),      // White/Blue
            1 => new Vector4(0.8f, 0.4f, 1f, 1f),      // Purple
            2 => new Vector4(0.4f, 1f, 0.6f, 1f),      // Green
            3 => _customColor,                          // Custom
            _ => new Vector4(0.8f, 0.9f, 1f, 1f)
        };
    }

    private void UpdateBolts(float deltaTime)
    {
        _activeBoltCount = 0;
        for (int i = 0; i < MaxBolts; i++)
        {
            if (_bolts[i].Lifetime > 0)
            {
                _bolts[i].Lifetime -= deltaTime;
                if (_bolts[i].Lifetime > 0)
                    _activeBoltCount++;
            }
        }
    }

    private void UpdateSparks(float deltaTime)
    {
        _activeSparkCount = 0;
        for (int i = 0; i < MaxSparks; i++)
        {
            if (_sparks[i].Color.W > 0) // Using alpha as "alive" flag
            {
                // Apply gravity and update position
                _sparks[i].Velocity += new Vector2(0, 200f) * deltaTime;
                _sparks[i].Position += _sparks[i].Velocity * deltaTime;

                // Fade out
                _sparks[i].Color.W -= deltaTime / _sparkLifetime;
                if (_sparks[i].Color.W > 0)
                    _activeSparkCount++;
            }
        }
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeBoltCount == 0 && _activeSparkCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU bolt buffer
        int gpuIndex = 0;
        for (int i = 0; i < MaxBolts && gpuIndex < MaxBolts; i++)
        {
            if (_bolts[i].Lifetime > 0)
            {
                _gpuBolts[gpuIndex++] = _bolts[i];
            }
        }

        // Fill remaining with zeroed bolts
        for (int i = gpuIndex; i < MaxBolts; i++)
        {
            _gpuBolts[i] = default;
        }

        // Update bolt buffer
        context.UpdateBuffer(_boltBuffer!, (ReadOnlySpan<LightningBolt>)_gpuBolts.AsSpan());

        // Build GPU spark buffer (if enabled)
        if (_enableSparks)
        {
            int sparkIndex = 0;
            for (int i = 0; i < MaxSparks && sparkIndex < MaxSparks; i++)
            {
                if (_sparks[i].Color.W > 0)
                {
                    _gpuSparks[sparkIndex++] = _sparks[i];
                }
            }

            for (int i = sparkIndex; i < MaxSparks; i++)
            {
                _gpuSparks[i] = default;
            }

            context.UpdateBuffer(_sparkBuffer!, (ReadOnlySpan<ImpactSpark>)_gpuSparks.AsSpan());
        }

        // Update constant buffer
        var constants = new LightningConstants
        {
            ViewportSize = context.ViewportSize,
            MousePosition = _lastMousePos,
            Time = currentTime,
            BoltThickness = _boltThickness,
            FlickerSpeed = _flickerSpeed,
            FlashIntensity = _flashIntensity,
            GlowIntensity = _glowIntensity,
            BranchIntensity = _branchProbability,
            HdrMultiplier = context.HdrPeakBrightness,
            Padding1 = 0f,
            Padding2 = Vector4.Zero
        };
        context.UpdateBuffer(_constantBuffer!, constants);

        // Set up rendering state
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _boltBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 1, _sparkBuffer!);
        context.SetBlendState(BlendMode.Additive);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw fullscreen triangle
        context.Draw(3, 0);

        // Restore blend state
        context.SetBlendState(BlendMode.Alpha);
    }

    protected override void OnViewportSizeChanged(Vector2 newSize)
    {
        _viewportSize = newSize;
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _boltBuffer?.Dispose();
        _sparkBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.LightningStorm.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
