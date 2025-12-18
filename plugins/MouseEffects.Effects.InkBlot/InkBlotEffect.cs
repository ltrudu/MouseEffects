using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

using CoreMouseButtons = MouseEffects.Core.Input.MouseButtons;

namespace MouseEffects.Effects.InkBlot;

public sealed class InkBlotEffect : EffectBase
{
    private const int HardMaxBlots = 100;

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "inkblot",
        Name = "Ink Blot",
        Description = "Spreading ink and watercolor drops that bloom from clicks or cursor movement",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 80)]
    private struct InkBlotConstants
    {
        public Vector2 ViewportSize;       // 8 bytes
        public float Time;                 // 4 bytes
        public float EdgeIrregularity;     // 4 bytes = 16

        public float Opacity;              // 4 bytes
        public int ActiveBlotCount;        // 4 bytes
        public float HdrMultiplier;        // 4 bytes
        public float Padding1;             // 4 bytes = 32

        public Vector4 Padding2;           // 16 bytes = 48
        public Vector4 Padding3;           // 16 bytes = 64
        public Vector4 Padding4;           // 16 bytes = 80
    }

    [StructLayout(LayoutKind.Sequential, Size = 64)]
    private struct BlotInstance
    {
        public Vector2 Position;           // 8 bytes - Screen position
        public float CurrentRadius;        // 4 bytes - Current radius
        public float MaxRadius;            // 4 bytes = 16 - Maximum radius

        public float BirthTime;            // 4 bytes - When spawned
        public float Lifetime;             // 4 bytes - Total lifetime
        public float Age;                  // 4 bytes - Current age
        public float Seed;                 // 4 bytes = 32 - Random seed for noise

        public Vector4 Color;              // 16 bytes = 48 - RGBA color

        public float SpreadSpeed;          // 4 bytes - Pixels per second
        public float Padding1;             // 4 bytes
        public float Padding2;             // 4 bytes
        public float Padding3;             // 4 bytes = 64
    }

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _blotBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Blot management (CPU side)
    private readonly BlotInstance[] _blots = new BlotInstance[HardMaxBlots];
    private readonly BlotInstance[] _gpuBlots = new BlotInstance[HardMaxBlots];
    private int _activeBlotCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _accumulatedDistance;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;

    // Viewport
    private Vector2 _viewportSize;

    // Rate limiting
    private int _spawnsThisSecond;
    private float _lastSecondStart;

    // Configuration fields
    private float _dropSize = 60f;
    private float _spreadSpeed = 50f;
    private float _edgeIrregularity = 0.3f;
    private float _opacity = 0.7f;
    private float _lifetime = 3.0f;
    private int _colorMode = 1; // 0=Ink, 1=Watercolor
    private int _inkColorIndex = 0;
    private int _watercolorIndex = 0;
    private bool _randomColor = true;
    private bool _spawnOnClick = true;
    private bool _spawnOnMove = false;
    private float _moveDistance = 80f;
    private int _maxBlots = 30;
    private int _maxBlotsPerSecond = 20;

    // Color palettes
    private static readonly Vector4[] InkColors =
    [
        new(0.1f, 0.1f, 0.1f, 1f),      // Black
        new(0.118f, 0.227f, 0.541f, 1f), // Blue (#1E3A8A)
        new(0.6f, 0.106f, 0.106f, 1f),   // Red (#991B1B)
        new(0.471f, 0.208f, 0.059f, 1f)  // Sepia (#78350F)
    ];

    private static readonly Vector4[] WatercolorColors =
    [
        new(0.576f, 0.773f, 0.992f, 1f), // Soft Blue (#93C5FD)
        new(0.984f, 0.812f, 0.910f, 1f), // Soft Pink (#FBCFE8)
        new(0.733f, 0.969f, 0.816f, 1f), // Soft Green (#BBF7D0)
        new(0.867f, 0.839f, 0.996f, 1f), // Soft Purple (#DDD6FE)
        new(0.996f, 0.941f, 0.659f, 1f)  // Soft Yellow (#FEF08A)
    ];

    // Public properties for UI binding
    public float DropSize { get => _dropSize; set => _dropSize = value; }
    public float SpreadSpeed { get => _spreadSpeed; set => _spreadSpeed = value; }
    public float EdgeIrregularity { get => _edgeIrregularity; set => _edgeIrregularity = value; }
    public float Opacity { get => _opacity; set => _opacity = value; }
    public float Lifetime { get => _lifetime; set => _lifetime = value; }
    public int ColorMode { get => _colorMode; set => _colorMode = value; }
    public int InkColorIndex { get => _inkColorIndex; set => _inkColorIndex = value; }
    public int WatercolorIndex { get => _watercolorIndex; set => _watercolorIndex = value; }
    public bool RandomColor { get => _randomColor; set => _randomColor = value; }
    public bool SpawnOnClick { get => _spawnOnClick; set => _spawnOnClick = value; }
    public bool SpawnOnMove { get => _spawnOnMove; set => _spawnOnMove = value; }
    public float MoveDistance { get => _moveDistance; set => _moveDistance = value; }
    public int MaxBlots { get => _maxBlots; set => _maxBlots = value; }
    public int MaxBlotsPerSecond { get => _maxBlotsPerSecond; set => _maxBlotsPerSecond = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        _viewportSize = context.ViewportSize;

        // Load and compile shaders
        var shaderCode = LoadShaderResource("Shaders.InkBlotShader.hlsl");
        _vertexShader = context.CompileShader(shaderCode, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderCode, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<InkBlotConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create blot instance buffer (structured buffer)
        _blotBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<BlotInstance>() * HardMaxBlots,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<BlotInstance>()
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("dropSize", out float dropSize))
            _dropSize = dropSize;
        if (Configuration.TryGet("spreadSpeed", out float spreadSpeed))
            _spreadSpeed = spreadSpeed;
        if (Configuration.TryGet("edgeIrregularity", out float irregularity))
            _edgeIrregularity = irregularity;
        if (Configuration.TryGet("opacity", out float opacity))
            _opacity = opacity;
        if (Configuration.TryGet("lifetime", out float lifetime))
            _lifetime = lifetime;
        if (Configuration.TryGet("colorMode", out int colorMode))
            _colorMode = colorMode;
        if (Configuration.TryGet("inkColorIndex", out int inkColor))
            _inkColorIndex = inkColor;
        if (Configuration.TryGet("watercolorIndex", out int watercolorColor))
            _watercolorIndex = watercolorColor;
        if (Configuration.TryGet("randomColor", out bool randomColor))
            _randomColor = randomColor;
        if (Configuration.TryGet("spawnOnClick", out bool spawnOnClick))
            _spawnOnClick = spawnOnClick;
        if (Configuration.TryGet("spawnOnMove", out bool spawnOnMove))
            _spawnOnMove = spawnOnMove;
        if (Configuration.TryGet("moveDistance", out float moveDistance))
            _moveDistance = moveDistance;
        if (Configuration.TryGet("maxBlots", out int maxBlots))
            _maxBlots = maxBlots;
        if (Configuration.TryGet("maxBlotsPerSecond", out int maxBlotsPerSecond))
            _maxBlotsPerSecond = maxBlotsPerSecond;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        var dt = gameTime.DeltaSeconds;
        var totalTime = gameTime.TotalSeconds;

        // Reset rate limiting each second
        if (totalTime - _lastSecondStart >= 1f)
        {
            _lastSecondStart = totalTime;
            _spawnsThisSecond = 0;
        }

        // Track mouse movement
        var currentPos = mouseState.Position;
        var distanceFromLast = Vector2.Distance(currentPos, _lastMousePos);

        // Mouse move trigger
        if (_spawnOnMove && distanceFromLast > 0.1f)
        {
            _accumulatedDistance += distanceFromLast;
            if (_accumulatedDistance >= _moveDistance)
            {
                SpawnBlot(currentPos, totalTime);
                _accumulatedDistance = 0f;
            }
        }

        // Click triggers
        var leftPressed = mouseState.ButtonsDown.HasFlag(CoreMouseButtons.Left);
        var rightPressed = mouseState.ButtonsDown.HasFlag(CoreMouseButtons.Right);

        if (_spawnOnClick && leftPressed && !_wasLeftPressed)
        {
            SpawnBlot(currentPos, totalTime);
        }
        if (_spawnOnClick && rightPressed && !_wasRightPressed)
        {
            SpawnBlot(currentPos, totalTime);
        }

        _wasLeftPressed = leftPressed;
        _wasRightPressed = rightPressed;
        _lastMousePos = currentPos;

        // Update existing blots
        UpdateBlots(dt, totalTime);
    }

    private void UpdateBlots(float dt, float totalTime)
    {
        _activeBlotCount = 0;

        for (int i = 0; i < HardMaxBlots; i++)
        {
            ref var blot = ref _blots[i];
            if (blot.Age < 0) continue; // Inactive blot

            // Update age
            blot.Age += dt;

            // Check if expired
            if (blot.Age >= blot.Lifetime)
            {
                blot.Age = -1; // Mark as inactive
                continue;
            }

            // Update radius (expand over time)
            float progress = blot.Age * blot.SpreadSpeed;
            blot.CurrentRadius = Math.Min(progress, blot.MaxRadius);

            // Copy to GPU buffer
            _gpuBlots[_activeBlotCount++] = blot;
        }
    }

    private void SpawnBlot(Vector2 position, float totalTime)
    {
        // Rate limiting
        if (_spawnsThisSecond >= _maxBlotsPerSecond)
            return;

        // Active count limiting
        if (_activeBlotCount >= _maxBlots)
            return;

        // Find empty slot
        int slot = -1;
        for (int i = 0; i < HardMaxBlots; i++)
        {
            if (_blots[i].Age < 0)
            {
                slot = i;
                break;
            }
        }
        if (slot < 0) return;

        // Determine color
        Vector4 color;
        if (_colorMode == 0) // Ink mode
        {
            if (_randomColor)
            {
                color = InkColors[Random.Shared.Next(InkColors.Length)];
            }
            else
            {
                color = InkColors[Math.Clamp(_inkColorIndex, 0, InkColors.Length - 1)];
            }
        }
        else // Watercolor mode
        {
            if (_randomColor)
            {
                color = WatercolorColors[Random.Shared.Next(WatercolorColors.Length)];
            }
            else
            {
                color = WatercolorColors[Math.Clamp(_watercolorIndex, 0, WatercolorColors.Length - 1)];
            }
        }

        // Create blot
        _blots[slot] = new BlotInstance
        {
            Position = position,
            CurrentRadius = 0f,
            MaxRadius = _dropSize,
            BirthTime = totalTime,
            Lifetime = _lifetime,
            Age = 0f,
            Seed = Random.Shared.NextSingle() * 1000f,
            Color = color,
            SpreadSpeed = _spreadSpeed,
            Padding1 = 0,
            Padding2 = 0,
            Padding3 = 0
        };

        _spawnsThisSecond++;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeBlotCount == 0) return;

        var totalTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Update constant buffer
        var constants = new InkBlotConstants
        {
            ViewportSize = _viewportSize,
            Time = totalTime,
            EdgeIrregularity = _edgeIrregularity,
            Opacity = _opacity,
            ActiveBlotCount = _activeBlotCount,
            HdrMultiplier = context.HdrPeakBrightness
        };

        context.UpdateBuffer(_constantBuffer!, MemoryMarshal.CreateReadOnlySpan(ref constants, 1));

        // Update blot buffer
        context.UpdateBuffer(_blotBuffer!, new ReadOnlySpan<BlotInstance>(_gpuBlots, 0, _activeBlotCount));

        // Set render state
        context.SetBlendState(BlendMode.Alpha);
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _blotBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _blotBuffer!);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced quads (6 vertices per quad, one per blot)
        context.DrawInstanced(6, _activeBlotCount, 0, 0);
    }

    protected override void OnViewportSizeChanged(Vector2 newSize)
    {
        _viewportSize = newSize;
    }

    protected override void OnDispose()
    {
        _blotBuffer?.Dispose();
        _constantBuffer?.Dispose();
        _pixelShader?.Dispose();
        _vertexShader?.Dispose();
    }

    private string LoadShaderResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.InkBlot.{name}";
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new FileNotFoundException($"Shader resource not found: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
