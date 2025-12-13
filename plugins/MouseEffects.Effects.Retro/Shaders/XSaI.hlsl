// XSaI.hlsl
// Super 2xSaI scaling algorithm implementation for HLSL
// Based on the algorithm by Derek Liauw Kie Fa (GNU-GPL)
// Adapted from DOSBox source (GNU-GPL, 2002-2007 The DOSBox Team)

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
// Constant Buffer 1: XSaI Filter Parameters (64 bytes)
// ============================================================================
cbuffer XSaIParams : register(b1)
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
    float ScaleFactor;         // 2, 4, 8, 16 for downscale mode
    float Strength;
    float Time;
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
// Super 2xSaI Algorithm
// ============================================================================

// Color reduction for comparison (from original algorithm)
static const float3 dtt = float3(65536.0, 256.0, 1.0);

float reduce(float3 color)
{
    return dot(color, dtt);
}

// GET_RESULT function from Derek Liauw Kie Fa (GNU-GPL)
int GET_RESULT(float A, float B, float C, float D)
{
    int x = 0;
    int y = 0;
    int r = 0;

    if (A == C) x += 1; else if (B == C) y += 1;
    if (A == D) x += 1; else if (B == D) y += 1;
    if (x <= 1) r += 1;
    if (y <= 1) r -= 1;

    return r;
}

// Super 2xSaI core algorithm
float4 Super2xSaI(float2 uv, float2 texelSize)
{
    // Calculate base texel position
    float2 texelPos = uv / texelSize;
    float2 fp = frac(texelPos);
    float2 baseUV = (floor(texelPos) + 0.5) * texelSize;

    // Sample 4x4 neighborhood
    float4 c00 = ScreenTexture.Sample(PointSampler, baseUV + float2(-1, -1) * texelSize);
    float4 c10 = ScreenTexture.Sample(PointSampler, baseUV + float2( 0, -1) * texelSize);
    float4 c20 = ScreenTexture.Sample(PointSampler, baseUV + float2( 1, -1) * texelSize);
    float4 c30 = ScreenTexture.Sample(PointSampler, baseUV + float2( 2, -1) * texelSize);

    float4 c01 = ScreenTexture.Sample(PointSampler, baseUV + float2(-1, 0) * texelSize);
    float4 c11 = ScreenTexture.Sample(PointSampler, baseUV + float2( 0, 0) * texelSize);
    float4 c21 = ScreenTexture.Sample(PointSampler, baseUV + float2( 1, 0) * texelSize);
    float4 c31 = ScreenTexture.Sample(PointSampler, baseUV + float2( 2, 0) * texelSize);

    float4 c02 = ScreenTexture.Sample(PointSampler, baseUV + float2(-1, 1) * texelSize);
    float4 c12 = ScreenTexture.Sample(PointSampler, baseUV + float2( 0, 1) * texelSize);
    float4 c22 = ScreenTexture.Sample(PointSampler, baseUV + float2( 1, 1) * texelSize);
    float4 c32 = ScreenTexture.Sample(PointSampler, baseUV + float2( 2, 1) * texelSize);

    float4 c03 = ScreenTexture.Sample(PointSampler, baseUV + float2(-1, 2) * texelSize);
    float4 c13 = ScreenTexture.Sample(PointSampler, baseUV + float2( 0, 2) * texelSize);
    float4 c23 = ScreenTexture.Sample(PointSampler, baseUV + float2( 1, 2) * texelSize);
    float4 c33 = ScreenTexture.Sample(PointSampler, baseUV + float2( 2, 2) * texelSize);

    // Reduce colors to scalar values for comparison
    float r01 = reduce(c01.rgb); float r11 = reduce(c11.rgb); float r21 = reduce(c21.rgb); float r31 = reduce(c31.rgb);
    float r10 = reduce(c10.rgb); float r12 = reduce(c12.rgb); float r02 = reduce(c02.rgb);
    float r20 = reduce(c20.rgb); float r22 = reduce(c22.rgb); float r13 = reduce(c13.rgb);
    float r23 = reduce(c23.rgb);

    // Calculate output using Super 2xSaI logic
    float4 product1b, product2b;

    if (r11 == r22 && r12 != r21)
    {
        product2b = c11;
        product1b = c11;
    }
    else if (r12 == r21 && r11 != r22)
    {
        product2b = c12;
        product1b = c21;
    }
    else if (r11 == r22 && r12 == r21)
    {
        int result = GET_RESULT(r11, r21, r01, r10);
        result += GET_RESULT(r11, r21, r22, r13);
        result += GET_RESULT(r11, r21, r02, r31);
        result += GET_RESULT(r11, r21, r23, r20);

        if (result > 0)
        {
            product2b = c11;
            product1b = c11;
        }
        else if (result < 0)
        {
            product2b = c12;
            product1b = c21;
        }
        else
        {
            product2b = (c11 + c12 + c21 + c22) * 0.25;
            product1b = product2b;
        }
    }
    else
    {
        product2b = (c11 + c12 + c21 + c22) * 0.25;
        product1b = product2b;
    }

    // Diagonal difference for edge-aware blending
    float m1 = dot(abs(c11.rgb - c22.rgb), float3(1, 1, 1)) + 0.001;
    float m2 = dot(abs(c21.rgb - c12.rgb), float3(1, 1, 1)) + 0.001;

    float4 edgeBlend = (m1 * (c21 + c12) + m2 * (c11 + c22)) / (2.0 * (m1 + m2));

    // Interpolate based on sub-pixel position
    float4 result;
    if (fp.x < 0.5)
    {
        if (fp.y < 0.5)
            result = lerp(c11, edgeBlend, fp.x + fp.y);
        else
            result = lerp(lerp(c11, c12, fp.y), edgeBlend, fp.x);
    }
    else
    {
        if (fp.y < 0.5)
            result = lerp(lerp(c11, c21, fp.x), edgeBlend, fp.y);
        else
            result = lerp(edgeBlend, c22, (fp.x - 0.5) + (fp.y - 0.5));
    }

    return result;
}

// Simple xSaI variant for enhancement mode
float4 XSaIEnhance(float2 uv, float2 texelSize)
{
    float4 c00 = ScreenTexture.Sample(PointSampler, uv + float2(-0.25, -0.25) * texelSize);
    float4 c10 = ScreenTexture.Sample(PointSampler, uv + float2( 0.25, -0.25) * texelSize);
    float4 c01 = ScreenTexture.Sample(PointSampler, uv + float2(-0.25,  0.25) * texelSize);
    float4 c11 = ScreenTexture.Sample(PointSampler, uv + float2( 0.25,  0.25) * texelSize);

    float m1 = dot(abs(c00.rgb - c11.rgb), float3(1, 1, 1)) + 0.001;
    float m2 = dot(abs(c10.rgb - c01.rgb), float3(1, 1, 1)) + 0.001;

    return (m1 * (c10 + c01) + m2 * (c00 + c11)) / (2.0 * (m1 + m2));
}

// Helper: Sample a downscaled cell by averaging 4 points within the cell
float4 SampleCell(float2 cellCenter, float2 cellSize)
{
    float4 sum = ScreenTexture.Sample(LinearSampler, cellCenter + float2(-0.25, -0.25) * cellSize);
    sum += ScreenTexture.Sample(LinearSampler, cellCenter + float2( 0.25, -0.25) * cellSize);
    sum += ScreenTexture.Sample(LinearSampler, cellCenter + float2(-0.25,  0.25) * cellSize);
    sum += ScreenTexture.Sample(LinearSampler, cellCenter + float2( 0.25,  0.25) * cellSize);
    return sum * 0.25;
}

// Downscale by averaging pixels in a cell, then apply xSaI upscaling
float4 DownscaleUpscale(float2 uv, float2 cellSize)
{
    // Calculate which cell we're in
    float2 cellPos = floor(uv / cellSize);
    float2 baseUV = (cellPos + 0.5) * cellSize;

    // Fractional position within the cell (for xSaI interpolation)
    float2 fp = frac(uv / cellSize);

    // Sample 4x4 neighborhood of downscaled cells explicitly (no arrays)
    float4 c00 = SampleCell(baseUV + float2(-1, -1) * cellSize, cellSize);
    float4 c10 = SampleCell(baseUV + float2( 0, -1) * cellSize, cellSize);
    float4 c20 = SampleCell(baseUV + float2( 1, -1) * cellSize, cellSize);
    float4 c30 = SampleCell(baseUV + float2( 2, -1) * cellSize, cellSize);

    float4 c01 = SampleCell(baseUV + float2(-1,  0) * cellSize, cellSize);
    float4 c11 = SampleCell(baseUV + float2( 0,  0) * cellSize, cellSize);
    float4 c21 = SampleCell(baseUV + float2( 1,  0) * cellSize, cellSize);
    float4 c31 = SampleCell(baseUV + float2( 2,  0) * cellSize, cellSize);

    float4 c02 = SampleCell(baseUV + float2(-1,  1) * cellSize, cellSize);
    float4 c12 = SampleCell(baseUV + float2( 0,  1) * cellSize, cellSize);
    float4 c22 = SampleCell(baseUV + float2( 1,  1) * cellSize, cellSize);
    float4 c32 = SampleCell(baseUV + float2( 2,  1) * cellSize, cellSize);

    float4 c03 = SampleCell(baseUV + float2(-1,  2) * cellSize, cellSize);
    float4 c13 = SampleCell(baseUV + float2( 0,  2) * cellSize, cellSize);
    float4 c23 = SampleCell(baseUV + float2( 1,  2) * cellSize, cellSize);
    float4 c33 = SampleCell(baseUV + float2( 2,  2) * cellSize, cellSize);

    // Reduce colors to scalar values for comparison
    float r01 = reduce(c01.rgb); float r11 = reduce(c11.rgb); float r21 = reduce(c21.rgb); float r31 = reduce(c31.rgb);
    float r10 = reduce(c10.rgb); float r12 = reduce(c12.rgb); float r02 = reduce(c02.rgb);
    float r20 = reduce(c20.rgb); float r22 = reduce(c22.rgb); float r13 = reduce(c13.rgb);
    float r23 = reduce(c23.rgb);

    // Apply Super 2xSaI logic
    float4 product1b, product2b;

    if (r11 == r22 && r12 != r21)
    {
        product2b = c11;
        product1b = c11;
    }
    else if (r12 == r21 && r11 != r22)
    {
        product2b = c12;
        product1b = c21;
    }
    else if (r11 == r22 && r12 == r21)
    {
        int result = GET_RESULT(r11, r21, r01, r10);
        result += GET_RESULT(r11, r21, r22, r13);
        result += GET_RESULT(r11, r21, r02, r31);
        result += GET_RESULT(r11, r21, r23, r20);

        if (result > 0)
        {
            product2b = c11;
            product1b = c11;
        }
        else if (result < 0)
        {
            product2b = c12;
            product1b = c21;
        }
        else
        {
            product2b = (c11 + c12 + c21 + c22) * 0.25;
            product1b = product2b;
        }
    }
    else
    {
        product2b = (c11 + c12 + c21 + c22) * 0.25;
        product1b = product2b;
    }

    // Diagonal difference for edge-aware blending
    float m1 = dot(abs(c11.rgb - c22.rgb), float3(1, 1, 1)) + 0.001;
    float m2 = dot(abs(c21.rgb - c12.rgb), float3(1, 1, 1)) + 0.001;

    float4 edgeBlend = (m1 * (c21 + c12) + m2 * (c11 + c22)) / (2.0 * (m1 + m2));

    // Interpolate based on sub-pixel position within cell
    float4 result;
    if (fp.x < 0.5)
    {
        if (fp.y < 0.5)
            result = lerp(c11, edgeBlend, fp.x + fp.y);
        else
            result = lerp(lerp(c11, c12, fp.y), edgeBlend, fp.x);
    }
    else
    {
        if (fp.y < 0.5)
            result = lerp(lerp(c11, c21, fp.x), edgeBlend, fp.y);
        else
            result = lerp(edgeBlend, c22, (fp.x - 0.5) + (fp.y - 0.5));
    }

    return result;
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

    // Calculate effective texel size based on mode
    float2 effectiveTexelSize;
    float4 filtered;

    if (Mode < 0.5)
    {
        // Mode 0: Enhancement (native resolution with edge smoothing)
        effectiveTexelSize = TexelSize;
        filtered = XSaIEnhance(distortedUV, effectiveTexelSize);
    }
    else if (Mode < 1.5)
    {
        // Mode 1: Pixelate + Scale
        effectiveTexelSize = PixelSize / ViewportSize;
        filtered = Super2xSaI(distortedUV, effectiveTexelSize);
    }
    else
    {
        // Mode 2: Downscale + Upscale (reduce resolution by ScaleFactor, then upscale with xSaI)
        float2 cellSize = ScaleFactor * TexelSize;  // Each cell = ScaleFactor pixels
        filtered = DownscaleUpscale(distortedUV, cellSize);
    }

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
