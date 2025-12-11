// ToonFilter.hlsl
// Cel-shading / Cartoon effect with edge detection and color quantization

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
// Constant Buffer 1: Toon Filter Parameters (80 bytes)
// ============================================================================
cbuffer ToonFilterParams : register(b1)
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

    // Toon-specific (16 bytes)
    float EdgeThreshold;       // Edge detection sensitivity (0.0-1.0)
    float EdgeWidth;           // Outline thickness in pixels (1-5)
    float ColorLevels;         // Color quantization levels (2-16)
    float Saturation;          // Color saturation boost (0.5-2.0)
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
// Toon Filter - Cel Shading Functions
// ============================================================================

// Convert RGB to luminance
float Luminance(float3 color)
{
    return dot(color, float3(0.299, 0.587, 0.114));
}

// Convert RGB to HSV
float3 RGBtoHSV(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

// Convert HSV to RGB
float3 HSVtoRGB(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}

// Sobel edge detection
float SobelEdge(float2 uv, float2 texelSize)
{
    float2 offset = texelSize * EdgeWidth;

    // Sample 3x3 neighborhood luminances
    float tl = Luminance(ScreenTexture.Sample(LinearSampler, uv + float2(-offset.x, -offset.y)).rgb);
    float t  = Luminance(ScreenTexture.Sample(LinearSampler, uv + float2(0, -offset.y)).rgb);
    float tr = Luminance(ScreenTexture.Sample(LinearSampler, uv + float2(offset.x, -offset.y)).rgb);
    float l  = Luminance(ScreenTexture.Sample(LinearSampler, uv + float2(-offset.x, 0)).rgb);
    float r  = Luminance(ScreenTexture.Sample(LinearSampler, uv + float2(offset.x, 0)).rgb);
    float bl = Luminance(ScreenTexture.Sample(LinearSampler, uv + float2(-offset.x, offset.y)).rgb);
    float b  = Luminance(ScreenTexture.Sample(LinearSampler, uv + float2(0, offset.y)).rgb);
    float br = Luminance(ScreenTexture.Sample(LinearSampler, uv + float2(offset.x, offset.y)).rgb);

    // Sobel operators
    float sobelX = -tl - 2.0*l - bl + tr + 2.0*r + br;
    float sobelY = -tl - 2.0*t - tr + bl + 2.0*b + br;

    return sqrt(sobelX * sobelX + sobelY * sobelY);
}

// Quantize color to discrete levels
float3 QuantizeColor(float3 color, float levels)
{
    return floor(color * levels + 0.5) / levels;
}

// Apply toon shading effect
float4 RenderToonPixel(float2 uv, float2 cellSize)
{
    // Get virtual pixel position for pixelation
    float2 pixelPos = floor(uv / cellSize);
    float2 pixelUV = (pixelPos + 0.5) * cellSize;

    // Sample the color
    float4 color = ScreenTexture.Sample(LinearSampler, pixelUV);

    // Edge detection
    float edge = SobelEdge(pixelUV, TexelSize);
    float edgeMask = step(EdgeThreshold, edge);

    // Convert to HSV for better color manipulation
    float3 hsv = RGBtoHSV(color.rgb);

    // Boost saturation for cartoon look
    hsv.y = saturate(hsv.y * Saturation);

    // Quantize value (brightness) for cel-shading effect
    hsv.z = floor(hsv.z * ColorLevels + 0.5) / ColorLevels;

    // Convert back to RGB
    float3 toonColor = HSVtoRGB(hsv);

    // Quantize hue as well for more distinct colors
    float3 quantizedColor = QuantizeColor(toonColor, ColorLevels);

    // Blend quantized with toon color
    float3 finalColor = lerp(toonColor, quantizedColor, 0.5);

    // Apply black outline on edges
    finalColor = lerp(finalColor, float3(0, 0, 0), edgeMask);

    return float4(finalColor, 1.0);
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

    // Apply CRT curvature if enabled
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
        cellSize = TexelSize;
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

    // Render toon effect
    float4 filtered = RenderToonPixel(distortedUV, cellSize);

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
