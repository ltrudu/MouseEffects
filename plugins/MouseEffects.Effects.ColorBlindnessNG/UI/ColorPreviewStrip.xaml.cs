using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MouseEffects.Effects.ColorBlindnessNG.UI;

/// <summary>
/// Shows original and corrected color spectrum strips for visual comparison.
/// </summary>
public partial class ColorPreviewStrip : System.Windows.Controls.UserControl
{
    private const int StripWidth = 256;
    private const int StripHeight = 18;

    public ColorPreviewStrip()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    #region Dependency Properties

    public static readonly DependencyProperty RedChannelProperty = DependencyProperty.Register(
        nameof(RedChannel), typeof(ChannelLUTSettings), typeof(ColorPreviewStrip),
        new PropertyMetadata(null, OnSettingsChanged));

    public static readonly DependencyProperty GreenChannelProperty = DependencyProperty.Register(
        nameof(GreenChannel), typeof(ChannelLUTSettings), typeof(ColorPreviewStrip),
        new PropertyMetadata(null, OnSettingsChanged));

    public static readonly DependencyProperty BlueChannelProperty = DependencyProperty.Register(
        nameof(BlueChannel), typeof(ChannelLUTSettings), typeof(ColorPreviewStrip),
        new PropertyMetadata(null, OnSettingsChanged));

    public static readonly DependencyProperty GradientTypeProperty = DependencyProperty.Register(
        nameof(GradientType), typeof(GradientType), typeof(ColorPreviewStrip),
        new PropertyMetadata(GradientType.LinearRGB, OnSettingsChanged));

    public static readonly DependencyProperty ApplicationModeProperty = DependencyProperty.Register(
        nameof(ApplicationMode), typeof(ApplicationMode), typeof(ColorPreviewStrip),
        new PropertyMetadata(ApplicationMode.FullChannel, OnSettingsChanged));

    public static readonly DependencyProperty ThresholdProperty = DependencyProperty.Register(
        nameof(Threshold), typeof(float), typeof(ColorPreviewStrip),
        new PropertyMetadata(0.3f, OnSettingsChanged));

    public static readonly DependencyProperty IntensityProperty = DependencyProperty.Register(
        nameof(Intensity), typeof(float), typeof(ColorPreviewStrip),
        new PropertyMetadata(1.0f, OnSettingsChanged));

    public ChannelLUTSettings? RedChannel
    {
        get => (ChannelLUTSettings?)GetValue(RedChannelProperty);
        set => SetValue(RedChannelProperty, value);
    }

    public ChannelLUTSettings? GreenChannel
    {
        get => (ChannelLUTSettings?)GetValue(GreenChannelProperty);
        set => SetValue(GreenChannelProperty, value);
    }

    public ChannelLUTSettings? BlueChannel
    {
        get => (ChannelLUTSettings?)GetValue(BlueChannelProperty);
        set => SetValue(BlueChannelProperty, value);
    }

    public GradientType GradientType
    {
        get => (GradientType)GetValue(GradientTypeProperty);
        set => SetValue(GradientTypeProperty, value);
    }

    public ApplicationMode ApplicationMode
    {
        get => (ApplicationMode)GetValue(ApplicationModeProperty);
        set => SetValue(ApplicationModeProperty, value);
    }

    public float Threshold
    {
        get => (float)GetValue(ThresholdProperty);
        set => SetValue(ThresholdProperty, value);
    }

    public float Intensity
    {
        get => (float)GetValue(IntensityProperty);
        set => SetValue(IntensityProperty, value);
    }

    private static void OnSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ColorPreviewStrip strip)
            strip.UpdateStrips();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Force a refresh of the preview strips.
    /// </summary>
    public void Refresh() => UpdateStrips();

    #endregion

    #region Initialization

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RenderOriginalSpectrum();
        UpdateStrips();
    }

    #endregion

    #region Rendering

    private void RenderOriginalSpectrum()
    {
        var bitmap = new WriteableBitmap(StripWidth, StripHeight, 96, 96, PixelFormats.Bgra32, null);
        int stride = StripWidth * 4;
        byte[] pixels = new byte[StripWidth * StripHeight * 4];

        for (int x = 0; x < StripWidth; x++)
        {
            double hue = (x / (double)(StripWidth - 1)) * 360;
            var (r, g, b) = HsvToRgb(hue, 1.0, 1.0);

            byte rb = (byte)(r * 255);
            byte gb = (byte)(g * 255);
            byte bb = (byte)(b * 255);

            for (int y = 0; y < StripHeight; y++)
            {
                int idx = (y * StripWidth + x) * 4;
                pixels[idx + 0] = bb; // B
                pixels[idx + 1] = gb; // G
                pixels[idx + 2] = rb; // R
                pixels[idx + 3] = 255; // A
            }
        }

        bitmap.WritePixels(new Int32Rect(0, 0, StripWidth, StripHeight), pixels, stride, 0);
        OriginalStrip.Source = bitmap;
    }

    private void UpdateStrips()
    {
        if (!IsLoaded) return;

        var bitmap = new WriteableBitmap(StripWidth, StripHeight, 96, 96, PixelFormats.Bgra32, null);
        int stride = StripWidth * 4;
        byte[] pixels = new byte[StripWidth * StripHeight * 4];

        // Pre-generate LUTs for each channel
        Vector3[]? redLut = null;
        Vector3[]? greenLut = null;
        Vector3[]? blueLut = null;

        if (RedChannel?.Enabled == true)
            redLut = GenerateLUT(RedChannel.StartColor, RedChannel.EndColor);
        if (GreenChannel?.Enabled == true)
            greenLut = GenerateLUT(GreenChannel.StartColor, GreenChannel.EndColor);
        if (BlueChannel?.Enabled == true)
            blueLut = GenerateLUT(BlueChannel.StartColor, BlueChannel.EndColor);

        for (int x = 0; x < StripWidth; x++)
        {
            double hue = (x / (double)(StripWidth - 1)) * 360;
            var (r, g, b) = HsvToRgb(hue, 1.0, 1.0);
            var original = new Vector3((float)r, (float)g, (float)b);
            var corrected = ApplyLutCorrection(original, redLut, greenLut, blueLut);

            byte rb = (byte)(Math.Clamp(corrected.X, 0, 1) * 255);
            byte gb = (byte)(Math.Clamp(corrected.Y, 0, 1) * 255);
            byte bb = (byte)(Math.Clamp(corrected.Z, 0, 1) * 255);

            for (int y = 0; y < StripHeight; y++)
            {
                int idx = (y * StripWidth + x) * 4;
                pixels[idx + 0] = bb; // B
                pixels[idx + 1] = gb; // G
                pixels[idx + 2] = rb; // R
                pixels[idx + 3] = 255; // A
            }
        }

        bitmap.WritePixels(new Int32Rect(0, 0, StripWidth, StripHeight), pixels, stride, 0);
        CorrectedStrip.Source = bitmap;
    }

    private Vector3[] GenerateLUT(Vector3 startColor, Vector3 endColor)
    {
        var lut = new Vector3[256];
        for (int i = 0; i < 256; i++)
        {
            float t = i / 255f;
            lut[i] = InterpolateColor(startColor, endColor, t, GradientType);
        }
        return lut;
    }

    private Vector3 ApplyLutCorrection(Vector3 color, Vector3[]? redLut, Vector3[]? greenLut, Vector3[]? blueLut)
    {
        Vector3 result = color;

        // Check application mode
        bool applyRed = ShouldApplyChannel(color, color.X, RedChannel);
        bool applyGreen = ShouldApplyChannel(color, color.Y, GreenChannel);
        bool applyBlue = ShouldApplyChannel(color, color.Z, BlueChannel);

        if (applyRed && redLut != null && RedChannel != null)
        {
            int idx = Math.Clamp((int)(color.X * 255), 0, 255);
            result = ApplyBlendMode(result, redLut[idx], color.X, RedChannel.Strength * Intensity, RedChannel.BlendMode, color);
        }

        if (applyGreen && greenLut != null && GreenChannel != null)
        {
            int idx = Math.Clamp((int)(color.Y * 255), 0, 255);
            result = ApplyBlendMode(result, greenLut[idx], color.Y, GreenChannel.Strength * Intensity, GreenChannel.BlendMode, color);
        }

        if (applyBlue && blueLut != null && BlueChannel != null)
        {
            int idx = Math.Clamp((int)(color.Z * 255), 0, 255);
            result = ApplyBlendMode(result, blueLut[idx], color.Z, BlueChannel.Strength * Intensity, BlueChannel.BlendMode, color);
        }

        return result;
    }

    private bool ShouldApplyChannel(Vector3 color, float channelValue, ChannelLUTSettings? settings)
    {
        if (settings == null || !settings.Enabled) return false;

        // White protection
        float minChannel = MathF.Min(color.X, MathF.Min(color.Y, color.Z));
        if (minChannel > 1.0f - settings.WhiteProtection) return false;

        // Dominance threshold
        if (settings.DominanceThreshold > 0.001f)
        {
            float total = color.X + color.Y + color.Z;
            if (total > 0.001f)
            {
                float dominance = channelValue / total;
                if (dominance < settings.DominanceThreshold) return false;
            }
        }

        // Application mode
        switch (ApplicationMode)
        {
            case ApplicationMode.DominantOnly:
                float maxChannel = MathF.Max(color.X, MathF.Max(color.Y, color.Z));
                if (channelValue < maxChannel - 0.001f) return false;
                break;
            case ApplicationMode.Threshold:
                if (channelValue < Threshold) return false;
                break;
        }

        return true;
    }

    private static Vector3 ApplyBlendMode(Vector3 result, Vector3 lutColor, float channelValue, float strength, LutBlendMode blendMode, Vector3 originalColor)
    {
        Vector3 blended;

        switch (blendMode)
        {
            case LutBlendMode.ChannelWeighted:
                // Blend amount depends on channel intensity
                blended = Vector3.Lerp(result, Vector3.Lerp(result, lutColor, channelValue), strength);
                break;

            case LutBlendMode.Direct:
                // Full replacement controlled only by strength
                blended = Vector3.Lerp(result, lutColor, strength);
                break;

            case LutBlendMode.Proportional:
                // Blend based on channel's relative dominance
                float maxChannel = MathF.Max(originalColor.X, MathF.Max(originalColor.Y, originalColor.Z));
                float proportion = (maxChannel > 0.001f) ? (channelValue / maxChannel) : 0.0f;
                blended = Vector3.Lerp(result, lutColor, proportion * strength);
                break;

            case LutBlendMode.Additive:
                // Add color shift - preserves luminosity better
                Vector3 shift = (lutColor - result) * channelValue * strength;
                blended = Vector3.Clamp(result + shift, Vector3.Zero, Vector3.One);
                break;

            case LutBlendMode.Screen:
                // Screen blend - brightens colors
                Vector3 screenFactor = lutColor * channelValue * strength;
                blended = Vector3.One - (Vector3.One - result) * (Vector3.One - screenFactor);
                break;

            default:
                blended = result;
                break;
        }

        return blended;
    }

    #endregion

    #region Color Interpolation

    private static Vector3 InterpolateColor(Vector3 start, Vector3 end, float t, GradientType type)
    {
        return type switch
        {
            GradientType.LinearRGB => Vector3.Lerp(start, end, t),
            GradientType.PerceptualLAB => LerpLAB(start, end, t),
            GradientType.HSL => LerpHSL(start, end, t),
            _ => Vector3.Lerp(start, end, t)
        };
    }

    private static Vector3 LerpLAB(Vector3 startRGB, Vector3 endRGB, float t)
    {
        Vector3 startLAB = RGBToLAB(startRGB);
        Vector3 endLAB = RGBToLAB(endRGB);
        Vector3 interpLAB = Vector3.Lerp(startLAB, endLAB, t);
        return LABToRGB(interpLAB);
    }

    private static Vector3 LerpHSL(Vector3 startRGB, Vector3 endRGB, float t)
    {
        Vector3 startHSL = RGBToHSL(startRGB);
        Vector3 endHSL = RGBToHSL(endRGB);

        float hStart = startHSL.X;
        float hEnd = endHSL.X;
        float hDiff = hEnd - hStart;

        if (hDiff > 0.5f) hStart += 1.0f;
        else if (hDiff < -0.5f) hEnd += 1.0f;

        float h = Lerp(hStart, hEnd, t);
        if (h > 1.0f) h -= 1.0f;
        if (h < 0.0f) h += 1.0f;

        float s = Lerp(startHSL.Y, endHSL.Y, t);
        float l = Lerp(startHSL.Z, endHSL.Z, t);

        return HSLToRGB(new Vector3(h, s, l));
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    #endregion

    #region Color Space Conversions

    private static (double r, double g, double b) HsvToRgb(double h, double s, double v)
    {
        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = v - c;

        double r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return (r + m, g + m, b + m);
    }

    private static Vector3 RGBToXYZ(Vector3 rgb)
    {
        float r = SRGBToLinear(rgb.X);
        float g = SRGBToLinear(rgb.Y);
        float b = SRGBToLinear(rgb.Z);

        float x = r * 0.4124564f + g * 0.3575761f + b * 0.1804375f;
        float y = r * 0.2126729f + g * 0.7151522f + b * 0.0721750f;
        float z = r * 0.0193339f + g * 0.1191920f + b * 0.9503041f;

        return new Vector3(x, y, z);
    }

    private static Vector3 XYZToRGB(Vector3 xyz)
    {
        float r = xyz.X * 3.2404542f + xyz.Y * -1.5371385f + xyz.Z * -0.4985314f;
        float g = xyz.X * -0.9692660f + xyz.Y * 1.8760108f + xyz.Z * 0.0415560f;
        float b = xyz.X * 0.0556434f + xyz.Y * -0.2040259f + xyz.Z * 1.0572252f;

        return new Vector3(LinearToSRGB(r), LinearToSRGB(g), LinearToSRGB(b));
    }

    private const float RefX = 0.95047f;
    private const float RefY = 1.00000f;
    private const float RefZ = 1.08883f;

    private static Vector3 XYZToLAB(Vector3 xyz)
    {
        float x = xyz.X / RefX;
        float y = xyz.Y / RefY;
        float z = xyz.Z / RefZ;

        x = x > 0.008856f ? MathF.Pow(x, 1f / 3f) : (7.787f * x) + (16f / 116f);
        y = y > 0.008856f ? MathF.Pow(y, 1f / 3f) : (7.787f * y) + (16f / 116f);
        z = z > 0.008856f ? MathF.Pow(z, 1f / 3f) : (7.787f * z) + (16f / 116f);

        return new Vector3((116f * y) - 16f, 500f * (x - y), 200f * (y - z));
    }

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

    private static Vector3 RGBToLAB(Vector3 rgb) => XYZToLAB(RGBToXYZ(rgb));

    private static Vector3 LABToRGB(Vector3 lab)
    {
        Vector3 rgb = XYZToRGB(LABToXYZ(lab));
        return new Vector3(Math.Clamp(rgb.X, 0f, 1f), Math.Clamp(rgb.Y, 0f, 1f), Math.Clamp(rgb.Z, 0f, 1f));
    }

    private static Vector3 RGBToHSL(Vector3 rgb)
    {
        float r = rgb.X, g = rgb.Y, b = rgb.Z;
        float max = MathF.Max(r, MathF.Max(g, b));
        float min = MathF.Min(r, MathF.Min(g, b));
        float l = (max + min) / 2f;

        if (max == min)
            return new Vector3(0, 0, l);

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

    private static Vector3 HSLToRGB(Vector3 hsl)
    {
        float h = hsl.X, s = hsl.Y, l = hsl.Z;

        if (s == 0)
            return new Vector3(l, l, l);

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
