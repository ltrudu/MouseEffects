using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

using CoreMouseButtons = MouseEffects.Core.Input.MouseButtons;

namespace MouseEffects.Effects.Tesla;

public sealed class TeslaEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "tesla",
        Name = "Tesla",
        Description = "Creates electrical lightning bolt effects around the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Other
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 48)]
    private struct TeslaConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public Vector2 MousePosition;     // 8 bytes = 16
        public float Time;                // 4 bytes
        public float BoltIntensity;       // 4 bytes
        public float BoltThickness;       // 4 bytes
        public float FlickerSpeed;        // 4 bytes = 32
        public float GlowIntensity;       // 4 bytes
        public float FadeDuration;        // 4 bytes
        public float HdrMultiplier;       // 4 bytes - HDR peak brightness
        public float Padding;             // 4 bytes = 48
    }

    [StructLayout(LayoutKind.Sequential, Size = 48)]
    private struct BoltInstance
    {
        public Vector2 Position;          // 8 bytes - Origin position
        public float Angle;               // 4 bytes - Direction angle (radians)
        public float Length;              // 4 bytes = 16
        public Vector4 Color;             // 16 bytes = 32
        public float Lifetime;            // 4 bytes - Current life remaining
        public float MaxLifetime;         // 4 bytes - Total lifetime
        public float BranchSeed;          // 4 bytes - Random seed for branches
        public float Padding;             // 4 bytes = 48
    }

    // Trail Constants for Electrical Follow effect
    [StructLayout(LayoutKind.Sequential, Size = 160)]
    private struct TrailConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public float Time;                // 4 bytes
        public float GlowIntensity;       // 4 bytes = 16
        public float FlickerSpeed;        // 4 bytes
        public float FlickerIntensity;    // 4 bytes
        public float CrackleIntensity;    // 4 bytes
        public float LineThickness;       // 4 bytes = 32
        public Vector4 PrimaryColor;      // 16 bytes = 48
        public Vector4 SecondaryColor;    // 16 bytes = 64
        public float BurstProbability;    // 4 bytes
        public float BurstIntensity;      // 4 bytes
        public float NoiseScale;          // 4 bytes
        public float BranchBoltEnabled;   // 4 bytes = 80
        public float BranchBoltCount;     // 4 bytes
        public float BranchBoltLength;    // 4 bytes
        public float BranchBoltThickness; // 4 bytes
        public float BranchBoltSpread;    // 4 bytes = 96
        public float SparkleEnabled;      // 4 bytes
        public float SparkleCount;        // 4 bytes
        public float SparkleSize;         // 4 bytes
        public float SparkleIntensity;    // 4 bytes = 112
        public Vector4 BranchBoltColor;   // 16 bytes = 128
        public Vector4 SparkleColor;      // 16 bytes = 144
        public float HdrMultiplier;       // 4 bytes - HDR peak brightness
        public Vector3 Padding2;          // 12 bytes = 160
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    private struct TrailPoint
    {
        public Vector2 Position;          // 8 bytes
        public float Lifetime;            // 4 bytes
        public float MaxLifetime;         // 4 bytes = 16
        public Vector4 Color;             // 16 bytes = 32
    }

    // Constants
    private const int MaxBolts = 256;
    private const int MaxTrailPoints = 512;

    // GPU Resources - Lightning Bolts
    private IBuffer? _constantBuffer;
    private IBuffer? _boltBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // GPU Resources - Electrical Follow Trail
    private IBuffer? _trailConstantBuffer;
    private IBuffer? _trailPointBuffer;
    private IShader? _trailVertexShader;
    private IShader? _trailPixelShader;

    // Bolt management (CPU side)
    private readonly BoltInstance[] _bolts = new BoltInstance[MaxBolts];
    private readonly BoltInstance[] _gpuBolts = new BoltInstance[MaxBolts];
    private int _nextBoltIndex;
    private int _activeBoltCount;

    // Trail point management (CPU side) - circular buffer for connected trail
    private readonly TrailPoint[] _trailPoints = new TrailPoint[MaxTrailPoints];
    private readonly TrailPoint[] _gpuTrailPoints = new TrailPoint[MaxTrailPoints];
    private int _trailHeadIndex;  // Index where next point will be added
    private int _trailPointCount; // Number of active points in the trail
    private float _trailAccumulatedDistance;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private Vector2 _lastVelocity;
    private float _accumulatedDistance;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;

    // Random distance management
    private float _currentDistanceThreshold;
    private float _lastDistanceRecalcTime;

    // Rainbow hue tracking
    private float _rainbowHue;

    // Viewport
    private Vector2 _viewportSize;

    // Configuration fields - Legacy (kept for backward compatibility)
    private TriggerType _mouseMoveEffect = TriggerType.LightningBolt;
    private TriggerType _leftClickEffect = TriggerType.LightningBolt;
    private TriggerType _rightClickEffect = TriggerType.None;

    // Separate trigger flags for each effect
    // Lightning Bolt triggers
    private bool _lbMouseMoveEnabled = true;
    private bool _lbLeftClickEnabled = true;
    private bool _lbRightClickEnabled;

    // Electrical Follow triggers
    private bool _efMouseMoveEnabled;

    // Move trigger settings
    private float _moveDistanceThreshold = 50f;
    private bool _randomDistanceEnabled;

    // Direction settings
    private DirectionMode _moveDirectionMode = DirectionMode.AllDirections;
    private DirectionMode _clickDirectionMode = DirectionMode.AllDirections;
    private float _spreadAngle = 180f;

    // Bolt count settings
    private bool _randomBoltCount = true;
    private int _minBoltCount = 3;
    private int _maxBoltCount = 6;
    private int _fixedBoltCount = 4;

    // Bolt appearance
    private float _minBoltLength = 50f;
    private float _maxBoltLength = 150f;
    private float _boltThickness = 1.0f;
    private float _branchProbability = 0.5f;

    // Colors
    private Vector4 _glowColor = new(0.3f, 0.5f, 1f, 1f);
    private bool _randomColorVariation;
    private bool _rainbowMode;
    private float _rainbowSpeed = 0.5f;

    // Timing
    private float _boltLifetime = 0.15f;
    private float _flickerSpeed = 15f;
    private float _fadeDuration = 0.05f;

    // Glow
    private float _glowIntensity = 0.8f;

    // Performance settings
    private int _maxActiveBolts = 64;
    private int _maxBoltsPerSecond = 30;
    private float _lastSpawnSecond;
    private int _boltsSpawnedThisSecond;

    // ===== Electrical Follow Configuration =====
    // General
    private int _efMaxPieces = 512;
    private float _efPieceSize = 8f;
    private float _efLifetime = 1.5f;
    private bool _efRandomLifetime;
    private float _efMinLifetime = 0.5f;
    private float _efMaxLifetime = 2.0f;

    // Appearance
    private float _efLineThickness = 1.5f;
    private bool _efRandomThickness;
    private float _efMinThickness = 0.8f;
    private float _efMaxThickness = 2.5f;
    private float _efGlowIntensity = 1.0f;
    private bool _efRandomGlow;
    private float _efMinGlow = 0.5f;
    private float _efMaxGlow = 1.5f;

    // Flicker
    private float _efFlickerSpeed = 20f;
    private float _efFlickerIntensity = 0.6f;
    private bool _efRandomFlicker;
    private float _efMinFlickerSpeed = 10f;
    private float _efMaxFlickerSpeed = 40f;

    // Crackle
    private float _efCrackleIntensity = 0.8f;
    private bool _efRandomCrackle;
    private float _efMinCrackle = 0.3f;
    private float _efMaxCrackle = 1.0f;
    private float _efNoiseScale = 1.0f;

    // Burst sparks
    private float _efBurstProbability = 0.3f;
    private float _efBurstIntensity = 0.8f;
    private bool _efRandomBurst;
    private float _efMinBurstProb = 0.1f;
    private float _efMaxBurstProb = 0.6f;

    // Colors
    private Vector4 _efPrimaryColor = new(0.4f, 0.6f, 1f, 1f);
    private Vector4 _efSecondaryColor = new(0.8f, 0.9f, 1f, 1f);
    private bool _efRandomColorVariation;
    private bool _efRainbowMode;
    private float _efRainbowSpeed = 0.3f;
    private float _efTrailRainbowHue;

    // Branch Bolts
    private bool _efBranchBoltEnabled = true;
    private int _efBranchBoltCount = 3;
    private bool _efRandomBranchCount;
    private int _efMinBranchCount = 1;
    private int _efMaxBranchCount = 5;
    private float _efBranchBoltLength = 25f;
    private bool _efRandomBranchLength;
    private float _efMinBranchLength = 10f;
    private float _efMaxBranchLength = 40f;
    private float _efBranchBoltThickness = 1.0f;
    private bool _efRandomBranchThickness;
    private float _efMinBranchThickness = 0.5f;
    private float _efMaxBranchThickness = 2.0f;
    private float _efBranchBoltSpread = 90f;
    private Vector4 _efBranchBoltColor = new(0.6f, 0.8f, 1f, 0f); // Alpha 0 = use segment color

    // Sparkles
    private bool _efSparkleEnabled = true;
    private int _efSparkleCount = 5;
    private bool _efRandomSparkleCount;
    private int _efMinSparkleCount = 2;
    private int _efMaxSparkleCount = 8;
    private float _efSparkleSize = 3f;
    private bool _efRandomSparkleSize;
    private float _efMinSparkleSize = 1f;
    private float _efMaxSparkleSize = 5f;
    private float _efSparkleIntensity = 1.0f;
    private bool _efRandomSparkleIntensity;
    private float _efMinSparkleIntensity = 0.5f;
    private float _efMaxSparkleIntensity = 1.5f;
    private Vector4 _efSparkleColor = new(1f, 1f, 1f, 0f); // Alpha 0 = use segment color

    // Public properties for UI binding
    // Legacy trigger properties (kept for backward compatibility)
    public TriggerType MouseMoveEffect { get => _mouseMoveEffect; set => _mouseMoveEffect = value; }
    public TriggerType LeftClickEffect { get => _leftClickEffect; set => _leftClickEffect = value; }
    public TriggerType RightClickEffect { get => _rightClickEffect; set => _rightClickEffect = value; }

    // Separate trigger properties for each effect
    // Lightning Bolt triggers
    public bool LbMouseMoveEnabled { get => _lbMouseMoveEnabled; set => _lbMouseMoveEnabled = value; }
    public bool LbLeftClickEnabled { get => _lbLeftClickEnabled; set => _lbLeftClickEnabled = value; }
    public bool LbRightClickEnabled { get => _lbRightClickEnabled; set => _lbRightClickEnabled = value; }

    // Electrical Follow trigger
    public bool EfMouseMoveEnabled { get => _efMouseMoveEnabled; set => _efMouseMoveEnabled = value; }
    public float MoveDistanceThreshold { get => _moveDistanceThreshold; set => _moveDistanceThreshold = value; }
    public bool RandomDistanceEnabled { get => _randomDistanceEnabled; set => _randomDistanceEnabled = value; }
    public DirectionMode MoveDirectionMode { get => _moveDirectionMode; set => _moveDirectionMode = value; }
    public DirectionMode ClickDirectionMode { get => _clickDirectionMode; set => _clickDirectionMode = value; }
    public float SpreadAngle { get => _spreadAngle; set => _spreadAngle = value; }
    public bool RandomBoltCount { get => _randomBoltCount; set => _randomBoltCount = value; }
    public int MinBoltCount { get => _minBoltCount; set => _minBoltCount = value; }
    public int MaxBoltCount { get => _maxBoltCount; set => _maxBoltCount = value; }
    public int FixedBoltCount { get => _fixedBoltCount; set => _fixedBoltCount = value; }
    public float MinBoltLength { get => _minBoltLength; set => _minBoltLength = value; }
    public float MaxBoltLength { get => _maxBoltLength; set => _maxBoltLength = value; }
    public float BoltThickness { get => _boltThickness; set => _boltThickness = value; }
    public float BranchProbability { get => _branchProbability; set => _branchProbability = value; }
    public Vector4 GlowColor { get => _glowColor; set => _glowColor = value; }
    public bool RandomColorVariation { get => _randomColorVariation; set => _randomColorVariation = value; }
    public bool RainbowMode { get => _rainbowMode; set => _rainbowMode = value; }
    public float RainbowSpeed { get => _rainbowSpeed; set => _rainbowSpeed = value; }
    public float BoltLifetime { get => _boltLifetime; set => _boltLifetime = value; }
    public float FlickerSpeed { get => _flickerSpeed; set => _flickerSpeed = value; }
    public float FadeDuration { get => _fadeDuration; set => _fadeDuration = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }

    // Performance properties
    public int MaxActiveBolts { get => _maxActiveBolts; set => _maxActiveBolts = value; }
    public int MaxBoltsPerSecond { get => _maxBoltsPerSecond; set => _maxBoltsPerSecond = value; }

    // Electrical Follow properties
    public int EfMaxPieces { get => _efMaxPieces; set => _efMaxPieces = value; }
    public float EfPieceSize { get => _efPieceSize; set => _efPieceSize = value; }
    public float EfLifetime { get => _efLifetime; set => _efLifetime = value; }
    public bool EfRandomLifetime { get => _efRandomLifetime; set => _efRandomLifetime = value; }
    public float EfMinLifetime { get => _efMinLifetime; set => _efMinLifetime = value; }
    public float EfMaxLifetime { get => _efMaxLifetime; set => _efMaxLifetime = value; }
    public float EfLineThickness { get => _efLineThickness; set => _efLineThickness = value; }
    public bool EfRandomThickness { get => _efRandomThickness; set => _efRandomThickness = value; }
    public float EfMinThickness { get => _efMinThickness; set => _efMinThickness = value; }
    public float EfMaxThickness { get => _efMaxThickness; set => _efMaxThickness = value; }
    public float EfGlowIntensity { get => _efGlowIntensity; set => _efGlowIntensity = value; }
    public bool EfRandomGlow { get => _efRandomGlow; set => _efRandomGlow = value; }
    public float EfMinGlow { get => _efMinGlow; set => _efMinGlow = value; }
    public float EfMaxGlow { get => _efMaxGlow; set => _efMaxGlow = value; }
    public float EfFlickerSpeed { get => _efFlickerSpeed; set => _efFlickerSpeed = value; }
    public float EfFlickerIntensity { get => _efFlickerIntensity; set => _efFlickerIntensity = value; }
    public bool EfRandomFlicker { get => _efRandomFlicker; set => _efRandomFlicker = value; }
    public float EfMinFlickerSpeed { get => _efMinFlickerSpeed; set => _efMinFlickerSpeed = value; }
    public float EfMaxFlickerSpeed { get => _efMaxFlickerSpeed; set => _efMaxFlickerSpeed = value; }
    public float EfCrackleIntensity { get => _efCrackleIntensity; set => _efCrackleIntensity = value; }
    public bool EfRandomCrackle { get => _efRandomCrackle; set => _efRandomCrackle = value; }
    public float EfMinCrackle { get => _efMinCrackle; set => _efMinCrackle = value; }
    public float EfMaxCrackle { get => _efMaxCrackle; set => _efMaxCrackle = value; }
    public float EfNoiseScale { get => _efNoiseScale; set => _efNoiseScale = value; }
    public float EfBurstProbability { get => _efBurstProbability; set => _efBurstProbability = value; }
    public float EfBurstIntensity { get => _efBurstIntensity; set => _efBurstIntensity = value; }
    public bool EfRandomBurst { get => _efRandomBurst; set => _efRandomBurst = value; }
    public float EfMinBurstProb { get => _efMinBurstProb; set => _efMinBurstProb = value; }
    public float EfMaxBurstProb { get => _efMaxBurstProb; set => _efMaxBurstProb = value; }
    public Vector4 EfPrimaryColor { get => _efPrimaryColor; set => _efPrimaryColor = value; }
    public Vector4 EfSecondaryColor { get => _efSecondaryColor; set => _efSecondaryColor = value; }
    public bool EfRandomColorVariation { get => _efRandomColorVariation; set => _efRandomColorVariation = value; }
    public bool EfRainbowMode { get => _efRainbowMode; set => _efRainbowMode = value; }
    public float EfRainbowSpeed { get => _efRainbowSpeed; set => _efRainbowSpeed = value; }

    // Branch Bolt properties
    public bool EfBranchBoltEnabled { get => _efBranchBoltEnabled; set => _efBranchBoltEnabled = value; }
    public int EfBranchBoltCount { get => _efBranchBoltCount; set => _efBranchBoltCount = value; }
    public bool EfRandomBranchCount { get => _efRandomBranchCount; set => _efRandomBranchCount = value; }
    public int EfMinBranchCount { get => _efMinBranchCount; set => _efMinBranchCount = value; }
    public int EfMaxBranchCount { get => _efMaxBranchCount; set => _efMaxBranchCount = value; }
    public float EfBranchBoltLength { get => _efBranchBoltLength; set => _efBranchBoltLength = value; }
    public bool EfRandomBranchLength { get => _efRandomBranchLength; set => _efRandomBranchLength = value; }
    public float EfMinBranchLength { get => _efMinBranchLength; set => _efMinBranchLength = value; }
    public float EfMaxBranchLength { get => _efMaxBranchLength; set => _efMaxBranchLength = value; }
    public float EfBranchBoltThickness { get => _efBranchBoltThickness; set => _efBranchBoltThickness = value; }
    public bool EfRandomBranchThickness { get => _efRandomBranchThickness; set => _efRandomBranchThickness = value; }
    public float EfMinBranchThickness { get => _efMinBranchThickness; set => _efMinBranchThickness = value; }
    public float EfMaxBranchThickness { get => _efMaxBranchThickness; set => _efMaxBranchThickness = value; }
    public float EfBranchBoltSpread { get => _efBranchBoltSpread; set => _efBranchBoltSpread = value; }
    public Vector4 EfBranchBoltColor { get => _efBranchBoltColor; set => _efBranchBoltColor = value; }

    // Sparkle properties
    public bool EfSparkleEnabled { get => _efSparkleEnabled; set => _efSparkleEnabled = value; }
    public int EfSparkleCount { get => _efSparkleCount; set => _efSparkleCount = value; }
    public bool EfRandomSparkleCount { get => _efRandomSparkleCount; set => _efRandomSparkleCount = value; }
    public int EfMinSparkleCount { get => _efMinSparkleCount; set => _efMinSparkleCount = value; }
    public int EfMaxSparkleCount { get => _efMaxSparkleCount; set => _efMaxSparkleCount = value; }
    public float EfSparkleSize { get => _efSparkleSize; set => _efSparkleSize = value; }
    public bool EfRandomSparkleSize { get => _efRandomSparkleSize; set => _efRandomSparkleSize = value; }
    public float EfMinSparkleSize { get => _efMinSparkleSize; set => _efMinSparkleSize = value; }
    public float EfMaxSparkleSize { get => _efMaxSparkleSize; set => _efMaxSparkleSize = value; }
    public float EfSparkleIntensity { get => _efSparkleIntensity; set => _efSparkleIntensity = value; }
    public bool EfRandomSparkleIntensity { get => _efRandomSparkleIntensity; set => _efRandomSparkleIntensity = value; }
    public float EfMinSparkleIntensity { get => _efMinSparkleIntensity; set => _efMinSparkleIntensity = value; }
    public float EfMaxSparkleIntensity { get => _efMaxSparkleIntensity; set => _efMaxSparkleIntensity = value; }
    public Vector4 EfSparkleColor { get => _efSparkleColor; set => _efSparkleColor = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        _viewportSize = context.ViewportSize;

        // Load and compile Lightning Bolt shaders
        string shaderSource = LoadEmbeddedShader("LightningBoltShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Load and compile Electrical Follow shaders
        string trailShaderSource = LoadEmbeddedShader("ElectricalFollowShader.hlsl");
        _trailVertexShader = context.CompileShader(trailShaderSource, "VSMain", ShaderStage.Vertex);
        _trailPixelShader = context.CompileShader(trailShaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer for Lightning Bolts
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<TeslaConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create bolt structured buffer
        _boltBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<BoltInstance>() * MaxBolts,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<BoltInstance>()
        });

        // Create constant buffer for Electrical Follow
        _trailConstantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<TrailConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create trail point structured buffer
        _trailPointBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<TrailPoint>() * MaxTrailPoints,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<TrailPoint>()
        });

        // Initialize distance threshold
        _currentDistanceThreshold = _moveDistanceThreshold;
    }

    protected override void OnConfigurationChanged()
    {
        // New separate trigger flags for each effect
        // Lightning Bolt triggers
        if (Configuration.TryGet("lb_mouseMoveEnabled", out bool lbMoveEnabled))
            _lbMouseMoveEnabled = lbMoveEnabled;
        if (Configuration.TryGet("lb_leftClickEnabled", out bool lbLeftEnabled))
            _lbLeftClickEnabled = lbLeftEnabled;
        if (Configuration.TryGet("lb_rightClickEnabled", out bool lbRightEnabled))
            _lbRightClickEnabled = lbRightEnabled;

        // Electrical Follow trigger
        if (Configuration.TryGet("ef_mouseMoveEnabled", out bool efMoveEnabled))
            _efMouseMoveEnabled = efMoveEnabled;

        // Legacy trigger settings (for backward compatibility)
        if (Configuration.TryGet("ts_mouseMoveEffect", out int moveEffect))
            _mouseMoveEffect = (TriggerType)moveEffect;
        if (Configuration.TryGet("ts_leftClickEffect", out int leftEffect))
            _leftClickEffect = (TriggerType)leftEffect;
        if (Configuration.TryGet("ts_rightClickEffect", out int rightEffect))
            _rightClickEffect = (TriggerType)rightEffect;

        // Move trigger settings
        if (Configuration.TryGet("mt_distanceThreshold", out float dist))
        {
            _moveDistanceThreshold = dist;
            _currentDistanceThreshold = dist;
        }
        if (Configuration.TryGet("mt_randomDistanceEnabled", out bool randDist))
            _randomDistanceEnabled = randDist;
        if (Configuration.TryGet("mt_directionMode", out int moveDirMode))
            _moveDirectionMode = (DirectionMode)moveDirMode;

        // Click trigger settings
        if (Configuration.TryGet("ct_directionMode", out int clickDirMode))
            _clickDirectionMode = (DirectionMode)clickDirMode;
        if (Configuration.TryGet("ct_spreadAngle", out float spread))
            _spreadAngle = spread;

        // Bolt count settings
        if (Configuration.TryGet("bc_randomCount", out bool randCount))
            _randomBoltCount = randCount;
        if (Configuration.TryGet("bc_minCount", out int minCount))
            _minBoltCount = minCount;
        if (Configuration.TryGet("bc_maxCount", out int maxCount))
            _maxBoltCount = maxCount;
        if (Configuration.TryGet("bc_fixedCount", out int fixedCount))
            _fixedBoltCount = fixedCount;

        // Bolt appearance settings
        if (Configuration.TryGet("ba_minLength", out float minLen))
            _minBoltLength = minLen;
        if (Configuration.TryGet("ba_maxLength", out float maxLen))
            _maxBoltLength = maxLen;
        if (Configuration.TryGet("ba_thickness", out float thick))
            _boltThickness = thick;
        if (Configuration.TryGet("ba_branchProbability", out float branchProb))
            _branchProbability = branchProb;

        // Color settings
        if (Configuration.TryGet("col_glow", out Vector4 glowCol))
            _glowColor = glowCol;
        if (Configuration.TryGet("col_randomVariation", out bool randVar))
            _randomColorVariation = randVar;
        if (Configuration.TryGet("col_rainbowMode", out bool rainbow))
            _rainbowMode = rainbow;
        if (Configuration.TryGet("col_rainbowSpeed", out float rainbowSpd))
            _rainbowSpeed = rainbowSpd;

        // Timing settings
        if (Configuration.TryGet("time_boltLifetime", out float lifetime))
            _boltLifetime = lifetime;
        if (Configuration.TryGet("time_flickerSpeed", out float flicker))
            _flickerSpeed = flicker;
        if (Configuration.TryGet("time_fadeDuration", out float fade))
            _fadeDuration = fade;

        // Glow settings
        if (Configuration.TryGet("glow_intensity", out float glowInt))
            _glowIntensity = glowInt;

        // Performance settings
        if (Configuration.TryGet("perf_maxActiveBolts", out int maxActive))
            _maxActiveBolts = Math.Clamp(maxActive, 16, MaxBolts);
        if (Configuration.TryGet("perf_maxBoltsPerSecond", out int maxPerSec))
            _maxBoltsPerSecond = Math.Clamp(maxPerSec, 10, 100);

        // ===== Electrical Follow Configuration =====
        // General
        if (Configuration.TryGet("ef_maxPieces", out int efMaxPcs))
            _efMaxPieces = efMaxPcs;
        if (Configuration.TryGet("ef_pieceSize", out float efPcSize))
            _efPieceSize = efPcSize;
        if (Configuration.TryGet("ef_lifetime", out float efLife))
            _efLifetime = efLife;
        if (Configuration.TryGet("ef_randomLifetime", out bool efRandLife))
            _efRandomLifetime = efRandLife;
        if (Configuration.TryGet("ef_minLifetime", out float efMinLife))
            _efMinLifetime = efMinLife;
        if (Configuration.TryGet("ef_maxLifetime", out float efMaxLife))
            _efMaxLifetime = efMaxLife;

        // Appearance
        if (Configuration.TryGet("ef_lineThickness", out float efThick))
            _efLineThickness = efThick;
        if (Configuration.TryGet("ef_randomThickness", out bool efRandThick))
            _efRandomThickness = efRandThick;
        if (Configuration.TryGet("ef_minThickness", out float efMinThick))
            _efMinThickness = efMinThick;
        if (Configuration.TryGet("ef_maxThickness", out float efMaxThick))
            _efMaxThickness = efMaxThick;
        if (Configuration.TryGet("ef_glowIntensity", out float efGlow))
            _efGlowIntensity = efGlow;
        if (Configuration.TryGet("ef_randomGlow", out bool efRandGlow))
            _efRandomGlow = efRandGlow;
        if (Configuration.TryGet("ef_minGlow", out float efMinGl))
            _efMinGlow = efMinGl;
        if (Configuration.TryGet("ef_maxGlow", out float efMaxGl))
            _efMaxGlow = efMaxGl;

        // Flicker
        if (Configuration.TryGet("ef_flickerSpeed", out float efFlickSpd))
            _efFlickerSpeed = efFlickSpd;
        if (Configuration.TryGet("ef_flickerIntensity", out float efFlickInt))
            _efFlickerIntensity = efFlickInt;
        if (Configuration.TryGet("ef_randomFlicker", out bool efRandFlick))
            _efRandomFlicker = efRandFlick;
        if (Configuration.TryGet("ef_minFlickerSpeed", out float efMinFlickSpd))
            _efMinFlickerSpeed = efMinFlickSpd;
        if (Configuration.TryGet("ef_maxFlickerSpeed", out float efMaxFlickSpd))
            _efMaxFlickerSpeed = efMaxFlickSpd;

        // Crackle
        if (Configuration.TryGet("ef_crackleIntensity", out float efCrackle))
            _efCrackleIntensity = efCrackle;
        if (Configuration.TryGet("ef_randomCrackle", out bool efRandCrackle))
            _efRandomCrackle = efRandCrackle;
        if (Configuration.TryGet("ef_minCrackle", out float efMinCrack))
            _efMinCrackle = efMinCrack;
        if (Configuration.TryGet("ef_maxCrackle", out float efMaxCrack))
            _efMaxCrackle = efMaxCrack;
        if (Configuration.TryGet("ef_noiseScale", out float efNoise))
            _efNoiseScale = efNoise;

        // Burst sparks
        if (Configuration.TryGet("ef_burstProbability", out float efBurstProb))
            _efBurstProbability = efBurstProb;
        if (Configuration.TryGet("ef_burstIntensity", out float efBurstInt))
            _efBurstIntensity = efBurstInt;
        if (Configuration.TryGet("ef_randomBurst", out bool efRandBurst))
            _efRandomBurst = efRandBurst;
        if (Configuration.TryGet("ef_minBurstProb", out float efMinBurst))
            _efMinBurstProb = efMinBurst;
        if (Configuration.TryGet("ef_maxBurstProb", out float efMaxBurst))
            _efMaxBurstProb = efMaxBurst;

        // Colors
        if (Configuration.TryGet("ef_primaryColor", out Vector4 efPrimCol))
            _efPrimaryColor = efPrimCol;
        if (Configuration.TryGet("ef_secondaryColor", out Vector4 efSecCol))
            _efSecondaryColor = efSecCol;
        if (Configuration.TryGet("ef_randomColorVariation", out bool efRandCol))
            _efRandomColorVariation = efRandCol;
        if (Configuration.TryGet("ef_rainbowMode", out bool efRainbow))
            _efRainbowMode = efRainbow;
        if (Configuration.TryGet("ef_rainbowSpeed", out float efRainbowSpd))
            _efRainbowSpeed = efRainbowSpd;

        // Branch Bolts
        if (Configuration.TryGet("ef_branchBoltEnabled", out bool efBranchEn))
            _efBranchBoltEnabled = efBranchEn;
        if (Configuration.TryGet("ef_branchBoltCount", out int efBranchCnt))
            _efBranchBoltCount = efBranchCnt;
        if (Configuration.TryGet("ef_randomBranchCount", out bool efRandBranchCnt))
            _efRandomBranchCount = efRandBranchCnt;
        if (Configuration.TryGet("ef_minBranchCount", out int efMinBranchCnt))
            _efMinBranchCount = efMinBranchCnt;
        if (Configuration.TryGet("ef_maxBranchCount", out int efMaxBranchCnt))
            _efMaxBranchCount = efMaxBranchCnt;
        if (Configuration.TryGet("ef_branchBoltLength", out float efBranchLen))
            _efBranchBoltLength = efBranchLen;
        if (Configuration.TryGet("ef_randomBranchLength", out bool efRandBranchLen))
            _efRandomBranchLength = efRandBranchLen;
        if (Configuration.TryGet("ef_minBranchLength", out float efMinBranchLen))
            _efMinBranchLength = efMinBranchLen;
        if (Configuration.TryGet("ef_maxBranchLength", out float efMaxBranchLen))
            _efMaxBranchLength = efMaxBranchLen;
        if (Configuration.TryGet("ef_branchBoltThickness", out float efBranchThick))
            _efBranchBoltThickness = efBranchThick;
        if (Configuration.TryGet("ef_randomBranchThickness", out bool efRandBranchThick))
            _efRandomBranchThickness = efRandBranchThick;
        if (Configuration.TryGet("ef_minBranchThickness", out float efMinBranchThick))
            _efMinBranchThickness = efMinBranchThick;
        if (Configuration.TryGet("ef_maxBranchThickness", out float efMaxBranchThick))
            _efMaxBranchThickness = efMaxBranchThick;
        if (Configuration.TryGet("ef_branchBoltSpread", out float efBranchSpread))
            _efBranchBoltSpread = efBranchSpread;
        if (Configuration.TryGet("ef_branchBoltColor", out Vector4 efBranchCol))
            _efBranchBoltColor = efBranchCol;

        // Sparkles
        if (Configuration.TryGet("ef_sparkleEnabled", out bool efSparkleEn))
            _efSparkleEnabled = efSparkleEn;
        if (Configuration.TryGet("ef_sparkleCount", out int efSparkleCnt))
            _efSparkleCount = efSparkleCnt;
        if (Configuration.TryGet("ef_randomSparkleCount", out bool efRandSparkleCnt))
            _efRandomSparkleCount = efRandSparkleCnt;
        if (Configuration.TryGet("ef_minSparkleCount", out int efMinSparkleCnt))
            _efMinSparkleCount = efMinSparkleCnt;
        if (Configuration.TryGet("ef_maxSparkleCount", out int efMaxSparkleCnt))
            _efMaxSparkleCount = efMaxSparkleCnt;
        if (Configuration.TryGet("ef_sparkleSize", out float efSparkleSize))
            _efSparkleSize = efSparkleSize;
        if (Configuration.TryGet("ef_randomSparkleSize", out bool efRandSparkleSize))
            _efRandomSparkleSize = efRandSparkleSize;
        if (Configuration.TryGet("ef_minSparkleSize", out float efMinSparkleSize))
            _efMinSparkleSize = efMinSparkleSize;
        if (Configuration.TryGet("ef_maxSparkleSize", out float efMaxSparkleSize))
            _efMaxSparkleSize = efMaxSparkleSize;
        if (Configuration.TryGet("ef_sparkleIntensity", out float efSparkleInt))
            _efSparkleIntensity = efSparkleInt;
        if (Configuration.TryGet("ef_randomSparkleIntensity", out bool efRandSparkleInt))
            _efRandomSparkleIntensity = efRandSparkleInt;
        if (Configuration.TryGet("ef_minSparkleIntensity", out float efMinSparkleInt))
            _efMinSparkleIntensity = efMinSparkleInt;
        if (Configuration.TryGet("ef_maxSparkleIntensity", out float efMaxSparkleInt))
            _efMaxSparkleIntensity = efMaxSparkleInt;
        if (Configuration.TryGet("ef_sparkleColor", out Vector4 efSparkleCol))
            _efSparkleColor = efSparkleCol;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        float totalTime = gameTime.TotalSeconds;

        // Update rainbow hue for Lightning Bolts
        if (_rainbowMode)
        {
            _rainbowHue += _rainbowSpeed * deltaTime;
            if (_rainbowHue > 1f) _rainbowHue -= 1f;
        }

        // Update rainbow hue for Electrical Follow
        if (_efRainbowMode)
        {
            _efTrailRainbowHue += _efRainbowSpeed * deltaTime;
            if (_efTrailRainbowHue > 1f) _efTrailRainbowHue -= 1f;
        }

        // Update random distance threshold every second
        if (_randomDistanceEnabled)
        {
            if (totalTime - _lastDistanceRecalcTime >= 1.0f)
            {
                _currentDistanceThreshold = 1f + Random.Shared.NextSingle() * (_moveDistanceThreshold - 1f);
                _lastDistanceRecalcTime = totalTime;
            }
        }
        else
        {
            _currentDistanceThreshold = _moveDistanceThreshold;
        }

        // Track mouse velocity
        _lastVelocity = mouseState.Velocity;

        // Calculate distance moved this frame
        float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);

        // Handle mouse move trigger for Lightning Bolt
        if (_lbMouseMoveEnabled)
        {
            _accumulatedDistance += distanceFromLast;

            if (_accumulatedDistance >= _currentDistanceThreshold)
            {
                SpawnLightning(mouseState.Position, _lastVelocity, totalTime, isClick: false);
                _accumulatedDistance = 0f;
            }
        }

        // Handle mouse move trigger for Electrical Follow
        if (_efMouseMoveEnabled)
        {
            _trailAccumulatedDistance += distanceFromLast;

            // Spawn trail points based on piece size (distance threshold)
            while (_trailAccumulatedDistance >= _efPieceSize && _trailPointCount < _efMaxPieces)
            {
                // Calculate position along the movement path
                float t = _efPieceSize / _trailAccumulatedDistance;
                Vector2 pointPos = Vector2.Lerp(_lastMousePos, mouseState.Position, t);

                SpawnTrailPoint(pointPos);
                _trailAccumulatedDistance -= _efPieceSize;
            }
        }

        // Handle left click trigger for Lightning Bolt
        bool leftPressed = mouseState.IsButtonPressed(CoreMouseButtons.Left);
        if (_lbLeftClickEnabled && leftPressed && !_wasLeftPressed)
        {
            SpawnLightning(mouseState.Position, null, totalTime, isClick: true);
        }
        _wasLeftPressed = leftPressed;

        // Handle right click trigger for Lightning Bolt
        bool rightPressed = mouseState.IsButtonPressed(CoreMouseButtons.Right);
        if (_lbRightClickEnabled && rightPressed && !_wasRightPressed)
        {
            SpawnLightning(mouseState.Position, null, totalTime, isClick: true);
        }
        _wasRightPressed = rightPressed;

        // Update last mouse position
        _lastMousePos = mouseState.Position;

        // Update existing bolts (age them)
        UpdateBolts(deltaTime);

        // Update existing trail points (age them)
        UpdateTrailPoints(deltaTime);
    }

    private void SpawnLightning(Vector2 position, Vector2? velocity, float time, bool isClick)
    {
        // Performance: Check rate limiting
        float currentSecond = MathF.Floor(time);
        if (currentSecond != _lastSpawnSecond)
        {
            _lastSpawnSecond = currentSecond;
            _boltsSpawnedThisSecond = 0;
        }

        // Performance: Check if we've hit the rate limit
        if (_boltsSpawnedThisSecond >= _maxBoltsPerSecond)
            return;

        // Performance: Check active bolt count
        if (_activeBoltCount >= _maxActiveBolts)
            return;

        // Determine bolt count
        int boltCount = _randomBoltCount
            ? Random.Shared.Next(_minBoltCount, _maxBoltCount + 1)
            : _fixedBoltCount;

        // Performance: Limit bolt count to not exceed limits
        int remainingRate = _maxBoltsPerSecond - _boltsSpawnedThisSecond;
        int remainingActive = _maxActiveBolts - _activeBoltCount;
        boltCount = Math.Min(boltCount, Math.Min(remainingRate, remainingActive));

        // Determine direction mode
        DirectionMode dirMode = isClick ? _clickDirectionMode : _moveDirectionMode;

        // Velocity-based not valid for clicks
        if (isClick && dirMode == DirectionMode.VelocityBased)
            dirMode = DirectionMode.AllDirections;

        // Calculate base angle for velocity-based mode
        float baseAngle = 0f;
        if (dirMode == DirectionMode.VelocityBased && velocity.HasValue && velocity.Value.LengthSquared() > 0.01f)
        {
            baseAngle = MathF.Atan2(velocity.Value.Y, velocity.Value.X);
        }

        // Spawn bolts
        for (int i = 0; i < boltCount; i++)
        {
            float angle;
            switch (dirMode)
            {
                case DirectionMode.AllDirections:
                    angle = Random.Shared.NextSingle() * MathF.PI * 2f;
                    break;

                case DirectionMode.ConfigurableSpread:
                    float spreadRad = _spreadAngle * MathF.PI / 180f;
                    angle = baseAngle - spreadRad / 2f + Random.Shared.NextSingle() * spreadRad;
                    break;

                case DirectionMode.VelocityBased:
                    // Spread bolts around velocity direction
                    float velSpread = MathF.PI / 3f; // 60 degree spread
                    angle = baseAngle - velSpread / 2f + Random.Shared.NextSingle() * velSpread;
                    break;

                default:
                    angle = Random.Shared.NextSingle() * MathF.PI * 2f;
                    break;
            }

            // Calculate length
            float length = _minBoltLength + Random.Shared.NextSingle() * (_maxBoltLength - _minBoltLength);

            // Get color
            Vector4 color = GetBoltColor();

            // Create bolt instance
            ref BoltInstance bolt = ref _bolts[_nextBoltIndex];
            bolt.Position = position;
            bolt.Angle = angle;
            bolt.Length = length;
            bolt.Color = color;
            bolt.Lifetime = _boltLifetime;
            bolt.MaxLifetime = _boltLifetime;
            bolt.BranchSeed = _branchProbability > 0 ? Random.Shared.NextSingle() : 0f;
            bolt.Padding = 0f;

            _nextBoltIndex = (_nextBoltIndex + 1) % MaxBolts;
            _boltsSpawnedThisSecond++;
        }
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

    private void SpawnTrailPoint(Vector2 position)
    {
        // Determine lifetime (with optional randomization)
        float lifetime = _efRandomLifetime
            ? _efMinLifetime + Random.Shared.NextSingle() * (_efMaxLifetime - _efMinLifetime)
            : _efLifetime;

        // Get color
        Vector4 color = GetTrailColor();

        // Create trail point - just store position, no angle needed
        ref TrailPoint point = ref _trailPoints[_trailHeadIndex];
        point.Position = position;
        point.Lifetime = lifetime;
        point.MaxLifetime = lifetime;
        point.Color = color;

        _trailHeadIndex = (_trailHeadIndex + 1) % MaxTrailPoints;
        _trailPointCount = Math.Min(_trailPointCount + 1, MaxTrailPoints);
    }

    private void UpdateTrailPoints(float deltaTime)
    {
        _trailPointCount = 0;
        for (int i = 0; i < MaxTrailPoints; i++)
        {
            if (_trailPoints[i].Lifetime > 0)
            {
                _trailPoints[i].Lifetime -= deltaTime;
                if (_trailPoints[i].Lifetime > 0)
                    _trailPointCount++;
            }
        }
    }

    private Vector4 GetTrailColor()
    {
        Vector4 baseColor;

        if (_efRainbowMode)
        {
            baseColor = HueToRgb(_efTrailRainbowHue + Random.Shared.NextSingle() * 0.1f);
        }
        else
        {
            // Lerp between primary and secondary colors randomly
            float t = Random.Shared.NextSingle();
            baseColor = Vector4.Lerp(_efPrimaryColor, _efSecondaryColor, t);
        }

        // Apply random color variation
        if (_efRandomColorVariation)
        {
            baseColor.X = MathF.Max(0f, MathF.Min(1f, baseColor.X + (Random.Shared.NextSingle() - 0.5f) * 0.3f));
            baseColor.Y = MathF.Max(0f, MathF.Min(1f, baseColor.Y + (Random.Shared.NextSingle() - 0.5f) * 0.3f));
            baseColor.Z = MathF.Max(0f, MathF.Min(1f, baseColor.Z + (Random.Shared.NextSingle() - 0.5f) * 0.3f));
        }

        return baseColor;
    }

    private Vector4 GetBoltColor()
    {
        Vector4 baseColor;

        if (_rainbowMode)
        {
            baseColor = HueToRgb(_rainbowHue + Random.Shared.NextSingle() * 0.1f);
        }
        else
        {
            baseColor = _glowColor;
        }

        // Apply random variation
        if (_randomColorVariation)
        {
            baseColor.X = MathF.Max(0f, MathF.Min(1f, baseColor.X + (Random.Shared.NextSingle() - 0.5f) * 0.3f));
            baseColor.Y = MathF.Max(0f, MathF.Min(1f, baseColor.Y + (Random.Shared.NextSingle() - 0.5f) * 0.3f));
            baseColor.Z = MathF.Max(0f, MathF.Min(1f, baseColor.Z + (Random.Shared.NextSingle() - 0.5f) * 0.3f));
        }

        return baseColor;
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

    protected override void OnRender(IRenderContext context)
    {
        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Render Lightning Bolts if there are active bolts
        if (_activeBoltCount > 0)
        {
            RenderLightningBolts(context, currentTime);
        }

        // Render Electrical Follow trail if there are active trail points
        if (_trailPointCount > 0)
        {
            RenderElectricalFollow(context, currentTime);
        }
    }

    private void RenderLightningBolts(IRenderContext context, float currentTime)
    {
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
        context.UpdateBuffer(_boltBuffer!, (ReadOnlySpan<BoltInstance>)_gpuBolts.AsSpan());

        // Update constant buffer
        var constants = new TeslaConstants
        {
            ViewportSize = context.ViewportSize,
            MousePosition = _lastMousePos,
            Time = currentTime,
            BoltIntensity = 0.1f,
            BoltThickness = _boltThickness,
            FlickerSpeed = _flickerSpeed,
            GlowIntensity = _glowIntensity,
            FadeDuration = _fadeDuration,
            HdrMultiplier = context.HdrPeakBrightness,
            Padding = 0f
        };
        context.UpdateBuffer(_constantBuffer!, constants);

        // Set up rendering state
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _boltBuffer!);
        context.SetBlendState(BlendMode.Additive);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw fullscreen triangle
        context.Draw(3, 0);

        // Restore blend state
        context.SetBlendState(BlendMode.Alpha);
    }

    private void RenderElectricalFollow(IRenderContext context, float currentTime)
    {
        // Build GPU trail point buffer - preserve order for point-to-point rendering
        int gpuIndex = 0;
        for (int i = 0; i < MaxTrailPoints && gpuIndex < MaxTrailPoints; i++)
        {
            if (_trailPoints[i].Lifetime > 0)
            {
                _gpuTrailPoints[gpuIndex++] = _trailPoints[i];
            }
        }

        // Fill remaining with zeroed points
        for (int i = gpuIndex; i < MaxTrailPoints; i++)
        {
            _gpuTrailPoints[i] = default;
        }

        // Update trail point buffer
        context.UpdateBuffer(_trailPointBuffer!, (ReadOnlySpan<TrailPoint>)_gpuTrailPoints.AsSpan());

        // Get randomized parameters if enabled
        float glowIntensity = _efRandomGlow
            ? _efMinGlow + Random.Shared.NextSingle() * (_efMaxGlow - _efMinGlow)
            : _efGlowIntensity;
        float flickerSpeed = _efRandomFlicker
            ? _efMinFlickerSpeed + Random.Shared.NextSingle() * (_efMaxFlickerSpeed - _efMinFlickerSpeed)
            : _efFlickerSpeed;
        float crackleIntensity = _efRandomCrackle
            ? _efMinCrackle + Random.Shared.NextSingle() * (_efMaxCrackle - _efMinCrackle)
            : _efCrackleIntensity;
        float lineThickness = _efRandomThickness
            ? _efMinThickness + Random.Shared.NextSingle() * (_efMaxThickness - _efMinThickness)
            : _efLineThickness;
        float burstProbability = _efRandomBurst
            ? _efMinBurstProb + Random.Shared.NextSingle() * (_efMaxBurstProb - _efMinBurstProb)
            : _efBurstProbability;

        // Get randomized branch bolt parameters
        int branchCount = _efRandomBranchCount
            ? Random.Shared.Next(_efMinBranchCount, _efMaxBranchCount + 1)
            : _efBranchBoltCount;
        float branchLength = _efRandomBranchLength
            ? _efMinBranchLength + Random.Shared.NextSingle() * (_efMaxBranchLength - _efMinBranchLength)
            : _efBranchBoltLength;
        float branchThickness = _efRandomBranchThickness
            ? _efMinBranchThickness + Random.Shared.NextSingle() * (_efMaxBranchThickness - _efMinBranchThickness)
            : _efBranchBoltThickness;

        // Get randomized sparkle parameters
        int sparkleCount = _efRandomSparkleCount
            ? Random.Shared.Next(_efMinSparkleCount, _efMaxSparkleCount + 1)
            : _efSparkleCount;
        float sparkleSize = _efRandomSparkleSize
            ? _efMinSparkleSize + Random.Shared.NextSingle() * (_efMaxSparkleSize - _efMinSparkleSize)
            : _efSparkleSize;
        float sparkleIntensity = _efRandomSparkleIntensity
            ? _efMinSparkleIntensity + Random.Shared.NextSingle() * (_efMaxSparkleIntensity - _efMinSparkleIntensity)
            : _efSparkleIntensity;

        // Update trail constant buffer
        var trailConstants = new TrailConstants
        {
            ViewportSize = context.ViewportSize,
            Time = currentTime,
            GlowIntensity = glowIntensity,
            FlickerSpeed = flickerSpeed,
            FlickerIntensity = _efFlickerIntensity,
            CrackleIntensity = crackleIntensity,
            LineThickness = lineThickness,
            PrimaryColor = _efPrimaryColor,
            SecondaryColor = _efSecondaryColor,
            BurstProbability = burstProbability,
            BurstIntensity = _efBurstIntensity,
            NoiseScale = _efNoiseScale,
            BranchBoltEnabled = _efBranchBoltEnabled ? 1f : 0f,
            BranchBoltCount = branchCount,
            BranchBoltLength = branchLength,
            BranchBoltThickness = branchThickness,
            BranchBoltSpread = _efBranchBoltSpread,
            SparkleEnabled = _efSparkleEnabled ? 1f : 0f,
            SparkleCount = sparkleCount,
            SparkleSize = sparkleSize,
            SparkleIntensity = sparkleIntensity,
            BranchBoltColor = _efBranchBoltColor,
            SparkleColor = _efSparkleColor,
            HdrMultiplier = context.HdrPeakBrightness,
            Padding2 = Vector3.Zero
        };
        context.UpdateBuffer(_trailConstantBuffer!, trailConstants);

        // Set up rendering state for trail
        context.SetVertexShader(_trailVertexShader!);
        context.SetPixelShader(_trailPixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _trailConstantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _trailConstantBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _trailPointBuffer!);
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
        // Dispose Lightning Bolt resources
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _boltBuffer?.Dispose();

        // Dispose Electrical Follow resources
        _trailVertexShader?.Dispose();
        _trailPixelShader?.Dispose();
        _trailConstantBuffer?.Dispose();
        _trailPointBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.Tesla.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
