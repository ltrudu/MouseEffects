using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Zoom.UI;

namespace MouseEffects.Effects.Zoom;

/// <summary>
/// Factory for creating ZoomEffect instances.
/// </summary>
public sealed class ZoomEffectFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "zoom-effect",
        Name = "Zoom",
        Description = "Magnifies the area around the mouse cursor with selectable circle or rectangle shape",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create()
    {
        return new ZoomEffect();
    }

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();
        config.Set("zoomFactor", 3.4f);
        config.Set("radius", 230.2f);
        config.Set("width", 367.42f);
        config.Set("height", 369.94f);
        config.Set("shapeType", 1); // 0 = Circle, 1 = Rectangle
        config.Set("syncSizes", false);
        config.Set("borderWidth", 3.73f);
        config.Set("borderColor", new Vector4(0.2f, 0.6f, 1.0f, 1.0f));
        config.Set("enableZoomHotkey", true);
        config.Set("enableSizeHotkey", false);
        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                new ChoiceParameter
                {
                    Key = "shapeType",
                    DisplayName = "Shape",
                    Description = "The shape of the zoom lens",
                    Choices = ["Circle", "Rectangle"],
                    DefaultValue = "Circle"
                },
                new FloatParameter
                {
                    Key = "zoomFactor",
                    DisplayName = "Zoom Factor",
                    Description = "Magnification level (1.1x to 5.0x)",
                    MinValue = 1.1f,
                    MaxValue = 5.0f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "radius",
                    DisplayName = "Radius",
                    Description = "Size of the circular zoom lens (in pixels)",
                    MinValue = 20.0f,
                    MaxValue = 500.0f,
                    DefaultValue = 100.0f,
                    Step = 10.0f,
                    Group = "Circle"
                },
                new FloatParameter
                {
                    Key = "width",
                    DisplayName = "Width",
                    Description = "Width of the rectangular zoom lens (in pixels)",
                    MinValue = 40.0f,
                    MaxValue = 800.0f,
                    DefaultValue = 200.0f,
                    Step = 10.0f,
                    Group = "Rectangle"
                },
                new FloatParameter
                {
                    Key = "height",
                    DisplayName = "Height",
                    Description = "Height of the rectangular zoom lens (in pixels)",
                    MinValue = 40.0f,
                    MaxValue = 800.0f,
                    DefaultValue = 150.0f,
                    Step = 10.0f,
                    Group = "Rectangle"
                },
                new BoolParameter
                {
                    Key = "syncSizes",
                    DisplayName = "Square (Sync Width/Height)",
                    Description = "Make width and height equal for a square shape",
                    DefaultValue = false,
                    Group = "Rectangle"
                },
                new FloatParameter
                {
                    Key = "borderWidth",
                    DisplayName = "Border Width",
                    Description = "Width of the zoom lens border (in pixels)",
                    MinValue = 0.0f,
                    MaxValue = 10.0f,
                    DefaultValue = 2.0f,
                    Step = 0.5f
                },
                new ColorParameter
                {
                    Key = "borderColor",
                    DisplayName = "Border Color",
                    Description = "Color of the zoom lens border",
                    DefaultValue = new Vector4(0.2f, 0.6f, 1.0f, 1.0f),
                    SupportsAlpha = true
                },
                new BoolParameter
                {
                    Key = "enableZoomHotkey",
                    DisplayName = "Zoom Hotkey (Shift+Ctrl+Wheel)",
                    Description = "Enable Shift+Ctrl+Mouse Wheel to adjust zoom factor by 0.1",
                    DefaultValue = false,
                    Group = "Hotkeys"
                },
                new BoolParameter
                {
                    Key = "enableSizeHotkey",
                    DisplayName = "Size Hotkey (Shift+Alt+Wheel)",
                    Description = "Enable Shift+Alt+Mouse Wheel to adjust size by 5%",
                    DefaultValue = false,
                    Group = "Hotkeys"
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect)
    {
        return new ZoomSettingsControl(effect);
    }
}
