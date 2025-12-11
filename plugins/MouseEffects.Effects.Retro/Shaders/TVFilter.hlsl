// TVFilter.hlsl
// CRT TV simulation with RGB phosphor pattern
// Renders each pixel as 3 separate oval-shaped R, G, B phosphor cells

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
// Constant Buffer 1: TV Filter Parameters (80 bytes)
// ============================================================================
cbuffer TVFilterParams : register(b1)
{
    // Core layout (32 bytes)
    float2 ViewportSize;
    float2 MousePosition;
    float2 TexelSize;          // 1.0 / ViewportSize
    float LayoutMode;          // 0=Fullscreen, 1=Circle, 2=Rectangle
    float Radius;

    // Layout continued (32 bytes)
    float RectWidth;
    float RectHeight;
    float EdgeSoftness;
    float Mode;                // 0=Enhancement, 1=Pixelate+Scale, 2=Downscale+Upscale
    float PixelSize;
    float ScaleFactor;
    float Strength;
    float Time;

    // TV-specific (16 bytes)
    float PhosphorWidth;       // Width of oval phosphors (0.3-0.9)
    float PhosphorHeight;      // Height of oval phosphors (0.5-1.0)
    float PhosphorGap;         // Gap between phosphors (0.0-0.2)
    float Brightness;          // Phosphor brightness boost (1.0-3.0)
};

// ============================================================================
// Textures and Samplers
// ============================================================================
Texture2D<float4> ScreenTexture : register(t0);
SamplerState PointSampler : register(s0);
SamplerState LinearSampler : register(s1);

// ============================================================================
// Post-Effects Helper Functions
// ============================================================================

float PE_Hash(float2 p)
{
    float h = dot(p, float2(127.1, 311.7));
    return frac(sin(h) * 43758.5453123);
}

float PE_Noise2D(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f);

    float a = PE_Hash(i);
    float b = PE_Hash(i + float2(1.0, 0.0));
    float c = PE_Hash(i + float2(0.0, 1.0));
    float d = PE_Hash(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

float2 PE_ApplyCRTCurvature(float2 uv)
{
    if (PE_CrtCurvature < 0.5)
        return uv;

    float2 centered = uv * 2.0 - 1.0;
    float r2 = dot(centered, centered);
    centered *= 1.0 + PE_CrtAmount * r2;
    return centered * 0.5 + 0.5;
}

bool PE_IsValidUV(float2 uv)
{
    return uv.x >= 0.0 && uv.x <= 1.0 && uv.y >= 0.0 && uv.y <= 1.0;
}

float PE_ApplyScanlines(float2 screenPos, float value)
{
    if (PE_Scanlines < 0.5)
        return value;

    float spacing = max(PE_ScanlineSpacing, 1.0);
    float rowPos = fmod(screenPos.y, spacing * 2.0);
    float scanEffect = step(spacing, rowPos);

    return lerp(value, value * (1.0 - PE_ScanlineIntensity), scanEffect);
}

float3 PE_ApplyVignette(float2 uv, float3 color)
{
    if (PE_Vignette < 0.5)
        return color;

    float2 centered = uv * 2.0 - 1.0;
    float dist = length(centered);
    float vignetteFactor = smoothstep(PE_VignetteRadius, PE_VignetteRadius + 0.5, dist);

    return color * (1.0 - vignetteFactor * PE_VignetteIntensity);
}

float3 PE_ApplyNoise(float2 screenPos, float3 color)
{
    if (PE_Noise < 0.5)
        return color;

    float n = PE_Noise2D(screenPos * 0.1 + PE_Time * 10.0);
    n = (n - 0.5) * 2.0 * PE_NoiseAmount;

    return saturate(color + n);
}

float PE_ApplyFlicker(float value)
{
    if (PE_Flicker < 0.5)
        return value;

    float flicker = 0.95 + 0.05 * sin(PE_Time * PE_FlickerSpeed * 60.0);
    flicker *= 0.98 + 0.02 * sin(PE_Time * PE_FlickerSpeed * 17.3);

    return value * flicker;
}

float3 PE_ApplyPhosphorGlow(float alpha, float3 color)
{
    if (PE_PhosphorGlow < 0.5)
        return color;

    float glowFactor = alpha * PE_PhosphorIntensity;
    float3 glowColor = color * (1.0 + glowFactor);

    return lerp(color, glowColor, alpha);
}

float3 PE_ApplyAllPostEffects(float2 uv, float2 screenPos, float3 color, float charAlpha)
{
    color = PE_ApplyPhosphorGlow(charAlpha, color);
    color = PE_ApplyVignette(uv, color);
    color = PE_ApplyNoise(screenPos + PE_Time * 100.0, color);
    return color;
}

float PE_ApplyAlphaEffects(float2 screenPos, float alpha)
{
    alpha = PE_ApplyFlicker(alpha);
    alpha = PE_ApplyScanlines(screenPos, alpha);
    return alpha;
}

// ============================================================================
// TV Filter - CRT Phosphor Rendering
// ============================================================================

// Calculate phosphor mask for a single channel
float CalcPhosphorMask(float subPixelX, float cellY, int targetChannel)
{
    // Each channel has its own position within the RGB triplet
    // Channel 0 (R) is at 0-1/3, Channel 1 (G) at 1/3-2/3, Channel 2 (B) at 2/3-1
    float channelCenter = (targetChannel + 0.5) / 3.0;
    float distFromCenter = abs(subPixelX - channelCenter) * 3.0;

    // Horizontal mask - gaussian-like falloff from channel center
    float hMask = saturate(1.0 - distFromCenter / PhosphorWidth);
    hMask = hMask * hMask; // Squared for smoother falloff

    // Vertical mask - oval shape
    float vDist = abs(cellY - 0.5) / (PhosphorHeight * 0.5);
    float vMask = saturate(1.0 - vDist);
    vMask = smoothstep(0.0, 0.3, vMask);

    return hMask * vMask;
}

// Render a single pixel as 3 RGB phosphor cells with proper blending
float4 RenderTVPixel(float2 uv, float2 cellSize)
{
    // Get the virtual pixel position
    float2 pixelPos = floor(uv / cellSize);
    float2 pixelUV = (pixelPos + 0.5) * cellSize;

    // Sample the color for this pixel
    float4 color = ScreenTexture.Sample(LinearSampler, pixelUV);

    // Position within the pixel cell (0-1)
    float2 cellFrac = frac(uv / cellSize);

    // Calculate phosphor masks for each channel
    float rMask = CalcPhosphorMask(cellFrac.x, cellFrac.y, 0);
    float gMask = CalcPhosphorMask(cellFrac.x, cellFrac.y, 1);
    float bMask = CalcPhosphorMask(cellFrac.x, cellFrac.y, 2);

    // Apply gap between phosphor rows (scanline-like effect within each pixel)
    float rowGap = smoothstep(0.0, PhosphorGap, cellFrac.y) * smoothstep(0.0, PhosphorGap, 1.0 - cellFrac.y);
    rMask *= rowGap;
    gMask *= rowGap;
    bMask *= rowGap;

    // Apply masks to color channels with brightness compensation
    float3 result;
    result.r = color.r * rMask * Brightness;
    result.g = color.g * gMask * Brightness;
    result.b = color.b * bMask * Brightness;

    // Add a subtle base level to prevent pure black (simulates phosphor persistence/ambient)
    float3 baseColor = color.rgb * 0.1;
    result = max(result, baseColor);

    return float4(result, 1.0);
}

// ============================================================================
// Layout Mask Calculation
// ============================================================================

float CalculateLayoutMask(float2 screenPos, float2 mousePos)
{
    if (LayoutMode < 0.5)
        return 1.0;

    if (LayoutMode < 1.5)
    {
        float dist = length(screenPos - mousePos);
        return 1.0 - smoothstep(Radius - EdgeSoftness, Radius, dist);
    }

    float2 halfSize = float2(RectWidth, RectHeight) * 0.5;
    float2 delta = abs(screenPos - mousePos);
    float2 edgeDist = delta - halfSize + EdgeSoftness;
    return 1.0 - smoothstep(0, EdgeSoftness, max(edgeDist.x, edgeDist.y));
}

// ============================================================================
// Vertex and Pixel Shaders
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

float4 PSMain(PSInput input) : SV_Target
{
    float2 uv = input.TexCoord;
    float2 screenPos = uv * ViewportSize;

    // Apply CRT curvature
    float2 distortedUV = PE_ApplyCRTCurvature(uv);
    if (!PE_IsValidUV(distortedUV))
        return float4(0, 0, 0, 1);

    // Get original screen color
    float4 original = ScreenTexture.Sample(LinearSampler, distortedUV);

    // Calculate cell size based on scaling mode
    float2 cellSize;
    if (Mode < 0.5)
    {
        // Mode 0: Enhancement (native resolution)
        cellSize = TexelSize * 3.0;  // Need at least 3 pixels wide for RGB
    }
    else if (Mode < 1.5)
    {
        // Mode 1: Pixelate + Scale
        cellSize = PixelSize / ViewportSize;
    }
    else
    {
        // Mode 2: Downscale + Upscale
        cellSize = ScaleFactor * TexelSize;
    }

    // Render TV phosphor pattern
    float4 filtered = RenderTVPixel(distortedUV, cellSize);

    // Blend based on strength
    float4 result = lerp(original, filtered, Strength);

    // Apply layout mask
    float mask = CalculateLayoutMask(screenPos, MousePosition);
    result = lerp(original, result, mask);

    // Apply scanlines
    float scanlineMask = PE_ApplyAlphaEffects(screenPos, mask);
    result.rgb = lerp(original.rgb, result.rgb, scanlineMask);

    // Apply post-effects
    result.rgb = PE_ApplyAllPostEffects(distortedUV, screenPos, result.rgb, mask);

    return result;
}
