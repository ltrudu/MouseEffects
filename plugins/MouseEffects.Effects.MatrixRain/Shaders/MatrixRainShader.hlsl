// Matrix Rain Effect Shader
// Procedurally generates falling green code characters around the cursor

cbuffer Constants : register(b0)
{
    float2 ViewportSize;
    float2 MousePosition;
    float Time;
    float ColumnDensity;
    float FallSpeed;
    float CharChangeRate;
    float GlowIntensity;
    float TrailLength;
    float EffectRadius;
    float HdrMultiplier;
    float4 Color;
}

struct VSOutput
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
};

// Fullscreen triangle vertex shader
VSOutput VSMain(uint vertexId : SV_VertexID)
{
    VSOutput output;
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y;
    output.TexCoord = uv;
    return output;
}

// Hash function for pseudo-random values
float hash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

// Generate a random character pattern (katakana-like or matrix-style)
// Returns 1 if pixel should be lit, 0 otherwise
float getCharacter(float2 charUV, float charIndex)
{
    // Normalize to 0-1 within the character cell
    charUV = frac(charUV);

    // Character selection based on index
    float charType = frac(charIndex * 0.618);

    // Create different character patterns using simple geometry
    float pattern = 0.0;

    if (charType < 0.2)
    {
        // Vertical line
        pattern = step(abs(charUV.x - 0.5), 0.15);
    }
    else if (charType < 0.4)
    {
        // Horizontal line
        pattern = step(abs(charUV.y - 0.5), 0.15);
    }
    else if (charType < 0.6)
    {
        // Cross pattern
        float vert = step(abs(charUV.x - 0.5), 0.12);
        float horiz = step(abs(charUV.y - 0.5), 0.12);
        pattern = max(vert, horiz);
    }
    else if (charType < 0.75)
    {
        // Diagonal line
        float dist = abs(charUV.x - charUV.y);
        pattern = step(dist, 0.15);
    }
    else if (charType < 0.85)
    {
        // L shape
        float vert = step(abs(charUV.x - 0.3), 0.12) * step(charUV.y, 0.8);
        float horiz = step(abs(charUV.y - 0.8), 0.12) * step(0.3, charUV.x);
        pattern = max(vert, horiz);
    }
    else
    {
        // T shape
        float vert = step(abs(charUV.x - 0.5), 0.12);
        float horiz = step(abs(charUV.y - 0.3), 0.12) * step(0.3, charUV.x) * step(charUV.x, 0.7);
        pattern = max(vert, horiz);
    }

    return pattern;
}

float4 PSMain(VSOutput input) : SV_Target
{
    float2 pixelPos = input.TexCoord * ViewportSize;

    // Distance from mouse cursor
    float distFromMouse = length(pixelPos - MousePosition);

    // Only render within effect radius
    if (distFromMouse > EffectRadius)
        discard;

    // Fade at edges of radius
    float radiusFade = smoothstep(EffectRadius, EffectRadius * 0.7, distFromMouse);

    // Character cell size (pixels)
    float cellSize = 20.0;

    // Determine which column this pixel belongs to
    float columnWidth = 1.0 / ColumnDensity;
    float2 columnPos = float2(floor(pixelPos.x / columnWidth), 0);

    // Column seed for randomization
    float columnSeed = hash(columnPos);

    // Column fall speed varies per column
    float columnSpeed = FallSpeed * (0.5 + columnSeed);

    // Column starting offset (different start positions)
    float columnOffset = hash(columnPos + float2(1.234, 5.678)) * 1000.0;

    // Current fall position
    float fallPos = fmod(Time * columnSpeed + columnOffset, ViewportSize.y + 500.0);

    // Vertical position relative to the falling head
    float relativeY = pixelPos.y - fallPos;

    // Trail length in pixels
    float trailPixels = 400.0 * TrailLength;

    // Character is only visible if we're in the trail
    if (relativeY < -50.0 || relativeY > trailPixels)
        discard;

    // Position within character grid
    float2 charGridPos = pixelPos / cellSize;
    charGridPos.y += Time * columnSpeed / cellSize;

    // Character cell coordinates
    float2 charCell = floor(charGridPos);
    float2 charUV = frac(charGridPos);

    // Character index (changes over time)
    float charTime = Time * CharChangeRate;
    float charIndex = hash(charCell + floor(charTime));

    // Get character pattern
    float charPattern = getCharacter(charUV, charIndex);

    if (charPattern < 0.5)
        discard;

    // Calculate trail fade
    float trailFade = 1.0;
    if (relativeY > 0)
    {
        trailFade = 1.0 - smoothstep(0.0, trailPixels, relativeY);
    }
    else
    {
        // Leading character is brighter
        trailFade = 1.5;
    }

    // Distance from character center for glow
    float2 charCenter = (floor(charGridPos) + 0.5) * cellSize;
    float distFromCenter = length(pixelPos - charCenter) / cellSize;
    float glow = exp(-distFromCenter * 3.0) * GlowIntensity;

    // Final intensity
    float intensity = trailFade * (0.8 + glow * 0.5);

    // Apply fade at radius edges
    intensity *= radiusFade;

    // Final color with HDR support
    float4 finalColor = Color * intensity;

    // Apply HDR multiplier for bright highlights
    if (trailFade > 1.0)
    {
        finalColor.rgb *= HdrMultiplier;
    }

    return finalColor;
}
