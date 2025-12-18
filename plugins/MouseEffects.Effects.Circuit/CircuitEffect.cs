using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Circuit;

public sealed class CircuitEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "circuit",
        Name = "Circuit",
        Description = "PCB-style circuit traces that grow from the mouse cursor like electronic veins",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 48)]
    private struct CircuitConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public float Time;                // 4 bytes
        public float GlowIntensity;       // 4 bytes = 16
        public float NodeSize;            // 4 bytes
        public float LineThickness;       // 4 bytes
        public float HdrMultiplier;       // 4 bytes
        public float Padding;             // 4 bytes = 32
        public Vector4 TraceColor;        // 16 bytes = 48
    }

    [StructLayout(LayoutKind.Sequential, Size = 48)]
    private struct TraceSegment
    {
        public Vector2 Start;             // 8 bytes - Start position
        public Vector2 End;               // 8 bytes = 16 - End position
        public float Progress;            // 4 bytes - Growth progress (0->1)
        public float Lifetime;            // 4 bytes - Current lifetime
        public float MaxLifetime;         // 4 bytes - Total lifetime
        public int Direction;             // 4 bytes = 32 - 0=right, 1=down, 2=left, 3=up
        public Vector4 Color;             // 16 bytes = 48
    }

    // Constants
    private const int MaxSegments = 512;
    private const float SegmentLength = 40f;     // Length of each trace segment

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _segmentBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Trace management (CPU side)
    private readonly TraceSegment[] _segments = new TraceSegment[MaxSegments];
    private readonly TraceSegment[] _gpuSegments = new TraceSegment[MaxSegments];
    private int _nextSegmentIndex;
    private int _activeSegmentCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _accumulatedDistance;
    private bool _isFirstUpdate = true;

    // Configuration fields (cir_ prefix for Circuit)
    private int _traceCount = 12;                // Number of traces per spawn
    private float _growthSpeed = 150f;           // Growth rate
    private float _maxLength = 200f;             // Max trace length
    private float _branchProbability = 0.3f;     // Probability of branching
    private float _nodeSize = 4f;                // Size of connection nodes
    private float _glowIntensity = 1.5f;         // Glow intensity
    private float _lineThickness = 2.5f;         // Trace thickness
    private float _traceLifetime = 1.5f;         // How long traces persist
    private float _spawnThreshold = 50f;         // Distance threshold for spawning
    private int _colorPreset = 0;                // 0=Green, 1=Cyan, 2=Gold, 3=Orange, 4=Purple, 5=Custom
    private Vector4 _customColor = new(0f, 1f, 0f, 1f);  // Custom color

    // Public properties for UI binding
    public int TraceCount { get => _traceCount; set => _traceCount = value; }
    public float GrowthSpeed { get => _growthSpeed; set => _growthSpeed = value; }
    public float MaxLength { get => _maxLength; set => _maxLength = value; }
    public float BranchProbability { get => _branchProbability; set => _branchProbability = value; }
    public float NodeSize { get => _nodeSize; set => _nodeSize = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public float LineThickness { get => _lineThickness; set => _lineThickness = value; }
    public float TraceLifetime { get => _traceLifetime; set => _traceLifetime = value; }
    public float SpawnThreshold { get => _spawnThreshold; set => _spawnThreshold = value; }
    public int ColorPreset { get => _colorPreset; set => _colorPreset = value; }
    public Vector4 CustomColor { get => _customColor; set => _customColor = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("CircuitShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<CircuitConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create segment structured buffer
        _segmentBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<TraceSegment>() * MaxSegments,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<TraceSegment>()
        });

        _lastMousePos = new Vector2(context.ViewportSize.X / 2, context.ViewportSize.Y / 2);
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("cir_traceCount", out int count))
            _traceCount = count;
        if (Configuration.TryGet("cir_growthSpeed", out float speed))
            _growthSpeed = speed;
        if (Configuration.TryGet("cir_maxLength", out float maxLen))
            _maxLength = maxLen;
        if (Configuration.TryGet("cir_branchProbability", out float branchProb))
            _branchProbability = branchProb;
        if (Configuration.TryGet("cir_nodeSize", out float nodeSize))
            _nodeSize = nodeSize;
        if (Configuration.TryGet("cir_glowIntensity", out float glow))
            _glowIntensity = glow;
        if (Configuration.TryGet("cir_lineThickness", out float thickness))
            _lineThickness = thickness;
        if (Configuration.TryGet("cir_traceLifetime", out float lifetime))
            _traceLifetime = lifetime;
        if (Configuration.TryGet("cir_spawnThreshold", out float threshold))
            _spawnThreshold = threshold;
        if (Configuration.TryGet("cir_colorPreset", out int preset))
            _colorPreset = preset;
        if (Configuration.TryGet("cir_customColor", out Vector4 color))
            _customColor = color;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;

        // Skip first frame to avoid huge line from (0,0)
        if (_isFirstUpdate)
        {
            _lastMousePos = mouseState.Position;
            _isFirstUpdate = false;
            return;
        }

        // Calculate distance moved this frame
        float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);
        _accumulatedDistance += distanceFromLast;

        // Spawn new traces when threshold is reached
        if (_accumulatedDistance >= _spawnThreshold)
        {
            SpawnTraces(mouseState.Position);
            _accumulatedDistance = 0f;
        }

        // Update last mouse position
        _lastMousePos = mouseState.Position;

        // Update existing segments
        UpdateSegments(deltaTime);
    }

    private void SpawnTraces(Vector2 origin)
    {
        Vector4 traceColor = GetTraceColor();

        for (int i = 0; i < _traceCount; i++)
        {
            if (_activeSegmentCount >= MaxSegments)
                break;

            // Random initial direction (0=right, 1=down, 2=left, 3=up)
            int direction = Random.Shared.Next(4);

            SpawnSegment(origin, direction, traceColor, 0);
        }
    }

    private void SpawnSegment(Vector2 start, int direction, Vector4 color, int depth)
    {
        if (depth > 3 || _activeSegmentCount >= MaxSegments)
            return;

        // Calculate end position based on direction
        Vector2 end = CalculateEndPosition(start, direction, SegmentLength);

        // Create segment
        ref TraceSegment segment = ref _segments[_nextSegmentIndex];
        segment.Start = start;
        segment.End = end;
        segment.Progress = 0f;
        segment.Lifetime = _traceLifetime;
        segment.MaxLifetime = _traceLifetime;
        segment.Direction = direction;
        segment.Color = color;

        _nextSegmentIndex = (_nextSegmentIndex + 1) % MaxSegments;
        _activeSegmentCount++;

        // Branch with probability
        if (Random.Shared.NextSingle() < _branchProbability && depth < 2)
        {
            // Create perpendicular branches
            int branchDir1 = (direction + 1) % 4;
            int branchDir2 = (direction + 3) % 4;

            // Branch from midpoint or endpoint
            Vector2 branchPoint = Vector2.Lerp(start, end, 0.5f + Random.Shared.NextSingle() * 0.3f);

            if (Random.Shared.NextSingle() > 0.5f)
                SpawnSegment(branchPoint, branchDir1, color, depth + 1);
            if (Random.Shared.NextSingle() > 0.5f)
                SpawnSegment(branchPoint, branchDir2, color, depth + 1);
        }
    }

    private Vector2 CalculateEndPosition(Vector2 start, int direction, float length)
    {
        return direction switch
        {
            0 => start + new Vector2(length, 0),      // Right
            1 => start + new Vector2(0, length),      // Down
            2 => start + new Vector2(-length, 0),     // Left
            3 => start + new Vector2(0, -length),     // Up
            _ => start
        };
    }

    private void UpdateSegments(float deltaTime)
    {
        _activeSegmentCount = 0;

        for (int i = 0; i < MaxSegments; i++)
        {
            if (_segments[i].Lifetime > 0)
            {
                // Grow the segment
                if (_segments[i].Progress < 1f)
                {
                    float growthThisFrame = (_growthSpeed / SegmentLength) * deltaTime;
                    _segments[i].Progress = Math.Min(1f, _segments[i].Progress + growthThisFrame);
                }

                // Age the segment
                _segments[i].Lifetime -= deltaTime;

                if (_segments[i].Lifetime > 0)
                    _activeSegmentCount++;
            }
        }
    }

    private Vector4 GetTraceColor()
    {
        return _colorPreset switch
        {
            0 => new Vector4(0f, 1f, 0f, 1f),           // Classic Green
            1 => new Vector4(0f, 1f, 1f, 1f),           // Cyan
            2 => new Vector4(1f, 0.84f, 0f, 1f),        // Gold PCB
            3 => new Vector4(1f, 0.55f, 0f, 1f),        // Orange
            4 => new Vector4(0.58f, 0f, 0.83f, 1f),     // Purple
            5 => _customColor,                          // Custom
            _ => new Vector4(0f, 1f, 0f, 1f)
        };
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeSegmentCount == 0) return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU segment buffer
        int gpuIndex = 0;
        for (int i = 0; i < MaxSegments && gpuIndex < MaxSegments; i++)
        {
            if (_segments[i].Lifetime > 0)
            {
                _gpuSegments[gpuIndex++] = _segments[i];
            }
        }

        // Fill remaining with zeroed segments
        for (int i = gpuIndex; i < MaxSegments; i++)
        {
            _gpuSegments[i] = default;
        }

        // Update segment buffer
        context.UpdateBuffer(_segmentBuffer!, (ReadOnlySpan<TraceSegment>)_gpuSegments.AsSpan());

        // Update constant buffer
        var constants = new CircuitConstants
        {
            ViewportSize = context.ViewportSize,
            Time = currentTime,
            GlowIntensity = _glowIntensity,
            NodeSize = _nodeSize,
            LineThickness = _lineThickness,
            HdrMultiplier = context.HdrPeakBrightness,
            Padding = 0f,
            TraceColor = GetTraceColor()
        };
        context.UpdateBuffer(_constantBuffer!, constants);

        // Set up rendering state
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _segmentBuffer!);
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
        _segmentBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.Circuit.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
