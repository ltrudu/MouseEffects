using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfImage = System.Windows.Controls.Image;
using WpfPoint = System.Windows.Point;
using WpfColor = System.Windows.Media.Color;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfCursors = System.Windows.Input.Cursors;
using WpfSize = System.Windows.Size;

namespace MouseEffects.Effects.ColorBlindnessNG.UI;

public partial class HueWheelSelector : System.Windows.Controls.UserControl
{
    private const double HandleRadius = 8;
    private const double WheelThickness = 24;
    private const double ShiftArrowOffset = 15;

    private Ellipse? _startHandle;
    private Ellipse? _endHandle;
    private Ellipse? _shiftHandle;
    private TextBlock? _startSymbol;
    private TextBlock? _endSymbol;
    private TextBlock? _shiftSymbol;
    private Path? _sourceArc;
    private Path? _targetArc;
    private Path? _shiftArrow;
    private WpfImage? _wheelImage;

    private enum DragMode { None, Start, End, Shift }
    private DragMode _dragMode = DragMode.None;

    private double _centerX;
    private double _centerY;
    private double _outerRadius;
    private double _innerRadius;
    private double _handleRadius;

    public HueWheelSelector()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
    }

    #region Dependency Properties

    public static readonly DependencyProperty SourceStartProperty = DependencyProperty.Register(
        nameof(SourceStart), typeof(double), typeof(HueWheelSelector),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPropertyChanged));

    public static readonly DependencyProperty SourceEndProperty = DependencyProperty.Register(
        nameof(SourceEnd), typeof(double), typeof(HueWheelSelector),
        new FrameworkPropertyMetadata(120.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPropertyChanged));

    public static readonly DependencyProperty HueShiftProperty = DependencyProperty.Register(
        nameof(HueShift), typeof(double), typeof(HueWheelSelector),
        new FrameworkPropertyMetadata(60.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPropertyChanged));

    public static readonly DependencyProperty FalloffProperty = DependencyProperty.Register(
        nameof(Falloff), typeof(double), typeof(HueWheelSelector),
        new FrameworkPropertyMetadata(0.3, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPropertyChanged));

    public double SourceStart
    {
        get => (double)GetValue(SourceStartProperty);
        set => SetValue(SourceStartProperty, value);
    }

    public double SourceEnd
    {
        get => (double)GetValue(SourceEndProperty);
        set => SetValue(SourceEndProperty, value);
    }

    public double HueShift
    {
        get => (double)GetValue(HueShiftProperty);
        set => SetValue(HueShiftProperty, value);
    }

    public double Falloff
    {
        get => (double)GetValue(FalloffProperty);
        set => SetValue(FalloffProperty, value);
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HueWheelSelector selector)
            selector.UpdateVisuals();
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
        _outerRadius = (size / 2) - HandleRadius - 2;
        _innerRadius = _outerRadius - WheelThickness;
        _handleRadius = (_outerRadius + _innerRadius) / 2;
    }

    private void CreateVisuals()
    {
        WheelCanvas.Children.Clear();
        UpdateDimensions();

        if (_outerRadius <= 0) return;

        // Create color wheel
        _wheelImage = CreateColorWheelImage();
        WheelCanvas.Children.Add(_wheelImage);

        // Create target arc (shifted position) - draw first so it's behind
        _targetArc = new Path
        {
            Stroke = new SolidColorBrush(WpfColor.FromArgb(180, 100, 200, 255)),
            StrokeThickness = WheelThickness - 4,
            StrokeDashArray = new DoubleCollection { 2, 2 },
            Fill = WpfBrushes.Transparent
        };
        WheelCanvas.Children.Add(_targetArc);

        // Create source arc overlay
        _sourceArc = new Path
        {
            Stroke = new SolidColorBrush(WpfColor.FromArgb(200, 255, 255, 255)),
            StrokeThickness = 3,
            Fill = WpfBrushes.Transparent
        };
        WheelCanvas.Children.Add(_sourceArc);

        // Create shift arrow
        _shiftArrow = new Path
        {
            Stroke = new SolidColorBrush(WpfColor.FromArgb(220, 255, 200, 100)),
            StrokeThickness = 2,
            Fill = new SolidColorBrush(WpfColor.FromArgb(180, 255, 200, 100))
        };
        WheelCanvas.Children.Add(_shiftArrow);

        // Create handles
        _startHandle = CreateHandle(Colors.LimeGreen, "Source Start - Drag to adjust");
        _endHandle = CreateHandle(Colors.OrangeRed, "Source End - Drag to adjust");
        _shiftHandle = CreateHandle(Colors.Gold, "Hue Shift - Drag to adjust shift amount");

        WheelCanvas.Children.Add(_startHandle);
        WheelCanvas.Children.Add(_endHandle);
        WheelCanvas.Children.Add(_shiftHandle);

        // Create accessibility symbols (for colorblind users)
        _startSymbol = CreateHandleSymbol(">");
        _endSymbol = CreateHandleSymbol("<");
        _shiftSymbol = CreateHandleSymbol("x");

        WheelCanvas.Children.Add(_startSymbol);
        WheelCanvas.Children.Add(_endSymbol);
        WheelCanvas.Children.Add(_shiftSymbol);
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
            Cursor = WpfCursors.Hand,
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

    private WpfImage CreateColorWheelImage()
    {
        int size = (int)(_outerRadius * 2) + 4;
        if (size <= 0) size = 200;

        var bitmap = new System.Windows.Media.Imaging.WriteableBitmap(size, size, 96, 96, PixelFormats.Bgra32, null);
        int stride = size * 4;
        byte[] pixels = new byte[size * size * 4];

        double centerX = size / 2.0;
        double centerY = size / 2.0;
        double outerR = _outerRadius;
        double innerR = _innerRadius;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                double dx = x - centerX;
                double dy = y - centerY;
                double distance = Math.Sqrt(dx * dx + dy * dy);

                int idx = (y * size + x) * 4;

                if (distance >= innerR && distance <= outerR)
                {
                    // Calculate hue from angle (0° at right, counter-clockwise)
                    double angle = Math.Atan2(-dy, dx) * 180.0 / Math.PI;
                    if (angle < 0) angle += 360;

                    // Convert HSV to RGB (S=1, V=1)
                    var (r, g, b) = HsvToRgb(angle, 1.0, 1.0);

                    // Anti-aliasing at edges
                    double alpha = 1.0;
                    double edgeDist = Math.Min(distance - innerR, outerR - distance);
                    if (edgeDist < 1.5) alpha = edgeDist / 1.5;

                    pixels[idx + 0] = (byte)(b * 255); // B
                    pixels[idx + 1] = (byte)(g * 255); // G
                    pixels[idx + 2] = (byte)(r * 255); // R
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
        if (_startHandle == null || _endHandle == null || _shiftHandle == null) return;
        if (_sourceArc == null || _targetArc == null || _shiftArrow == null) return;

        // Position start handle
        var startPos = AngleToPoint(SourceStart, _handleRadius);
        Canvas.SetLeft(_startHandle, startPos.X - HandleRadius);
        Canvas.SetTop(_startHandle, startPos.Y - HandleRadius);

        // Position end handle
        var endPos = AngleToPoint(SourceEnd, _handleRadius);
        Canvas.SetLeft(_endHandle, endPos.X - HandleRadius);
        Canvas.SetTop(_endHandle, endPos.Y - HandleRadius);

        // Position shift handle (at midpoint of source arc, offset outward)
        double midAngle = GetMidAngle(SourceStart, SourceEnd);
        double shiftedMidAngle = midAngle + HueShift;
        var shiftPos = AngleToPoint(shiftedMidAngle, _outerRadius + ShiftArrowOffset);
        Canvas.SetLeft(_shiftHandle, shiftPos.X - HandleRadius);
        Canvas.SetTop(_shiftHandle, shiftPos.Y - HandleRadius);

        // Position accessibility symbols centered on handles
        if (_startSymbol != null)
        {
            Canvas.SetLeft(_startSymbol, startPos.X - 4);
            Canvas.SetTop(_startSymbol, startPos.Y - 7);
        }
        if (_endSymbol != null)
        {
            Canvas.SetLeft(_endSymbol, endPos.X - 4);
            Canvas.SetTop(_endSymbol, endPos.Y - 7);
        }
        if (_shiftSymbol != null)
        {
            Canvas.SetLeft(_shiftSymbol, shiftPos.X - 4);
            Canvas.SetTop(_shiftSymbol, shiftPos.Y - 7);
        }

        // Update source arc
        _sourceArc.Data = CreateArcGeometry(SourceStart, SourceEnd, _handleRadius);

        // Update target arc (where colors shift to)
        _targetArc.Data = CreateArcGeometry(SourceStart + HueShift, SourceEnd + HueShift, _handleRadius);

        // Update shift arrow
        UpdateShiftArrow(midAngle, shiftedMidAngle);
    }

    private void UpdateShiftArrow(double fromAngle, double toAngle)
    {
        if (_shiftArrow == null) return;

        var fromPoint = AngleToPoint(fromAngle, _outerRadius + 5);
        var toPoint = AngleToPoint(toAngle, _outerRadius + 5);

        // Create curved arrow
        var pathFigure = new PathFigure { StartPoint = fromPoint };

        // Calculate control point for curve
        double midAngle = (fromAngle + toAngle) / 2;
        double curveRadius = _outerRadius + ShiftArrowOffset + 10;
        var controlPoint = AngleToPoint(midAngle, curveRadius);

        pathFigure.Segments.Add(new QuadraticBezierSegment(controlPoint, toPoint, true));

        // Add arrowhead
        double arrowAngle = Math.Atan2(toPoint.Y - controlPoint.Y, toPoint.X - controlPoint.X);
        double arrowSize = 8;
        var arrow1 = new WpfPoint(
            toPoint.X - arrowSize * Math.Cos(arrowAngle - Math.PI / 6),
            toPoint.Y - arrowSize * Math.Sin(arrowAngle - Math.PI / 6));
        var arrow2 = new WpfPoint(
            toPoint.X - arrowSize * Math.Cos(arrowAngle + Math.PI / 6),
            toPoint.Y - arrowSize * Math.Sin(arrowAngle + Math.PI / 6));

        var arrowFigure = new PathFigure { StartPoint = arrow1 };
        arrowFigure.Segments.Add(new LineSegment(toPoint, true));
        arrowFigure.Segments.Add(new LineSegment(arrow2, true));
        arrowFigure.IsClosed = true;

        var geometry = new PathGeometry();
        geometry.Figures.Add(pathFigure);
        geometry.Figures.Add(arrowFigure);

        _shiftArrow.Data = geometry;
        _shiftArrow.Visibility = Math.Abs(HueShift) > 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private Geometry CreateArcGeometry(double startAngle, double endAngle, double radius)
    {
        // Normalize angles
        startAngle = NormalizeAngle(startAngle);
        endAngle = NormalizeAngle(endAngle);

        var startPoint = AngleToPoint(startAngle, radius);
        var endPoint = AngleToPoint(endAngle, radius);

        // Calculate sweep angle
        double sweep = endAngle - startAngle;
        if (sweep < 0) sweep += 360;

        bool isLargeArc = sweep > 180;

        var arcSegment = new ArcSegment(
            endPoint,
            new WpfSize(radius, radius),
            0,
            isLargeArc,
            SweepDirection.Clockwise,
            true);

        var pathFigure = new PathFigure
        {
            StartPoint = startPoint,
            IsClosed = false
        };
        pathFigure.Segments.Add(arcSegment);

        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pathFigure);

        return pathGeometry;
    }

    #endregion

    #region Mouse Handling

    private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(WheelCanvas);

        // Check which handle was clicked
        if (IsNearHandle(_startHandle, pos))
        {
            _dragMode = DragMode.Start;
        }
        else if (IsNearHandle(_endHandle, pos))
        {
            _dragMode = DragMode.End;
        }
        else if (IsNearHandle(_shiftHandle, pos))
        {
            _dragMode = DragMode.Shift;
        }
        else
        {
            _dragMode = DragMode.None;
            return;
        }

        WheelCanvas.CaptureMouse();
        e.Handled = true;
    }

    private void Canvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_dragMode == DragMode.None) return;

        var pos = e.GetPosition(WheelCanvas);
        double angle = PointToAngle(pos);

        switch (_dragMode)
        {
            case DragMode.Start:
                SourceStart = Math.Round(angle);
                break;
            case DragMode.End:
                SourceEnd = Math.Round(angle);
                break;
            case DragMode.Shift:
                double midAngle = GetMidAngle(SourceStart, SourceEnd);
                double newShift = angle - midAngle;
                // Normalize to -180 to 180
                while (newShift > 180) newShift -= 360;
                while (newShift < -180) newShift += 360;
                HueShift = Math.Round(newShift);
                break;
        }

        RaiseValueChanged();
        e.Handled = true;
    }

    private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_dragMode != DragMode.None)
        {
            _dragMode = DragMode.None;
            WheelCanvas.ReleaseMouseCapture();
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

    #region Utility Methods

    private WpfPoint AngleToPoint(double angleDegrees, double radius)
    {
        // Convert to radians, 0° at top, clockwise
        double angleRad = (angleDegrees - 90) * Math.PI / 180.0;
        return new WpfPoint(
            _centerX + radius * Math.Cos(angleRad),
            _centerY + radius * Math.Sin(angleRad));
    }

    private double PointToAngle(WpfPoint pos)
    {
        double dx = pos.X - _centerX;
        double dy = pos.Y - _centerY;
        double angle = Math.Atan2(dy, dx) * 180.0 / Math.PI + 90;
        return NormalizeAngle(angle);
    }

    private static double NormalizeAngle(double angle)
    {
        while (angle < 0) angle += 360;
        while (angle >= 360) angle -= 360;
        return angle;
    }

    private static double GetMidAngle(double start, double end)
    {
        double sweep = end - start;
        if (sweep < 0) sweep += 360;
        return NormalizeAngle(start + sweep / 2);
    }

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

    #endregion
}
