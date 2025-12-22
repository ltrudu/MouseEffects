using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MouseEffects.Core.Effects;

namespace MouseEffects.Effects.ProceduralSigil.UI;

public partial class ProceduralSigilSettingsControl : System.Windows.Controls.UserControl
{
    private readonly ProceduralSigilEffect _effect;
    private bool _isLoading = true; // Start true to prevent events during init

    public ProceduralSigilSettingsControl(IEffect effect)
    {
        _effect = (ProceduralSigilEffect)effect;
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _isLoading = true;

        // Alpha
        SigilAlphaSlider.Value = _effect.SigilAlpha;

        // Style
        SigilStyleCombo.SelectedIndex = (int)_effect.Style;
        UpdateStylePanelsVisibility();

        // Position
        PositionModeCombo.SelectedIndex = (int)_effect.Position;
        SigilRadiusSlider.Value = _effect.SigilRadius;
        FadeDurationSlider.Value = _effect.FadeDuration;
        UpdateFadeDurationVisibility();

        // Appearance
        ColorPresetCombo.SelectedIndex = (int)_effect.Preset;
        UpdateSigilCustomColorsVisibility();
        UpdateSigilColorPreviews();
        GlowIntensitySlider.Value = _effect.GlowIntensity;
        LineThicknessSlider.Value = _effect.LineThickness;

        // Layers
        LayerCenterCheckBox.IsChecked = (_effect.Layers & ProceduralSigilEffect.SigilLayers.Center) != 0;
        LayerInnerCheckBox.IsChecked = (_effect.Layers & ProceduralSigilEffect.SigilLayers.Inner) != 0;
        LayerMiddleCheckBox.IsChecked = (_effect.Layers & ProceduralSigilEffect.SigilLayers.Middle) != 0;
        LayerRunesCheckBox.IsChecked = (_effect.Layers & ProceduralSigilEffect.SigilLayers.Runes) != 0;
        LayerGlowCheckBox.IsChecked = (_effect.Layers & ProceduralSigilEffect.SigilLayers.Glow) != 0;

        // Animation
        RotationSpeedSlider.Value = _effect.RotationSpeed;
        CounterRotateCheckBox.IsChecked = _effect.CounterRotateLayers;
        PulseCheckBox.IsChecked = (_effect.Animations & ProceduralSigilEffect.SigilAnimations.Pulse) != 0;
        PulseSpeedSlider.Value = _effect.PulseSpeed;
        MorphCheckBox.IsChecked = (_effect.Animations & ProceduralSigilEffect.SigilAnimations.Morph) != 0;
        RuneScrollSpeedSlider.Value = _effect.RuneScrollSpeed;

        // Triangle Mandala
        TriangleLayersSlider.Value = _effect.TriangleLayers;
        ZoomSpeedSlider.Value = _effect.ZoomSpeed;
        ZoomAmountSlider.Value = _effect.ZoomAmount;
        InnerTrianglesSlider.Value = _effect.InnerTriangles;
        FractalDepthSlider.Value = _effect.FractalDepth;

        // Moon
        MoonPhaseRotationSlider.Value = _effect.MoonPhaseRotationSpeed;
        ZodiacRotationSlider.Value = _effect.ZodiacRotationSpeed;
        TreeOfLifeScaleSlider.Value = _effect.TreeOfLifeScale;
        CosmicGlowSlider.Value = _effect.CosmicGlowIntensity;

        // Energy Particles
        ParticleTypeCombo.SelectedIndex = (int)_effect.ParticleType;
        ParticleIntensitySlider.Value = _effect.ParticleIntensity;
        ParticleSpeedSlider.Value = _effect.ParticleSpeed;
        ParticleEntropySlider.Value = _effect.ParticleEntropy;
        ParticleSizeSlider.Value = _effect.ParticleSize;
        FireRiseHeightSlider.Value = _effect.FireRiseHeight;
        ElectricitySpreadSlider.Value = _effect.ElectricitySpread;

        // Wind
        WindEnabledCheckBox.IsChecked = _effect.WindEnabled;
        WindStrengthSlider.Value = _effect.WindStrength;
        WindTurbulenceSlider.Value = _effect.WindTurbulence;

        // Fire Particles
        FireParticleEnabledCheckBox.IsChecked = _effect.FireParticleEnabled;
        FireSpawnLocationCombo.SelectedIndex = (int)_effect.FireSpawnLoc;
        FireRenderOrderCombo.SelectedIndex = (int)_effect.FireRenderOrd;
        FireColorPaletteCombo.SelectedIndex = (int)_effect.FirePalette;
        UpdateFireCustomColorsVisibility();

        // Custom colors
        UpdateFireColorPreviews();

        FireParticleAlphaSlider.Value = _effect.FireParticleAlpha;
        FireParticleCountSlider.Value = _effect.FireParticleCount;
        FireSpawnRateSlider.Value = _effect.FireSpawnRate;
        FireParticleSizeSlider.Value = _effect.FireParticleSize;
        FireLifetimeSlider.Value = _effect.FireLifetime;
        FireRiseSpeedSlider.Value = _effect.FireRiseSpeed;
        FireTurbulenceSlider.Value = _effect.FireTurbulence;
        FireWindEnabledCheckBox.IsChecked = _effect.FireWindEnabled;

        _isLoading = false;
    }

    private void UpdateStylePanelsVisibility()
    {
        var style = (ProceduralSigilEffect.SigilStyle)SigilStyleCombo.SelectedIndex;
        TriangleMandalaPanel.Visibility = style == ProceduralSigilEffect.SigilStyle.TriangleMandala
            ? Visibility.Visible : Visibility.Collapsed;
        MoonPanel.Visibility = style == ProceduralSigilEffect.SigilStyle.Moon
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void SigilAlphaSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.SigilAlpha = (float)e.NewValue;
        _effect.Configuration.Set("sigilAlpha", (float)e.NewValue);
    }

    private void SigilStyleCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        var style = (ProceduralSigilEffect.SigilStyle)SigilStyleCombo.SelectedIndex;
        _effect.Style = style;
        _effect.Configuration.Set("sigilStyle", (int)style);
        UpdateStylePanelsVisibility();
    }

    private void UpdateFadeDurationVisibility()
    {
        var mode = (ProceduralSigilEffect.PositionMode)PositionModeCombo.SelectedIndex;
        bool showFade = mode == ProceduralSigilEffect.PositionMode.ClickToSummon ||
                       mode == ProceduralSigilEffect.PositionMode.ClickAtCursor;
        FadeDurationLabel.Visibility = showFade ? Visibility.Visible : Visibility.Collapsed;
        FadeDurationGrid.Visibility = showFade ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PositionModeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        var mode = (ProceduralSigilEffect.PositionMode)PositionModeCombo.SelectedIndex;
        _effect.Position = mode;
        _effect.Configuration.Set("positionMode", (int)mode);
        UpdateFadeDurationVisibility();
    }

    private void SigilRadiusSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.SigilRadius = (float)e.NewValue;
        _effect.Configuration.Set("sigilRadius", (float)e.NewValue);
    }

    private void FadeDurationSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.FadeDuration = (float)e.NewValue;
        _effect.Configuration.Set("fadeDuration", (float)e.NewValue);
    }

    private void ColorPresetCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        var preset = (ProceduralSigilEffect.ColorPreset)ColorPresetCombo.SelectedIndex;
        _effect.Preset = preset;
        _effect.Configuration.Set("colorPreset", (int)preset);
        UpdateSigilCustomColorsVisibility();
    }

    private void UpdateSigilCustomColorsVisibility()
    {
        var preset = (ProceduralSigilEffect.ColorPreset)ColorPresetCombo.SelectedIndex;
        SigilCustomColorsPanel.Visibility = preset == ProceduralSigilEffect.ColorPreset.Custom
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateSigilColorPreviews()
    {
        var core = _effect.CoreColor;
        var mid = _effect.MidColor;
        var edge = _effect.EdgeColor;

        SigilCoreColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromScRgb(1f, core.X, core.Y, core.Z));
        SigilMidColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromScRgb(1f, mid.X, mid.Y, mid.Z));
        SigilEdgeColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromScRgb(1f, edge.X, edge.Y, edge.Z));
    }

    private void SigilCoreColorButton_Click(object sender, RoutedEventArgs e)
    {
        var currentColor = _effect.CoreColor;
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(255,
            (int)(currentColor.X * 255),
            (int)(currentColor.Y * 255),
            (int)(currentColor.Z * 255));
        dialog.FullOpen = true;

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var newColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                1.0f);
            _effect.CoreColor = newColor;
            _effect.Configuration.Set("coreColor", newColor);
            UpdateSigilColorPreviews();
        }
    }

    private void SigilMidColorButton_Click(object sender, RoutedEventArgs e)
    {
        var currentColor = _effect.MidColor;
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(255,
            (int)(currentColor.X * 255),
            (int)(currentColor.Y * 255),
            (int)(currentColor.Z * 255));
        dialog.FullOpen = true;

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var newColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                1.0f);
            _effect.MidColor = newColor;
            _effect.Configuration.Set("midColor", newColor);
            UpdateSigilColorPreviews();
        }
    }

    private void SigilEdgeColorButton_Click(object sender, RoutedEventArgs e)
    {
        var currentColor = _effect.EdgeColor;
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(255,
            (int)(currentColor.X * 255),
            (int)(currentColor.Y * 255),
            (int)(currentColor.Z * 255));
        dialog.FullOpen = true;

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var newColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                1.0f);
            _effect.EdgeColor = newColor;
            _effect.Configuration.Set("edgeColor", newColor);
            UpdateSigilColorPreviews();
        }
    }

    private void GlowIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.GlowIntensity = (float)e.NewValue;
        _effect.Configuration.Set("glowIntensity", (float)e.NewValue);
    }

    private void LineThicknessSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.LineThickness = (float)e.NewValue;
        _effect.Configuration.Set("lineThickness", (float)e.NewValue);
    }

    private void LayerCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        var layers = ProceduralSigilEffect.SigilLayers.None;
        if (LayerCenterCheckBox.IsChecked == true) layers |= ProceduralSigilEffect.SigilLayers.Center;
        if (LayerInnerCheckBox.IsChecked == true) layers |= ProceduralSigilEffect.SigilLayers.Inner;
        if (LayerMiddleCheckBox.IsChecked == true) layers |= ProceduralSigilEffect.SigilLayers.Middle;
        if (LayerRunesCheckBox.IsChecked == true) layers |= ProceduralSigilEffect.SigilLayers.Runes;
        if (LayerGlowCheckBox.IsChecked == true) layers |= ProceduralSigilEffect.SigilLayers.Glow;

        _effect.Layers = layers;
        _effect.Configuration.Set("layerFlags", (uint)layers);
    }

    private void RotationSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.RotationSpeed = (float)e.NewValue;
        _effect.Configuration.Set("rotationSpeed", (float)e.NewValue);
    }

    private void CounterRotateCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        _effect.CounterRotateLayers = CounterRotateCheckBox.IsChecked == true;
        _effect.Configuration.Set("counterRotateLayers", _effect.CounterRotateLayers);
    }

    private void AnimationCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        var animations = ProceduralSigilEffect.SigilAnimations.Rotate; // Always enabled
        if (PulseCheckBox.IsChecked == true) animations |= ProceduralSigilEffect.SigilAnimations.Pulse;
        if (MorphCheckBox.IsChecked == true) animations |= ProceduralSigilEffect.SigilAnimations.Morph;

        _effect.Animations = animations;
        _effect.Configuration.Set("animationFlags", (uint)animations);
    }

    private void PulseSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.PulseSpeed = (float)e.NewValue;
        _effect.Configuration.Set("pulseSpeed", (float)e.NewValue);
    }

    private void RuneScrollSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.RuneScrollSpeed = (float)e.NewValue;
        _effect.Configuration.Set("runeScrollSpeed", (float)e.NewValue);
    }

    // Triangle Mandala event handlers
    private void TriangleLayersSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.TriangleLayers = (int)e.NewValue;
        _effect.Configuration.Set("triangleLayers", (int)e.NewValue);
    }

    private void ZoomSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.ZoomSpeed = (float)e.NewValue;
        _effect.Configuration.Set("zoomSpeed", (float)e.NewValue);
    }

    private void ZoomAmountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.ZoomAmount = (float)e.NewValue;
        _effect.Configuration.Set("zoomAmount", (float)e.NewValue);
    }

    private void InnerTrianglesSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.InnerTriangles = (int)e.NewValue;
        _effect.Configuration.Set("innerTriangles", (int)e.NewValue);
    }

    private void FractalDepthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.FractalDepth = (float)e.NewValue;
        _effect.Configuration.Set("fractalDepth", (float)e.NewValue);
    }

    // Moon event handlers
    private void MoonPhaseRotationSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.MoonPhaseRotationSpeed = (float)e.NewValue;
        _effect.Configuration.Set("moonPhaseRotationSpeed", (float)e.NewValue);
    }

    private void ZodiacRotationSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.ZodiacRotationSpeed = (float)e.NewValue;
        _effect.Configuration.Set("zodiacRotationSpeed", (float)e.NewValue);
    }

    private void TreeOfLifeScaleSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.TreeOfLifeScale = (float)e.NewValue;
        _effect.Configuration.Set("treeOfLifeScale", (float)e.NewValue);
    }

    private void CosmicGlowSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.CosmicGlowIntensity = (float)e.NewValue;
        _effect.Configuration.Set("cosmicGlowIntensity", (float)e.NewValue);
    }

    // Energy particle event handlers
    private void ParticleTypeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        _effect.ParticleType = (uint)ParticleTypeCombo.SelectedIndex;
        _effect.Configuration.Set("particleType", ParticleTypeCombo.SelectedIndex);
    }

    private void ParticleIntensitySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.ParticleIntensity = (float)e.NewValue;
        _effect.Configuration.Set("particleIntensity", (float)e.NewValue);
    }

    private void ParticleSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.ParticleSpeed = (float)e.NewValue;
        _effect.Configuration.Set("particleSpeed", (float)e.NewValue);
    }

    private void ParticleEntropySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.ParticleEntropy = (float)e.NewValue;
        _effect.Configuration.Set("particleEntropy", (float)e.NewValue);
    }

    private void ParticleSizeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.ParticleSize = (float)e.NewValue;
        _effect.Configuration.Set("particleSize", (float)e.NewValue);
    }

    private void FireRiseHeightSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.FireRiseHeight = (float)e.NewValue;
        _effect.Configuration.Set("fireRiseHeight", (float)e.NewValue);
    }

    private void ElectricitySpreadSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.ElectricitySpread = (float)e.NewValue;
        _effect.Configuration.Set("electricitySpread", (float)e.NewValue);
    }

    // Wind event handlers
    private void WindEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        _effect.WindEnabled = WindEnabledCheckBox.IsChecked == true;
        _effect.Configuration.Set("windEnabled", _effect.WindEnabled);
    }

    private void WindStrengthSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.WindStrength = (float)e.NewValue;
        _effect.Configuration.Set("windStrength", (float)e.NewValue);
    }

    private void WindTurbulenceSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.WindTurbulence = (float)e.NewValue;
        _effect.Configuration.Set("windTurbulence", (float)e.NewValue);
    }

    // Fire particle event handlers
    private void FireParticleEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        _effect.FireParticleEnabled = FireParticleEnabledCheckBox.IsChecked == true;
        _effect.Configuration.Set("fireParticleEnabled", _effect.FireParticleEnabled);
    }

    private void FireSpawnLocationCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        var location = (ProceduralSigilEffect.FireSpawnLocation)FireSpawnLocationCombo.SelectedIndex;
        _effect.FireSpawnLoc = location;
        _effect.Configuration.Set("fireSpawnLocation", (int)location);
    }

    private void FireRenderOrderCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        var order = (ProceduralSigilEffect.FireRenderOrder)FireRenderOrderCombo.SelectedIndex;
        _effect.FireRenderOrd = order;
        _effect.Configuration.Set("fireRenderOrder", (int)order);
    }

    private void FireColorPaletteCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        var palette = (ProceduralSigilEffect.FireColorPalette)FireColorPaletteCombo.SelectedIndex;
        _effect.FirePalette = palette;
        _effect.Configuration.Set("fireColorPalette", (int)palette);
        UpdateFireCustomColorsVisibility();
    }

    private void UpdateFireCustomColorsVisibility()
    {
        var palette = (ProceduralSigilEffect.FireColorPalette)FireColorPaletteCombo.SelectedIndex;
        FireCustomColorsPanel.Visibility = palette == ProceduralSigilEffect.FireColorPalette.Custom
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateFireColorPreviews()
    {
        var core = _effect.FireCustomCoreColor;
        var mid = _effect.FireCustomMidColor;
        var edge = _effect.FireCustomEdgeColor;

        FireCoreColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromScRgb(1f, core.X, core.Y, core.Z));
        FireMidColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromScRgb(1f, mid.X, mid.Y, mid.Z));
        FireEdgeColorPreview.Background = new SolidColorBrush(System.Windows.Media.Color.FromScRgb(1f, edge.X, edge.Y, edge.Z));
    }

    private void FireCoreColorButton_Click(object sender, RoutedEventArgs e)
    {
        var currentColor = _effect.FireCustomCoreColor;
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(255,
            (int)(currentColor.X * 255),
            (int)(currentColor.Y * 255),
            (int)(currentColor.Z * 255));
        dialog.FullOpen = true;

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var newColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                1.0f);
            _effect.FireCustomCoreColor = newColor;
            _effect.Configuration.Set("fireCustomCoreColor", newColor);
            UpdateFireColorPreviews();
        }
    }

    private void FireMidColorButton_Click(object sender, RoutedEventArgs e)
    {
        var currentColor = _effect.FireCustomMidColor;
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(255,
            (int)(currentColor.X * 255),
            (int)(currentColor.Y * 255),
            (int)(currentColor.Z * 255));
        dialog.FullOpen = true;

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var newColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                1.0f);
            _effect.FireCustomMidColor = newColor;
            _effect.Configuration.Set("fireCustomMidColor", newColor);
            UpdateFireColorPreviews();
        }
    }

    private void FireEdgeColorButton_Click(object sender, RoutedEventArgs e)
    {
        var currentColor = _effect.FireCustomEdgeColor;
        using var dialog = new System.Windows.Forms.ColorDialog();
        dialog.Color = System.Drawing.Color.FromArgb(255,
            (int)(currentColor.X * 255),
            (int)(currentColor.Y * 255),
            (int)(currentColor.Z * 255));
        dialog.FullOpen = true;

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var newColor = new Vector4(
                dialog.Color.R / 255f,
                dialog.Color.G / 255f,
                dialog.Color.B / 255f,
                1.0f);
            _effect.FireCustomEdgeColor = newColor;
            _effect.Configuration.Set("fireCustomEdgeColor", newColor);
            UpdateFireColorPreviews();
        }
    }

    private void FireParticleAlphaSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.FireParticleAlpha = (float)e.NewValue;
        _effect.Configuration.Set("fireParticleAlpha", (float)e.NewValue);
    }

    private void FireParticleCountSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.FireParticleCount = (int)e.NewValue;
        _effect.Configuration.Set("fireParticleCount", (int)e.NewValue);
    }

    private void FireSpawnRateSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.FireSpawnRate = (float)e.NewValue;
        _effect.Configuration.Set("fireSpawnRate", (float)e.NewValue);
    }

    private void FireParticleSizeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.FireParticleSize = (float)e.NewValue;
        _effect.Configuration.Set("fireParticleSize", (float)e.NewValue);
    }

    private void FireLifetimeSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.FireLifetime = (float)e.NewValue;
        _effect.Configuration.Set("fireLifetime", (float)e.NewValue);
    }

    private void FireRiseSpeedSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.FireRiseSpeed = (float)e.NewValue;
        _effect.Configuration.Set("fireRiseSpeed", (float)e.NewValue);
    }

    private void FireTurbulenceSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isLoading) return;
        _effect.FireTurbulence = (float)e.NewValue;
        _effect.Configuration.Set("fireTurbulence", (float)e.NewValue);
    }

    private void FireWindEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        _effect.FireWindEnabled = FireWindEnabledCheckBox.IsChecked == true;
        _effect.Configuration.Set("fireWindEnabled", _effect.FireWindEnabled);
    }
}
