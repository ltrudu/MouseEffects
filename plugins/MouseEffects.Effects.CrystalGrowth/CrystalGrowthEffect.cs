using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

using CoreMouseButtons = MouseEffects.Core.Input.MouseButtons;

namespace MouseEffects.Effects.CrystalGrowth;

public sealed class CrystalGrowthEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "crystal-growth",
        Name = "Crystal Growth",
        Description = "Ice/crystal structures that grow from mouse clicks with geometric, angular branches",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Artistic
    };

    public override EffectMetadata Metadata => _metadata;

    // GPU Structures (16-byte aligned)
    [StructLayout(LayoutKind.Sequential, Size = 48)]
    private struct CrystalConstants
    {
        public Vector2 ViewportSize;      // 8 bytes
        public float Time;                // 4 bytes
        public float BranchThickness;     // 4 bytes = 16
        public float SparkleIntensity;    // 4 bytes
        public float GlowIntensity;       // 4 bytes
        public float HdrMultiplier;       // 4 bytes
        public float Padding;             // 4 bytes = 32
        public Vector4 Padding2;          // 16 bytes = 48
    }

    [StructLayout(LayoutKind.Sequential, Size = 80)]
    private struct CrystalBranch
    {
        public Vector2 Start;             // 8 bytes - Start position
        public Vector2 End;               // 8 bytes = 16 - End position (at full growth)
        public float Progress;            // 4 bytes - Growth progress (0->1)
        public float Lifetime;            // 4 bytes - Current lifetime
        public float MaxLifetime;         // 4 bytes - Total lifetime
        public float Angle;               // 4 bytes = 32 - Branch angle (radians)
        public Vector4 Color;             // 16 bytes = 48
        public float MaxLength;           // 4 bytes - Maximum branch length
        public float Generation;          // 4 bytes - Branch generation (0=main, 1=sub, etc)
        public float SparklePhase;        // 4 bytes - Random phase for sparkle animation
        public float RotationAngle;       // 4 bytes = 64 - Current rotation angle
        public Vector2 CrystalOrigin;     // 8 bytes - Origin point for rotation
        public float RotationSpeed;       // 4 bytes - Rotation speed (radians/sec)
        public float Padding;             // 4 bytes = 80
    }

    // Constants
    private const int MaxBranches = 1024;
    private const float BranchAngle = 1.0472f; // 60 degrees in radians (hexagonal symmetry)

    // GPU Resources
    private IBuffer? _constantBuffer;
    private IBuffer? _branchBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    // Branch management (CPU side)
    private readonly CrystalBranch[] _branches = new CrystalBranch[MaxBranches];
    private readonly CrystalBranch[] _gpuBranches = new CrystalBranch[MaxBranches];
    private int _nextBranchIndex;
    private int _activeBranchCount;

    // Mouse tracking
    private Vector2 _lastMousePos;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;

    // Configuration fields (cg_ prefix for CrystalGrowth)
    private int _crystalsPerClick = 3;
    private float _growthSpeed = 120f;
    private float _maxSize = 100f;
    private float _branchProbability = 0.7f;
    private int _colorPreset = 0; // 0=IceBlue, 1=Amethyst, 2=Emerald, 3=Diamond, 4=Custom, 5=Rainbow
    private Vector4 _customColor = new(0.53f, 0.81f, 0.92f, 1f); // Light sky blue
    private float _sparkleIntensity = 1.2f;
    private float _lifetime = 2.5f;
    private float _branchThickness = 1.5f;
    private float _glowIntensity = 1.0f;
    private int _maxGenerations = 3;

    // Rainbow settings
    private float _rainbowSpeed = 1.0f;
    private bool _rainbowMultiColor = false;
    private float _rainbowHueOffset = 0f; // Tracks current rainbow position

    // Rotation settings
    private bool _rotationEnabled = false;
    private int _rotationDirection = 0; // 0=Clockwise, 1=Counter-Clockwise, 2=Random
    private bool _rotationRandomSpeed = false;
    private float _rotationSpeed = 1.0f;
    private float _rotationMinSpeed = 0.5f;
    private float _rotationMaxSpeed = 2.0f;

    // Trigger settings
    private bool _leftClickEnabled = true;
    private bool _rightClickEnabled = true;

    // Public properties for UI binding
    public int CrystalsPerClick { get => _crystalsPerClick; set => _crystalsPerClick = value; }
    public float GrowthSpeed { get => _growthSpeed; set => _growthSpeed = value; }
    public float MaxSize { get => _maxSize; set => _maxSize = value; }
    public float BranchProbability { get => _branchProbability; set => _branchProbability = value; }
    public int ColorPreset { get => _colorPreset; set => _colorPreset = value; }
    public Vector4 CustomColor { get => _customColor; set => _customColor = value; }
    public float SparkleIntensity { get => _sparkleIntensity; set => _sparkleIntensity = value; }
    public float Lifetime { get => _lifetime; set => _lifetime = value; }
    public float BranchThickness { get => _branchThickness; set => _branchThickness = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public int MaxGenerations { get => _maxGenerations; set => _maxGenerations = value; }
    public bool LeftClickEnabled { get => _leftClickEnabled; set => _leftClickEnabled = value; }
    public bool RightClickEnabled { get => _rightClickEnabled; set => _rightClickEnabled = value; }
    public float RainbowSpeed { get => _rainbowSpeed; set => _rainbowSpeed = value; }
    public bool RainbowMultiColor { get => _rainbowMultiColor; set => _rainbowMultiColor = value; }
    public bool RotationEnabled { get => _rotationEnabled; set => _rotationEnabled = value; }
    public int RotationDirection { get => _rotationDirection; set => _rotationDirection = value; }
    public bool RotationRandomSpeed { get => _rotationRandomSpeed; set => _rotationRandomSpeed = value; }
    public float RotationSpeed { get => _rotationSpeed; set => _rotationSpeed = value; }
    public float RotationMinSpeed { get => _rotationMinSpeed; set => _rotationMinSpeed = value; }
    public float RotationMaxSpeed { get => _rotationMaxSpeed; set => _rotationMaxSpeed = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        // Load and compile shader
        string shaderSource = LoadEmbeddedShader("CrystalGrowthShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        // Create constant buffer
        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<CrystalConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        // Create branch structured buffer
        _branchBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<CrystalBranch>() * MaxBranches,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<CrystalBranch>()
        });

        _lastMousePos = new Vector2(context.ViewportSize.X / 2, context.ViewportSize.Y / 2);
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("cg_crystalsPerClick", out int count))
            _crystalsPerClick = count;
        if (Configuration.TryGet("cg_growthSpeed", out float speed))
            _growthSpeed = speed;
        if (Configuration.TryGet("cg_maxSize", out float maxSize))
            _maxSize = maxSize;
        if (Configuration.TryGet("cg_branchProbability", out float branchProb))
            _branchProbability = branchProb;
        if (Configuration.TryGet("cg_colorPreset", out int preset))
            _colorPreset = preset;
        if (Configuration.TryGet("cg_customColor", out Vector4 color))
            _customColor = color;
        if (Configuration.TryGet("cg_sparkleIntensity", out float sparkle))
            _sparkleIntensity = sparkle;
        if (Configuration.TryGet("cg_lifetime", out float lifetime))
            _lifetime = lifetime;
        if (Configuration.TryGet("cg_branchThickness", out float thickness))
            _branchThickness = thickness;
        if (Configuration.TryGet("cg_glowIntensity", out float glow))
            _glowIntensity = glow;
        if (Configuration.TryGet("cg_maxGenerations", out int maxGen))
            _maxGenerations = maxGen;
        if (Configuration.TryGet("cg_leftClickEnabled", out bool leftEnabled))
            _leftClickEnabled = leftEnabled;
        if (Configuration.TryGet("cg_rightClickEnabled", out bool rightEnabled))
            _rightClickEnabled = rightEnabled;
        if (Configuration.TryGet("cg_rainbowSpeed", out float rainbowSpeed))
            _rainbowSpeed = rainbowSpeed;
        if (Configuration.TryGet("cg_rainbowMultiColor", out bool rainbowMulti))
            _rainbowMultiColor = rainbowMulti;
        if (Configuration.TryGet("cg_rotationEnabled", out bool rotEnabled))
            _rotationEnabled = rotEnabled;
        if (Configuration.TryGet("cg_rotationDirection", out int rotDir))
            _rotationDirection = rotDir;
        if (Configuration.TryGet("cg_rotationRandomSpeed", out bool rotRandom))
            _rotationRandomSpeed = rotRandom;
        if (Configuration.TryGet("cg_rotationSpeed", out float rotSpeed))
            _rotationSpeed = rotSpeed;
        if (Configuration.TryGet("cg_rotationMinSpeed", out float rotMinSpeed))
            _rotationMinSpeed = rotMinSpeed;
        if (Configuration.TryGet("cg_rotationMaxSpeed", out float rotMaxSpeed))
            _rotationMaxSpeed = rotMaxSpeed;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;

        // Advance rainbow hue offset
        if (_colorPreset == 5) // Rainbow mode
        {
            _rainbowHueOffset += deltaTime * _rainbowSpeed;
            if (_rainbowHueOffset > 1f) _rainbowHueOffset -= 1f;
        }

        // Handle left click trigger
        bool leftPressed = mouseState.IsButtonPressed(CoreMouseButtons.Left);
        if (_leftClickEnabled && leftPressed && !_wasLeftPressed)
        {
            SpawnCrystal(mouseState.Position);
        }
        _wasLeftPressed = leftPressed;

        // Handle right click trigger
        bool rightPressed = mouseState.IsButtonPressed(CoreMouseButtons.Right);
        if (_rightClickEnabled && rightPressed && !_wasRightPressed)
        {
            SpawnCrystal(mouseState.Position);
        }
        _wasRightPressed = rightPressed;

        // Update last mouse position
        _lastMousePos = mouseState.Position;

        // Update existing branches
        UpdateBranches(deltaTime);
    }

    private void SpawnCrystal(Vector2 origin)
    {
        // Calculate rotation speed for this crystal
        float crystalRotationSpeed = 0f;
        if (_rotationEnabled)
        {
            if (_rotationRandomSpeed)
            {
                crystalRotationSpeed = _rotationMinSpeed + Random.Shared.NextSingle() * (_rotationMaxSpeed - _rotationMinSpeed);
            }
            else
            {
                crystalRotationSpeed = _rotationSpeed;
            }

            // Apply direction: 0=Clockwise (negative), 1=Counter-Clockwise (positive), 2=Random
            float direction = _rotationDirection switch
            {
                0 => -1f,  // Clockwise
                1 => 1f,   // Counter-Clockwise
                _ => Random.Shared.NextSingle() > 0.5f ? 1f : -1f  // Random
            };
            crystalRotationSpeed *= direction;
        }

        // Spawn main branches (typically 6 for hexagonal symmetry)
        for (int i = 0; i < _crystalsPerClick; i++)
        {
            if (_activeBranchCount >= MaxBranches)
                break;

            // Calculate evenly distributed angles
            float angle = (MathF.PI * 2f / _crystalsPerClick) * i + (Random.Shared.NextSingle() - 0.5f) * 0.2f;

            // For rainbow multi-color mode, each main branch gets a different rainbow color
            Vector4 branchColor;
            if (_colorPreset == 5 && _rainbowMultiColor)
            {
                // Distribute colors across the rainbow for each branch
                float hueOffset = (float)i / _crystalsPerClick;
                branchColor = GetRainbowColor((_rainbowHueOffset + hueOffset) % 1f);
            }
            else
            {
                branchColor = GetCrystalColor();
            }

            SpawnBranch(origin, angle, branchColor, 0, _rainbowHueOffset + (float)i / _crystalsPerClick, origin, crystalRotationSpeed);
        }
    }

    private void SpawnBranch(Vector2 start, float angle, Vector4 color, int generation, float baseHue = 0f, Vector2 crystalOrigin = default, float rotationSpeed = 0f)
    {
        if (generation >= _maxGenerations || _activeBranchCount >= MaxBranches)
            return;

        // Calculate length with variation (use exponential decay to avoid negative lengths)
        float lengthVariation = 0.7f + Random.Shared.NextSingle() * 0.6f;
        float generationScale = MathF.Pow(0.65f, generation); // 1.0, 0.65, 0.42, 0.27, 0.18, 0.12...
        float branchLength = _maxSize * lengthVariation * generationScale;

        // Calculate end position
        Vector2 direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        Vector2 end = start + direction * branchLength;

        // For multi-color rainbow mode, shift color slightly for each generation
        Vector4 branchColor = color;
        if (_colorPreset == 5 && _rainbowMultiColor && generation > 0)
        {
            // Each generation shifts the hue slightly for a fading rainbow effect
            float hueShift = generation * 0.1f;
            branchColor = GetRainbowColor((baseHue + hueShift) % 1f);
        }

        // Create branch
        ref CrystalBranch branch = ref _branches[_nextBranchIndex];
        branch.Start = start;
        branch.End = end;
        branch.Progress = 0f;
        branch.Lifetime = _lifetime;
        branch.MaxLifetime = _lifetime;
        branch.Angle = angle;
        branch.Color = branchColor;
        branch.MaxLength = branchLength;
        branch.Generation = generation;
        branch.SparklePhase = Random.Shared.NextSingle() * MathF.PI * 2f;
        branch.RotationAngle = 0f;
        branch.CrystalOrigin = crystalOrigin;
        branch.RotationSpeed = rotationSpeed;
        branch.Padding = 0f;

        _nextBranchIndex = (_nextBranchIndex + 1) % MaxBranches;
        _activeBranchCount++;

        // Spawn sub-branches with probability
        if (Random.Shared.NextSingle() < _branchProbability && generation < _maxGenerations - 1)
        {
            // Calculate branch point (30-70% along the main branch)
            float branchPoint = 0.3f + Random.Shared.NextSingle() * 0.4f;
            Vector2 subBranchStart = Vector2.Lerp(start, end, branchPoint);

            // Create two sub-branches at 60-degree angles (hexagonal symmetry)
            float subAngle1 = angle + BranchAngle;
            float subAngle2 = angle - BranchAngle;

            SpawnBranch(subBranchStart, subAngle1, branchColor, generation + 1, baseHue, crystalOrigin, rotationSpeed);
            SpawnBranch(subBranchStart, subAngle2, branchColor, generation + 1, baseHue, crystalOrigin, rotationSpeed);
        }
    }

    private void UpdateBranches(float deltaTime)
    {
        _activeBranchCount = 0;

        for (int i = 0; i < MaxBranches; i++)
        {
            if (_branches[i].Lifetime > 0)
            {
                // Grow the branch
                if (_branches[i].Progress < 1f)
                {
                    float growthThisFrame = (_growthSpeed / _branches[i].MaxLength) * deltaTime;
                    _branches[i].Progress = Math.Min(1f, _branches[i].Progress + growthThisFrame);
                }

                // Age the branch (start aging after full growth)
                if (_branches[i].Progress >= 1f)
                {
                    _branches[i].Lifetime -= deltaTime;
                }

                // Update rotation angle
                if (_branches[i].RotationSpeed != 0f)
                {
                    _branches[i].RotationAngle += _branches[i].RotationSpeed * deltaTime;
                }

                if (_branches[i].Lifetime > 0)
                    _activeBranchCount++;
            }
        }
    }

    private Vector4 GetCrystalColor()
    {
        return _colorPreset switch
        {
            0 => new Vector4(0.53f, 0.81f, 0.92f, 1f),  // Ice Blue (Light Sky Blue)
            1 => new Vector4(0.6f, 0.4f, 0.8f, 1f),     // Amethyst
            2 => new Vector4(0.31f, 0.78f, 0.47f, 1f),  // Emerald
            3 => new Vector4(0.9f, 0.95f, 1f, 1f),      // Diamond (near white with blue tint)
            4 => _customColor,                          // Custom
            5 => GetRainbowColor(_rainbowHueOffset),    // Rainbow (single color per crystal)
            _ => new Vector4(0.53f, 0.81f, 0.92f, 1f)
        };
    }

    private Vector4 GetRainbowColor(float hue)
    {
        // HSV to RGB conversion (S=1, V=1 for vibrant colors)
        float h = hue * 6f;
        int i = (int)MathF.Floor(h);
        float f = h - i;
        float q = 1f - f;
        float t = f;

        Vector3 rgb = (i % 6) switch
        {
            0 => new Vector3(1f, t, 0f),
            1 => new Vector3(q, 1f, 0f),
            2 => new Vector3(0f, 1f, t),
            3 => new Vector3(0f, q, 1f),
            4 => new Vector3(t, 0f, 1f),
            _ => new Vector3(1f, 0f, q)
        };

        return new Vector4(rgb.X, rgb.Y, rgb.Z, 1f);
    }

    private static Vector2 RotatePoint(Vector2 point, Vector2 origin, float angle)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);
        Vector2 translated = point - origin;
        return new Vector2(
            translated.X * cos - translated.Y * sin + origin.X,
            translated.X * sin + translated.Y * cos + origin.Y
        );
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeBranchCount == 0) return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        // Build GPU branch buffer with rotation applied
        int gpuIndex = 0;
        for (int i = 0; i < MaxBranches && gpuIndex < MaxBranches; i++)
        {
            if (_branches[i].Lifetime > 0)
            {
                var branch = _branches[i];

                // Apply rotation if enabled
                if (branch.RotationAngle != 0f)
                {
                    branch.Start = RotatePoint(branch.Start, branch.CrystalOrigin, branch.RotationAngle);
                    branch.End = RotatePoint(branch.End, branch.CrystalOrigin, branch.RotationAngle);
                }

                _gpuBranches[gpuIndex++] = branch;
            }
        }

        // Fill remaining with zeroed branches
        for (int i = gpuIndex; i < MaxBranches; i++)
        {
            _gpuBranches[i] = default;
        }

        // Update branch buffer
        context.UpdateBuffer(_branchBuffer!, (ReadOnlySpan<CrystalBranch>)_gpuBranches.AsSpan());

        // Update constant buffer
        var constants = new CrystalConstants
        {
            ViewportSize = context.ViewportSize,
            Time = currentTime,
            BranchThickness = _branchThickness,
            SparkleIntensity = _sparkleIntensity,
            GlowIntensity = _glowIntensity,
            HdrMultiplier = context.HdrPeakBrightness,
            Padding = 0f,
            Padding2 = Vector4.Zero
        };
        context.UpdateBuffer(_constantBuffer!, constants);

        // Set up rendering state
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _branchBuffer!);
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
        _branchBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.CrystalGrowth.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
