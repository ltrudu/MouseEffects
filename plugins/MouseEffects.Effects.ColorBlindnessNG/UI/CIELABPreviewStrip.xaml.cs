using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MouseEffects.Effects.ColorBlindnessNG.UI;

/// <summary>
/// Shows before/after color spectrum preview for CIELAB remapping correction.
/// </summary>
public partial class CIELABPreviewStrip : System.Windows.Controls.UserControl
{
    private const int StripWidth = 360;
    private const int StripHeight = 20;

    public CIELABPreviewStrip()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    #region Dependency Properties

    public static readonly DependencyProperty AtoBTransferProperty = DependencyProperty.Register(
        nameof(AtoBTransfer), typeof(double), typeof(CIELABPreviewStrip),
        new PropertyMetadata(0.5, OnParameterChanged));

    public static readonly DependencyProperty BtoATransferProperty = DependencyProperty.Register(
        nameof(BtoATransfer), typeof(double), typeof(CIELABPreviewStrip),
        new PropertyMetadata(0.0, OnParameterChanged));

    public static readonly DependencyProperty AEnhanceProperty = DependencyProperty.Register(
        nameof(AEnhance), typeof(double), typeof(CIELABPreviewStrip),
        new PropertyMetadata(1.0, OnParameterChanged));

    public static readonly DependencyProperty BEnhanceProperty = DependencyProperty.Register(
        nameof(BEnhance), typeof(double), typeof(CIELABPreviewStrip),
        new PropertyMetadata(1.0, OnParameterChanged));

    public static readonly DependencyProperty LEnhanceProperty = DependencyProperty.Register(
        nameof(LEnhance), typeof(double), typeof(CIELABPreviewStrip),
        new PropertyMetadata(0.0, OnParameterChanged));

    public static readonly DependencyProperty StrengthProperty = DependencyProperty.Register(
        nameof(Strength), typeof(double), typeof(CIELABPreviewStrip),
        new PropertyMetadata(1.0, OnParameterChanged));

    public double AtoBTransfer
    {
        get => (double)GetValue(AtoBTransferProperty);
        set => SetValue(AtoBTransferProperty, value);
    }

    public double BtoATransfer
    {
        get => (double)GetValue(BtoATransferProperty);
        set => SetValue(BtoATransferProperty, value);
    }

    public double AEnhance
    {
        get => (double)GetValue(AEnhanceProperty);
        set => SetValue(AEnhanceProperty, value);
    }

    public double BEnhance
    {
        get => (double)GetValue(BEnhanceProperty);
        set => SetValue(BEnhanceProperty, value);
    }

    public double LEnhance
    {
        get => (double)GetValue(LEnhanceProperty);
        set => SetValue(LEnhanceProperty, value);
    }

    public double Strength
    {
        get => (double)GetValue(StrengthProperty);
        set => SetValue(StrengthProperty, value);
    }

    private static void OnParameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CIELABPreviewStrip strip)
            strip.UpdateCorrectedStrip();
    }

    #endregion

    #region Rendering

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RenderOriginalStrip();
        UpdateCorrectedStrip();
    }

    private void RenderOriginalStrip()
    {
        var bitmap = new WriteableBitmap(StripWidth, StripHeight, 96, 96, PixelFormats.Bgra32, null);
        int stride = StripWidth * 4;
        byte[] pixels = new byte[StripWidth * StripHeight * 4];

        for (int x = 0; x < StripWidth; x++)
        {
            double hue = x; // 0-360 degrees
            var (r, g, b) = HsvToRgb(hue, 1.0, 1.0);

            for (int y = 0; y < StripHeight; y++)
            {
                int idx = (y * StripWidth + x) * 4;
                pixels[idx + 0] = (byte)(b * 255);
                pixels[idx + 1] = (byte)(g * 255);
                pixels[idx + 2] = (byte)(r * 255);
                pixels[idx + 3] = 255;
            }
        }

        bitmap.WritePixels(new Int32Rect(0, 0, StripWidth, StripHeight), pixels, stride, 0);
        OriginalStrip.Source = bitmap;
    }

    private void UpdateCorrectedStrip()
    {
        if (!IsLoaded) return;

        var bitmap = new WriteableBitmap(StripWidth, StripHeight, 96, 96, PixelFormats.Bgra32, null);
        int stride = StripWidth * 4;
        byte[] pixels = new byte[StripWidth * StripHeight * 4];

        for (int x = 0; x < StripWidth; x++)
        {
            double hue = x; // 0-360 degrees
            var (r, g, b) = HsvToRgb(hue, 1.0, 1.0);

            // Apply CIELAB correction
            var (cr, cg, cb) = ApplyCIELABCorrection(r, g, b);

            for (int y = 0; y < StripHeight; y++)
            {
                int idx = (y * StripWidth + x) * 4;
                pixels[idx + 0] = (byte)(cb * 255);
                pixels[idx + 1] = (byte)(cg * 255);
                pixels[idx + 2] = (byte)(cr * 255);
                pixels[idx + 3] = 255;
            }
        }

        bitmap.WritePixels(new Int32Rect(0, 0, StripWidth, StripHeight), pixels, stride, 0);
        CorrectedStrip.Source = bitmap;
    }

    private (double r, double g, double b) ApplyCIELABCorrection(double r, double g, double b)
    {
        // Convert RGB to LAB
        var (L, a, bLab) = RGBToLAB(r, g, b);

        // Apply CIELAB transformation
        double originalA = a;
        double originalB = bLab;

        // Transfer between axes
        double newA = originalA + (originalB * BtoATransfer);
        double newB = originalB + (originalA * AtoBTransfer);

        // Apply enhancement
        newA *= AEnhance;
        newB *= BEnhance;

        // Lightness encoding: encode color difference as brightness
        if (LEnhance > 0)
        {
            double chromaDiff = Math.Sqrt(newA * newA + newB * newB) - Math.Sqrt(originalA * originalA + originalB * originalB);
            L += chromaDiff * LEnhance * 0.5;
            L = Math.Clamp(L, 0, 100);
        }

        // Convert back to RGB
        var (rOut, gOut, bOut) = LABToRGB(L, newA, newB);

        // Apply strength
        rOut = r + (rOut - r) * Strength;
        gOut = g + (gOut - g) * Strength;
        bOut = b + (bOut - b) * Strength;

        return (Math.Clamp(rOut, 0, 1), Math.Clamp(gOut, 0, 1), Math.Clamp(bOut, 0, 1));
    }

    #endregion

    #region Color Space Conversions

    private static (double h, double s, double v) RgbToHsv(double r, double g, double b)
    {
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double h = 0;
        if (delta > 0)
        {
            if (max == r) h = 60 * (((g - b) / delta) % 6);
            else if (max == g) h = 60 * (((b - r) / delta) + 2);
            else h = 60 * (((r - g) / delta) + 4);
        }
        if (h < 0) h += 360;

        double s = max == 0 ? 0 : delta / max;
        double v = max;

        return (h, s, v);
    }

    private static (double r, double g, double b) HsvToRgb(double h, double s, double v)
    {
        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = v - c;

        double r = 0, g = 0, b = 0;

        if (h < 60) { r = c; g = x; }
        else if (h < 120) { r = x; g = c; }
        else if (h < 180) { g = c; b = x; }
        else if (h < 240) { g = x; b = c; }
        else if (h < 300) { r = x; b = c; }
        else { r = c; b = x; }

        return (r + m, g + m, b + m);
    }

    private static (double L, double a, double b) RGBToLAB(double r, double g, double bVal)
    {
        // sRGB to linear RGB
        r = r <= 0.04045 ? r / 12.92 : Math.Pow((r + 0.055) / 1.055, 2.4);
        g = g <= 0.04045 ? g / 12.92 : Math.Pow((g + 0.055) / 1.055, 2.4);
        bVal = bVal <= 0.04045 ? bVal / 12.92 : Math.Pow((bVal + 0.055) / 1.055, 2.4);

        // RGB to XYZ (D65)
        double x = r * 0.4124564 + g * 0.3575761 + bVal * 0.1804375;
        double y = r * 0.2126729 + g * 0.7151522 + bVal * 0.0721750;
        double z = r * 0.0193339 + g * 0.1191920 + bVal * 0.9503041;

        // Normalize to D65 white point
        x /= 0.95047;
        y /= 1.00000;
        z /= 1.08883;

        // XYZ to LAB
        x = x > 0.008856 ? Math.Pow(x, 1.0 / 3) : (7.787 * x) + (16.0 / 116);
        y = y > 0.008856 ? Math.Pow(y, 1.0 / 3) : (7.787 * y) + (16.0 / 116);
        z = z > 0.008856 ? Math.Pow(z, 1.0 / 3) : (7.787 * z) + (16.0 / 116);

        double L = (116 * y) - 16;
        double a = 500 * (x - y);
        double bLab = 200 * (y - z);

        return (L, a, bLab);
    }

    private static (double r, double g, double b) LABToRGB(double L, double a, double bLab)
    {
        // LAB to XYZ
        double y = (L + 16) / 116;
        double x = a / 500 + y;
        double z = y - bLab / 200;

        double x3 = x * x * x;
        double y3 = y * y * y;
        double z3 = z * z * z;

        x = x3 > 0.008856 ? x3 : (x - 16.0 / 116) / 7.787;
        y = y3 > 0.008856 ? y3 : (y - 16.0 / 116) / 7.787;
        z = z3 > 0.008856 ? z3 : (z - 16.0 / 116) / 7.787;

        // D65 reference white
        x *= 0.95047;
        y *= 1.00000;
        z *= 1.08883;

        // XYZ to RGB
        double r = x * 3.2404542 + y * -1.5371385 + z * -0.4985314;
        double g = x * -0.9692660 + y * 1.8760108 + z * 0.0415560;
        double bOut = x * 0.0556434 + y * -0.2040259 + z * 1.0572252;

        // Gamma correction
        r = r <= 0.0031308 ? r * 12.92 : 1.055 * Math.Pow(r, 1 / 2.4) - 0.055;
        g = g <= 0.0031308 ? g * 12.92 : 1.055 * Math.Pow(g, 1 / 2.4) - 0.055;
        bOut = bOut <= 0.0031308 ? bOut * 12.92 : 1.055 * Math.Pow(bOut, 1 / 2.4) - 0.055;

        return (Math.Clamp(r, 0, 1), Math.Clamp(g, 0, 1), Math.Clamp(bOut, 0, 1));
    }

    #endregion
}
