// Dot Matrix Shader
// Renders the screen as an LED display with circular or shaped dots
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
// Constant Buffer 1: Dot Matrix Filter Parameters (128 bytes)
// ============================================================================
cbuffer DotMatrixParams : register(b1)
{
    // Core (32 bytes)
    float2 MousePosition;
    float LayoutMode;           // 0=Fullscreen, 1=Circle, 2=Rectangle
    float Radius;
    float EdgeSoftness;
    float ShapeFeather;
    float RectWidth;
    float RectHeight;

    // Dot settings (32 bytes)
    float DotSize;              // 0.3-0.9 - max dot radius as fraction of cell
    float DotSpacing;           // 1-4 px - gap between dots
    float CellSize;             // Cell size in pixels
    float LedShape;             // 0=circle, 1=square, 2=diamond
    float OffBrightness;        // 0-0.2 - brightness of "off" LEDs
    float RgbMode;              // 0=single, 1=RGB sub-pixels
    float ColorMode;            // 0=Colored, 1=Monochrome
    float _pad1;

    // Colors (32 bytes)
    float4 ForegroundColor;
    float4 BackgroundColor;

    // Brightness (32 bytes)
    float Brightness;
    float Contrast;
    float Gamma;
    float Saturation;
    float4 _pad2;
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
// Post-Effect Functions
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

float3 ApplyPhosphorGlow(float intensity, float3 color)
{
    if (PE_PhosphorGlow < 0.5)
        return color;

    float glowFactor = intensity * PE_PhosphorIntensity;
    float3 glowColor = color * (1.0 + glowFactor);

    return lerp(color, glowColor, intensity);
}

// ============================================================================
// Dot Matrix Functions
// ============================================================================

// Calculate dot shape mask
float CalculateDotMask(float2 cellUV, float brightness)
{
    // Center of cell
    float2 centered = cellUV - 0.5;
    float dotRadius = DotSize * 0.5 * brightness;

    if (LedShape < 0.5)
    {
        // Circle
        float dist = length(centered);
        return smoothstep(dotRadius + 0.02, dotRadius - 0.02, dist);
    }
    else if (LedShape < 1.5)
    {
        // Square
        float2 absCentered = abs(centered);
        float maxDist = max(absCentered.x, absCentered.y);
        return smoothstep(dotRadius + 0.02, dotRadius - 0.02, maxDist);
    }
    else
    {
        // Diamond
        float dist = abs(centered.x) + abs(centered.y);
        return smoothstep(dotRadius + 0.02, dotRadius - 0.02, dist);
    }
}

// Calculate RGB sub-pixel mask (for RGB mode)
float3 CalculateRGBSubPixelMask(float2 cellUV, float brightness)
{
    float3 result = float3(0.0, 0.0, 0.0);

    // Divide cell into 3 horizontal sub-pixels
    float subPixelWidth = 1.0 / 3.0;
    float dotRadius = DotSize * 0.5 * brightness * 0.9; // Slightly smaller for RGB mode

    // Red sub-pixel
    float2 redCenter = float2(subPixelWidth * 0.5, 0.5);
    float redDist = length(cellUV - redCenter);
    result.r = smoothstep(dotRadius + 0.02, dotRadius - 0.02, redDist);

    // Green sub-pixel
    float2 greenCenter = float2(subPixelWidth * 1.5, 0.5);
    float greenDist = length(cellUV - greenCenter);
    result.g = smoothstep(dotRadius + 0.02, dotRadius - 0.02, greenDist);

    // Blue sub-pixel
    float2 blueCenter = float2(subPixelWidth * 2.5, 0.5);
    float blueDist = length(cellUV - blueCenter);
    result.b = smoothstep(dotRadius + 0.02, dotRadius - 0.02, blueDist);

    return result;
}

float3 AdjustColor(float3 color)
{
    // Apply gamma
    color = pow(saturate(color), 1.0 / max(Gamma, 0.1));

    // Apply contrast
    color = (color - 0.5) * Contrast + 0.5;

    // Apply brightness
    color = color + Brightness;

    // Apply saturation
    float luma = GetLuminance(color);
    color = lerp(float3(luma, luma, luma), color, Saturation);

    return saturate(color);
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
    float cellSizeWithSpacing = CellSize + DotSpacing;
    float2 cellIndex = floor(screenPos / cellSizeWithSpacing);
    float2 cellUV = frac(screenPos / cellSizeWithSpacing);

    // Adjust UV to account for spacing (dots don't fill the entire cell)
    float dotAreaRatio = CellSize / cellSizeWithSpacing;
    float2 dotUV = cellUV / dotAreaRatio;

    // Sample color at cell center
    float2 cellCenter = (cellIndex + 0.5) * cellSizeWithSpacing / PE_ViewportSize;
    float4 cellColor = ScreenTexture.SampleLevel(LinearSampler, cellCenter, 0);

    // Adjust color
    cellColor.rgb = AdjustColor(cellColor.rgb);

    float luma = GetLuminance(cellColor.rgb);

    // Apply flicker to brightness
    luma = ApplyFlicker(luma);

    // Calculate dot mask
    float3 finalColor;

    if (dotUV.x <= 1.0 && dotUV.y <= 1.0)
    {
        if (RgbMode > 0.5)
        {
            // RGB sub-pixel mode
            float3 subPixelMask = CalculateRGBSubPixelMask(dotUV, luma);

            if (ColorMode < 0.5)
            {
                // Colored mode - use actual colors
                finalColor = cellColor.rgb * subPixelMask;
            }
            else
            {
                // Monochrome mode
                finalColor = ForegroundColor.rgb * subPixelMask * luma;
            }

            // Add off-brightness for inactive pixels
            float3 offColor = BackgroundColor.rgb * OffBrightness;
            finalColor = max(finalColor, offColor * (1.0 - max(subPixelMask.r, max(subPixelMask.g, subPixelMask.b))));
        }
        else
        {
            // Single dot mode
            float dotMask = CalculateDotMask(dotUV, max(luma, 0.1));

            if (ColorMode < 0.5)
            {
                // Colored mode
                finalColor = lerp(BackgroundColor.rgb * OffBrightness, cellColor.rgb, dotMask);
            }
            else
            {
                // Monochrome mode
                float3 litColor = ForegroundColor.rgb * luma;
                float3 offColor = BackgroundColor.rgb * OffBrightness;
                finalColor = lerp(offColor, litColor, dotMask);
            }
        }
    }
    else
    {
        // In spacing area - show background
        finalColor = BackgroundColor.rgb * OffBrightness;
    }

    // Apply scanlines
    float scanlineFactor = ApplyScanlines(screenPos, 1.0);
    finalColor *= scanlineFactor;

    // Apply post-effects
    finalColor = ApplyPhosphorGlow(luma, finalColor);
    finalColor = ApplyVignette(crtUV, finalColor);
    finalColor = ApplyNoise(screenPos + PE_Time * 100.0, finalColor);

    // Blend with original
    float3 original = ScreenTexture.SampleLevel(LinearSampler, uv, 0).rgb;
    finalColor = lerp(original, finalColor, effectMask);

    return float4(finalColor, 1.0);
}
