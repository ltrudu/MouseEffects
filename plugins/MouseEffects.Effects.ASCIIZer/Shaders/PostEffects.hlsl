// PostEffects.hlsl
// Shared post-processing effects for all ASCIIZer filters
// Include this file in filter shaders and call ApplyAllPostEffects() at the end

#ifndef POST_EFFECTS_HLSL
#define POST_EFFECTS_HLSL

// Post-effects constant buffer - register b0
// Each filter uses b1 for its specific parameters
cbuffer PostEffectsParams : register(b0)
{
    // Core parameters (32 bytes)
    float2 PE_ViewportSize;
    float PE_Time;
    float PE_Scanlines;
    float PE_ScanlineIntensity;
    float PE_ScanlineSpacing;
    float PE_CrtCurvature;
    float PE_CrtAmount;

    // More effects (32 bytes)
    float PE_PhosphorGlow;
    float PE_PhosphorIntensity;
    float PE_Chromatic;
    float PE_ChromaticOffset;
    float PE_Vignette;
    float PE_VignetteIntensity;
    float PE_VignetteRadius;
    float PE_Noise;

    // Even more (32 bytes)
    float PE_NoiseAmount;
    float PE_Flicker;
    float PE_FlickerSpeed;
    float _pe_pad1;
    float4 _pe_pad2;

    // Reserved for future (32 bytes)
    float4 _pe_reserved1;
    float4 _pe_reserved2;
};

// ============================================================================
// Helper Functions
// ============================================================================

// Random number generation for noise
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

// ============================================================================
// Post-Effect Functions
// ============================================================================

// Apply CRT curvature distortion to UV coordinates
// Call this BEFORE sampling the screen texture
float2 PE_ApplyCRTCurvature(float2 uv)
{
    if (PE_CrtCurvature < 0.5)
        return uv;

    // Center UV coordinates
    float2 centered = uv * 2.0 - 1.0;

    // Apply barrel distortion
    float r2 = dot(centered, centered);
    centered *= 1.0 + PE_CrtAmount * r2;

    // Convert back to UV space
    return centered * 0.5 + 0.5;
}

// Check if UV is valid after CRT distortion
bool PE_IsValidUV(float2 uv)
{
    return uv.x >= 0.0 && uv.x <= 1.0 && uv.y >= 0.0 && uv.y <= 1.0;
}

// Apply scanline effect
// Typically applied to character alpha or overall brightness
float PE_ApplyScanlines(float2 screenPos, float value)
{
    if (PE_Scanlines < 0.5)
        return value;

    float spacing = max(PE_ScanlineSpacing, 1.0);
    float rowPos = fmod(screenPos.y, spacing * 2.0);
    float scanEffect = step(spacing, rowPos);

    return lerp(value, value * (1.0 - PE_ScanlineIntensity), scanEffect);
}

// Apply vignette effect (darkening at edges)
float3 PE_ApplyVignette(float2 uv, float3 color)
{
    if (PE_Vignette < 0.5)
        return color;

    float2 centered = uv * 2.0 - 1.0;
    float dist = length(centered);
    float vignetteFactor = smoothstep(PE_VignetteRadius, PE_VignetteRadius + 0.5, dist);

    return color * (1.0 - vignetteFactor * PE_VignetteIntensity);
}

// Apply chromatic aberration (RGB channel separation)
// Note: Requires access to screen texture, so this is a helper for the offset calculation
float2 PE_GetChromaticOffset(float2 uv)
{
    if (PE_Chromatic < 0.5)
        return float2(0.0, 0.0);

    float2 centered = uv - 0.5;
    return centered * PE_ChromaticOffset / PE_ViewportSize;
}

// Apply noise/grain effect
float3 PE_ApplyNoise(float2 screenPos, float3 color)
{
    if (PE_Noise < 0.5)
        return color;

    float n = PE_Noise2D(screenPos * 0.1 + PE_Time * 10.0);
    n = (n - 0.5) * 2.0 * PE_NoiseAmount;

    return saturate(color + n);
}

// Apply flicker effect (brightness variation)
float PE_ApplyFlicker(float value)
{
    if (PE_Flicker < 0.5)
        return value;

    float flicker = 0.95 + 0.05 * sin(PE_Time * PE_FlickerSpeed * 60.0);
    flicker *= 0.98 + 0.02 * sin(PE_Time * PE_FlickerSpeed * 17.3);

    return value * flicker;
}

// Apply phosphor glow effect (bloom on lit areas)
float3 PE_ApplyPhosphorGlow(float alpha, float3 color)
{
    if (PE_PhosphorGlow < 0.5)
        return color;

    float glowFactor = alpha * PE_PhosphorIntensity;
    float3 glowColor = color * (1.0 + glowFactor);

    return lerp(color, glowColor, alpha);
}

// ============================================================================
// Convenience Wrappers
// ============================================================================

// Apply all post-effects to final color
// Call this at the end of your pixel shader, before blending with original
// Parameters:
//   uv - texture coordinates (after CRT distortion)
//   screenPos - screen position in pixels
//   color - the rendered filter output
//   charAlpha - character/shape alpha (for scanlines and phosphor glow)
float3 PE_ApplyAllPostEffects(float2 uv, float2 screenPos, float3 color, float charAlpha)
{
    // Apply phosphor glow
    color = PE_ApplyPhosphorGlow(charAlpha, color);

    // Apply vignette
    color = PE_ApplyVignette(uv, color);

    // Apply noise
    color = PE_ApplyNoise(screenPos + PE_Time * 100.0, color);

    return color;
}

// Apply scanlines and flicker to alpha/intensity value
// Call this on character alpha before using it
float PE_ApplyAlphaEffects(float2 screenPos, float alpha)
{
    alpha = PE_ApplyFlicker(alpha);
    alpha = PE_ApplyScanlines(screenPos, alpha);
    return alpha;
}

#endif // POST_EFFECTS_HLSL
