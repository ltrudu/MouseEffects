// Color Blindness Correction Shader
// Uses Daltonization algorithm to correct colors for people with color vision deficiency
// Supports circular, rectangular, and fullscreen application modes
// Based on: daltonize.org, Godot correction shader, Brettel/Vienot research

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
    float FilterType;           // Correction type (0-11)
    float OutsideFilterType;    // Outside filter (for shape modes)
    float Intensity;            // Filter intensity (0-1)
    float ColorBoost;           // Color saturation boost (0-2)
    float EdgeSoftness;         // Edge softness for shape modes (0-1)
    float EnableCurves;         // 1.0 = RGB curves enabled
    float CurveStrength;        // Strength of curve adjustment (0-1)
    float EnableCustomMatrix;   // 1.0 = Use custom matrix instead of filter type
    // Custom 3x3 color matrix (each row stored as float4, w component unused)
    float4 CustomMatrixRow0;    // Row 0: R coefficients
    float4 CustomMatrixRow1;    // Row 1: G coefficients
    float4 CustomMatrixRow2;    // Row 2: B coefficients
};

Texture2D<float4> ScreenTexture : register(t0);
Texture2D<float4> CurveLUT : register(t1);  // 256x1 lookup table for RGB curves (2D texture with height=1)
SamplerState LinearSampler : register(s0);
SamplerState PointSampler : register(s1);

// ============================================================================
// Filter Type Constants
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
// 11 = Custom Matrix

// ============================================================================
// CVD Simulation Matrices (RGB space)
// Based on daltonize.org - these matrices simulate what colorblind people see
// ============================================================================

// Simulate what a colorblind person sees in RGB space
// Returns the simulated color that the person perceives
float3 SimulateCVD_RGB(float3 color, float cvdType)
{
    float3 result;

    if (cvdType < 1.5) // Protanopia (1) - red blind
    {
        // Red cone missing - reds appear as greens/yellows
        result.r = 0.567 * color.r + 0.433 * color.g + 0.0 * color.b;
        result.g = 0.558 * color.r + 0.442 * color.g + 0.0 * color.b;
        result.b = 0.0 * color.r + 0.242 * color.g + 0.758 * color.b;
    }
    else if (cvdType < 2.5) // Protanomaly (2) - red weak
    {
        // Partial red cone deficiency
        result.r = 0.817 * color.r + 0.183 * color.g + 0.0 * color.b;
        result.g = 0.333 * color.r + 0.667 * color.g + 0.0 * color.b;
        result.b = 0.0 * color.r + 0.125 * color.g + 0.875 * color.b;
    }
    else if (cvdType < 3.5) // Deuteranopia (3) - green blind
    {
        // Green cone missing - greens appear as reds/yellows
        result.r = 0.625 * color.r + 0.375 * color.g + 0.0 * color.b;
        result.g = 0.7 * color.r + 0.3 * color.g + 0.0 * color.b;
        result.b = 0.0 * color.r + 0.3 * color.g + 0.7 * color.b;
    }
    else if (cvdType < 4.5) // Deuteranomaly (4) - green weak
    {
        // Partial green cone deficiency
        result.r = 0.8 * color.r + 0.2 * color.g + 0.0 * color.b;
        result.g = 0.258 * color.r + 0.742 * color.g + 0.0 * color.b;
        result.b = 0.0 * color.r + 0.142 * color.g + 0.858 * color.b;
    }
    else if (cvdType < 5.5) // Tritanopia (5) - blue blind
    {
        // Blue cone missing - blues appear as greens/cyans
        result.r = 0.95 * color.r + 0.05 * color.g + 0.0 * color.b;
        result.g = 0.0 * color.r + 0.433 * color.g + 0.567 * color.b;
        result.b = 0.0 * color.r + 0.475 * color.g + 0.525 * color.b;
    }
    else if (cvdType < 6.5) // Tritanomaly (6) - blue weak
    {
        // Partial blue cone deficiency
        result.r = 0.967 * color.r + 0.033 * color.g + 0.0 * color.b;
        result.g = 0.0 * color.r + 0.733 * color.g + 0.267 * color.b;
        result.b = 0.0 * color.r + 0.183 * color.g + 0.817 * color.b;
    }
    else
    {
        result = color;
    }

    return result;
}

// Grayscale weights (luminance)
static const float3 GrayscaleWeights = float3(0.2126, 0.7152, 0.0722);

// ============================================================================
// Custom Matrix Application
// ============================================================================

float3 ApplyCustomMatrix(float3 color)
{
    float3 result;
    result.r = dot(color, CustomMatrixRow0.xyz);
    result.g = dot(color, CustomMatrixRow1.xyz);
    result.b = dot(color, CustomMatrixRow2.xyz);
    return clamp(result, 0.0, 1.0);
}

// ============================================================================
// Daltonization Correction Algorithm
// ============================================================================

float3 ApplyCorrection(float3 color, float cvdType, float useCustomMatrix)
{
    // Check if custom matrix is enabled
    if (useCustomMatrix > 0.5)
    {
        return ApplyCustomMatrix(color);
    }

    // No correction needed
    if (cvdType < 0.5)
    {
        return color;
    }

    // Handle Achromatopsia (7) - complete color blindness
    if (cvdType > 6.5 && cvdType < 7.5)
    {
        // Person sees only grayscale - enhance luminance contrast
        float gray = dot(color, GrayscaleWeights);

        // Apply contrast enhancement curve
        // S-curve: increases contrast in midtones
        float enhanced = gray;
        if (gray < 0.5)
        {
            enhanced = 2.0 * gray * gray;
        }
        else
        {
            enhanced = 1.0 - 2.0 * (1.0 - gray) * (1.0 - gray);
        }

        // Boost dark/light separation
        float contrast = lerp(gray, enhanced, 0.5);
        return float3(contrast, contrast, contrast);
    }

    // Handle Achromatomaly (8) - partial color blindness
    if (cvdType > 7.5 && cvdType < 8.5)
    {
        // Partial color blindness - boost saturation and contrast
        float gray = dot(color, GrayscaleWeights);

        // Increase saturation to help distinguish colors
        float3 saturated = lerp(float3(gray, gray, gray), color, 1.5);
        saturated = clamp(saturated, 0.0, 1.0);

        // Apply mild contrast enhancement
        float3 enhanced = saturated;
        enhanced = (enhanced - 0.5) * 1.2 + 0.5;

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

    // Handle Custom Matrix (11) - already handled above, but fallback
    if (cvdType > 10.5)
    {
        return ApplyCustomMatrix(color);
    }

    // Daltonization algorithm for dichromacy and anomalous trichromacy (types 1-6)
    // Based on daltonize.org algorithm

    // Step 1: Simulate what the colorblind person sees (in RGB space)
    float3 simRgb = SimulateCVD_RGB(color, cvdType);

    // Step 2: Calculate the error (what colors are lost/confused)
    float3 error = color - simRgb;

    // Step 3: Shift the lost colors into the visible spectrum
    // The error modification matrix shifts lost information to channels the person can see
    float3 correction = float3(0.0, 0.0, 0.0);

    if (cvdType < 4.5) // Protan/Deutan types (red-green blindness)
    {
        // Person can still see blue well, so shift red/green errors to blue
        // Also boost green channel slightly with red error
        correction.r = 0.0;
        correction.g = 0.7 * error.r + 1.0 * error.g;
        correction.b = 0.7 * error.r + 1.0 * error.b;
    }
    else // Tritan types (blue-yellow blindness)
    {
        // Person can still see red/green, so shift blue errors to those channels
        correction.r = 1.0 * error.r + 0.7 * error.b;
        correction.g = 1.0 * error.g + 0.7 * error.b;
        correction.b = 0.0;
    }

    // Step 4: Add correction to original color
    float3 result = color + correction;

    return clamp(result, 0.0, 1.0);
}

// ============================================================================
// Utility Functions
// ============================================================================

// Apply saturation/color boost
float3 ApplyColorBoost(float3 color, float boost)
{
    float gray = dot(color, GrayscaleWeights);
    float3 grayColor = float3(gray, gray, gray);

    // Boost > 1 increases saturation, < 1 decreases
    return lerp(grayColor, color, boost);
}

// Apply RGB curves from lookup texture
float3 ApplyCurves(float3 color, float strength)
{
    // Sample the curve LUT for each channel
    // The LUT is a 2D texture (256x1), so we sample with float2(value, 0.5)
    // The LUT contains: R curve in .r, G curve in .g, B curve in .b, Master in .a
    float4 rSample = CurveLUT.Sample(PointSampler, float2(color.r, 0.5));
    float4 gSample = CurveLUT.Sample(PointSampler, float2(color.g, 0.5));
    float4 bSample = CurveLUT.Sample(PointSampler, float2(color.b, 0.5));

    // Apply individual channel curves first, then master curve
    float3 curved;
    curved.r = CurveLUT.Sample(PointSampler, float2(rSample.r, 0.5)).a; // R through R curve, then master
    curved.g = CurveLUT.Sample(PointSampler, float2(gSample.g, 0.5)).a; // G through G curve, then master
    curved.b = CurveLUT.Sample(PointSampler, float2(bSample.b, 0.5)).a; // B through B curve, then master

    // Blend based on strength
    return lerp(color, curved, strength);
}

// Calculate effect mask based on shape mode
float CalculateShapeMask(float2 screenPos, float2 mousePos)
{
    if (ShapeMode > 1.5)
    {
        // Fullscreen - always apply
        return 1.0;
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
            // Inside rectangle
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

// Generates fullscreen quad procedurally
PSInput VSMain(uint vertexId : SV_VertexID)
{
    PSInput output;

    // Generate fullscreen triangle strip: 0,1,2,3 -> positions
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y; // Flip Y for DirectX
    output.TexCoord = uv;

    return output;
}

// ============================================================================
// Pixel Shader
// ============================================================================

// Applies color blindness correction
// Always renders the full screen - effect area gets corrected, rest is passthrough
float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;
    float2 screenPos = uv * ViewportSize;

    // Sample original screen color
    float4 screenColor = ScreenTexture.Sample(LinearSampler, uv);
    float3 color = screenColor.rgb;

    // Calculate shape mask
    float mask = CalculateShapeMask(screenPos, MousePosition);

    // Apply RGB curves if enabled (applies to both inside and outside)
    float3 curvedColor = color;
    if (EnableCurves > 0.5)
    {
        curvedColor = ApplyCurves(color, CurveStrength);
    }

    // Apply inside correction
    float3 insideColor = curvedColor;
    float3 insideCorrected = ApplyCorrection(curvedColor, FilterType, EnableCustomMatrix);
    insideCorrected = lerp(curvedColor, insideCorrected, Intensity);
    insideCorrected = ApplyColorBoost(insideCorrected, ColorBoost);

    // Apply outside correction (custom matrix only applies to inside)
    float3 outsideColor = curvedColor;
    float3 outsideCorrected = ApplyCorrection(curvedColor, OutsideFilterType, 0.0);
    outsideCorrected = lerp(curvedColor, outsideCorrected, Intensity);
    outsideCorrected = ApplyColorBoost(outsideCorrected, ColorBoost);

    // Blend inside and outside based on mask
    color = lerp(outsideCorrected, insideCorrected, mask);

    // Always output fully opaque
    return float4(color, 1.0);
}
