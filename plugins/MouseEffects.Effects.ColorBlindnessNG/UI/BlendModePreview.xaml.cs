using System;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfCursors = System.Windows.Input.Cursors;
using WpfToolTip = System.Windows.Controls.ToolTip;

namespace MouseEffects.Effects.ColorBlindnessNG.UI;

/// <summary>
/// Visual blend mode selector showing preview swatches for each blend mode.
/// </summary>
public partial class BlendModePreview : System.Windows.Controls.UserControl
{
    private readonly Border[] _swatches = new Border[5];
    private readonly string[] _modeNames = { "CW", "Direct", "Prop", "Add", "Screen" };
    private readonly string[] _tooltips =
    {
        "Channel-Weighted: Blend depends on channel intensity. Best for bright colors.",
        "Direct: Full replacement controlled by strength. Works for all intensities.",
        "Proportional: Blend based on channel's relative dominance in the pixel.",
        "Additive: Adds color shift while preserving luminosity.",
        "Screen: Brightens colors, good for light overlays."
    };

    public BlendModePreview()
    {
        InitializeComponent();
        CreateSwatches();
        Loaded += OnLoaded;
    }

    #region Dependency Properties

    public static readonly DependencyProperty SelectedModeProperty = DependencyProperty.Register(
        nameof(SelectedMode), typeof(LutBlendMode), typeof(BlendModePreview),
        new FrameworkPropertyMetadata(LutBlendMode.ChannelWeighted, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnModeChanged));

    public static readonly DependencyProperty StartColorProperty = DependencyProperty.Register(
        nameof(StartColor), typeof(Vector3), typeof(BlendModePreview),
        new PropertyMetadata(new Vector3(1, 0, 0), OnColorChanged));

    public static readonly DependencyProperty EndColorProperty = DependencyProperty.Register(
        nameof(EndColor), typeof(Vector3), typeof(BlendModePreview),
        new PropertyMetadata(new Vector3(0, 1, 1), OnColorChanged));

    public static readonly DependencyProperty SampleChannelValueProperty = DependencyProperty.Register(
        nameof(SampleChannelValue), typeof(float), typeof(BlendModePreview),
        new PropertyMetadata(0.6f, OnColorChanged));

    public LutBlendMode SelectedMode
    {
        get => (LutBlendMode)GetValue(SelectedModeProperty);
        set => SetValue(SelectedModeProperty, value);
    }

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

    public float SampleChannelValue
    {
        get => (float)GetValue(SampleChannelValueProperty);
        set => SetValue(SampleChannelValueProperty, value);
    }

    private static void OnModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BlendModePreview preview)
            preview.UpdateSelection();
    }

    private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is BlendModePreview preview)
            preview.UpdatePreviews();
    }

    #endregion

    #region Events

    public event EventHandler? SelectionChanged;

    #endregion

    #region Initialization

    private void CreateSwatches()
    {
        SwatchGrid.Children.Clear();

        for (int i = 0; i < 5; i++)
        {
            var border = new Border
            {
                Background = WpfBrushes.Gray,
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(80, 80, 80)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(4),
                Margin = new Thickness(2),
                Cursor = WpfCursors.Hand,
                Tag = i,
                ToolTip = new WpfToolTip
                {
                    Content = new StackPanel
                    {
                        Children =
                        {
                            new TextBlock { Text = _modeNames[i], FontWeight = FontWeights.Bold, Foreground = WpfBrushes.White },
                            new TextBlock { Text = _tooltips[i], TextWrapping = TextWrapping.Wrap, MaxWidth = 200, Foreground = WpfBrushes.White }
                        }
                    }
                }
            };

            // Add label
            var label = new TextBlock
            {
                Text = _modeNames[i],
                FontSize = 9,
                FontWeight = FontWeights.SemiBold,
                Foreground = WpfBrushes.White,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Black,
                    BlurRadius = 3,
                    ShadowDepth = 1,
                    Opacity = 0.8
                }
            };
            border.Child = label;

            border.MouseLeftButtonDown += Swatch_Click;
            border.MouseEnter += Swatch_MouseEnter;
            border.MouseLeave += Swatch_MouseLeave;

            _swatches[i] = border;
            SwatchGrid.Children.Add(border);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdatePreviews();
        UpdateSelection();
    }

    #endregion

    #region Event Handlers

    private void Swatch_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is int index)
        {
            SelectedMode = (LutBlendMode)index;
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void Swatch_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is Border border)
        {
            border.BorderBrush = new SolidColorBrush(WpfColor.FromRgb(200, 200, 200));
        }
    }

    private void Swatch_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is Border border)
        {
            int index = (int)border.Tag;
            bool isSelected = index == (int)SelectedMode;
            border.BorderBrush = isSelected
                ? new SolidColorBrush(WpfColor.FromRgb(255, 180, 0))
                : new SolidColorBrush(WpfColor.FromRgb(80, 80, 80));
        }
    }

    #endregion

    #region Visual Updates

    private void UpdatePreviews()
    {
        if (_swatches[0] == null) return;

        // Sample color for preview: use channel value as the primary channel
        // Create a sample color that is channel-dominant
        var sampleColor = new Vector3(SampleChannelValue, 0.3f, 0.3f);
        Vector3 lutColor = InterpolateColor(StartColor, EndColor, SampleChannelValue);

        for (int i = 0; i < 5; i++)
        {
            var mode = (LutBlendMode)i;
            var result = ApplyBlendMode(sampleColor, lutColor, SampleChannelValue, 1.0f, mode, sampleColor);
            _swatches[i].Background = new SolidColorBrush(Vector3ToColor(result));
        }
    }

    private void UpdateSelection()
    {
        if (_swatches[0] == null) return;

        for (int i = 0; i < 5; i++)
        {
            bool isSelected = i == (int)SelectedMode;
            _swatches[i].BorderBrush = isSelected
                ? new SolidColorBrush(WpfColor.FromRgb(255, 180, 0))
                : new SolidColorBrush(WpfColor.FromRgb(80, 80, 80));
            _swatches[i].BorderThickness = isSelected
                ? new Thickness(3)
                : new Thickness(2);
        }
    }

    #endregion

    #region Blend Mode Logic

    private static Vector3 ApplyBlendMode(Vector3 result, Vector3 lutColor, float channelValue, float strength, LutBlendMode blendMode, Vector3 originalColor)
    {
        Vector3 blended;

        switch (blendMode)
        {
            case LutBlendMode.ChannelWeighted:
                blended = Vector3.Lerp(result, Vector3.Lerp(result, lutColor, channelValue), strength);
                break;

            case LutBlendMode.Direct:
                blended = Vector3.Lerp(result, lutColor, strength);
                break;

            case LutBlendMode.Proportional:
                float maxChannel = MathF.Max(originalColor.X, MathF.Max(originalColor.Y, originalColor.Z));
                float proportion = (maxChannel > 0.001f) ? (channelValue / maxChannel) : 0.0f;
                blended = Vector3.Lerp(result, lutColor, proportion * strength);
                break;

            case LutBlendMode.Additive:
                Vector3 shift = (lutColor - result) * channelValue * strength;
                blended = Vector3.Clamp(result + shift, Vector3.Zero, Vector3.One);
                break;

            case LutBlendMode.Screen:
                Vector3 screenFactor = lutColor * channelValue * strength;
                blended = Vector3.One - (Vector3.One - result) * (Vector3.One - screenFactor);
                break;

            default:
                blended = result;
                break;
        }

        return blended;
    }

    private static Vector3 InterpolateColor(Vector3 start, Vector3 end, float t)
    {
        return Vector3.Lerp(start, end, t);
    }

    private static WpfColor Vector3ToColor(Vector3 v) =>
        WpfColor.FromRgb(
            (byte)(Math.Clamp(v.X, 0, 1) * 255),
            (byte)(Math.Clamp(v.Y, 0, 1) * 255),
            (byte)(Math.Clamp(v.Z, 0, 1) * 255));

    #endregion
}
