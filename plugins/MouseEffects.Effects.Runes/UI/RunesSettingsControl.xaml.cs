using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Runes.UI;

public partial class RunesSettingsControl : UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;
    private bool _isExpanded;

    public event Action<string>? SettingsChanged;

    public RunesSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isLoading = false;
    }

    private void LoadConfiguration()
    {
        EnabledCheckBox.IsChecked = _effect.IsEnabled;

        if (_effect.Configuration.TryGet("rn_mouseMoveEnabled", out bool moveEnabled))
            MouseMoveCheckBox.IsChecked = moveEnabled;

        if (_effect.Configuration.TryGet("rn_moveDistanceThreshold", out float moveDist))
        {
            MoveDistanceSlider.Value = moveDist;
            MoveDistanceValue.Text = moveDist.ToString("F0");
        }

        if (_effect.Configuration.TryGet("rn_leftClickEnabled", out bool leftEnabled))
            LeftClickCheckBox.IsChecked = leftEnabled;

        if (_effect.Configuration.TryGet("rn_leftClickBurstCount", out int leftCount))
        {
            LeftClickCountSlider.Value = leftCount;
            LeftClickCountValue.Text = leftCount.ToString();
        }

        if (_effect.Configuration.TryGet("rn_rightClickEnabled", out bool rightEnabled))
            RightClickCheckBox.IsChecked = rightEnabled;

        if (_effect.Configuration.TryGet("rn_rightClickBurstCount", out int rightCount))
        {
            RightClickCountSlider.Value = rightCount;
            RightClickCountValue.Text = rightCount.ToString();
        }

        if (_effect.Configuration.TryGet("rn_runeCount", out int count))
        {
            RuneCountSlider.Value = count;
            RuneCountValue.Text = count.ToString();
        }

        if (_effect.Configuration.TryGet("rn_runeSize", out float size))
        {
            RuneSizeSlider.Value = size;
            RuneSizeValue.Text = size.ToString("F0");
        }

        if (_effect.Configuration.TryGet("rn_lifetime", out float lifetime))
        {
            LifetimeSlider.Value = lifetime;
            LifetimeValue.Text = lifetime.ToString("F1") + "s";
        }

        if (_effect.Configuration.TryGet("rn_glowIntensity", out float glow))
        {
            GlowIntensitySlider.Value = glow;
            GlowIntensityValue.Text = glow.ToString("F1");
        }

        if (_effect.Configuration.TryGet("rn_rotationSpeed", out float rotSpeed))
        {
            RotationSpeedSlider.Value = rotSpeed;
            RotationSpeedValue.Text = rotSpeed.ToString("F1");
        }

        if (_effect.Configuration.TryGet("rn_floatDistance", out float floatDist))
        {
            FloatDistanceSlider.Value = floatDist;
            FloatDistanceValue.Text = floatDist.ToString("F0");
        }

        if (_effect.Configuration.TryGet("rn_rainbowMode", out bool rainbow))
            RainbowModeCheckBox.IsChecked = rainbow;

        if (_effect.Configuration.TryGet("rn_rainbowSpeed", out float rainbowSpeed))
        {
            RainbowSpeedSlider.Value = rainbowSpeed;
            RainbowSpeedValue.Text = rainbowSpeed.ToString("F1");
        }
    }

    private void UpdateConfiguration()
    {
        if (_isLoading) return;

        var config = new EffectConfiguration();

        config.Set("rn_mouseMoveEnabled", MouseMoveCheckBox.IsChecked ?? true);
        config.Set("rn_moveDistanceThreshold", (float)MoveDistanceSlider.Value);
        config.Set("rn_leftClickEnabled", LeftClickCheckBox.IsChecked ?? true);
        config.Set("rn_leftClickBurstCount", (int)LeftClickCountSlider.Value);
        config.Set("rn_rightClickEnabled", RightClickCheckBox.IsChecked ?? true);
        config.Set("rn_rightClickBurstCount", (int)RightClickCountSlider.Value);
        config.Set("rn_runeCount", (int)RuneCountSlider.Value);
        config.Set("rn_runeSize", (float)RuneSizeSlider.Value);
        config.Set("rn_lifetime", (float)LifetimeSlider.Value);
        config.Set("rn_glowIntensity", (float)GlowIntensitySlider.Value);
        config.Set("rn_rotationSpeed", (float)RotationSpeedSlider.Value);
        config.Set("rn_floatDistance", (float)FloatDistanceSlider.Value);
        config.Set("rn_rainbowMode", RainbowModeCheckBox.IsChecked ?? false);
        config.Set("rn_rainbowSpeed", (float)RainbowSpeedSlider.Value);

        if (_effect.Configuration.TryGet("rn_fixedColor", out Vector4 fixedColor))
            config.Set("rn_fixedColor", fixedColor);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void EnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
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

    private void RuneCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RuneCountValue != null)
            RuneCountValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void RuneSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RuneSizeValue != null)
            RuneSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void LifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LifetimeValue != null)
            LifetimeValue.Text = e.NewValue.ToString("F1") + "s";
        UpdateConfiguration();
    }

    private void GlowIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (GlowIntensityValue != null)
            GlowIntensityValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void RotationSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RotationSpeedValue != null)
            RotationSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void FloatDistanceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FloatDistanceValue != null)
            FloatDistanceValue.Text = e.NewValue.ToString("F0");
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
