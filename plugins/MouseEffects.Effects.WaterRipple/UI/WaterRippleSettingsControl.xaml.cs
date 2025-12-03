using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.WaterRipple.UI;

public partial class WaterRippleSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private bool _isInitializing = true;
    private bool _isExpanded;
    private Vector4 _gridColor = new(0.0f, 1.0f, 0.5f, 0.8f);

    /// <summary>
    /// Event raised when settings are changed and should be saved.
    /// </summary>
    public event Action<string>? SettingsChanged;

    public WaterRippleSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        // Load current values from effect configuration
        LoadConfiguration();
        _isInitializing = false;
    }

    private void LoadConfiguration()
    {
        EnabledCheckBox.IsChecked = _effect.IsEnabled;

        if (_effect.Configuration.TryGet("maxRipples", out int maxRipples))
        {
            MaxRipplesSlider.Value = maxRipples;
            MaxRipplesValue.Text = maxRipples.ToString();
        }

        if (_effect.Configuration.TryGet("rippleLifespan", out float lifespan))
        {
            LifespanSlider.Value = lifespan;
            LifespanValue.Text = lifespan.ToString("F1");
        }

        if (_effect.Configuration.TryGet("waveSpeed", out float speed))
        {
            WaveSpeedSlider.Value = speed;
            WaveSpeedValue.Text = speed.ToString("F0");
        }

        if (_effect.Configuration.TryGet("wavelength", out float wavelength))
        {
            WavelengthSlider.Value = wavelength;
            WavelengthValue.Text = wavelength.ToString("F0");
        }

        if (_effect.Configuration.TryGet("damping", out float damping))
        {
            DampingSlider.Value = damping;
            DampingValue.Text = damping.ToString("F1");
        }

        if (_effect.Configuration.TryGet("spawnOnLeftClick", out bool leftClick))
        {
            LeftClickCheckBox.IsChecked = leftClick;
        }

        if (_effect.Configuration.TryGet("spawnOnRightClick", out bool rightClick))
        {
            RightClickCheckBox.IsChecked = rightClick;
        }

        if (_effect.Configuration.TryGet("clickMinAmplitude", out float clickMinAmp))
        {
            ClickMinAmplitudeSlider.Value = clickMinAmp;
            ClickMinAmplitudeValue.Text = clickMinAmp.ToString("F0");
        }

        if (_effect.Configuration.TryGet("clickMaxAmplitude", out float clickMaxAmp))
        {
            ClickMaxAmplitudeSlider.Value = clickMaxAmp;
            ClickMaxAmplitudeValue.Text = clickMaxAmp.ToString("F0");
        }

        if (_effect.Configuration.TryGet("spawnOnMove", out bool spawnOnMove))
        {
            SpawnOnMoveCheckBox.IsChecked = spawnOnMove;
        }

        if (_effect.Configuration.TryGet("moveSpawnDistance", out float moveSpawnDist))
        {
            MoveSpawnDistanceSlider.Value = moveSpawnDist;
            MoveSpawnDistanceValue.Text = moveSpawnDist.ToString("F0");
        }

        if (_effect.Configuration.TryGet("moveMinAmplitude", out float moveMinAmp))
        {
            MoveMinAmplitudeSlider.Value = moveMinAmp;
            MoveMinAmplitudeValue.Text = moveMinAmp.ToString("F0");
        }

        if (_effect.Configuration.TryGet("moveMaxAmplitude", out float moveMaxAmp))
        {
            MoveMaxAmplitudeSlider.Value = moveMaxAmp;
            MoveMaxAmplitudeValue.Text = moveMaxAmp.ToString("F0");
        }

        if (_effect.Configuration.TryGet("moveRippleLifespan", out float moveLifespan))
        {
            MoveLifespanSlider.Value = moveLifespan;
            MoveLifespanValue.Text = moveLifespan.ToString("F1");
        }

        if (_effect.Configuration.TryGet("moveWaveSpeed", out float moveWaveSpeed))
        {
            MoveWaveSpeedSlider.Value = moveWaveSpeed;
            MoveWaveSpeedValue.Text = moveWaveSpeed.ToString("F0");
        }

        if (_effect.Configuration.TryGet("moveWavelength", out float moveWavelength))
        {
            MoveWavelengthSlider.Value = moveWavelength;
            MoveWavelengthValue.Text = moveWavelength.ToString("F0");
        }

        if (_effect.Configuration.TryGet("moveDamping", out float moveDamping))
        {
            MoveDampingSlider.Value = moveDamping;
            MoveDampingValue.Text = moveDamping.ToString("F1");
        }

        if (_effect.Configuration.TryGet("enableGrid", out bool enableGrid))
        {
            GridCheckBox.IsChecked = enableGrid;
        }

        if (_effect.Configuration.TryGet("gridSpacing", out float gridSpacing))
        {
            GridSpacingSlider.Value = gridSpacing;
            GridSpacingValue.Text = gridSpacing.ToString("F0");
        }

        if (_effect.Configuration.TryGet("gridThickness", out float gridThickness))
        {
            GridThicknessSlider.Value = gridThickness;
            GridThicknessValue.Text = gridThickness.ToString("F1");
        }

        if (_effect.Configuration.TryGet("gridColor", out Vector4 gridColor))
        {
            _gridColor = gridColor;
            UpdateGridColorPreview();
        }
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();
        config.Set("maxRipples", (int)MaxRipplesSlider.Value);
        config.Set("rippleLifespan", (float)LifespanSlider.Value);
        config.Set("waveSpeed", (float)WaveSpeedSlider.Value);
        config.Set("wavelength", (float)WavelengthSlider.Value);
        config.Set("damping", (float)DampingSlider.Value);
        config.Set("spawnOnLeftClick", LeftClickCheckBox.IsChecked ?? true);
        config.Set("spawnOnRightClick", RightClickCheckBox.IsChecked ?? false);
        config.Set("clickMinAmplitude", (float)ClickMinAmplitudeSlider.Value);
        config.Set("clickMaxAmplitude", (float)ClickMaxAmplitudeSlider.Value);
        config.Set("spawnOnMove", SpawnOnMoveCheckBox.IsChecked ?? false);
        config.Set("moveSpawnDistance", (float)MoveSpawnDistanceSlider.Value);
        config.Set("moveMinAmplitude", (float)MoveMinAmplitudeSlider.Value);
        config.Set("moveMaxAmplitude", (float)MoveMaxAmplitudeSlider.Value);
        config.Set("moveRippleLifespan", (float)MoveLifespanSlider.Value);
        config.Set("moveWaveSpeed", (float)MoveWaveSpeedSlider.Value);
        config.Set("moveWavelength", (float)MoveWavelengthSlider.Value);
        config.Set("moveDamping", (float)MoveDampingSlider.Value);
        config.Set("enableGrid", GridCheckBox.IsChecked ?? false);
        config.Set("gridSpacing", (float)GridSpacingSlider.Value);
        config.Set("gridThickness", (float)GridThicknessSlider.Value);
        config.Set("gridColor", _gridColor);

        _effect.Configure(config);

        // Notify that settings changed for persistence
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _effect.IsEnabled = EnabledCheckBox.IsChecked ?? true;
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        ContentPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        FoldButton.Content = _isExpanded ? "\u25B2" : "\u25BC";
    }

    private void LifespanSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LifespanValue != null)
            LifespanValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void WaveSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WaveSpeedValue != null)
            WaveSpeedValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MaxRipplesSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxRipplesValue != null)
            MaxRipplesValue.Text = ((int)e.NewValue).ToString();
        UpdateConfiguration();
    }

    private void WavelengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WavelengthValue != null)
            WavelengthValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void DampingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DampingValue != null)
            DampingValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void LeftClickCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void RightClickCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void ClickMinAmplitudeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        const double minGap = 5;
        var minVal = e.NewValue;
        var maxVal = ClickMaxAmplitudeSlider.Value;

        // Ensure min < max with minimum gap
        if (minVal >= maxVal - minGap + 1)
        {
            var newMax = Math.Min(minVal + minGap, ClickMaxAmplitudeSlider.Maximum);
            ClickMaxAmplitudeSlider.Value = newMax;
            ClickMaxAmplitudeValue.Text = newMax.ToString("F0");
        }

        if (ClickMinAmplitudeValue != null)
            ClickMinAmplitudeValue.Text = minVal.ToString("F0");
        UpdateConfiguration();
    }

    private void ClickMaxAmplitudeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        const double minGap = 5;
        var maxVal = e.NewValue;
        var minVal = ClickMinAmplitudeSlider.Value;

        // Ensure max > min with minimum gap
        if (maxVal <= minVal + minGap - 1)
        {
            var newMin = Math.Max(maxVal - minGap, ClickMinAmplitudeSlider.Minimum);
            ClickMinAmplitudeSlider.Value = newMin;
            ClickMinAmplitudeValue.Text = newMin.ToString("F0");
        }

        if (ClickMaxAmplitudeValue != null)
            ClickMaxAmplitudeValue.Text = maxVal.ToString("F0");
        UpdateConfiguration();
    }

    private void SpawnOnMoveCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void MoveSpawnDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MoveSpawnDistanceValue != null)
            MoveSpawnDistanceValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MoveMinAmplitudeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        const double minGap = 2;
        var minVal = e.NewValue;
        var maxVal = MoveMaxAmplitudeSlider.Value;

        // Ensure min < max with minimum gap
        if (minVal >= maxVal - minGap + 1)
        {
            var newMax = Math.Min(minVal + minGap, MoveMaxAmplitudeSlider.Maximum);
            MoveMaxAmplitudeSlider.Value = newMax;
            MoveMaxAmplitudeValue.Text = newMax.ToString("F0");
        }

        if (MoveMinAmplitudeValue != null)
            MoveMinAmplitudeValue.Text = minVal.ToString("F0");
        UpdateConfiguration();
    }

    private void MoveMaxAmplitudeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;

        const double minGap = 2;
        var maxVal = e.NewValue;
        var minVal = MoveMinAmplitudeSlider.Value;

        // Ensure max > min with minimum gap
        if (maxVal <= minVal + minGap - 1)
        {
            var newMin = Math.Max(maxVal - minGap, MoveMinAmplitudeSlider.Minimum);
            MoveMinAmplitudeSlider.Value = newMin;
            MoveMinAmplitudeValue.Text = newMin.ToString("F0");
        }

        if (MoveMaxAmplitudeValue != null)
            MoveMaxAmplitudeValue.Text = maxVal.ToString("F0");
        UpdateConfiguration();
    }

    private void MoveLifespanSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MoveLifespanValue != null)
            MoveLifespanValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void MoveWaveSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MoveWaveSpeedValue != null)
            MoveWaveSpeedValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MoveWavelengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MoveWavelengthValue != null)
            MoveWavelengthValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MoveDampingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MoveDampingValue != null)
            MoveDampingValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void GridCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void GridSpacingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GridSpacingValue != null)
            GridSpacingValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void GridThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GridThicknessValue != null)
            GridThicknessValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void GridColorButton_Click(object sender, RoutedEventArgs e)
    {
        // Use Windows Forms ColorDialog
        using var dialog = new System.Windows.Forms.ColorDialog
        {
            Color = System.Drawing.Color.FromArgb(
                (int)(_gridColor.W * 255),
                (int)(_gridColor.X * 255),
                (int)(_gridColor.Y * 255),
                (int)(_gridColor.Z * 255)),
            FullOpen = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            _gridColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                dialog.Color.A / 255f);
            UpdateGridColorPreview();
            UpdateConfiguration();
        }
    }

    private void UpdateGridColorPreview()
    {
        GridColorPreview.Background = new SolidColorBrush(
            System.Windows.Media.Color.FromArgb(
                (byte)(_gridColor.W * 255),
                (byte)(_gridColor.X * 255),
                (byte)(_gridColor.Y * 255),
                (byte)(_gridColor.Z * 255)));
    }
}
