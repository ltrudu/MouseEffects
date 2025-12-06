// Color Blindness NG Shader
// Next-generation CVD simulation and correction
//
// Two operating modes:
// 1. SIMULATION - Scientific matrices showing what CVD people see
//    - Machado et al. (2009) - Direct RGB matrices
//    - Brettel/Vienot (1997) - LMS colorspace simulation
//
// 2. CORRECTION - LUT-based color remapping for practical enhancement
//    - Per-channel LUT mapping
//    - Multiple application modes

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// ============================================================================
// Constant Buffer
// ============================================================================

cbuffer ColorBlindnessNGParams : register(b0)
{
    float2 ViewportSize;        // 8 bytes
    float Mode;                 // 4 bytes - 0=Simulation, 1=Correction
    float SimulationFilterType; // 4 bytes - Filter type for simulation
    float ApplicationMode;      // 4 bytes - 0=Full, 1=Dominant, 2=Threshold
    float Threshold;            // 4 bytes - Threshold for threshold mode
    float Intensity;            // 4 bytes - Global intensity
    float RedEnabled;           // 4 bytes
    float RedStrength;          // 4 bytes
    float GreenEnabled;         // 4 bytes
    float GreenStrength;        // 4 bytes
    float BlueEnabled;          // 4 bytes
    float BlueStrength;         // 4 bytes
    float _padding;             // 4 bytes
};

// Textures and Samplers
Texture2D<float4> ScreenTexture : register(t0);
Texture2D<float4> RedLUT : register(t1);
Texture2D<float4> GreenLUT : register(t2);
Texture2D<float4> BlueLUT : register(t3);
SamplerState LinearSampler : register(s0);
SamplerState PointSampler : register(s1);

// ============================================================================
// Simulation Filter Type Constants
// ============================================================================
// Machado et al. (2009) - RGB-space matrices
// 0  = None (pass through)
// 1  = Protanopia (Machado)
// 2  = Protanomaly (Machado)
// 3  = Deuteranopia (Machado)
// 4  = Deuteranomaly (Machado)
// 5  = Tritanopia (Machado)
// 6  = Tritanomaly (Machado)
// Brettel/Vienot (1997) - LMS Space (Strict)
// 7  = Protanopia (Strict)
// 8  = Protanomaly (Strict)
// 9  = Deuteranopia (Strict)
// 10 = Deuteranomaly (Strict)
// 11 = Tritanopia (Strict)
// 12 = Tritanomaly (Strict)
// Other
// 13 = Achromatopsia
// 14 = Achromatomaly

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
// Machado et al. (2009) CVD Simulation Matrices
// ============================================================================
// Source: "A Physiologically-based Model for Simulation of Color Vision Deficiency"
// IEEE Transactions on Visualization and Computer Graphics, Vol. 15, No. 6, 2009
// Official Page: https://www.inf.ufrgs.br/~oliveira/pubs_files/CVD_Simulation/CVD_Simulation.html
//
// These are the VERIFIED ORIGINAL VALUES at 100% severity (dichromacy)

// Protanopia (100% L-cone loss) - Machado 2009
static const float3x3 Machado_Protanopia = float3x3(
    0.567, 0.433, 0.000,
    0.558, 0.442, 0.000,
    0.000, 0.242, 0.758
);

// Protanomaly (50% severity) - Machado 2009
static const float3x3 Machado_Protanomaly = float3x3(
    0.817, 0.183, 0.000,
    0.333, 0.667, 0.000,
    0.000, 0.125, 0.875
);

// Deuteranopia (100% M-cone loss) - Machado 2009
static const float3x3 Machado_Deuteranopia = float3x3(
    0.625, 0.375, 0.000,
    0.700, 0.300, 0.000,
    0.000, 0.300, 0.700
);

// Deuteranomaly (50% severity) - Machado 2009
static const float3x3 Machado_Deuteranomaly = float3x3(
    0.800, 0.200, 0.000,
    0.258, 0.742, 0.000,
    0.000, 0.142, 0.858
);

// Tritanopia (100% S-cone loss) - Machado 2009
static const float3x3 Machado_Tritanopia = float3x3(
    0.950, 0.050, 0.000,
    0.000, 0.433, 0.567,
    0.000, 0.475, 0.525
);

// Tritanomaly (50% severity) - Machado 2009
static const float3x3 Machado_Tritanomaly = float3x3(
    0.967, 0.033, 0.000,
    0.000, 0.733, 0.267,
    0.000, 0.183, 0.817
);

// ============================================================================
// Brettel/Vienot (1997) Strict LMS Simulation
// ============================================================================
// Source: "Computerized simulation of color appearance for dichromats"
// Journal of the Optical Society of America A, Vol. 14, No. 10, 1997

// Strict Protanopia - L-cone deficient
// L' = M (reconstruct L from M only)
static const float3x3 Strict_Protanopia_LMS = float3x3(
    0.0, 1.0, 0.0,  // L' = M
    0.0, 1.0, 0.0,  // M' = M
    0.0, 0.0, 1.0   // S' = S
);

// Strict Deuteranopia - M-cone deficient
// M' = L (reconstruct M from L only)
static const float3x3 Strict_Deuteranopia_LMS = float3x3(
    1.0, 0.0, 0.0,  // L' = L
    1.0, 0.0, 0.0,  // M' = L
    0.0, 0.0, 1.0   // S' = S
);

// Strict Tritanopia - S-cone deficient (Vienot 1999)
// S' = -0.395913*L + 0.801109*M (preserves white)
static const float3x3 Strict_Tritanopia_LMS = float3x3(
    1.0,       0.0,      0.0,
    0.0,       1.0,      0.0,
    -0.395913, 0.801109, 0.0
);

// Apply Machado CVD simulation
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

// Apply Strict LMS-based CVD simulation
// cvdType: 7=Protanopia, 8=Protanomaly, 9=Deuteranopia, 10=Deuteranomaly, 11=Tritanopia, 12=Tritanomaly
float3 SimulateStrict(float3 linearRGB, float cvdType)
{
    // Convert to LMS
    float3 lms = linearRGBToLMS(linearRGB);
    float3 simLMS;

    // Map cvdType 7-12 to the simulation
    float strictType = cvdType - 6.0;

    if (strictType < 1.5) // Protanopia (7 -> 1)
    {
        simLMS.x = dot(lms, Strict_Protanopia_LMS[0]);
        simLMS.y = dot(lms, Strict_Protanopia_LMS[1]);
        simLMS.z = dot(lms, Strict_Protanopia_LMS[2]);
    }
    else if (strictType < 2.5) // Protanomaly (8 -> 2) - 50% blend
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
    else if (strictType < 4.5) // Deuteranomaly (10 -> 4) - 50% blend
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
// CVD Simulation (shows what colorblind people see)
// ============================================================================

float3 ApplySimulation(float3 color, float cvdType)
{
    // No simulation needed
    if (cvdType < 0.5)
    {
        return color;
    }

    // Handle Achromatopsia (13) - complete color blindness
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
// LUT-Based Correction
// ============================================================================

float3 ApplyLUTCorrection(float3 color)
{
    float3 result = color;

    // Determine if we should apply LUTs based on application mode
    bool applyRed = false;
    bool applyGreen = false;
    bool applyBlue = false;

    if (ApplicationMode < 0.5) // Full Channel
    {
        applyRed = RedEnabled > 0.5 && color.r > 0.001;
        applyGreen = GreenEnabled > 0.5 && color.g > 0.001;
        applyBlue = BlueEnabled > 0.5 && color.b > 0.001;
    }
    else if (ApplicationMode < 1.5) // Dominant Only
    {
        applyRed = RedEnabled > 0.5 && color.r > color.g && color.r > color.b;
        applyGreen = GreenEnabled > 0.5 && color.g > color.r && color.g > color.b;
        applyBlue = BlueEnabled > 0.5 && color.b > color.r && color.b > color.g;
    }
    else // Threshold
    {
        applyRed = RedEnabled > 0.5 && color.r > Threshold;
        applyGreen = GreenEnabled > 0.5 && color.g > Threshold;
        applyBlue = BlueEnabled > 0.5 && color.b > Threshold;
    }

    // Apply Red channel LUT
    if (applyRed)
    {
        float3 lutColor = RedLUT.Sample(PointSampler, float2(color.r, 0.5)).rgb;
        // Blend based on the original channel intensity and strength
        result = lerp(result, lerp(result, lutColor, color.r), RedStrength);
    }

    // Apply Green channel LUT
    if (applyGreen)
    {
        float3 lutColor = GreenLUT.Sample(PointSampler, float2(color.g, 0.5)).rgb;
        result = lerp(result, lerp(result, lutColor, color.g), GreenStrength);
    }

    // Apply Blue channel LUT
    if (applyBlue)
    {
        float3 lutColor = BlueLUT.Sample(PointSampler, float2(color.b, 0.5)).rgb;
        result = lerp(result, lerp(result, lutColor, color.b), BlueStrength);
    }

    return result;
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

    // Sample original screen color
    float4 screenColor = ScreenTexture.Sample(LinearSampler, uv);
    float3 color = screenColor.rgb;

    float3 processedColor;

    // Apply based on mode
    if (Mode < 0.5) // Simulation Mode
    {
        processedColor = ApplySimulation(color, SimulationFilterType);
    }
    else // Correction Mode
    {
        processedColor = ApplyLUTCorrection(color);
    }

    // Apply intensity blend
    float3 finalColor = lerp(color, processedColor, Intensity);

    return float4(finalColor, 1.0);
}
