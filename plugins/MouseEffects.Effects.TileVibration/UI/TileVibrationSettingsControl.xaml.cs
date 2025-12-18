using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.TileVibration.UI;

public partial class TileVibrationSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private bool _isInitializing = true;
    private Vector4 _outlineColor = new(1f, 1f, 1f, 1f);

    /// <summary>
    /// Event raised when settings are changed and should be saved.
    /// </summary>
    public event Action<string>? SettingsChanged;

    public TileVibrationSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        // Load current values from effect configuration
        LoadConfiguration();
        UpdateSyncVisibility();
        _isInitializing = false;
    }

    private void LoadConfiguration()
    {
        if (_effect.Configuration.TryGet("tileLifespan", out float lifespan))
        {
            LifespanSlider.Value = lifespan;
            LifespanValue.Text = lifespan.ToString("F1");
        }

        if (_effect.Configuration.TryGet("maxWidth", out float maxWidth))
        {
            MaxWidthSlider.Value = maxWidth;
            MaxWidthValue.Text = maxWidth.ToString("F0");
        }

        if (_effect.Configuration.TryGet("maxHeight", out float maxHeight))
        {
            MaxHeightSlider.Value = maxHeight;
            MaxHeightValue.Text = maxHeight.ToString("F0");
        }

        if (_effect.Configuration.TryGet("minWidth", out float minWidth))
        {
            MinWidthSlider.Value = minWidth;
            MinWidthValue.Text = minWidth.ToString("F0");
        }

        if (_effect.Configuration.TryGet("minHeight", out float minHeight))
        {
            MinHeightSlider.Value = minHeight;
            MinHeightValue.Text = minHeight.ToString("F0");
        }

        if (_effect.Configuration.TryGet("syncWidthHeight", out bool sync))
        {
            SyncSizeCheckBox.IsChecked = sync;
        }

        if (_effect.Configuration.TryGet("edgeStyle", out int edgeStyle))
        {
            EdgeStyleCombo.SelectedIndex = edgeStyle;
        }

        if (_effect.Configuration.TryGet("vibrationSpeed", out float speed))
        {
            VibrationSpeedSlider.Value = speed;
            VibrationSpeedValue.Text = speed.ToString("F1");
        }

        if (_effect.Configuration.TryGet("displacementEnabled", out bool dispEnabled))
        {
            DisplacementCheckBox.IsChecked = dispEnabled;
        }

        if (_effect.Configuration.TryGet("displacementMax", out float dispMax))
        {
            DisplacementMaxSlider.Value = dispMax;
            DisplacementMaxValue.Text = dispMax.ToString("F0");
        }

        if (_effect.Configuration.TryGet("zoomEnabled", out bool zoomEnabled))
        {
            ZoomCheckBox.IsChecked = zoomEnabled;
        }

        if (_effect.Configuration.TryGet("zoomMin", out float zoomMin))
        {
            ZoomMinSlider.Value = zoomMin;
            ZoomMinValue.Text = zoomMin.ToString("F2");
        }

        if (_effect.Configuration.TryGet("zoomMax", out float zoomMax))
        {
            ZoomMaxSlider.Value = zoomMax;
            ZoomMaxValue.Text = zoomMax.ToString("F2");
        }

        if (_effect.Configuration.TryGet("rotationEnabled", out bool rotEnabled))
        {
            RotationCheckBox.IsChecked = rotEnabled;
        }

        if (_effect.Configuration.TryGet("rotationAmplitude", out float rotAmplitude))
        {
            RotationAmplitudeSlider.Value = rotAmplitude;
            RotationAmplitudeValue.Text = rotAmplitude.ToString("F0");
        }

        if (_effect.Configuration.TryGet("outlineEnabled", out bool outlineEnabled))
        {
            OutlineCheckBox.IsChecked = outlineEnabled;
        }

        if (_effect.Configuration.TryGet("outlineSize", out float outlineSize))
        {
            OutlineSizeSlider.Value = outlineSize;
            OutlineSizeValue.Text = outlineSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("outlineColor", out Vector4 outlineColor))
        {
            _outlineColor = outlineColor;
            UpdateOutlineColorPreview();
        }
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();
        config.Set("tileLifespan", (float)LifespanSlider.Value);
        config.Set("maxWidth", (float)MaxWidthSlider.Value);
        config.Set("maxHeight", (float)MaxHeightSlider.Value);
        config.Set("minWidth", (float)MinWidthSlider.Value);
        config.Set("minHeight", (float)MinHeightSlider.Value);
        config.Set("syncWidthHeight", SyncSizeCheckBox.IsChecked ?? true);
        config.Set("edgeStyle", EdgeStyleCombo.SelectedIndex);
        config.Set("vibrationSpeed", (float)VibrationSpeedSlider.Value);
        config.Set("displacementEnabled", DisplacementCheckBox.IsChecked ?? true);
        config.Set("displacementMax", (float)DisplacementMaxSlider.Value);
        config.Set("zoomEnabled", ZoomCheckBox.IsChecked ?? false);
        config.Set("zoomMin", (float)ZoomMinSlider.Value);
        config.Set("zoomMax", (float)ZoomMaxSlider.Value);
        config.Set("rotationEnabled", RotationCheckBox.IsChecked ?? false);
        config.Set("rotationAmplitude", (float)RotationAmplitudeSlider.Value);
        config.Set("outlineEnabled", OutlineCheckBox.IsChecked ?? false);
        config.Set("outlineSize", (float)OutlineSizeSlider.Value);
        config.Set("outlineColor", _outlineColor);

        _effect.Configure(config);

        // Notify that settings changed for persistence
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void UpdateSyncVisibility()
    {
        bool synced = SyncSizeCheckBox.IsChecked ?? true;

        // Hide height controls when synced
        MaxHeightLabel.Visibility = synced ? Visibility.Collapsed : Visibility.Visible;
        MaxHeightSlider.Visibility = synced ? Visibility.Collapsed : Visibility.Visible;
        MaxHeightValue.Visibility = synced ? Visibility.Collapsed : Visibility.Visible;

        MinHeightLabel.Visibility = synced ? Visibility.Collapsed : Visibility.Visible;
        MinHeightSlider.Visibility = synced ? Visibility.Collapsed : Visibility.Visible;
        MinHeightValue.Visibility = synced ? Visibility.Collapsed : Visibility.Visible;
    }

    private void LifespanSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LifespanValue != null)
            LifespanValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void MaxWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxWidthValue != null)
            MaxWidthValue.Text = e.NewValue.ToString("F0");

        // Sync height if checkbox is checked
        if (!_isInitializing && (SyncSizeCheckBox?.IsChecked ?? true))
        {
            MaxHeightSlider.Value = e.NewValue;
        }

        UpdateConfiguration();
    }

    private void MaxHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxHeightValue != null)
            MaxHeightValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MinWidthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MinWidthValue != null)
            MinWidthValue.Text = e.NewValue.ToString("F0");

        // Sync height if checkbox is checked
        if (!_isInitializing && (SyncSizeCheckBox?.IsChecked ?? true))
        {
            MinHeightSlider.Value = e.NewValue;
        }

        UpdateConfiguration();
    }

    private void MinHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MinHeightValue != null)
            MinHeightValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void SyncSizeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        UpdateSyncVisibility();

        // When enabling sync, copy width to height
        if (SyncSizeCheckBox.IsChecked ?? true)
        {
            MaxHeightSlider.Value = MaxWidthSlider.Value;
            MinHeightSlider.Value = MinWidthSlider.Value;
        }

        UpdateConfiguration();
    }

    private void EdgeStyleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void VibrationSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VibrationSpeedValue != null)
            VibrationSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void DisplacementCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void DisplacementMaxSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DisplacementMaxValue != null)
            DisplacementMaxValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void ZoomCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void ZoomMinSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ZoomMinValue != null)
            ZoomMinValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void ZoomMaxSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ZoomMaxValue != null)
            ZoomMaxValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void RotationCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void RotationAmplitudeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RotationAmplitudeValue != null)
            RotationAmplitudeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void OutlineCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void OutlineSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OutlineSizeValue != null)
            OutlineSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void OutlineColorButton_Click(object sender, RoutedEventArgs e)
    {
        // Use Windows Forms ColorDialog
        using var dialog = new System.Windows.Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(
                (int)(_outlineColor.W * 255),
                (int)(_outlineColor.X * 255),
                (int)(_outlineColor.Y * 255),
                (int)(_outlineColor.Z * 255)),
            FullOpen = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _outlineColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                dialog.Color.A / 255f);
            UpdateOutlineColorPreview();
            UpdateConfiguration();
        }
    }

    private void UpdateOutlineColorPreview()
    {
        OutlineColorPreview.Background = new SolidColorBrush(
            System.Windows.Media.Color.FromArgb(
                (byte)(_outlineColor.W * 255),
                (byte)(_outlineColor.X * 255),
                (byte)(_outlineColor.Y * 255),
                (byte)(_outlineColor.Z * 255)));
    }
}
