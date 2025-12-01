using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfColor = System.Windows.Media.Color;
using WpfPoint = System.Windows.Point;

namespace MouseEffects.Effects.ColorBlindness.UI;

public partial class CurveEditor : System.Windows.Controls.UserControl
{
    private const double PointRadius = 6;
    private const double HitRadius = 10;

    private CurveData _masterCurve = CurveData.CreateLinear();
    private CurveData _redCurve = CurveData.CreateLinear();
    private CurveData _greenCurve = CurveData.CreateLinear();
    private CurveData _blueCurve = CurveData.CreateLinear();

    private int _selectedChannel = 0; // 0=RGB, 1=Red, 2=Green, 3=Blue
    private int _draggedPointIndex = -1;
    private bool _isDragging;

    /// <summary>
    /// Event raised when any curve is modified.
    /// </summary>
    public event EventHandler? CurveChanged;

    public CurveEditor()
    {
        InitializeComponent();
        Loaded += CurveEditor_Loaded;
        SizeChanged += CurveEditor_SizeChanged;
    }

    private void CurveEditor_Loaded(object sender, RoutedEventArgs e)
    {
        RedrawCurves();
    }

    private void CurveEditor_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RedrawCurves();
    }

    /// <summary>
    /// Gets or sets the master (RGB) curve.
    /// </summary>
    public CurveData MasterCurve
    {
        get => _masterCurve;
        set
        {
            _masterCurve = value ?? CurveData.CreateLinear();
            RedrawCurves();
        }
    }

    /// <summary>
    /// Gets or sets the red channel curve.
    /// </summary>
    public CurveData RedCurve
    {
        get => _redCurve;
        set
        {
            _redCurve = value ?? CurveData.CreateLinear();
            RedrawCurves();
        }
    }

    /// <summary>
    /// Gets or sets the green channel curve.
    /// </summary>
    public CurveData GreenCurve
    {
        get => _greenCurve;
        set
        {
            _greenCurve = value ?? CurveData.CreateLinear();
            RedrawCurves();
        }
    }

    /// <summary>
    /// Gets or sets the blue channel curve.
    /// </summary>
    public CurveData BlueCurve
    {
        get => _blueCurve;
        set
        {
            _blueCurve = value ?? CurveData.CreateLinear();
            RedrawCurves();
        }
    }

    private CurveData GetCurrentCurve()
    {
        return _selectedChannel switch
        {
            0 => _masterCurve,
            1 => _redCurve,
            2 => _greenCurve,
            3 => _blueCurve,
            _ => _masterCurve
        };
    }

    private void SetCurrentCurve(CurveData curve)
    {
        switch (_selectedChannel)
        {
            case 0:
                _masterCurve = curve;
                break;
            case 1:
                _redCurve = curve;
                break;
            case 2:
                _greenCurve = curve;
                break;
            case 3:
                _blueCurve = curve;
                break;
        }
    }

    private WpfColor GetChannelColor(int channel)
    {
        return channel switch
        {
            0 => GetThemeAwareMasterColor(),
            1 => WpfColor.FromRgb(255, 107, 107),
            2 => WpfColor.FromRgb(107, 255, 107),
            3 => WpfColor.FromRgb(107, 158, 255),
            _ => GetThemeAwareMasterColor()
        };
    }

    /// <summary>
    /// Gets a master curve color that works with both light and dark themes.
    /// </summary>
    private WpfColor GetThemeAwareMasterColor()
    {
        // Check if we're in light theme by looking at the background
        if (TryFindResource("SystemControlForegroundBaseHighBrush") is SolidColorBrush brush)
        {
            return brush.Color;
        }
        // Fallback to a neutral gray that works on both themes
        return WpfColor.FromRgb(100, 100, 100);
    }

    /// <summary>
    /// Gets a color for grid lines that adapts to the current theme.
    /// </summary>
    private WpfColor GetGridColor()
    {
        if (TryFindResource("SystemControlForegroundBaseMediumLowBrush") is SolidColorBrush brush)
        {
            var color = brush.Color;
            return WpfColor.FromArgb(80, color.R, color.G, color.B);
        }
        return WpfColor.FromArgb(80, 128, 128, 128);
    }

    /// <summary>
    /// Gets a color for the reference diagonal line that adapts to the current theme.
    /// </summary>
    private WpfColor GetReferenceLineColor()
    {
        if (TryFindResource("SystemControlForegroundBaseMediumBrush") is SolidColorBrush brush)
        {
            var color = brush.Color;
            return WpfColor.FromArgb(100, color.R, color.G, color.B);
        }
        return WpfColor.FromArgb(100, 128, 128, 128);
    }

    private void RedrawCurves()
    {
        if (CurveCanvas == null) return;

        CurveCanvas.Children.Clear();

        double width = CurveCanvas.ActualWidth;
        double height = CurveCanvas.ActualHeight;

        if (width <= 0 || height <= 0) return;

        // Draw grid
        DrawGrid(width, height);

        // Draw diagonal reference line
        DrawReferenceLine(width, height);

        // Draw curves for visible channels
        if (ShowRedCheck?.IsChecked == true && _selectedChannel != 1)
            DrawCurve(_redCurve, WpfColor.FromArgb(128, 255, 107, 107), width, height, false);

        if (ShowGreenCheck?.IsChecked == true && _selectedChannel != 2)
            DrawCurve(_greenCurve, WpfColor.FromArgb(128, 107, 255, 107), width, height, false);

        if (ShowBlueCheck?.IsChecked == true && _selectedChannel != 3)
            DrawCurve(_blueCurve, WpfColor.FromArgb(128, 107, 107, 255), width, height, false);

        // Draw master curve if not editing a specific channel
        if (_selectedChannel == 0)
            DrawCurve(_masterCurve, Colors.White, width, height, false);

        // Draw the active curve with control points
        var currentCurve = GetCurrentCurve();
        var currentColor = GetChannelColor(_selectedChannel);
        DrawCurve(currentCurve, currentColor, width, height, true);
    }

    private void DrawGrid(double width, double height)
    {
        var gridBrush = new SolidColorBrush(GetGridColor());

        // Draw vertical and horizontal lines at 25% intervals
        for (int i = 1; i < 4; i++)
        {
            double x = width * i / 4;
            double y = height * i / 4;

            var vLine = new Line
            {
                X1 = x, Y1 = 0,
                X2 = x, Y2 = height,
                Stroke = gridBrush,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 4 }
            };
            CurveCanvas.Children.Add(vLine);

            var hLine = new Line
            {
                X1 = 0, Y1 = y,
                X2 = width, Y2 = y,
                Stroke = gridBrush,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 4, 4 }
            };
            CurveCanvas.Children.Add(hLine);
        }
    }

    private void DrawReferenceLine(double width, double height)
    {
        var line = new Line
        {
            X1 = 0, Y1 = height,
            X2 = width, Y2 = 0,
            Stroke = new SolidColorBrush(GetReferenceLineColor()),
            StrokeThickness = 1,
            StrokeDashArray = new DoubleCollection { 2, 2 }
        };
        CurveCanvas.Children.Add(line);
    }

    private void DrawCurve(CurveData curve, WpfColor color, double width, double height, bool showPoints)
    {
        if (curve.ControlPoints.Count < 2) return;

        // Draw the curve as a polyline by sampling
        var points = new PointCollection();
        const int samples = 100;

        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            float value = curve.Evaluate(t);

            double x = t * width;
            double y = height - (value * height);
            points.Add(new WpfPoint(x, y));
        }

        var polyline = new Polyline
        {
            Points = points,
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 2,
            StrokeLineJoin = PenLineJoin.Round
        };
        CurveCanvas.Children.Add(polyline);

        // Draw control points if this is the active curve
        if (showPoints)
        {
            var sortedPoints = curve.ControlPoints.OrderBy(p => p.X).ToList();
            for (int i = 0; i < sortedPoints.Count; i++)
            {
                var point = sortedPoints[i];
                double x = point.X * width;
                double y = height - (point.Y * height);

                var ellipse = new Ellipse
                {
                    Width = PointRadius * 2,
                    Height = PointRadius * 2,
                    Fill = new SolidColorBrush(color),
                    Stroke = new SolidColorBrush(GetThemeAwareMasterColor()),
                    StrokeThickness = 1
                };

                Canvas.SetLeft(ellipse, x - PointRadius);
                Canvas.SetTop(ellipse, y - PointRadius);
                CurveCanvas.Children.Add(ellipse);
            }
        }
    }

    private WpfPoint CanvasToNormalized(WpfPoint canvasPoint)
    {
        double width = CurveCanvas.ActualWidth;
        double height = CurveCanvas.ActualHeight;

        return new WpfPoint(
            Math.Clamp(canvasPoint.X / width, 0, 1),
            Math.Clamp(1.0 - (canvasPoint.Y / height), 0, 1)
        );
    }

    private int FindPointAtPosition(CurveData curve, WpfPoint canvasPoint)
    {
        double width = CurveCanvas.ActualWidth;
        double height = CurveCanvas.ActualHeight;

        var sortedPoints = curve.ControlPoints.OrderBy(p => p.X).ToList();
        for (int i = 0; i < sortedPoints.Count; i++)
        {
            var point = sortedPoints[i];
            double px = point.X * width;
            double py = height - (point.Y * height);

            double dist = Math.Sqrt(Math.Pow(canvasPoint.X - px, 2) + Math.Pow(canvasPoint.Y - py, 2));
            if (dist <= HitRadius)
            {
                // Find the actual index in the unsorted list
                return curve.ControlPoints.FindIndex(p =>
                    Math.Abs(p.X - point.X) < 0.001f && Math.Abs(p.Y - point.Y) < 0.001f);
            }
        }

        return -1;
    }

    private void CurveCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var canvasPoint = e.GetPosition(CurveCanvas);
        var curve = GetCurrentCurve();

        _draggedPointIndex = FindPointAtPosition(curve, canvasPoint);

        if (_draggedPointIndex >= 0)
        {
            // Start dragging existing point
            _isDragging = true;
            CurveCanvas.CaptureMouse();
        }
        else
        {
            // Add new point
            var normalizedPoint = CanvasToNormalized(canvasPoint);
            curve.ControlPoints.Add(new Vector2((float)normalizedPoint.X, (float)normalizedPoint.Y));

            // Find the newly added point for dragging
            _draggedPointIndex = curve.ControlPoints.Count - 1;
            _isDragging = true;
            CurveCanvas.CaptureMouse();

            RedrawCurves();
            OnCurveChanged();
        }
    }

    private void CurveCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            _draggedPointIndex = -1;
            CurveCanvas.ReleaseMouseCapture();
        }
    }

    private void CurveCanvas_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var canvasPoint = e.GetPosition(CurveCanvas);
        var normalizedPoint = CanvasToNormalized(canvasPoint);

        // Update coordinates display
        CoordinatesText.Text = $"({(int)(normalizedPoint.X * 255)}, {(int)(normalizedPoint.Y * 255)})";

        if (_isDragging && _draggedPointIndex >= 0)
        {
            var curve = GetCurrentCurve();
            if (_draggedPointIndex < curve.ControlPoints.Count)
            {
                // Don't allow moving the first or last point's X position to extreme values
                float newX = (float)normalizedPoint.X;
                float newY = (float)normalizedPoint.Y;

                // Keep first point near 0 and last point near 1 in X
                var sortedPoints = curve.ControlPoints.OrderBy(p => p.X).ToList();
                int sortedIndex = sortedPoints.FindIndex(p =>
                    Math.Abs(p.X - curve.ControlPoints[_draggedPointIndex].X) < 0.001f);

                if (sortedIndex == 0)
                    newX = Math.Min(newX, 0.1f);
                else if (sortedIndex == sortedPoints.Count - 1)
                    newX = Math.Max(newX, 0.9f);

                curve.ControlPoints[_draggedPointIndex] = new Vector2(newX, newY);

                RedrawCurves();
                OnCurveChanged();
            }
        }
    }

    private void CurveCanvas_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var canvasPoint = e.GetPosition(CurveCanvas);
        var curve = GetCurrentCurve();

        int pointIndex = FindPointAtPosition(curve, canvasPoint);
        if (pointIndex >= 0 && curve.ControlPoints.Count > 2)
        {
            // Don't allow removing if only 2 points remain
            curve.ControlPoints.RemoveAt(pointIndex);
            RedrawCurves();
            OnCurveChanged();
        }
    }

    private void ChannelCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedChannel = ChannelCombo.SelectedIndex;
        _draggedPointIndex = -1;
        _isDragging = false;
        RedrawCurves();
    }

    private void ShowChannel_Changed(object sender, RoutedEventArgs e)
    {
        RedrawCurves();
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        // Reset the current curve to linear
        SetCurrentCurve(CurveData.CreateLinear());
        RedrawCurves();
        OnCurveChanged();
    }

    /// <summary>
    /// Resets all curves to linear.
    /// </summary>
    public void ResetAllCurves()
    {
        _masterCurve = CurveData.CreateLinear();
        _redCurve = CurveData.CreateLinear();
        _greenCurve = CurveData.CreateLinear();
        _blueCurve = CurveData.CreateLinear();
        RedrawCurves();
        OnCurveChanged();
    }

    private void OnCurveChanged()
    {
        CurveChanged?.Invoke(this, EventArgs.Empty);
    }
}
