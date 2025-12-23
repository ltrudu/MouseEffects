using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

/// <summary>
/// Peony style - perfect spherical explosion with color-shifting particles.
/// Creates a uniform sphere of stars that smoothly transition through colors as they expand.
/// </summary>
public class PeonyStyle : FireworkStyleBase
{
    public override string Name => "Peony";
    public override string Description => "Perfect expanding sphere of stars that change color as they burst outwards";
    public override int StyleId => 10;

    // Style-specific parameters
    private float _colorShiftSpeed = 1.5f;
    private int _colorShiftMode = 0;  // 0=warm, 1=cool, 2=rainbow
    private float _spherePerfection = 0.95f;

    public float ColorShiftSpeed
    {
        get => _colorShiftSpeed;
        set => _colorShiftSpeed = value;
    }

    public int ColorShiftMode
    {
        get => _colorShiftMode;
        set => _colorShiftMode = value;
    }

    public float SpherePerfection
    {
        get => _spherePerfection;
        set => _spherePerfection = value;
    }

    public override StyleDefaults GetDefaults() => new()
    {
        ParticleLifespan = 2.8f,
        Gravity = 100f,              // Lighter gravity for more spherical expansion
        Drag = 0.985f,               // Moderate drag
        MinParticlesPerFirework = 80,
        MaxParticlesPerFirework = 180,
        ExplosionForce = 300f,
        MinParticleSize = 4f,        // Larger particles
        MaxParticleSize = 10f,
        SpreadAngle = 360f,          // Perfect sphere requires full spread
        EnableSecondaryExplosion = false,  // Pure peony has no secondary
        StyleSpecific = new Dictionary<string, object>
        {
            ["colorShiftSpeed"] = 1.5f,
            ["colorShiftMode"] = 0,
            ["spherePerfection"] = 0.95f
        }
    };

    protected override void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary)
    {
        // For perfect sphere, distribute particles evenly using Fibonacci sphere algorithm
        float phi = MathF.Acos(1f - 2f * (index + 0.5f) / total);
        float theta = MathF.PI * (1f + MathF.Sqrt(5f)) * index;

        // Apply sphere perfection - higher value = more uniform, lower = more random
        float randomFactor = 1f - _spherePerfection;
        phi += (ctx.GetRandomFloat() - 0.5f) * randomFactor * MathF.PI * 0.3f;
        theta += (ctx.GetRandomFloat() - 0.5f) * randomFactor * MathF.PI;

        // Convert spherical to Cartesian for 2D projection
        float x = MathF.Sin(phi) * MathF.Cos(theta);
        float y = MathF.Sin(phi) * MathF.Sin(theta);

        // Recalculate velocity for perfect spherical distribution
        // Use the current velocity magnitude from the base class
        float currentSpeed = p.Velocity.Length();
        Vector2 direction = new Vector2(x, y);
        p.Velocity = direction * currentSpeed;

        // Store original color in StyleData for color shifting
        // StyleData1 = original R
        // StyleData2 = original G
        // StyleData3 = original B
        p.StyleData1 = p.Color.X;
        p.StyleData2 = p.Color.Y;
        p.StyleData3 = p.Color.Z;

        // Make particles slightly larger and rounder
        p.Size *= 1.2f;
    }

    public override void UpdateParticle(ref FireworkParticle particle, float dt, float time)
    {
        // Calculate life progress (0 = just spawned, 1 = about to die)
        float lifeProgress = 1f - (particle.Life / particle.MaxLife);

        // Apply color shift based on life progress
        Vector4 newColor = GetShiftedColor(
            particle.StyleData1,  // Original R
            particle.StyleData2,  // Original G
            particle.StyleData3,  // Original B
            lifeProgress
        );

        particle.Color.X = newColor.X;
        particle.Color.Y = newColor.Y;
        particle.Color.Z = newColor.Z;
        particle.Color.W = newColor.W;
    }

    private Vector4 GetShiftedColor(float originalR, float originalG, float originalB, float progress)
    {
        // Adjust progress by speed
        float t = MathF.Min(1f, progress * _colorShiftSpeed);

        switch (_colorShiftMode)
        {
            case 0: // Warm: red -> orange -> yellow -> white
                {
                    float r = originalR;
                    float g = originalG;
                    float b = originalB;

                    if (t < 0.33f)
                    {
                        // Red to Orange (increase green)
                        float localT = t / 0.33f;
                        g = MathF.Min(1f, g + localT * 0.5f);
                    }
                    else if (t < 0.66f)
                    {
                        // Orange to Yellow (increase green more)
                        float localT = (t - 0.33f) / 0.33f;
                        g = MathF.Min(1f, g + 0.5f + localT * 0.5f);
                    }
                    else
                    {
                        // Yellow to White (increase blue, normalize)
                        float localT = (t - 0.66f) / 0.34f;
                        b = MathF.Min(1f, b + localT * (1f - b));
                        g = MathF.Min(1f, g + localT * (1f - g));
                        r = MathF.Min(1f, r + localT * (1f - r));
                    }

                    return new Vector4(r, g, b, 1f);
                }

            case 1: // Cool: color -> blue -> cyan -> white
                {
                    float r = originalR;
                    float g = originalG;
                    float b = originalB;

                    if (t < 0.33f)
                    {
                        // Shift to blue (increase blue, reduce red)
                        float localT = t / 0.33f;
                        b = MathF.Min(1f, b + localT * 0.5f);
                        r *= (1f - localT * 0.3f);
                    }
                    else if (t < 0.66f)
                    {
                        // Blue to Cyan (increase green)
                        float localT = (t - 0.33f) / 0.33f;
                        g = MathF.Min(1f, g + localT * 0.6f);
                        b = MathF.Min(1f, b + 0.5f + localT * 0.3f);
                    }
                    else
                    {
                        // Cyan to White
                        float localT = (t - 0.66f) / 0.34f;
                        r = MathF.Min(1f, r + localT * (1f - r));
                        g = MathF.Min(1f, g + localT * (1f - g));
                        b = MathF.Min(1f, b + localT * (1f - b));
                    }

                    return new Vector4(r, g, b, 1f);
                }

            case 2: // Rainbow: cycle through spectrum
                {
                    // Use HSV color space for smooth rainbow transitions
                    float hue = (progress * 360f * _colorShiftSpeed) % 360f;
                    return HsvToRgb(hue, 0.9f, 1f);
                }

            default:
                return new Vector4(originalR, originalG, originalB, 1f);
        }
    }

    private static Vector4 HsvToRgb(float h, float s, float v)
    {
        float c = v * s;
        float x = c * (1f - MathF.Abs((h / 60f) % 2f - 1f));
        float m = v - c;

        float r, g, b;

        if (h < 60f)
        {
            r = c; g = x; b = 0;
        }
        else if (h < 120f)
        {
            r = x; g = c; b = 0;
        }
        else if (h < 180f)
        {
            r = 0; g = c; b = x;
        }
        else if (h < 240f)
        {
            r = 0; g = x; b = c;
        }
        else if (h < 300f)
        {
            r = x; g = 0; b = c;
        }
        else
        {
            r = c; g = 0; b = x;
        }

        return new Vector4(r + m, g + m, b + m, 1f);
    }

    public override void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle)
    {
        gpu.StyleData1 = particle.StyleData1;  // Original R
        gpu.StyleData2 = particle.StyleData2;  // Original G
        gpu.StyleData3 = particle.StyleData3;  // Original B
        gpu.StyleFlags = (uint)StyleId;
    }

    protected override float GetParticleSize(FireworkContext ctx, bool isSecondary)
    {
        // Peony particles are larger and rounder
        return base.GetParticleSize(ctx, isSecondary) * 1.3f;
    }

    public override IEnumerable<StyleParameter> GetParameters()
    {
        yield return new FloatStyleParameter
        {
            Key = "colorShiftSpeed",
            DisplayName = "Color Shift Speed",
            Description = "How quickly particles change color over their lifetime",
            MinValue = 0.5f,
            MaxValue = 3f,
            DefaultValue = 1.5f,
            Step = 0.1f
        };
        yield return new IntStyleParameter
        {
            Key = "colorShiftMode",
            DisplayName = "Color Shift Mode",
            Description = "0 = Warm (red→yellow→white), 1 = Cool (blue→cyan→white), 2 = Rainbow",
            MinValue = 0,
            MaxValue = 2,
            DefaultValue = 0
        };
        yield return new FloatStyleParameter
        {
            Key = "spherePerfection",
            DisplayName = "Sphere Perfection",
            Description = "How uniform the spherical distribution is (higher = more perfect sphere)",
            MinValue = 0.8f,
            MaxValue = 1f,
            DefaultValue = 0.95f,
            Step = 0.01f
        };
    }

    public override void SetParameter(string key, object value)
    {
        base.SetParameter(key, value);
        switch (key)
        {
            case "colorShiftSpeed":
                _colorShiftSpeed = Convert.ToSingle(value);
                break;
            case "colorShiftMode":
                _colorShiftMode = Convert.ToInt32(value);
                break;
            case "spherePerfection":
                _spherePerfection = Convert.ToSingle(value);
                break;
        }
    }

    public override object? GetParameter(string key)
    {
        return key switch
        {
            "colorShiftSpeed" => _colorShiftSpeed,
            "colorShiftMode" => _colorShiftMode,
            "spherePerfection" => _spherePerfection,
            _ => base.GetParameter(key)
        };
    }
}
