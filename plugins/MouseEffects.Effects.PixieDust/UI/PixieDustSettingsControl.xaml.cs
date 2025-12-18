using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.PixieDust.UI;

public partial class PixieDustSettingsControl : UserControl
{
    private readonly IEffect _effect;
    private bool _isInitializing = true;
    private bool _isExpanded;

    public event Action<string>? SettingsChanged;

    public PixieDustSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isInitializing = false;
    }

    private void LoadConfiguration()
    {
        EnabledCheckBox.IsChecked = _effect.IsEnabled;

        // Trigger settings
        if (_effect.Configuration.TryGet("pd_mouseMoveEnabled", out bool moveEnabled))
            MouseMoveCheckBox.IsChecked = moveEnabled;

        if (_effect.Configuration.TryGet("pd_moveDistanceThreshold", out float moveDist))
        {
            MoveDistanceSlider.Value = moveDist;
            MoveDistanceValue.Text = moveDist.ToString("F0");
        }

        if (_effect.Configuration.TryGet("pd_leftClickEnabled", out bool leftEnabled))
            LeftClickCheckBox.IsChecked = leftEnabled;

        if (_effect.Configuration.TryGet("pd_leftClickBurstCount", out int leftCount))
        {
            LeftClickCountSlider.Value = leftCount;
            LeftClickCountValue.Text = leftCount.ToString();
        }

        if (_effect.Configuration.TryGet("pd_rightClickEnabled", out bool rightEnabled))
            RightClickCheckBox.IsChecked = rightEnabled;

        if (_effect.Configuration.TryGet("pd_rightClickBurstCount", out int rightCount))
        {
            RightClickCountSlider.Value = rightCount;
            RightClickCountValue.Text = rightCount.ToString();
        }

        // Particle settings
        if (_effect.Configuration.TryGet("pd_particleCount", out int count))
        {
            ParticleCountSlider.Value = count;
            ParticleCountValue.Text = count.ToString();
        }

        if (_effect.Configuration.TryGet("pd_particleSize", out float size))
        {
            ParticleSizeSlider.Value = size;
            ParticleSizeValue.Text = size.ToString("F0");
        }

        if (_effect.Configuration.TryGet("pd_lifetime", out float lifetime))
        {
            LifetimeSlider.Value = lifetime;
            LifetimeValue.Text = lifetime.ToString("F1");
        }

        if (_effect.Configuration.TryGet("pd_driftSpeed", out float drift))
        {
            DriftSpeedSlider.Value = drift;
            DriftSpeedValue.Text = drift.ToString("F0");
        }

        if (_effect.Configuration.TryGet("pd_glowIntensity", out float glow))
        {
            GlowIntensitySlider.Value = glow;
            GlowIntensityValue.Text = glow.ToString("F1");
        }

        // Color settings
        if (_effect.Configuration.TryGet("pd_rainbowMode", out bool rainbow))
            RainbowModeCheckBox.IsChecked = rainbow;

        if (_effect.Configuration.TryGet("pd_rainbowSpeed", out float rainbowSpeed))
        {
            RainbowSpeedSlider.Value = rainbowSpeed;
            RainbowSpeedValue.Text = rainbowSpeed.ToString("F1");
        }
    }

    private void UpdateConfiguration()
    {
        if (_isInitializing) return;

        var config = new EffectConfiguration();

        // Trigger settings
        config.Set("pd_mouseMoveEnabled", MouseMoveCheckBox.IsChecked ?? true);
        config.Set("pd_moveDistanceThreshold", (float)MoveDistanceSlider.Value);
        config.Set("pd_leftClickEnabled", LeftClickCheckBox.IsChecked ?? true);
        config.Set("pd_leftClickBurstCount", (int)LeftClickCountSlider.Value);
        config.Set("pd_rightClickEnabled", RightClickCheckBox.IsChecked ?? true);
        config.Set("pd_rightClickBurstCount", (int)RightClickCountSlider.Value);

        // Particle settings
        config.Set("pd_particleCount", (int)ParticleCountSlider.Value);
        config.Set("pd_particleSize", (float)ParticleSizeSlider.Value);
        config.Set("pd_lifetime", (float)LifetimeSlider.Value);
        config.Set("pd_spawnRate", 0.05f); // Keep default
        config.Set("pd_driftSpeed", (float)DriftSpeedSlider.Value);
        config.Set("pd_glowIntensity", (float)GlowIntensitySlider.Value);

        // Color settings
        config.Set("pd_rainbowMode", RainbowModeCheckBox.IsChecked ?? true);
        config.Set("pd_rainbowSpeed", (float)RainbowSpeedSlider.Value);

        // Preserve fixed color setting
        if (_effect.Configuration.TryGet("pd_fixedColor", out Vector4 fixedColor))
            config.Set("pd_fixedColor", fixedColor);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;
        _effect.IsEnabled = EnabledCheckBox.IsChecked ?? true;
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void MouseMoveCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void MoveDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MoveDistanceValue != null)
            MoveDistanceValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void LeftClickCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void LeftClickCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LeftClickCountValue != null)
            LeftClickCountValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void RightClickCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void RightClickCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RightClickCountValue != null)
            RightClickCountValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
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
            ParticleSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void LifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LifetimeValue != null)
            LifetimeValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void DriftSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DriftSpeedValue != null)
            DriftSpeedValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void GlowIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GlowIntensityValue != null)
            GlowIntensityValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void RainbowModeCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void RainbowSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RainbowSpeedValue != null)
            RainbowSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void FoldButton_Click(object sender, RoutedEventArgs e)
    {
        _isExpanded = !_isExpanded;
        ContentPanel.Visibility = _isExpanded ? Visibility.Visible : Visibility.Collapsed;
        FoldButton.Content = _isExpanded ? "▲" : "▼";
    }
}
