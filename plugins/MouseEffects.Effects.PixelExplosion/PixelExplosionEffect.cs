using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using MouseEffects.Core.Effects;
using MouseEffects.Core.Input;
using MouseEffects.Core.Rendering;
using MouseEffects.Core.Time;

using MouseButtons = MouseEffects.Core.Input.MouseButtons;

namespace MouseEffects.Effects.PixelExplosion;

public sealed class PixelExplosionEffect : EffectBase
{
    private struct Pixel
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector4 Color;
        public float Size;
        public float Life;
        public float MaxLife;
        public float Rotation;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PixelGPU
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector4 Color;
        public float Size;
        public float Life;
        public float MaxLife;
        public float Rotation;
    }

    [StructLayout(LayoutKind.Sequential, Size = 32)]
    private struct FrameData
    {
        public Vector2 ViewportSize;
        public float Time;
        public float Padding1;
        public float Padding2;
        public float Padding3;
        public float Padding4;
        public float Padding5;
    }

    private const int MaxPixelsLimit = 10000;

    private static readonly EffectMetadata _metadata = new()
    {
        Id = "pixel-explosion",
        Name = "Pixel Explosion",
        Description = "Retro 8-bit style pixel explosions on clicks with gravity physics",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Particle
    };

    private IBuffer? _pixelBuffer;
    private IBuffer? _frameDataBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    private readonly Pixel[] _pixels = new Pixel[MaxPixelsLimit];
    private readonly PixelGPU[] _gpuPixels = new PixelGPU[MaxPixelsLimit];
    private int _nextPixel;
    private int _activePixelCount;

    private Vector2 _lastMousePos;
    private bool _wasLeftPressed;
    private bool _wasRightPressed;

    // Configuration values
    private int _maxPixels = 5000;
    private int _pixelCountMin = 30;
    private int _pixelCountMax = 49;
    private float _pixelSizeMin = 3f;
    private float _pixelSizeMax = 8f;
    private float _explosionForce = 400f;
    private float _gravity = 250f;
    private float _lifetime = 2.0f;
    private int _colorPalette = 5; // 0=fire, 1=ice, 2=rainbow, 3=retro, 4=neon, 5=animated rainbow
    private bool _spawnOnLeftClick = true;
    private bool _spawnOnRightClick;
    private bool _spawnOnMouseMove = true;
    private float _mouseThreshold = 150f;
    private float _rainbowSpeed = 3.07f;
    private float _currentTime;
    private float _accumulatedDistance;

    public override EffectMetadata Metadata => _metadata;

    protected override void OnInitialize(IRenderContext context)
    {
        var pixelDesc = new BufferDescription
        {
            Size = MaxPixelsLimit * Marshal.SizeOf<PixelGPU>(),
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<PixelGPU>()
        };
        _pixelBuffer = context.CreateBuffer(pixelDesc, default);

        var frameDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<FrameData>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _frameDataBuffer = context.CreateBuffer(frameDesc, default);

        string shaderSource = LoadEmbeddedShader("PixelExplosionShader.hlsl");
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        for (int i = 0; i < MaxPixelsLimit; i++)
            _pixels[i] = new Pixel { Life = 0f };
    }

    protected override void OnConfigurationChanged()
    {
        if (Configuration.TryGet("maxPixels", out int maxPix))
            _maxPixels = Math.Clamp(maxPix, 100, MaxPixelsLimit);

        if (Configuration.TryGet("pixelCountMin", out int minCount))
            _pixelCountMin = minCount;

        if (Configuration.TryGet("pixelCountMax", out int maxCount))
            _pixelCountMax = maxCount;

        if (Configuration.TryGet("pixelSizeMin", out float minSize))
            _pixelSizeMin = minSize;

        if (Configuration.TryGet("pixelSizeMax", out float maxSize))
            _pixelSizeMax = maxSize;

        if (Configuration.TryGet("explosionForce", out float force))
            _explosionForce = force;

        if (Configuration.TryGet("gravity", out float grav))
            _gravity = grav;

        if (Configuration.TryGet("lifetime", out float life))
            _lifetime = life;

        if (Configuration.TryGet("colorPalette", out int palette))
            _colorPalette = palette;

        if (Configuration.TryGet("spawnOnLeftClick", out bool leftClick))
            _spawnOnLeftClick = leftClick;

        if (Configuration.TryGet("spawnOnRightClick", out bool rightClick))
            _spawnOnRightClick = rightClick;

        if (Configuration.TryGet("spawnOnMouseMove", out bool mouseMove))
            _spawnOnMouseMove = mouseMove;

        if (Configuration.TryGet("mouseThreshold", out float threshold))
            _mouseThreshold = threshold;

        if (Configuration.TryGet("rainbowSpeed", out float rainbowSpd))
            _rainbowSpeed = rainbowSpd;
    }

    protected override void OnUpdate(GameTime gameTime, MouseState mouseState)
    {
        float dt = (float)gameTime.DeltaTime.TotalSeconds;
        _currentTime += dt;

        UpdatePixels(dt);

        bool leftPressed = mouseState.IsButtonPressed(MouseButtons.Left);
        bool rightPressed = mouseState.IsButtonPressed(MouseButtons.Right);

        if (_spawnOnLeftClick && leftPressed && !_wasLeftPressed)
        {
            int pixelCount = Random.Shared.Next(_pixelCountMin, _pixelCountMax + 1);
            SpawnExplosion(mouseState.Position, pixelCount);
        }

        if (_spawnOnRightClick && rightPressed && !_wasRightPressed)
        {
            int pixelCount = Random.Shared.Next(_pixelCountMin, _pixelCountMax + 1);
            SpawnExplosion(mouseState.Position, pixelCount);
        }

        // Mouse move spawning
        if (_spawnOnMouseMove)
        {
            float distance = Vector2.Distance(mouseState.Position, _lastMousePos);
            _accumulatedDistance += distance;

            while (_accumulatedDistance >= _mouseThreshold)
            {
                _accumulatedDistance -= _mouseThreshold;
                int pixelCount = Random.Shared.Next(_pixelCountMin, _pixelCountMax + 1);
                SpawnExplosion(mouseState.Position, pixelCount);
            }
        }

        _wasLeftPressed = leftPressed;
        _wasRightPressed = rightPressed;
        _lastMousePos = mouseState.Position;
    }

    private void UpdatePixels(float dt)
    {
        _activePixelCount = 0;
        for (int i = 0; i < _maxPixels; i++)
        {
            ref Pixel p = ref _pixels[i];
            if (p.Life <= 0f) continue;

            p.Life -= dt;
            if (p.Life <= 0f) continue;

            p.Position += p.Velocity * dt;
            p.Velocity.Y += _gravity * dt;

            // Add slight rotation for visual interest
            p.Rotation += dt * 2f;

            _activePixelCount++;
        }
    }

    private void SpawnExplosion(Vector2 position, int count)
    {
        for (int i = 0; i < count; i++)
        {
            float angle = Random.Shared.NextSingle() * MathF.PI * 2f;
            float speed = _explosionForce * (0.5f + Random.Shared.NextSingle() * 0.5f);
            Vector2 velocity = new(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed);

            float size = _pixelSizeMin + Random.Shared.NextSingle() * (_pixelSizeMax - _pixelSizeMin);
            Vector4 color = GetPixelColor();

            SpawnPixel(position, velocity, color, size);
        }
    }

    private void SpawnPixel(Vector2 position, Vector2 velocity, Vector4 color, float size)
    {
        int startIndex = _nextPixel;
        do
        {
            ref Pixel p = ref _pixels[_nextPixel];
            _nextPixel = (_nextPixel + 1) % _maxPixels;

            if (p.Life <= 0f)
            {
                p.Position = position;
                p.Velocity = velocity;
                p.Color = color;
                p.Size = size;
                p.Life = _lifetime;
                p.MaxLife = _lifetime;
                p.Rotation = Random.Shared.NextSingle() * MathF.PI * 2f;
                break;
            }
        } while (_nextPixel != startIndex);
    }

    private Vector4 GetPixelColor()
    {
        return _colorPalette switch
        {
            0 => GetFireColor(),
            1 => GetIceColor(),
            2 => HueToRgb(Random.Shared.NextSingle()),
            3 => GetRetroColor(),
            4 => GetNeonColor(),
            5 => GetAnimatedRainbowColor(),
            _ => GetFireColor()
        };
    }

    private Vector4 GetAnimatedRainbowColor()
    {
        // Cycle through hue based on time and speed, with slight random offset per particle
        float hue = (_currentTime * _rainbowSpeed + Random.Shared.NextSingle() * 0.1f) % 1.0f;
        return HueToRgb(hue);
    }

    private static Vector4 GetFireColor()
    {
        float rand = Random.Shared.NextSingle();
        if (rand < 0.4f)
            return new Vector4(1f, 0.2f, 0f, 1f); // Red
        else if (rand < 0.7f)
            return new Vector4(1f, 0.5f, 0f, 1f); // Orange
        else
            return new Vector4(1f, 0.9f, 0.1f, 1f); // Yellow
    }

    private static Vector4 GetIceColor()
    {
        float rand = Random.Shared.NextSingle();
        if (rand < 0.4f)
            return new Vector4(0.5f, 0.8f, 1f, 1f); // Light blue
        else if (rand < 0.7f)
            return new Vector4(0.2f, 0.6f, 1f, 1f); // Blue
        else
            return new Vector4(0.8f, 0.95f, 1f, 1f); // White-blue
    }

    private static Vector4 GetRetroColor()
    {
        int choice = Random.Shared.Next(8);
        return choice switch
        {
            0 => new Vector4(1f, 0f, 0f, 1f),     // Red
            1 => new Vector4(0f, 1f, 0f, 1f),     // Green
            2 => new Vector4(0f, 0f, 1f, 1f),     // Blue
            3 => new Vector4(1f, 1f, 0f, 1f),     // Yellow
            4 => new Vector4(1f, 0f, 1f, 1f),     // Magenta
            5 => new Vector4(0f, 1f, 1f, 1f),     // Cyan
            6 => new Vector4(1f, 0.5f, 0f, 1f),   // Orange
            _ => new Vector4(1f, 1f, 1f, 1f)      // White
        };
    }

    private static Vector4 GetNeonColor()
    {
        int choice = Random.Shared.Next(5);
        return choice switch
        {
            0 => new Vector4(1f, 0.1f, 0.9f, 1f),  // Hot pink
            1 => new Vector4(0f, 1f, 0.5f, 1f),    // Neon green
            2 => new Vector4(0f, 0.8f, 1f, 1f),    // Electric blue
            3 => new Vector4(1f, 1f, 0f, 1f),      // Neon yellow
            _ => new Vector4(1f, 0.3f, 0f, 1f)     // Neon orange
        };
    }

    protected override void OnRender(IRenderContext context)
    {
        if (_vertexShader == null || _pixelShader == null)
            return;

        if (_activePixelCount == 0)
            return;

        float totalTime = (float)DateTime.Now.TimeOfDay.TotalSeconds;

        var frameData = new FrameData
        {
            ViewportSize = context.ViewportSize,
            Time = totalTime
        };
        context.UpdateBuffer(_frameDataBuffer!, frameData);

        int activeIndex = 0;
        for (int i = 0; i < _maxPixels && activeIndex < _maxPixels; i++)
        {
            ref Pixel p = ref _pixels[i];
            if (p.Life <= 0f) continue;

            _gpuPixels[activeIndex] = new PixelGPU
            {
                Position = p.Position,
                Velocity = p.Velocity,
                Color = p.Color,
                Size = p.Size,
                Life = p.Life,
                MaxLife = p.MaxLife,
                Rotation = p.Rotation
            };
            activeIndex++;
        }

        for (int j = activeIndex; j < _maxPixels; j++)
        {
            _gpuPixels[j] = default;
        }

        context.UpdateBuffer(_pixelBuffer!, (ReadOnlySpan<PixelGPU>)_gpuPixels.AsSpan(0, _maxPixels));
        context.SetVertexShader(_vertexShader);
        context.SetPixelShader(_pixelShader);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _frameDataBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _frameDataBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _pixelBuffer!);
        context.SetBlendState(BlendMode.Additive);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);
        context.DrawInstanced(6, _maxPixels, 0, 0);
        context.SetBlendState(BlendMode.Alpha);
    }

    protected override void OnDispose()
    {
        _pixelBuffer?.Dispose();
        _frameDataBuffer?.Dispose();
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
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

    private static string LoadEmbeddedShader(string name)
    {
        var assembly = typeof(PixelExplosionEffect).Assembly;
        string resourceName = $"MouseEffects.Effects.PixelExplosion.Shaders.{name}";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
