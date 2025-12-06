// Color Blindness Correction Shader
// Supports two correction algorithms:
// 1. LMS Correction - Scientific approach with gamma correction (DaltonLens)
// 2. RGB Matrix - Simple customizable matrix multiplication
// Supports multiple layout modes with up to 4 zones

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// ============================================================================
// Constant Buffer - Zone-Based Architecture
// ============================================================================
// Layout Modes: 0=Fullscreen, 1=Circle, 2=Rectangle, 3=SplitV, 4=SplitH, 5=Quadrants

cbuffer ColorBlindnessParams : register(b0)
{
    // General settings (64 bytes)
    float2 MousePosition;       // 8 bytes - Mouse position in screen pixels
    float2 ViewportSize;        // 8 bytes - Viewport size in pixels
    float LayoutMode;           // 4 bytes - 0-5
    float Radius;               // 4 bytes - For circle mode (pixels)
    float RectWidth;            // 4 bytes - For rectangle mode (pixels)
    float RectHeight;           // 4 bytes - For rectangle mode (pixels)
    float SplitPosition;        // 4 bytes - Horizontal split (0-1)
    float SplitPositionV;       // 4 bytes - Vertical split (0-1)
    float EdgeSoftness;         // 4 bytes - Edge softness (0-1)
    float Intensity;            // 4 bytes - Filter intensity (0-1)
    float ColorBoost;           // 4 bytes - Saturation boost (0-2)
    float EnableCurves;         // 4 bytes - 1.0 = RGB curves enabled
    float CurveStrength;        // 4 bytes - Strength of curve adjustment (0-1)
    float ComparisonMode;       // 4 bytes - 0=off, 1=on - copy screen to all zones

    // Zone 0 (64 bytes)
    float Zone0_CorrectionMode; // 4 bytes - 0=LMS, 1=RGB
    float Zone0_LMSFilterType;  // 4 bytes - LMS filter type
    float Zone0_SimulationMode; // 4 bytes - 0=Correction, 1=Simulation
    float _zone0_pad1;          // 4 bytes - padding
    float4 Zone0_MatrixRow0;    // 16 bytes - RGB matrix row 0
    float4 Zone0_MatrixRow1;    // 16 bytes - RGB matrix row 1
    float4 Zone0_MatrixRow2;    // 16 bytes - RGB matrix row 2

    // Zone 1 (64 bytes)
    float Zone1_CorrectionMode;
    float Zone1_LMSFilterType;
    float Zone1_SimulationMode;
    float _zone1_pad1;
    float4 Zone1_MatrixRow0;
    float4 Zone1_MatrixRow1;
    float4 Zone1_MatrixRow2;

    // Zone 2 (64 bytes)
    float Zone2_CorrectionMode;
    float Zone2_LMSFilterType;
    float Zone2_SimulationMode;
    float _zone2_pad1;
    float4 Zone2_MatrixRow0;
    float4 Zone2_MatrixRow1;
    float4 Zone2_MatrixRow2;

    // Zone 3 (64 bytes)
    float Zone3_CorrectionMode;
    float Zone3_LMSFilterType;
    float Zone3_SimulationMode;
    float _zone3_pad1;
    float4 Zone3_MatrixRow0;
    float4 Zone3_MatrixRow1;
    float4 Zone3_MatrixRow2;
};

Texture2D<float4> ScreenTexture : register(t0);
Texture2D<float4> CurveLUT : register(t1);
SamplerState LinearSampler : register(s0);
SamplerState PointSampler : register(s1);

// ============================================================================
// LMS Filter Type Constants
// ============================================================================
// Machado et al. (2009) - RGB-space matrices (fast, widely used)
// 0  = None (pass through)
// 1  = Protanopia (Machado)
// 2  = Protanomaly (Machado)
// 3  = Deuteranopia (Machado)
// 4  = Deuteranomaly (Machado)
// 5  = Tritanopia (Machado)
// 6  = Tritanomaly (Machado)
// Strict LMS - Proper LMS colorspace simulation (more accurate)
// 7  = Protanopia (Strict)
// 8  = Protanomaly (Strict)
// 9  = Deuteranopia (Strict)
// 10 = Deuteranomaly (Strict)
// 11 = Tritanopia (Strict)
// 12 = Tritanomaly (Strict)
// Other effects
// 13 = Achromatopsia (monochromacy)
// 14 = Achromatomaly (weak color)
// 15 = Grayscale
// 16 = Inverted Grayscale

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
// LMS Color Space Conversion (Smith & Pokorny)
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
// CVD Simulation Matrices - Machado et al. (2009)
// These work directly in linear RGB space - fast and widely used
// Source: https://godotshaders.com/shader/color-blind/
// ============================================================================

// Machado Protanopia 100% severity
static const float3x3 Machado_Protanopia = float3x3(
    0.152286, 1.052583, -0.204868,
    0.114503, 0.786281, 0.099216,
    -0.003882, -0.048116, 1.051998
);

// Machado Protanomaly 50% severity
static const float3x3 Machado_Protanomaly = float3x3(
    0.817, 0.333, -0.150,
    0.333, 0.667, 0.000,
    -0.017, 0.000, 1.017
);

// Machado Deuteranopia 100% severity - Original from Machado et al. (2009)
// This is the scientifically correct simulation that makes red and green
// converge toward similar yellows - exactly what deuteranopes see.
// The "15" Ishihara plate (green on green) should become nearly invisible.
static const float3x3 Machado_Deuteranopia = float3x3(
    0.367322, 0.860646, -0.227968,
    0.280085, 0.672501,  0.047413,
   -0.011820, 0.042940,  0.968881
);

// Machado Deuteranomaly 50% severity
static const float3x3 Machado_Deuteranomaly = float3x3(
    0.800, 0.200, 0.000,
    0.258, 0.742, 0.000,
    0.000, 0.142, 0.858
);

// Machado Tritanopia 100% severity
static const float3x3 Machado_Tritanopia = float3x3(
    1.255528, -0.076749, -0.178779,
    -0.078411, 0.930809, 0.147602,
    0.004733, 0.691367, 0.303900
);

// Machado Tritanomaly 50% severity
static const float3x3 Machado_Tritanomaly = float3x3(
    0.967, 0.033, 0.000,
    0.000, 0.733, 0.267,
    0.000, 0.183, 0.817
);

// Machado Achromatopsia (complete color blindness)
static const float3x3 Machado_Achromatopsia = float3x3(
    0.299, 0.587, 0.114,
    0.299, 0.587, 0.114,
    0.299, 0.587, 0.114
);

// Machado Achromatomaly (blue-cone monochromacy)
static const float3x3 Machado_Achromatomaly = float3x3(
    0.618, 0.320, 0.062,
    0.163, 0.775, 0.062,
    0.163, 0.320, 0.516
);

// ============================================================================
// Strict LMS Simulation Matrices
// These work in LMS color space for more accurate physiological simulation
// Source: ixora.io/projects/colorblindness/color-blindness-simulation-research/
// Coefficients preserve white point (sum to ~1.0)
// ============================================================================

// Strict Protanopia - L-cone deficient (reconstructs L from M only)
// Protanopes confuse red with green. Red shifts toward green.
// L' = M (no S contribution - keeps blue pure, doesn't add purple tint)
// For white (L=M=S=1): L'=1 ✓ preserves white
static const float3x3 Strict_Protanopia_LMS = float3x3(
    0.0,        1.0,         0.0,          // L' = M (red reconstructed from green only)
    0.0,        1.0,         0.0,          // M' = M (preserved)
    0.0,        0.0,         1.0           // S' = S (preserved)
);

// Strict Deuteranopia - M-cone deficient
// Uses min(M, L) approach consistent with Protanopia
// Only reduces M for greens (where M > L), preserves M for reds (where M < L)
// This ensures reds stay red, greens shift toward yellow
// (Matrix not used - see SimulateStrict function for actual implementation)

// Strict Tritanopia - S-cone deficient (reconstructs S from L and M)
// Tritanopes confuse blue with green. Blue should shift toward green, not the other way.
// S' = -0.395913*L + 0.801109*M (from Viénot 1999, preserves white)
static const float3x3 Strict_Tritanopia_LMS = float3x3(
    1.0,         0.0,         0.0,         // L' = L (preserved)
    0.0,         1.0,         0.0,         // M' = M (preserved)
    -0.395913,   0.801109,    0.0          // S' = -0.4*L + 0.8*M (reduces S for most colors)
);

// Apply Machado CVD simulation (operates directly on linear RGB)
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

// Apply Strict LMS-based CVD simulation (proper LMS colorspace)
// cvdType: 7=Protanopia, 8=Protanomaly, 9=Deuteranopia, 10=Deuteranomaly, 11=Tritanopia, 12=Tritanomaly
float3 SimulateStrict(float3 linearRGB, float cvdType)
{
    // Convert to LMS
    float3 lms = linearRGBToLMS(linearRGB);
    float3 simLMS;

    // Map cvdType 7-12 to the simulation matrices
    float strictType = cvdType - 6.0; // 1-6 range

    if (strictType < 1.5) // Protanopia (7 -> 1)
    {
        // L' = min(L, M) - only reduce L for reds, never increase for greens/blues
        // This preserves green and blue while shifting red toward green
        simLMS.x = min(lms.x, lms.y);  // L' = min(L, M)
        simLMS.y = lms.y;              // M preserved
        simLMS.z = lms.z;              // S preserved
    }
    else if (strictType < 2.5) // Protanomaly (8 -> 2) - 50% blend
    {
        float3 fullSim;
        fullSim.x = min(lms.x, lms.y);  // L' = min(L, M)
        fullSim.y = lms.y;
        fullSim.z = lms.z;
        simLMS = lerp(lms, fullSim, 0.5);
    }
    else if (strictType < 3.5) // Deuteranopia (9 -> 3)
    {
        // M' = min(M, L) - only reduce M for greens (where M > L), preserve for reds
        // This is consistent with Protanopia using min(L, M)
        // Reds stay red, greens shift toward yellow, blues unaffected
        simLMS.x = lms.x;              // L preserved
        simLMS.y = min(lms.y, lms.x);  // M' = min(M, L)
        simLMS.z = lms.z;              // S preserved
    }
    else if (strictType < 4.5) // Deuteranomaly (10 -> 4) - 50% blend
    {
        float3 fullSim;
        fullSim.x = lms.x;
        fullSim.y = min(lms.y, lms.x);  // M' = min(M, L)
        fullSim.z = lms.z;
        simLMS = lerp(lms, fullSim, 0.5);
    }
    else if (strictType < 5.5) // Tritanopia (11 -> 5)
    {
        simLMS.x = dot(lms, Strict_Tritanopia_LMS[0]);
        simLMS.y = dot(lms, Strict_Tritanopia_LMS[1]);
        simLMS.z = dot(lms, Strict_Tritanopia_LMS[2]);
    }
    else // Tritanomaly (12 -> 6) - 50% blend
    {
        float3 fullSim;
        fullSim.x = dot(lms, Strict_Tritanopia_LMS[0]);
        fullSim.y = dot(lms, Strict_Tritanopia_LMS[1]);
        fullSim.z = dot(lms, Strict_Tritanopia_LMS[2]);
        simLMS = lerp(lms, fullSim, 0.5);
    }

    // Convert back to RGB
    return LMSToLinearRGB(simLMS);
}

// ============================================================================
// Daltonization Correction
// ============================================================================

float3 ApplyLMSCorrection(float3 color, float cvdType)
{
    // No correction needed
    if (cvdType < 0.5)
    {
        return color;
    }

    // Handle Achromatopsia (13) - complete color blindness
    // For monochromats, enhance contrast in grayscale
    if (cvdType > 12.5 && cvdType < 13.5)
    {
        float gray = dot(color, GrayscaleWeights);
        // Apply S-curve for contrast enhancement
        float enhanced = gray;
        if (gray < 0.5)
        {
            enhanced = 2.0 * gray * gray;
        }
        else
        {
            enhanced = 1.0 - 2.0 * (1.0 - gray) * (1.0 - gray);
        }
        float contrast = lerp(gray, enhanced, 0.5);
        return float3(contrast, contrast, contrast);
    }

    // Handle Achromatomaly (14) - partial color blindness
    // Boost saturation and contrast
    if (cvdType > 13.5 && cvdType < 14.5)
    {
        float gray = dot(color, GrayscaleWeights);
        float3 saturated = lerp(float3(gray, gray, gray), color, 1.5);
        saturated = clamp(saturated, 0.0, 1.0);
        float3 enhanced = (saturated - 0.5) * 1.2 + 0.5;
        return clamp(enhanced, 0.0, 1.0);
    }

    // Handle Grayscale (15)
    if (cvdType > 14.5 && cvdType < 15.5)
    {
        float gray = dot(color, GrayscaleWeights);
        return float3(gray, gray, gray);
    }

    // Handle Inverted Grayscale (16)
    if (cvdType > 15.5 && cvdType < 16.5)
    {
        float gray = 1.0 - dot(color, GrayscaleWeights);
        return float3(gray, gray, gray);
    }

    // Convert to linear RGB for correction
    float3 linearRGB = sRGBToLinear3(color);
    float3 simLinearRGB;
    float3 error;
    float3 correction = float3(0.0, 0.0, 0.0);

    // Determine if Machado (1-6) or Strict (7-12)
    bool isMachado = cvdType < 6.5;
    bool isStrict = cvdType > 6.5 && cvdType < 12.5;

    if (isMachado)
    {
        // Machado simulation for error calculation
        simLinearRGB = SimulateMachado(linearRGB, cvdType);
        simLinearRGB = clamp(simLinearRGB, 0.0, 1.0);
        error = linearRGB - simLinearRGB;

        // Redistribute error based on CVD type
        if (cvdType < 2.5) // Protanopia, Protanomaly (1, 2) - red-blind
        {
            // Only correct actual reds (positive error.r), not colors that gained red
            float redError = max(0.0, error.r);
            correction.r = 0.0;
            correction.g = 0.7 * redError;
            correction.b = 1.0 * redError;
        }
        else if (cvdType < 4.5) // Deuteranopia, Deuteranomaly (3, 4) - green-blind
        {
            // Only correct actual greens (positive error.g), leave reds unchanged
            float greenError = max(0.0, error.g);
            correction.r = 0.0;
            correction.g = -0.5 * greenError;   // Reduce green
            correction.b = 1.5 * greenError;    // Add blue (shift to cyan)
        }
        else // Tritanopia, Tritanomaly (5, 6) - blue-blind
        {
            // Only correct actual blues (positive error.b)
            float blueError = max(0.0, error.b);
            correction.r = 0.7 * blueError;
            correction.g = 0.7 * blueError;
            correction.b = 0.0;
        }
    }
    else if (isStrict)
    {
        // Strict LMS simulation for error calculation
        simLinearRGB = SimulateStrict(linearRGB, cvdType);
        simLinearRGB = clamp(simLinearRGB, 0.0, 1.0);
        error = linearRGB - simLinearRGB;

        // Map strict type: 7-8 = protan, 9-10 = deutan, 11-12 = tritan
        float strictType = cvdType - 6.0;

        if (strictType < 2.5) // Protanopia, Protanomaly (7, 8) - red-blind
        {
            // Only correct actual reds (positive error.r)
            float redError = max(0.0, error.r);
            correction.r = 0.0;
            correction.g = 0.7 * redError;
            correction.b = 1.0 * redError;
        }
        else if (strictType < 4.5) // Deuteranopia, Deuteranomaly (9, 10) - green-blind
        {
            // Only correct actual greens (positive error.g), leave reds unchanged
            float greenError = max(0.0, error.g);
            correction.r = 0.0;
            correction.g = -0.5 * greenError;   // Reduce green
            correction.b = 1.5 * greenError;    // Add blue (shift to cyan)
        }
        else // Tritanopia, Tritanomaly (11, 12) - blue-blind
        {
            // Only correct actual blues (positive error.b)
            float blueError = max(0.0, error.b);
            correction.r = 0.7 * blueError;
            correction.g = 0.7 * blueError;
            correction.b = 0.0;
        }
    }
    else
    {
        // Unknown type - return original
        return color;
    }

    // Apply correction
    float3 correctedLinear = linearRGB + correction;
    correctedLinear = clamp(correctedLinear, 0.0, 1.0);

    return linearToSRGB3(correctedLinear);
}

// ============================================================================
// CVD Simulation (shows what colorblind people see)
// ============================================================================

float3 ApplyLMSSimulation(float3 color, float cvdType)
{
    // No simulation needed
    if (cvdType < 0.5)
    {
        return color;
    }

    // Handle Achromatopsia (13) - complete color blindness (monochromacy)
    if (cvdType > 12.5 && cvdType < 13.5)
    {
        float gray = dot(color, GrayscaleWeights);
        return float3(gray, gray, gray);
    }

    // Handle Achromatomaly (14) - partial color blindness
    if (cvdType > 13.5 && cvdType < 14.5)
    {
        float gray = dot(color, GrayscaleWeights);
        return lerp(float3(gray, gray, gray), color, 0.3);
    }

    // Handle Grayscale (15)
    if (cvdType > 14.5 && cvdType < 15.5)
    {
        float gray = dot(color, GrayscaleWeights);
        return float3(gray, gray, gray);
    }

    // Handle Inverted Grayscale (16)
    if (cvdType > 15.5 && cvdType < 16.5)
    {
        float gray = 1.0 - dot(color, GrayscaleWeights);
        return float3(gray, gray, gray);
    }

    // Convert to linear for simulation
    float3 linearRGB = sRGBToLinear3(color);
    float3 simLinearRGB;

    // Machado simulation (1-6)
    if (cvdType < 6.5)
    {
        simLinearRGB = SimulateMachado(linearRGB, cvdType);
    }
    // Strict LMS simulation (7-12)
    else if (cvdType < 12.5)
    {
        simLinearRGB = SimulateStrict(linearRGB, cvdType);
    }
    else
    {
        return color;
    }

    // Clamp and convert back to sRGB
    simLinearRGB = clamp(simLinearRGB, 0.0, 1.0);
    return linearToSRGB3(simLinearRGB);
}

// ============================================================================
// RGB Matrix Correction
// ============================================================================

float3 ApplyRGBMatrix(float3 color, float4 row0, float4 row1, float4 row2)
{
    float3 result;
    result.r = dot(color, row0.xyz);
    result.g = dot(color, row1.xyz);
    result.b = dot(color, row2.xyz);
    return clamp(result, 0.0, 1.0);
}

// ============================================================================
// Utility Functions
// ============================================================================

float3 ApplyColorBoost(float3 color, float boost)
{
    float gray = dot(color, GrayscaleWeights);
    float3 grayColor = float3(gray, gray, gray);
    return lerp(grayColor, color, boost);
}

float3 ApplyCurves(float3 color, float strength)
{
    float4 rSample = CurveLUT.Sample(PointSampler, float2(color.r, 0.5));
    float4 gSample = CurveLUT.Sample(PointSampler, float2(color.g, 0.5));
    float4 bSample = CurveLUT.Sample(PointSampler, float2(color.b, 0.5));

    float3 curved;
    curved.r = CurveLUT.Sample(PointSampler, float2(rSample.r, 0.5)).a;
    curved.g = CurveLUT.Sample(PointSampler, float2(gSample.g, 0.5)).a;
    curved.b = CurveLUT.Sample(PointSampler, float2(bSample.b, 0.5)).a;

    return lerp(color, curved, strength);
}

// ============================================================================
// Zone-Based Architecture Functions
// ============================================================================

// Get zone bounds for comparison mode
void GetZoneBounds(int zoneIndex, out float2 zoneMin, out float2 zoneMax)
{
    float splitX = ViewportSize.x * SplitPosition;
    float splitY = ViewportSize.y * SplitPositionV;

    if (LayoutMode > 4.5) // Quadrants
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
        else // Bottom-Right
        {
            zoneMin = float2(splitX, splitY);
            zoneMax = ViewportSize;
        }
    }
    else if (LayoutMode > 3.5) // Split Horizontal
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
    else // Split Vertical
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

// Get comparison UV with aspect ratio preservation for split modes
// Returns (-1, -1) if pixel is in black bar area
float2 GetComparisonUV(float2 screenPos, int zoneIndex)
{
    float2 zoneMin, zoneMax;
    GetZoneBounds(zoneIndex, zoneMin, zoneMax);
    float2 zoneSize = zoneMax - zoneMin;
    float2 zoneCenter = (zoneMin + zoneMax) * 0.5;

    if (LayoutMode > 4.5) // Quadrants - stretch to fit
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

// Get zone index and blend factor based on layout mode
void GetZoneInfo(float2 screenPos, out int zoneIndex, out float blendFactor, out int nextZoneIndex)
{
    blendFactor = 0.0;
    nextZoneIndex = 0;

    // Fullscreen (0) - single zone
    if (LayoutMode < 0.5)
    {
        zoneIndex = 0;
        return;
    }

    // Circle (1) - inside/outside zones
    if (LayoutMode < 1.5)
    {
        float dist = length(screenPos - MousePosition);
        float normalizedDist = dist / Radius;
        float edgeWidth = EdgeSoftness * 0.5;

        if (normalizedDist < 1.0 - edgeWidth)
        {
            zoneIndex = 0;
            blendFactor = 0.0;
        }
        else if (normalizedDist > 1.0 + edgeWidth)
        {
            zoneIndex = 1;
            blendFactor = 0.0;
        }
        else
        {
            zoneIndex = 0;
            nextZoneIndex = 1;
            blendFactor = smoothstep(1.0 - edgeWidth, 1.0 + edgeWidth, normalizedDist);
        }
        return;
    }

    // Rectangle (2) - inside/outside zones
    if (LayoutMode < 2.5)
    {
        float2 toMouse = abs(screenPos - MousePosition);
        float2 halfSize = float2(RectWidth, RectHeight) * 0.5;
        float2 edgeSize = halfSize * EdgeSoftness;

        bool fullyInside = toMouse.x < halfSize.x - edgeSize.x && toMouse.y < halfSize.y - edgeSize.y;
        bool fullyOutside = toMouse.x > halfSize.x + edgeSize.x || toMouse.y > halfSize.y + edgeSize.y;

        if (fullyInside)
        {
            zoneIndex = 0;
            blendFactor = 0.0;
        }
        else if (fullyOutside)
        {
            zoneIndex = 1;
            blendFactor = 0.0;
        }
        else
        {
            // In edge transition
            float2 edgeDist = (toMouse - halfSize + edgeSize) / (2.0 * edgeSize);
            edgeDist = clamp(edgeDist, 0.0, 1.0);
            float dist = max(edgeDist.x, edgeDist.y);

            zoneIndex = 0;
            nextZoneIndex = 1;
            blendFactor = smoothstep(0.0, 1.0, dist);
        }
        return;
    }

    // Split Vertical (3) - left/right zones
    if (LayoutMode < 3.5)
    {
        float splitX = ViewportSize.x * SplitPosition;
        float blendWidth = EdgeSoftness * ViewportSize.x * 0.1;
        float distToSplit = screenPos.x - splitX;

        if (distToSplit < -blendWidth)
        {
            zoneIndex = 0;
            blendFactor = 0.0;
        }
        else if (distToSplit > blendWidth)
        {
            zoneIndex = 1;
            blendFactor = 0.0;
        }
        else
        {
            zoneIndex = 0;
            nextZoneIndex = 1;
            blendFactor = smoothstep(-blendWidth, blendWidth, distToSplit);
        }
        return;
    }

    // Split Horizontal (4) - top/bottom zones
    if (LayoutMode < 4.5)
    {
        float splitY = ViewportSize.y * SplitPosition;
        float blendWidth = EdgeSoftness * ViewportSize.y * 0.1;
        float distToSplit = screenPos.y - splitY;

        if (distToSplit < -blendWidth)
        {
            zoneIndex = 0;
            blendFactor = 0.0;
        }
        else if (distToSplit > blendWidth)
        {
            zoneIndex = 1;
            blendFactor = 0.0;
        }
        else
        {
            zoneIndex = 0;
            nextZoneIndex = 1;
            blendFactor = smoothstep(-blendWidth, blendWidth, distToSplit);
        }
        return;
    }

    // Quadrants (5) - 4 zones
    float splitX = ViewportSize.x * SplitPosition;
    float splitY = ViewportSize.y * SplitPositionV;
    float blendWidthX = EdgeSoftness * ViewportSize.x * 0.05;
    float blendWidthY = EdgeSoftness * ViewportSize.y * 0.05;

    bool left = screenPos.x < splitX;
    bool top = screenPos.y < splitY;

    // Determine primary zone
    if (top && left) zoneIndex = 0;      // Top-Left
    else if (top && !left) zoneIndex = 1; // Top-Right
    else if (!top && left) zoneIndex = 2; // Bottom-Left
    else zoneIndex = 3;                   // Bottom-Right

    // Calculate blend for edges (simplified - blend to adjacent zone)
    float distToSplitX = abs(screenPos.x - splitX);
    float distToSplitY = abs(screenPos.y - splitY);

    if (distToSplitX < blendWidthX && distToSplitY < blendWidthY)
    {
        // Near center - complex 4-way blend, simplified to diagonal
        float blendX = 1.0 - distToSplitX / blendWidthX;
        float blendY = 1.0 - distToSplitY / blendWidthY;
        blendFactor = blendX * blendY * 0.5;
        nextZoneIndex = 3 - zoneIndex; // Diagonal opposite
    }
    else if (distToSplitX < blendWidthX)
    {
        // Near vertical split
        blendFactor = 1.0 - distToSplitX / blendWidthX;
        blendFactor *= 0.5;
        nextZoneIndex = left ? (top ? 1 : 3) : (top ? 0 : 2);
    }
    else if (distToSplitY < blendWidthY)
    {
        // Near horizontal split
        blendFactor = 1.0 - distToSplitY / blendWidthY;
        blendFactor *= 0.5;
        nextZoneIndex = top ? (left ? 2 : 3) : (left ? 0 : 1);
    }
}

// Apply correction or simulation for a specific zone
float3 ApplyZoneCorrection(float3 color, int zoneIndex)
{
    float correctionMode, lmsFilterType, simulationMode;
    float4 matrixRow0, matrixRow1, matrixRow2;

    // Select zone parameters
    if (zoneIndex == 0)
    {
        correctionMode = Zone0_CorrectionMode;
        lmsFilterType = Zone0_LMSFilterType;
        simulationMode = Zone0_SimulationMode;
        matrixRow0 = Zone0_MatrixRow0;
        matrixRow1 = Zone0_MatrixRow1;
        matrixRow2 = Zone0_MatrixRow2;
    }
    else if (zoneIndex == 1)
    {
        correctionMode = Zone1_CorrectionMode;
        lmsFilterType = Zone1_LMSFilterType;
        simulationMode = Zone1_SimulationMode;
        matrixRow0 = Zone1_MatrixRow0;
        matrixRow1 = Zone1_MatrixRow1;
        matrixRow2 = Zone1_MatrixRow2;
    }
    else if (zoneIndex == 2)
    {
        correctionMode = Zone2_CorrectionMode;
        lmsFilterType = Zone2_LMSFilterType;
        simulationMode = Zone2_SimulationMode;
        matrixRow0 = Zone2_MatrixRow0;
        matrixRow1 = Zone2_MatrixRow1;
        matrixRow2 = Zone2_MatrixRow2;
    }
    else
    {
        correctionMode = Zone3_CorrectionMode;
        lmsFilterType = Zone3_LMSFilterType;
        simulationMode = Zone3_SimulationMode;
        matrixRow0 = Zone3_MatrixRow0;
        matrixRow1 = Zone3_MatrixRow1;
        matrixRow2 = Zone3_MatrixRow2;
    }

    // Apply based on correction mode (LMS vs RGB) and simulation mode
    if (correctionMode < 0.5)
    {
        // LMS mode - check if simulation or correction
        if (simulationMode > 0.5)
        {
            return ApplyLMSSimulation(color, lmsFilterType);
        }
        else
        {
            return ApplyLMSCorrection(color, lmsFilterType);
        }
    }
    else
    {
        // RGB Matrix mode (always applies the matrix directly)
        return ApplyRGBMatrix(color, matrixRow0, matrixRow1, matrixRow2);
    }
}

// ============================================================================
// Virtual Cursor for Comparison Mode
// ============================================================================

// Get the transformed mouse position within a zone for comparison mode
float2 GetTransformedMousePos(int zoneIndex)
{
    float2 zoneMin, zoneMax;
    GetZoneBounds(zoneIndex, zoneMin, zoneMax);
    float2 zoneSize = zoneMax - zoneMin;
    float2 zoneCenter = (zoneMin + zoneMax) * 0.5;

    // Normalize mouse position to 0-1
    float2 normalizedMouse = MousePosition / ViewportSize;

    if (LayoutMode > 4.5) // Quadrants - stretch to fit
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
// Pixel Shader
// ============================================================================

float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;
    float2 screenPos = uv * ViewportSize;

    // Get zone information
    int zoneIndex, nextZoneIndex;
    float blendFactor;
    GetZoneInfo(screenPos, zoneIndex, blendFactor, nextZoneIndex);

    // Handle comparison mode for split/quadrant layouts
    float2 sampleUV = uv;
    bool isBlackBar = false;

    if (ComparisonMode > 0.5 && LayoutMode > 2.5)
    {
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

    // Sample original screen color
    float4 screenColor = ScreenTexture.Sample(LinearSampler, sampleUV);
    float3 color = screenColor.rgb;

    // Apply RGB curves if enabled
    float3 curvedColor = color;
    if (EnableCurves > 0.5)
    {
        curvedColor = ApplyCurves(color, CurveStrength);
    }

    // Apply zone correction
    float3 zoneColor = ApplyZoneCorrection(curvedColor, zoneIndex);

    // Blend with next zone if needed
    if (blendFactor > 0.001)
    {
        float3 nextZoneColor = ApplyZoneCorrection(curvedColor, nextZoneIndex);
        zoneColor = lerp(zoneColor, nextZoneColor, blendFactor);
    }

    // Apply intensity
    float3 finalColor = lerp(curvedColor, zoneColor, Intensity);

    // Apply color boost
    finalColor = ApplyColorBoost(finalColor, ColorBoost);

    // Draw virtual cursor in comparison mode
    if (ComparisonMode > 0.5 && LayoutMode > 2.5)
    {
        float2 transformedMouse = GetTransformedMousePos(zoneIndex);
        float cursorSize = 15.0;

        // Scale cursor size based on zone size (smaller for quadrants)
        if (LayoutMode > 4.5) // Quadrants
        {
            cursorSize = 12.0;
        }

        float cursorAlpha = DrawCursor(screenPos, transformedMouse, cursorSize);

        if (cursorAlpha > 0.5)
        {
            // Draw cursor with contrasting color (inverted luminance)
            float lum = dot(finalColor, float3(0.299, 0.587, 0.114));
            float3 cursorColor = lum > 0.5 ? float3(0.0, 0.0, 0.0) : float3(1.0, 1.0, 1.0);

            // Add colored outline for better visibility
            float2 delta = screenPos - transformedMouse;
            float dist = length(delta);

            // Inner part is solid contrasting color
            if (dist < 3.0 || abs(dist - cursorSize) < 1.5)
            {
                // Center dot and outer ring - use accent color (cyan/magenta based on zone)
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

    return float4(finalColor, 1.0);
}
