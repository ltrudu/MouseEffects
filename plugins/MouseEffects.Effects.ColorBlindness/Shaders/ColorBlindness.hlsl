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
    float _zone0_pad1;          // 4 bytes - padding
    float _zone0_pad2;          // 4 bytes - padding
    float4 Zone0_MatrixRow0;    // 16 bytes - RGB matrix row 0
    float4 Zone0_MatrixRow1;    // 16 bytes - RGB matrix row 1
    float4 Zone0_MatrixRow2;    // 16 bytes - RGB matrix row 2

    // Zone 1 (64 bytes)
    float Zone1_CorrectionMode;
    float Zone1_LMSFilterType;
    float _zone1_pad1;
    float _zone1_pad2;
    float4 Zone1_MatrixRow0;
    float4 Zone1_MatrixRow1;
    float4 Zone1_MatrixRow2;

    // Zone 2 (64 bytes)
    float Zone2_CorrectionMode;
    float Zone2_LMSFilterType;
    float _zone2_pad1;
    float _zone2_pad2;
    float4 Zone2_MatrixRow0;
    float4 Zone2_MatrixRow1;
    float4 Zone2_MatrixRow2;

    // Zone 3 (64 bytes)
    float Zone3_CorrectionMode;
    float Zone3_LMSFilterType;
    float _zone3_pad1;
    float _zone3_pad2;
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
// 0  = None (pass through)
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
        float3 saturated = lerp(float3(gray, gray, gray), color, 1.5);
        saturated = clamp(saturated, 0.0, 1.0);
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
    float3 linearRGB = sRGBToLinear3(color);
    float3 lms = linearRGBToLMS(linearRGB);
    float3 simLMS = SimulateCVD_LMS(lms, cvdType);
    float3 simLinearRGB = LMSToLinearRGB(simLMS);
    float3 error = linearRGB - simLinearRGB;

    float3 correction = float3(0.0, 0.0, 0.0);

    if (cvdType < 4.5) // Protan/Deutan types (red-green)
    {
        correction.r = 0.0;
        correction.g = 0.7 * error.r + 1.0 * error.g;
        correction.b = 0.7 * error.r + 1.0 * error.b;
    }
    else // Tritan types (blue-yellow)
    {
        correction.r = 1.0 * error.r + 0.7 * error.b;
        correction.g = 1.0 * error.g + 0.7 * error.b;
        correction.b = 0.0;
    }

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

// Apply correction for a specific zone
float3 ApplyZoneCorrection(float3 color, int zoneIndex)
{
    float correctionMode, lmsFilterType;
    float4 matrixRow0, matrixRow1, matrixRow2;

    // Select zone parameters
    if (zoneIndex == 0)
    {
        correctionMode = Zone0_CorrectionMode;
        lmsFilterType = Zone0_LMSFilterType;
        matrixRow0 = Zone0_MatrixRow0;
        matrixRow1 = Zone0_MatrixRow1;
        matrixRow2 = Zone0_MatrixRow2;
    }
    else if (zoneIndex == 1)
    {
        correctionMode = Zone1_CorrectionMode;
        lmsFilterType = Zone1_LMSFilterType;
        matrixRow0 = Zone1_MatrixRow0;
        matrixRow1 = Zone1_MatrixRow1;
        matrixRow2 = Zone1_MatrixRow2;
    }
    else if (zoneIndex == 2)
    {
        correctionMode = Zone2_CorrectionMode;
        lmsFilterType = Zone2_LMSFilterType;
        matrixRow0 = Zone2_MatrixRow0;
        matrixRow1 = Zone2_MatrixRow1;
        matrixRow2 = Zone2_MatrixRow2;
    }
    else
    {
        correctionMode = Zone3_CorrectionMode;
        lmsFilterType = Zone3_LMSFilterType;
        matrixRow0 = Zone3_MatrixRow0;
        matrixRow1 = Zone3_MatrixRow1;
        matrixRow2 = Zone3_MatrixRow2;
    }

    // Apply correction based on mode
    if (correctionMode < 0.5)
    {
        return ApplyLMSCorrection(color, lmsFilterType);
    }
    else
    {
        return ApplyRGBMatrix(color, matrixRow0, matrixRow1, matrixRow2);
    }
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

    return float4(finalColor, 1.0);
}
