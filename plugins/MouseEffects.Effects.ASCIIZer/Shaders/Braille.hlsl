// Braille Pattern Shader
// Renders screen as Unicode Braille patterns (2x4 dot grid = 256 patterns)
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
    float2 PE_ViewportSize;
    float PE_Time;
    float PE_Scanlines;
    float PE_ScanlineIntensity;
    float PE_ScanlineSpacing;
    float PE_CrtCurvature;
    float PE_CrtAmount;

    float PE_PhosphorGlow;
    float PE_PhosphorIntensity;
    float PE_Chromatic;
    float PE_ChromaticOffset;
    float PE_Vignette;
    float PE_VignetteIntensity;
    float PE_VignetteRadius;
    float PE_Noise;

    float PE_NoiseAmount;
    float PE_Flicker;
    float PE_FlickerSpeed;
    float _pe_pad1;
    float4 _pe_pad2;

    float4 _pe_reserved1;
    float4 _pe_reserved2;
};

// ============================================================================
// Constant Buffer 1: Braille Filter Parameters (128 bytes)
// ============================================================================
cbuffer BrailleParams : register(b1)
{
    // Core (32 bytes)
    float2 MousePosition;
    float LayoutMode;           // 0=Fullscreen, 1=Circle, 2=Rectangle
    float Radius;
    float EdgeSoftness;
    float ShapeFeather;
    float RectWidth;
    float RectHeight;

    // Braille settings (32 bytes)
    float Threshold;            // 0-1 luminance threshold
    float AdaptiveThreshold;    // 0 or 1 for per-block threshold adjustment
    float DotSize;              // 0.5-1.0 size of dots
    float DotSpacing;           // Space between dots
    float InvertDots;           // 0 or 1 to invert pattern
    float CellWidth;            // Width of Braille cell
    float CellHeight;           // Height of Braille cell
    float _pad1;

    // Colors (32 bytes)
    float4 ForegroundColor;     // Dot color
    float4 BackgroundColor;     // Background color

    // Brightness (32 bytes)
    float Brightness;
    float Contrast;
    float _pad2;
    float _pad3;
    float4 _pad4;
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
        float dist = length(delta);
        float softness = max(EdgeSoftness, 1.0);
        return 1.0 - smoothstep(Radius - softness, Radius + ShapeFeather, dist);
    }
    else
    {
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

// ============================================================================
// Braille Functions
// ============================================================================

// Braille character dot positions (2x4 grid)
// Unicode Braille patterns U+2800-U+28FF have this dot numbering:
// 1 4
// 2 5
// 3 6
// 7 8
// Bit 0 = dot 1, bit 1 = dot 2, etc.

// Sample luminance at a specific offset within a Braille cell
float SampleBrailleDot(float2 cellTopLeft, float2 cellSize, int dotX, int dotY, float2 texelSize)
{
    // Calculate position within the cell for this dot
    float2 dotPos = cellTopLeft + float2(
        (dotX + 0.5) * (cellSize.x / 2.0),
        (dotY + 0.5) * (cellSize.y / 4.0)
    );

    float2 uv = dotPos * texelSize;
    float3 color = ScreenTexture.SampleLevel(LinearSampler, uv, 0).rgb;
    return GetLuminance(color);
}

// Calculate adaptive threshold for a cell
float CalculateAdaptiveThreshold(float2 cellTopLeft, float2 cellSize, float2 texelSize)
{
    float totalLuminance = 0.0;

    // Sample all 8 dot positions
    for (int dy = 0; dy < 4; dy++)
    {
        for (int dx = 0; dx < 2; dx++)
        {
            totalLuminance += SampleBrailleDot(cellTopLeft, cellSize, dx, dy, texelSize);
        }
    }

    return totalLuminance / 8.0;
}

// Get the Braille pattern (8 bits) for a cell
int GetBraillePattern(float2 cellTopLeft, float2 cellSize, float2 texelSize, float threshold)
{
    int pattern = 0;

    // Sample each dot position and set bits
    // Braille bit layout:
    // bit 0 (1): top-left
    // bit 1 (2): middle-left upper
    // bit 2 (4): middle-left lower
    // bit 3 (8): top-right
    // bit 4 (16): middle-right upper
    // bit 5 (32): middle-right lower
    // bit 6 (64): bottom-left
    // bit 7 (128): bottom-right

    float lum;

    // Left column (dots 1, 2, 3, 7)
    lum = SampleBrailleDot(cellTopLeft, cellSize, 0, 0, texelSize);
    if (lum > threshold) pattern |= 1;

    lum = SampleBrailleDot(cellTopLeft, cellSize, 0, 1, texelSize);
    if (lum > threshold) pattern |= 2;

    lum = SampleBrailleDot(cellTopLeft, cellSize, 0, 2, texelSize);
    if (lum > threshold) pattern |= 4;

    lum = SampleBrailleDot(cellTopLeft, cellSize, 0, 3, texelSize);
    if (lum > threshold) pattern |= 64;

    // Right column (dots 4, 5, 6, 8)
    lum = SampleBrailleDot(cellTopLeft, cellSize, 1, 0, texelSize);
    if (lum > threshold) pattern |= 8;

    lum = SampleBrailleDot(cellTopLeft, cellSize, 1, 1, texelSize);
    if (lum > threshold) pattern |= 16;

    lum = SampleBrailleDot(cellTopLeft, cellSize, 1, 2, texelSize);
    if (lum > threshold) pattern |= 32;

    lum = SampleBrailleDot(cellTopLeft, cellSize, 1, 3, texelSize);
    if (lum > threshold) pattern |= 128;

    return pattern;
}

// Check if current pixel position should show a dot for a given pattern
bool ShouldShowDot(float2 localUV, int pattern)
{
    // Determine which dot position we're at (0-1 in each dimension)
    int dotX = localUV.x < 0.5 ? 0 : 1;
    int dotY = (int)(localUV.y * 4.0);
    dotY = clamp(dotY, 0, 3);

    // Get the bit for this dot position
    int bit = 0;
    if (dotX == 0)
    {
        if (dotY == 0) bit = 1;
        else if (dotY == 1) bit = 2;
        else if (dotY == 2) bit = 4;
        else bit = 64;
    }
    else
    {
        if (dotY == 0) bit = 8;
        else if (dotY == 1) bit = 16;
        else if (dotY == 2) bit = 32;
        else bit = 128;
    }

    return (pattern & bit) != 0;
}

// Draw a circular dot at the center of a dot cell
float DrawDot(float2 localUV, int dotX, int dotY, float dotSize, float spacing)
{
    // Calculate the center of this dot's cell
    float2 dotCellSize = float2(0.5, 0.25); // 2 columns, 4 rows
    float2 dotCenter = float2(
        (dotX + 0.5) * dotCellSize.x,
        (dotY + 0.5) * dotCellSize.y
    );

    // Calculate distance from center
    float2 delta = localUV - dotCenter;

    // Account for aspect ratio (cells are taller than wide)
    delta.x *= 2.0; // Make it circular

    float dist = length(delta);
    float radius = dotSize * 0.2; // Scale radius to fit in cell

    // Smooth circle with anti-aliasing
    return 1.0 - smoothstep(radius - 0.02, radius + 0.02, dist);
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

    // Calculate cell position
    float2 cellSize = float2(CellWidth, CellHeight);
    float2 cellIndex = floor(screenPos / cellSize);
    float2 cellTopLeft = cellIndex * cellSize;
    float2 localUV = frac(screenPos / cellSize);

    float2 texelSize = 1.0 / PE_ViewportSize;

    // Calculate threshold (optionally adaptive)
    float threshold = Threshold;
    if (AdaptiveThreshold > 0.5)
    {
        float adaptiveT = CalculateAdaptiveThreshold(cellTopLeft, cellSize, texelSize);
        threshold = lerp(Threshold, adaptiveT, 0.5); // Blend with base threshold
    }

    // Get Braille pattern for this cell
    int pattern = GetBraillePattern(cellTopLeft, cellSize, texelSize, threshold);

    // Apply inversion if enabled
    if (InvertDots > 0.5)
    {
        pattern = ~pattern & 0xFF;
    }

    // Draw the dots
    float dotAlpha = 0.0;

    // Check each dot position
    for (int dy = 0; dy < 4; dy++)
    {
        for (int dx = 0; dx < 2; dx++)
        {
            int bit = 0;
            if (dx == 0)
            {
                if (dy == 0) bit = 1;
                else if (dy == 1) bit = 2;
                else if (dy == 2) bit = 4;
                else bit = 64;
            }
            else
            {
                if (dy == 0) bit = 8;
                else if (dy == 1) bit = 16;
                else if (dy == 2) bit = 32;
                else bit = 128;
            }

            if ((pattern & bit) != 0)
            {
                dotAlpha = max(dotAlpha, DrawDot(localUV, dx, dy, DotSize, DotSpacing));
            }
        }
    }

    // Apply brightness and contrast
    float3 fg = ForegroundColor.rgb;
    float3 bg = BackgroundColor.rgb;

    fg = (fg - 0.5) * Contrast + 0.5 + Brightness;
    bg = (bg - 0.5) * Contrast + 0.5 + Brightness;

    // Blend foreground and background
    float3 finalColor = lerp(bg, fg, dotAlpha);

    // Apply scanlines
    float scanlineFactor = ApplyScanlines(screenPos, 1.0);
    finalColor *= scanlineFactor;

    // Apply post-effects
    finalColor = ApplyVignette(crtUV, finalColor);
    finalColor = ApplyNoise(screenPos + PE_Time * 100.0, finalColor);

    // Phosphor glow effect
    if (PE_PhosphorGlow > 0.5 && dotAlpha > 0.1)
    {
        float glowFactor = dotAlpha * PE_PhosphorIntensity;
        finalColor = finalColor * (1.0 + glowFactor * 0.5);
    }

    // Blend with original based on effect mask
    float3 original = ScreenTexture.SampleLevel(LinearSampler, uv, 0).rgb;
    finalColor = lerp(original, finalColor, effectMask);

    return float4(saturate(finalColor), 1.0);
}
