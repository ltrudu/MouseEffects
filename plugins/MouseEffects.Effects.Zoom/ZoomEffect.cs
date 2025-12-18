using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Zoom;

/// <summary>
/// Zoom effect that magnifies the area around the mouse cursor.
/// Supports both circular and rectangular shapes with configurable zoom factor.
/// </summary>
public sealed class ZoomEffect : EffectBase
{
    private const float DefaultZoomFactor = 1.5f;
    private const float DefaultRadius = 100.0f;
    private const float DefaultWidth = 200.0f;
    private const float DefaultHeight = 150.0f;
    private const int DefaultShapeType = 0; // 0 = Circle, 1 = Rectangle
    private const bool DefaultSyncSizes = false;
    private const float DefaultBorderWidth = 2.0f;
    private const bool DefaultEnableZoomHotkey = false;
    private const bool DefaultEnableSizeHotkey = false;

    private const float ZoomStep = 0.1f;
    private const float MinZoom = 1.1f;
    private const float MaxZoom = 5.0f;
    private const float SizeChangePercent = 0.05f; // 5%

    // Virtual key codes
    private const int VK_SHIFT = 0x10;
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12; // Alt key

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "zoom-effect",
        Name = "Zoom",
        Description = "Magnifies the area around the mouse cursor with selectable circle or rectangle shape",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Screen
    };

    // GPU resources
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _paramsBuffer;
    private ISamplerState? _linearSampler;

    // Effect parameters
    private float _zoomFactor = DefaultZoomFactor;
    private float _radius = DefaultRadius;
    private float _width = DefaultWidth;
    private float _height = DefaultHeight;
    private int _shapeType = DefaultShapeType;
    private bool _syncSizes = DefaultSyncSizes;
    private float _borderWidth = DefaultBorderWidth;
    private Vector4 _borderColor = new(0.2f, 0.6f, 1.0f, 1.0f); // Default blue
    private Vector2 _mousePosition;

    // Hotkey settings
    private bool _enableZoomHotkey = DefaultEnableZoomHotkey;
    private bool _enableSizeHotkey = DefaultEnableSizeHotkey;

    /// <summary>
    /// Event raised when configuration is changed by hotkeys.
    /// The UI can subscribe to this to refresh its controls.
    /// </summary>
    public event Action? ConfigurationChangedByHotkey;

    public override EffectMetadata Metadata => _metadata;

    /// <summary>
    /// Zoom effect requires screen capture to magnify screen content.
    /// </summary>
    public override bool RequiresContinuousScreenCapture => true;

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("ZoomShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        var paramsDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<ZoomParams>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _paramsBuffer = context.CreateBuffer(paramsDesc);

        // Create linear sampler for texture sampling
        _linearSampler = context.CreateSamplerState(SamplerDescription.LinearClamp);
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("zoomFactor", out float zoom))
            _zoomFactor = zoom;

        if (Configuration.TryGet("radius", out float radius))
            _radius = radius;

        if (Configuration.TryGet("width", out float width))
            _width = width;

        if (Configuration.TryGet("height", out float height))
            _height = height;

        if (Configuration.TryGet("shapeType", out int shapeType))
            _shapeType = shapeType;

        if (Configuration.TryGet("syncSizes", out bool sync))
            _syncSizes = sync;

        if (Configuration.TryGet("borderWidth", out float borderWidth))
            _borderWidth = borderWidth;

        if (Configuration.TryGet("borderColor", out Vector4 borderColor))
            _borderColor = borderColor;

        if (Configuration.TryGet("enableZoomHotkey", out bool zoomHotkey))
            _enableZoomHotkey = zoomHotkey;

        if (Configuration.TryGet("enableSizeHotkey", out bool sizeHotkey))
            _enableSizeHotkey = sizeHotkey;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        _mousePosition = mouseState.Position;

        // Handle hotkeys
        if (mouseState.ScrollDelta != 0)
        {
            HandleHotkeys(mouseState.ScrollDelta);
        }
    }

    private void HandleHotkeys(int scrollDelta)
    {
        bool shiftDown = IsKeyDown(VK_SHIFT);
        bool ctrlDown = IsKeyDown(VK_CONTROL);
        bool altDown = IsKeyDown(VK_MENU);

        bool configChanged = false;

        // Zoom hotkey: Shift+Ctrl+MouseWheel
        if (_enableZoomHotkey && shiftDown && ctrlDown && !altDown)
        {
            float delta = scrollDelta > 0 ? ZoomStep : -ZoomStep;
            float newZoom = Math.Clamp(_zoomFactor + delta, MinZoom, MaxZoom);

            if (Math.Abs(newZoom - _zoomFactor) > 0.001f)
            {
                _zoomFactor = newZoom;
                Configuration.Set("zoomFactor", _zoomFactor);
                configChanged = true;
            }
        }
        // Size hotkey: Shift+Alt+MouseWheel
        else if (_enableSizeHotkey && shiftDown && altDown && !ctrlDown)
        {
            float multiplier = scrollDelta > 0 ? (1.0f + SizeChangePercent) : (1.0f - SizeChangePercent);

            if (_shapeType == 0) // Circle
            {
                float newRadius = Math.Clamp(_radius * multiplier, 20.0f, 500.0f);
                if (Math.Abs(newRadius - _radius) > 0.1f)
                {
                    _radius = newRadius;
                    Configuration.Set("radius", _radius);
                    configChanged = true;
                }
            }
            else // Rectangle
            {
                float newWidth = Math.Clamp(_width * multiplier, 40.0f, 800.0f);
                float newHeight = Math.Clamp(_height * multiplier, 40.0f, 800.0f);

                bool widthChanged = Math.Abs(newWidth - _width) > 0.1f;
                bool heightChanged = Math.Abs(newHeight - _height) > 0.1f;

                if (widthChanged || heightChanged)
                {
                    _width = newWidth;
                    _height = newHeight;
                    Configuration.Set("width", _width);
                    Configuration.Set("height", _height);
                    configChanged = true;
                }
            }
        }

        if (configChanged)
        {
            ConfigurationChangedByHotkey?.Invoke();
        }
    }

    private static bool IsKeyDown(int vKey)
    {
        return (GetAsyncKeyState(vKey) & 0x8000) != 0;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null) return;

        // Get the screen texture from context
        var screenTexture = context.ScreenTexture;
        if (screenTexture == null) return;

        // Calculate effective dimensions based on shape type and sync setting
        float effectiveWidth = _width;
        float effectiveHeight = _height;

        if (_shapeType == 1 && _syncSizes) // Rectangle with sync
        {
            effectiveHeight = effectiveWidth; // Square
        }

        // Update parameters
        var zoomParams = new ZoomParams
        {
            MousePosition = _mousePosition,
            ViewportSize = context.ViewportSize,
            ZoomFactor = _zoomFactor,
            Radius = _radius,
            Width = effectiveWidth,
            Height = effectiveHeight,
            ShapeType = _shapeType,
            BorderWidth = _borderWidth,
            BorderColor = _borderColor
        };

        context.UpdateBuffer(_paramsBuffer!, zoomParams);

        // Set shaders
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _paramsBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);
        context.SetSampler(ShaderStage.Pixel, 0, _linearSampler!);

        // Enable alpha blending
        context.SetBlendState(BlendMode.Alpha);

        // Draw fullscreen quad (vertices generated procedurally in shader)
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Unbind screen texture
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);

        // Restore blend state
        context.SetBlendState(BlendMode.Opaque);
    }

    protected override void OnViewportSizeChanged(Vector2 newSize)
    {
        // No texture recreation needed - we use the screen capture
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _paramsBuffer?.Dispose();
        _linearSampler?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(ZoomEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.Zoom.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #region Shader Structures

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct ZoomParams
    {
        // Must match HLSL cbuffer layout exactly!
        // Total size: 64 bytes (4 * 16), must be multiple of 16 for constant buffers

        public Vector2 MousePosition;      // 8 bytes, offset 0
        public Vector2 ViewportSize;       // 8 bytes, offset 8
        public float ZoomFactor;           // 4 bytes, offset 16
        public float Radius;               // 4 bytes, offset 20
        public float Width;                // 4 bytes, offset 24
        public float Height;               // 4 bytes, offset 28
        public int ShapeType;              // 4 bytes, offset 32
        public float BorderWidth;          // 4 bytes, offset 36
        private float _padding1;           // 4 bytes, offset 40
        private float _padding2;           // 4 bytes, offset 44
        public Vector4 BorderColor;        // 16 bytes, offset 48
    }

    #endregion
}
