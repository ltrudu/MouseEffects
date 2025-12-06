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
// Per-Zone Parameters Structure (64 bytes each)
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
    float _padding1;
    float _padding2;
};

// ============================================================================
// Constant Buffer (288 bytes total)
// ============================================================================

cbuffer ColorBlindnessNGParams : register(b0)
{
    // Global parameters (32 bytes)
    float2 ViewportSize;
    float SplitModeValue;       // 0=Full, 1=SplitV, 2=SplitH, 3=Quad
    float SplitPosition;        // Horizontal split (0-1)

    float SplitPositionV;       // Vertical split (0-1)
    float ComparisonMode;       // 0=off, 1=on (zone 0 shows original)
    float _globalPad1;
    float _globalPad2;

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
        result = lerp(result, lerp(result, lutColor, color.r), effectiveStrength);
    }

    if (applyGreen)
    {
        float3 lutColor = Zone0_GreenLUT.Sample(PointSampler, float2(color.g, 0.5)).rgb;
        float effectiveStrength = zone.GreenStrength * GetWhiteProtectionFactor(color, zone.GreenWhiteProtection);
        result = lerp(result, lerp(result, lutColor, color.g), effectiveStrength);
    }

    if (applyBlue)
    {
        float3 lutColor = Zone0_BlueLUT.Sample(PointSampler, float2(color.b, 0.5)).rgb;
        float effectiveStrength = zone.BlueStrength * GetWhiteProtectionFactor(color, zone.BlueWhiteProtection);
        result = lerp(result, lerp(result, lutColor, color.b), effectiveStrength);
    }

    return result;
}

float3 ApplyLUTCorrectionZone1(float3 color, ZoneParams zone)
{
    float3 result = color;

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
        result = lerp(result, lerp(result, lutColor, color.r), effectiveStrength);
    }

    if (applyGreen)
    {
        float3 lutColor = Zone1_GreenLUT.Sample(PointSampler, float2(color.g, 0.5)).rgb;
        float effectiveStrength = zone.GreenStrength * GetWhiteProtectionFactor(color, zone.GreenWhiteProtection);
        result = lerp(result, lerp(result, lutColor, color.g), effectiveStrength);
    }

    if (applyBlue)
    {
        float3 lutColor = Zone1_BlueLUT.Sample(PointSampler, float2(color.b, 0.5)).rgb;
        float effectiveStrength = zone.BlueStrength * GetWhiteProtectionFactor(color, zone.BlueWhiteProtection);
        result = lerp(result, lerp(result, lutColor, color.b), effectiveStrength);
    }

    return result;
}

float3 ApplyLUTCorrectionZone2(float3 color, ZoneParams zone)
{
    float3 result = color;

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
        result = lerp(result, lerp(result, lutColor, color.r), effectiveStrength);
    }

    if (applyGreen)
    {
        float3 lutColor = Zone2_GreenLUT.Sample(PointSampler, float2(color.g, 0.5)).rgb;
        float effectiveStrength = zone.GreenStrength * GetWhiteProtectionFactor(color, zone.GreenWhiteProtection);
        result = lerp(result, lerp(result, lutColor, color.g), effectiveStrength);
    }

    if (applyBlue)
    {
        float3 lutColor = Zone2_BlueLUT.Sample(PointSampler, float2(color.b, 0.5)).rgb;
        float effectiveStrength = zone.BlueStrength * GetWhiteProtectionFactor(color, zone.BlueWhiteProtection);
        result = lerp(result, lerp(result, lutColor, color.b), effectiveStrength);
    }

    return result;
}

float3 ApplyLUTCorrectionZone3(float3 color, ZoneParams zone)
{
    float3 result = color;

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
        result = lerp(result, lerp(result, lutColor, color.r), effectiveStrength);
    }

    if (applyGreen)
    {
        float3 lutColor = Zone3_GreenLUT.Sample(PointSampler, float2(color.g, 0.5)).rgb;
        float effectiveStrength = zone.GreenStrength * GetWhiteProtectionFactor(color, zone.GreenWhiteProtection);
        result = lerp(result, lerp(result, lutColor, color.g), effectiveStrength);
    }

    if (applyBlue)
    {
        float3 lutColor = Zone3_BlueLUT.Sample(PointSampler, float2(color.b, 0.5)).rgb;
        float effectiveStrength = zone.BlueStrength * GetWhiteProtectionFactor(color, zone.BlueWhiteProtection);
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

int GetZoneIndex(float2 screenPos)
{
    // Fullscreen - always zone 0
    if (SplitModeValue < 0.5)
    {
        return 0;
    }

    float splitX = ViewportSize.x * SplitPosition;
    float splitY = ViewportSize.y * SplitPositionV;

    // Split Vertical (1) - left/right
    if (SplitModeValue < 1.5)
    {
        return screenPos.x < splitX ? 0 : 1;
    }

    // Split Horizontal (2) - top/bottom
    if (SplitModeValue < 2.5)
    {
        return screenPos.y < splitY ? 0 : 1;
    }

    // Quadrants (3) - 4 zones
    bool left = screenPos.x < splitX;
    bool top = screenPos.y < splitY;

    if (top && left) return 0;
    if (top && !left) return 1;
    if (!top && left) return 2;
    return 3;
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
// Pixel Shader
// ============================================================================

float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;
    float2 screenPos = uv * ViewportSize;

    // Sample original screen color
    float4 screenColor = ScreenTexture.Sample(LinearSampler, uv);
    float3 color = screenColor.rgb;

    // Get zone index
    int zoneIndex = GetZoneIndex(screenPos);

    float3 finalColor;

    // In comparison mode, zone 0 always shows original
    bool forceOriginal = (ComparisonMode > 0.5 && zoneIndex == 0);

    if (forceOriginal)
    {
        finalColor = color;
    }
    else
    {
        // Get zone parameters and process
        ZoneParams zone;
        if (zoneIndex == 0)
            zone = Zone0;
        else if (zoneIndex == 1)
            zone = Zone1;
        else if (zoneIndex == 2)
            zone = Zone2;
        else
            zone = Zone3;

        finalColor = ProcessZone(color, zone, zoneIndex);
    }

    // Draw separator line
    float separatorAlpha = GetSeparatorAlpha(screenPos);
    if (separatorAlpha > 0.0)
    {
        finalColor = lerp(finalColor, float3(0.2, 0.2, 0.2), separatorAlpha);
    }

    return float4(finalColor, 1.0);
}
