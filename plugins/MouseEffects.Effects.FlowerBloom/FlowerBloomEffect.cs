using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.FlowerBloom;

public sealed class FlowerBloomEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "flowerbloom",
        Name = "Flower Bloom",
        Description = "Beautiful flowers that bloom and grow from the mouse cursor with organic petal unfurling",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    private struct FrameConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public float Time;                // 4 bytes
        public float HdrMultiplier;       // 4 bytes = 16
        public Vector4 Padding;           // 16 bytes = 32
    }

    [StructLayout(LayoutKind.Sequential, Size = 96)]
    private struct FlowerInstance
    {
        public Vector2 Position;          // 8 bytes - Flower center position
        public float BloomProgress;       // 4 bytes - 0.0 to 1.0 animation progress
        public float TotalBloomTime;      // 4 bytes - How long bloom animation takes = 16
        public Vector4 PrimaryColor;      // 16 bytes - Main petal color = 32
        public Vector4 SecondaryColor;    // 16 bytes - Center/accent color = 48
        public float Size;                // 4 bytes - Flower size
        public int PetalCount;            // 4 bytes - Number of petals (3-12)
        public int FlowerType;            // 4 bytes - 0=rose, 1=daisy, 2=lotus, 3=cherry
        public float RotationAngle;       // 4 bytes - Base rotation = 64
        public float Lifetime;            // 4 bytes - Current life remaining
        public float MaxLifetime;         // 4 bytes - Total lifetime
        public float FadeOutTime;         // 4 bytes - When to start fade
        public int HasStem;               // 4 bytes - 1 if stem visible = 80
        public float BirthTime;           // 4 bytes - When flower was created
        public float PetalCurvature;      // 4 bytes - How curved the petals are
        public float PetalWidth;          // 4 bytes - Width ratio of petals
        public float Padding;             // 4 bytes = 96
    }

    // Constants
    private const int MaxFlowers = 100;

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _flowerBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Flower management (CPU side)
    private readonly FlowerInstance[] _flowers = new FlowerInstance[MaxFlowers];
    private readonly FlowerInstance[] _gpuFlowers = new FlowerInstance[MaxFlowers];
    private int _nextFlowerIndex;
    private int _activeFlowerCount;

    // Mouse tracking
    private bool _wasLeftPressed;
    private bool _wasRightPressed;

    // Configuration fields
    private int _flowerType = 0; // 0=rose, 1=daisy, 2=lotus, 3=cherry
    private int _colorPalette = 0; // 0=spring, 1=summer, 2=tropical, 3=pastel
    private int _petalCount = 6;
    private float _flowerSize = 80f;
    private float _bloomDuration = 1.5f;
    private float _flowerLifetime = 5.0f;
    private float _fadeOutDuration = 1.0f;
    private bool _showStem = true;
    private bool _sizeVariation = true;
    private float _sizeVariationAmount = 0.3f;
    private bool _continuousSpawn = false;
    private float _spawnRate = 0.5f;
    private float _spawnAccumulator = 0f;

    // Trigger settings
    private bool _leftClickEnabled = true;
    private bool _rightClickEnabled = true;

    // Public properties for UI binding
    public int FlowerType { get => _flowerType; set => _flowerType = value; }
    public int ColorPalette { get => _colorPalette; set => _colorPalette = value; }
    public int PetalCount { get => _petalCount; set => _petalCount = Math.Clamp(value, 3, 12); }
    public float FlowerSize { get => _flowerSize; set => _flowerSize = value; }
    public float BloomDuration { get => _bloomDuration; set => _bloomDuration = value; }
    public float FlowerLifetime { get => _flowerLifetime; set => _flowerLifetime = value; }
    public float FadeOutDuration { get => _fadeOutDuration; set => _fadeOutDuration = value; }
    public bool ShowStem { get => _showStem; set => _showStem = value; }
    public bool SizeVariation { get => _sizeVariation; set => _sizeVariation = value; }
    public float SizeVariationAmount { get => _sizeVariationAmount; set => _sizeVariationAmount = value; }
    public bool ContinuousSpawn { get => _continuousSpawn; set => _continuousSpawn = value; }
    public float SpawnRate { get => _spawnRate; set => _spawnRate = value; }
    public bool LeftClickEnabled { get => _leftClickEnabled; set => _leftClickEnabled = value; }
    public bool RightClickEnabled { get => _rightClickEnabled; set => _rightClickEnabled = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("FlowerBloomShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FrameConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create flower structured buffer
        _flowerBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FlowerInstance>() * MaxFlowers,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<FlowerInstance>()
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("fb_flowerType", out int type))
            _flowerType = type;
        if (Configuration.TryGet("fb_colorPalette", out int palette))
            _colorPalette = palette;
        if (Configuration.TryGet("fb_petalCount", out int count))
            _petalCount = Math.Clamp(count, 3, 12);
        if (Configuration.TryGet("fb_flowerSize", out float size))
            _flowerSize = size;
        if (Configuration.TryGet("fb_bloomDuration", out float bloomDur))
            _bloomDuration = bloomDur;
        if (Configuration.TryGet("fb_flowerLifetime", out float lifetime))
            _flowerLifetime = lifetime;
        if (Configuration.TryGet("fb_fadeOutDuration", out float fadeDur))
            _fadeOutDuration = fadeDur;
        if (Configuration.TryGet("fb_showStem", out bool stem))
            _showStem = stem;
        if (Configuration.TryGet("fb_sizeVariation", out bool sizeVar))
            _sizeVariation = sizeVar;
        if (Configuration.TryGet("fb_sizeVariationAmount", out float sizeVarAmt))
            _sizeVariationAmount = sizeVarAmt;
        if (Configuration.TryGet("fb_continuousSpawn", out bool continuous))
            _continuousSpawn = continuous;
        if (Configuration.TryGet("fb_spawnRate", out float rate))
            _spawnRate = rate;
        if (Configuration.TryGet("fb_leftClickEnabled", out bool leftEnabled))
            _leftClickEnabled = leftEnabled;
        if (Configuration.TryGet("fb_rightClickEnabled", out bool rightEnabled))
            _rightClickEnabled = rightEnabled;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        float totalTime = gameTime.TotalSeconds;

        // Update existing flowers
        UpdateFlowers(deltaTime);

        // Handle continuous spawn
        if (_continuousSpawn)
        {
            _spawnAccumulator += deltaTime;
            if (_spawnAccumulator >= _spawnRate)
            {
                SpawnFlower(mouseState.Position, totalTime);
                _spawnAccumulator = 0f;
            }
        }

        // Handle left click trigger
        bool leftPressed = mouseState.IsButtonPressed(MouseButtons.Left);
        if (_leftClickEnabled && leftPressed && !_wasLeftPressed)
        {
            SpawnFlower(mouseState.Position, totalTime);
        }
        _wasLeftPressed = leftPressed;

        // Handle right click trigger
        bool rightPressed = mouseState.IsButtonPressed(MouseButtons.Right);
        if (_rightClickEnabled && rightPressed && !_wasRightPressed)
        {
            SpawnFlower(mouseState.Position, totalTime);
        }
        _wasRightPressed = rightPressed;
    }

    private void UpdateFlowers(float deltaTime)
    {
        _activeFlowerCount = 0;
        for (int i = 0; i < MaxFlowers; i++)
        {
            if (_flowers[i].Lifetime > 0)
            {
                ref var f = ref _flowers[i];

                // Age flower
                f.Lifetime -= deltaTime;

                if (f.Lifetime > 0)
                {
                    // Update bloom progress
                    float age = f.MaxLifetime - f.Lifetime;
                    if (age < f.TotalBloomTime)
                    {
                        f.BloomProgress = age / f.TotalBloomTime;
                        // Ease out cubic for smooth bloom
                        f.BloomProgress = 1f - MathF.Pow(1f - f.BloomProgress, 3f);
                    }
                    else
                    {
                        f.BloomProgress = 1f;
                    }

                    _activeFlowerCount++;
                }
            }
        }
    }

    private void SpawnFlower(Vector2 position, float time)
    {
        ref var f = ref _flowers[_nextFlowerIndex];
        _nextFlowerIndex = (_nextFlowerIndex + 1) % MaxFlowers;

        f.Position = position;
        f.Lifetime = _flowerLifetime;
        f.MaxLifetime = _flowerLifetime;
        f.BirthTime = time;
        f.BloomProgress = 0f;
        f.TotalBloomTime = _bloomDuration;
        f.FadeOutTime = _fadeOutDuration;

        // Randomize size if variation enabled
        if (_sizeVariation)
        {
            float variation = 1f - _sizeVariationAmount + Random.Shared.NextSingle() * _sizeVariationAmount * 2f;
            f.Size = _flowerSize * variation;
        }
        else
        {
            f.Size = _flowerSize;
        }

        // Set petal count with slight randomization
        f.PetalCount = _petalCount + (Random.Shared.Next(0, 3) - 1);
        f.PetalCount = Math.Clamp(f.PetalCount, 3, 12);

        // Set flower type
        f.FlowerType = _flowerType;

        // Random rotation
        f.RotationAngle = Random.Shared.NextSingle() * MathF.PI * 2f;

        // Stem visibility
        f.HasStem = _showStem ? 1 : 0;

        // Flower-type specific parameters
        switch (_flowerType)
        {
            case 0: // Rose
                f.PetalCurvature = 0.6f + Random.Shared.NextSingle() * 0.2f;
                f.PetalWidth = 0.7f + Random.Shared.NextSingle() * 0.2f;
                break;
            case 1: // Daisy
                f.PetalCurvature = 0.3f + Random.Shared.NextSingle() * 0.1f;
                f.PetalWidth = 0.4f + Random.Shared.NextSingle() * 0.1f;
                break;
            case 2: // Lotus
                f.PetalCurvature = 0.5f + Random.Shared.NextSingle() * 0.2f;
                f.PetalWidth = 0.8f + Random.Shared.NextSingle() * 0.2f;
                break;
            case 3: // Cherry Blossom
                f.PetalCurvature = 0.4f + Random.Shared.NextSingle() * 0.1f;
                f.PetalWidth = 0.5f + Random.Shared.NextSingle() * 0.2f;
                break;
        }

        // Get colors based on palette
        GetFlowerColors(out f.PrimaryColor, out f.SecondaryColor);

        f.Padding = 0f;
    }

    private void GetFlowerColors(out Vector4 primary, out Vector4 secondary)
    {
        // Color palettes
        switch (_colorPalette)
        {
            case 0: // Spring (pinks, yellows, light purples)
                primary = _flowerType switch
                {
                    0 => new Vector4(1f, 0.4f, 0.6f, 1f), // Rose - Pink
                    1 => new Vector4(1f, 1f, 0.3f, 1f),   // Daisy - Yellow
                    2 => new Vector4(0.9f, 0.5f, 0.9f, 1f), // Lotus - Light purple
                    _ => new Vector4(1f, 0.7f, 0.8f, 1f)  // Cherry - Light pink
                };
                secondary = new Vector4(1f, 0.9f, 0.2f, 1f); // Yellow center
                break;

            case 1: // Summer (bright reds, oranges, blues)
                primary = _flowerType switch
                {
                    0 => new Vector4(1f, 0.1f, 0.2f, 1f), // Rose - Red
                    1 => new Vector4(1f, 0.6f, 0f, 1f),   // Daisy - Orange
                    2 => new Vector4(0.2f, 0.5f, 1f, 1f), // Lotus - Blue
                    _ => new Vector4(1f, 0.3f, 0.4f, 1f)  // Cherry - Coral
                };
                secondary = new Vector4(1f, 0.8f, 0f, 1f); // Golden center
                break;

            case 2: // Tropical (vibrant purples, magentas, oranges)
                primary = _flowerType switch
                {
                    0 => new Vector4(1f, 0f, 0.5f, 1f),   // Rose - Magenta
                    1 => new Vector4(1f, 0.4f, 0f, 1f),   // Daisy - Bright orange
                    2 => new Vector4(0.6f, 0.2f, 1f, 1f), // Lotus - Purple
                    _ => new Vector4(1f, 0.2f, 0.6f, 1f)  // Cherry - Hot pink
                };
                secondary = new Vector4(1f, 1f, 0f, 1f); // Bright yellow center
                break;

            case 3: // Pastel (soft colors)
                primary = _flowerType switch
                {
                    0 => new Vector4(1f, 0.7f, 0.8f, 1f), // Rose - Pastel pink
                    1 => new Vector4(1f, 1f, 0.7f, 1f),   // Daisy - Pastel yellow
                    2 => new Vector4(0.8f, 0.7f, 1f, 1f), // Lotus - Pastel purple
                    _ => new Vector4(1f, 0.85f, 0.9f, 1f) // Cherry - Pastel pink
                };
                secondary = new Vector4(1f, 0.95f, 0.7f, 1f); // Pale yellow center
                break;

            default:
                primary = new Vector4(1f, 0.5f, 0.7f, 1f);
                secondary = new Vector4(1f, 0.9f, 0.2f, 1f);
                break;
        }

        // Add slight random variation to colors
        float variation = 0.9f + Random.Shared.NextSingle() * 0.2f;
        primary.X *= variation;
        primary.Y *= variation;
        primary.Z *= variation;
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeFlowerCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU flower buffer - only include alive flowers
        int gpuIndex = 0;
        for (int i = 0; i < MaxFlowers && gpuIndex < MaxFlowers; i++)
        {
            if (_flowers[i].Lifetime > 0)
            {
                _gpuFlowers[gpuIndex++] = _flowers[i];
            }
        }

        // Fill remaining with zeroed flowers
        for (int i = gpuIndex; i < MaxFlowers; i++)
        {
            _gpuFlowers[i] = default;
        }

        // Update flower buffer
        context.UpdateBuffer(_flowerBuffer!, (ReadOnlySpan<FlowerInstance>)_gpuFlowers.AsSpan());

        // Update constant buffer
        var constants = new FrameConstants
        {
            ViewportSize = context.ViewportSize,
            Time = currentTime,
            HdrMultiplier = context.HdrPeakBrightness,
            Padding = Vector4.Zero
        };
        context.UpdateBuffer(_constantBuffer!, constants);

        // Set up rendering state
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _flowerBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _flowerBuffer!);
        context.SetBlendState(BlendMode.Alpha);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw instanced flowers (6 vertices per quad, one instance per flower)
        context.DrawInstanced(6, MaxFlowers, 0, 0);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _flowerBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.FlowerBloom.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
