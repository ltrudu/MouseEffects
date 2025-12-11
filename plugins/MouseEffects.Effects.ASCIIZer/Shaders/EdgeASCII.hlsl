// Edge ASCII Shader
// Applies Sobel edge detection and maps edges to directional ASCII characters
// Uses split constant buffers: b0 for shared post-effects, b1 for filter-specific params

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// ============================================================================
// Constant Buffer 0: Shared Post-Effects (128 bytes)
// ============================================================================
cbuffer PostEffectsParams : register(b0)
{
    // Core (32 bytes)
    float2 PE_ViewportSize;
    float PE_Time;
    float PE_Scanlines;
    float PE_ScanlineIntensity;
    float PE_ScanlineSpacing;
    float PE_CrtCurvature;
    float PE_CrtAmount;

    // Effects (32 bytes)
    float PE_PhosphorGlow;
    float PE_PhosphorIntensity;
    float PE_Chromatic;
    float PE_ChromaticOffset;
    float PE_Vignette;
    float PE_VignetteIntensity;
    float PE_VignetteRadius;
    float PE_Noise;

    // More effects (32 bytes)
    float PE_NoiseAmount;
    float PE_Flicker;
    float PE_FlickerSpeed;
    float _pe_pad1;
    float4 _pe_pad2;

    // Reserved (32 bytes)
    float4 _pe_reserved1;
    float4 _pe_reserved2;
};

// ============================================================================
// Constant Buffer 1: Edge ASCII Filter Parameters (128 bytes)
// ============================================================================
cbuffer EdgeASCIIFilterParams : register(b1)
{
    // Core (32 bytes)
    float2 MousePosition;
    float LayoutMode;       // 0=fullscreen, 1=circle, 2=rectangle
    float Radius;
    float EdgeSoftness;
    float ShapeFeather;
    float RectWidth;
    float RectHeight;

    // Cell settings (16 bytes)
    float CellWidth;
    float CellHeight;
    float CharCount;
    float _pad1;

    // Edge detection (32 bytes)
    float EdgeThreshold;     // 0.05-0.5: minimum edge strength to show
    float LineThickness;     // 1-3: edge detection kernel size
    float ShowCorners;       // bool: use + for intersections
    float FillBackground;    // bool: show faint original behind
    float BackgroundOpacity; // 0-0.5: opacity of background
    float EdgeBrightness;    // 0.5-2.0: edge character brightness
    float _pad2;
    float _pad3;

    // Colors (32 bytes)
    float4 EdgeColor;        // Edge character color
    float4 BackgroundColor;  // Background color

    // Brightness (16 bytes)
    float Brightness;
    float Contrast;
    float _pad4;
    float _pad5;
};

// Textures and samplers
Texture2D<float4> ScreenTexture : register(t0);
SamplerState LinearSampler : register(s0);

// ============================================================================
// Helper Functions
// ============================================================================

float hash(float2 p)
{
    float h = dot(p, float2(127.1, 311.7));
    return frac(sin(h) * 43758.5453123);
}

float noise2D(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    float a = hash(i);
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

float GetLuminance(float3 color)
{
    return dot(color, float3(0.299, 0.587, 0.114));
}

// ============================================================================
// Layout Functions
// ============================================================================

float CalculateEffectMask(float2 screenPos)
{
    if (LayoutMode < 0.5)
        return 1.0;

    float2 delta = screenPos - MousePosition;

    if (LayoutMode < 1.5)
    {
        // Circle mode
        float dist = length(delta);
        float softness = max(EdgeSoftness, 1.0);
        return 1.0 - smoothstep(Radius - softness, Radius + ShapeFeather, dist);
    }
    else
    {
        // Rectangle mode
        float2 halfSize = float2(RectWidth, RectHeight) * 0.5;
        float2 d = abs(delta) - halfSize;
        float softness = max(EdgeSoftness, 1.0);
        float dist = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
        return 1.0 - smoothstep(-softness, ShapeFeather, dist);
    }
}

// ============================================================================
// Post-Effect Functions (using PE_ prefix for shared params)
// ============================================================================

float2 ApplyCRTCurvature(float2 uv)
{
    if (PE_CrtCurvature < 0.5)
        return uv;

    float2 centered = uv * 2.0 - 1.0;
    float r2 = dot(centered, centered);
    centered *= 1.0 + PE_CrtAmount * r2;
    return centered * 0.5 + 0.5;
}

float ApplyScanlines(float2 screenPos, float value)
{
    if (PE_Scanlines < 0.5)
        return value;

    float spacing = max(PE_ScanlineSpacing, 1.0);
    float rowPos = fmod(screenPos.y, spacing * 2.0);
    float scanEffect = step(spacing, rowPos);

    return lerp(value, value * (1.0 - PE_ScanlineIntensity), scanEffect);
}

float3 ApplyVignette(float2 uv, float3 color)
{
    if (PE_Vignette < 0.5)
        return color;

    float2 centered = uv * 2.0 - 1.0;
    float dist = length(centered);
    float vignetteFactor = smoothstep(PE_VignetteRadius, PE_VignetteRadius + 0.5, dist);

    return color * (1.0 - vignetteFactor * PE_VignetteIntensity);
}

float3 ApplyNoise(float2 screenPos, float3 color)
{
    if (PE_Noise < 0.5)
        return color;

    float n = noise2D(screenPos * 0.1 + PE_Time * 10.0);
    n = (n - 0.5) * 2.0 * PE_NoiseAmount;

    return saturate(color + n);
}

float ApplyFlicker(float value)
{
    if (PE_Flicker < 0.5)
        return value;

    float flicker = 0.95 + 0.05 * sin(PE_Time * PE_FlickerSpeed * 60.0);
    flicker *= 0.98 + 0.02 * sin(PE_Time * PE_FlickerSpeed * 17.3);

    return value * flicker;
}

float3 ApplyPhosphorGlow(float charAlpha, float3 color)
{
    if (PE_PhosphorGlow < 0.5)
        return color;

    float glowFactor = charAlpha * PE_PhosphorIntensity;
    float3 glowColor = color * (1.0 + glowFactor);

    return lerp(color, glowColor, charAlpha);
}

// ============================================================================
// Edge Detection Functions
// ============================================================================

// Sobel operators for edge detection
static const float3x3 SobelX = float3x3(
    -1.0, 0.0, 1.0,
    -2.0, 0.0, 2.0,
    -1.0, 0.0, 1.0
);

static const float3x3 SobelY = float3x3(
    -1.0, -2.0, -1.0,
     0.0,  0.0,  0.0,
     1.0,  2.0,  1.0
);

// Sample a 3x3 neighborhood and apply Sobel edge detection
void SobelEdgeDetection(float2 uv, out float edgeMagnitude, out float edgeAngle)
{
    float thickness = max(1.0, LineThickness);
    float2 texelSize = thickness / PE_ViewportSize;

    // Sample 3x3 neighborhood
    float samples[9];
    samples[0] = GetLuminance(ScreenTexture.SampleLevel(LinearSampler, uv + float2(-texelSize.x, -texelSize.y), 0).rgb);
    samples[1] = GetLuminance(ScreenTexture.SampleLevel(LinearSampler, uv + float2(0.0, -texelSize.y), 0).rgb);
    samples[2] = GetLuminance(ScreenTexture.SampleLevel(LinearSampler, uv + float2(texelSize.x, -texelSize.y), 0).rgb);
    samples[3] = GetLuminance(ScreenTexture.SampleLevel(LinearSampler, uv + float2(-texelSize.x, 0.0), 0).rgb);
    samples[4] = GetLuminance(ScreenTexture.SampleLevel(LinearSampler, uv, 0).rgb);
    samples[5] = GetLuminance(ScreenTexture.SampleLevel(LinearSampler, uv + float2(texelSize.x, 0.0), 0).rgb);
    samples[6] = GetLuminance(ScreenTexture.SampleLevel(LinearSampler, uv + float2(-texelSize.x, texelSize.y), 0).rgb);
    samples[7] = GetLuminance(ScreenTexture.SampleLevel(LinearSampler, uv + float2(0.0, texelSize.y), 0).rgb);
    samples[8] = GetLuminance(ScreenTexture.SampleLevel(LinearSampler, uv + float2(texelSize.x, texelSize.y), 0).rgb);

    // Apply Sobel operators
    float Gx = 0.0;
    float Gy = 0.0;

    for (int i = 0; i < 3; i++)
    {
        for (int j = 0; j < 3; j++)
        {
            int idx = i * 3 + j;
            Gx += samples[idx] * SobelX[i][j];
            Gy += samples[idx] * SobelY[i][j];
        }
    }

    // Calculate magnitude and angle
    edgeMagnitude = sqrt(Gx * Gx + Gy * Gy);
    edgeAngle = atan2(Gy, Gx);
}

// Constants
static const float PI = 3.14159265359;

// Map edge angle to character index
// Character atlas: 0=space, 1=horizontal (-), 2=vertical (|), 3=forward (/), 4=backslash, 5=corner (+)
float MapAngleToCharacter(float angle, float edgeMagnitude, float2 uv)
{
    // If edge is weak, return space
    if (edgeMagnitude < EdgeThreshold)
        return 0.0;

    // Check for corners if enabled
    if (ShowCorners > 0.5)
    {
        // Sample additional points to detect intersections
        float thickness = max(1.0, LineThickness);
        float2 texelSize = thickness / PE_ViewportSize;

        float edgeMag1, angle1;
        float edgeMag2, angle2;
        float edgeMag3, angle3;
        float edgeMag4, angle4;

        SobelEdgeDetection(uv + float2(-texelSize.x * 2, 0), edgeMag1, angle1);
        SobelEdgeDetection(uv + float2(texelSize.x * 2, 0), edgeMag2, angle2);
        SobelEdgeDetection(uv + float2(0, -texelSize.y * 2), edgeMag3, angle3);
        SobelEdgeDetection(uv + float2(0, texelSize.y * 2), edgeMag4, angle4);

        // Count strong edges in different directions
        int strongEdges = 0;
        if (edgeMag1 > EdgeThreshold) strongEdges++;
        if (edgeMag2 > EdgeThreshold) strongEdges++;
        if (edgeMag3 > EdgeThreshold) strongEdges++;
        if (edgeMag4 > EdgeThreshold) strongEdges++;

        // If multiple strong edges, use corner character
        if (strongEdges >= 2)
            return 5.0; // +
    }

    // Normalize angle to [0, 2*PI]
    float normalizedAngle = angle;
    if (normalizedAngle < 0.0)
        normalizedAngle += 2.0 * PI;

    // Map angle to 8 directions, then to 4 primary directions
    float sector = normalizedAngle / (PI / 4.0);

    // Map sectors to characters:
    // 0-1 (0-45 deg, right): horizontal -
    // 1-3 (45-135 deg, up-right to up-left): forward /
    // 3-5 (135-225 deg, left): horizontal -
    // 5-7 (225-315 deg, down-left to down-right): backslash
    // 7-8 (315-360 deg, right): horizontal -

    if (sector < 1.0 || sector >= 7.0)
        return 1.0; // horizontal -
    else if (sector >= 1.0 && sector < 3.0)
        return 3.0; // forward /
    else if (sector >= 3.0 && sector < 5.0)
        return 1.0; // horizontal -
    else // sector >= 5.0 && sector < 7.0
        return 4.0; // backslash
}

// Alternative simplified angle mapping (more vertical/horizontal biased)
float MapAngleToCharacterSimple(float angle, float edgeMagnitude)
{
    if (edgeMagnitude < EdgeThreshold)
        return 0.0;
    float normalizedAngle = angle;
    if (normalizedAngle < 0.0)
        normalizedAngle += 2.0 * PI;

    float degrees = normalizedAngle * 180.0 / PI;

    // Simplified mapping favoring horizontal and vertical
    if ((degrees < 22.5) || (degrees >= 157.5 && degrees < 202.5) || (degrees >= 337.5))
        return 1.0; // horizontal -
    else if ((degrees >= 67.5 && degrees < 112.5) || (degrees >= 247.5 && degrees < 292.5))
        return 2.0; // vertical |
    else if ((degrees >= 22.5 && degrees < 67.5) || (degrees >= 202.5 && degrees < 247.5))
        return 3.0; // forward /
    else
        return 4.0; // backslash
}

// Draw procedural line shapes based on character index
// 0=space, 1=horizontal (-), 2=vertical (|), 3=forward (/), 4=backslash, 5=corner (+)
float DrawProceduralChar(float charIndex, float2 cellUV)
{
    // No edge - return empty
    if (charIndex < 0.5)
        return 0.0;

    // Center the UV coordinates (-0.5 to 0.5)
    float2 centered = cellUV - 0.5;
    float lineWidth = 0.15;

    // Horizontal line (-)
    if (charIndex < 1.5)
    {
        return smoothstep(lineWidth, lineWidth * 0.5, abs(centered.y)) *
               smoothstep(0.45, 0.35, abs(centered.x));
    }

    // Vertical line (|)
    if (charIndex < 2.5)
    {
        return smoothstep(lineWidth, lineWidth * 0.5, abs(centered.x)) *
               smoothstep(0.45, 0.35, abs(centered.y));
    }

    // Forward diagonal (/)
    if (charIndex < 3.5)
    {
        float diag = centered.x + centered.y;
        return smoothstep(lineWidth * 1.4, lineWidth * 0.7, abs(diag)) *
               smoothstep(0.6, 0.4, abs(centered.x) + abs(centered.y));
    }

    // Back diagonal (backslash)
    if (charIndex < 4.5)
    {
        float diag = centered.x - centered.y;
        return smoothstep(lineWidth * 1.4, lineWidth * 0.7, abs(diag)) *
               smoothstep(0.6, 0.4, abs(centered.x) + abs(centered.y));
    }

    // Corner/cross (+)
    float horiz = smoothstep(lineWidth, lineWidth * 0.5, abs(centered.y)) *
                  smoothstep(0.45, 0.35, abs(centered.x));
    float vert = smoothstep(lineWidth, lineWidth * 0.5, abs(centered.x)) *
                 smoothstep(0.45, 0.35, abs(centered.y));
    return max(horiz, vert);
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

    // Apply CRT curvature
    float2 crtUV = ApplyCRTCurvature(uv);

    if (crtUV.x < 0.0 || crtUV.x > 1.0 || crtUV.y < 0.0 || crtUV.y > 1.0)
    {
        return float4(0.0, 0.0, 0.0, 1.0);
    }

    float2 screenPos = crtUV * PE_ViewportSize;

    // Calculate effect mask
    float effectMask = CalculateEffectMask(screenPos);

    if (effectMask < 0.01)
    {
        return ScreenTexture.SampleLevel(LinearSampler, uv, 0);
    }

    // Calculate cell
    float2 cellSize = float2(CellWidth, CellHeight);
    float2 cellIndex = floor(screenPos / cellSize);
    float2 cellUV = frac(screenPos / cellSize);
    float2 cellCenter = (cellIndex + 0.5) * cellSize / PE_ViewportSize;

    // Perform edge detection at cell center
    float edgeMagnitude, edgeAngle;
    SobelEdgeDetection(cellCenter, edgeMagnitude, edgeAngle);

    // Map angle to character index
    float charIndex = MapAngleToCharacterSimple(edgeAngle, edgeMagnitude);

    // Draw procedural character shape
    float charAlpha = DrawProceduralChar(charIndex, cellUV);

    // Adjust character alpha based on edge strength
    float normalizedEdge = saturate(edgeMagnitude / EdgeThreshold);
    charAlpha *= normalizedEdge * EdgeBrightness;

    // Apply flicker and scanlines to alpha
    charAlpha = ApplyFlicker(charAlpha);
    charAlpha = ApplyScanlines(screenPos, charAlpha);

    // Get original color
    float3 originalColor = ScreenTexture.SampleLevel(LinearSampler, crtUV, 0).rgb;

    // Determine final color
    float3 finalColor;

    if (FillBackground > 0.5)
    {
        // Blend edge characters with faint background
        float3 background = originalColor * BackgroundOpacity;
        float3 edgeChar = EdgeColor.rgb * charAlpha;
        finalColor = lerp(background, edgeChar, charAlpha);
    }
    else
    {
        // Use solid background color
        finalColor = lerp(BackgroundColor.rgb, EdgeColor.rgb, charAlpha);
    }

    // Apply brightness and contrast adjustments
    finalColor = (finalColor - 0.5) * Contrast + 0.5 + Brightness;
    finalColor = saturate(finalColor);

    // Apply post-effects
    finalColor = ApplyPhosphorGlow(charAlpha, finalColor);
    finalColor = ApplyVignette(crtUV, finalColor);
    finalColor = ApplyNoise(screenPos + PE_Time * 100.0, finalColor);

    // Blend with original based on effect mask
    finalColor = lerp(originalColor, finalColor, effectMask);

    return float4(finalColor, 1.0);
}
