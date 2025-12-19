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
        Category = EffectCategory.Digital
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
    private const int MaxSegmentsBuffer = 2048;  // Buffer size (max configurable)
    private const float SegmentLength = 40f;     // Length of each trace segment

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _segmentBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Trace management (CPU side)
    private readonly TraceSegment[] _segments = new TraceSegment[MaxSegmentsBuffer];
    private readonly TraceSegment[] _gpuSegments = new TraceSegment[MaxSegmentsBuffer];
    private readonly float[] _originalAlpha = new float[MaxSegmentsBuffer];  // Store original alpha for fade
    private readonly int[] _recycleQueue = new int[64];   // Indices of segments being recycled
    private int _recycleQueueCount;
    private int _nextSegmentIndex;
    private int _activeSegmentCount;
    private int _maxSegments = 512;              // Configurable max segments

    // Mouse tracking
    private Vector2 _lastMousePos;
    private float _accumulatedDistance;
    private bool _isFirstUpdate = true;

    // Configuration fields (cir_ prefix for Circuit)
    private int _traceCount = 12;                // Number of traces per spawn
    private float _growthSpeed = 150f;           // Growth rate
    private float _maxLength = 200f;             // Max trace length
    private float _branchProbability = 0.3f;     // Probability of branching
    private float _nodeSize = 2f;                 // Size of connection nodes
    private float _glowIntensity = 0.5f;         // Glow intensity
    private bool _glowAnimationEnabled = true;   // Animate glow
    private float _glowAnimationSpeed = 0.5f;    // Animation speed
    private float _glowMinIntensity = 0.3f;      // Min glow when animating
    private float _glowMaxIntensity = 1.0f;      // Max glow when animating
    private float _lineThickness = 1f;           // Trace thickness
    private float _traceLifetime = 5f;           // How long traces persist
    private float _spawnThreshold = 50f;         // Distance threshold for spawning
    private int _colorPreset = 5;                // 0=Green, 1=Cyan, 2=Gold, 3=Orange, 4=Purple, 5=Custom
    private Vector4 _customColor = new(0f, 1f, 0f, 1f);  // Custom color
    private bool _rainbowEnabled = true;         // Rainbow color mode
    private float _rainbowSpeed = 1.0f;          // Rainbow cycle speed
    private float _elapsedTime;                  // Time tracking for rainbow

    // Public properties for UI binding
    public int MaxSegments { get => _maxSegments; set => _maxSegments = Math.Clamp(value, 64, MaxSegmentsBuffer); }
    public int TraceCount { get => _traceCount; set => _traceCount = value; }
    public float GrowthSpeed { get => _growthSpeed; set => _growthSpeed = value; }
    public float MaxLength { get => _maxLength; set => _maxLength = value; }
    public float BranchProbability { get => _branchProbability; set => _branchProbability = value; }
    public float NodeSize { get => _nodeSize; set => _nodeSize = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public bool GlowAnimationEnabled { get => _glowAnimationEnabled; set => _glowAnimationEnabled = value; }
    public float GlowAnimationSpeed { get => _glowAnimationSpeed; set => _glowAnimationSpeed = value; }
    public float GlowMinIntensity
    {
        get => _glowMinIntensity;
        set => _glowMinIntensity = Math.Min(value, _glowMaxIntensity - 0.1f);
    }
    public float GlowMaxIntensity
    {
        get => _glowMaxIntensity;
        set => _glowMaxIntensity = Math.Max(value, _glowMinIntensity + 0.1f);
    }
    public float LineThickness { get => _lineThickness; set => _lineThickness = value; }
    public float TraceLifetime { get => _traceLifetime; set => _traceLifetime = value; }
    public float SpawnThreshold { get => _spawnThreshold; set => _spawnThreshold = value; }
    public int ColorPreset { get => _colorPreset; set => _colorPreset = value; }
    public Vector4 CustomColor { get => _customColor; set => _customColor = value; }
    public bool RainbowEnabled { get => _rainbowEnabled; set => _rainbowEnabled = value; }
    public float RainbowSpeed { get => _rainbowSpeed; set => _rainbowSpeed = value; }

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
            Size = Marshal.SizeOf<TraceSegment>() * MaxSegmentsBuffer,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<TraceSegment>()
        });

        _lastMousePos = new Vector2(context.ViewportSize.X / 2, context.ViewportSize.Y / 2);
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("cir_maxSegments", out int maxSeg))
            _maxSegments = Math.Clamp(maxSeg, 64, MaxSegmentsBuffer);
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
        if (Configuration.TryGet("cir_glowAnimationEnabled", out bool glowAnim))
            _glowAnimationEnabled = glowAnim;
        if (Configuration.TryGet("cir_glowAnimationSpeed", out float glowAnimSpeed))
            _glowAnimationSpeed = glowAnimSpeed;
        if (Configuration.TryGet("cir_glowMinIntensity", out float glowMin))
            _glowMinIntensity = glowMin;
        if (Configuration.TryGet("cir_glowMaxIntensity", out float glowMax))
            _glowMaxIntensity = glowMax;
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
        if (Configuration.TryGet("cir_rainbowEnabled", out bool rainbow))
            _rainbowEnabled = rainbow;
        if (Configuration.TryGet("cir_rainbowSpeed", out float rainbowSpeed))
            _rainbowSpeed = rainbowSpeed;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        _elapsedTime += deltaTime;

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

        // When at capacity and moving, prepare segments for recycling with fade
        if (_activeSegmentCount >= _maxSegments && _accumulatedDistance > 0)
        {
            // Mark oldest segments for recycling if not already marked
            if (_recycleQueueCount == 0)
            {
                MarkOldestForRecycle(_traceCount);
            }

            // Apply fade based on distance progress toward threshold
            float fadeProgress = Math.Clamp(_accumulatedDistance / _spawnThreshold, 0f, 1f);
            ApplyRecycleFade(fadeProgress);
        }

        // Spawn new traces when threshold is reached
        if (_accumulatedDistance >= _spawnThreshold)
        {
            SpawnTraces(mouseState.Position);
            _accumulatedDistance = 0f;
            _recycleQueueCount = 0; // Clear recycle queue after spawning
        }

        // Update last mouse position
        _lastMousePos = mouseState.Position;

        // Update existing segments
        UpdateSegments(deltaTime);
    }

    private void MarkOldestForRecycle(int count)
    {
        // Find the oldest segments by lifetime ratio (lowest remaining lifetime = oldest)
        var candidates = new List<(int index, float remainingRatio)>();

        for (int i = 0; i < MaxSegmentsBuffer; i++)
        {
            if (_segments[i].Lifetime > 0 && _segments[i].MaxLifetime > 0)
            {
                float ratio = _segments[i].Lifetime / _segments[i].MaxLifetime;
                candidates.Add((i, ratio));
            }
        }

        // Sort by remaining lifetime ratio (lowest first = oldest)
        candidates.Sort((a, b) => a.remainingRatio.CompareTo(b.remainingRatio));

        // Mark the oldest 'count' segments for recycling
        _recycleQueueCount = Math.Min(count, candidates.Count);
        for (int i = 0; i < _recycleQueueCount; i++)
        {
            int idx = candidates[i].index;
            _recycleQueue[i] = idx;
            _originalAlpha[idx] = _segments[idx].Color.W; // Store original alpha
        }
    }

    private void ApplyRecycleFade(float fadeProgress)
    {
        // Apply fade to segments marked for recycling
        for (int i = 0; i < _recycleQueueCount; i++)
        {
            int idx = _recycleQueue[i];
            if (_segments[idx].Lifetime > 0)
            {
                // Fade from original alpha to 0 based on progress
                _segments[idx].Color.W = _originalAlpha[idx] * (1f - fadeProgress);
            }
        }
    }

    private void SpawnTraces(Vector2 origin)
    {
        Vector4 traceColor = GetTraceColor();
        int recycleIndex = 0;

        for (int i = 0; i < _traceCount; i++)
        {
            // If at capacity, recycle faded segments
            if (_activeSegmentCount >= _maxSegments)
            {
                if (recycleIndex < _recycleQueueCount)
                {
                    // Kill the faded segment to make room
                    int recycleSlot = _recycleQueue[recycleIndex++];
                    _segments[recycleSlot].Lifetime = 0;
                    _activeSegmentCount--;
                }
                else
                {
                    break; // No more segments to recycle
                }
            }

            // Random initial direction (0=right, 1=down, 2=left, 3=up)
            int direction = Random.Shared.Next(4);

            SpawnSegment(origin, direction, traceColor, 0);
        }
    }

    private void SpawnSegment(Vector2 start, int direction, Vector4 color, int depth)
    {
        if (depth > 3 || _activeSegmentCount >= _maxSegments)
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

        _nextSegmentIndex = (_nextSegmentIndex + 1) % _maxSegments;
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

        for (int i = 0; i < MaxSegmentsBuffer; i++)
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
        // Rainbow mode when in custom preset and rainbow enabled
        if (_colorPreset == 5 && _rainbowEnabled)
        {
            return HsvToRgb(_elapsedTime * _rainbowSpeed % 1.0f, 1.0f, 1.0f);
        }

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

    private static Vector4 HsvToRgb(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1.0f - MathF.Abs(h * 6.0f % 2.0f - 1.0f));
        float m = v - c;

        float r, g, b;
        if (h < 1.0f / 6.0f)
            (r, g, b) = (c, x, 0);
        else if (h < 2.0f / 6.0f)
            (r, g, b) = (x, c, 0);
        else if (h < 3.0f / 6.0f)
            (r, g, b) = (0, c, x);
        else if (h < 4.0f / 6.0f)
            (r, g, b) = (0, x, c);
        else if (h < 5.0f / 6.0f)
            (r, g, b) = (x, 0, c);
        else
            (r, g, b) = (c, 0, x);

        return new Vector4(r + m, g + m, b + m, 1.0f);
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeSegmentCount == 0) return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU segment buffer
        int gpuIndex = 0;
        for (int i = 0; i < MaxSegmentsBuffer && gpuIndex < _maxSegments; i++)
        {
            if (_segments[i].Lifetime > 0)
            {
                _gpuSegments[gpuIndex++] = _segments[i];
            }
        }

        // Fill remaining with zeroed segments
        for (int i = gpuIndex; i < MaxSegmentsBuffer; i++)
        {
            _gpuSegments[i] = default;
        }

        // Update segment buffer
        context.UpdateBuffer(_segmentBuffer!, (ReadOnlySpan<TraceSegment>)_gpuSegments.AsSpan());

        // Calculate glow intensity (static or animated)
        float glowValue = _glowIntensity;
        if (_glowAnimationEnabled && _glowAnimationSpeed > 0)
        {
            // Oscillate between min and max using sine wave
            float pulse = MathF.Sin(_elapsedTime * _glowAnimationSpeed * MathF.PI * 2f) * 0.5f + 0.5f;
            glowValue = _glowMinIntensity + pulse * (_glowMaxIntensity - _glowMinIntensity);
        }

        // Update constant buffer
        var constants = new CircuitConstants
        {
            ViewportSize = context.ViewportSize,
            Time = currentTime,
            GlowIntensity = glowValue,
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
