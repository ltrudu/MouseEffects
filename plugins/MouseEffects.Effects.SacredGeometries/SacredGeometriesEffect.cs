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
        Category = EffectCategory.Artistic
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

        public int MorphEnabled;           // 4 bytes (bool as int)
        public float MorphSpeed;           // 4 bytes
        public float MorphIntensity;       // 4 bytes
        public int MorphBetweenPatterns;   // 4 bytes (bool as int) = 112

        public Vector4 Padding3;           // 16 bytes = 128
    }

    [StructLayout(LayoutKind.Sequential, Size = 80)]
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

        public float MorphPhase;           // 4 bytes - Current morph animation phase
        public float MorphTargetPattern;   // 4 bytes - Target pattern to morph to (-1 = none)
        public float MorphSpeed;           // 4 bytes - Individual morph speed multiplier
        public float Padding;              // 4 bytes = 80 - Padding for alignment
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

    // Morphing settings
    private bool _morphEnabled = true;
    private float _morphSpeed = 0.5f;
    private float _morphIntensity = 0.5f;
    private bool _morphBetweenPatterns = true;

    // ===== Shapes Effect Fields =====
    private int _selectedEffectType; // 0 = Mandala, 1 = Shapes

    // Shape settings
    private ShapeType _selectedShape = ShapeType.FlowerOfLife;
    private bool _randomShapeEnabled = true;
    private bool _cycleShapesEnabled;
    private float _cycleSpeed = 3.0f;
    private float _shapeRadius = 120f;
    private bool _pulseRadiusEnabled = true;
    private float _pulseAmount = 0.15f;
    private float _pulseSpeed = 1.0f;
    private float _shapeRotationSpeed = 20f;
    private int _shapeRotationDirection = 2; // 0=CW, 1=CCW, 2=Random
    private bool _shapeRainbowMode = true;
    private float _shapeRainbowSpeed = 0.3f;
    private bool _shapeIndependentRainbow = true;
    private Vector4 _shapePrimaryColor = new(0.6f, 0.4f, 1f, 1f);
    private Vector4 _shapeSecondaryColor = new(0.3f, 0.9f, 1f, 1f);
    private float _shapeGlowIntensity = 1.5f;
    private float _shapeLineThickness = 0.015f;
    private float _shapeTwinkleIntensity = 0.2f;
    private float _shapeAnimationSpeed = 1.0f;
    private AppearanceMode _shapeAppearanceMode = AppearanceMode.Both;
    private int _maxShapeCount = 3;
    private bool _shapeMouseMoveEnabled = true;
    private float _shapeMoveDistanceThreshold = 100f;
    private bool _shapeLeftClickEnabled = true;
    private bool _shapeRightClickEnabled;
    private float _shapeLifetimeDuration = 4.0f;
    private int _maxActiveShapes = 20;
    private int _shapeMaxSpawnsPerSecond = 10;

    // Shapes morphing settings
    private bool _shapeMorphEnabled = true;
    private float _shapeMorphSpeed = 0.5f;
    private float _shapeMorphIntensity = 0.5f;
    private bool _shapeMorphBetweenShapes = true;

    // Shapes GPU resources (lazy initialized)
    private IBuffer? _shapesConstantBuffer;
    private IBuffer? _shapesInstanceBuffer;
    private IShader? _shapesVertexShader;
    private IShader? _shapesPixelShader;
    private bool _shapesInitialized;

    // Shapes state
    private readonly ShapeInstanceData[] _shapes = new ShapeInstanceData[MaxMandalas];
    private readonly ShapeInstanceData[] _gpuShapes = new ShapeInstanceData[MaxMandalas];
    private int _activeShapeCount;
    private float _shapeRainbowHue;

    // Shuffled shape queue - ensures all shapes are shown before repeating
    private const int TotalShapeTypes = 20;
    private readonly List<int> _shapeQueue = new(TotalShapeTypes);
    private int _shapeQueueIndex;

    // Shapes GPU structure
    [StructLayout(LayoutKind.Sequential, Size = 128)]
    private struct ShapesConstants
    {
        public Vector2 ViewportSize;       // 8
        public float Time;                 // 4
        public float GlowIntensity;        // 4 = 16

        public float HdrMultiplier;        // 4
        public float LineThickness;        // 4
        public int ActiveShapeCount;       // 4
        public float TwinkleIntensity;     // 4 = 32

        public Vector4 PrimaryColor;       // 16 = 48
        public Vector4 SecondaryColor;     // 16 = 64

        public float RainbowSpeed;         // 4
        public int RainbowEnabled;         // 4
        public float AnimationSpeed;       // 4
        public int MorphEnabled;           // 4 = 80

        public float MorphSpeed;           // 4
        public float MorphIntensity;       // 4
        public int MorphBetweenShapes;     // 4
        public int IndependentRainbow;     // 4 = 96

        public Vector3 Padding1;           // 12
        public float Padding1b;            // 4 = 112
        public Vector4 Padding2;           // 16 = 128
    }

    [StructLayout(LayoutKind.Sequential, Size = 80)]
    private struct ShapeInstanceData
    {
        public Vector2 Position;           // 8
        public float Radius;               // 4
        public float Rotation;             // 4 = 16

        public float RotationSpeed;        // 4
        public float RotationDirection;    // 4
        public float Lifetime;             // 4
        public float MaxLifetime;          // 4 = 32

        public int ShapeIndex;             // 4
        public int AppearMode;             // 4
        public float SpawnTime;            // 4
        public float AnimPhase;            // 4 = 48

        public Vector4 Color;              // 16 = 64

        public float MorphPhase;           // 4
        public int MorphTargetShape;       // 4
        public float MorphSpeedMult;       // 4
        public float Padding;              // 4 = 80
    }

    // Public properties for UI binding - Effect Type
    public int SelectedEffectType { get => _selectedEffectType; set => _selectedEffectType = value; }

    // Public properties for UI binding - Shapes
    public ShapeType SelectedShape { get => _selectedShape; set => _selectedShape = value; }
    public bool RandomShapeEnabled { get => _randomShapeEnabled; set => _randomShapeEnabled = value; }
    public bool CycleShapesEnabled { get => _cycleShapesEnabled; set => _cycleShapesEnabled = value; }
    public float CycleSpeed { get => _cycleSpeed; set => _cycleSpeed = value; }
    public float ShapeRadius { get => _shapeRadius; set => _shapeRadius = value; }
    public bool PulseRadiusEnabled { get => _pulseRadiusEnabled; set => _pulseRadiusEnabled = value; }
    public float PulseAmount { get => _pulseAmount; set => _pulseAmount = value; }
    public float PulseSpeed { get => _pulseSpeed; set => _pulseSpeed = value; }
    public float ShapeRotationSpeed { get => _shapeRotationSpeed; set => _shapeRotationSpeed = value; }
    public int ShapeRotationDirection { get => _shapeRotationDirection; set => _shapeRotationDirection = value; }
    public bool ShapeRainbowMode { get => _shapeRainbowMode; set => _shapeRainbowMode = value; }
    public float ShapeRainbowSpeed { get => _shapeRainbowSpeed; set => _shapeRainbowSpeed = value; }
    public bool ShapeIndependentRainbow { get => _shapeIndependentRainbow; set => _shapeIndependentRainbow = value; }
    public Vector4 ShapePrimaryColor { get => _shapePrimaryColor; set => _shapePrimaryColor = value; }
    public Vector4 ShapeSecondaryColor { get => _shapeSecondaryColor; set => _shapeSecondaryColor = value; }
    public float ShapeGlowIntensity { get => _shapeGlowIntensity; set => _shapeGlowIntensity = value; }
    public float ShapeLineThickness { get => _shapeLineThickness; set => _shapeLineThickness = value; }
    public float ShapeTwinkleIntensity { get => _shapeTwinkleIntensity; set => _shapeTwinkleIntensity = value; }
    public float ShapeAnimationSpeed { get => _shapeAnimationSpeed; set => _shapeAnimationSpeed = value; }
    public AppearanceMode ShapeAppearanceMode { get => _shapeAppearanceMode; set => _shapeAppearanceMode = value; }
    public int MaxShapeCount { get => _maxShapeCount; set => _maxShapeCount = value; }
    public bool ShapeMouseMoveEnabled { get => _shapeMouseMoveEnabled; set => _shapeMouseMoveEnabled = value; }
    public float ShapeMoveDistanceThreshold { get => _shapeMoveDistanceThreshold; set => _shapeMoveDistanceThreshold = value; }
    public bool ShapeLeftClickEnabled { get => _shapeLeftClickEnabled; set => _shapeLeftClickEnabled = value; }
    public bool ShapeRightClickEnabled { get => _shapeRightClickEnabled; set => _shapeRightClickEnabled = value; }
    public float ShapeLifetimeDuration { get => _shapeLifetimeDuration; set => _shapeLifetimeDuration = value; }
    public int MaxActiveShapes { get => _maxActiveShapes; set => _maxActiveShapes = value; }
    public int ShapeMaxSpawnsPerSecond { get => _shapeMaxSpawnsPerSecond; set => _shapeMaxSpawnsPerSecond = value; }
    public bool ShapeMorphEnabled { get => _shapeMorphEnabled; set => _shapeMorphEnabled = value; }
    public float ShapeMorphSpeed { get => _shapeMorphSpeed; set => _shapeMorphSpeed = value; }
    public float ShapeMorphIntensity { get => _shapeMorphIntensity; set => _shapeMorphIntensity = value; }
    public bool ShapeMorphBetweenShapes { get => _shapeMorphBetweenShapes; set => _shapeMorphBetweenShapes = value; }

    // Public properties for UI binding - Mandala
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
    public bool MorphEnabled { get => _morphEnabled; set => _morphEnabled = value; }
    public float MorphSpeed { get => _morphSpeed; set => _morphSpeed = value; }
    public float MorphIntensity { get => _morphIntensity; set => _morphIntensity = value; }
    public bool MorphBetweenPatterns { get => _morphBetweenPatterns; set => _morphBetweenPatterns = value; }

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

        // Morphing settings
        if (Configuration.TryGet("sg_morph_enabled", out bool morphEnabled))
            _morphEnabled = morphEnabled;
        if (Configuration.TryGet("sg_morph_speed", out float morphSpeed))
            _morphSpeed = morphSpeed;
        if (Configuration.TryGet("sg_morph_intensity", out float morphIntensity))
            _morphIntensity = morphIntensity;
        if (Configuration.TryGet("sg_morph_betweenPatterns", out bool morphBetween))
            _morphBetweenPatterns = morphBetween;

        // Effect type
        if (Configuration.TryGet("selectedEffectType", out int effectType))
            _selectedEffectType = effectType;

        // ===== Shapes Settings =====
        if (Configuration.TryGet("sh_shape_selected", out int shapeSelected))
            _selectedShape = (ShapeType)shapeSelected;
        if (Configuration.TryGet("sh_shape_randomEnabled", out bool shapeRandom))
            _randomShapeEnabled = shapeRandom;
        if (Configuration.TryGet("sh_shape_cycleEnabled", out bool shapeCycle))
            _cycleShapesEnabled = shapeCycle;
        if (Configuration.TryGet("sh_shape_cycleSpeed", out float shapeCycleSpeed))
            _cycleSpeed = shapeCycleSpeed;
        if (Configuration.TryGet("sh_rad_fixed", out float shapeRadius))
            _shapeRadius = shapeRadius;
        if (Configuration.TryGet("sh_rad_pulseEnabled", out bool shapePulse))
            _pulseRadiusEnabled = shapePulse;
        if (Configuration.TryGet("sh_rad_pulseAmount", out float shapePulseAmount))
            _pulseAmount = shapePulseAmount;
        if (Configuration.TryGet("sh_rad_pulseSpeed", out float shapePulseSpeed))
            _pulseSpeed = shapePulseSpeed;
        if (Configuration.TryGet("sh_rot_speed", out float shapeRotSpeed))
            _shapeRotationSpeed = shapeRotSpeed;
        if (Configuration.TryGet("sh_rot_direction", out int shapeRotDir))
            _shapeRotationDirection = shapeRotDir;
        if (Configuration.TryGet("sh_col_rainbowMode", out bool shapeRainbow))
            _shapeRainbowMode = shapeRainbow;
        if (Configuration.TryGet("sh_col_rainbowSpeed", out float shapeRainbowSpd))
            _shapeRainbowSpeed = shapeRainbowSpd;
        if (Configuration.TryGet("sh_col_independentRainbow", out bool shapeIndRainbow))
            _shapeIndependentRainbow = shapeIndRainbow;
        if (Configuration.TryGet("sh_col_primary", out Vector4 shapePrimary))
            _shapePrimaryColor = shapePrimary;
        if (Configuration.TryGet("sh_col_secondary", out Vector4 shapeSecondary))
            _shapeSecondaryColor = shapeSecondary;
        if (Configuration.TryGet("sh_glow_intensity", out float shapeGlow))
            _shapeGlowIntensity = shapeGlow;
        if (Configuration.TryGet("sh_glow_lineThickness", out float shapeThickness))
            _shapeLineThickness = shapeThickness;
        if (Configuration.TryGet("sh_glow_twinkleIntensity", out float shapeTwinkle))
            _shapeTwinkleIntensity = shapeTwinkle;
        if (Configuration.TryGet("sh_anim_speed", out float shapeAnimSpeed))
            _shapeAnimationSpeed = shapeAnimSpeed;
        if (Configuration.TryGet("sh_app_mode", out int shapeAppMode))
            _shapeAppearanceMode = (AppearanceMode)shapeAppMode;
        if (Configuration.TryGet("sh_spawn_maxCount", out int shapeMaxCount))
            _maxShapeCount = shapeMaxCount;
        if (Configuration.TryGet("sh_trig_mouseMoveEnabled", out bool shapeMove))
            _shapeMouseMoveEnabled = shapeMove;
        if (Configuration.TryGet("sh_trig_moveDistance", out float shapeMoveDist))
            _shapeMoveDistanceThreshold = shapeMoveDist;
        if (Configuration.TryGet("sh_trig_leftClickEnabled", out bool shapeLeft))
            _shapeLeftClickEnabled = shapeLeft;
        if (Configuration.TryGet("sh_trig_rightClickEnabled", out bool shapeRight))
            _shapeRightClickEnabled = shapeRight;
        if (Configuration.TryGet("sh_life_duration", out float shapeLife))
            _shapeLifetimeDuration = shapeLife;
        if (Configuration.TryGet("sh_perf_maxActive", out int shapeMaxActive))
            _maxActiveShapes = shapeMaxActive;
        if (Configuration.TryGet("sh_perf_maxSpawnsPerSecond", out int shapeMaxSpawns))
            _shapeMaxSpawnsPerSecond = shapeMaxSpawns;

        // Shapes morphing settings
        if (Configuration.TryGet("sh_morph_enabled", out bool shapeMorphEnabled))
            _shapeMorphEnabled = shapeMorphEnabled;
        if (Configuration.TryGet("sh_morph_speed", out float shapeMorphSpeed))
            _shapeMorphSpeed = shapeMorphSpeed;
        if (Configuration.TryGet("sh_morph_intensity", out float shapeMorphIntensity))
            _shapeMorphIntensity = shapeMorphIntensity;
        if (Configuration.TryGet("sh_morph_betweenShapes", out bool shapeMorphBetween))
            _shapeMorphBetweenShapes = shapeMorphBetween;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        var dt = gameTime.DeltaSeconds;
        var totalTime = gameTime.TotalSeconds;

        // Dispatch based on effect type
        if (_selectedEffectType == 1)
        {
            UpdateShapesEffect(dt, totalTime, mouseState);
            return;
        }

        // ===== Mandala Effect Update =====

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

    private void UpdateShapesEffect(float dt, float totalTime, MouseState mouseState)
    {
        // Update rainbow hue
        if (_shapeRainbowMode)
        {
            _shapeRainbowHue += _shapeRainbowSpeed * dt;
            if (_shapeRainbowHue > 1f) _shapeRainbowHue -= 1f;
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
        if (_shapeMouseMoveEnabled && distanceFromLast > 0.1f)
        {
            _accumulatedDistance += distanceFromLast;
            if (_accumulatedDistance >= _shapeMoveDistanceThreshold)
            {
                SpawnShape(currentPos, totalTime);
                _accumulatedDistance = 0f;
            }
        }

        // Click triggers
        var leftPressed = mouseState.ButtonsDown.HasFlag(CoreMouseButtons.Left);
        var rightPressed = mouseState.ButtonsDown.HasFlag(CoreMouseButtons.Right);

        if (_shapeLeftClickEnabled && leftPressed && !_wasLeftPressed)
        {
            SpawnShape(currentPos, totalTime);
        }
        if (_shapeRightClickEnabled && rightPressed && !_wasRightPressed)
        {
            SpawnShape(currentPos, totalTime);
        }

        _wasLeftPressed = leftPressed;
        _wasRightPressed = rightPressed;
        _lastMousePos = currentPos;

        // Update existing shapes
        UpdateShapes(dt, totalTime, currentPos);
    }

    private void UpdateShapes(float dt, float totalTime, Vector2 mousePos)
    {
        _activeShapeCount = 0;

        for (int i = 0; i < MaxMandalas; i++)
        {
            ref var s = ref _shapes[i];
            if (s.Lifetime <= 0) continue;

            s.Lifetime -= dt;
            if (s.Lifetime <= 0) continue;

            // Update rotation
            s.Rotation += s.RotationSpeed * s.RotationDirection * dt;

            // Follow cursor mode
            if (_maxShapeCount == 1)
            {
                s.Position = mousePos;
            }

            // Update animated radius with pulse
            if (_pulseRadiusEnabled)
            {
                float t = (totalTime - s.SpawnTime) * _pulseSpeed;
                float pulse = 1f + MathF.Sin(t * MathF.PI * 2f) * _pulseAmount;
                s.Radius = _shapeRadius * pulse;
            }

            // Update animation phase
            s.AnimPhase = totalTime - s.SpawnTime;

            // Update morph phase
            if (_shapeMorphEnabled)
            {
                s.MorphPhase += dt * _shapeMorphSpeed * s.MorphSpeedMult;

                // When morphing between shapes, swap shapes when phase reaches 1
                if (s.MorphPhase >= 1f)
                {
                    s.MorphPhase -= 1f;
                    if (_shapeMorphBetweenShapes && s.MorphTargetShape >= 0)
                    {
                        // Swap current and target shape
                        int temp = s.ShapeIndex;
                        s.ShapeIndex = s.MorphTargetShape;
                        s.MorphTargetShape = temp;
                    }
                }
            }

            // Copy to GPU buffer
            _gpuShapes[_activeShapeCount++] = s;
        }
    }

    /// <summary>
    /// Gets the next shape from the shuffled queue, reshuffling when exhausted.
    /// Ensures all 20 shapes are shown before any repeats.
    /// </summary>
    private int GetNextShapeFromQueue()
    {
        // Initialize or reshuffle queue if needed
        if (_shapeQueue.Count == 0 || _shapeQueueIndex >= _shapeQueue.Count)
        {
            ShuffleShapeQueue();
        }

        return _shapeQueue[_shapeQueueIndex++];
    }

    /// <summary>
    /// Shuffles the shape queue using Fisher-Yates algorithm.
    /// </summary>
    private void ShuffleShapeQueue()
    {
        _shapeQueue.Clear();
        for (int i = 0; i < TotalShapeTypes; i++)
        {
            _shapeQueue.Add(i);
        }

        // Fisher-Yates shuffle
        for (int i = _shapeQueue.Count - 1; i > 0; i--)
        {
            int j = Random.Shared.Next(i + 1);
            (_shapeQueue[i], _shapeQueue[j]) = (_shapeQueue[j], _shapeQueue[i]);
        }

        _shapeQueueIndex = 0;
    }

    private void SpawnShape(Vector2 position, float totalTime)
    {
        // Rate limiting (use shape-specific spawn rate)
        if (_spawnsThisSecond >= _shapeMaxSpawnsPerSecond)
            return;

        // Active count limiting
        if (_activeShapeCount >= _maxActiveShapes)
            return;

        // Max shape count limiting
        int currentCount = 0;
        for (int i = 0; i < MaxMandalas; i++)
        {
            if (_shapes[i].Lifetime > 0) currentCount++;
        }
        if (currentCount >= _maxShapeCount)
        {
            if (_maxShapeCount == 1)
            {
                for (int i = 0; i < MaxMandalas; i++)
                {
                    if (_shapes[i].Lifetime > 0)
                    {
                        _shapes[i].Position = position;
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
            if (_shapes[i].Lifetime <= 0)
            {
                slot = i;
                break;
            }
        }
        if (slot < 0) return;

        // Determine shape - use shuffled queue for both random and cycle modes
        int shapeIndex;
        if (_cycleShapesEnabled || _randomShapeEnabled)
            shapeIndex = GetNextShapeFromQueue();
        else
            shapeIndex = (int)_selectedShape;

        // Determine rotation
        float rotSpeed = _shapeRotationSpeed * MathF.PI / 180f;
        float rotDir = _shapeRotationDirection switch
        {
            0 => 1f,   // Clockwise
            1 => -1f,  // Counter-clockwise
            _ => Random.Shared.NextSingle() > 0.5f ? 1f : -1f
        };

        // Determine color
        Vector4 color;
        if (_shapeRainbowMode)
        {
            float hue = _shapeRainbowHue + Random.Shared.NextSingle() * 0.1f;
            color = HueToRgb(hue);
        }
        else
        {
            color = _shapePrimaryColor;
        }

        // Determine morph target (different shape for morphing)
        int morphTarget = -1;
        float morphSpeedMultiplier = 0.8f + Random.Shared.NextSingle() * 0.4f; // 0.8-1.2 variation
        if (_shapeMorphBetweenShapes)
        {
            // Pick a different shape as morph target
            do
            {
                morphTarget = Random.Shared.Next(0, TotalShapeTypes);
            } while (morphTarget == shapeIndex);
        }

        _shapes[slot] = new ShapeInstanceData
        {
            Position = position,
            Radius = _shapeRadius,
            Rotation = Random.Shared.NextSingle() * MathF.PI * 2f,
            RotationSpeed = rotSpeed,
            RotationDirection = rotDir,
            Lifetime = _shapeLifetimeDuration,
            MaxLifetime = _shapeLifetimeDuration,
            ShapeIndex = shapeIndex,
            AppearMode = (int)_shapeAppearanceMode,
            SpawnTime = totalTime,
            AnimPhase = 0,
            Color = color,
            MorphPhase = Random.Shared.NextSingle(), // Random starting phase for variety
            MorphTargetShape = morphTarget,
            MorphSpeedMult = morphSpeedMultiplier,
            Padding = 0
        };

        _spawnsThisSecond++;
        _lastSpawnTime = totalTime;
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

            // Update morph phase (continuously cycling for internal animations)
            if (_morphEnabled)
            {
                m.MorphPhase += dt * _morphSpeed * m.MorphSpeed;
                // Keep phase cycling for continuous animation
                if (m.MorphPhase > 1f)
                {
                    m.MorphPhase -= 1f;
                    // When morphing between patterns, swap patterns on cycle
                    if (_morphBetweenPatterns && m.MorphTargetPattern >= 0)
                    {
                        float temp = m.PatternIndex;
                        m.PatternIndex = m.MorphTargetPattern;
                        m.MorphTargetPattern = temp;
                    }
                }
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

        // Determine morph target (different pattern for morphing)
        float morphTarget = -1f;
        float morphSpeedMultiplier = 0.8f + Random.Shared.NextSingle() * 0.4f; // 0.8-1.2 variation
        if (_morphBetweenPatterns)
        {
            int targetPattern;
            do
            {
                targetPattern = Random.Shared.Next(0, 10);
            } while (targetPattern == (int)pattern);
            morphTarget = (float)targetPattern;
        }

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
            Color = color,
            MorphPhase = Random.Shared.NextSingle(),  // Random starting phase for variety
            MorphTargetPattern = morphTarget,
            MorphSpeed = morphSpeedMultiplier,
            Padding = 0
        };

        _spawnsThisSecond++;
        _lastSpawnTime = totalTime;
    }

    private void InitializeShapesResources(IRenderContext context)
    {
        if (_shapesInitialized) return;

        // Load and compile Shapes shaders
        var shaderCode = LoadShaderResource("Shaders.ShapesShader.hlsl");
        _shapesVertexShader = context.CompileShader(shaderCode, "VSMain", ShaderStage.Vertex);
        _shapesPixelShader = context.CompileShader(shaderCode, "PSMain", ShaderStage.Pixel);

        // Create Shapes constant buffer
        _shapesConstantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<ShapesConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create Shapes instance buffer (structured buffer)
        _shapesInstanceBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<ShapeInstanceData>() * MaxMandalas,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<ShapeInstanceData>()
        });

        _shapesInitialized = true;
    }

    protected override void OnRender(IRenderContext context)
    {
        // Dispatch based on effect type
        if (_selectedEffectType == 1)
        {
            RenderShapes(context);
            return;
        }

        // ===== Mandala Effect Render =====
        if (_activeMandalaCount == 0) return;

        var totalTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Update constant buffer
        var constants = new SacredGeometryConstants
        {
            ViewportSize = _viewportSize,
            Time = totalTime,
            GlowIntensity = _glowIntensity,
            HdrMultiplier = context.HdrPeakBrightness,
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
            ScaleOutDuration = _scaleOutDuration,
            MorphEnabled = _morphEnabled ? 1 : 0,
            MorphSpeed = _morphSpeed,
            MorphIntensity = _morphIntensity,
            MorphBetweenPatterns = _morphBetweenPatterns ? 1 : 0
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

    private void RenderShapes(IRenderContext context)
    {
        // Lazy initialize Shapes GPU resources
        InitializeShapesResources(context);

        if (_activeShapeCount == 0) return;

        var totalTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Update constant buffer
        var constants = new ShapesConstants
        {
            ViewportSize = _viewportSize,
            Time = totalTime,
            GlowIntensity = _shapeGlowIntensity,
            HdrMultiplier = context.HdrPeakBrightness,
            LineThickness = _shapeLineThickness,
            ActiveShapeCount = _activeShapeCount,
            TwinkleIntensity = _shapeTwinkleIntensity,
            PrimaryColor = _shapePrimaryColor,
            SecondaryColor = _shapeSecondaryColor,
            RainbowSpeed = _shapeRainbowSpeed,
            RainbowEnabled = _shapeRainbowMode ? 1 : 0,
            AnimationSpeed = _shapeAnimationSpeed,
            MorphEnabled = _shapeMorphEnabled ? 1 : 0,
            MorphSpeed = _shapeMorphSpeed,
            MorphIntensity = _shapeMorphIntensity,
            MorphBetweenShapes = _shapeMorphBetweenShapes ? 1 : 0,
            IndependentRainbow = _shapeIndependentRainbow ? 1 : 0
        };

        context.UpdateBuffer(_shapesConstantBuffer!, MemoryMarshal.CreateReadOnlySpan(ref constants, 1));

        // Update shape instance buffer
        context.UpdateBuffer(_shapesInstanceBuffer!, new ReadOnlySpan<ShapeInstanceData>(_gpuShapes, 0, _activeShapeCount));

        // Set render state
        context.SetBlendState(BlendMode.Additive);
        context.SetVertexShader(_shapesVertexShader!);
        context.SetPixelShader(_shapesPixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _shapesConstantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _shapesConstantBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _shapesInstanceBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _shapesInstanceBuffer!);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced quads (6 vertices per quad, one per shape)
        context.DrawInstanced(6, _activeShapeCount, 0, 0);
    }

    protected override void OnViewportSizeChanged(Vector2 newSize)
    {
        _viewportSize = newSize;
    }

    protected override void OnDispose()
    {
        // Mandala resources
        _mandalaBuffer?.Dispose();
        _constantBuffer?.Dispose();
        _pixelShader?.Dispose();
        _vertexShader?.Dispose();

        // Shapes resources
        _shapesInstanceBuffer?.Dispose();
        _shapesConstantBuffer?.Dispose();
        _shapesPixelShader?.Dispose();
        _shapesVertexShader?.Dispose();
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
