// Typewriter Shader
// Simulates old mechanical typewriter with ink variations and imperfections
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
// Constant Buffer 1: Typewriter Filter Parameters (128 bytes)
// ============================================================================
cbuffer TypewriterParams : register(b1)
{
    // Core (32 bytes)
    float2 MousePosition;
    float LayoutMode;           // 0=Fullscreen, 1=Circle, 2=Rectangle
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

    // Typewriter settings (32 bytes)
    float InkVariation;         // 0-0.5 random intensity per character
    float PositionJitter;       // 0-2 px random position offset
    float RibbonWear;           // 0 or 1 for horizontal fading effect
    float DoubleStrike;         // 0 or 1 for bold appearance with offset
    float StrikeOffset;         // Offset for double strike effect
    float AgeEffect;            // 0-1 overall aging/wear
    float _pad2;
    float _pad3;

    // Colors (32 bytes)
    float4 InkColor;            // Typewriter ink color
    float4 PaperColor;          // Background paper color

    // Brightness (16 bytes)
    float Brightness;
    float Contrast;
    float _pad4;
    float _pad5;
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

float hash2(float2 p)
{
    float h = dot(p, float2(269.5, 183.3));
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
// Typewriter Functions
// ============================================================================

// Get ink intensity variation for a cell
float GetInkVariation(float2 cellIndex)
{
    float baseVariation = hash(cellIndex * 3.14159);
    return 1.0 - baseVariation * InkVariation;
}

// Get position jitter for a cell
float2 GetPositionJitter(float2 cellIndex)
{
    float jitterX = (hash(cellIndex * 7.13) - 0.5) * 2.0 * PositionJitter;
    float jitterY = (hash2(cellIndex * 11.31) - 0.5) * 2.0 * PositionJitter;
    return float2(jitterX, jitterY);
}

// Get ribbon wear factor (horizontal fading)
float GetRibbonWear(float2 screenPos)
{
    if (RibbonWear < 0.5)
        return 1.0;

    // Simulate ribbon getting lighter from left to right
    float normalizedX = screenPos.x / PE_ViewportSize.x;

    // Add some noise to make it look more natural
    float wearNoise = noise2D(screenPos * 0.02) * 0.1;
    float wear = 1.0 - (normalizedX * 0.3 + wearNoise);

    return clamp(wear, 0.5, 1.0);
}

// Get character index based on luminance
float GetCharacterIndex(float luminance)
{
    // Map luminance to character index
    // Higher luminance = lower index (lighter character)
    float index = floor((1.0 - luminance) * (CharCount - 1.0) + 0.5);
    return clamp(index, 0.0, CharCount - 1.0);
}

// Sample character from atlas with optional offset
float SampleCharacter(float charIndex, float2 cellUV, float2 offset)
{
    float2 adjustedUV = cellUV + offset / float2(CellWidth, CellHeight);

    // Clamp to valid range
    adjustedUV = clamp(adjustedUV, 0.0, 1.0);

    float atlasU = (charIndex + adjustedUV.x) / CharCount;
    float atlasV = adjustedUV.y;
    return FontAtlas.SampleLevel(LinearSampler, float2(atlasU, atlasV), 0).r;
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
    float2 cellCenter = cellTopLeft + cellSize * 0.5;

    // Get position jitter
    float2 jitter = GetPositionJitter(cellIndex);
    float2 jitteredPos = screenPos + jitter;
    float2 cellUV = frac(jitteredPos / cellSize);

    // Sample screen texture for this cell
    float2 sampleUV = cellCenter / PE_ViewportSize;
    float3 sourceColor = ScreenTexture.SampleLevel(LinearSampler, sampleUV, 0).rgb;
    float luminance = GetLuminance(sourceColor);

    // Get character index
    float charIndex = GetCharacterIndex(luminance);

    // Sample character
    float charAlpha = SampleCharacter(charIndex, cellUV, float2(0, 0));

    // Apply double strike effect
    if (DoubleStrike > 0.5)
    {
        float offsetAmount = StrikeOffset * 0.5;
        float strikeAlpha = SampleCharacter(charIndex, cellUV, float2(offsetAmount, offsetAmount));
        charAlpha = max(charAlpha, strikeAlpha * 0.7);
    }

    // Get ink variation
    float inkIntensity = GetInkVariation(cellIndex);

    // Get ribbon wear
    float ribbonFactor = GetRibbonWear(screenPos);

    // Apply aging effect (random spots and inconsistencies)
    float ageNoise = noise2D(screenPos * 0.05);
    float ageFactor = lerp(1.0, ageNoise, AgeEffect * 0.3);

    // Combine all intensity factors
    float finalIntensity = charAlpha * inkIntensity * ribbonFactor * ageFactor;

    // Apply brightness and contrast to ink color
    float3 ink = InkColor.rgb;
    ink = (ink - 0.5) * Contrast + 0.5 + Brightness;

    // Apply brightness and contrast to paper color
    float3 paper = PaperColor.rgb;
    paper = (paper - 0.5) * Contrast + 0.5 + Brightness;

    // Add paper texture (subtle noise)
    float paperNoise = noise2D(screenPos * 0.3) * 0.02;
    paper = saturate(paper + paperNoise);

    // Blend ink and paper
    float3 finalColor = lerp(paper, ink, finalIntensity);

    // Apply scanlines
    float scanlineFactor = ApplyScanlines(screenPos, 1.0);
    finalColor *= scanlineFactor;

    // Apply post-effects
    finalColor = ApplyVignette(crtUV, finalColor);
    finalColor = ApplyNoise(screenPos + PE_Time * 100.0, finalColor);

    // Phosphor glow effect
    if (PE_PhosphorGlow > 0.5 && finalIntensity > 0.1)
    {
        float glowFactor = finalIntensity * PE_PhosphorIntensity;
        finalColor = finalColor * (1.0 + glowFactor * 0.3);
    }

    // Blend with original based on effect mask
    float3 original = ScreenTexture.SampleLevel(LinearSampler, uv, 0).rgb;
    finalColor = lerp(original, finalColor, effectMask);

    return float4(saturate(finalColor), 1.0);
}
