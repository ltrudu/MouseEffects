using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.PaintSplatter.UI;

public partial class PaintSplatterSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;

    public event Action<string>? SettingsChanged;

    public PaintSplatterSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isLoading = false;
    }

    private void LoadConfiguration()
    {
        // Trigger settings
        if (_effect.Configuration.TryGet("ps_clickEnabled", out bool clickEnabled))
            ClickEnabledCheckBox.IsChecked = clickEnabled;

        // Splat settings
        if (_effect.Configuration.TryGet("ps_splatSize", out float splatSize))
        {
            SplatSizeSlider.Value = splatSize;
            SplatSizeValue.Text = splatSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("ps_dropletCount", out int dropletCount))
        {
            DropletCountSlider.Value = dropletCount;
            DropletCountValue.Text = dropletCount.ToString();
        }

        if (_effect.Configuration.TryGet("ps_spreadRadius", out float spreadRadius))
        {
            SpreadRadiusSlider.Value = spreadRadius;
            SpreadRadiusValue.Text = spreadRadius.ToString("F0");
        }

        if (_effect.Configuration.TryGet("ps_edgeNoisiness", out float edgeNoisiness))
        {
            EdgeNoisinessSlider.Value = edgeNoisiness;
            EdgeNoisinessValue.Text = edgeNoisiness.ToString("F2");
        }

        if (_effect.Configuration.TryGet("ps_enableDrips", out bool enableDrips))
            EnableDripsCheckBox.IsChecked = enableDrips;

        if (_effect.Configuration.TryGet("ps_dripLength", out float dripLength))
        {
            DripLengthSlider.Value = dripLength;
            DripLengthValue.Text = dripLength.ToString("F0");
        }

        if (_effect.Configuration.TryGet("ps_opacity", out float opacity))
        {
            OpacitySlider.Value = opacity;
            OpacityValue.Text = opacity.ToString("F2");
        }

        if (_effect.Configuration.TryGet("ps_lifetime", out float lifetime))
        {
            LifetimeSlider.Value = lifetime;
            LifetimeValue.Text = lifetime.ToString("F1");
        }

        if (_effect.Configuration.TryGet("ps_maxSplats", out int maxSplats))
        {
            MaxSplatsSlider.Value = maxSplats;
            MaxSplatsValue.Text = maxSplats.ToString();
        }

        // Color settings
        if (_effect.Configuration.TryGet("ps_colorMode", out int colorMode))
            ColorModeCombo.SelectedIndex = colorMode;

        if (_effect.Configuration.TryGet("ps_paletteIndex", out int paletteIndex))
            PaletteCombo.SelectedIndex = paletteIndex;

        if (_effect.Configuration.TryGet("ps_singleColor", out Vector4 color))
        {
            RedSlider.Value = color.X;
            GreenSlider.Value = color.Y;
            BlueSlider.Value = color.Z;
            UpdateColorPreview();
        }
    }

    private void UpdateConfiguration()
    {
        if (_isLoading) return;

        var config = new EffectConfiguration();

        // Trigger settings
        config.Set("ps_clickEnabled", ClickEnabledCheckBox.IsChecked ?? true);

        // Splat settings
        config.Set("ps_splatSize", (float)SplatSizeSlider.Value);
        config.Set("ps_dropletCount", (int)DropletCountSlider.Value);
        config.Set("ps_spreadRadius", (float)SpreadRadiusSlider.Value);
        config.Set("ps_edgeNoisiness", (float)EdgeNoisinessSlider.Value);
        config.Set("ps_enableDrips", EnableDripsCheckBox.IsChecked ?? true);
        config.Set("ps_dripLength", (float)DripLengthSlider.Value);
        config.Set("ps_opacity", (float)OpacitySlider.Value);
        config.Set("ps_lifetime", (float)LifetimeSlider.Value);
        config.Set("ps_maxSplats", (int)MaxSplatsSlider.Value);

        // Color settings
        config.Set("ps_colorMode", ColorModeCombo.SelectedIndex);
        config.Set("ps_paletteIndex", PaletteCombo.SelectedIndex);
        config.Set("ps_singleColor", new Vector4(
            (float)RedSlider.Value,
            (float)GreenSlider.Value,
            (float)BlueSlider.Value,
            1f
        ));

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void UpdateColorPreview()
    {
        if (ColorPreview == null) return;

        byte r = (byte)(RedSlider.Value * 255);
        byte g = (byte)(GreenSlider.Value * 255);
        byte b = (byte)(BlueSlider.Value * 255);

        ColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(r, g, b));
    }

    private void ClickEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void SplatSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SplatSizeValue != null)
            SplatSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void DropletCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DropletCountValue != null)
            DropletCountValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void SpreadRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SpreadRadiusValue != null)
            SpreadRadiusValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void EdgeNoisinessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EdgeNoisinessValue != null)
            EdgeNoisinessValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void EnableDripsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void DripLengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DripLengthValue != null)
            DripLengthValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OpacityValue != null)
            OpacityValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void LifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LifetimeValue != null)
            LifetimeValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void MaxSplatsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxSplatsValue != null)
            MaxSplatsValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void ColorModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void PaletteCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void ColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateColorPreview();
        UpdateConfiguration();
    }
}
