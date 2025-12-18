using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Spirograph;

public sealed class SpirographEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "spirograph",
        Name = "Spirograph",
        Description = "Beautiful spirograph-like geometric patterns following the mouse cursor with intricate mathematical curves",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Artistic
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 128)]
    private struct SpirographConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public Vector2 MousePosition;     // 8 bytes = 16
        public float Time;                // 4 bytes
        public float InnerRadius;         // 4 bytes
        public float OuterRadius;         // 4 bytes
        public float PenOffset;           // 4 bytes = 32
        public float RotationSpeed;       // 4 bytes
        public float LineThickness;       // 4 bytes
        public float GlowIntensity;       // 4 bytes
        public int NumPetals;             // 4 bytes = 48
        public float TrailFadeSpeed;      // 4 bytes
        public float ColorCycleSpeed;     // 4 bytes
        public int ColorMode;             // 4 bytes (0=rainbow, 1=fixed, 2=gradient)
        public float HdrMultiplier;       // 4 bytes = 64
        public Vector4 PrimaryColor;      // 16 bytes = 80
        public Vector4 SecondaryColor;    // 16 bytes = 96
        public Vector4 TertiaryColor;     // 16 bytes = 112
        public Vector4 Padding;           // 16 bytes = 128
    }

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Mouse tracking
    private Vector2 _currentMousePosition;

    // Configuration fields (sp_ prefix for Spirograph)
    private float _innerRadius = 50f;
    private float _outerRadius = 120f;
    private float _penOffset = 80f;
    private float _rotationSpeed = 1.0f;
    private float _lineThickness = 2.0f;
    private float _glowIntensity = 1.5f;
    private int _numPetals = 12;
    private float _trailFadeSpeed = 0.5f;
    private float _colorCycleSpeed = 1.0f;
    private int _colorMode = 0; // 0=rainbow, 1=fixed, 2=gradient
    private Vector4 _primaryColor = new(1f, 0f, 0.5f, 1f);
    private Vector4 _secondaryColor = new(0f, 0.5f, 1f, 1f);
    private Vector4 _tertiaryColor = new(0.5f, 1f, 0f, 1f);

    // Trigger settings
    private bool _showOnLeftClick = false;
    private bool _showOnRightClick = false;
    private int _mouseMoveMode = 1; // 0=None, 1=Follow, 2=Follow/Rotate
    private bool _invertRotation = false;

    // Runtime state
    private Vector2 _lastMousePosition;
    private float _mouseRotationOffset = 0f;
    private bool _isVisible = true;

    // Public properties for UI binding
    public float InnerRadius { get => _innerRadius; set => _innerRadius = value; }
    public float OuterRadius { get => _outerRadius; set => _outerRadius = value; }
    public float PenOffset { get => _penOffset; set => _penOffset = value; }
    public float RotationSpeed { get => _rotationSpeed; set => _rotationSpeed = value; }
    public float LineThickness { get => _lineThickness; set => _lineThickness = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public int NumPetals { get => _numPetals; set => _numPetals = value; }
    public float TrailFadeSpeed { get => _trailFadeSpeed; set => _trailFadeSpeed = value; }
    public float ColorCycleSpeed { get => _colorCycleSpeed; set => _colorCycleSpeed = value; }
    public int ColorMode { get => _colorMode; set => _colorMode = value; }
    public Vector4 PrimaryColor { get => _primaryColor; set => _primaryColor = value; }
    public Vector4 SecondaryColor { get => _secondaryColor; set => _secondaryColor = value; }
    public Vector4 TertiaryColor { get => _tertiaryColor; set => _tertiaryColor = value; }
    public bool ShowOnLeftClick { get => _showOnLeftClick; set => _showOnLeftClick = value; }
    public bool ShowOnRightClick { get => _showOnRightClick; set => _showOnRightClick = value; }
    public int MouseMoveMode { get => _mouseMoveMode; set => _mouseMoveMode = value; }
    public bool InvertRotation { get => _invertRotation; set => _invertRotation = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("SpirographShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<SpirographConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("sp_innerRadius", out float innerRadius))
            _innerRadius = innerRadius;
        if (Configuration.TryGet("sp_outerRadius", out float outerRadius))
            _outerRadius = outerRadius;
        if (Configuration.TryGet("sp_penOffset", out float penOffset))
            _penOffset = penOffset;
        if (Configuration.TryGet("sp_rotationSpeed", out float rotSpeed))
            _rotationSpeed = rotSpeed;
        if (Configuration.TryGet("sp_lineThickness", out float lineThick))
            _lineThickness = lineThick;
        if (Configuration.TryGet("sp_glowIntensity", out float glowInt))
            _glowIntensity = glowInt;
        if (Configuration.TryGet("sp_numPetals", out int numPetals))
            _numPetals = numPetals;
        if (Configuration.TryGet("sp_trailFadeSpeed", out float trailFade))
            _trailFadeSpeed = trailFade;
        if (Configuration.TryGet("sp_colorCycleSpeed", out float colorCycle))
            _colorCycleSpeed = colorCycle;
        if (Configuration.TryGet("sp_colorMode", out int colorMode))
            _colorMode = colorMode;
        if (Configuration.TryGet("sp_primaryColor", out Vector4 primCol))
            _primaryColor = primCol;
        if (Configuration.TryGet("sp_secondaryColor", out Vector4 secCol))
            _secondaryColor = secCol;
        if (Configuration.TryGet("sp_tertiaryColor", out Vector4 tertCol))
            _tertiaryColor = tertCol;

        // Trigger settings
        if (Configuration.TryGet("sp_showOnLeftClick", out bool showLeft))
            _showOnLeftClick = showLeft;
        if (Configuration.TryGet("sp_showOnRightClick", out bool showRight))
            _showOnRightClick = showRight;
        if (Configuration.TryGet("sp_mouseMoveMode", out int moveMode))
            _mouseMoveMode = moveMode;
        if (Configuration.TryGet("sp_invertRotation", out bool invertRot))
            _invertRotation = invertRot;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        // Determine visibility based on mouse button triggers
        if (_showOnLeftClick || _showOnRightClick)
        {
            // If triggers are enabled, only show when button is held
            _isVisible = (_showOnLeftClick && mouseState.IsButtonDown(MouseButtons.Left)) ||
                         (_showOnRightClick && mouseState.IsButtonDown(MouseButtons.Right));
        }
        else
        {
            // No triggers enabled, always visible
            _isVisible = true;
        }

        // Handle mouse move mode
        if (_mouseMoveMode == 0) // None
        {
            // Don't update position - keep static
        }
        else if (_mouseMoveMode == 1 || _mouseMoveMode == 2) // Follow or Follow/Rotate
        {
            // Update position to follow mouse
            Vector2 delta = mouseState.Position - _lastMousePosition;

            // In Follow/Rotate mode, adjust rotation based on mouse movement
            if (_mouseMoveMode == 2 && _lastMousePosition != Vector2.Zero)
            {
                // Up or right = clockwise, down or left = counter-clockwise
                float rotationDelta = (delta.X + -delta.Y) * 0.01f;

                if (_invertRotation)
                    rotationDelta = -rotationDelta;

                _mouseRotationOffset += rotationDelta;
            }

            _currentMousePosition = mouseState.Position;
            _lastMousePosition = mouseState.Position;
        }
    }

    protected override void OnRender(IRenderContext context)
    {
        // Don't render if not visible
        if (!_isVisible)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Calculate effective rotation speed (automatic in Follow mode, mouse-driven in Follow/Rotate)
        float effectiveRotationOffset = _mouseMoveMode == 2 ? _mouseRotationOffset : 0f;

        // Update constant buffer
        var constants = new SpirographConstants
        {
            ViewportSize = context.ViewportSize,
            MousePosition = _currentMousePosition,
            Time = currentTime,
            InnerRadius = _innerRadius,
            OuterRadius = _outerRadius,
            PenOffset = _penOffset,
            RotationSpeed = _mouseMoveMode == 2 ? 0f : _rotationSpeed, // No auto rotation in Follow/Rotate mode
            LineThickness = _lineThickness,
            GlowIntensity = _glowIntensity,
            NumPetals = _numPetals,
            TrailFadeSpeed = _trailFadeSpeed,
            ColorCycleSpeed = _colorCycleSpeed,
            ColorMode = _colorMode,
            HdrMultiplier = context.HdrPeakBrightness,
            PrimaryColor = _primaryColor,
            SecondaryColor = _secondaryColor,
            TertiaryColor = _tertiaryColor,
            Padding = new Vector4(effectiveRotationOffset, 0, 0, 0)
        };
        context.UpdateBuffer(_constantBuffer!, constants);

        // Set up rendering state
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetBlendState(BlendMode.Additive);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw fullscreen triangle
        context.Draw(3, 0);

        // Restore blend state
        context.SetBlendState(BlendMode.Alpha);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.Spirograph.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
