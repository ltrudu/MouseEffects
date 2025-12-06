using System.Numerics;

namespace MouseEffects.Effects.ColorBlindnessNG;

/// <summary>
/// Generates LUT (Look-Up Table) textures for color remapping.
/// </summary>
public static class LUTGenerator
{
    public const int LutSize = 256;

    /// <summary>
    /// Generates a 256x1 RGBA float LUT texture data from start to end color.
    /// </summary>
    public static byte[] GenerateLUT(Vector3 startColor, Vector3 endColor, GradientType gradientType)
    {
        var data = new float[LutSize * 4]; // RGBA

        for (int i = 0; i < LutSize; i++)
        {
            float t = i / (float)(LutSize - 1);
            Vector3 color = gradientType switch
            {
                GradientType.LinearRGB => LerpLinearRGB(startColor, endColor, t),
                GradientType.PerceptualLAB => LerpLAB(startColor, endColor, t),
                GradientType.HSL => LerpHSL(startColor, endColor, t),
                _ => LerpLinearRGB(startColor, endColor, t)
            };

            data[i * 4 + 0] = color.X; // R
            data[i * 4 + 1] = color.Y; // G
            data[i * 4 + 2] = color.Z; // B
            data[i * 4 + 3] = 1.0f;    // A
        }

        // Convert to bytes
        var bytes = new byte[data.Length * sizeof(float)];
        Buffer.BlockCopy(data, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// Simple linear interpolation in RGB space.
    /// </summary>
    private static Vector3 LerpLinearRGB(Vector3 start, Vector3 end, float t)
    {
        return Vector3.Lerp(start, end, t);
    }

    /// <summary>
    /// Perceptually uniform interpolation in LAB color space.
    /// </summary>
    private static Vector3 LerpLAB(Vector3 startRGB, Vector3 endRGB, float t)
    {
        // Convert RGB to LAB
        Vector3 startLAB = RGBToLAB(startRGB);
        Vector3 endLAB = RGBToLAB(endRGB);

        // Interpolate in LAB
        Vector3 interpLAB = Vector3.Lerp(startLAB, endLAB, t);

        // Convert back to RGB
        return LABToRGB(interpLAB);
    }

    /// <summary>
    /// HSL interpolation for more vibrant gradients.
    /// </summary>
    private static Vector3 LerpHSL(Vector3 startRGB, Vector3 endRGB, float t)
    {
        // Convert RGB to HSL
        Vector3 startHSL = RGBToHSL(startRGB);
        Vector3 endHSL = RGBToHSL(endRGB);

        // Handle hue wrap-around (take shortest path)
        float hStart = startHSL.X;
        float hEnd = endHSL.X;
        float hDiff = hEnd - hStart;

        if (hDiff > 0.5f)
            hStart += 1.0f;
        else if (hDiff < -0.5f)
            hEnd += 1.0f;

        float h = Lerp(hStart, hEnd, t);
        if (h > 1.0f) h -= 1.0f;
        if (h < 0.0f) h += 1.0f;

        float s = Lerp(startHSL.Y, endHSL.Y, t);
        float l = Lerp(startHSL.Z, endHSL.Z, t);

        // Convert back to RGB
        return HSLToRGB(new Vector3(h, s, l));
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    #region Color Space Conversions

    // RGB to XYZ (D65 illuminant)
    private static Vector3 RGBToXYZ(Vector3 rgb)
    {
        // Linearize sRGB
        float r = SRGBToLinear(rgb.X);
        float g = SRGBToLinear(rgb.Y);
        float b = SRGBToLinear(rgb.Z);

        // RGB to XYZ matrix (D65)
        float x = r * 0.4124564f + g * 0.3575761f + b * 0.1804375f;
        float y = r * 0.2126729f + g * 0.7151522f + b * 0.0721750f;
        float z = r * 0.0193339f + g * 0.1191920f + b * 0.9503041f;

        return new Vector3(x, y, z);
    }

    // XYZ to RGB (D65 illuminant)
    private static Vector3 XYZToRGB(Vector3 xyz)
    {
        // XYZ to RGB matrix (D65)
        float r = xyz.X * 3.2404542f + xyz.Y * -1.5371385f + xyz.Z * -0.4985314f;
        float g = xyz.X * -0.9692660f + xyz.Y * 1.8760108f + xyz.Z * 0.0415560f;
        float b = xyz.X * 0.0556434f + xyz.Y * -0.2040259f + xyz.Z * 1.0572252f;

        // Gamma encode to sRGB
        return new Vector3(
            LinearToSRGB(r),
            LinearToSRGB(g),
            LinearToSRGB(b)
        );
    }

    // D65 reference white
    private const float RefX = 0.95047f;
    private const float RefY = 1.00000f;
    private const float RefZ = 1.08883f;

    // XYZ to LAB
    private static Vector3 XYZToLAB(Vector3 xyz)
    {
        float x = xyz.X / RefX;
        float y = xyz.Y / RefY;
        float z = xyz.Z / RefZ;

        x = x > 0.008856f ? MathF.Pow(x, 1f / 3f) : (7.787f * x) + (16f / 116f);
        y = y > 0.008856f ? MathF.Pow(y, 1f / 3f) : (7.787f * y) + (16f / 116f);
        z = z > 0.008856f ? MathF.Pow(z, 1f / 3f) : (7.787f * z) + (16f / 116f);

        float L = (116f * y) - 16f;
        float a = 500f * (x - y);
        float B = 200f * (y - z);

        return new Vector3(L, a, B);
    }

    // LAB to XYZ
    private static Vector3 LABToXYZ(Vector3 lab)
    {
        float y = (lab.X + 16f) / 116f;
        float x = lab.Y / 500f + y;
        float z = y - lab.Z / 200f;

        float x3 = x * x * x;
        float y3 = y * y * y;
        float z3 = z * z * z;

        x = x3 > 0.008856f ? x3 : (x - 16f / 116f) / 7.787f;
        y = y3 > 0.008856f ? y3 : (y - 16f / 116f) / 7.787f;
        z = z3 > 0.008856f ? z3 : (z - 16f / 116f) / 7.787f;

        return new Vector3(x * RefX, y * RefY, z * RefZ);
    }

    // RGB to LAB
    private static Vector3 RGBToLAB(Vector3 rgb)
    {
        return XYZToLAB(RGBToXYZ(rgb));
    }

    // LAB to RGB
    private static Vector3 LABToRGB(Vector3 lab)
    {
        Vector3 rgb = XYZToRGB(LABToXYZ(lab));
        // Clamp to valid RGB range
        return new Vector3(
            Math.Clamp(rgb.X, 0f, 1f),
            Math.Clamp(rgb.Y, 0f, 1f),
            Math.Clamp(rgb.Z, 0f, 1f)
        );
    }

    // RGB to HSL
    private static Vector3 RGBToHSL(Vector3 rgb)
    {
        float r = rgb.X, g = rgb.Y, b = rgb.Z;
        float max = MathF.Max(r, MathF.Max(g, b));
        float min = MathF.Min(r, MathF.Min(g, b));
        float l = (max + min) / 2f;

        if (max == min)
        {
            return new Vector3(0, 0, l); // achromatic
        }

        float d = max - min;
        float s = l > 0.5f ? d / (2f - max - min) : d / (max + min);

        float h;
        if (max == r)
            h = ((g - b) / d + (g < b ? 6f : 0f)) / 6f;
        else if (max == g)
            h = ((b - r) / d + 2f) / 6f;
        else
            h = ((r - g) / d + 4f) / 6f;

        return new Vector3(h, s, l);
    }

    // HSL to RGB
    private static Vector3 HSLToRGB(Vector3 hsl)
    {
        float h = hsl.X, s = hsl.Y, l = hsl.Z;

        if (s == 0)
        {
            return new Vector3(l, l, l); // achromatic
        }

        float q = l < 0.5f ? l * (1f + s) : l + s - l * s;
        float p = 2f * l - q;

        return new Vector3(
            HueToRGB(p, q, h + 1f / 3f),
            HueToRGB(p, q, h),
            HueToRGB(p, q, h - 1f / 3f)
        );
    }

    private static float HueToRGB(float p, float q, float t)
    {
        if (t < 0f) t += 1f;
        if (t > 1f) t -= 1f;
        if (t < 1f / 6f) return p + (q - p) * 6f * t;
        if (t < 1f / 2f) return q;
        if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6f;
        return p;
    }

    // sRGB gamma correction
    private static float SRGBToLinear(float c)
    {
        return c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
    }

    private static float LinearToSRGB(float c)
    {
        c = Math.Clamp(c, 0f, 1f);
        return c <= 0.0031308f ? c * 12.92f : 1.055f * MathF.Pow(c, 1f / 2.4f) - 0.055f;
    }

    #endregion
}
