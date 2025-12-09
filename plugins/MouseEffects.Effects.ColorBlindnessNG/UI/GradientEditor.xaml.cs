using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MouseEffects.Core.UI;
using WpfColor = System.Windows.Media.Color;
using WpfPoint = System.Windows.Point;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfCursors = System.Windows.Input.Cursors;

namespace MouseEffects.Effects.ColorBlindnessNG.UI;

/// <summary>
/// Interactive gradient editor control with draggable color handles.
/// </summary>
public partial class GradientEditor : System.Windows.Controls.UserControl
{
    private const double HandleSize = 16;
    private const double HandleOffset = 4;

    private Ellipse? _startHandle;
    private Ellipse? _endHandle;

    private enum DragMode { None, Start, End }
    private DragMode _dragMode = DragMode.None;

    public GradientEditor()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    #region Dependency Properties

    public static readonly DependencyProperty StartColorProperty = DependencyProperty.Register(
        nameof(StartColor), typeof(Vector3), typeof(GradientEditor),
        new FrameworkPropertyMetadata(new Vector3(1, 0, 0), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColorChanged));

    public static readonly DependencyProperty EndColorProperty = DependencyProperty.Register(
        nameof(EndColor), typeof(Vector3), typeof(GradientEditor),
        new FrameworkPropertyMetadata(new Vector3(0, 1, 1), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColorChanged));

    public static readonly DependencyProperty GradientTypeProperty = DependencyProperty.Register(
        nameof(GradientType), typeof(GradientType), typeof(GradientEditor),
        new FrameworkPropertyMetadata(GradientType.LinearRGB, OnColorChanged));

    public Vector3 StartColor
    {
        get => (Vector3)GetValue(StartColorProperty);
        set => SetValue(StartColorProperty, value);
    }

    public Vector3 EndColor
    {
        get => (Vector3)GetValue(EndColorProperty);
        set => SetValue(EndColorProperty, value);
    }

    public GradientType GradientType
    {
        get => (GradientType)GetValue(GradientTypeProperty);
        set => SetValue(GradientTypeProperty, value);
    }

    private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is GradientEditor editor)
        {
            editor.RenderGradient();
            editor.UpdateHandleColors();
        }
    }

    #endregion

    #region Events

    public event EventHandler? ValueChanged;

    private void RaiseValueChanged() => ValueChanged?.Invoke(this, EventArgs.Empty);

    #endregion

    #region Initialization

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CreateHandles();
        RenderGradient();
        UpdateHandlePositions();
        UpdateHandleColors();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderGradient();
        UpdateHandlePositions();
    }

    private void CreateHandles()
    {
        HandleCanvas.Children.Clear();

        _startHandle = CreateHandle("Start Color - Click to change");
        _endHandle = CreateHandle("End Color - Click to change");

        HandleCanvas.Children.Add(_startHandle);
        HandleCanvas.Children.Add(_endHandle);
    }

    private Ellipse CreateHandle(string tooltip)
    {
        var handle = new Ellipse
        {
            Width = HandleSize,
            Height = HandleSize,
            Fill = WpfBrushes.White,
            Stroke = WpfBrushes.White,
            StrokeThickness = 2,
            Cursor = WpfCursors.Hand,
            ToolTip = tooltip
        };
        handle.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = Colors.Black,
            BlurRadius = 4,
            ShadowDepth = 1,
            Opacity = 0.6
        };
        return handle;
    }

    #endregion

    #region Gradient Rendering

    private void RenderGradient()
    {
        if (ActualWidth <= 0 || ActualHeight <= 0) return;

        int width = (int)ActualWidth;
        int height = (int)ActualHeight;

        if (width <= 0) width = 200;
        if (height <= 0) height = 32;

        var bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        int stride = width * 4;
        byte[] pixels = new byte[width * height * 4];

        for (int x = 0; x < width; x++)
        {
            float t = x / (float)(width - 1);
            Vector3 color = InterpolateColor(StartColor, EndColor, t, GradientType);

            byte r = (byte)(Math.Clamp(color.X, 0, 1) * 255);
            byte g = (byte)(Math.Clamp(color.Y, 0, 1) * 255);
            byte b = (byte)(Math.Clamp(color.Z, 0, 1) * 255);

            for (int y = 0; y < height; y++)
            {
                int idx = (y * width + x) * 4;
                pixels[idx + 0] = b; // B
                pixels[idx + 1] = g; // G
                pixels[idx + 2] = r; // R
                pixels[idx + 3] = 255; // A
            }
        }

        bitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        GradientImage.Source = bitmap;
    }

    private void UpdateHandlePositions()
    {
        if (_startHandle == null || _endHandle == null) return;
        if (ActualWidth <= 0 || ActualHeight <= 0) return;

        double centerY = ActualHeight / 2 - HandleSize / 2;

        Canvas.SetLeft(_startHandle, HandleOffset);
        Canvas.SetTop(_startHandle, centerY);

        Canvas.SetLeft(_endHandle, ActualWidth - HandleSize - HandleOffset);
        Canvas.SetTop(_endHandle, centerY);
    }

    private void UpdateHandleColors()
    {
        if (_startHandle == null || _endHandle == null) return;

        _startHandle.Fill = new SolidColorBrush(Vector3ToColor(StartColor));
        _endHandle.Fill = new SolidColorBrush(Vector3ToColor(EndColor));
    }

    #endregion

    #region Mouse Handling

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(HandleCanvas);

        if (IsNearHandle(_startHandle, pos))
        {
            if (e.ClickCount == 2)
            {
                PickColor(true);
            }
            else
            {
                _dragMode = DragMode.Start;
                HandleCanvas.CaptureMouse();
            }
            e.Handled = true;
        }
        else if (IsNearHandle(_endHandle, pos))
        {
            if (e.ClickCount == 2)
            {
                PickColor(false);
            }
            else
            {
                _dragMode = DragMode.End;
                HandleCanvas.CaptureMouse();
            }
            e.Handled = true;
        }
    }

    private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // Handles are fixed at endpoints - no dragging to reposition
        // Just highlighting on hover could be added here
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragMode != DragMode.None)
        {
            // On single click release, open color picker
            if (_dragMode == DragMode.Start)
            {
                PickColor(true);
            }
            else if (_dragMode == DragMode.End)
            {
                PickColor(false);
            }

            _dragMode = DragMode.None;
            HandleCanvas.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void Canvas_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // Release capture if mouse leaves while dragging
        if (_dragMode != DragMode.None)
        {
            _dragMode = DragMode.None;
            HandleCanvas.ReleaseMouseCapture();
        }
    }

    private bool IsNearHandle(Ellipse? handle, WpfPoint pos)
    {
        if (handle == null) return false;

        double handleX = Canvas.GetLeft(handle) + HandleSize / 2;
        double handleY = Canvas.GetTop(handle) + HandleSize / 2;
        double dist = Math.Sqrt(Math.Pow(pos.X - handleX, 2) + Math.Pow(pos.Y - handleY, 2));

        return dist <= HandleSize / 2 + 5;
    }

    private void PickColor(bool isStart)
    {
        var currentColor = isStart ? StartColor : EndColor;
        var wpfColor = Vector3ToColor(currentColor);

        var dialog = new System.Windows.Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(wpfColor.R, wpfColor.G, wpfColor.B),
            FullOpen = true
        };

        DialogHelper.SuspendOverlayTopmost();
        try
        {
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var newColor = new Vector3(
                    dialog.Color.R / 255f,
                    dialog.Color.G / 255f,
                    dialog.Color.B / 255f);

                if (isStart)
                    StartColor = newColor;
                else
                    EndColor = newColor;

                RaiseValueChanged();
            }
        }
        finally
        {
            DialogHelper.ResumeOverlayTopmost();
        }
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

        if (hDiff > 0.5f)
            hStart += 1.0f;
        else if (hDiff < -0.5f)
            hEnd += 1.0f;

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

    private static WpfColor Vector3ToColor(Vector3 v) =>
        WpfColor.FromRgb(
            (byte)(Math.Clamp(v.X, 0, 1) * 255),
            (byte)(Math.Clamp(v.Y, 0, 1) * 255),
            (byte)(Math.Clamp(v.Z, 0, 1) * 255));

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

        return new Vector3(
            LinearToSRGB(r),
            LinearToSRGB(g),
            LinearToSRGB(b)
        );
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

        float L = (116f * y) - 16f;
        float a = 500f * (x - y);
        float B = 200f * (y - z);

        return new Vector3(L, a, B);
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
        return new Vector3(
            Math.Clamp(rgb.X, 0f, 1f),
            Math.Clamp(rgb.Y, 0f, 1f),
            Math.Clamp(rgb.Z, 0f, 1f)
        );
    }

    private static Vector3 RGBToHSL(Vector3 rgb)
    {
        float r = rgb.X, g = rgb.Y, b = rgb.Z;
        float max = MathF.Max(r, MathF.Max(g, b));
        float min = MathF.Min(r, MathF.Min(g, b));
        float l = (max + min) / 2f;

        if (max == min)
        {
            return new Vector3(0, 0, l);
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

    private static Vector3 HSLToRGB(Vector3 hsl)
    {
        float h = hsl.X, s = hsl.Y, l = hsl.Z;

        if (s == 0)
        {
            return new Vector3(l, l, l);
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
