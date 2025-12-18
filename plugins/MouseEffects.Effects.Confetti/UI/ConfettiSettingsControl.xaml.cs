using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Confetti.UI;

public partial class ConfettiSettingsControl : System.Windows.Controls.UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;

    public event Action<string>? SettingsChanged;

    public ConfettiSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isLoading = false;
    }

    private void LoadConfiguration()
    {
        // Trigger settings
        if (_effect.Configuration.TryGet("burstOnClick", out bool burstClick))
            BurstOnClickCheckBox.IsChecked = burstClick;

        if (_effect.Configuration.TryGet("trailOnMove", out bool trailMove))
            TrailOnMoveCheckBox.IsChecked = trailMove;

        // Burst settings
        if (_effect.Configuration.TryGet("burstCount", out int burst))
        {
            BurstCountSlider.Value = burst;
            BurstCountValue.Text = burst.ToString();
        }

        if (_effect.Configuration.TryGet("burstForce", out float force))
        {
            BurstForceSlider.Value = force;
            BurstForceValue.Text = force.ToString("F0");
        }

        // Trail settings
        if (_effect.Configuration.TryGet("trailSpacing", out float spacing))
        {
            TrailSpacingSlider.Value = spacing;
            TrailSpacingValue.Text = spacing.ToString("F0");
        }

        // Particle settings
        if (_effect.Configuration.TryGet("minParticleSize", out float minSize))
        {
            MinParticleSizeSlider.Value = minSize;
            MinParticleSizeValue.Text = minSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("maxParticleSize", out float maxSize))
        {
            MaxParticleSizeSlider.Value = maxSize;
            MaxParticleSizeValue.Text = maxSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("particleLifespan", out float lifespan))
        {
            ParticleLifespanSlider.Value = lifespan;
            ParticleLifespanValue.Text = lifespan.ToString("F1");
        }

        // Physics settings
        if (_effect.Configuration.TryGet("gravity", out float gravity))
        {
            GravitySlider.Value = gravity;
            GravityValue.Text = gravity.ToString("F0");
        }

        if (_effect.Configuration.TryGet("airResistance", out float air))
        {
            AirResistanceSlider.Value = air;
            AirResistanceValue.Text = air.ToString("F3");
        }

        if (_effect.Configuration.TryGet("flutterAmount", out float flutter))
        {
            FlutterAmountSlider.Value = flutter;
            FlutterAmountValue.Text = flutter.ToString("F1");
        }

        // Shape settings
        if (_effect.Configuration.TryGet("useRectangles", out bool rects))
            UseRectanglesCheckBox.IsChecked = rects;

        if (_effect.Configuration.TryGet("useCircles", out bool circles))
            UseCirclesCheckBox.IsChecked = circles;

        if (_effect.Configuration.TryGet("useRibbons", out bool ribbons))
            UseRibbonsCheckBox.IsChecked = ribbons;

        // Color settings
        if (_effect.Configuration.TryGet("rainbowMode", out bool rainbow))
            RainbowModeCheckBox.IsChecked = rainbow;

        if (_effect.Configuration.TryGet("rainbowSpeed", out float rainbowSpeed))
        {
            RainbowSpeedSlider.Value = rainbowSpeed;
            RainbowSpeedValue.Text = rainbowSpeed.ToString("F1");
        }

        // Performance settings
        if (_effect.Configuration.TryGet("maxParticles", out int maxPart))
        {
            MaxParticlesSlider.Value = maxPart;
            MaxParticlesValue.Text = maxPart.ToString();
        }
    }

    private void UpdateConfiguration()
    {
        if (_isLoading) return;

        var config = new EffectConfiguration();

        // Trigger settings
        config.Set("burstOnClick", BurstOnClickCheckBox.IsChecked ?? true);
        config.Set("trailOnMove", TrailOnMoveCheckBox.IsChecked ?? false);

        // Burst settings
        config.Set("burstCount", (int)BurstCountSlider.Value);
        config.Set("burstForce", (float)BurstForceSlider.Value);

        // Trail settings
        config.Set("trailSpacing", (float)TrailSpacingSlider.Value);

        // Particle settings
        config.Set("minParticleSize", (float)MinParticleSizeSlider.Value);
        config.Set("maxParticleSize", (float)MaxParticleSizeSlider.Value);
        config.Set("particleLifespan", (float)ParticleLifespanSlider.Value);

        // Physics settings
        config.Set("gravity", (float)GravitySlider.Value);
        config.Set("airResistance", (float)AirResistanceSlider.Value);
        config.Set("flutterAmount", (float)FlutterAmountSlider.Value);

        // Shape settings
        config.Set("useRectangles", UseRectanglesCheckBox.IsChecked ?? true);
        config.Set("useCircles", UseCirclesCheckBox.IsChecked ?? true);
        config.Set("useRibbons", UseRibbonsCheckBox.IsChecked ?? true);

        // Color settings
        config.Set("rainbowMode", RainbowModeCheckBox.IsChecked ?? true);
        config.Set("rainbowSpeed", (float)RainbowSpeedSlider.Value);

        // Performance settings
        config.Set("maxParticles", (int)MaxParticlesSlider.Value);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void BurstOnClickCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void TrailOnMoveCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void BurstCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BurstCountValue != null)
            BurstCountValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void BurstForceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BurstForceValue != null)
            BurstForceValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void TrailSpacingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TrailSpacingValue != null)
            TrailSpacingValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MinParticleSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MinParticleSizeValue != null)
            MinParticleSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MaxParticleSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxParticleSizeValue != null)
            MaxParticleSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void ParticleLifespanSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (ParticleLifespanValue != null)
            ParticleLifespanValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void GravitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GravityValue != null)
            GravityValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void AirResistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (AirResistanceValue != null)
            AirResistanceValue.Text = e.NewValue.ToString("F3");
        UpdateConfiguration();
    }

    private void FlutterAmountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FlutterAmountValue != null)
            FlutterAmountValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void UseRectanglesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void UseCirclesCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void UseRibbonsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
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

    private void MaxParticlesSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxParticlesValue != null)
            MaxParticlesValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }
}
