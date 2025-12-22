using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.ProceduralSigil;

public sealed class ProceduralSigilEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "procedural-sigil",
        Name = "Procedural Sigil",
        Description = "Magical sigil with procedural geometry, runes, and glowing energy",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Artistic
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Resources - Sigil
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _constantBuffer;

    // GPU Resources - Fire Particles
    private const int MaxFireParticles = 1000;
    private IBuffer? _fireParticleBuffer;
    private IBuffer? _fireConstantBuffer;
    private IShader? _fireVertexShader;
    private IShader? _firePixelShader;

    // Fire Particle CPU State
    private readonly FireParticle[] _fireParticles = new FireParticle[MaxFireParticles];
    private readonly FireParticle[] _gpuFireParticles = new FireParticle[MaxFireParticles];
    private int _nextFireParticle;
    private int _activeFireParticleCount;
    private float _fireSpawnAccumulator;

    // Sigil style enum
    public enum SigilStyle
    {
        ArcaneCircle = 0,      // Original circular sigil with runes
        TriangleMandala = 1,   // Triangle-based mandala with fractal patterns
        Moon = 2               // Moon phases, zodiac, runes, Tree of Life
    }

    // Position mode enum
    public enum PositionMode
    {
        FollowCursor = 0,
        ScreenCenter = 1,
        ClickToSummon = 2,
        ClickAtCursor = 3
    }

    // Layer flags
    [Flags]
    public enum SigilLayers : uint
    {
        None = 0,
        Center = 1,
        Inner = 2,
        Middle = 4,
        Runes = 8,
        Glow = 16,
        All = Center | Inner | Middle | Runes | Glow
    }

    // Animation flags
    [Flags]
    public enum SigilAnimations : uint
    {
        None = 0,
        Rotate = 1,
        Pulse = 2,
        Morph = 4,
        All = Rotate | Pulse | Morph
    }

    // Color presets
    public enum ColorPreset
    {
        ShieldOfFire = 0,
        ArcaneBlue = 1,
        DarkMagic = 2,
        HolyLight = 3,
        Void = 4,
        Nature = 5,
        Custom = 6
    }

    // Effect Parameters
    private SigilStyle _sigilStyle = SigilStyle.ArcaneCircle;
    private PositionMode _positionMode = PositionMode.FollowCursor;
    private float _sigilAlpha = 0.7f;
    private float _sigilRadius = 200f;
    private float _lineThickness = 2.0f;
    private float _glowIntensity = 1.5f;
    private SigilLayers _layerFlags = SigilLayers.All;
    private SigilAnimations _animationFlags = SigilAnimations.All;
    private float _rotationSpeed = 0.5f;
    private float _pulseSpeed = 1.0f;
    private float _pulseAmplitude = 0.3f;
    private float _morphAmount = 1.0f;
    private bool _counterRotateLayers = true;
    private float _runeScrollSpeed = 0.3f;
    private float _fadeDuration = 2.0f;
    private ColorPreset _colorPreset = ColorPreset.ShieldOfFire;

    // Triangle Mandala specific parameters
    private int _triangleLayers = 3;
    private float _zoomSpeed = 0.5f;
    private float _zoomAmount = 0.3f;
    private int _innerTriangles = 4;
    private float _fractalDepth = 3.0f;

    // Moon style specific parameters
    private float _moonPhaseRotationSpeed = 0.1f;
    private float _zodiacRotationSpeed = -0.15f;
    private float _moonPhaseOffset = 0f;
    private float _treeOfLifeScale = 0.35f;
    private float _starfieldDensity = 0.5f;
    private float _cosmicGlowIntensity = 1.0f;

    // Energy particle parameters
    private float _particleIntensity = 0f;
    private float _particleSpeed = 1f;
    private uint _particleType = 0; // 0=None, 1=Fire, 2=Electricity, 3=Mixed
    private float _particleEntropy = 0.5f;
    private float _particleSize = 1f;
    private float _fireRiseHeight = 0.4f;
    private float _electricitySpread = 1f;
    private bool _windEnabled = true;
    private float _windStrength = 0.5f;
    private float _windTurbulence = 0.5f;

    // Fire particle spawn location
    public enum FireSpawnLocation
    {
        InnerRing = 0,   // 30-50% of radius
        RuneBand = 1     // 75-85% of radius (outer rune band)
    }

    // Fire particle render order
    public enum FireRenderOrder
    {
        BehindSigil = 0,
        OnTopOfSigil = 1
    }

    // Fire particle color palette
    public enum FireColorPalette
    {
        SigilColors = 0,
        VibrantFire = 1,
        Ethereal = 2,
        MysticalBlue = 3,
        MagicalPink = 4,
        PoisonGreen = 5,
        DeepCrimson = 6,
        Custom = 7
    }

    // Fire particle pool parameters
    private bool _fireParticleEnabled = false;
    private FireSpawnLocation _fireSpawnLocation = FireSpawnLocation.InnerRing;
    private FireRenderOrder _fireRenderOrder = FireRenderOrder.BehindSigil;
    private FireColorPalette _fireColorPalette = FireColorPalette.SigilColors;
    private Vector4 _fireCustomCoreColor = new(1.0f, 0.7f, 0.2f, 1.0f);
    private Vector4 _fireCustomMidColor = new(1.0f, 0.4f, 0.0f, 1.0f);
    private Vector4 _fireCustomEdgeColor = new(0.8f, 0.2f, 0.0f, 1.0f);
    private float _fireParticleAlpha = 1.0f;
    private int _fireParticleCount = 300;
    private float _fireSpawnRate = 30f;
    private float _fireParticleSize = 8f;
    private float _fireLifetime = 2.0f;
    private float _fireRiseSpeed = 60f;
    private float _fireTurbulence = 0.3f;
    private bool _fireWindEnabled = true;

    // Colors
    private Vector4 _coreColor = new(1.0f, 0.6f, 0.1f, 1.0f);  // Orange/gold
    private Vector4 _midColor = new(1.0f, 0.4f, 0.0f, 1.0f);   // Orange
    private Vector4 _edgeColor = new(0.8f, 0.2f, 0.0f, 1.0f);  // Red-orange

    // Animation state
    private float _elapsedTime;
    private Vector2 _mousePosition;
    private Vector2 _sigilPosition;
    private float _fadeAlpha = 1.0f;
    private float _spawnTime;
    private bool _sigilActive;

    // Click tracking for ClickAtCursor mode
    private bool _wasLeftButtonDown;

    [StructLayout(LayoutKind.Sequential, Size = 224)]
    private struct SigilConstants
    {
        public Vector2 ViewportSize;
        public Vector2 SigilCenter;

        public float Time;
        public float SigilRadius;
        public float LineThickness;
        public float GlowIntensity;

        public Vector4 CoreColor;
        public Vector4 MidColor;
        public Vector4 EdgeColor;

        public uint LayerFlags;
        public uint AnimationFlags;
        public float RotationSpeed;
        public float PulseSpeed;

        public float PulseAmplitude;
        public float MorphAmount;
        public float FadeAlpha;
        public float HdrMultiplier;

        public float CounterRotateLayers;
        public float RuneScrollSpeed;
        public float InnerRotationMult;
        public float MiddleRotationMult;

        // Style and Triangle Mandala parameters
        public uint SigilStyle;
        public int TriangleLayers;
        public float ZoomSpeed;
        public float ZoomAmount;

        public int InnerTriangles;
        public float FractalDepth;
        public float MoonPhaseRotationSpeed;
        public float ZodiacRotationSpeed;

        public float MoonPhaseOffset;
        public float TreeOfLifeScale;
        public float StarfieldDensity;
        public float CosmicGlowIntensity;

        // Energy particle parameters
        public float ParticleIntensity;
        public float ParticleSpeed;
        public uint ParticleType;
        public float ParticleEntropy;

        public float ParticleSize;
        public float FireRiseHeight;
        public float ElectricitySpread;
        public float SigilAlpha;

        public float WindStrength;
        public float WindTurbulence;
        public uint WindEnabled;
        public float _Padding1;
    }

    // Public properties for UI binding
    public SigilStyle Style
    {
        get => _sigilStyle;
        set => _sigilStyle = value;
    }

    public PositionMode Position
    {
        get => _positionMode;
        set => _positionMode = value;
    }

    public float SigilAlpha
    {
        get => _sigilAlpha;
        set => _sigilAlpha = Math.Clamp(value, 0f, 1f);
    }

    public float SigilRadius
    {
        get => _sigilRadius;
        set => _sigilRadius = Math.Clamp(value, 100f, 800f);
    }

    public float LineThickness
    {
        get => _lineThickness;
        set => _lineThickness = Math.Clamp(value, 1f, 5f);
    }

    public float GlowIntensity
    {
        get => _glowIntensity;
        set => _glowIntensity = Math.Clamp(value, 0.5f, 3.0f);
    }

    public SigilLayers Layers
    {
        get => _layerFlags;
        set => _layerFlags = value;
    }

    public SigilAnimations Animations
    {
        get => _animationFlags;
        set => _animationFlags = value;
    }

    public float RotationSpeed
    {
        get => _rotationSpeed;
        set => _rotationSpeed = Math.Clamp(value, 0f, 2f);
    }

    public float PulseSpeed
    {
        get => _pulseSpeed;
        set => _pulseSpeed = Math.Clamp(value, 0.5f, 3f);
    }

    public float PulseAmplitude
    {
        get => _pulseAmplitude;
        set => _pulseAmplitude = Math.Clamp(value, 0f, 1f);
    }

    public float MorphAmount
    {
        get => _morphAmount;
        set => _morphAmount = Math.Clamp(value, 0f, 1f);
    }

    public bool CounterRotateLayers
    {
        get => _counterRotateLayers;
        set => _counterRotateLayers = value;
    }

    public float RuneScrollSpeed
    {
        get => _runeScrollSpeed;
        set => _runeScrollSpeed = Math.Clamp(value, 0f, 1f);
    }

    public float FadeDuration
    {
        get => _fadeDuration;
        set => _fadeDuration = Math.Clamp(value, 0.5f, 5f);
    }

    public ColorPreset Preset
    {
        get => _colorPreset;
        set
        {
            _colorPreset = value;
            ApplyColorPreset(value);
        }
    }

    public Vector4 CoreColor
    {
        get => _coreColor;
        set => _coreColor = value;
    }

    public Vector4 MidColor
    {
        get => _midColor;
        set => _midColor = value;
    }

    public Vector4 EdgeColor
    {
        get => _edgeColor;
        set => _edgeColor = value;
    }

    // Triangle Mandala properties
    public int TriangleLayers
    {
        get => _triangleLayers;
        set => _triangleLayers = Math.Clamp(value, 1, 5);
    }

    public float ZoomSpeed
    {
        get => _zoomSpeed;
        set => _zoomSpeed = Math.Clamp(value, 0f, 2f);
    }

    public float ZoomAmount
    {
        get => _zoomAmount;
        set => _zoomAmount = Math.Clamp(value, 0f, 1f);
    }

    public int InnerTriangles
    {
        get => _innerTriangles;
        set => _innerTriangles = Math.Clamp(value, 2, 8);
    }

    public float FractalDepth
    {
        get => _fractalDepth;
        set => _fractalDepth = Math.Clamp(value, 1f, 5f);
    }

    // Moon style properties
    public float MoonPhaseRotationSpeed
    {
        get => _moonPhaseRotationSpeed;
        set => _moonPhaseRotationSpeed = Math.Clamp(value, -1f, 1f);
    }

    public float ZodiacRotationSpeed
    {
        get => _zodiacRotationSpeed;
        set => _zodiacRotationSpeed = Math.Clamp(value, -1f, 1f);
    }

    public float MoonPhaseOffset
    {
        get => _moonPhaseOffset;
        set => _moonPhaseOffset = value;
    }

    public float TreeOfLifeScale
    {
        get => _treeOfLifeScale;
        set => _treeOfLifeScale = Math.Clamp(value, 0.2f, 0.6f);
    }

    public float StarfieldDensity
    {
        get => _starfieldDensity;
        set => _starfieldDensity = Math.Clamp(value, 0f, 1f);
    }

    public float CosmicGlowIntensity
    {
        get => _cosmicGlowIntensity;
        set => _cosmicGlowIntensity = Math.Clamp(value, 0.5f, 2f);
    }

    // Energy particle properties
    public float ParticleIntensity
    {
        get => _particleIntensity;
        set => _particleIntensity = Math.Clamp(value, 0f, 1f);
    }

    public float ParticleSpeed
    {
        get => _particleSpeed;
        set => _particleSpeed = Math.Clamp(value, 0.1f, 3f);
    }

    public uint ParticleType
    {
        get => _particleType;
        set => _particleType = Math.Min(value, 3); // 0=None, 1=Fire, 2=Electricity, 3=Mixed
    }

    public float ParticleEntropy
    {
        get => _particleEntropy;
        set => _particleEntropy = Math.Clamp(value, 0f, 1f);
    }

    public float ParticleSize
    {
        get => _particleSize;
        set => _particleSize = Math.Clamp(value, 0.1f, 5f);
    }

    public float FireRiseHeight
    {
        get => _fireRiseHeight;
        set => _fireRiseHeight = Math.Clamp(value, 0.1f, 2f);
    }

    public float ElectricitySpread
    {
        get => _electricitySpread;
        set => _electricitySpread = Math.Clamp(value, 0.1f, 5f);
    }

    public bool WindEnabled
    {
        get => _windEnabled;
        set => _windEnabled = value;
    }

    public float WindStrength
    {
        get => _windStrength;
        set => _windStrength = Math.Clamp(value, 0f, 3f);
    }

    public float WindTurbulence
    {
        get => _windTurbulence;
        set => _windTurbulence = Math.Clamp(value, 0f, 2f);
    }

    private void ApplyColorPreset(ColorPreset preset)
    {
        switch (preset)
        {
            case ColorPreset.ShieldOfFire:
                _coreColor = new Vector4(1.0f, 0.7f, 0.2f, 1.0f);
                _midColor = new Vector4(1.0f, 0.4f, 0.0f, 1.0f);
                _edgeColor = new Vector4(0.8f, 0.2f, 0.0f, 1.0f);
                break;
            case ColorPreset.ArcaneBlue:
                _coreColor = new Vector4(0.4f, 0.8f, 1.0f, 1.0f);
                _midColor = new Vector4(0.2f, 0.5f, 1.0f, 1.0f);
                _edgeColor = new Vector4(0.1f, 0.2f, 0.8f, 1.0f);
                break;
            case ColorPreset.DarkMagic:
                _coreColor = new Vector4(0.8f, 0.3f, 1.0f, 1.0f);
                _midColor = new Vector4(0.5f, 0.1f, 0.8f, 1.0f);
                _edgeColor = new Vector4(0.2f, 0.0f, 0.4f, 1.0f);
                break;
            case ColorPreset.HolyLight:
                _coreColor = new Vector4(1.0f, 1.0f, 0.9f, 1.0f);
                _midColor = new Vector4(1.0f, 0.9f, 0.6f, 1.0f);
                _edgeColor = new Vector4(0.9f, 0.8f, 0.4f, 1.0f);
                break;
            case ColorPreset.Void:
                _coreColor = new Vector4(0.4f, 0.0f, 0.6f, 1.0f);
                _midColor = new Vector4(0.2f, 0.0f, 0.3f, 1.0f);
                _edgeColor = new Vector4(0.1f, 0.0f, 0.15f, 1.0f);
                break;
            case ColorPreset.Nature:
                _coreColor = new Vector4(0.4f, 1.0f, 0.5f, 1.0f);
                _midColor = new Vector4(0.2f, 0.8f, 0.3f, 1.0f);
                _edgeColor = new Vector4(0.1f, 0.5f, 0.2f, 1.0f);
                break;
            case ColorPreset.Custom:
                // Keep current custom colors
                break;
        }
    }

    protected override void OnInitialize(IRenderContext context)
    {
        // Create constant buffer for sigil
        var bufferDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<SigilConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _constantBuffer = context.CreateBuffer(bufferDesc);

        // Load and compile sigil shaders
        var shaderSource = LoadEmbeddedShader("ProceduralSigilShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create fire particle buffer (structured buffer)
        var fireParticleDesc = new BufferDescription
        {
            Size = MaxFireParticles * Marshal.SizeOf<FireParticle>(),
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<FireParticle>()
        };
        _fireParticleBuffer = context.CreateBuffer(fireParticleDesc);

        // Create fire constant buffer
        var fireConstantDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<FireConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _fireConstantBuffer = context.CreateBuffer(fireConstantDesc);

        // Load and compile fire particle shaders
        var fireShaderSource = LoadEmbeddedShader("FireParticleShader.hlsl");
        _fireVertexShader = context.CompileShader(fireShaderSource, "VSMain", ShaderStage.Vertex);
        _firePixelShader = context.CompileShader(fireShaderSource, "PSMain", ShaderStage.Pixel);

        // Initialize fire particles to dead state
        for (int i = 0; i < MaxFireParticles; i++)
        {
            _fireParticles[i] = new FireParticle { Lifetime = 0 };
        }

        // Load configuration
        LoadConfiguration();

        // Initialize sigil state
        _sigilActive = _positionMode != PositionMode.ClickToSummon && _positionMode != PositionMode.ClickAtCursor;
        _fadeAlpha = _sigilActive ? 1.0f : 0.0f;
    }

    protected override void OnConfigurationChanged()
    {
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        if (Configuration.TryGet("sigilStyle", out int sigilStyle))
            _sigilStyle = (SigilStyle)sigilStyle;
        if (Configuration.TryGet("positionMode", out int positionMode))
            _positionMode = (PositionMode)positionMode;
        if (Configuration.TryGet("sigilAlpha", out float sigilAlpha))
            _sigilAlpha = sigilAlpha;
        if (Configuration.TryGet("sigilRadius", out float sigilRadius))
            _sigilRadius = sigilRadius;
        if (Configuration.TryGet("lineThickness", out float lineThickness))
            _lineThickness = lineThickness;
        if (Configuration.TryGet("glowIntensity", out float glowIntensity))
            _glowIntensity = glowIntensity;
        if (Configuration.TryGet("layerFlags", out uint layerFlags))
            _layerFlags = (SigilLayers)layerFlags;
        if (Configuration.TryGet("animationFlags", out uint animationFlags))
            _animationFlags = (SigilAnimations)animationFlags;
        if (Configuration.TryGet("rotationSpeed", out float rotationSpeed))
            _rotationSpeed = rotationSpeed;
        if (Configuration.TryGet("pulseSpeed", out float pulseSpeed))
            _pulseSpeed = pulseSpeed;
        if (Configuration.TryGet("pulseAmplitude", out float pulseAmplitude))
            _pulseAmplitude = pulseAmplitude;
        if (Configuration.TryGet("morphAmount", out float morphAmount))
            _morphAmount = morphAmount;
        if (Configuration.TryGet("counterRotateLayers", out bool counterRotate))
            _counterRotateLayers = counterRotate;
        if (Configuration.TryGet("runeScrollSpeed", out float runeScrollSpeed))
            _runeScrollSpeed = runeScrollSpeed;
        if (Configuration.TryGet("fadeDuration", out float fadeDuration))
            _fadeDuration = fadeDuration;
        if (Configuration.TryGet("colorPreset", out int colorPreset))
        {
            _colorPreset = (ColorPreset)colorPreset;
            ApplyColorPreset(_colorPreset);
        }
        if (Configuration.TryGet("coreColor", out Vector4 coreColor))
            _coreColor = coreColor;
        if (Configuration.TryGet("midColor", out Vector4 midColor))
            _midColor = midColor;
        if (Configuration.TryGet("edgeColor", out Vector4 edgeColor))
            _edgeColor = edgeColor;

        // Triangle Mandala parameters
        if (Configuration.TryGet("triangleLayers", out int triangleLayers))
            _triangleLayers = triangleLayers;
        if (Configuration.TryGet("zoomSpeed", out float zoomSpeed))
            _zoomSpeed = zoomSpeed;
        if (Configuration.TryGet("zoomAmount", out float zoomAmount))
            _zoomAmount = zoomAmount;
        if (Configuration.TryGet("innerTriangles", out int innerTriangles))
            _innerTriangles = innerTriangles;
        if (Configuration.TryGet("fractalDepth", out float fractalDepth))
            _fractalDepth = fractalDepth;

        // Moon style parameters
        if (Configuration.TryGet("moonPhaseRotationSpeed", out float moonPhaseRotationSpeed))
            _moonPhaseRotationSpeed = moonPhaseRotationSpeed;
        if (Configuration.TryGet("zodiacRotationSpeed", out float zodiacRotationSpeed))
            _zodiacRotationSpeed = zodiacRotationSpeed;
        if (Configuration.TryGet("moonPhaseOffset", out float moonPhaseOffset))
            _moonPhaseOffset = moonPhaseOffset;
        if (Configuration.TryGet("treeOfLifeScale", out float treeOfLifeScale))
            _treeOfLifeScale = treeOfLifeScale;
        if (Configuration.TryGet("starfieldDensity", out float starfieldDensity))
            _starfieldDensity = starfieldDensity;
        if (Configuration.TryGet("cosmicGlowIntensity", out float cosmicGlowIntensity))
            _cosmicGlowIntensity = cosmicGlowIntensity;

        // Energy particle parameters
        if (Configuration.TryGet("particleIntensity", out float particleIntensity))
            _particleIntensity = particleIntensity;
        if (Configuration.TryGet("particleSpeed", out float particleSpeed))
            _particleSpeed = particleSpeed;
        if (Configuration.TryGet("particleType", out int particleType))
            _particleType = (uint)particleType;
        if (Configuration.TryGet("particleEntropy", out float particleEntropy))
            _particleEntropy = particleEntropy;
        if (Configuration.TryGet("particleSize", out float particleSize))
            _particleSize = particleSize;
        if (Configuration.TryGet("fireRiseHeight", out float fireRiseHeight))
            _fireRiseHeight = fireRiseHeight;
        if (Configuration.TryGet("electricitySpread", out float electricitySpread))
            _electricitySpread = electricitySpread;
        if (Configuration.TryGet("windEnabled", out bool windEnabled))
            _windEnabled = windEnabled;
        if (Configuration.TryGet("windStrength", out float windStrength))
            _windStrength = windStrength;
        if (Configuration.TryGet("windTurbulence", out float windTurbulence))
            _windTurbulence = windTurbulence;

        // Fire particle pool parameters
        if (Configuration.TryGet("fireParticleEnabled", out bool fireParticleEnabled))
            _fireParticleEnabled = fireParticleEnabled;
        if (Configuration.TryGet("fireSpawnLocation", out int fireSpawnLocation))
            _fireSpawnLocation = (FireSpawnLocation)fireSpawnLocation;
        if (Configuration.TryGet("fireRenderOrder", out int fireRenderOrder))
            _fireRenderOrder = (FireRenderOrder)fireRenderOrder;
        if (Configuration.TryGet("fireColorPalette", out int fireColorPalette))
            _fireColorPalette = (FireColorPalette)fireColorPalette;
        if (Configuration.TryGet("fireCustomCoreColor", out Vector4 fireCustomCoreColor))
            _fireCustomCoreColor = fireCustomCoreColor;
        if (Configuration.TryGet("fireCustomMidColor", out Vector4 fireCustomMidColor))
            _fireCustomMidColor = fireCustomMidColor;
        if (Configuration.TryGet("fireCustomEdgeColor", out Vector4 fireCustomEdgeColor))
            _fireCustomEdgeColor = fireCustomEdgeColor;
        if (Configuration.TryGet("fireParticleAlpha", out float fireParticleAlpha))
            _fireParticleAlpha = fireParticleAlpha;
        if (Configuration.TryGet("fireParticleCount", out int fireParticleCount))
            _fireParticleCount = Math.Clamp(fireParticleCount, 100, MaxFireParticles);
        if (Configuration.TryGet("fireSpawnRate", out float fireSpawnRate))
            _fireSpawnRate = fireSpawnRate;
        if (Configuration.TryGet("fireParticleSize", out float fireParticleSize))
            _fireParticleSize = fireParticleSize;
        if (Configuration.TryGet("fireLifetime", out float fireLifetime))
            _fireLifetime = fireLifetime;
        if (Configuration.TryGet("fireRiseSpeed", out float fireRiseSpeed))
            _fireRiseSpeed = fireRiseSpeed;
        if (Configuration.TryGet("fireTurbulence", out float fireTurbulence))
            _fireTurbulence = fireTurbulence;
        if (Configuration.TryGet("fireWindEnabled", out bool fireWindEnabled))
            _fireWindEnabled = fireWindEnabled;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        _elapsedTime += gameTime.DeltaSeconds;
        _mousePosition = mouseState.Position;

        // Handle position modes
        switch (_positionMode)
        {
            case PositionMode.FollowCursor:
                _sigilPosition = _mousePosition;
                _sigilActive = true;
                _fadeAlpha = 1.0f;
                break;

            case PositionMode.ScreenCenter:
                // Will be set in render based on viewport
                _sigilActive = true;
                _fadeAlpha = 1.0f;
                break;

            case PositionMode.ClickToSummon:
                // Spawn at screen center on click
                bool leftButtonDown = (mouseState.ButtonsDown & MouseEffects.Core.Input.MouseButtons.Left) != 0;
                if (leftButtonDown && !_wasLeftButtonDown)
                {
                    _sigilActive = true;
                    _spawnTime = _elapsedTime;
                    _fadeAlpha = 1.0f;
                }
                _wasLeftButtonDown = leftButtonDown;

                // Fade out after duration
                if (_sigilActive)
                {
                    float timeSinceSpawn = _elapsedTime - _spawnTime;
                    if (timeSinceSpawn > _fadeDuration)
                    {
                        _fadeAlpha = Math.Max(0, 1.0f - (timeSinceSpawn - _fadeDuration) / 1.0f);
                        if (_fadeAlpha <= 0)
                        {
                            _sigilActive = false;
                        }
                    }
                }
                break;

            case PositionMode.ClickAtCursor:
                // Spawn at cursor position on click
                leftButtonDown = (mouseState.ButtonsDown & MouseEffects.Core.Input.MouseButtons.Left) != 0;
                if (leftButtonDown && !_wasLeftButtonDown)
                {
                    _sigilActive = true;
                    _sigilPosition = _mousePosition;
                    _spawnTime = _elapsedTime;
                    _fadeAlpha = 1.0f;
                }
                _wasLeftButtonDown = leftButtonDown;

                // Fade out after duration
                if (_sigilActive)
                {
                    float timeSinceSpawn = _elapsedTime - _spawnTime;
                    if (timeSinceSpawn > _fadeDuration)
                    {
                        _fadeAlpha = Math.Max(0, 1.0f - (timeSinceSpawn - _fadeDuration) / 1.0f);
                        if (_fadeAlpha <= 0)
                        {
                            _sigilActive = false;
                        }
                    }
                }
                break;
        }

        // Update fire particles
        if (_fireParticleEnabled && _sigilActive)
        {
            float dt = gameTime.DeltaSeconds;

            // Spawn new particles based on spawn rate
            _fireSpawnAccumulator += _fireSpawnRate * dt;
            while (_fireSpawnAccumulator >= 1.0f)
            {
                SpawnFireParticle(_elapsedTime);
                _fireSpawnAccumulator -= 1.0f;
            }

            // Update existing particles
            UpdateFireParticles(dt, _elapsedTime);
        }
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null || _constantBuffer == null)
            return;

        if (!_sigilActive && _fadeAlpha <= 0)
            return;

        // Determine sigil center based on position mode
        Vector2 sigilCenter = _positionMode switch
        {
            PositionMode.ScreenCenter => context.ViewportSize / 2f,
            PositionMode.ClickToSummon => context.ViewportSize / 2f,
            _ => _sigilPosition
        };

        // Update constant buffer
        var constants = new SigilConstants
        {
            ViewportSize = context.ViewportSize,
            SigilCenter = sigilCenter,
            Time = _elapsedTime,
            SigilRadius = _sigilRadius,
            LineThickness = _lineThickness,
            GlowIntensity = _glowIntensity,
            CoreColor = _coreColor,
            MidColor = _midColor,
            EdgeColor = _edgeColor,
            LayerFlags = (uint)_layerFlags,
            AnimationFlags = (uint)_animationFlags,
            RotationSpeed = _rotationSpeed,
            PulseSpeed = _pulseSpeed,
            PulseAmplitude = _pulseAmplitude,
            MorphAmount = _morphAmount,
            FadeAlpha = _fadeAlpha,
            HdrMultiplier = context.HdrPeakBrightness,
            CounterRotateLayers = _counterRotateLayers ? 1.0f : 0.0f,
            RuneScrollSpeed = _runeScrollSpeed,
            InnerRotationMult = 0.7f,
            MiddleRotationMult = _counterRotateLayers ? -0.5f : 0.5f,
            // Style and Triangle Mandala parameters
            SigilStyle = (uint)_sigilStyle,
            TriangleLayers = _triangleLayers,
            ZoomSpeed = _zoomSpeed,
            ZoomAmount = _zoomAmount,
            InnerTriangles = _innerTriangles,
            FractalDepth = _fractalDepth,
            MoonPhaseRotationSpeed = _moonPhaseRotationSpeed,
            ZodiacRotationSpeed = _zodiacRotationSpeed,
            MoonPhaseOffset = _moonPhaseOffset,
            TreeOfLifeScale = _treeOfLifeScale,
            StarfieldDensity = _starfieldDensity,
            CosmicGlowIntensity = _cosmicGlowIntensity,
            // Energy particle parameters
            ParticleIntensity = _particleIntensity,
            ParticleSpeed = _particleSpeed,
            ParticleType = _particleType,
            ParticleEntropy = _particleEntropy,
            ParticleSize = _particleSize,
            FireRiseHeight = _fireRiseHeight,
            ElectricitySpread = _electricitySpread,
            SigilAlpha = _sigilAlpha,
            WindStrength = _windStrength,
            WindTurbulence = _windTurbulence,
            WindEnabled = _windEnabled ? 1u : 0u
        };

        context.UpdateBuffer(_constantBuffer, constants);

        // Render fire particles BEFORE sigil if configured (so sigil draws on top)
        if (_fireParticleEnabled && _fireRenderOrder == FireRenderOrder.BehindSigil)
        {
            RenderFireParticles(context);
        }

        // Set pipeline state - Additive for glowing effect
        context.SetBlendState(BlendMode.Additive);
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw fullscreen triangle (sigil)
        context.Draw(3, 0);

        // Render fire particles AFTER sigil if configured (so particles draw on top)
        if (_fireParticleEnabled && _fireRenderOrder == FireRenderOrder.OnTopOfSigil)
        {
            RenderFireParticles(context);
        }
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();

        // Dispose fire particle resources
        _fireVertexShader?.Dispose();
        _firePixelShader?.Dispose();
        _fireParticleBuffer?.Dispose();
        _fireConstantBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(ProceduralSigilEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.ProceduralSigil.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #region Fire Particle System

    // Fire particle public properties
    public bool FireParticleEnabled
    {
        get => _fireParticleEnabled;
        set => _fireParticleEnabled = value;
    }

    public FireSpawnLocation FireSpawnLoc
    {
        get => _fireSpawnLocation;
        set => _fireSpawnLocation = value;
    }

    public FireRenderOrder FireRenderOrd
    {
        get => _fireRenderOrder;
        set => _fireRenderOrder = value;
    }

    public FireColorPalette FirePalette
    {
        get => _fireColorPalette;
        set => _fireColorPalette = value;
    }

    public Vector4 FireCustomCoreColor
    {
        get => _fireCustomCoreColor;
        set => _fireCustomCoreColor = value;
    }

    public Vector4 FireCustomMidColor
    {
        get => _fireCustomMidColor;
        set => _fireCustomMidColor = value;
    }

    public Vector4 FireCustomEdgeColor
    {
        get => _fireCustomEdgeColor;
        set => _fireCustomEdgeColor = value;
    }

    public float FireParticleAlpha
    {
        get => _fireParticleAlpha;
        set => _fireParticleAlpha = Math.Clamp(value, 0f, 1f);
    }

    public int FireParticleCount
    {
        get => _fireParticleCount;
        set => _fireParticleCount = Math.Clamp(value, 100, MaxFireParticles);
    }

    public float FireSpawnRate
    {
        get => _fireSpawnRate;
        set => _fireSpawnRate = Math.Clamp(value, 10f, 100f);
    }

    public float FireParticleSize
    {
        get => _fireParticleSize;
        set => _fireParticleSize = Math.Clamp(value, 2f, 20f);
    }

    public float FireLifetime
    {
        get => _fireLifetime;
        set => _fireLifetime = Math.Clamp(value, 0.5f, 4f);
    }

    public float FireRiseSpeed
    {
        get => _fireRiseSpeed;
        set => _fireRiseSpeed = Math.Clamp(value, 30f, 150f);
    }

    public float FireTurbulence
    {
        get => _fireTurbulence;
        set => _fireTurbulence = Math.Clamp(value, 0f, 1f);
    }

    public bool FireWindEnabled
    {
        get => _fireWindEnabled;
        set => _fireWindEnabled = value;
    }

    private void SpawnFireParticle(float time)
    {
        ref var p = ref _fireParticles[_nextFireParticle];
        _nextFireParticle = (_nextFireParticle + 1) % _fireParticleCount;

        // Spawn at configured location
        float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
        float spawnRadius = _fireSpawnLocation switch
        {
            FireSpawnLocation.RuneBand => _sigilRadius * (0.75f + Random.Shared.NextSingle() * 0.1f),  // 75-85% of radius
            _ => _sigilRadius * (0.3f + Random.Shared.NextSingle() * 0.2f)  // Inner ring: 30-50% of radius
        };

        p.Position = _sigilPosition + new Vector2(
            MathF.Cos(angle) * spawnRadius,
            MathF.Sin(angle) * spawnRadius
        );

        // Initial upward velocity with slight horizontal variance
        float riseSpeed = _fireRiseSpeed * (0.8f + Random.Shared.NextSingle() * 0.4f);
        p.Velocity = new Vector2(
            (Random.Shared.NextSingle() - 0.5f) * 20f,
            -riseSpeed // Negative Y = up
        );

        // Lifetime and properties
        p.Lifetime = _fireLifetime * (0.7f + Random.Shared.NextSingle() * 0.6f);
        p.MaxLifetime = p.Lifetime;
        p.Size = _fireParticleSize * (0.6f + Random.Shared.NextSingle() * 0.8f);
        p.Heat = 1.0f;
        p.FlickerPhase = Random.Shared.NextSingle() * MathF.PI * 2f;
        p.SpawnAngle = angle;

        // Initial color (hot = core color)
        p.Color = GetFireColor(p.Heat);
    }

    private void UpdateFireParticles(float dt, float time)
    {
        for (int i = 0; i < _fireParticleCount; i++)
        {
            ref var p = ref _fireParticles[i];
            if (p.Lifetime <= 0) continue;

            // Age the particle
            p.Lifetime -= dt;

            // Cool down (heat decreases quadratically)
            float lifeRatio = p.Lifetime / p.MaxLifetime;
            p.Heat = lifeRatio * lifeRatio;

            // Update color based on heat
            p.Color = GetFireColor(p.Heat);

            // Apply wind if enabled
            if (_fireWindEnabled && _windEnabled)
            {
                float wind = GetWind(time);
                p.Velocity.X += wind * _windStrength * dt * 30f;
            }

            // Add turbulence
            p.Velocity.X += MathF.Sin(time * 5f + p.FlickerPhase) * _fireTurbulence * dt * 50f;

            // Slight vertical drag
            p.Velocity.Y *= 0.998f;

            // Update position
            p.Position += p.Velocity * dt;
        }
    }

    private (Vector4 core, Vector4 mid, Vector4 edge) GetFirePaletteColors()
    {
        return _fireColorPalette switch
        {
            // Vibrant Fire - exact current orange palette
            FireColorPalette.VibrantFire => (
                new Vector4(1.0f, 0.7f, 0.2f, 1.0f),   // Bright yellow-orange core
                new Vector4(1.0f, 0.4f, 0.0f, 1.0f),   // Orange mid
                new Vector4(0.8f, 0.2f, 0.0f, 1.0f)    // Red-orange edge
            ),
            // Ethereal - white/gray ghostly
            FireColorPalette.Ethereal => (
                new Vector4(1.0f, 1.0f, 1.0f, 1.0f),   // Pure white core
                new Vector4(0.75f, 0.82f, 0.9f, 1.0f), // Pale blue-gray mid
                new Vector4(0.4f, 0.45f, 0.55f, 1.0f)  // Cool gray edge
            ),
            // Mystical Blue - arcane energy
            FireColorPalette.MysticalBlue => (
                new Vector4(0.6f, 0.95f, 1.0f, 1.0f),  // Bright cyan-white core
                new Vector4(0.2f, 0.5f, 1.0f, 1.0f),   // Vibrant blue mid
                new Vector4(0.1f, 0.2f, 0.7f, 1.0f)    // Deep blue edge
            ),
            // Magical Pink - enchanted magenta
            FireColorPalette.MagicalPink => (
                new Vector4(1.0f, 0.75f, 0.95f, 1.0f), // Bright pink-white core
                new Vector4(1.0f, 0.3f, 0.7f, 1.0f),   // Vibrant magenta mid
                new Vector4(0.7f, 0.1f, 0.5f, 1.0f)    // Deep rose edge
            ),
            // Poison Green - toxic/nature
            FireColorPalette.PoisonGreen => (
                new Vector4(0.7f, 1.0f, 0.4f, 1.0f),   // Bright yellow-green core
                new Vector4(0.2f, 0.9f, 0.2f, 1.0f),   // Vibrant green mid
                new Vector4(0.0f, 0.5f, 0.1f, 1.0f)    // Deep forest edge
            ),
            // Deep Crimson - blood/dark fire
            FireColorPalette.DeepCrimson => (
                new Vector4(1.0f, 0.7f, 0.3f, 1.0f),   // Hot orange-yellow core
                new Vector4(1.0f, 0.15f, 0.1f, 1.0f),  // Vibrant red mid
                new Vector4(0.5f, 0.0f, 0.05f, 1.0f)   // Deep crimson edge
            ),
            // Custom - user-defined colors
            FireColorPalette.Custom => (_fireCustomCoreColor, _fireCustomMidColor, _fireCustomEdgeColor),
            // Sigil Colors - use current sigil preset
            _ => (_coreColor, _midColor, _edgeColor)
        };
    }

    private Vector4 GetFireColor(float heat)
    {
        var (coreColor, midColor, edgeColor) = GetFirePaletteColors();

        Vector4 color;
        if (heat > 0.5f)
        {
            float t = (heat - 0.5f) * 2f;
            color = Vector4.Lerp(midColor, coreColor, t);
        }
        else
        {
            float t = heat * 2f;
            var coldColor = new Vector4(edgeColor.X, edgeColor.Y, edgeColor.Z, 0f); // Fade out alpha
            color = Vector4.Lerp(coldColor, midColor, t);
        }
        // Apply particle alpha
        color.W *= _fireParticleAlpha;
        return color;
    }

    private float GetWind(float time)
    {
        // Simple wind simulation - oscillates direction
        float wind = MathF.Sin(time * 0.5f) * 0.5f + 0.5f;
        wind += MathF.Sin(time * 1.7f) * 0.3f;
        wind += MathF.Sin(time * 2.3f) * _windTurbulence * 0.2f;
        return (wind - 0.5f) * 2f; // -1 to 1
    }

    private void RenderFireParticles(IRenderContext context)
    {
        if (_fireVertexShader == null || _firePixelShader == null || _fireParticleBuffer == null)
            return;

        // Copy alive particles to GPU array
        int gpuIndex = 0;
        for (int i = 0; i < _fireParticleCount && gpuIndex < MaxFireParticles; i++)
        {
            if (_fireParticles[i].Lifetime > 0)
            {
                _gpuFireParticles[gpuIndex++] = _fireParticles[i];
            }
        }
        _activeFireParticleCount = gpuIndex;

        if (_activeFireParticleCount == 0)
            return;

        // Fill remaining with zeroed particles
        for (int i = gpuIndex; i < MaxFireParticles; i++)
        {
            _gpuFireParticles[i] = default;
        }

        // Update GPU buffer
        context.UpdateBuffer(_fireParticleBuffer, (ReadOnlySpan<FireParticle>)_gpuFireParticles);

        // Update fire constant buffer
        var fireConstants = new FireConstants
        {
            ViewportSize = context.ViewportSize,
            Time = _elapsedTime,
            HdrMultiplier = context.HdrPeakBrightness,
            FadeAlpha = _fadeAlpha
        };
        context.UpdateBuffer(_fireConstantBuffer!, fireConstants);

        // Set shaders
        context.SetVertexShader(_fireVertexShader);
        context.SetPixelShader(_firePixelShader);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _fireConstantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _fireConstantBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _fireParticleBuffer);

        // Additive blending for fire glow
        context.SetBlendState(BlendMode.Additive);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced (6 vertices per quad)
        context.DrawInstanced(6, MaxFireParticles, 0, 0);
    }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct FireParticle
    {
        public Vector2 Position;      // 8 bytes
        public Vector2 Velocity;      // 8 bytes
        public Vector4 Color;         // 16 bytes = 32
        public float Size;            // 4 bytes
        public float Lifetime;        // 4 bytes
        public float MaxLifetime;     // 4 bytes
        public float Heat;            // 4 bytes = 48
        public float FlickerPhase;    // 4 bytes
        public float SpawnAngle;      // 4 bytes
        public float Padding1;        // 4 bytes
        public float Padding2;        // 4 bytes = 64
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    private struct FireConstants
    {
        public Vector2 ViewportSize;  // 8 bytes
        public float Time;            // 4 bytes
        public float HdrMultiplier;   // 4 bytes = 16
        public float FadeAlpha;       // 4 bytes
        public float Padding1;        // 4 bytes
        public float Padding2;        // 4 bytes
        public float Padding3;        // 4 bytes = 32
    }

    #endregion
}
