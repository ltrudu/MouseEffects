// ASCII Classic Shader
// Renders the screen as ASCII art using a font texture atlas

struct PSInput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// Constant buffer - must match C# ASCIIClassicParams struct (320 bytes)
cbuffer ASCIIParams : register(b0)
{
    // Core (32 bytes)
    float2 MousePosition;
    float2 ViewportSize;
    float Time;
    float LayoutMode;       // 0=Fullscreen, 1=Circle, 2=Rectangle
    float Radius;
    float EdgeSoftness;

    // Shape (16 bytes)
    float RectWidth;
    float RectHeight;
    float ShapeFeather;
    float _pad1;

    // Cell (16 bytes)
    float CellWidth;
    float CellHeight;
    float CharCount;
    float SampleMode;       // 0=Center, 1=Average, 2=Weighted

    // Color mode (48 bytes)
    float ColorMode;        // 0=Colored, 1=Monochrome, 2=Palette
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

    // Visual effects flags (16 bytes)
    float Scanlines;
    float ScanlineIntensity;
    float ScanlineSpacing;
    float CrtCurvature;

    // CRT effects (16 bytes)
    float CrtAmount;
    float PhosphorGlow;
    float PhosphorRadius;
    float PhosphorIntensity;

    // More effects (16 bytes)
    float Chromatic;
    float ChromaticOffset;
    float Vignette;
    float VignetteIntensity;

    // Even more (16 bytes)
    float VignetteRadius;
    float Noise;
    float NoiseAmount;
    float Flicker;

    // Character rendering (32 bytes)
    float FlickerSpeed;
    float Antialiasing;
    float CharShadow;
    float CharOutline;
    float4 ShadowColor;

    // Outline & glow (32 bytes)
    float2 ShadowOffset;
    float OutlineThickness;
    float GlowOnBright;
    float4 OutlineColor;

    // Grid (32 bytes)
    float GlowThreshold;
    float GlowRadius;
    float GridLines;
    float GridThickness;
    float4 GridColor;

    // Inner glow (32 bytes)
    float InnerGlow;
    float InnerGlowSize;
    float2 _pad2;
    float4 InnerGlowColor;
};

// Textures and samplers
Texture2D<float4> ScreenTexture : register(t0);
Texture2D<float4> FontAtlas : register(t1);
SamplerState LinearSampler : register(s0);
SamplerState PointSampler : register(s1);

// Constants
static const float PI = 3.14159265359;

// Random number generation for noise
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

// Calculate effect mask based on layout mode
float CalculateEffectMask(float2 screenPos)
{
    if (LayoutMode < 0.5)
    {
        // Fullscreen
        return 1.0;
    }

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

        // Rounded rectangle SDF
        float dist = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
        return 1.0 - smoothstep(-softness, ShapeFeather, dist);
    }
}

// Apply CRT curvature distortion
float2 ApplyCRTCurvature(float2 uv)
{
    if (CrtCurvature < 0.5)
        return uv;

    // Center UV coordinates
    float2 centered = uv * 2.0 - 1.0;

    // Apply barrel distortion
    float r2 = dot(centered, centered);
    centered *= 1.0 + CrtAmount * r2;

    // Convert back to UV space
    return centered * 0.5 + 0.5;
}

// Sample cell color with different modes
float4 SampleCellColor(float2 cellCenter)
{
    if (SampleMode < 0.5)
    {
        // Center sampling
        return ScreenTexture.SampleLevel(LinearSampler, cellCenter, 0);
    }
    else if (SampleMode < 1.5)
    {
        // Average sampling - 4 samples
        float2 offset = float2(CellWidth * 0.25, CellHeight * 0.25) / ViewportSize;
        float4 c1 = ScreenTexture.SampleLevel(LinearSampler, cellCenter + float2(-offset.x, -offset.y), 0);
        float4 c2 = ScreenTexture.SampleLevel(LinearSampler, cellCenter + float2( offset.x, -offset.y), 0);
        float4 c3 = ScreenTexture.SampleLevel(LinearSampler, cellCenter + float2(-offset.x,  offset.y), 0);
        float4 c4 = ScreenTexture.SampleLevel(LinearSampler, cellCenter + float2( offset.x,  offset.y), 0);
        return (c1 + c2 + c3 + c4) * 0.25;
    }
    else
    {
        // Weighted center sampling - 5 samples with center weighted
        float2 offset = float2(CellWidth * 0.3, CellHeight * 0.3) / ViewportSize;
        float4 center = ScreenTexture.SampleLevel(LinearSampler, cellCenter, 0);
        float4 c1 = ScreenTexture.SampleLevel(LinearSampler, cellCenter + float2(-offset.x, -offset.y), 0);
        float4 c2 = ScreenTexture.SampleLevel(LinearSampler, cellCenter + float2( offset.x, -offset.y), 0);
        float4 c3 = ScreenTexture.SampleLevel(LinearSampler, cellCenter + float2(-offset.x,  offset.y), 0);
        float4 c4 = ScreenTexture.SampleLevel(LinearSampler, cellCenter + float2( offset.x,  offset.y), 0);
        return center * 0.5 + (c1 + c2 + c3 + c4) * 0.125;
    }
}

// Calculate luminance from RGB
float GetLuminance(float3 color)
{
    return dot(color, float3(0.299, 0.587, 0.114));
}

// Apply brightness, contrast, and gamma corrections
float AdjustLuminance(float luma)
{
    // Gamma correction
    luma = pow(saturate(luma), 1.0 / max(Gamma, 0.1));

    // Contrast (centered around 0.5)
    luma = (luma - 0.5) * Contrast + 0.5;

    // Brightness
    luma = luma + Brightness;

    // Invert if enabled
    if (Invert > 0.5)
        luma = 1.0 - luma;

    return saturate(luma);
}

// Apply color saturation
float3 AdjustSaturation(float3 color, float sat)
{
    float luma = GetLuminance(color);
    return lerp(float3(luma, luma, luma), color, sat);
}

// Quantize color to limited palette levels
float3 QuantizeColor(float3 color, float levels)
{
    levels = max(levels, 2.0);
    return floor(color * levels) / (levels - 1.0);
}

// Sample character from atlas with optional antialiasing
float SampleCharacter(float charIndex, float2 cellUV, float2 cellPixelPos)
{
    float atlasU = (charIndex + cellUV.x) / CharCount;
    float atlasV = cellUV.y;

    if (Antialiasing < 0.5)
    {
        // No antialiasing - point sampling
        return FontAtlas.SampleLevel(PointSampler, float2(atlasU, atlasV), 0).r;
    }
    else if (Antialiasing < 1.5)
    {
        // 2x antialiasing - bilinear
        return FontAtlas.SampleLevel(LinearSampler, float2(atlasU, atlasV), 0).r;
    }
    else
    {
        // 4x supersampling
        float2 texelSize = float2(1.0 / (CharCount * CellWidth), 1.0 / CellHeight);
        float2 offset = texelSize * 0.25;

        float s1 = FontAtlas.SampleLevel(LinearSampler, float2(atlasU - offset.x, atlasV - offset.y), 0).r;
        float s2 = FontAtlas.SampleLevel(LinearSampler, float2(atlasU + offset.x, atlasV - offset.y), 0).r;
        float s3 = FontAtlas.SampleLevel(LinearSampler, float2(atlasU - offset.x, atlasV + offset.y), 0).r;
        float s4 = FontAtlas.SampleLevel(LinearSampler, float2(atlasU + offset.x, atlasV + offset.y), 0).r;

        return (s1 + s2 + s3 + s4) * 0.25;
    }
}

// Sample character with shadow
float SampleCharacterWithShadow(float charIndex, float2 cellUV, float2 cellSize)
{
    float mainChar = SampleCharacter(charIndex, cellUV, float2(0, 0));

    if (CharShadow < 0.5)
        return mainChar;

    // Sample shadow (offset character)
    float2 shadowUV = cellUV - ShadowOffset / cellSize;
    float shadowChar = 0.0;

    if (shadowUV.x >= 0.0 && shadowUV.x <= 1.0 && shadowUV.y >= 0.0 && shadowUV.y <= 1.0)
    {
        shadowChar = SampleCharacter(charIndex, shadowUV, float2(0, 0));
    }

    return mainChar + shadowChar * ShadowColor.a * (1.0 - mainChar);
}

// Apply scanline effect
float ApplyScanlines(float2 screenPos, float value)
{
    if (Scanlines < 0.5)
        return value;

    float spacing = max(ScanlineSpacing, 1.0);
    float line = fmod(screenPos.y, spacing * 2.0);
    float scanline = step(spacing, line);

    return lerp(value, value * (1.0 - ScanlineIntensity), scanline);
}

// Apply vignette effect
float3 ApplyVignette(float2 uv, float3 color)
{
    if (Vignette < 0.5)
        return color;

    float2 centered = uv * 2.0 - 1.0;
    float dist = length(centered);
    float vignetteFactor = smoothstep(VignetteRadius, VignetteRadius + 0.5, dist);

    return color * (1.0 - vignetteFactor * VignetteIntensity);
}

// Apply chromatic aberration
float3 ApplyChromaticAberration(float2 uv, float3 baseColor)
{
    if (Chromatic < 0.5)
        return baseColor;

    float2 centered = uv - 0.5;
    float2 offset = centered * ChromaticOffset / ViewportSize;

    float r = ScreenTexture.SampleLevel(LinearSampler, uv + offset, 0).r;
    float g = baseColor.g;
    float b = ScreenTexture.SampleLevel(LinearSampler, uv - offset, 0).b;

    return float3(r, g, b);
}

// Apply noise/grain effect
float3 ApplyNoise(float2 screenPos, float3 color)
{
    if (Noise < 0.5)
        return color;

    float n = noise2D(screenPos * 0.1 + Time * 10.0);
    n = (n - 0.5) * 2.0 * NoiseAmount;

    return saturate(color + n);
}

// Apply flicker effect
float ApplyFlicker(float value)
{
    if (Flicker < 0.5)
        return value;

    float flicker = 0.95 + 0.05 * sin(Time * FlickerSpeed * 60.0);
    flicker *= 0.98 + 0.02 * sin(Time * FlickerSpeed * 17.3);

    return value * flicker;
}

// Apply phosphor glow effect (bloom on characters)
float3 ApplyPhosphorGlow(float charAlpha, float3 color, float luma)
{
    if (PhosphorGlow < 0.5)
        return color;

    float glowFactor = charAlpha * PhosphorIntensity;
    float3 glowColor = color * (1.0 + glowFactor);

    return lerp(color, glowColor, charAlpha);
}

// Apply glow on bright characters
float3 ApplyBrightGlow(float charAlpha, float3 color, float luma)
{
    if (GlowOnBright < 0.5 || luma < GlowThreshold)
        return color;

    float glowStrength = (luma - GlowThreshold) / (1.0 - GlowThreshold);
    glowStrength *= charAlpha;

    return color * (1.0 + glowStrength * 0.5);
}

// Draw grid lines between cells
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

// Apply inner glow effect for shape edges
float3 ApplyInnerGlow(float effectMask, float3 color)
{
    if (InnerGlow < 0.5)
        return color;

    // Glow at edges where mask transitions
    float glowMask = smoothstep(0.0, InnerGlowSize / 100.0, 1.0 - effectMask);
    glowMask *= effectMask; // Only inside the shape

    return lerp(color, InnerGlowColor.rgb, glowMask * InnerGlowColor.a);
}

// Vertex shader - generates fullscreen quad procedurally
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

// Main pixel shader
float4 PSMain(PSInput input) : SV_TARGET
{
    float2 uv = input.TexCoord;

    // Apply CRT curvature to UV
    float2 crtUV = ApplyCRTCurvature(uv);

    // Check if UV is out of bounds after curvature
    if (crtUV.x < 0.0 || crtUV.x > 1.0 || crtUV.y < 0.0 || crtUV.y > 1.0)
    {
        return float4(0.0, 0.0, 0.0, 1.0);
    }

    float2 screenPos = crtUV * ViewportSize;

    // Calculate effect mask
    float effectMask = CalculateEffectMask(screenPos);

    // Early out if outside effect area
    if (effectMask < 0.01)
    {
        return ScreenTexture.SampleLevel(LinearSampler, uv, 0);
    }

    // Calculate which cell this pixel belongs to
    float2 cellSize = float2(CellWidth, CellHeight);
    float2 cellIndex = floor(screenPos / cellSize);
    float2 cellUV = frac(screenPos / cellSize);

    // Calculate cell center in UV space
    float2 cellCenter = (cellIndex + 0.5) * cellSize / ViewportSize;

    // Sample cell color
    float4 cellColor = SampleCellColor(cellCenter);

    // Apply chromatic aberration to cell color
    cellColor.rgb = ApplyChromaticAberration(cellCenter, cellColor.rgb);

    // Calculate luminance
    float luma = GetLuminance(cellColor.rgb);
    float adjustedLuma = AdjustLuminance(luma);

    // Map luminance to character index
    float charIndex = floor(adjustedLuma * (CharCount - 0.001));
    charIndex = clamp(charIndex, 0.0, CharCount - 1.0);

    // Sample character from atlas
    float charAlpha = SampleCharacterWithShadow(charIndex, cellUV, cellSize);

    // Apply flicker to character
    charAlpha = ApplyFlicker(charAlpha);

    // Apply scanlines
    charAlpha = ApplyScanlines(screenPos, charAlpha);

    // Determine final color based on color mode
    float3 finalColor;

    if (ColorMode < 0.5)
    {
        // Colored mode - use cell color
        float3 adjustedColor = AdjustSaturation(cellColor.rgb, Saturation);

        if (QuantizeLevels < 255.5)
        {
            adjustedColor = QuantizeColor(adjustedColor, QuantizeLevels);
        }

        // Darken background slightly, full color for character
        finalColor = lerp(adjustedColor * 0.1, adjustedColor, charAlpha);
    }
    else if (ColorMode < 1.5)
    {
        // Monochrome mode
        finalColor = lerp(BackgroundColor.rgb, ForegroundColor.rgb, charAlpha);
    }
    else
    {
        // Palette mode (quantized colored)
        float3 adjustedColor = AdjustSaturation(cellColor.rgb, Saturation);
        adjustedColor = QuantizeColor(adjustedColor, QuantizeLevels);

        if (PreserveLuminance > 0.5)
        {
            // Keep original luminance after quantization
            float newLuma = GetLuminance(adjustedColor);
            if (newLuma > 0.001)
            {
                adjustedColor = adjustedColor * (luma / newLuma);
            }
        }

        finalColor = lerp(BackgroundColor.rgb, adjustedColor, charAlpha);
    }

    // Apply phosphor glow
    finalColor = ApplyPhosphorGlow(charAlpha, finalColor, adjustedLuma);

    // Apply bright character glow
    finalColor = ApplyBrightGlow(charAlpha, finalColor, adjustedLuma);

    // Apply grid lines
    finalColor = ApplyGridLines(cellUV, finalColor);

    // Apply vignette
    finalColor = ApplyVignette(crtUV, finalColor);

    // Apply noise
    finalColor = ApplyNoise(screenPos + Time * 100.0, finalColor);

    // Apply inner glow at shape edges
    finalColor = ApplyInnerGlow(effectMask, finalColor);

    // Blend with original based on effect mask
    float3 original = ScreenTexture.SampleLevel(LinearSampler, uv, 0).rgb;
    finalColor = lerp(original, finalColor, effectMask);

    return float4(finalColor, 1.0);
}
