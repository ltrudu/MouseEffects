using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.GravityWell.UI;

public partial class GravityWellSettingsControl : UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;

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
        // Reset settings
        if (_effect.Configuration.TryGet("gw_resetOnLeftClick", out bool resetLeft))
        {
            ResetOnLeftClickCheckBox.IsChecked = resetLeft;
        }

        if (_effect.Configuration.TryGet("gw_resetOnRightClick", out bool resetRight))
        {
            ResetOnRightClickCheckBox.IsChecked = resetRight;
        }

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

        if (_effect.Configuration.TryGet("gw_gravityRadius", out float radius))
        {
            GravityRadiusSlider.Value = radius;
            GravityRadiusValue.Text = radius.ToString("F0");
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

        if (_effect.Configuration.TryGet("gw_edgeBehavior", out int edgeBehavior))
        {
            EdgeBehaviorCombo.SelectedIndex = edgeBehavior;
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

        // Trigger settings
        if (_effect.Configuration.TryGet("gw_triggerAlwaysActive", out bool triggerAlways))
        {
            TriggerAlwaysActiveCheckBox.IsChecked = triggerAlways;
        }

        if (_effect.Configuration.TryGet("gw_triggerOnLeftMouseDown", out bool triggerLeft))
        {
            TriggerOnLeftMouseDownCheckBox.IsChecked = triggerLeft;
        }

        if (_effect.Configuration.TryGet("gw_triggerOnRightMouseDown", out bool triggerRight))
        {
            TriggerOnRightMouseDownCheckBox.IsChecked = triggerRight;
        }

        if (_effect.Configuration.TryGet("gw_triggerOnMouseMove", out bool triggerMove))
        {
            TriggerOnMouseMoveCheckBox.IsChecked = triggerMove;
        }

        if (_effect.Configuration.TryGet("gw_mouseMoveTimeMultiplier", out float timeMultiplier))
        {
            MouseMoveTimeMultiplierSlider.Value = timeMultiplier;
            MouseMoveTimeMultiplierValue.Text = timeMultiplier.ToString("F1") + "x";
        }

        // Drift settings
        if (_effect.Configuration.TryGet("gw_driftEnabled", out bool driftEnabled))
        {
            DriftEnabledCheckBox.IsChecked = driftEnabled;
        }

        if (_effect.Configuration.TryGet("gw_driftAmount", out float driftAmount))
        {
            DriftAmountSlider.Value = driftAmount;
            DriftAmountValue.Text = driftAmount.ToString("F2");
        }
    }

    private void UpdateConfiguration()
    {
        if (_isLoading) return;

        var config = new EffectConfiguration();

        // Reset settings
        config.Set("gw_resetOnLeftClick", ResetOnLeftClickCheckBox.IsChecked ?? false);
        config.Set("gw_resetOnRightClick", ResetOnRightClickCheckBox.IsChecked ?? false);

        // Particle settings
        config.Set("gw_particleCount", (int)ParticleCountSlider.Value);
        config.Set("gw_particleSize", (float)ParticleSizeSlider.Value);
        config.Set("gw_randomColors", RandomColorsCheckBox.IsChecked ?? false);

        // Physics settings
        config.Set("gw_gravityMode", GravityModeCombo.SelectedIndex);
        config.Set("gw_gravityStrength", (float)GravityStrengthSlider.Value);
        config.Set("gw_gravityRadius", (float)GravityRadiusSlider.Value);
        config.Set("gw_orbitSpeed", (float)OrbitSpeedSlider.Value);
        config.Set("gw_damping", (float)DampingSlider.Value);
        config.Set("gw_edgeBehavior", EdgeBehaviorCombo.SelectedIndex);

        // Trail settings
        config.Set("gw_trailEnabled", TrailEnabledCheckBox.IsChecked ?? true);
        config.Set("gw_trailLength", (float)TrailLengthSlider.Value);

        // Trigger settings
        config.Set("gw_triggerAlwaysActive", TriggerAlwaysActiveCheckBox.IsChecked ?? true);
        config.Set("gw_triggerOnLeftMouseDown", TriggerOnLeftMouseDownCheckBox.IsChecked ?? false);
        config.Set("gw_triggerOnRightMouseDown", TriggerOnRightMouseDownCheckBox.IsChecked ?? false);
        config.Set("gw_triggerOnMouseMove", TriggerOnMouseMoveCheckBox.IsChecked ?? false);
        config.Set("gw_mouseMoveTimeMultiplier", (float)MouseMoveTimeMultiplierSlider.Value);

        // Drift settings
        config.Set("gw_driftEnabled", DriftEnabledCheckBox.IsChecked ?? false);
        config.Set("gw_driftAmount", (float)DriftAmountSlider.Value);

        _effect.Configure(config);
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

    private void GravityRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GravityRadiusValue != null)
            GravityRadiusValue.Text = e.NewValue.ToString("F0");
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

    private void EdgeBehaviorCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
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

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        if (_effect is GravityWellEffect gravityWell)
        {
            gravityWell.ResetParticles();
        }
    }

    private void ResetOnLeftClickCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void ResetOnRightClickCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void TriggerCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void MouseMoveTimeMultiplierSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MouseMoveTimeMultiplierValue != null)
            MouseMoveTimeMultiplierValue.Text = e.NewValue.ToString("F1") + "x";
        UpdateConfiguration();
    }

    private void DriftEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void DriftAmountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DriftAmountValue != null)
            DriftAmountValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }
}
