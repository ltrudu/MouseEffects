// Color Blindness NG Shader
// Next-generation CVD simulation and correction with per-zone support
//
// Each zone can independently be:
// - Original (no processing)
// - Simulation (CVD simulation using scientific matrices)
// - Correction (LUT-based color remapping)

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// ============================================================================
// Per-Zone Parameters Structure (80 bytes each = 20 floats, 16-byte aligned)
// ============================================================================

struct ZoneParams
{
    float Mode;                 // 0=Original, 1=Simulation, 2=Correction
    float SimulationFilterType; // Filter type for simulation
    float ApplicationMode;      // 0=Full, 1=Dominant, 2=Threshold
    float Threshold;            // Threshold for threshold mode

    float Intensity;            // Global intensity for zone
    float RedEnabled;           // Red channel enabled
    float RedStrength;          // Red channel strength
    float RedWhiteProtection;   // Red white protection

    float GreenEnabled;         // Green channel enabled
    float GreenStrength;        // Green channel strength
    float GreenWhiteProtection; // Green white protection
    float BlueEnabled;          // Blue channel enabled

    float BlueStrength;         // Blue channel strength
    float BlueWhiteProtection;  // Blue white protection
    float SimulationGuidedEnabled;    // 1.0 = use simulation to detect affected pixels
    float SimulationGuidedFilterType; // CVD type for detection (0=None, 1-6=Machado, 7-12=Strict)

    float SimulationGuidedSensitivity; // Sensitivity multiplier (0.5 = conservative, 5.0 = aggressive)
    float _padding1;
    float _padding2;
    float _padding3;
};

// ============================================================================
// Constant Buffer (48 + 80*4 = 368 bytes total)
// ============================================================================

cbuffer ColorBlindnessNGParams : register(b0)
{
    // Global parameters (48 bytes)
    float2 MousePosition;       // Mouse position in screen pixels
    float2 ViewportSize;        // Viewport size in pixels
    float SplitModeValue;       // 0=Full, 1=SplitV, 2=SplitH, 3=Quad, 4=Circle, 5=Rectangle
    float SplitPosition;        // Horizontal split (0-1)
    float SplitPositionV;       // Vertical split (0-1)
    float ComparisonMode;       // 0=off, 1=on (screen duplication mode)
    float Radius;               // Circle mode radius in pixels
    float RectWidth;            // Rectangle mode width in pixels
    float RectHeight;           // Rectangle mode height in pixels
    float EdgeSoftness;         // Edge blending (0=hard, 1=maximum soft)

    // Per-zone parameters (64 bytes each × 4 = 256 bytes)
    ZoneParams Zone0;
    ZoneParams Zone1;
    ZoneParams Zone2;
    ZoneParams Zone3;
};

// Textures and Samplers
Texture2D<float4> ScreenTexture : register(t0);

// Per-zone LUT textures (3 channels per zone × 4 zones = 12 textures)
Texture2D<float4> Zone0_RedLUT : register(t1);
Texture2D<float4> Zone0_GreenLUT : register(t2);
Texture2D<float4> Zone0_BlueLUT : register(t3);
Texture2D<float4> Zone1_RedLUT : register(t4);
Texture2D<float4> Zone1_GreenLUT : register(t5);
Texture2D<float4> Zone1_BlueLUT : register(t6);
Texture2D<float4> Zone2_RedLUT : register(t7);
Texture2D<float4> Zone2_GreenLUT : register(t8);
Texture2D<float4> Zone2_BlueLUT : register(t9);
Texture2D<float4> Zone3_RedLUT : register(t10);
Texture2D<float4> Zone3_GreenLUT : register(t11);
Texture2D<float4> Zone3_BlueLUT : register(t12);

SamplerState LinearSampler : register(s0);
SamplerState PointSampler : register(s1);

// Grayscale weights (luminance)
static const float3 GrayscaleWeights = float3(0.2126, 0.7152, 0.0722);

// ============================================================================
// Gamma Correction (sRGB <-> Linear)
// ============================================================================

float sRGBToLinear(float c)
{
    return c <= 0.04045 ? c / 12.92 : pow((c + 0.055) / 1.055, 2.4);
}

float linearToSRGB(float c)
{
    return c <= 0.0031308 ? c * 12.92 : 1.055 * pow(c, 1.0 / 2.4) - 0.055;
}

float3 sRGBToLinear3(float3 c)
{
    return float3(sRGBToLinear(c.r), sRGBToLinear(c.g), sRGBToLinear(c.b));
}

float3 linearToSRGB3(float3 c)
{
    return float3(linearToSRGB(c.r), linearToSRGB(c.g), linearToSRGB(c.b));
}

// ============================================================================
// LMS Color Space Conversion
// ============================================================================

float3 linearRGBToLMS(float3 rgb)
{
    float L = 0.31399022 * rgb.r + 0.63951294 * rgb.g + 0.04649755 * rgb.b;
    float M = 0.15537241 * rgb.r + 0.75789446 * rgb.g + 0.08670142 * rgb.b;
    float S = 0.01775239 * rgb.r + 0.10944209 * rgb.g + 0.87256922 * rgb.b;
    return float3(L, M, S);
}

float3 LMSToLinearRGB(float3 lms)
{
    float R =  5.47221206 * lms.x - 4.64196010 * lms.y + 0.16963708 * lms.z;
    float G = -1.12524190 * lms.x + 2.29317094 * lms.y - 0.16789520 * lms.z;
    float B =  0.02980165 * lms.x - 0.19318073 * lms.y + 1.16364789 * lms.z;
    return float3(R, G, B);
}

// ============================================================================
// Machado et al. (2009) CVD Simulation Matrices
// ============================================================================

static const float3x3 Machado_Protanopia = float3x3(
    0.567, 0.433, 0.000,
    0.558, 0.442, 0.000,
    0.000, 0.242, 0.758
);

static const float3x3 Machado_Protanomaly = float3x3(
    0.817, 0.183, 0.000,
    0.333, 0.667, 0.000,
    0.000, 0.125, 0.875
);

static const float3x3 Machado_Deuteranopia = float3x3(
    0.625, 0.375, 0.000,
    0.700, 0.300, 0.000,
    0.000, 0.300, 0.700
);

static const float3x3 Machado_Deuteranomaly = float3x3(
    0.800, 0.200, 0.000,
    0.258, 0.742, 0.000,
    0.000, 0.142, 0.858
);

static const float3x3 Machado_Tritanopia = float3x3(
    0.950, 0.050, 0.000,
    0.000, 0.433, 0.567,
    0.000, 0.475, 0.525
);

static const float3x3 Machado_Tritanomaly = float3x3(
    0.967, 0.033, 0.000,
    0.000, 0.733, 0.267,
    0.000, 0.183, 0.817
);

// ============================================================================
// Brettel/Vienot Strict LMS Simulation Matrices
// ============================================================================

static const float3x3 Strict_Protanopia_LMS = float3x3(
    0.0, 1.0, 0.0,
    0.0, 1.0, 0.0,
    0.0, 0.0, 1.0
);

static const float3x3 Strict_Deuteranopia_LMS = float3x3(
    1.0, 0.0, 0.0,
    1.0, 0.0, 0.0,
    0.0, 0.0, 1.0
);

static const float3x3 Strict_Tritanopia_LMS = float3x3(
    1.0,       0.0,      0.0,
    0.0,       1.0,      0.0,
    -0.395913, 0.801109, 0.0
);

// ============================================================================
// Simulation Functions
// ============================================================================

float3 SimulateMachado(float3 linearRGB, float cvdType)
{
    float3 result;

    if (cvdType < 1.5) // Protanopia (1)
    {
        result.r = dot(linearRGB, Machado_Protanopia[0]);
        result.g = dot(linearRGB, Machado_Protanopia[1]);
        result.b = dot(linearRGB, Machado_Protanopia[2]);
    }
    else if (cvdType < 2.5) // Protanomaly (2)
    {
        result.r = dot(linearRGB, Machado_Protanomaly[0]);
        result.g = dot(linearRGB, Machado_Protanomaly[1]);
        result.b = dot(linearRGB, Machado_Protanomaly[2]);
    }
    else if (cvdType < 3.5) // Deuteranopia (3)
    {
        result.r = dot(linearRGB, Machado_Deuteranopia[0]);
        result.g = dot(linearRGB, Machado_Deuteranopia[1]);
        result.b = dot(linearRGB, Machado_Deuteranopia[2]);
    }
    else if (cvdType < 4.5) // Deuteranomaly (4)
    {
        result.r = dot(linearRGB, Machado_Deuteranomaly[0]);
        result.g = dot(linearRGB, Machado_Deuteranomaly[1]);
        result.b = dot(linearRGB, Machado_Deuteranomaly[2]);
    }
    else if (cvdType < 5.5) // Tritanopia (5)
    {
        result.r = dot(linearRGB, Machado_Tritanopia[0]);
        result.g = dot(linearRGB, Machado_Tritanopia[1]);
        result.b = dot(linearRGB, Machado_Tritanopia[2]);
    }
    else // Tritanomaly (6)
    {
        result.r = dot(linearRGB, Machado_Tritanomaly[0]);
        result.g = dot(linearRGB, Machado_Tritanomaly[1]);
        result.b = dot(linearRGB, Machado_Tritanomaly[2]);
    }

    return result;
}

float3 SimulateStrict(float3 linearRGB, float cvdType)
{
    float3 lms = linearRGBToLMS(linearRGB);
    float3 simLMS;

    float strictType = cvdType - 6.0;

    if (strictType < 1.5) // Protanopia (7 -> 1)
    {
        simLMS.x = dot(lms, Strict_Protanopia_LMS[0]);
        simLMS.y = dot(lms, Strict_Protanopia_LMS[1]);
        simLMS.z = dot(lms, Strict_Protanopia_LMS[2]);
    }
    else if (strictType < 2.5) // Protanomaly (8 -> 2)
    {
        float3 fullSim;
        fullSim.x = dot(lms, Strict_Protanopia_LMS[0]);
        fullSim.y = dot(lms, Strict_Protanopia_LMS[1]);
        fullSim.z = dot(lms, Strict_Protanopia_LMS[2]);
        simLMS = lerp(lms, fullSim, 0.5);
    }
    else if (strictType < 3.5) // Deuteranopia (9 -> 3)
    {
        simLMS.x = dot(lms, Strict_Deuteranopia_LMS[0]);
        simLMS.y = dot(lms, Strict_Deuteranopia_LMS[1]);
        simLMS.z = dot(lms, Strict_Deuteranopia_LMS[2]);
    }
    else if (strictType < 4.5) // Deuteranomaly (10 -> 4)
    {
        float3 fullSim;
        fullSim.x = dot(lms, Strict_Deuteranopia_LMS[0]);
        fullSim.y = dot(lms, Strict_Deuteranopia_LMS[1]);
        fullSim.z = dot(lms, Strict_Deuteranopia_LMS[2]);
        simLMS = lerp(lms, fullSim, 0.5);
    }
    else if (strictType < 5.5) // Tritanopia (11 -> 5)
    {
        simLMS.x = dot(lms, Strict_Tritanopia_LMS[0]);
        simLMS.y = dot(lms, Strict_Tritanopia_LMS[1]);
        simLMS.z = dot(lms, Strict_Tritanopia_LMS[2]);
    }
    else // Tritanomaly (12 -> 6)
    {
        float3 fullSim;
        fullSim.x = dot(lms, Strict_Tritanopia_LMS[0]);
        fullSim.y = dot(lms, Strict_Tritanopia_LMS[1]);
        fullSim.z = dot(lms, Strict_Tritanopia_LMS[2]);
        simLMS = lerp(lms, fullSim, 0.5);
    }

    return LMSToLinearRGB(simLMS);
}

float3 ApplySimulation(float3 color, float cvdType)
{
    if (cvdType < 0.5) return color;

    // Achromatopsia (13)
    if (cvdType > 12.5 && cvdType < 13.5)
    {
        float gray = dot(color, GrayscaleWeights);
        return float3(gray, gray, gray);
    }

    // Achromatomaly (14)
    if (cvdType > 13.5 && cvdType < 14.5)
    {
        float gray = dot(color, GrayscaleWeights);
        return lerp(float3(gray, gray, gray), color, 0.3);
    }

    float3 linearRGB = sRGBToLinear3(color);
    float3 simLinearRGB;

    if (cvdType < 6.5)
    {
        simLinearRGB = SimulateMachado(linearRGB, cvdType);
    }
    else if (cvdType < 12.5)
    {
        simLinearRGB = SimulateStrict(linearRGB, cvdType);
    }
    else
    {
        return color;
    }

    simLinearRGB = clamp(simLinearRGB, 0.0, 1.0);
    return linearToSRGB3(simLinearRGB);
}

// ============================================================================
// Simulation-Guided Correction Detection
// ============================================================================
// Calculates the per-channel "error" between original and simulated colors.
// Returns a weight (0-1) for each channel indicating how much correction to apply.
// Only positive errors (lost colors) are considered for correction.

float3 GetSimulationError(float3 color, float cvdType)
{
    if (cvdType < 0.5) return float3(0.0, 0.0, 0.0);

    float3 linearRGB = sRGBToLinear3(color);
    float3 simLinearRGB;

    // Simulate based on CVD type
    if (cvdType < 6.5)
    {
        simLinearRGB = SimulateMachado(linearRGB, cvdType);
    }
    else if (cvdType < 12.5)
    {
        simLinearRGB = SimulateStrict(linearRGB, cvdType);
    }
    else if (cvdType < 13.5) // Achromatopsia
    {
        float gray = dot(linearRGB, GrayscaleWeights);
        simLinearRGB = float3(gray, gray, gray);
    }
    else if (cvdType < 14.5) // Achromatomaly
    {
        float gray = dot(linearRGB, GrayscaleWeights);
        simLinearRGB = lerp(float3(gray, gray, gray), linearRGB, 0.3);
    }
    else
    {
        return float3(0.0, 0.0, 0.0);
    }

    simLinearRGB = clamp(simLinearRGB, 0.0, 1.0);

    // Calculate error: positive values indicate color lost in simulation
    float3 error = linearRGB - simLinearRGB;

    // Return only positive errors (colors that were lost/reduced in simulation)
    // These are the colors that need correction
    return max(float3(0.0, 0.0, 0.0), error);
}

// Calculates a blend weight for simulation-guided correction.
// Returns a value 0-1 indicating how much of the LUT correction to apply.
// Higher error = more correction applied.
// sensitivity: multiplier for error detection (0.5 = conservative, 2.0 = default, 5.0 = aggressive)
float GetSimulationGuidedWeight(float3 color, float cvdType, float sensitivity)
{
    float3 error = GetSimulationError(color, cvdType);

    // Calculate the total error magnitude
    // Using max instead of sum to get the dominant error channel
    float errorMagnitude = max(max(error.r, error.g), error.b);

    // Scale error to 0-1 range with sensitivity adjustment
    // Higher sensitivity = more pixels detected as needing correction
    // Lower sensitivity = only strongly affected pixels get corrected
    float weight = saturate(errorMagnitude * sensitivity);

    // Apply smoothstep for a more natural transition
    return smoothstep(0.0, 1.0, weight);
}

// ============================================================================
// LUT Correction Functions
// ============================================================================

float GetWhiteness(float3 color)
{
    return min(min(color.r, color.g), color.b);
}

float GetWhiteProtectionFactor(float3 color, float threshold)
{
    if (threshold < 0.001) return 1.0;
    float whiteness = GetWhiteness(color);
    return saturate(1.0 - (whiteness - threshold) / 0.1);
}

// Apply LUT correction for a specific zone
float3 ApplyLUTCorrectionZone0(float3 color, ZoneParams zone)
{
    float3 result = color;

    // Calculate simulation-guided weight if enabled
    float simWeight = 1.0;
    if (zone.SimulationGuidedEnabled > 0.5)
    {
        simWeight = GetSimulationGuidedWeight(color, zone.SimulationGuidedFilterType, zone.SimulationGuidedSensitivity);
        // If no significant error detected, skip correction entirely
        if (simWeight < 0.001)
            return color;
    }

    bool applyRed = false;
    bool applyGreen = false;
    bool applyBlue = false;

    if (zone.ApplicationMode < 0.5) // Full Channel
    {
        applyRed = zone.RedEnabled > 0.5 && color.r > 0.001;
        applyGreen = zone.GreenEnabled > 0.5 && color.g > 0.001;
        applyBlue = zone.BlueEnabled > 0.5 && color.b > 0.001;
    }
    else if (zone.ApplicationMode < 1.5) // Dominant Only
    {
        applyRed = zone.RedEnabled > 0.5 && color.r > color.g && color.r > color.b;
        applyGreen = zone.GreenEnabled > 0.5 && color.g > color.r && color.g > color.b;
        applyBlue = zone.BlueEnabled > 0.5 && color.b > color.r && color.b > color.g;
    }
    else // Threshold
    {
        applyRed = zone.RedEnabled > 0.5 && color.r > zone.Threshold;
        applyGreen = zone.GreenEnabled > 0.5 && color.g > zone.Threshold;
        applyBlue = zone.BlueEnabled > 0.5 && color.b > zone.Threshold;
    }

    if (applyRed)
    {
        float3 lutColor = Zone0_RedLUT.Sample(PointSampler, float2(color.r, 0.5)).rgb;
        float effectiveStrength = zone.RedStrength * GetWhiteProtectionFactor(color, zone.RedWhiteProtection);
        // Apply simulation-guided weight to modulate correction strength
        effectiveStrength *= simWeight;
        result = lerp(result, lerp(result, lutColor, color.r), effectiveStrength);
    }

    if (applyGreen)
    {
        float3 lutColor = Zone0_GreenLUT.Sample(PointSampler, float2(color.g, 0.5)).rgb;
        float effectiveStrength = zone.GreenStrength * GetWhiteProtectionFactor(color, zone.GreenWhiteProtection);
        effectiveStrength *= simWeight;
        result = lerp(result, lerp(result, lutColor, color.g), effectiveStrength);
    }

    if (applyBlue)
    {
        float3 lutColor = Zone0_BlueLUT.Sample(PointSampler, float2(color.b, 0.5)).rgb;
        float effectiveStrength = zone.BlueStrength * GetWhiteProtectionFactor(color, zone.BlueWhiteProtection);
        effectiveStrength *= simWeight;
        result = lerp(result, lerp(result, lutColor, color.b), effectiveStrength);
    }

    return result;
}

float3 ApplyLUTCorrectionZone1(float3 color, ZoneParams zone)
{
    float3 result = color;

    // Calculate simulation-guided weight if enabled
    float simWeight = 1.0;
    if (zone.SimulationGuidedEnabled > 0.5)
    {
        simWeight = GetSimulationGuidedWeight(color, zone.SimulationGuidedFilterType, zone.SimulationGuidedSensitivity);
        if (simWeight < 0.001)
            return color;
    }

    bool applyRed = false;
    bool applyGreen = false;
    bool applyBlue = false;

    if (zone.ApplicationMode < 0.5)
    {
        applyRed = zone.RedEnabled > 0.5 && color.r > 0.001;
        applyGreen = zone.GreenEnabled > 0.5 && color.g > 0.001;
        applyBlue = zone.BlueEnabled > 0.5 && color.b > 0.001;
    }
    else if (zone.ApplicationMode < 1.5)
    {
        applyRed = zone.RedEnabled > 0.5 && color.r > color.g && color.r > color.b;
        applyGreen = zone.GreenEnabled > 0.5 && color.g > color.r && color.g > color.b;
        applyBlue = zone.BlueEnabled > 0.5 && color.b > color.r && color.b > color.g;
    }
    else
    {
        applyRed = zone.RedEnabled > 0.5 && color.r > zone.Threshold;
        applyGreen = zone.GreenEnabled > 0.5 && color.g > zone.Threshold;
        applyBlue = zone.BlueEnabled > 0.5 && color.b > zone.Threshold;
    }

    if (applyRed)
    {
        float3 lutColor = Zone1_RedLUT.Sample(PointSampler, float2(color.r, 0.5)).rgb;
        float effectiveStrength = zone.RedStrength * GetWhiteProtectionFactor(color, zone.RedWhiteProtection);
        effectiveStrength *= simWeight;
        result = lerp(result, lerp(result, lutColor, color.r), effectiveStrength);
    }

    if (applyGreen)
    {
        float3 lutColor = Zone1_GreenLUT.Sample(PointSampler, float2(color.g, 0.5)).rgb;
        float effectiveStrength = zone.GreenStrength * GetWhiteProtectionFactor(color, zone.GreenWhiteProtection);
        effectiveStrength *= simWeight;
        result = lerp(result, lerp(result, lutColor, color.g), effectiveStrength);
    }

    if (applyBlue)
    {
        float3 lutColor = Zone1_BlueLUT.Sample(PointSampler, float2(color.b, 0.5)).rgb;
        float effectiveStrength = zone.BlueStrength * GetWhiteProtectionFactor(color, zone.BlueWhiteProtection);
        effectiveStrength *= simWeight;
        result = lerp(result, lerp(result, lutColor, color.b), effectiveStrength);
    }

    return result;
}

float3 ApplyLUTCorrectionZone2(float3 color, ZoneParams zone)
{
    float3 result = color;

    // Calculate simulation-guided weight if enabled
    float simWeight = 1.0;
    if (zone.SimulationGuidedEnabled > 0.5)
    {
        simWeight = GetSimulationGuidedWeight(color, zone.SimulationGuidedFilterType, zone.SimulationGuidedSensitivity);
        if (simWeight < 0.001)
            return color;
    }

    bool applyRed = false;
    bool applyGreen = false;
    bool applyBlue = false;

    if (zone.ApplicationMode < 0.5)
    {
        applyRed = zone.RedEnabled > 0.5 && color.r > 0.001;
        applyGreen = zone.GreenEnabled > 0.5 && color.g > 0.001;
        applyBlue = zone.BlueEnabled > 0.5 && color.b > 0.001;
    }
    else if (zone.ApplicationMode < 1.5)
    {
        applyRed = zone.RedEnabled > 0.5 && color.r > color.g && color.r > color.b;
        applyGreen = zone.GreenEnabled > 0.5 && color.g > color.r && color.g > color.b;
        applyBlue = zone.BlueEnabled > 0.5 && color.b > color.r && color.b > color.g;
    }
    else
    {
        applyRed = zone.RedEnabled > 0.5 && color.r > zone.Threshold;
        applyGreen = zone.GreenEnabled > 0.5 && color.g > zone.Threshold;
        applyBlue = zone.BlueEnabled > 0.5 && color.b > zone.Threshold;
    }

    if (applyRed)
    {
        float3 lutColor = Zone2_RedLUT.Sample(PointSampler, float2(color.r, 0.5)).rgb;
        float effectiveStrength = zone.RedStrength * GetWhiteProtectionFactor(color, zone.RedWhiteProtection);
        effectiveStrength *= simWeight;
        result = lerp(result, lerp(result, lutColor, color.r), effectiveStrength);
    }

    if (applyGreen)
    {
        float3 lutColor = Zone2_GreenLUT.Sample(PointSampler, float2(color.g, 0.5)).rgb;
        float effectiveStrength = zone.GreenStrength * GetWhiteProtectionFactor(color, zone.GreenWhiteProtection);
        effectiveStrength *= simWeight;
        result = lerp(result, lerp(result, lutColor, color.g), effectiveStrength);
    }

    if (applyBlue)
    {
        float3 lutColor = Zone2_BlueLUT.Sample(PointSampler, float2(color.b, 0.5)).rgb;
        float effectiveStrength = zone.BlueStrength * GetWhiteProtectionFactor(color, zone.BlueWhiteProtection);
        effectiveStrength *= simWeight;
        result = lerp(result, lerp(result, lutColor, color.b), effectiveStrength);
    }

    return result;
}

float3 ApplyLUTCorrectionZone3(float3 color, ZoneParams zone)
{
    float3 result = color;

    // Calculate simulation-guided weight if enabled
    float simWeight = 1.0;
    if (zone.SimulationGuidedEnabled > 0.5)
    {
        simWeight = GetSimulationGuidedWeight(color, zone.SimulationGuidedFilterType, zone.SimulationGuidedSensitivity);
        if (simWeight < 0.001)
            return color;
    }

    bool applyRed = false;
    bool applyGreen = false;
    bool applyBlue = false;

    if (zone.ApplicationMode < 0.5)
    {
        applyRed = zone.RedEnabled > 0.5 && color.r > 0.001;
        applyGreen = zone.GreenEnabled > 0.5 && color.g > 0.001;
        applyBlue = zone.BlueEnabled > 0.5 && color.b > 0.001;
    }
    else if (zone.ApplicationMode < 1.5)
    {
        applyRed = zone.RedEnabled > 0.5 && color.r > color.g && color.r > color.b;
        applyGreen = zone.GreenEnabled > 0.5 && color.g > color.r && color.g > color.b;
        applyBlue = zone.BlueEnabled > 0.5 && color.b > color.r && color.b > color.g;
    }
    else
    {
        applyRed = zone.RedEnabled > 0.5 && color.r > zone.Threshold;
        applyGreen = zone.GreenEnabled > 0.5 && color.g > zone.Threshold;
        applyBlue = zone.BlueEnabled > 0.5 && color.b > zone.Threshold;
    }

    if (applyRed)
    {
        float3 lutColor = Zone3_RedLUT.Sample(PointSampler, float2(color.r, 0.5)).rgb;
        float effectiveStrength = zone.RedStrength * GetWhiteProtectionFactor(color, zone.RedWhiteProtection);
        effectiveStrength *= simWeight;
        result = lerp(result, lerp(result, lutColor, color.r), effectiveStrength);
    }

    if (applyGreen)
    {
        float3 lutColor = Zone3_GreenLUT.Sample(PointSampler, float2(color.g, 0.5)).rgb;
        float effectiveStrength = zone.GreenStrength * GetWhiteProtectionFactor(color, zone.GreenWhiteProtection);
        effectiveStrength *= simWeight;
        result = lerp(result, lerp(result, lutColor, color.g), effectiveStrength);
    }

    if (applyBlue)
    {
        float3 lutColor = Zone3_BlueLUT.Sample(PointSampler, float2(color.b, 0.5)).rgb;
        float effectiveStrength = zone.BlueStrength * GetWhiteProtectionFactor(color, zone.BlueWhiteProtection);
        effectiveStrength *= simWeight;
        result = lerp(result, lerp(result, lutColor, color.b), effectiveStrength);
    }

    return result;
}

// ============================================================================
// Zone Processing
// ============================================================================

float3 ProcessZone(float3 color, ZoneParams zone, int zoneIndex)
{
    // Mode 0 = Original (no processing)
    if (zone.Mode < 0.5)
    {
        return color;
    }

    float3 processedColor;

    // Mode 1 = Simulation
    if (zone.Mode < 1.5)
    {
        processedColor = ApplySimulation(color, zone.SimulationFilterType);
    }
    // Mode 2 = Correction
    else
    {
        // Call zone-specific LUT function
        if (zoneIndex == 0)
            processedColor = ApplyLUTCorrectionZone0(color, zone);
        else if (zoneIndex == 1)
            processedColor = ApplyLUTCorrectionZone1(color, zone);
        else if (zoneIndex == 2)
            processedColor = ApplyLUTCorrectionZone2(color, zone);
        else
            processedColor = ApplyLUTCorrectionZone3(color, zone);
    }

    // Apply intensity blend
    return lerp(color, processedColor, zone.Intensity);
}

// ============================================================================
// Vertex Shader
// ============================================================================

PSInput VSMain(uint vertexId : SV_VertexID)
{
    PSInput output;

    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y;
    output.TexCoord = uv;

    return output;
}

// ============================================================================
// Zone Detection
// ============================================================================

// Returns zone info with blend factor for smooth transitions between zones
// zoneIndex: primary zone (0-3)
// blendFactor: 0.0 = fully in primary zone, 1.0 = fully in secondary zone
// nextZoneIndex: secondary zone to blend with (only used when blendFactor > 0)
void GetZoneInfo(float2 screenPos, out int zoneIndex, out float blendFactor, out int nextZoneIndex)
{
    blendFactor = 0.0;
    nextZoneIndex = 0;

    // Fullscreen - always zone 0
    if (SplitModeValue < 0.5)
    {
        zoneIndex = 0;
        return;
    }

    float splitX = ViewportSize.x * SplitPosition;
    float splitY = ViewportSize.y * SplitPositionV;

    // Split Vertical (1) - left/right
    if (SplitModeValue < 1.5)
    {
        zoneIndex = screenPos.x < splitX ? 0 : 1;
        return;
    }

    // Split Horizontal (2) - top/bottom
    if (SplitModeValue < 2.5)
    {
        zoneIndex = screenPos.y < splitY ? 0 : 1;
        return;
    }

    // Quadrants (3) - 4 zones
    if (SplitModeValue < 3.5)
    {
        bool left = screenPos.x < splitX;
        bool top = screenPos.y < splitY;

        if (top && left) zoneIndex = 0;
        else if (top && !left) zoneIndex = 1;
        else if (!top && left) zoneIndex = 2;
        else zoneIndex = 3;
        return;
    }

    // Circle mode (4) - Zone 0 = inside circle, Zone 1 = outside
    if (SplitModeValue < 4.5)
    {
        float dist = length(screenPos - MousePosition);
        float normalizedDist = dist / max(Radius, 1.0);
        float edgeWidth = EdgeSoftness * 0.5;

        if (normalizedDist < 1.0 - edgeWidth)
        {
            // Fully inside the circle
            zoneIndex = 0;
        }
        else if (normalizedDist > 1.0 + edgeWidth)
        {
            // Fully outside the circle
            zoneIndex = 1;
        }
        else
        {
            // In the transition zone - blend between inner and outer
            zoneIndex = 0;
            nextZoneIndex = 1;
            blendFactor = smoothstep(1.0 - edgeWidth, 1.0 + edgeWidth, normalizedDist);
        }
        return;
    }

    // Rectangle mode (5) - Zone 0 = inside rectangle, Zone 1 = outside
    {
        float2 toMouse = abs(screenPos - MousePosition);
        float2 halfSize = float2(RectWidth, RectHeight) * 0.5;
        float2 edgeSize = halfSize * EdgeSoftness * 0.5;

        // Check if we're fully inside (no edge transition)
        bool fullyInside = toMouse.x < halfSize.x - edgeSize.x &&
                           toMouse.y < halfSize.y - edgeSize.y;
        // Check if we're fully outside (no edge transition)
        bool fullyOutside = toMouse.x > halfSize.x + edgeSize.x ||
                            toMouse.y > halfSize.y + edgeSize.y;

        if (fullyInside)
        {
            zoneIndex = 0;
        }
        else if (fullyOutside)
        {
            zoneIndex = 1;
        }
        else
        {
            // In the transition zone - calculate blend factor based on edge distance
            float2 edgeDist = (toMouse - halfSize + edgeSize) / (2.0 * edgeSize + 0.001);
            edgeDist = saturate(edgeDist);
            float dist = max(edgeDist.x, edgeDist.y);

            zoneIndex = 0;
            nextZoneIndex = 1;
            blendFactor = smoothstep(0.0, 1.0, dist);
        }
    }
}

// Legacy function for compatibility (no blending)
int GetZoneIndex(float2 screenPos)
{
    int zoneIndex;
    float blendFactor;
    int nextZoneIndex;
    GetZoneInfo(screenPos, zoneIndex, blendFactor, nextZoneIndex);
    return blendFactor > 0.5 ? nextZoneIndex : zoneIndex;
}

// Separator line
float GetSeparatorAlpha(float2 screenPos)
{
    if (SplitModeValue < 0.5) return 0.0;

    float lineWidth = 2.0;
    float splitX = ViewportSize.x * SplitPosition;
    float splitY = ViewportSize.y * SplitPositionV;

    if (SplitModeValue > 0.5 && SplitModeValue < 1.5)
    {
        if (abs(screenPos.x - splitX) < lineWidth)
            return 0.8;
    }
    else if (SplitModeValue > 1.5 && SplitModeValue < 2.5)
    {
        if (abs(screenPos.y - splitY) < lineWidth)
            return 0.8;
    }
    else if (SplitModeValue > 2.5)
    {
        if (abs(screenPos.x - splitX) < lineWidth || abs(screenPos.y - splitY) < lineWidth)
            return 0.8;
    }

    return 0.0;
}

// ============================================================================
// Comparison Mode - Screen Duplication
// ============================================================================

// Get zone bounds in screen coordinates
void GetZoneBounds(int zoneIndex, out float2 zoneMin, out float2 zoneMax)
{
    float splitX = ViewportSize.x * SplitPosition;
    float splitY = ViewportSize.y * SplitPositionV;

    if (SplitModeValue > 2.5) // Quadrants (3)
    {
        if (zoneIndex == 0) // Top-Left
        {
            zoneMin = float2(0, 0);
            zoneMax = float2(splitX, splitY);
        }
        else if (zoneIndex == 1) // Top-Right
        {
            zoneMin = float2(splitX, 0);
            zoneMax = float2(ViewportSize.x, splitY);
        }
        else if (zoneIndex == 2) // Bottom-Left
        {
            zoneMin = float2(0, splitY);
            zoneMax = float2(splitX, ViewportSize.y);
        }
        else // Bottom-Right (3)
        {
            zoneMin = float2(splitX, splitY);
            zoneMax = ViewportSize;
        }
    }
    else if (SplitModeValue > 1.5) // Split Horizontal (2)
    {
        if (zoneIndex == 0) // Top
        {
            zoneMin = float2(0, 0);
            zoneMax = float2(ViewportSize.x, splitY);
        }
        else // Bottom
        {
            zoneMin = float2(0, splitY);
            zoneMax = ViewportSize;
        }
    }
    else // Split Vertical (1)
    {
        if (zoneIndex == 0) // Left
        {
            zoneMin = float2(0, 0);
            zoneMax = float2(splitX, ViewportSize.y);
        }
        else // Right
        {
            zoneMin = float2(splitX, 0);
            zoneMax = ViewportSize;
        }
    }
}

// Get UV coordinates for comparison mode with aspect ratio preservation
// Returns (-1, -1) if pixel is in letterbox/pillarbox area (black bars)
float2 GetComparisonUV(float2 screenPos, int zoneIndex)
{
    float2 zoneMin, zoneMax;
    GetZoneBounds(zoneIndex, zoneMin, zoneMax);
    float2 zoneSize = zoneMax - zoneMin;
    float2 zoneCenter = (zoneMin + zoneMax) * 0.5;

    if (SplitModeValue > 2.5) // Quadrants - stretch to fit
    {
        return (screenPos - zoneMin) / zoneSize;
    }

    // Split modes - preserve aspect ratio with letterboxing/pillarboxing
    float screenAspect = ViewportSize.x / ViewportSize.y;
    float zoneAspect = zoneSize.x / zoneSize.y;

    float2 scaledSize;
    if (screenAspect > zoneAspect)
    {
        // Screen is wider than zone - fit to zone width, letterbox
        scaledSize.x = zoneSize.x;
        scaledSize.y = zoneSize.x / screenAspect;
    }
    else
    {
        // Screen is taller than zone - fit to zone height, pillarbox
        scaledSize.y = zoneSize.y;
        scaledSize.x = zoneSize.y * screenAspect;
    }

    float2 contentMin = zoneCenter - scaledSize * 0.5;
    float2 contentMax = zoneCenter + scaledSize * 0.5;

    // Check if pixel is outside content area (in black bars)
    if (screenPos.x < contentMin.x || screenPos.x > contentMax.x ||
        screenPos.y < contentMin.y || screenPos.y > contentMax.y)
    {
        return float2(-1, -1); // Signal to render black
    }

    return (screenPos - contentMin) / scaledSize;
}

// Get the transformed mouse position within a zone for comparison mode
float2 GetTransformedMousePos(int zoneIndex)
{
    float2 zoneMin, zoneMax;
    GetZoneBounds(zoneIndex, zoneMin, zoneMax);
    float2 zoneSize = zoneMax - zoneMin;
    float2 zoneCenter = (zoneMin + zoneMax) * 0.5;

    // Normalize mouse position to 0-1
    float2 normalizedMouse = MousePosition / ViewportSize;

    if (SplitModeValue > 2.5) // Quadrants - stretch to fit
    {
        return zoneMin + normalizedMouse * zoneSize;
    }

    // Split modes - preserve aspect ratio
    float screenAspect = ViewportSize.x / ViewportSize.y;
    float zoneAspect = zoneSize.x / zoneSize.y;

    float2 scaledSize;
    if (screenAspect > zoneAspect)
    {
        scaledSize.x = zoneSize.x;
        scaledSize.y = zoneSize.x / screenAspect;
    }
    else
    {
        scaledSize.y = zoneSize.y;
        scaledSize.x = zoneSize.y * screenAspect;
    }

    float2 contentMin = zoneCenter - scaledSize * 0.5;
    return contentMin + normalizedMouse * scaledSize;
}

// Draw a crosshair cursor indicator
float DrawCursor(float2 screenPos, float2 cursorPos, float size)
{
    float2 delta = screenPos - cursorPos;
    float dist = length(delta);

    // Crosshair parameters
    float lineWidth = 2.0;
    float innerRadius = 4.0;
    float outerRadius = size;

    // Horizontal line
    bool onHLine = abs(delta.y) < lineWidth && abs(delta.x) > innerRadius && abs(delta.x) < outerRadius;
    // Vertical line
    bool onVLine = abs(delta.x) < lineWidth && abs(delta.y) > innerRadius && abs(delta.y) < outerRadius;
    // Center dot
    bool onCenter = dist < 3.0;
    // Outer circle (thin)
    bool onCircle = abs(dist - outerRadius) < 1.5;

    if (onHLine || onVLine || onCenter || onCircle)
    {
        return 1.0;
    }

    return 0.0;
}

// ============================================================================
// Pixel Shader
// ============================================================================

// Helper function to get zone parameters by index
ZoneParams GetZoneParams(int zoneIndex)
{
    if (zoneIndex == 0)
        return Zone0;
    else if (zoneIndex == 1)
        return Zone1;
    else if (zoneIndex == 2)
        return Zone2;
    else
        return Zone3;
}

float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;
    float2 screenPos = uv * ViewportSize;

    // Get zone info with blend factor for smooth transitions
    int zoneIndex;
    float blendFactor;
    int nextZoneIndex;
    GetZoneInfo(screenPos, zoneIndex, blendFactor, nextZoneIndex);

    // Handle comparison mode - full screen duplication in each zone
    // Note: Comparison mode is disabled for shape modes (Circle/Rectangle)
    float2 sampleUV = uv;
    bool isBlackBar = false;
    bool isShapeMode = SplitModeValue > 3.5;

    if (ComparisonMode > 0.5 && SplitModeValue > 0.5 && !isShapeMode)
    {
        // In comparison mode, duplicate the full screen into each zone
        float2 comparisonUV = GetComparisonUV(screenPos, zoneIndex);
        if (comparisonUV.x < 0)
        {
            isBlackBar = true;
        }
        else
        {
            sampleUV = comparisonUV;
        }
    }

    // Return black for letterbox/pillarbox areas
    if (isBlackBar)
    {
        return float4(0.0, 0.0, 0.0, 1.0);
    }

    // Sample original screen color at the (possibly remapped) UV
    float4 screenColor = ScreenTexture.Sample(LinearSampler, sampleUV);
    float3 color = screenColor.rgb;

    float3 finalColor;

    // Get zone parameters and process primary zone
    ZoneParams zone = GetZoneParams(zoneIndex);
    float3 primaryColor = ProcessZone(color, zone, zoneIndex);

    // If we have a blend factor, also process the secondary zone and blend
    if (blendFactor > 0.001)
    {
        ZoneParams nextZone = GetZoneParams(nextZoneIndex);
        float3 secondaryColor = ProcessZone(color, nextZone, nextZoneIndex);
        finalColor = lerp(primaryColor, secondaryColor, blendFactor);
    }
    else
    {
        finalColor = primaryColor;
    }

    // Draw virtual cursor in comparison mode (not for shape modes)
    if (ComparisonMode > 0.5 && SplitModeValue > 0.5 && !isShapeMode)
    {
        float2 transformedMouse = GetTransformedMousePos(zoneIndex);
        float cursorSize = 15.0;

        // Scale cursor size based on zone size (smaller for quadrants)
        if (SplitModeValue > 2.5 && SplitModeValue < 3.5) // Quadrants only
        {
            cursorSize = 12.0;
        }

        float cursorAlpha = DrawCursor(screenPos, transformedMouse, cursorSize);

        if (cursorAlpha > 0.5)
        {
            // Draw cursor with contrasting color (inverted luminance)
            float lum = dot(finalColor, float3(0.299, 0.587, 0.114));
            float3 cursorColor = lum > 0.5 ? float3(0.0, 0.0, 0.0) : float3(1.0, 1.0, 1.0);

            // Add colored accent for better visibility
            float2 delta = screenPos - transformedMouse;
            float dist = length(delta);

            // Center dot and outer ring - use accent color (cyan/magenta based on zone)
            if (dist < 3.0 || abs(dist - cursorSize) < 1.5)
            {
                float3 accentColor = (zoneIndex % 2 == 0) ? float3(0.0, 1.0, 1.0) : float3(1.0, 0.0, 1.0);
                finalColor = lerp(finalColor, accentColor, 0.9);
            }
            else
            {
                // Crosshair lines - use contrasting color
                finalColor = lerp(finalColor, cursorColor, 0.95);
            }
        }
    }

    // Draw separator line (not for shape modes - they have soft edges instead)
    if (!isShapeMode)
    {
        float separatorAlpha = GetSeparatorAlpha(screenPos);
        if (separatorAlpha > 0.0)
        {
            finalColor = lerp(finalColor, float3(0.2, 0.2, 0.2), separatorAlpha);
        }
    }

    return float4(finalColor, 1.0);
}
