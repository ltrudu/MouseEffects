using System.Numerics;
using System.Windows;
using Button = System.Windows.Controls.Button;
using UserControl = System.Windows.Controls.UserControl;
using WpfColor = System.Windows.Media.Color;

namespace MouseEffects.Effects.DNAHelix.UI;

public partial class DNAHelixSettingsControl : UserControl
{
    private readonly DNAHelixEffect _effect;
    private bool _isLoading = true;

    public DNAHelixSettingsControl(DNAHelixEffect effect)
    {
        _effect = effect;
        InitializeComponent();
        LoadSettings();
        _isLoading = false;
    }

    private void LoadSettings()
    {
        // Structure
        HelixHeightSlider.Value = _effect.HelixHeight;
        HelixRadiusSlider.Value = _effect.HelixRadius;
        TwistRateSlider.Value = _effect.TwistRate;
        BasePairCountSlider.Value = _effect.BasePairCount;

        // Animation
        RotationSpeedSlider.Value = _effect.RotationSpeed;

        // Appearance
        StrandThicknessSlider.Value = _effect.StrandThickness;
        GlowIntensitySlider.Value = _effect.GlowIntensity;

        // Colors
        UpdateColorButton(Strand1ColorButton, _effect.Strand1Color);
        UpdateColorButton(Strand2ColorButton, _effect.Strand2Color);
        UpdateColorButton(BasePair1ColorButton, _effect.BasePairColor1);
        UpdateColorButton(BasePair2ColorButton, _effect.BasePairColor2);
    }

    private void UpdateColorButton(Button button, Vector3 color)
    {
        byte r = (byte)(color.X * 255);
        byte g = (byte)(color.Y * 255);
        byte b = (byte)(color.Z * 255);
        button.Background = new System.Windows.Media.SolidColorBrush(WpfColor.FromRgb(r, g, b));
    }

    private void HelixHeightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.HelixHeight = (float)HelixHeightSlider.Value;
        _effect.Configuration.Set("helixHeight", _effect.HelixHeight);
    }

    private void HelixRadiusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.HelixRadius = (float)HelixRadiusSlider.Value;
        _effect.Configuration.Set("helixRadius", _effect.HelixRadius);
    }

    private void TwistRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.TwistRate = (float)TwistRateSlider.Value;
        _effect.Configuration.Set("twistRate", _effect.TwistRate);
    }

    private void BasePairCountSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.BasePairCount = (int)BasePairCountSlider.Value;
        _effect.Configuration.Set("basePairCount", _effect.BasePairCount);
    }

    private void RotationSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.RotationSpeed = (float)RotationSpeedSlider.Value;
        _effect.Configuration.Set("rotationSpeed", _effect.RotationSpeed);
    }

    private void StrandThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.StrandThickness = (float)StrandThicknessSlider.Value;
        _effect.Configuration.Set("strandThickness", _effect.StrandThickness);
    }

    private void GlowIntensitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.GlowIntensity = (float)GlowIntensitySlider.Value;
        _effect.Configuration.Set("glowIntensity", _effect.GlowIntensity);
    }

    private void Strand1ColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (PickColor(out Vector3 color))
        {
            _effect.Strand1Color = color;
            _effect.Configuration.Set("strand1ColorR", color.X);
            _effect.Configuration.Set("strand1ColorG", color.Y);
            _effect.Configuration.Set("strand1ColorB", color.Z);
            UpdateColorButton(Strand1ColorButton, color);
        }
    }

    private void Strand2ColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (PickColor(out Vector3 color))
        {
            _effect.Strand2Color = color;
            _effect.Configuration.Set("strand2ColorR", color.X);
            _effect.Configuration.Set("strand2ColorG", color.Y);
            _effect.Configuration.Set("strand2ColorB", color.Z);
            UpdateColorButton(Strand2ColorButton, color);
        }
    }

    private void BasePair1ColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (PickColor(out Vector3 color))
        {
            _effect.BasePairColor1 = color;
            _effect.Configuration.Set("basePair1ColorR", color.X);
            _effect.Configuration.Set("basePair1ColorG", color.Y);
            _effect.Configuration.Set("basePair1ColorB", color.Z);
            UpdateColorButton(BasePair1ColorButton, color);
        }
    }

    private void BasePair2ColorButton_Click(object sender, RoutedEventArgs e)
    {
        if (PickColor(out Vector3 color))
        {
            _effect.BasePairColor2 = color;
            _effect.Configuration.Set("basePair2ColorR", color.X);
            _effect.Configuration.Set("basePair2ColorG", color.Y);
            _effect.Configuration.Set("basePair2ColorB", color.Z);
            UpdateColorButton(BasePair2ColorButton, color);
        }
    }

    private bool PickColor(out Vector3 color)
    {
        color = Vector3.Zero;

        var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            AnyColor = true
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            color = new Vector3(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f
            );
            return true;
        }

        return false;
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        _isLoading = true;

        // Reset to defaults
        _effect.HelixHeight = 400f;
        _effect.HelixRadius = 50f;
        _effect.TwistRate = 0.03f;
        _effect.BasePairCount = 12;
        _effect.RotationSpeed = 1.0f;
        _effect.StrandThickness = 4.0f;
        _effect.GlowIntensity = 0.8f;
        _effect.Strand1Color = new Vector3(0.255f, 0.412f, 0.882f);
        _effect.Strand2Color = new Vector3(0.863f, 0.078f, 0.235f);
        _effect.BasePairColor1 = new Vector3(0.196f, 0.804f, 0.196f);
        _effect.BasePairColor2 = new Vector3(1.0f, 0.843f, 0.0f);

        // Save to config
        _effect.Configuration.Set("helixHeight", _effect.HelixHeight);
        _effect.Configuration.Set("helixRadius", _effect.HelixRadius);
        _effect.Configuration.Set("twistRate", _effect.TwistRate);
        _effect.Configuration.Set("basePairCount", _effect.BasePairCount);
        _effect.Configuration.Set("rotationSpeed", _effect.RotationSpeed);
        _effect.Configuration.Set("strandThickness", _effect.StrandThickness);
        _effect.Configuration.Set("glowIntensity", _effect.GlowIntensity);
        _effect.Configuration.Set("strand1ColorR", _effect.Strand1Color.X);
        _effect.Configuration.Set("strand1ColorG", _effect.Strand1Color.Y);
        _effect.Configuration.Set("strand1ColorB", _effect.Strand1Color.Z);
        _effect.Configuration.Set("strand2ColorR", _effect.Strand2Color.X);
        _effect.Configuration.Set("strand2ColorG", _effect.Strand2Color.Y);
        _effect.Configuration.Set("strand2ColorB", _effect.Strand2Color.Z);
        _effect.Configuration.Set("basePair1ColorR", _effect.BasePairColor1.X);
        _effect.Configuration.Set("basePair1ColorG", _effect.BasePairColor1.Y);
        _effect.Configuration.Set("basePair1ColorB", _effect.BasePairColor1.Z);
        _effect.Configuration.Set("basePair2ColorR", _effect.BasePairColor2.X);
        _effect.Configuration.Set("basePair2ColorG", _effect.BasePairColor2.Y);
        _effect.Configuration.Set("basePair2ColorB", _effect.BasePairColor2.Z);

        LoadSettings();
        _isLoading = false;
    }

}
