// Color Blindness Correction Shader
// Supports two correction algorithms:
// 1. LMS Correction - Scientific approach with gamma correction (DaltonLens)
// 2. RGB Matrix - Simple customizable matrix multiplication
// Supports circular, rectangular, and fullscreen application modes

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

cbuffer ColorBlindnessParams : register(b0)
{
    float2 MousePosition;       // Mouse position in screen pixels
    float2 ViewportSize;        // Viewport size in pixels
    float Radius;               // Radius for circular mode (pixels)
    float RectWidth;            // Width for rectangular mode (pixels)
    float RectHeight;           // Height for rectangular mode (pixels)
    float ShapeMode;            // 0=circle, 1=rectangle, 2=fullscreen
    float CorrectionMode;       // 0=LMS, 1=RGB Matrix
    float LMSFilterType;        // LMS filter type (inside)
    float LMSOutsideFilterType; // LMS filter type (outside)
    float Intensity;            // Filter intensity (0-1)
    float ColorBoost;           // Color saturation boost (0-2)
    float EdgeSoftness;         // Edge softness for shape modes (0-1)
    float EnableCurves;         // 1.0 = RGB curves enabled
    float CurveStrength;        // Strength of curve adjustment (0-1)
    // RGB Matrix (inside) - used in RGB Matrix mode
    float4 InsideMatrixRow0;    // Row 0: R coefficients
    float4 InsideMatrixRow1;    // Row 1: G coefficients
    float4 InsideMatrixRow2;    // Row 2: B coefficients
    // RGB Matrix (outside) - used in RGB Matrix mode
    float4 OutsideMatrixRow0;   // Row 0: R coefficients
    float4 OutsideMatrixRow1;   // Row 1: G coefficients
    float4 OutsideMatrixRow2;   // Row 2: B coefficients
};

Texture2D<float4> ScreenTexture : register(t0);
Texture2D<float4> CurveLUT : register(t1);
SamplerState LinearSampler : register(s0);
SamplerState PointSampler : register(s1);

// ============================================================================
// LMS Filter Type Constants
// ============================================================================
// 0  = None
// 1  = Protanopia (red-blind)
// 2  = Protanomaly (red-weak)
// 3  = Deuteranopia (green-blind)
// 4  = Deuteranomaly (green-weak)
// 5  = Tritanopia (blue-blind)
// 6  = Tritanomaly (blue-weak)
// 7  = Achromatopsia (monochromacy)
// 8  = Achromatomaly (weak color)
// 9  = Grayscale
// 10 = Inverted Grayscale

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
// LMS CVD Simulation (Viénot/Brettel)
// ============================================================================

float3 SimulateCVD_LMS(float3 lms, float cvdType)
{
    float l = lms.x;
    float m = lms.y;
    float s = lms.z;

    float3 result = lms;

    if (cvdType < 1.5) // Protanopia (1) - L cone missing
    {
        // L is derived from M and S
        result.x = 2.02344 * m - 2.52580 * s;
        result.y = m;
        result.z = s;
    }
    else if (cvdType < 2.5) // Protanomaly (2) - L cone weak
    {
        float3 simulated;
        simulated.x = 2.02344 * m - 2.52580 * s;
        simulated.y = m;
        simulated.z = s;
        result = lerp(lms, simulated, 0.5);
    }
    else if (cvdType < 3.5) // Deuteranopia (3) - M cone missing
    {
        // M is derived from L and S
        result.x = l;
        result.y = 0.49421 * l + 1.24827 * s;
        result.z = s;
    }
    else if (cvdType < 4.5) // Deuteranomaly (4) - M cone weak
    {
        float3 simulated;
        simulated.x = l;
        simulated.y = 0.49421 * l + 1.24827 * s;
        simulated.z = s;
        result = lerp(lms, simulated, 0.5);
    }
    else if (cvdType < 5.5) // Tritanopia (5) - S cone missing
    {
        // Brettel two-plane model
        // S is derived from L and M
        if (l * 0.34478 - m * 0.65518 >= 0)
        {
            result.z = -0.00257 * l + 0.05366 * m;
        }
        else
        {
            result.z = -0.06011 * l + 0.16299 * m;
        }
        result.x = l;
        result.y = m;
    }
    else if (cvdType < 6.5) // Tritanomaly (6) - S cone weak
    {
        float3 simulated = lms;
        if (l * 0.34478 - m * 0.65518 >= 0)
        {
            simulated.z = -0.00257 * l + 0.05366 * m;
        }
        else
        {
            simulated.z = -0.06011 * l + 0.16299 * m;
        }
        result = lerp(lms, simulated, 0.5);
    }

    return result;
}

// ============================================================================
// LMS Daltonization Correction
// ============================================================================

float3 ApplyLMSCorrection(float3 color, float cvdType)
{
    // No correction needed
    if (cvdType < 0.5)
    {
        return color;
    }

    // Handle Achromatopsia (7) - complete color blindness
    if (cvdType > 6.5 && cvdType < 7.5)
    {
        float gray = dot(color, GrayscaleWeights);
        // Apply contrast enhancement S-curve
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

    // Handle Achromatomaly (8) - partial color blindness
    if (cvdType > 7.5 && cvdType < 8.5)
    {
        float gray = dot(color, GrayscaleWeights);
        // Increase saturation to help distinguish colors
        float3 saturated = lerp(float3(gray, gray, gray), color, 1.5);
        saturated = clamp(saturated, 0.0, 1.0);
        // Apply mild contrast enhancement
        float3 enhanced = (saturated - 0.5) * 1.2 + 0.5;
        return clamp(enhanced, 0.0, 1.0);
    }

    // Handle Grayscale (9)
    if (cvdType > 8.5 && cvdType < 9.5)
    {
        float gray = dot(color, GrayscaleWeights);
        return float3(gray, gray, gray);
    }

    // Handle Inverted Grayscale (10)
    if (cvdType > 9.5 && cvdType < 10.5)
    {
        float gray = 1.0 - dot(color, GrayscaleWeights);
        return float3(gray, gray, gray);
    }

    // Daltonization for types 1-6
    // Step 1: Convert sRGB to Linear RGB
    float3 linearRGB = sRGBToLinear3(color);

    // Step 2: Convert to LMS
    float3 lms = linearRGBToLMS(linearRGB);

    // Step 3: Simulate what colorblind person sees
    float3 simLMS = SimulateCVD_LMS(lms, cvdType);

    // Step 4: Convert simulated LMS back to Linear RGB
    float3 simLinearRGB = LMSToLinearRGB(simLMS);

    // Step 5: Calculate error in linear space
    float3 error = linearRGB - simLinearRGB;

    // Step 6: Shift lost colors into visible spectrum
    float3 correction = float3(0.0, 0.0, 0.0);

    if (cvdType < 4.5) // Protan/Deutan types (red-green)
    {
        // Shift red/green errors to green and blue
        correction.r = 0.0;
        correction.g = 0.7 * error.r + 1.0 * error.g;
        correction.b = 0.7 * error.r + 1.0 * error.b;
    }
    else // Tritan types (blue-yellow)
    {
        // Shift blue errors to red and green
        correction.r = 1.0 * error.r + 0.7 * error.b;
        correction.g = 1.0 * error.g + 0.7 * error.b;
        correction.b = 0.0;
    }

    // Step 7: Add correction and convert back to sRGB
    float3 correctedLinear = linearRGB + correction;
    correctedLinear = clamp(correctedLinear, 0.0, 1.0);

    return linearToSRGB3(correctedLinear);
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

float CalculateShapeMask(float2 screenPos, float2 mousePos)
{
    if (ShapeMode > 1.5)
    {
        return 1.0; // Fullscreen
    }

    float2 toMouse = screenPos - mousePos;
    float mask = 0.0;

    if (ShapeMode < 0.5)
    {
        // Circle mode
        float dist = length(toMouse);
        float normalizedDist = dist / Radius;

        if (normalizedDist < 1.0)
        {
            mask = 1.0 - normalizedDist;
            mask = smoothstep(0.0, EdgeSoftness + 0.001, mask);
        }
    }
    else
    {
        // Rectangle mode
        float2 halfSize = float2(RectWidth, RectHeight) * 0.5;
        float2 absToMouse = abs(toMouse);

        if (absToMouse.x < halfSize.x && absToMouse.y < halfSize.y)
        {
            float2 edgeDist = halfSize - absToMouse;
            float minEdgeDist = min(edgeDist.x / halfSize.x, edgeDist.y / halfSize.y);
            mask = smoothstep(0.0, EdgeSoftness + 0.001, minEdgeDist);
        }
    }

    return mask;
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

    // Sample original screen color
    float4 screenColor = ScreenTexture.Sample(LinearSampler, uv);
    float3 color = screenColor.rgb;

    // Calculate shape mask
    float mask = CalculateShapeMask(screenPos, MousePosition);

    // Apply RGB curves if enabled
    float3 curvedColor = color;
    if (EnableCurves > 0.5)
    {
        curvedColor = ApplyCurves(color, CurveStrength);
    }

    float3 insideColor, outsideColor;

    if (CorrectionMode < 0.5)
    {
        // LMS Correction Mode
        insideColor = ApplyLMSCorrection(curvedColor, LMSFilterType);
        outsideColor = ApplyLMSCorrection(curvedColor, LMSOutsideFilterType);
    }
    else
    {
        // RGB Matrix Mode
        insideColor = ApplyRGBMatrix(curvedColor, InsideMatrixRow0, InsideMatrixRow1, InsideMatrixRow2);
        outsideColor = ApplyRGBMatrix(curvedColor, OutsideMatrixRow0, OutsideMatrixRow1, OutsideMatrixRow2);
    }

    // Apply intensity
    insideColor = lerp(curvedColor, insideColor, Intensity);
    outsideColor = lerp(curvedColor, outsideColor, Intensity);

    // Apply color boost
    insideColor = ApplyColorBoost(insideColor, ColorBoost);
    outsideColor = ApplyColorBoost(outsideColor, ColorBoost);

    // Blend inside and outside based on mask
    color = lerp(outsideColor, insideColor, mask);

    return float4(color, 1.0);
}
