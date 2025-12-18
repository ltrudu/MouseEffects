using System.IO;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

namespace MouseEffects.Effects.Runes;

public sealed class RunesEffect : EffectBase
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "runes",
        Name = "Runes",
        Description = "Floating magical runes and symbols that appear and fade around the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Digital
    };

    public override EffectMetadata Metadata => _metadata;

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    private struct FrameConstants
    {
        public Vector2 ViewportSize;
        public float Time;
        public float HdrMultiplier;
        public Vector4 Padding;
    }

    [StructLayout(LayoutKind.Sequential, Size = 80)]
    private struct RuneInstance
    {
        public Vector2 Position;
        public Vector2 FloatOffset;
        public Vector4 Color;
        public float Size;
        public float Rotation;
        public float RotationSpeed;
        public float Lifetime;
        public float MaxLifetime;
        public int RuneType;
        public float GlowIntensity;
        public float FloatPhase;
        public float BirthTime;
        public float FloatDistance;
        public float Padding1;
        public float Padding2;
    }

    private const int MaxRunes = 500;

    private IBuffer? _constantBuffer;
    private IBuffer? _runeBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    private readonly RuneInstance[] _runes = new RuneInstance[MaxRunes];
    private readonly RuneInstance[] _gpuRunes = new RuneInstance[MaxRunes];
    private int _nextRuneIndex;
    private int _activeRuneCount;

    private Vector2 _lastMousePos;
    private float _accumulatedDistance;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;
    private float _rainbowHue;

    private int _runeCount = 3;
    private float _runeSize = 40f;
    private float _runeLifetime = 3.0f;
    private float _glowIntensity = 1.5f;
    private float _rotationSpeed = 0.5f;
    private float _floatDistance = 20f;
    private bool _rainbowMode = false;
    private float _rainbowSpeed = 0.3f;
    private Vector4 _fixedColor = new(1f, 0.84f, 0f, 1f);
    private bool _mouseMoveEnabled = true;
    private float _moveDistanceThreshold = 60f;
    private bool _leftClickEnabled = true;
    private int _leftClickBurstCount = 5;
    private bool _rightClickEnabled = true;
    private int _rightClickBurstCount = 8;

    public int RuneCount { get => _runeCount; set => _runeCount = value; }
    public float RuneSize { get => _runeSize; set => _runeSize = value; }
    public float RuneLifetime { get => _runeLifetime; set => _runeLifetime = value; }
    public float GlowIntensity { get => _glowIntensity; set => _glowIntensity = value; }
    public float RotationSpeed { get => _rotationSpeed; set => _rotationSpeed = value; }
    public float FloatDistance { get => _floatDistance; set => _floatDistance = value; }
    public bool RainbowMode { get => _rainbowMode; set => _rainbowMode = value; }
    public float RainbowSpeed { get => _rainbowSpeed; set => _rainbowSpeed = value; }
    public Vector4 FixedColor { get => _fixedColor; set => _fixedColor = value; }
    public bool MouseMoveEnabled { get => _mouseMoveEnabled; set => _mouseMoveEnabled = value; }
    public float MoveDistanceThreshold { get => _moveDistanceThreshold; set => _moveDistanceThreshold = value; }
    public bool LeftClickEnabled { get => _leftClickEnabled; set => _leftClickEnabled = value; }
    public int LeftClickBurstCount { get => _leftClickBurstCount; set => _leftClickBurstCount = value; }
    public bool RightClickEnabled { get => _rightClickEnabled; set => _rightClickEnabled = value; }
    public int RightClickBurstCount { get => _rightClickBurstCount; set => _rightClickBurstCount = value; }

    protected override void OnInitialize(IRenderContext context)
    {
        string shaderSource = LoadEmbeddedShader("RunesShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        _constantBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<FrameConstants>(),
            Type = BufferType.Constant,
            Dynamic = true
        });

        _runeBuffer = context.CreateBuffer(new BufferDescription
        {
            Size = Marshal.SizeOf<RuneInstance>() * MaxRunes,
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<RuneInstance>()
        });
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("rn_runeCount", out int count))
            _runeCount = count;
        if (Configuration.TryGet("rn_runeSize", out float size))
            _runeSize = size;
        if (Configuration.TryGet("rn_lifetime", out float lifetime))
            _runeLifetime = lifetime;
        if (Configuration.TryGet("rn_glowIntensity", out float glow))
            _glowIntensity = glow;
        if (Configuration.TryGet("rn_rotationSpeed", out float rotSpeed))
            _rotationSpeed = rotSpeed;
        if (Configuration.TryGet("rn_floatDistance", out float floatDist))
            _floatDistance = floatDist;
        if (Configuration.TryGet("rn_rainbowMode", out bool rainbow))
            _rainbowMode = rainbow;
        if (Configuration.TryGet("rn_rainbowSpeed", out float rainbowSpd))
            _rainbowSpeed = rainbowSpd;
        if (Configuration.TryGet("rn_fixedColor", out Vector4 color))
            _fixedColor = color;
        if (Configuration.TryGet("rn_mouseMoveEnabled", out bool moveEnabled))
            _mouseMoveEnabled = moveEnabled;
        if (Configuration.TryGet("rn_moveDistanceThreshold", out float moveDist))
            _moveDistanceThreshold = moveDist;
        if (Configuration.TryGet("rn_leftClickEnabled", out bool leftEnabled))
            _leftClickEnabled = leftEnabled;
        if (Configuration.TryGet("rn_leftClickBurstCount", out int leftCount))
            _leftClickBurstCount = leftCount;
        if (Configuration.TryGet("rn_rightClickEnabled", out bool rightEnabled))
            _rightClickEnabled = rightEnabled;
        if (Configuration.TryGet("rn_rightClickBurstCount", out int rightCount))
            _rightClickBurstCount = rightCount;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float deltaTime = gameTime.DeltaSeconds;
        float totalTime = gameTime.TotalSeconds;

        if (_rainbowMode)
        {
            _rainbowHue += _rainbowSpeed * deltaTime;
            if (_rainbowHue > 1f) _rainbowHue -= 1f;
        }

        UpdateRunes(deltaTime, totalTime);

        float distanceFromLast = Vector2.Distance(mouseState.Position, _lastMousePos);

        if (_mouseMoveEnabled && distanceFromLast > 0.1f)
        {
            _accumulatedDistance += distanceFromLast;
            if (_accumulatedDistance >= _moveDistanceThreshold)
            {
                SpawnRunes(mouseState.Position, _runeCount, totalTime);
                _accumulatedDistance = 0f;
            }
        }

        bool leftPressed = mouseState.IsButtonPressed(MouseButtons.Left);
        if (_leftClickEnabled && leftPressed && !_wasLeftPressed)
        {
            SpawnRunes(mouseState.Position, _leftClickBurstCount, totalTime);
        }
        _wasLeftPressed = leftPressed;

        bool rightPressed = mouseState.IsButtonPressed(MouseButtons.Right);
        if (_rightClickEnabled && rightPressed && !_wasRightPressed)
        {
            SpawnRunes(mouseState.Position, _rightClickBurstCount, totalTime);
        }
        _wasRightPressed = rightPressed;

        _lastMousePos = mouseState.Position;
    }

    private void UpdateRunes(float deltaTime, float totalTime)
    {
        _activeRuneCount = 0;
        for (int i = 0; i < MaxRunes; i++)
        {
            if (_runes[i].Lifetime > 0)
            {
                ref var r = ref _runes[i];
                r.Lifetime -= deltaTime;

                if (r.Lifetime > 0)
                {
                    r.FloatPhase += deltaTime;
                    r.Rotation += r.RotationSpeed * deltaTime;
                    _activeRuneCount++;
                }
            }
        }
    }

    private void SpawnRunes(Vector2 position, int count, float time)
    {
        for (int i = 0; i < count; i++)
        {
            ref var r = ref _runes[_nextRuneIndex];
            _nextRuneIndex = (_nextRuneIndex + 1) % MaxRunes;

            float spreadRadius = 20f;
            float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
            float radius = Random.Shared.NextSingle() * spreadRadius;
            Vector2 offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;

            r.Position = position + offset;
            r.FloatOffset = new Vector2(
                (Random.Shared.NextSingle() - 0.5f) * 2f,
                (Random.Shared.NextSingle() - 0.5f) * 2f
            );
            r.Size = _runeSize * (0.7f + Random.Shared.NextSingle() * 0.6f);
            r.Rotation = Random.Shared.NextSingle() * MathF.PI * 2f;
            r.RotationSpeed = _rotationSpeed * (Random.Shared.NextSingle() - 0.5f) * 2f;
            r.Lifetime = _runeLifetime * (0.8f + Random.Shared.NextSingle() * 0.4f);
            r.MaxLifetime = r.Lifetime;
            r.RuneType = Random.Shared.Next(0, 10);
            r.GlowIntensity = _glowIntensity * (0.8f + Random.Shared.NextSingle() * 0.4f);
            r.FloatPhase = Random.Shared.NextSingle() * MathF.PI * 2f;
            r.BirthTime = time;
            r.FloatDistance = _floatDistance * (0.7f + Random.Shared.NextSingle() * 0.6f);
            r.Color = GetRuneColor();
        }
    }

    private Vector4 GetRuneColor()
    {
        if (_rainbowMode)
        {
            float hue = _rainbowHue + Random.Shared.NextSingle() * 0.2f;
            return HueToRgb(hue);
        }
        else
        {
            Vector4 color = _fixedColor;
            float brightness = 0.8f + Random.Shared.NextSingle() * 0.4f;
            color.X *= brightness;
            color.Y *= brightness;
            color.Z *= brightness;
            return color;
        }
    }

    private static Vector4 HueToRgb(float hue)
    {
        hue -= MathF.Floor(hue);
        float h = hue * 6f;
        float x = 1f - MathF.Abs(h % 2f - 1f);

        Vector3 rgb = (int)h switch
        {
            0 => new Vector3(1f, x, 0f),
            1 => new Vector3(x, 1f, 0f),
            2 => new Vector3(0f, 1f, x),
            3 => new Vector3(0f, x, 1f),
            4 => new Vector3(x, 0f, 1f),
            _ => new Vector3(1f, 0f, x),
        };

        return new Vector4(rgb.X, rgb.Y, rgb.Z, 1f);
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_activeRuneCount == 0)
            return;

        float currentTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        int gpuIndex = 0;
        for (int i = 0; i < MaxRunes && gpuIndex < MaxRunes; i++)
        {
            if (_runes[i].Lifetime > 0)
            {
                _gpuRunes[gpuIndex++] = _runes[i];
            }
        }

        for (int i = gpuIndex; i < MaxRunes; i++)
        {
            _gpuRunes[i] = default;
        }

        context.UpdateBuffer(_runeBuffer!, (ReadOnlySpan<RuneInstance>)_gpuRunes.AsSpan());

        var constants = new FrameConstants
        {
            ViewportSize = context.ViewportSize,
            Time = currentTime,
            HdrMultiplier = context.HdrPeakBrightness,
            Padding = Vector4.Zero
        };
        context.UpdateBuffer(_constantBuffer!, constants);

        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _constantBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _constantBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _runeBuffer!);
        context.SetShaderResource(ShaderStage.Pixel, 0, _runeBuffer!);
        context.SetBlendState(BlendMode.Additive);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        context.DrawInstanced(6, MaxRunes, 0, 0);

        context.SetBlendState(BlendMode.Alpha);
    }

    protected override void OnDispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _constantBuffer?.Dispose();
        _runeBuffer?.Dispose();
    }

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"MouseEffects.Effects.Runes.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
