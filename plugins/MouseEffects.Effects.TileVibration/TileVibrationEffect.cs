using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.TileVibration;

/// <summary>
/// Tile vibration effect that creates vibrating, shrinking tiles when the mouse moves.
/// Each tile captures a portion of the screen and displays it with vibration effects.
/// </summary>
public sealed class TileVibrationEffect : EffectBase
{
    private const int MaxTiles = 100;

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "tile-vibration",
        Name = "Tile Vibration",
        Description = "Creates vibrating screen tiles that follow the mouse cursor and shrink over time",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.VisualFilter
    };

    // GPU resources
    private IShader? _vertexShader;
    private IShader? _pixelShader;
    private IBuffer? _paramsBuffer;
    private IBuffer? _tileBuffer;
    private ISamplerState? _linearSampler;

    // Tile state (CPU-side)
    private readonly Tile[] _tiles = new Tile[MaxTiles];
    private readonly TileGPU[] _gpuTiles = new TileGPU[MaxTiles]; // Pooled to avoid allocation per frame
    private readonly int[] _sortedIndices = new int[MaxTiles]; // Pooled for sorting active tiles
    private int _nextTile;
    private Vector2 _lastTilePosition;
    private float _totalTime;

    // Configuration
    private float _tileLifespan = 2.0f;
    private float _maxWidth = 100f;
    private float _maxHeight = 100f;
    private float _minWidth = 20f;
    private float _minHeight = 20f;
    private bool _syncWidthHeight = true;
    private int _edgeStyle = 0; // 0=Sharp, 1=Soft
    private float _vibrationSpeed = 1.0f;
    private bool _displacementEnabled = true;
    private float _displacementMax = 10f;
    private bool _zoomEnabled = false;
    private float _zoomMin = 0.8f;
    private float _zoomMax = 1.2f;
    private bool _rotationEnabled = false;
    private float _rotationAmplitude = 15f; // degrees
    private bool _outlineEnabled = false;
    private Vector4 _outlineColor = new(1f, 1f, 1f, 1f); // White
    private float _outlineSize = 2f;

    public override EffectMetadata Metadata => _metadata;

    /// <summary>
    /// This effect requires continuous screen capture to show live desktop content.
    /// </summary>
    public override bool RequiresContinuousScreenCapture => true;

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shaders
        var shaderSource = LoadEmbeddedShader("TileVibration.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer for parameters
        var paramsDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<TileParams>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _paramsBuffer = context.CreateBuffer(paramsDesc);

        // Create structured buffer for tiles
        var tileDesc = new BufferDescription
        {
            Size = MaxTiles * Marshal.SizeOf<TileGPU>(),
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<TileGPU>()
        };
        _tileBuffer = context.CreateBuffer(tileDesc);

        // Create sampler
        _linearSampler = context.CreateSamplerState(SamplerDescription.LinearClamp);

        // Initialize tiles
        for (int i = 0; i < MaxTiles; i++)
        {
            _tiles[i] = new Tile { IsActive = false };
        }
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("tileLifespan", out float lifespan))
            _tileLifespan = lifespan;

        if (Configuration.TryGet("maxWidth", out float maxWidth))
            _maxWidth = maxWidth;

        if (Configuration.TryGet("maxHeight", out float maxHeight))
            _maxHeight = maxHeight;

        if (Configuration.TryGet("minWidth", out float minWidth))
            _minWidth = minWidth;

        if (Configuration.TryGet("minHeight", out float minHeight))
            _minHeight = minHeight;

        if (Configuration.TryGet("syncWidthHeight", out bool sync))
            _syncWidthHeight = sync;

        if (Configuration.TryGet("edgeStyle", out int edgeStyle))
            _edgeStyle = edgeStyle;

        if (Configuration.TryGet("vibrationSpeed", out float speed))
            _vibrationSpeed = speed;

        if (Configuration.TryGet("displacementEnabled", out bool dispEnabled))
            _displacementEnabled = dispEnabled;

        if (Configuration.TryGet("displacementMax", out float dispMax))
            _displacementMax = dispMax;

        if (Configuration.TryGet("zoomEnabled", out bool zoomEnabled))
            _zoomEnabled = zoomEnabled;

        if (Configuration.TryGet("zoomMin", out float zoomMin))
            _zoomMin = zoomMin;

        if (Configuration.TryGet("zoomMax", out float zoomMax))
            _zoomMax = zoomMax;

        if (Configuration.TryGet("rotationEnabled", out bool rotEnabled))
            _rotationEnabled = rotEnabled;

        if (Configuration.TryGet("rotationAmplitude", out float rotAmplitude))
            _rotationAmplitude = rotAmplitude;

        if (Configuration.TryGet("outlineEnabled", out bool outlineEnabled))
            _outlineEnabled = outlineEnabled;

        if (Configuration.TryGet("outlineColor", out Vector4 outlineColor))
            _outlineColor = outlineColor;

        if (Configuration.TryGet("outlineSize", out float outlineSize))
            _outlineSize = outlineSize;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        var dt = (float)gameTime.DeltaTime.TotalSeconds;
        _totalTime = (float)gameTime.TotalTime.TotalSeconds;

        // Update existing tiles - age them
        for (int i = 0; i < MaxTiles; i++)
        {
            ref var tile = ref _tiles[i];
            if (!tile.IsActive) continue;

            tile.Age += dt;
            if (tile.Age >= tile.Lifetime)
            {
                tile.IsActive = false;
            }
        }

        // Spawn new tiles based on mouse movement distance
        var mousePos = mouseState.Position;
        float distanceFromLast = Vector2.Distance(mousePos, _lastTilePosition);
        float spawnThreshold = MathF.Max(_maxWidth, _maxHeight) * 0.5f;

        if (distanceFromLast >= spawnThreshold)
        {
            SpawnTile(mousePos);
            _lastTilePosition = mousePos;
        }
    }

    private void SpawnTile(Vector2 position)
    {
        ref var tile = ref _tiles[_nextTile];
        _nextTile = (_nextTile + 1) % MaxTiles;

        tile.Position = position;
        tile.BirthTime = _totalTime;
        tile.Age = 0;
        tile.Lifetime = _tileLifespan;
        tile.RandomSeed = Random.Shared.NextSingle();
        tile.IsActive = true;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null) return;

        var screenTexture = context.ScreenTexture;
        if (screenTexture == null) return;

        // Count active tiles and prepare GPU data (using pooled arrays - no allocations)
        int activeTileCount = 0;

        // Collect active tile indices (no allocation)
        for (int i = 0; i < MaxTiles; i++)
        {
            if (_tiles[i].IsActive)
                _sortedIndices[activeTileCount++] = i;
        }

        // Sort by age descending (oldest first, so newest renders on top)
        // Using insertion sort - efficient for small N, no allocations
        for (int i = 1; i < activeTileCount; i++)
        {
            int key = _sortedIndices[i];
            float keyAge = _tiles[key].Age;
            int j = i - 1;
            while (j >= 0 && _tiles[_sortedIndices[j]].Age < keyAge)
            {
                _sortedIndices[j + 1] = _sortedIndices[j];
                j--;
            }
            _sortedIndices[j + 1] = key;
        }

        // Populate GPU tile data
        int gpuTileIndex = 0;
        for (int idx = 0; idx < activeTileCount; idx++)
        {
            int i = _sortedIndices[idx];
            ref var tile = ref _tiles[i];
            float normalizedAge = tile.Age / tile.Lifetime;

            // Calculate current size (interpolate from max to min)
            float currentWidth = MathF.Max(_minWidth, _maxWidth * (1 - normalizedAge) + _minWidth * normalizedAge);
            float currentHeight = _syncWidthHeight
                ? currentWidth
                : MathF.Max(_minHeight, _maxHeight * (1 - normalizedAge) + _minHeight * normalizedAge);

            _gpuTiles[gpuTileIndex] = new TileGPU
            {
                Position = tile.Position,
                Age = normalizedAge,
                Width = currentWidth,
                Height = currentHeight,
                RandomSeed = tile.RandomSeed
            };
            gpuTileIndex++;
        }

        // Fill remaining slots with inactive tiles
        for (int i = gpuTileIndex; i < MaxTiles; i++)
        {
            _gpuTiles[i] = new TileGPU { Age = 2.0f }; // Age > 1 means expired
        }

        context.UpdateBuffer(_tileBuffer!, (ReadOnlySpan<TileGPU>)_gpuTiles);

        // Build vibration flags
        int vibrationFlags = 0;
        if (_displacementEnabled) vibrationFlags |= 1;
        if (_zoomEnabled) vibrationFlags |= 2;
        if (_rotationEnabled) vibrationFlags |= 4;

        // Update parameters
        var cbParams = new TileParams
        {
            ViewportSize = context.ViewportSize,
            Time = _totalTime * _vibrationSpeed,
            TileCount = activeTileCount,
            VibrationFlags = vibrationFlags,
            DisplacementMax = _displacementMax,
            ZoomMin = _zoomMin,
            ZoomMax = _zoomMax,
            RotationAmplitude = _rotationAmplitude * MathF.PI / 180f, // Convert to radians
            EdgeStyle = _edgeStyle,
            OutlineEnabled = _outlineEnabled ? 1 : 0,
            OutlineColor = _outlineColor,
            OutlineSize = _outlineSize
        };

        context.UpdateBuffer(_paramsBuffer!, cbParams);

        // Set shaders
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);

        // Set resources
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _paramsBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, screenTexture);
        context.SetShaderResource(ShaderStage.Pixel, 1, _tileBuffer!);
        context.SetSampler(ShaderStage.Pixel, 0, _linearSampler!);

        // Use opaque blend - shader renders fullscreen with passthrough
        context.SetBlendState(BlendMode.Opaque);

        // Draw fullscreen quad
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleStrip);
        context.Draw(4, 0);

        // Unbind texture resource
        context.SetShaderResource(ShaderStage.Pixel, 0, (ITexture?)null);
    }

    protected override void OnViewportSizeChanged(Vector2 newSize)
    {
        // No texture recreation needed
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _paramsBuffer?.Dispose();
        _tileBuffer?.Dispose();
        _linearSampler?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(TileVibrationEffect).Assembly;
        var resourceName = $"MouseEffects.Effects.TileVibration.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    #region Data Structures

    private struct Tile
    {
        public Vector2 Position;
        public float BirthTime;
        public float Age;
        public float Lifetime;
        public float RandomSeed;
        public bool IsActive;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TileGPU
    {
        public Vector2 Position;
        public float Age;
        public float Width;
        public float Height;
        public float RandomSeed;
        public float Padding1;
        public float Padding2;
    }

    [StructLayout(LayoutKind.Sequential, Size = 96)]
    private struct TileParams
    {
        public Vector2 ViewportSize;      // 8 bytes, offset 0
        public float Time;                 // 4 bytes, offset 8
        public int TileCount;              // 4 bytes, offset 12
        public int VibrationFlags;         // 4 bytes, offset 16
        public float DisplacementMax;      // 4 bytes, offset 20
        public float ZoomMin;              // 4 bytes, offset 24
        public float ZoomMax;              // 4 bytes, offset 28
        public float RotationAmplitude;    // 4 bytes, offset 32
        public int EdgeStyle;              // 4 bytes, offset 36
        public int OutlineEnabled;         // 4 bytes, offset 40
        public float OutlineSize;          // 4 bytes, offset 44
        public Vector4 OutlineColor;       // 16 bytes, offset 48
        private Vector4 _padding;          // 16 bytes, offset 64 (padding to 96)
        private Vector4 _padding2;         // 16 bytes, offset 80
    }

    #endregion
}
