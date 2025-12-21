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

    // GPU Resources
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _constantBuffer;

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
    private PositionMode _positionMode = PositionMode.FollowCursor;
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

    [StructLayout(LayoutKind.Sequential, Size = 160)]
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
    }

    // Public properties for UI binding
    public PositionMode Position
    {
        get => _positionMode;
        set => _positionMode = value;
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
        // Create constant buffer
        var bufferDesc = new BufferDescription
        {
            Size = 160,
            Type = BufferType.Constant,
            Dynamic = true
        };
        _constantBuffer = context.CreateBuffer(bufferDesc);

        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("ProceduralSigilShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

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
        if (Configuration.TryGet("positionMode", out int positionMode))
            _positionMode = (PositionMode)positionMode;
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
                bool leftButtonDown = (mouseState.ButtonsDown & MouseButtons.Left) != 0;
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
                leftButtonDown = (mouseState.ButtonsDown & MouseButtons.Left) != 0;
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
            MiddleRotationMult = _counterRotateLayers ? -0.5f : 0.5f
        };

        context.UpdateBuffer(_constantBuffer, constants);

        // Set pipeline state - Additive for glowing effect
        context.SetBlendState(BlendMode.Additive);
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw fullscreen triangle
        context.Draw(3, 0);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
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
}
