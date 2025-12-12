using System.Numerics;
using MouseEffects.Core.Effects;
using MouseEffects.Effects.Tesla.UI;

namespace MouseEffects.Effects.Tesla;

public sealed class TeslaFactory : IEffectFactory
{
    private static readonly EffectMetadata _metadata = new()
    {
        Id = "tesla",
        Name = "Tesla",
        Description = "Creates electrical lightning bolt effects around the mouse cursor",
        Author = "MouseEffects",
        Version = new Version(1, 0, 0),
        Category = EffectCategory.Visual
    };

    public EffectMetadata Metadata => _metadata;

    public IEffect Create() => new TeslaEffect();

    public EffectConfiguration GetDefaultConfiguration()
    {
        var config = new EffectConfiguration();

        // Selected effect type for UI (0 = Lightning Bolt, 1 = Electrical Follow)
        config.Set("selectedEffectType", 0);

        // Lightning Bolt trigger settings (lb_ prefix)
        config.Set("lb_mouseMoveEnabled", true);
        config.Set("lb_leftClickEnabled", true);
        config.Set("lb_rightClickEnabled", true);

        // Electrical Follow trigger settings (ef_ prefix)
        config.Set("ef_mouseMoveEnabled", false);

        // Legacy trigger settings (ts_ prefix) - kept for backward compatibility
        config.Set("ts_mouseMoveEffect", (int)TriggerType.ElectricalFollow);
        config.Set("ts_leftClickEffect", (int)TriggerType.None);
        config.Set("ts_rightClickEffect", (int)TriggerType.None);

        // Move trigger (mt_ prefix)
        config.Set("mt_distanceThreshold", 83f);
        config.Set("mt_randomDistanceEnabled", true);
        config.Set("mt_directionMode", (int)DirectionMode.VelocityBased);

        // Click trigger (ct_ prefix)
        config.Set("ct_directionMode", (int)DirectionMode.AllDirections);
        config.Set("ct_spreadAngle", 252.54f);

        // Core settings (core_ prefix)
        config.Set("core_enabled", false);
        config.Set("core_radius", 11.57f);
        config.Set("core_color", new Vector4(0.2f, 0.4f, 1f, 1f));

        // Bolt count (bc_ prefix)
        config.Set("bc_randomCount", true);
        config.Set("bc_minCount", 4);
        config.Set("bc_maxCount", 8);
        config.Set("bc_fixedCount", 4);

        // Bolt appearance (ba_ prefix)
        config.Set("ba_minLength", 300f);
        config.Set("ba_maxLength", 500f);
        config.Set("ba_thickness", 0.61f);
        config.Set("ba_branchProbability", 0.86f);

        // Colors (col_ prefix)
        config.Set("col_glow", new Vector4(0.3f, 0.5f, 1f, 1f));
        config.Set("col_randomVariation", true);
        config.Set("col_rainbowMode", true);
        config.Set("col_rainbowSpeed", 0.5f);

        // Timing (time_ prefix)
        config.Set("time_boltLifetime", 0.94f);
        config.Set("time_flickerSpeed", 29.52f);
        config.Set("time_fadeDuration", 0.30f);

        // Glow
        config.Set("glow_intensity", 1.62f);

        // ===== Electrical Follow Configuration (ef_ prefix) =====
        // General
        config.Set("ef_maxPieces", 100);
        config.Set("ef_pieceSize", 22.28f);
        config.Set("ef_lifetime", 0.23f);
        config.Set("ef_randomLifetime", false);
        config.Set("ef_minLifetime", 0.5f);
        config.Set("ef_maxLifetime", 2.0f);

        // Appearance
        config.Set("ef_lineThickness", 0.62f);
        config.Set("ef_randomThickness", false);
        config.Set("ef_minThickness", 0.8f);
        config.Set("ef_maxThickness", 2.5f);
        config.Set("ef_glowIntensity", 0.27f);
        config.Set("ef_randomGlow", false);
        config.Set("ef_minGlow", 0.5f);
        config.Set("ef_maxGlow", 1.5f);

        // Flicker
        config.Set("ef_flickerSpeed", 20f);
        config.Set("ef_flickerIntensity", 0.6f);
        config.Set("ef_randomFlicker", false);
        config.Set("ef_minFlickerSpeed", 10f);
        config.Set("ef_maxFlickerSpeed", 40f);

        // Crackle
        config.Set("ef_crackleIntensity", 0.19f);
        config.Set("ef_randomCrackle", true);
        config.Set("ef_minCrackle", 0.09f);
        config.Set("ef_maxCrackle", 0.71f);
        config.Set("ef_noiseScale", 1.0f);

        // Burst sparks
        config.Set("ef_burstProbability", 0.3f);
        config.Set("ef_burstIntensity", 0.8f);
        config.Set("ef_randomBurst", false);
        config.Set("ef_minBurstProb", 0.1f);
        config.Set("ef_maxBurstProb", 0.6f);

        // Colors
        config.Set("ef_primaryColor", new Vector4(0.4f, 0.6f, 1f, 1f));
        config.Set("ef_secondaryColor", new Vector4(0.8f, 0.9f, 1f, 1f));
        config.Set("ef_randomColorVariation", false);
        config.Set("ef_rainbowMode", false);
        config.Set("ef_rainbowSpeed", 0.3f);

        // Branch Bolts
        config.Set("ef_branchBoltEnabled", true);
        config.Set("ef_branchBoltCount", 3);
        config.Set("ef_randomBranchCount", false);
        config.Set("ef_minBranchCount", 1);
        config.Set("ef_maxBranchCount", 5);
        config.Set("ef_branchBoltLength", 25f);
        config.Set("ef_randomBranchLength", false);
        config.Set("ef_minBranchLength", 10f);
        config.Set("ef_maxBranchLength", 40f);
        config.Set("ef_branchBoltThickness", 1.0f);
        config.Set("ef_randomBranchThickness", false);
        config.Set("ef_minBranchThickness", 0.5f);
        config.Set("ef_maxBranchThickness", 2.0f);
        config.Set("ef_branchBoltSpread", 90f);
        config.Set("ef_branchBoltColor", new Vector4(0.6f, 0.8f, 1f, 0f)); // Alpha 0 = use segment color

        // Sparkles
        config.Set("ef_sparkleEnabled", true);
        config.Set("ef_sparkleCount", 5);
        config.Set("ef_randomSparkleCount", false);
        config.Set("ef_minSparkleCount", 2);
        config.Set("ef_maxSparkleCount", 8);
        config.Set("ef_sparkleSize", 3f);
        config.Set("ef_randomSparkleSize", false);
        config.Set("ef_minSparkleSize", 1f);
        config.Set("ef_maxSparkleSize", 5f);
        config.Set("ef_sparkleIntensity", 1.0f);
        config.Set("ef_randomSparkleIntensity", false);
        config.Set("ef_minSparkleIntensity", 0.5f);
        config.Set("ef_maxSparkleIntensity", 1.5f);
        config.Set("ef_sparkleColor", new Vector4(1f, 1f, 1f, 0f)); // Alpha 0 = use segment color

        return config;
    }

    public EffectConfigurationSchema GetConfigurationSchema()
    {
        return new EffectConfigurationSchema
        {
            Parameters =
            [
                // Effect Type Selection
                new ChoiceParameter
                {
                    Key = "selectedEffectType",
                    DisplayName = "Effect Type",
                    Description = "Select which effect to configure",
                    Choices = ["Lightning Bolt", "Electrical Follow"],
                    DefaultValue = "Lightning Bolt"
                },

                // Lightning Bolt Trigger Settings (lb_ prefix)
                new BoolParameter
                {
                    Key = "lb_mouseMoveEnabled",
                    DisplayName = "LB: Mouse Move",
                    Description = "Trigger Lightning Bolt on mouse movement",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "lb_leftClickEnabled",
                    DisplayName = "LB: Left Click",
                    Description = "Trigger Lightning Bolt on left mouse click",
                    DefaultValue = true
                },
                new BoolParameter
                {
                    Key = "lb_rightClickEnabled",
                    DisplayName = "LB: Right Click",
                    Description = "Trigger Lightning Bolt on right mouse click",
                    DefaultValue = false
                },

                // Electrical Follow Trigger Settings (ef_ prefix)
                new BoolParameter
                {
                    Key = "ef_mouseMoveEnabled",
                    DisplayName = "EF: Mouse Move",
                    Description = "Trigger Electrical Follow trail on mouse movement",
                    DefaultValue = false
                },

                // Legacy Trigger Settings (kept for schema compatibility)
                new ChoiceParameter
                {
                    Key = "ts_mouseMoveEffect",
                    DisplayName = "Mouse Move Effect (Legacy)",
                    Description = "Legacy: Effect triggered when moving the mouse",
                    Choices = ["None", "Lightning Bolt", "Electrical Follow"],
                    DefaultValue = "Lightning Bolt"
                },
                new ChoiceParameter
                {
                    Key = "ts_leftClickEffect",
                    DisplayName = "Left Click Effect (Legacy)",
                    Description = "Legacy: Effect triggered on left mouse button click",
                    Choices = ["None", "Lightning Bolt"],
                    DefaultValue = "Lightning Bolt"
                },
                new ChoiceParameter
                {
                    Key = "ts_rightClickEffect",
                    DisplayName = "Right Click Effect (Legacy)",
                    Description = "Legacy: Effect triggered on right mouse button click",
                    Choices = ["None", "Lightning Bolt"],
                    DefaultValue = "None"
                },

                // Move Trigger Settings
                new FloatParameter
                {
                    Key = "mt_distanceThreshold",
                    DisplayName = "Distance Threshold",
                    Description = "Distance in pixels the mouse must move to trigger the effect",
                    MinValue = 1f,
                    MaxValue = 150f,
                    DefaultValue = 50f,
                    Step = 1f
                },
                new BoolParameter
                {
                    Key = "mt_randomDistanceEnabled",
                    DisplayName = "Random Distance",
                    Description = "Recalculate distance threshold randomly every second",
                    DefaultValue = false
                },
                new ChoiceParameter
                {
                    Key = "mt_directionMode",
                    DisplayName = "Move Direction Mode",
                    Description = "Direction mode for bolts triggered by mouse movement",
                    Choices = ["All Directions (360)", "Configurable Spread", "Velocity-Based"],
                    DefaultValue = "All Directions (360)"
                },

                // Click Trigger Settings
                new ChoiceParameter
                {
                    Key = "ct_directionMode",
                    DisplayName = "Click Direction Mode",
                    Description = "Direction mode for bolts triggered by mouse clicks",
                    Choices = ["All Directions (360)", "Configurable Spread"],
                    DefaultValue = "All Directions (360)"
                },
                new FloatParameter
                {
                    Key = "ct_spreadAngle",
                    DisplayName = "Spread Angle",
                    Description = "Spread angle in degrees for configurable spread mode",
                    MinValue = 30f,
                    MaxValue = 360f,
                    DefaultValue = 180f,
                    Step = 10f
                },

                // Core Settings
                new BoolParameter
                {
                    Key = "core_enabled",
                    DisplayName = "Enable Core",
                    Description = "Show glowing core at cursor position",
                    DefaultValue = true
                },
                new FloatParameter
                {
                    Key = "core_radius",
                    DisplayName = "Core Radius",
                    Description = "Radius of the core glow in pixels",
                    MinValue = 5f,
                    MaxValue = 100f,
                    DefaultValue = 20f,
                    Step = 1f
                },
                new ColorParameter
                {
                    Key = "core_color",
                    DisplayName = "Core Color",
                    Description = "Color of the core glow",
                    DefaultValue = new Vector4(0.2f, 0.4f, 1f, 1f),
                    SupportsAlpha = false
                },

                // Bolt Count Settings
                new BoolParameter
                {
                    Key = "bc_randomCount",
                    DisplayName = "Random Bolt Count",
                    Description = "Use random number of bolts per trigger",
                    DefaultValue = true
                },
                new IntParameter
                {
                    Key = "bc_minCount",
                    DisplayName = "Min Bolt Count",
                    Description = "Minimum number of bolts when random is enabled",
                    MinValue = 1,
                    MaxValue = 10,
                    DefaultValue = 3
                },
                new IntParameter
                {
                    Key = "bc_maxCount",
                    DisplayName = "Max Bolt Count",
                    Description = "Maximum number of bolts when random is enabled",
                    MinValue = 1,
                    MaxValue = 15,
                    DefaultValue = 6
                },
                new IntParameter
                {
                    Key = "bc_fixedCount",
                    DisplayName = "Fixed Bolt Count",
                    Description = "Number of bolts when random is disabled",
                    MinValue = 1,
                    MaxValue = 15,
                    DefaultValue = 4
                },

                // Bolt Appearance Settings
                new FloatParameter
                {
                    Key = "ba_minLength",
                    DisplayName = "Min Bolt Length",
                    Description = "Minimum length of lightning bolts in pixels",
                    MinValue = 20f,
                    MaxValue = 300f,
                    DefaultValue = 50f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "ba_maxLength",
                    DisplayName = "Max Bolt Length",
                    Description = "Maximum length of lightning bolts in pixels",
                    MinValue = 50f,
                    MaxValue = 500f,
                    DefaultValue = 150f,
                    Step = 5f
                },
                new FloatParameter
                {
                    Key = "ba_thickness",
                    DisplayName = "Bolt Thickness",
                    Description = "Thickness of the lightning bolts",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ba_branchProbability",
                    DisplayName = "Branch Probability",
                    Description = "Probability of secondary branches on bolts (0-1)",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },

                // Color Settings
                new ColorParameter
                {
                    Key = "col_glow",
                    DisplayName = "Glow Color",
                    Description = "Color of the lightning bolt glow",
                    DefaultValue = new Vector4(0.3f, 0.5f, 1f, 1f),
                    SupportsAlpha = false
                },
                new BoolParameter
                {
                    Key = "col_randomVariation",
                    DisplayName = "Random Color Variation",
                    Description = "Add random color variation to bolts",
                    DefaultValue = false
                },
                new BoolParameter
                {
                    Key = "col_rainbowMode",
                    DisplayName = "Rainbow Mode",
                    Description = "Cycle through rainbow colors over time",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "col_rainbowSpeed",
                    DisplayName = "Rainbow Speed",
                    Description = "Speed of rainbow color cycling",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },

                // Timing Settings
                new FloatParameter
                {
                    Key = "time_boltLifetime",
                    DisplayName = "Bolt Lifetime",
                    Description = "How long each bolt exists (seconds)",
                    MinValue = 0.05f,
                    MaxValue = 1f,
                    DefaultValue = 0.15f,
                    Step = 0.01f
                },
                new FloatParameter
                {
                    Key = "time_flickerSpeed",
                    DisplayName = "Flicker Speed",
                    Description = "Speed of the flickering effect",
                    MinValue = 1f,
                    MaxValue = 50f,
                    DefaultValue = 15f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "time_fadeDuration",
                    DisplayName = "Fade Duration",
                    Description = "Duration of fade out effect (seconds)",
                    MinValue = 0.01f,
                    MaxValue = 0.5f,
                    DefaultValue = 0.05f,
                    Step = 0.01f
                },

                // Glow Settings
                new FloatParameter
                {
                    Key = "glow_intensity",
                    DisplayName = "Glow Intensity",
                    Description = "Overall intensity of the glow effect",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 0.8f,
                    Step = 0.1f
                },

                // ===== Electrical Follow Settings =====
                // General
                new IntParameter
                {
                    Key = "ef_maxPieces",
                    DisplayName = "Max Trail Pieces",
                    Description = "Maximum number of trail pieces",
                    MinValue = 50,
                    MaxValue = 1000,
                    DefaultValue = 512
                },
                new FloatParameter
                {
                    Key = "ef_pieceSize",
                    DisplayName = "Piece Size",
                    Description = "Distance between trail pieces in pixels",
                    MinValue = 2f,
                    MaxValue = 30f,
                    DefaultValue = 8f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "ef_lifetime",
                    DisplayName = "Trail Piece Lifetime",
                    Description = "How long each trail piece exists (seconds)",
                    MinValue = 0.1f,
                    MaxValue = 5f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "ef_randomLifetime",
                    DisplayName = "Random Lifetime",
                    Description = "Use random lifetime for each trail piece",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "ef_minLifetime",
                    DisplayName = "Min Lifetime",
                    Description = "Minimum lifetime when random is enabled",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ef_maxLifetime",
                    DisplayName = "Max Lifetime",
                    Description = "Maximum lifetime when random is enabled",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                },

                // Trail Appearance
                new FloatParameter
                {
                    Key = "ef_lineThickness",
                    DisplayName = "Trail Thickness",
                    Description = "Thickness of the electrical trail lines",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "ef_randomThickness",
                    DisplayName = "Random Thickness",
                    Description = "Use random thickness for trail",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "ef_minThickness",
                    DisplayName = "Min Thickness",
                    Description = "Minimum thickness when random is enabled",
                    MinValue = 0.3f,
                    MaxValue = 2f,
                    DefaultValue = 0.8f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ef_maxThickness",
                    DisplayName = "Max Thickness",
                    Description = "Maximum thickness when random is enabled",
                    MinValue = 1f,
                    MaxValue = 5f,
                    DefaultValue = 2.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ef_glowIntensity",
                    DisplayName = "Trail Glow Intensity",
                    Description = "Glow intensity for the electrical trail",
                    MinValue = 0f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "ef_randomGlow",
                    DisplayName = "Random Glow",
                    Description = "Use random glow intensity",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "ef_minGlow",
                    DisplayName = "Min Glow",
                    Description = "Minimum glow when random is enabled",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ef_maxGlow",
                    DisplayName = "Max Glow",
                    Description = "Maximum glow when random is enabled",
                    MinValue = 0.5f,
                    MaxValue = 3f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },

                // Trail Flicker
                new FloatParameter
                {
                    Key = "ef_flickerSpeed",
                    DisplayName = "Trail Flicker Speed",
                    Description = "Speed of the flickering effect",
                    MinValue = 1f,
                    MaxValue = 60f,
                    DefaultValue = 20f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "ef_flickerIntensity",
                    DisplayName = "Trail Flicker Intensity",
                    Description = "Intensity of the flicker effect",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.6f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "ef_randomFlicker",
                    DisplayName = "Random Flicker Speed",
                    Description = "Use random flicker speed",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "ef_minFlickerSpeed",
                    DisplayName = "Min Flicker Speed",
                    Description = "Minimum flicker speed when random is enabled",
                    MinValue = 1f,
                    MaxValue = 30f,
                    DefaultValue = 10f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "ef_maxFlickerSpeed",
                    DisplayName = "Max Flicker Speed",
                    Description = "Maximum flicker speed when random is enabled",
                    MinValue = 20f,
                    MaxValue = 60f,
                    DefaultValue = 40f,
                    Step = 1f
                },

                // Trail Crackle
                new FloatParameter
                {
                    Key = "ef_crackleIntensity",
                    DisplayName = "Crackle Intensity",
                    Description = "Intensity of the electrical crackling distortion",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 0.8f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "ef_randomCrackle",
                    DisplayName = "Random Crackle",
                    Description = "Use random crackle intensity",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "ef_minCrackle",
                    DisplayName = "Min Crackle",
                    Description = "Minimum crackle when random is enabled",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ef_maxCrackle",
                    DisplayName = "Max Crackle",
                    Description = "Maximum crackle when random is enabled",
                    MinValue = 0.5f,
                    MaxValue = 2f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ef_noiseScale",
                    DisplayName = "Noise Scale",
                    Description = "Scale of the noise pattern for crackling",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },

                // Trail Burst Sparks
                new FloatParameter
                {
                    Key = "ef_burstProbability",
                    DisplayName = "Burst Probability",
                    Description = "Probability of electrical burst sparks (0-1)",
                    MinValue = 0f,
                    MaxValue = 1f,
                    DefaultValue = 0.3f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ef_burstIntensity",
                    DisplayName = "Burst Intensity",
                    Description = "Intensity of burst sparks",
                    MinValue = 0f,
                    MaxValue = 2f,
                    DefaultValue = 0.8f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "ef_randomBurst",
                    DisplayName = "Random Burst",
                    Description = "Use random burst probability",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "ef_minBurstProb",
                    DisplayName = "Min Burst Probability",
                    Description = "Minimum burst probability when random is enabled",
                    MinValue = 0f,
                    MaxValue = 0.5f,
                    DefaultValue = 0.1f,
                    Step = 0.05f
                },
                new FloatParameter
                {
                    Key = "ef_maxBurstProb",
                    DisplayName = "Max Burst Probability",
                    Description = "Maximum burst probability when random is enabled",
                    MinValue = 0.3f,
                    MaxValue = 1f,
                    DefaultValue = 0.6f,
                    Step = 0.05f
                },

                // Trail Colors
                new ColorParameter
                {
                    Key = "ef_primaryColor",
                    DisplayName = "Trail Primary Color",
                    Description = "Primary color of the electrical trail",
                    DefaultValue = new Vector4(0.4f, 0.6f, 1f, 1f),
                    SupportsAlpha = false
                },
                new ColorParameter
                {
                    Key = "ef_secondaryColor",
                    DisplayName = "Trail Secondary Color",
                    Description = "Secondary color for variation in the trail",
                    DefaultValue = new Vector4(0.8f, 0.9f, 1f, 1f),
                    SupportsAlpha = false
                },
                new BoolParameter
                {
                    Key = "ef_randomColorVariation",
                    DisplayName = "Trail Random Color",
                    Description = "Add random color variation to the trail",
                    DefaultValue = false
                },
                new BoolParameter
                {
                    Key = "ef_rainbowMode",
                    DisplayName = "Trail Rainbow Mode",
                    Description = "Cycle through rainbow colors for the trail",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "ef_rainbowSpeed",
                    DisplayName = "Trail Rainbow Speed",
                    Description = "Speed of rainbow color cycling for the trail",
                    MinValue = 0.1f,
                    MaxValue = 2f,
                    DefaultValue = 0.3f,
                    Step = 0.1f
                },

                // ===== Branch Bolts =====
                new BoolParameter
                {
                    Key = "ef_branchBoltEnabled",
                    DisplayName = "Enable Branch Bolts",
                    Description = "Show lightning bolts branching from the trail",
                    DefaultValue = true
                },
                new IntParameter
                {
                    Key = "ef_branchBoltCount",
                    DisplayName = "Branch Bolt Count",
                    Description = "Number of branch bolts per segment",
                    MinValue = 1,
                    MaxValue = 8,
                    DefaultValue = 3
                },
                new BoolParameter
                {
                    Key = "ef_randomBranchCount",
                    DisplayName = "Random Branch Count",
                    Description = "Use random number of branch bolts",
                    DefaultValue = false
                },
                new IntParameter
                {
                    Key = "ef_minBranchCount",
                    DisplayName = "Min Branch Count",
                    Description = "Minimum branch bolts when random is enabled",
                    MinValue = 1,
                    MaxValue = 5,
                    DefaultValue = 1
                },
                new IntParameter
                {
                    Key = "ef_maxBranchCount",
                    DisplayName = "Max Branch Count",
                    Description = "Maximum branch bolts when random is enabled",
                    MinValue = 2,
                    MaxValue = 8,
                    DefaultValue = 5
                },
                new FloatParameter
                {
                    Key = "ef_branchBoltLength",
                    DisplayName = "Branch Bolt Length",
                    Description = "Length of branch bolts in pixels",
                    MinValue = 5f,
                    MaxValue = 80f,
                    DefaultValue = 25f,
                    Step = 1f
                },
                new BoolParameter
                {
                    Key = "ef_randomBranchLength",
                    DisplayName = "Random Branch Length",
                    Description = "Use random length for branch bolts",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "ef_minBranchLength",
                    DisplayName = "Min Branch Length",
                    Description = "Minimum branch length when random is enabled",
                    MinValue = 5f,
                    MaxValue = 40f,
                    DefaultValue = 10f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "ef_maxBranchLength",
                    DisplayName = "Max Branch Length",
                    Description = "Maximum branch length when random is enabled",
                    MinValue = 20f,
                    MaxValue = 80f,
                    DefaultValue = 40f,
                    Step = 1f
                },
                new FloatParameter
                {
                    Key = "ef_branchBoltThickness",
                    DisplayName = "Branch Bolt Thickness",
                    Description = "Thickness of branch bolts",
                    MinValue = 0.3f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "ef_randomBranchThickness",
                    DisplayName = "Random Branch Thickness",
                    Description = "Use random thickness for branch bolts",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "ef_minBranchThickness",
                    DisplayName = "Min Branch Thickness",
                    Description = "Minimum thickness when random is enabled",
                    MinValue = 0.3f,
                    MaxValue = 1.5f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ef_maxBranchThickness",
                    DisplayName = "Max Branch Thickness",
                    Description = "Maximum thickness when random is enabled",
                    MinValue = 1f,
                    MaxValue = 3f,
                    DefaultValue = 2.0f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ef_branchBoltSpread",
                    DisplayName = "Branch Bolt Spread",
                    Description = "Spread angle for branch bolts (degrees)",
                    MinValue = 30f,
                    MaxValue = 180f,
                    DefaultValue = 90f,
                    Step = 10f
                },
                new ColorParameter
                {
                    Key = "ef_branchBoltColor",
                    DisplayName = "Branch Bolt Color",
                    Description = "Color of branch bolts (alpha 0 = use trail color)",
                    DefaultValue = new Vector4(0.6f, 0.8f, 1f, 0f),
                    SupportsAlpha = true
                },

                // ===== Sparkles =====
                new BoolParameter
                {
                    Key = "ef_sparkleEnabled",
                    DisplayName = "Enable Sparkles",
                    Description = "Show sparkles along the trail",
                    DefaultValue = true
                },
                new IntParameter
                {
                    Key = "ef_sparkleCount",
                    DisplayName = "Sparkle Count",
                    Description = "Number of sparkles per segment",
                    MinValue = 1,
                    MaxValue = 12,
                    DefaultValue = 5
                },
                new BoolParameter
                {
                    Key = "ef_randomSparkleCount",
                    DisplayName = "Random Sparkle Count",
                    Description = "Use random number of sparkles",
                    DefaultValue = false
                },
                new IntParameter
                {
                    Key = "ef_minSparkleCount",
                    DisplayName = "Min Sparkle Count",
                    Description = "Minimum sparkles when random is enabled",
                    MinValue = 1,
                    MaxValue = 6,
                    DefaultValue = 2
                },
                new IntParameter
                {
                    Key = "ef_maxSparkleCount",
                    DisplayName = "Max Sparkle Count",
                    Description = "Maximum sparkles when random is enabled",
                    MinValue = 3,
                    MaxValue = 12,
                    DefaultValue = 8
                },
                new FloatParameter
                {
                    Key = "ef_sparkleSize",
                    DisplayName = "Sparkle Size",
                    Description = "Size of sparkle points in pixels",
                    MinValue = 1f,
                    MaxValue = 10f,
                    DefaultValue = 3f,
                    Step = 0.5f
                },
                new BoolParameter
                {
                    Key = "ef_randomSparkleSize",
                    DisplayName = "Random Sparkle Size",
                    Description = "Use random size for sparkles",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "ef_minSparkleSize",
                    DisplayName = "Min Sparkle Size",
                    Description = "Minimum sparkle size when random is enabled",
                    MinValue = 0.5f,
                    MaxValue = 5f,
                    DefaultValue = 1f,
                    Step = 0.5f
                },
                new FloatParameter
                {
                    Key = "ef_maxSparkleSize",
                    DisplayName = "Max Sparkle Size",
                    Description = "Maximum sparkle size when random is enabled",
                    MinValue = 2f,
                    MaxValue = 10f,
                    DefaultValue = 5f,
                    Step = 0.5f
                },
                new FloatParameter
                {
                    Key = "ef_sparkleIntensity",
                    DisplayName = "Sparkle Intensity",
                    Description = "Brightness intensity of sparkles",
                    MinValue = 0.1f,
                    MaxValue = 3f,
                    DefaultValue = 1.0f,
                    Step = 0.1f
                },
                new BoolParameter
                {
                    Key = "ef_randomSparkleIntensity",
                    DisplayName = "Random Sparkle Intensity",
                    Description = "Use random intensity for sparkles",
                    DefaultValue = false
                },
                new FloatParameter
                {
                    Key = "ef_minSparkleIntensity",
                    DisplayName = "Min Sparkle Intensity",
                    Description = "Minimum intensity when random is enabled",
                    MinValue = 0.1f,
                    MaxValue = 1.5f,
                    DefaultValue = 0.5f,
                    Step = 0.1f
                },
                new FloatParameter
                {
                    Key = "ef_maxSparkleIntensity",
                    DisplayName = "Max Sparkle Intensity",
                    Description = "Maximum intensity when random is enabled",
                    MinValue = 1f,
                    MaxValue = 3f,
                    DefaultValue = 1.5f,
                    Step = 0.1f
                },
                new ColorParameter
                {
                    Key = "ef_sparkleColor",
                    DisplayName = "Sparkle Color",
                    Description = "Color of sparkles (alpha 0 = use trail color)",
                    DefaultValue = new Vector4(1f, 1f, 1f, 0f),
                    SupportsAlpha = true
                }
            ]
        };
    }

    public object? CreateSettingsControl(IEffect effect) => new TeslaSettingsControl(effect);
}
