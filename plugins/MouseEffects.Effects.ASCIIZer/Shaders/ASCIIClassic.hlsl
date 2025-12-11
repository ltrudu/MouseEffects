// ASCII Classic Shader
// Renders the screen as ASCII art using a font texture atlas
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
// Constant Buffer 1: ASCII Classic Filter Parameters (192 bytes)
// ============================================================================
cbuffer ASCIIClassicParams : register(b1)
{
    // Core (32 bytes)
    float2 MousePosition;
    float LayoutMode;           // 0=Fullscreen, 1=Circle, 2=Rectangle
    float Radius;
    float EdgeSoftness;
    float ShapeFeather;
    float RectWidth;
    float RectHeight;

    // Cell (16 bytes)
    float CellWidth;
    float CellHeight;
    float CharCount;
    float SampleMode;           // 0=Center, 1=Average, 2=Weighted

    // Color mode (48 bytes)
    float ColorMode;            // 0=Colored, 1=Monochrome, 2=Palette
    float Saturation;
    float QuantizeLevels;
    float PreserveLuminance;
    float4 ForegroundColor;
    float4 BackgroundColor;

    // Brightness (16 bytes)
    float Brightness;
    float Contrast;
    float Gamma;
    float Invert;

    // Character rendering (32 bytes)
    float Antialiasing;
    float CharShadow;
    float2 ShadowOffset;
    float4 ShadowColor;

    // Grid & glow (48 bytes)
    float GlowOnBright;
    float GlowThreshold;
    float GlowRadius;
    float GridLines;
    float GridThickness;
    float InnerGlow;
    float InnerGlowSize;
    float _pad1;
    float4 GridColor;
    float4 InnerGlowColor;
};

// Textures and samplers
Texture2D<float4> ScreenTexture : register(t0);
Texture2D<float4> FontAtlas : register(t1);
SamplerState LinearSampler : register(s0);
SamplerState PointSampler : register(s1);

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

float3 ApplyChromaticAberration(float2 uv, float3 baseColor)
{
    if (PE_Chromatic < 0.5)
        return baseColor;

    float2 centered = uv - 0.5;
    float2 offset = centered * PE_ChromaticOffset / PE_ViewportSize;

    float r = ScreenTexture.SampleLevel(LinearSampler, uv + offset, 0).r;
    float g = baseColor.g;
    float b = ScreenTexture.SampleLevel(LinearSampler, uv - offset, 0).b;

    return float3(r, g, b);
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
// Filter-Specific Functions (using filter params)
// ============================================================================

float4 SampleCellColor(float2 cellCenter)
{
    if (SampleMode < 0.5)
    {
        return ScreenTexture.SampleLevel(LinearSampler, cellCenter, 0);
    }
    else if (SampleMode < 1.5)
    {
        float2 offset = float2(CellWidth * 0.25, CellHeight * 0.25) / PE_ViewportSize;
        float4 c1 = ScreenTexture.SampleLevel(LinearSampler, cellCenter + float2(-offset.x, -offset.y), 0);
        float4 c2 = ScreenTexture.SampleLevel(LinearSampler, cellCenter + float2( offset.x, -offset.y), 0);
        float4 c3 = ScreenTexture.SampleLevel(LinearSampler, cellCenter + float2(-offset.x,  offset.y), 0);
        float4 c4 = ScreenTexture.SampleLevel(LinearSampler, cellCenter + float2( offset.x,  offset.y), 0);
        return (c1 + c2 + c3 + c4) * 0.25;
    }
    else
    {
        float2 offset = float2(CellWidth * 0.3, CellHeight * 0.3) / PE_ViewportSize;
        float4 center = ScreenTexture.SampleLevel(LinearSampler, cellCenter, 0);
        float4 c1 = ScreenTexture.SampleLevel(LinearSampler, cellCenter + float2(-offset.x, -offset.y), 0);
        float4 c2 = ScreenTexture.SampleLevel(LinearSampler, cellCenter + float2( offset.x, -offset.y), 0);
        float4 c3 = ScreenTexture.SampleLevel(LinearSampler, cellCenter + float2(-offset.x,  offset.y), 0);
        float4 c4 = ScreenTexture.SampleLevel(LinearSampler, cellCenter + float2( offset.x,  offset.y), 0);
        return center * 0.5 + (c1 + c2 + c3 + c4) * 0.125;
    }
}

float AdjustLuminance(float luma)
{
    luma = pow(saturate(luma), 1.0 / max(Gamma, 0.1));
    luma = (luma - 0.5) * Contrast + 0.5;
    luma = luma + Brightness;
    if (Invert > 0.5)
        luma = 1.0 - luma;
    return saturate(luma);
}

float3 AdjustSaturation(float3 color, float sat)
{
    float luma = GetLuminance(color);
    return lerp(float3(luma, luma, luma), color, sat);
}

float3 QuantizeColor(float3 color, float levels)
{
    levels = max(levels, 2.0);
    return floor(color * levels) / (levels - 1.0);
}

float SampleCharacter(float charIndex, float2 cellUV)
{
    float atlasU = (charIndex + cellUV.x) / CharCount;
    float atlasV = cellUV.y;

    if (Antialiasing < 0.5)
    {
        return FontAtlas.SampleLevel(PointSampler, float2(atlasU, atlasV), 0).r;
    }
    else if (Antialiasing < 1.5)
    {
        return FontAtlas.SampleLevel(LinearSampler, float2(atlasU, atlasV), 0).r;
    }
    else
    {
        float2 texelSize = float2(1.0 / (CharCount * CellWidth), 1.0 / CellHeight);
        float2 offset = texelSize * 0.25;

        float s1 = FontAtlas.SampleLevel(LinearSampler, float2(atlasU - offset.x, atlasV - offset.y), 0).r;
        float s2 = FontAtlas.SampleLevel(LinearSampler, float2(atlasU + offset.x, atlasV - offset.y), 0).r;
        float s3 = FontAtlas.SampleLevel(LinearSampler, float2(atlasU - offset.x, atlasV + offset.y), 0).r;
        float s4 = FontAtlas.SampleLevel(LinearSampler, float2(atlasU + offset.x, atlasV + offset.y), 0).r;

        return (s1 + s2 + s3 + s4) * 0.25;
    }
}

float SampleCharacterWithShadow(float charIndex, float2 cellUV, float2 cellSize)
{
    float mainChar = SampleCharacter(charIndex, cellUV);

    if (CharShadow < 0.5)
        return mainChar;

    float2 shadowUV = cellUV - ShadowOffset / cellSize;
    float shadowChar = 0.0;

    if (shadowUV.x >= 0.0 && shadowUV.x <= 1.0 && shadowUV.y >= 0.0 && shadowUV.y <= 1.0)
    {
        shadowChar = SampleCharacter(charIndex, shadowUV);
    }

    return mainChar + shadowChar * ShadowColor.a * (1.0 - mainChar);
}

float3 ApplyBrightGlow(float charAlpha, float3 color, float luma)
{
    if (GlowOnBright < 0.5 || luma < GlowThreshold)
        return color;

    float glowStrength = (luma - GlowThreshold) / (1.0 - GlowThreshold);
    glowStrength *= charAlpha;

    return color * (1.0 + glowStrength * 0.5);
}

float3 ApplyGridLines(float2 cellUV, float3 color)
{
    if (GridLines < 0.5)
        return color;

    float thickness = GridThickness / min(CellWidth, CellHeight);

    float gridX = step(cellUV.x, thickness) + step(1.0 - thickness, cellUV.x);
    float gridY = step(cellUV.y, thickness) + step(1.0 - thickness, cellUV.y);
    float grid = saturate(gridX + gridY);

    return lerp(color, GridColor.rgb, grid * GridColor.a);
}

float3 ApplyInnerGlow(float effectMask, float3 color)
{
    if (InnerGlow < 0.5)
        return color;

    float glowMask = smoothstep(0.0, InnerGlowSize / 100.0, 1.0 - effectMask);
    glowMask *= effectMask;

    return lerp(color, InnerGlowColor.rgb, glowMask * InnerGlowColor.a);
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

    // Sample cell color
    float4 cellColor = SampleCellColor(cellCenter);

    // Apply chromatic aberration
    cellColor.rgb = ApplyChromaticAberration(cellCenter, cellColor.rgb);

    // Calculate luminance
    float luma = GetLuminance(cellColor.rgb);
    float adjustedLuma = AdjustLuminance(luma);

    // Map to character
    float charIndex = floor(adjustedLuma * (CharCount - 0.001));
    charIndex = clamp(charIndex, 0.0, CharCount - 1.0);

    // Sample character
    float charAlpha = SampleCharacterWithShadow(charIndex, cellUV, cellSize);

    // Apply flicker and scanlines to alpha
    charAlpha = ApplyFlicker(charAlpha);
    charAlpha = ApplyScanlines(screenPos, charAlpha);

    // Determine color based on mode
    float3 finalColor;

    if (ColorMode < 0.5)
    {
        // Colored mode
        float3 adjustedColor = AdjustSaturation(cellColor.rgb, Saturation);
        if (QuantizeLevels < 255.5)
        {
            adjustedColor = QuantizeColor(adjustedColor, QuantizeLevels);
        }
        finalColor = lerp(adjustedColor * 0.1, adjustedColor, charAlpha);
    }
    else if (ColorMode < 1.5)
    {
        // Monochrome mode
        finalColor = lerp(BackgroundColor.rgb, ForegroundColor.rgb, charAlpha);
    }
    else
    {
        // Palette mode
        float3 adjustedColor = AdjustSaturation(cellColor.rgb, Saturation);
        adjustedColor = QuantizeColor(adjustedColor, QuantizeLevels);

        if (PreserveLuminance > 0.5)
        {
            float newLuma = GetLuminance(adjustedColor);
            if (newLuma > 0.001)
            {
                adjustedColor = adjustedColor * (luma / newLuma);
            }
        }

        finalColor = lerp(BackgroundColor.rgb, adjustedColor, charAlpha);
    }

    // Apply post-effects
    finalColor = ApplyPhosphorGlow(charAlpha, finalColor);
    finalColor = ApplyBrightGlow(charAlpha, finalColor, adjustedLuma);
    finalColor = ApplyGridLines(cellUV, finalColor);
    finalColor = ApplyVignette(crtUV, finalColor);
    finalColor = ApplyNoise(screenPos + PE_Time * 100.0, finalColor);
    finalColor = ApplyInnerGlow(effectMask, finalColor);

    // Blend with original
    float3 original = ScreenTexture.SampleLevel(LinearSampler, uv, 0).rgb;
    finalColor = lerp(original, finalColor, effectMask);

    return float4(finalColor, 1.0);
}
