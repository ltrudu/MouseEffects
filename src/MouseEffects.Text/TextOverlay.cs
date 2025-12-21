using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using MouseEffects.Core.Rendering;
using MouseEffects.Text.Layout;
using MouseEffects.Text.Style;

namespace MouseEffects.Text;

/// <summary>
/// Implementation of the centralized text overlay rendering system.
/// </summary>
public sealed class TextOverlay : ITextOverlay
{
    private const int DefaultMaxEntities = 2000;

    private IBuffer? _entityBuffer;
    private IBuffer? _frameDataBuffer;
    private IShader? _vertexShader;
    private IShader? _pixelShader;

    private readonly TextEntityGPU[] _entities;
    private int _entityCount;
    private bool _isInitialized;
    private float _time;
    private Vector2 _viewportSize;
    private float _hdrMultiplier = 1.0f;
    private float _globalGlowIntensity = 1.0f;

    public int MaxEntities { get; }
    public int UsedEntities => _entityCount;
    public bool IsInitialized => _isInitialized;
    public float Time { get => _time; set => _time = value; }

    public TextOverlay(int maxEntities = DefaultMaxEntities)
    {
        MaxEntities = maxEntities;
        _entities = new TextEntityGPU[maxEntities];
    }

    /// <summary>
    /// Initialize the text overlay system. Must be called before use.
    /// </summary>
    public void Initialize(IRenderContext context)
    {
        if (_isInitialized) return;

        // Create entity buffer
        var entityDesc = new BufferDescription
        {
            Size = MaxEntities * Marshal.SizeOf<TextEntityGPU>(),
            Type = BufferType.Structured,
            Dynamic = true,
            StructureStride = Marshal.SizeOf<TextEntityGPU>()
        };
        _entityBuffer = context.CreateBuffer(entityDesc, default);

        // Create frame data buffer
        var frameDesc = new BufferDescription
        {
            Size = Marshal.SizeOf<TextFrameData>(),
            Type = BufferType.Constant,
            Dynamic = true
        };
        _frameDataBuffer = context.CreateBuffer(frameDesc, default);

        // Load and compile shaders
        string shaderSource = LoadEmbeddedShader();
        _vertexShader = context.CompileShader(shaderSource, "VSMain", ShaderStage.Vertex);
        _pixelShader = context.CompileShader(shaderSource, "PSMain", ShaderStage.Pixel);

        _viewportSize = context.ViewportSize;
        _hdrMultiplier = context.IsHdrEnabled ? context.HdrPeakBrightness : 1.0f;
        _isInitialized = true;
    }

    private static string LoadEmbeddedShader()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "MouseEffects.Text.Shaders.TextOverlayShader.hlsl";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public void BeginFrame()
    {
        _entityCount = 0;
    }

    public void AddText(string text, Vector2 position, TextStyle style)
    {
        if (string.IsNullOrEmpty(text)) return;

        float charWidth = style.Size * style.Spacing;
        float x = position.X + style.Size / 2; // Center of first character

        for (int i = 0; i < text.Length && _entityCount < MaxEntities; i++)
        {
            char c = text[i];
            float entityType = CharacterSet.GetEntityType(c);

            // Skip spaces but advance position
            if (entityType == TextEntityTypes.Space)
            {
                x += charWidth * CharacterSet.GetWidthMultiplier(c);
                continue;
            }

            float widthMult = CharacterSet.GetWidthMultiplier(c);

            _entities[_entityCount++] = CreateCharacterEntity(
                new Vector2(x, position.Y + style.Size / 2),
                style,
                entityType,
                i * (style.Animation?.PhaseOffset ?? 0f)
            );

            x += charWidth * widthMult;
        }
    }

    /// <summary>
    /// Add text with explicit horizontal alignment.
    /// </summary>
    /// <param name="text">Text to render.</param>
    /// <param name="anchorX">X position - meaning depends on alignment.</param>
    /// <param name="centerY">Y center position of the text.</param>
    /// <param name="style">Text style.</param>
    /// <param name="alignment">Horizontal alignment relative to anchorX.</param>
    public void AddTextAligned(string text, float anchorX, float centerY, TextStyle style, Layout.TextAlignment alignment)
    {
        if (string.IsNullOrEmpty(text)) return;

        float charWidth = style.Size * style.Spacing;

        // Calculate the total advance (distance from first char center to position after last char)
        float totalAdvance = 0f;
        foreach (char c in text)
        {
            totalAdvance += charWidth * CharacterSet.GetWidthMultiplier(c);
        }

        // Visual width is from left edge of first char to right edge of last char
        // = Size/2 (left half of first) + totalAdvance - lastCharAdvance + Size/2 (right half of last)
        // But for positioning, we need to know where to place the first character's CENTER

        float firstCharX;
        switch (alignment)
        {
            case Layout.TextAlignment.Left:
                // anchorX is the left edge of the text block
                // First char left edge at anchorX, so first char center at anchorX + Size/2
                firstCharX = anchorX + style.Size / 2;
                break;

            case Layout.TextAlignment.Center:
                // anchorX is the center of the text block
                // Text visual width = Size + (totalAdvance - lastCharAdvance)
                // But simpler: first char center to last char center = totalAdvance - lastCharWidth
                // Center of that span should be at anchorX
                // First char center = anchorX - (totalAdvance - lastCharWidth) / 2
                // Simplified: firstCharX = anchorX - totalAdvance/2 + lastCharWidth/2
                // For uniform width approximation:
                firstCharX = anchorX - totalAdvance / 2 + charWidth / 2;
                break;

            case Layout.TextAlignment.Right:
                // anchorX is the right edge of the text block
                // Last char right edge at anchorX, so last char center at anchorX - Size/2
                // Last char center = firstCharX + (totalAdvance - lastCharAdvance)
                // So: firstCharX = anchorX - Size/2 - (totalAdvance - lastCharAdvance)
                // For the last character, get its advance
                float lastCharAdvance = charWidth * CharacterSet.GetWidthMultiplier(text[text.Length - 1]);
                firstCharX = anchorX - style.Size / 2 - (totalAdvance - lastCharAdvance);
                break;

            default:
                firstCharX = anchorX + style.Size / 2;
                break;
        }

        float x = firstCharX;

        for (int i = 0; i < text.Length && _entityCount < MaxEntities; i++)
        {
            char c = text[i];
            float entityType = CharacterSet.GetEntityType(c);
            float widthMult = CharacterSet.GetWidthMultiplier(c);

            // Skip spaces but advance position
            if (entityType == TextEntityTypes.Space)
            {
                x += charWidth * widthMult;
                continue;
            }

            _entities[_entityCount++] = CreateCharacterEntity(
                new Vector2(x, centerY),
                style,
                entityType,
                i * (style.Animation?.PhaseOffset ?? 0f)
            );

            x += charWidth * widthMult;
        }
    }

    public void AddTextCentered(string text, Vector2 centerPosition, TextStyle style)
    {
        if (string.IsNullOrEmpty(text)) return;

        AddTextAligned(text, centerPosition.X, centerPosition.Y, style, Layout.TextAlignment.Center);
    }

    public void AddNumber(int value, Vector2 position, TextStyle style, int minDigits = 0)
    {
        string text = minDigits > 0 ? value.ToString($"D{minDigits}") : value.ToString();
        AddText(text, position, style);
    }

    public void AddTimer(float remainingSeconds, Vector2 position, TextStyle style, bool showMilliseconds = false)
    {
        remainingSeconds = Math.Max(0f, remainingSeconds);

        string text;
        if (showMilliseconds)
        {
            int totalMs = (int)(remainingSeconds * 1000);
            int seconds = totalMs / 1000;
            int ms = (totalMs % 1000) / 10;
            text = $"{seconds:D2}.{ms:D2}";
        }
        else
        {
            int totalSeconds = (int)remainingSeconds;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            text = $"{minutes:D2}:{seconds:D2}";
        }

        AddText(text, position, style);
    }

    public void AddBackground(Vector2 center, Vector2 size, Vector4 color, float cornerRadius = 0.1f)
    {
        if (_entityCount >= MaxEntities) return;

        _entities[_entityCount++] = TextEntityGPU.CreateBackground(center, size, color);
    }

    /// <summary>
    /// Insert a background at the front of the entity list (renders behind everything).
    /// </summary>
    internal void InsertBackgroundAtFront(Vector2 center, Vector2 size, Vector4 color)
    {
        if (_entityCount >= MaxEntities) return;

        // Shift all existing entities forward by one position
        for (int i = _entityCount; i > 0; i--)
        {
            _entities[i] = _entities[i - 1];
        }

        // Insert background at position 0
        _entities[0] = TextEntityGPU.CreateBackground(center, size, color);
        _entityCount++;
    }

    public ITextBuilder CreateBuilder()
    {
        return new TextBuilder(this);
    }

    public void EndFrame()
    {
        // Nothing to do - entities are ready
    }

    public void Render(IRenderContext context)
    {
        if (!_isInitialized || _entityCount == 0) return;

        // Update viewport info
        _viewportSize = context.ViewportSize;
        _hdrMultiplier = context.IsHdrEnabled ? context.HdrPeakBrightness : 1.0f;

        // Update frame data
        var frameData = new TextFrameData
        {
            ViewportSize = _viewportSize,
            Time = _time,
            GlowIntensity = _globalGlowIntensity,
            HdrMultiplier = _hdrMultiplier
        };
        context.UpdateBuffer(_frameDataBuffer!, frameData);

        // Update entity buffer
        context.UpdateBuffer(_entityBuffer!, (ReadOnlySpan<TextEntityGPU>)_entities.AsSpan(0, _entityCount));

        // Set render state
        context.SetVertexShader(_vertexShader!);
        context.SetPixelShader(_pixelShader!);
        context.SetConstantBuffer(ShaderStage.Vertex, 0, _frameDataBuffer!);
        context.SetConstantBuffer(ShaderStage.Pixel, 0, _frameDataBuffer!);
        context.SetShaderResource(ShaderStage.Vertex, 0, _entityBuffer!);
        context.SetBlendState(BlendMode.Alpha);
        context.SetPrimitiveTopology(PrimitiveTopology.TriangleList);

        // Draw all entities (6 vertices per quad, instanced)
        context.DrawInstanced(6, _entityCount, 0, 0);
    }

    public void Dispose()
    {
        _entityBuffer?.Dispose();
        _frameDataBuffer?.Dispose();
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _isInitialized = false;
    }

    // ========== Internal Helpers ==========

    internal void AddEntity(TextEntityGPU entity)
    {
        if (_entityCount < MaxEntities)
            _entities[_entityCount++] = entity;
    }

    internal float CalculateTextWidth(string text, TextStyle style)
    {
        float width = 0f;
        float charWidth = style.Size * style.Spacing;

        foreach (char c in text)
        {
            width += charWidth * CharacterSet.GetWidthMultiplier(c);
        }

        return width;
    }

    private TextEntityGPU CreateCharacterEntity(Vector2 position, TextStyle style, float entityType, float phaseOffset)
    {
        var anim = style.Animation;

        return new TextEntityGPU
        {
            Position = position,
            Size = Vector2.Zero,
            Color = style.Color,
            CharacterSize = style.Size,
            GlowIntensity = style.GlowIntensity,
            EntityType = entityType,
            AnimationType = anim != null ? (float)anim.Type : 0f,
            AnimationPhase = phaseOffset,
            AnimationSpeed = anim?.Speed ?? 1f,
            AnimationIntensity = anim?.Intensity ?? 1f,
            Padding = 0f
        };
    }
}
