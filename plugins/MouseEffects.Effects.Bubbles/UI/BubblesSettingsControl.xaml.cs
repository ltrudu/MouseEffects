using System.Windows;
using System.Windows.Controls;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.Bubbles.UI;

public partial class BubblesSettingsControl : UserControl
{
    private readonly IEffect _effect;
    private bool _isLoading = true;

    public event Action<string>? SettingsChanged;

    public BubblesSettingsControl(IEffect effect)
    {
        InitializeComponent();
        _effect = effect;

        LoadConfiguration();
        _isLoading = false;
    }

    private void LoadConfiguration()
    {
        // Bubble settings
        if (_effect.Configuration.TryGet("b_bubbleCount", out int count))
        {
            BubbleCountSlider.Value = count;
            BubbleCountValue.Text = count.ToString();
        }

        if (_effect.Configuration.TryGet("b_minSize", out float minSize))
        {
            MinSizeSlider.Value = minSize;
            MinSizeValue.Text = minSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("b_maxSize", out float maxSize))
        {
            MaxSizeSlider.Value = maxSize;
            MaxSizeValue.Text = maxSize.ToString("F0");
        }

        if (_effect.Configuration.TryGet("b_lifetime", out float lifetime))
        {
            LifetimeSlider.Value = lifetime;
            LifetimeValue.Text = lifetime.ToString("F0");
        }

        // Motion settings
        if (_effect.Configuration.TryGet("b_floatSpeed", out float floatSpeed))
        {
            FloatSpeedSlider.Value = floatSpeed;
            FloatSpeedValue.Text = floatSpeed.ToString("F0");
        }

        if (_effect.Configuration.TryGet("b_wobbleAmount", out float wobble))
        {
            WobbleAmountSlider.Value = wobble;
            WobbleAmountValue.Text = wobble.ToString("F0");
        }

        if (_effect.Configuration.TryGet("b_wobbleFrequency", out float freq))
        {
            WobbleFrequencySlider.Value = freq;
            WobbleFrequencyValue.Text = freq.ToString("F1");
        }

        if (_effect.Configuration.TryGet("b_driftSpeed", out float drift))
        {
            DriftSpeedSlider.Value = drift;
            DriftSpeedValue.Text = drift.ToString("F0");
        }

        // Visual effects
        if (_effect.Configuration.TryGet("b_iridescenceIntensity", out float iridInt))
        {
            IridescenceIntensitySlider.Value = iridInt;
            IridescenceIntensityValue.Text = iridInt.ToString("F1");
        }

        if (_effect.Configuration.TryGet("b_iridescenceSpeed", out float iridSpd))
        {
            IridescenceSpeedSlider.Value = iridSpd;
            IridescenceSpeedValue.Text = iridSpd.ToString("F1");
        }

        if (_effect.Configuration.TryGet("b_transparency", out float trans))
        {
            TransparencySlider.Value = trans;
            TransparencyValue.Text = trans.ToString("F2");
        }

        if (_effect.Configuration.TryGet("b_rimThickness", out float rim))
        {
            RimThicknessSlider.Value = rim;
            RimThicknessValue.Text = rim.ToString("F2");
        }

        // Pop effect
        if (_effect.Configuration.TryGet("b_popEnabled", out bool popEnabled))
        {
            PopEnabledCheckBox.IsChecked = popEnabled;
        }

        if (_effect.Configuration.TryGet("b_popDuration", out float popDur))
        {
            PopDurationSlider.Value = popDur;
            PopDurationValue.Text = popDur.ToString("F1");
        }
    }

    private void UpdateConfiguration()
    {
        if (_isLoading) return;

        var config = new EffectConfiguration();

        // Bubble settings
        config.Set("b_bubbleCount", (int)BubbleCountSlider.Value);
        config.Set("b_minSize", (float)MinSizeSlider.Value);
        config.Set("b_maxSize", (float)MaxSizeSlider.Value);
        config.Set("b_lifetime", (float)LifetimeSlider.Value);

        // Motion settings
        config.Set("b_floatSpeed", (float)FloatSpeedSlider.Value);
        config.Set("b_wobbleAmount", (float)WobbleAmountSlider.Value);
        config.Set("b_wobbleFrequency", (float)WobbleFrequencySlider.Value);
        config.Set("b_driftSpeed", (float)DriftSpeedSlider.Value);

        // Visual effects
        config.Set("b_iridescenceIntensity", (float)IridescenceIntensitySlider.Value);
        config.Set("b_iridescenceSpeed", (float)IridescenceSpeedSlider.Value);
        config.Set("b_transparency", (float)TransparencySlider.Value);
        config.Set("b_rimThickness", (float)RimThicknessSlider.Value);

        // Pop effect
        config.Set("b_popEnabled", PopEnabledCheckBox.IsChecked ?? true);
        config.Set("b_popDuration", (float)PopDurationSlider.Value);

        _effect.Configure(config);
        SettingsChanged?.Invoke(_effect.Metadata.Id);
    }

    private void BubbleCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (BubbleCountValue != null)
            BubbleCountValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MinSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MinSizeValue != null)
            MinSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void MaxSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MaxSizeValue != null)
            MaxSizeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void LifetimeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (LifetimeValue != null)
            LifetimeValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void FloatSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FloatSpeedValue != null)
            FloatSpeedValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void WobbleAmountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WobbleAmountValue != null)
            WobbleAmountValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void WobbleFrequencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (WobbleFrequencyValue != null)
            WobbleFrequencyValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void DriftSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (DriftSpeedValue != null)
            DriftSpeedValue.Text = e.NewValue.ToString("F0");
        UpdateConfiguration();
    }

    private void IridescenceIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IridescenceIntensityValue != null)
            IridescenceIntensityValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void IridescenceSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (IridescenceSpeedValue != null)
            IridescenceSpeedValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }

    private void TransparencySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TransparencyValue != null)
            TransparencyValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void RimThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RimThicknessValue != null)
            RimThicknessValue.Text = e.NewValue.ToString("F2");
        UpdateConfiguration();
    }

    private void PopEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateConfiguration();
    }

    private void PopDurationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (PopDurationValue != null)
            PopDurationValue.Text = e.NewValue.ToString("F1");
        UpdateConfiguration();
    }
}
