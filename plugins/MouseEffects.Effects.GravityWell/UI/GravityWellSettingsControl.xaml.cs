using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.GravityWell.UI;

public partial class GravityWellSettingsControl : UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;
    private bool _isExpanded;

    public event Action<string>? SettingsChanged;

    public GravityWellSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isLoading = false;
    }

    private void LoadConfiguration()
    {
        EnabledCheckBox.IsChecked = _effect.IsEnabled;

        // Particle settings
        if (_effect.Configuration.TryGet("gw_particleCount", out int count))
        {
            ParticleCountSlider.Value = count;
            ParticleCountValue.Text = count.ToString();
        }

        if (_effect.Configuration.TryGet("gw_particleSize", out float size))
        {
            ParticleSizeSlider.Value = size;
            ParticleSizeValue.Text = size.ToString("F1");
        }

        if (_effect.Configuration.TryGet("gw_randomColors", out bool randomColors))
        {
            RandomColorsCheckBox.IsChecked = randomColors;
        }

        // Physics settings
        if (_effect.Configuration.TryGet("gw_gravityMode", out int mode))
        {
            GravityModeCombo.SelectedIndex = mode;
        }

        if (_effect.Configuration.TryGet("gw_gravityStrength", out float strength))
        {
            GravityStrengthSlider.Value = strength;
            GravityStrengthValue.Text = strength.ToString("F0");
        }

        if (_effect.Configuration.TryGet("gw_orbitSpeed", out float orbit))
        {
            OrbitSpeedSlider.Value = orbit;
            OrbitSpeedValue.Text = orbit.ToString("F0");
        }

        if (_effect.Configuration.TryGet("gw_damping", out float damping))
        {
            DampingSlider.Value = damping;
            DampingValue.Text = damping.ToString("F2");
        }

        // Trail settings
        if (_effect.Configuration.TryGet("gw_trailEnabled", out bool trail))
        {
            TrailEnabledCheckBox.IsChecked = trail;
        }

        if (_effect.Configuration.TryGet("gw_trailLength", out float trailLen))
        {
            TrailLengthSlider.Value = trailLen;
            TrailLengthValue.Text = trailLen.ToString("F2");
        }

        // Visual settings
        if (_effect.Configuration.TryGet("gw_hdrMultiplier", out float hdr))
        {
            HdrMultiplierSlider.Value = hdr;
            HdrMultiplierValue.Text = hdr.ToString("F1");
        }
    }

    private void UpdateConfiguration()
    {
        if (_isLoading) return;

        var config = new EffectConfiguration();

        // Particle settings
        config.Set("gw_particleCount", (int)ParticleCountSlider.Value);
        config.Set("gw_particleSize", (float)ParticleSizeSlider.Value);
        config.Set("gw_randomColors", RandomColorsCheckBox.IsChecked ?? false);

        // Physics settings
        config.Set("gw_gravityMode", GravityModeCombo.SelectedIndex);
        config.Set("gw_gravityStrength", (float)GravityStrengthSlider.Value);
        config.Set("gw_orbitSpeed", (float)OrbitSpeedSlider.Value);
        config.Set("gw_damping", (float)DampingSlider.Value);

        // Trail settings
        config.Set("gw_trailEnabled", TrailEnabledCheckBox.IsChecked ?? true);
        config.Set("gw_trailLength", (float)TrailLengthSlider.Value);

        // Visual settings
        config.Set("gw_hdrMultiplier", (float)HdrMultiplierSlider.Value);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        _effect.IsEnabled = EnabledCheckBox.IsChecked ?? true;
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void ParticleCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ParticleCountValue != null)
            ParticleCountValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void ParticleSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ParticleSizeValue != null)
            ParticleSizeValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void RandomColorsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void GravityModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void GravityStrengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GravityStrengthValue != null)
            GravityStrengthValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void OrbitSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (OrbitSpeedValue != null)
            OrbitSpeedValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void DampingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DampingValue != null)
            DampingValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void TrailEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void TrailLengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TrailLengthValue != null)
            TrailLengthValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void HdrMultiplierSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (HdrMultiplierValue != null)
            HdrMultiplierValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        ContentPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        FoldButton.Content = _isExpanded ? "▲" : "▼";
    }
}
