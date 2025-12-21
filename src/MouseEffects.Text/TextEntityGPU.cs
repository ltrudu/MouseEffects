using System.Numerics;
using System.Runtime.InteropServices;

namespace MouseEffects.Text;

/// <summary>
/// GPU entity structure for text rendering. Must match the shader's StructuredBuffer layout.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct TextEntityGPU
{
    /// <summary>Center position of the character in screen space.</summary>
    public Vector2 Position;

    /// <summary>For background entities: stores (width/2, height/2). Otherwise unused.</summary>
    public Vector2 Size;

    /// <summary>Character color (RGBA).</summary>
    public Vector4 Color;

    /// <summary>Character size in pixels.</summary>
    public float CharacterSize;

    /// <summary>Alpha/glow intensity multiplier (0-1+).</summary>
    public float GlowIntensity;

    /// <summary>Entity type (digit, letter, punctuation, background).</summary>
    public float EntityType;

    /// <summary>Animation type (0=none, 1=pulse, 2=wave, etc.).</summary>
    public float AnimationType;

    /// <summary>Animation phase offset (for per-character animation variation).</summary>
    public float AnimationPhase;

    /// <summary>Animation speed multiplier.</summary>
    public float AnimationSpeed;

    /// <summary>Animation intensity multiplier.</summary>
    public float AnimationIntensity;

    /// <summary>Padding for 16-byte alignment.</summary>
    public float Padding;

    /// <summary>
    /// Create a text entity for a character.
    /// </summary>
    public static TextEntityGPU CreateCharacter(
        Vector2 position,
        Vector4 color,
        float size,
        float entityType,
        float glowIntensity = 1f,
        float animationType = 0f,
        float animationPhase = 0f,
        float animationSpeed = 1f,
        float animationIntensity = 1f)
    {
        return new TextEntityGPU
        {
            Position = position,
            Size = Vector2.Zero,
            Color = color,
            CharacterSize = size,
            GlowIntensity = glowIntensity,
            EntityType = entityType,
            AnimationType = animationType,
            AnimationPhase = animationPhase,
            AnimationSpeed = animationSpeed,
            AnimationIntensity = animationIntensity,
            Padding = 0f
        };
    }

    /// <summary>
    /// Create a background panel entity.
    /// </summary>
    public static TextEntityGPU CreateBackground(
        Vector2 center,
        Vector2 size,
        Vector4 color,
        float glowIntensity = 1f)
    {
        return new TextEntityGPU
        {
            Position = center,
            Size = size * 0.5f, // Shader uses half-size
            Color = color,
            CharacterSize = 1f,
            GlowIntensity = glowIntensity,
            EntityType = TextEntityTypes.Background,
            AnimationType = 0f,
            AnimationPhase = 0f,
            AnimationSpeed = 0f,
            AnimationIntensity = 0f,
            Padding = 0f
        };
    }
}

/// <summary>
/// Frame data constant buffer for text rendering.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 48)]
public struct TextFrameData
{
    public Vector2 ViewportSize;
    public float Time;
    public float GlowIntensity;
    public float HdrMultiplier;
    public float Padding1;
    public float Padding2;
    public float Padding3;
    public Vector2 Padding4;
    public Vector2 Padding5;
}
