using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WpfColor = System.Windows.Media.Color;
using WpfPoint = System.Windows.Point;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfImage = System.Windows.Controls.Image;

namespace MouseEffects.Effects.ColorBlindnessNG.UI;

/// <summary>
/// Interactive CIELAB a*b* axis control showing color plane with transfer arrows.
/// </summary>
public partial class CIELABAxisControl : System.Windows.Controls.UserControl
{
    private const double HandleRadius = 8;
    private const double ArrowHeadSize = 10;

    private WpfImage? _colorPlaneImage;
    private Path? _atobArrow;
    private Path? _btoaArrow;
    private Ellipse? _atobHandle;
    private Ellipse? _btoaHandle;
    private TextBlock? _btoaSymbol;
    private Line? _aAxis;
    private Line? _bAxis;
    private TextBlock? _aLabel;
    private TextBlock? _bLabel;

    private enum DragMode { None, AtoB, BtoA }
    private DragMode _dragMode = DragMode.None;

    private double _centerX;
    private double _centerY;
    private double _radius;

    public CIELABAxisControl()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    #region Dependency Properties

    public static readonly DependencyProperty AtoBTransferProperty = DependencyProperty.Register(
        nameof(AtoBTransfer), typeof(double), typeof(CIELABAxisControl),
        new FrameworkPropertyMetadata(0.5, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPropertyChanged));

    public static readonly DependencyProperty BtoATransferProperty = DependencyProperty.Register(
        nameof(BtoATransfer), typeof(double), typeof(CIELABAxisControl),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPropertyChanged));

    public static readonly DependencyProperty AEnhanceProperty = DependencyProperty.Register(
        nameof(AEnhance), typeof(double), typeof(CIELABAxisControl),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPropertyChanged));

    public static readonly DependencyProperty BEnhanceProperty = DependencyProperty.Register(
        nameof(BEnhance), typeof(double), typeof(CIELABAxisControl),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPropertyChanged));

    public static readonly DependencyProperty LEnhanceProperty = DependencyProperty.Register(
        nameof(LEnhance), typeof(double), typeof(CIELABAxisControl),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPropertyChanged));

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

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CIELABAxisControl control)
            control.UpdateVisuals();
    }

    #endregion

    #region Events

    public event EventHandler? ValueChanged;

    private void RaiseValueChanged() => ValueChanged?.Invoke(this, EventArgs.Empty);

    #endregion

    #region Initialization

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CreateVisuals();
        UpdateVisuals();
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateDimensions();
        CreateVisuals();
        UpdateVisuals();
    }

    private void UpdateDimensions()
    {
        double size = Math.Min(ActualWidth, ActualHeight);
        _centerX = ActualWidth / 2;
        _centerY = ActualHeight / 2;
        _radius = (size / 2) - HandleRadius - 10;
    }

    private void CreateVisuals()
    {
        AxisCanvas.Children.Clear();
        UpdateDimensions();

        if (_radius <= 0) return;

        // Create color plane image
        _colorPlaneImage = CreateColorPlaneImage();
        AxisCanvas.Children.Add(_colorPlaneImage);

        // Create axis lines
        _aAxis = new Line
        {
            X1 = _centerX - _radius - 5,
            Y1 = _centerY,
            X2 = _centerX + _radius + 5,
            Y2 = _centerY,
            Stroke = new SolidColorBrush(WpfColor.FromArgb(100, 255, 255, 255)),
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 4, 2 }
        };
        AxisCanvas.Children.Add(_aAxis);

        _bAxis = new Line
        {
            X1 = _centerX,
            Y1 = _centerY - _radius - 5,
            X2 = _centerX,
            Y2 = _centerY + _radius + 5,
            Stroke = new SolidColorBrush(WpfColor.FromArgb(100, 255, 255, 255)),
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 4, 2 }
        };
        AxisCanvas.Children.Add(_bAxis);

        // Create axis labels
        _aLabel = new TextBlock
        {
            Text = "a* (Red↔Green)",
            FontSize = 9,
            Foreground = WpfBrushes.White
        };
        Canvas.SetLeft(_aLabel, _centerX + _radius - 40);
        Canvas.SetTop(_aLabel, _centerY + 5);
        AxisCanvas.Children.Add(_aLabel);

        _bLabel = new TextBlock
        {
            Text = "b* (Yellow↔Blue)",
            FontSize = 9,
            Foreground = WpfBrushes.White
        };
        Canvas.SetLeft(_bLabel, _centerX + 5);
        Canvas.SetTop(_bLabel, _centerY - _radius - 15);
        AxisCanvas.Children.Add(_bLabel);

        // Create transfer arrows
        _atobArrow = new Path
        {
            Stroke = new SolidColorBrush(WpfColor.FromRgb(255, 150, 50)),
            StrokeThickness = 3,
            Fill = new SolidColorBrush(WpfColor.FromRgb(255, 150, 50))
        };
        AxisCanvas.Children.Add(_atobArrow);

        _btoaArrow = new Path
        {
            Stroke = new SolidColorBrush(WpfColor.FromRgb(50, 150, 255)),
            StrokeThickness = 3,
            Fill = new SolidColorBrush(WpfColor.FromRgb(50, 150, 255))
        };
        AxisCanvas.Children.Add(_btoaArrow);

        // Create handles
        _atobHandle = CreateHandle(WpfColor.FromRgb(255, 150, 50), "a*→b* Transfer\nDrag up/down to adjust");
        _btoaHandle = CreateHandle(WpfColor.FromRgb(50, 150, 255), "b*→a* Transfer\nDrag left/right to adjust");

        AxisCanvas.Children.Add(_atobHandle);
        AxisCanvas.Children.Add(_btoaHandle);

        // Create accessibility symbol for b*→a* handle (for colorblind users)
        // a*→b* (orange) has no symbol, b*→a* (blue) has "x"
        _btoaSymbol = CreateHandleSymbol("x");
        AxisCanvas.Children.Add(_btoaSymbol);
    }

    private Ellipse CreateHandle(WpfColor color, string tooltip)
    {
        var handle = new Ellipse
        {
            Width = HandleRadius * 2,
            Height = HandleRadius * 2,
            Fill = new SolidColorBrush(color),
            Stroke = WpfBrushes.White,
            StrokeThickness = 2,
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = tooltip
        };
        handle.Effect = new System.Windows.Media.Effects.DropShadowEffect
        {
            Color = Colors.Black,
            BlurRadius = 4,
            ShadowDepth = 1,
            Opacity = 0.5
        };
        return handle;
    }

    private TextBlock CreateHandleSymbol(string symbol)
    {
        return new TextBlock
        {
            Text = symbol,
            Foreground = WpfBrushes.White,
            FontSize = 10,
            FontWeight = FontWeights.Bold,
            IsHitTestVisible = false,
            TextAlignment = TextAlignment.Center
        };
    }

    private WpfImage CreateColorPlaneImage()
    {
        int size = (int)(_radius * 2) + 4;
        if (size <= 0) size = 150;

        var bitmap = new WriteableBitmap(size, size, 96, 96, PixelFormats.Bgra32, null);
        int stride = size * 4;
        byte[] pixels = new byte[size * size * 4];

        double center = size / 2.0;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double dx = x - center;
                double dy = y - center;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                int idx = (y * size + x) * 4;

                if (distance <= _radius)
                {
                    // Map to a*b* coordinates (-128 to 128 range, normalized)
                    double a = (dx / _radius) * 128;
                    double b = (-dy / _radius) * 128; // Invert Y for b* (up is positive)

                    // Convert LAB to RGB (L=65 for good visibility)
                    var (r, g, bVal) = LABToRGB(65, a, b);

                    // Anti-aliasing at edge
                    double alpha = 1.0;
                    double edgeDist = _radius - distance;
                    if (edgeDist < 1.5) alpha = edgeDist / 1.5;

                    pixels[idx + 0] = (byte)(bVal * 255); // B
                    pixels[idx + 1] = (byte)(g * 255);    // G
                    pixels[idx + 2] = (byte)(r * 255);    // R
                    pixels[idx + 3] = (byte)(alpha * 255); // A
                }
                else
                {
                    pixels[idx + 0] = 0;
                    pixels[idx + 1] = 0;
                    pixels[idx + 2] = 0;
                    pixels[idx + 3] = 0;
                }
            }
        }

        bitmap.WritePixels(new Int32Rect(0, 0, size, size), pixels, stride, 0);

        var image = new WpfImage
        {
            Source = bitmap,
            Width = size,
            Height = size
        };
        Canvas.SetLeft(image, _centerX - size / 2.0);
        Canvas.SetTop(image, _centerY - size / 2.0);

        return image;
    }

    #endregion

    #region Visual Updates

    private void UpdateVisuals()
    {
        if (_atobArrow == null || _btoaArrow == null) return;
        if (_atobHandle == null || _btoaHandle == null) return;

        // A→B Transfer arrow (horizontal to vertical, on right side)
        double atobLength = Math.Abs(AtoBTransfer) * _radius * 0.6;
        double atobDirection = AtoBTransfer >= 0 ? -1 : 1; // Negative Y = up (positive b*)

        if (Math.Abs(AtoBTransfer) > 0.01)
        {
            var atobStart = new WpfPoint(_centerX + _radius * 0.3, _centerY);
            var atobEnd = new WpfPoint(_centerX + _radius * 0.3, _centerY + atobDirection * atobLength);
            _atobArrow.Data = CreateArrowGeometry(atobStart, atobEnd);
            _atobArrow.Visibility = Visibility.Visible;
        }
        else
        {
            _atobArrow.Visibility = Visibility.Collapsed;
        }

        // Position A→B handle (no symbol - orange handle has no accessibility marker)
        double atobHandleX = _centerX + _radius * 0.3;
        double atobHandleY = _centerY - AtoBTransfer * _radius * 0.6;
        Canvas.SetLeft(_atobHandle, atobHandleX - HandleRadius);
        Canvas.SetTop(_atobHandle, atobHandleY - HandleRadius);

        // B→A Transfer arrow (vertical to horizontal, on top)
        double btoaLength = Math.Abs(BtoATransfer) * _radius * 0.6;
        double btoaDirection = BtoATransfer >= 0 ? 1 : -1; // Positive X = right (positive a*)

        if (Math.Abs(BtoATransfer) > 0.01)
        {
            var btoaStart = new WpfPoint(_centerX, _centerY - _radius * 0.3);
            var btoaEnd = new WpfPoint(_centerX + btoaDirection * btoaLength, _centerY - _radius * 0.3);
            _btoaArrow.Data = CreateArrowGeometry(btoaStart, btoaEnd);
            _btoaArrow.Visibility = Visibility.Visible;
        }
        else
        {
            _btoaArrow.Visibility = Visibility.Collapsed;
        }

        // Position B→A handle
        double btoaHandleX = _centerX + BtoATransfer * _radius * 0.6;
        double btoaHandleY = _centerY - _radius * 0.3;
        Canvas.SetLeft(_btoaHandle, btoaHandleX - HandleRadius);
        Canvas.SetTop(_btoaHandle, btoaHandleY - HandleRadius);

        // Position B→A symbol (x) centered on handle
        if (_btoaSymbol != null)
        {
            Canvas.SetLeft(_btoaSymbol, btoaHandleX - 4);
            Canvas.SetTop(_btoaSymbol, btoaHandleY - 7);
        }

        // Update axis lines based on enhancement
        if (_aAxis != null && AEnhance != 1.0)
        {
            _aAxis.StrokeThickness = 1 + (AEnhance - 1) * 2;
            _aAxis.Stroke = new SolidColorBrush(WpfColor.FromArgb((byte)(100 + (AEnhance - 1) * 77), 255, 100, 100));
        }
        else if (_aAxis != null)
        {
            _aAxis.StrokeThickness = 1;
            _aAxis.Stroke = new SolidColorBrush(WpfColor.FromArgb(100, 255, 255, 255));
        }

        if (_bAxis != null && BEnhance != 1.0)
        {
            _bAxis.StrokeThickness = 1 + (BEnhance - 1) * 2;
            _bAxis.Stroke = new SolidColorBrush(WpfColor.FromArgb((byte)(100 + (BEnhance - 1) * 77), 100, 100, 255));
        }
        else if (_bAxis != null)
        {
            _bAxis.StrokeThickness = 1;
            _bAxis.Stroke = new SolidColorBrush(WpfColor.FromArgb(100, 255, 255, 255));
        }
    }

    private Geometry CreateArrowGeometry(WpfPoint start, WpfPoint end)
    {
        var pathFigure = new PathFigure { StartPoint = start };
        pathFigure.Segments.Add(new LineSegment(end, true));

        // Calculate arrowhead
        double angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
        var arrow1 = new WpfPoint(
            end.X - ArrowHeadSize * Math.Cos(angle - Math.PI / 6),
            end.Y - ArrowHeadSize * Math.Sin(angle - Math.PI / 6));
        var arrow2 = new WpfPoint(
            end.X - ArrowHeadSize * Math.Cos(angle + Math.PI / 6),
            end.Y - ArrowHeadSize * Math.Sin(angle + Math.PI / 6));

        var arrowFigure = new PathFigure { StartPoint = arrow1 };
        arrowFigure.Segments.Add(new LineSegment(end, true));
        arrowFigure.Segments.Add(new LineSegment(arrow2, true));
        arrowFigure.IsClosed = true;

        var geometry = new PathGeometry();
        geometry.Figures.Add(pathFigure);
        geometry.Figures.Add(arrowFigure);

        return geometry;
    }

    #endregion

    #region Mouse Handling

    private void Canvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(AxisCanvas);

        if (IsNearHandle(_atobHandle, pos))
        {
            _dragMode = DragMode.AtoB;
            AxisCanvas.CaptureMouse();
            e.Handled = true;
        }
        else if (IsNearHandle(_btoaHandle, pos))
        {
            _dragMode = DragMode.BtoA;
            AxisCanvas.CaptureMouse();
            e.Handled = true;
        }
    }

    private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_dragMode == DragMode.None) return;

        var pos = e.GetPosition(AxisCanvas);

        switch (_dragMode)
        {
            case DragMode.AtoB:
                // Vertical drag for A→B transfer
                double atobValue = -((pos.Y - _centerY) / (_radius * 0.6));
                AtoBTransfer = Math.Clamp(Math.Round(atobValue, 2), -1, 1);
                break;

            case DragMode.BtoA:
                // Horizontal drag for B→A transfer
                double btoaValue = (pos.X - _centerX) / (_radius * 0.6);
                BtoATransfer = Math.Clamp(Math.Round(btoaValue, 2), -1, 1);
                break;
        }

        RaiseValueChanged();
        e.Handled = true;
    }

    private void Canvas_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_dragMode != DragMode.None)
        {
            _dragMode = DragMode.None;
            AxisCanvas.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void Canvas_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        // Don't release if we're dragging (mouse capture will handle it)
    }

    private bool IsNearHandle(Ellipse? handle, WpfPoint pos)
    {
        if (handle == null) return false;

        double handleX = Canvas.GetLeft(handle) + HandleRadius;
        double handleY = Canvas.GetTop(handle) + HandleRadius;
        double dist = Math.Sqrt(Math.Pow(pos.X - handleX, 2) + Math.Pow(pos.Y - handleY, 2));

        return dist <= HandleRadius + 5;
    }

    #endregion

    #region Color Space Conversions

    private static (double r, double g, double b) LABToRGB(double L, double a, double bVal)
    {
        // LAB to XYZ
        double y = (L + 16) / 116;
        double x = a / 500 + y;
        double z = y - bVal / 200;

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
