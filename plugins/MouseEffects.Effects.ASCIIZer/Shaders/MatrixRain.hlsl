// Matrix Rain Shader
// Renders falling green characters like The Matrix movie
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
// Constant Buffer 1: Matrix Rain Filter Parameters (128 bytes)
// ============================================================================
cbuffer MatrixRainParams : register(b1)
{
    // Core (32 bytes)
    float2 MousePosition;
    float LayoutMode;           // 0=Fullscreen, 1=Circle, 2=Rectangle
    float Radius;
    float EdgeSoftness;
    float ShapeFeather;
    float RectWidth;
    float RectHeight;

    // Rain settings (32 bytes)
    float FallSpeed;            // 0.5-5.0 speed of falling characters
    float TrailLength;          // 3-20 number of fading characters
    float CharCycleSpeed;       // 0.5-3.0 how fast chars change
    float ColumnDensity;        // 0.3-1.0 percentage of columns active
    float GlowIntensity;        // 0-1 bright leading character glow
    float CellWidth;
    float CellHeight;
    float CharCount;

    // Colors (32 bytes)
    float4 PrimaryColor;        // Main green color
    float4 GlowColor;           // Brighter glow color

    // Brightness (32 bytes)
    float Brightness;
    float Contrast;
    float BackgroundFade;       // 0-1 how much original image shows through
    float _pad1;
    float4 _pad2;
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

float ApplyFlicker(float value)
{
    if (PE_Flicker < 0.5)
        return value;

    float flicker = 0.95 + 0.05 * sin(PE_Time * PE_FlickerSpeed * 60.0);
    flicker *= 0.98 + 0.02 * sin(PE_Time * PE_FlickerSpeed * 17.3);

    return value * flicker;
}

// ============================================================================
// Matrix Rain Functions
// ============================================================================

// Get column properties - each column has unique speed, start time, density
float4 GetColumnProperties(float columnIndex)
{
    float columnHash = hash(float2(columnIndex, 0.0));
    float columnHash2 = hash2(float2(columnIndex, 1.0));

    float speed = FallSpeed * (0.5 + columnHash);           // Varied speed per column
    float startOffset = columnHash2 * 100.0;                // Random start phase
    float isActive = step(1.0 - ColumnDensity, columnHash); // Column activity

    return float4(speed, startOffset, isActive, columnHash);
}

// Calculate the rain drop position for a column at current time
float GetDropPosition(float columnIndex, float time)
{
    float4 props = GetColumnProperties(columnIndex);
    float speed = props.x;
    float offset = props.y;

    // Calculate position cycling through the screen
    float cycleHeight = PE_ViewportSize.y + TrailLength * CellHeight;
    float pos = fmod((time + offset) * speed * CellHeight * 3.0, cycleHeight);

    return pos;
}

// Get character at a specific position (animated)
float GetCharacterAtPosition(float columnIndex, float rowIndex, float time)
{
    // Use hash to get base character
    float charHash = hash(float2(columnIndex * 7.13, rowIndex * 11.31));

    // Animate character changes
    float cycleTime = floor(time * CharCycleSpeed + charHash * 10.0);
    float animHash = hash(float2(columnIndex + cycleTime, rowIndex - cycleTime * 0.3));

    return floor(animHash * CharCount);
}

// Sample character from atlas
float SampleCharacter(float charIndex, float2 cellUV)
{
    float atlasU = (charIndex + cellUV.x) / CharCount;
    float atlasV = cellUV.y;
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
    float2 cellUV = frac(screenPos / cellSize);

    float columnIndex = cellIndex.x;
    float rowIndex = cellIndex.y;

    // Get column properties
    float4 columnProps = GetColumnProperties(columnIndex);
    float isActive = columnProps.z;

    // Start with background
    float3 bgColor = ScreenTexture.SampleLevel(LinearSampler, crtUV, 0).rgb;
    bgColor = bgColor * BackgroundFade * 0.1; // Darken background significantly

    float3 finalColor = bgColor;
    float totalAlpha = 0.0;

    // Only render if column is active
    if (isActive > 0.5)
    {
        // Get drop position
        float dropPos = GetDropPosition(columnIndex, PE_Time);
        float dropRowIndex = floor(dropPos / CellHeight);

        // Calculate distance from current row to drop head
        float distFromHead = dropRowIndex - rowIndex;

        // Check if we're in the trail
        if (distFromHead >= 0.0 && distFromHead <= TrailLength)
        {
            // Get character for this position
            float charIndex = GetCharacterAtPosition(columnIndex, rowIndex, PE_Time);

            // Sample character
            float charAlpha = SampleCharacter(charIndex, cellUV);

            // Calculate fade based on position in trail
            float trailFade = 1.0 - (distFromHead / TrailLength);
            trailFade = pow(trailFade, 0.7); // Adjust falloff curve

            // The head character is brightest
            float isHead = step(distFromHead, 0.5);

            // Apply flicker
            charAlpha = ApplyFlicker(charAlpha);

            // Calculate final character intensity
            float intensity = charAlpha * trailFade;

            // Determine color
            float3 charColor;
            if (isHead > 0.5)
            {
                // Head character - bright glow
                charColor = lerp(PrimaryColor.rgb, GlowColor.rgb, GlowIntensity);
                intensity *= (1.0 + GlowIntensity);
            }
            else
            {
                // Trail characters - primary color with fade
                charColor = PrimaryColor.rgb * trailFade;
            }

            // Apply brightness and contrast
            charColor = (charColor - 0.5) * Contrast + 0.5 + Brightness;

            // Blend with background
            finalColor = lerp(finalColor, charColor, intensity);
            totalAlpha = max(totalAlpha, intensity);
        }
    }

    // Apply scanlines
    float scanlineFactor = ApplyScanlines(screenPos, 1.0);
    finalColor *= scanlineFactor;

    // Apply post-effects
    finalColor = ApplyVignette(crtUV, finalColor);
    finalColor = ApplyNoise(screenPos + PE_Time * 100.0, finalColor);

    // Phosphor glow effect
    if (PE_PhosphorGlow > 0.5 && totalAlpha > 0.1)
    {
        float glowFactor = totalAlpha * PE_PhosphorIntensity;
        finalColor = finalColor * (1.0 + glowFactor * 0.5);
    }

    // Blend with original based on effect mask
    float3 original = ScreenTexture.SampleLevel(LinearSampler, uv, 0).rgb;
    finalColor = lerp(original, finalColor, effectMask);

    return float4(saturate(finalColor), 1.0);
}
