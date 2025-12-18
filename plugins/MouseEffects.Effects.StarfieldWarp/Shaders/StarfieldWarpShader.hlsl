// StarfieldWarpShader.hlsl - Hyperspace/warp speed starfield effect
// Creates stars streaking outward from cursor with depth layers and parallax

static const float PI = 3.14159265359;
static const float TAU = 6.28318530718;

// Constant buffer
cbuffer Constants : register(b0)
{
    float2 ViewportSize;
    float2 MousePosition;

    float Time;
    int StarCount;
    float WarpSpeed;
    float StreakLength;

    float EffectRadius;
    float StarBrightness;
    float ColorTintEnabled;
    float TunnelEffect;

    float4 ColorTint;

    float TunnelDarkness;
    float StarSize;
    int DepthLayers;
    float PulseEffect;

    float PulseSpeed;
    float Padding1;
    float Padding2;
    float Padding3;
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// ============================================
// Utility Functions
// ============================================

// Hash function for pseudo-random values
float Hash(float n)
{
    return frac(sin(n) * 43758.5453123);
}

float Hash2D(float2 p)
{
    float h = dot(p, float2(127.1, 311.7));
    return frac(sin(h) * 43758.5453123);
}

// Generate a 2D pseudo-random vector
float2 Hash22(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)),
               dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453123);
}

// ============================================
// Star Generation and Rendering
// ============================================

// Generate star properties from index
void GenerateStar(int starIndex, int layer, out float2 direction, out float distance, out float speed, out float size, out float brightness)
{
    // Use star index and layer to generate unique random values
    float seed = float(starIndex) + float(layer) * 1000.0;

    // Random angle for direction
    float angle = Hash(seed) * TAU;
    direction = float2(cos(angle), sin(angle));

    // Random distance - varies per layer for depth
    float baseDistance = Hash(seed + 1.0);
    distance = baseDistance;

    // Speed varies by layer (closer layers move faster for parallax)
    float layerSpeed = 1.0 + float(layer) * 0.5;
    speed = WarpSpeed * layerSpeed * (0.8 + Hash(seed + 2.0) * 0.4);

    // Random size variation
    size = StarSize * (0.5 + Hash(seed + 3.0) * 1.0);

    // Random brightness variation
    brightness = 0.6 + Hash(seed + 4.0) * 0.8;
}

// Calculate star intensity at a given position
float CalculateStarIntensity(float2 starPos, float2 pixelPos, float2 velocity, float starSize)
{
    float2 toPixel = pixelPos - starPos;

    // Calculate distance from pixel to star center
    float dist = length(toPixel);

    // Streak direction (opposite of velocity)
    float2 streakDir = normalize(velocity);

    // Project pixel position onto streak line
    float alongStreak = dot(toPixel, -streakDir);
    float perpStreak = length(toPixel - (-streakDir * alongStreak));

    // Create elongated star with streak
    float streakDist = length(velocity) * StreakLength * 100.0;

    // Core point (circular)
    float coreIntensity = exp(-dist * dist / (starSize * starSize));

    // Streak (elongated in velocity direction)
    float streakIntensity = 0.0;
    if (alongStreak > 0.0 && alongStreak < streakDist)
    {
        float streakWidth = starSize * 0.5;
        streakIntensity = exp(-perpStreak * perpStreak / (streakWidth * streakWidth));
        streakIntensity *= smoothstep(streakDist, 0.0, alongStreak);
    }

    return max(coreIntensity, streakIntensity * 0.6);
}

// ============================================
// Vertex Shader - Fullscreen triangle
// ============================================

VSOutput VSMain(uint vertexId : SV_VertexID)
{
    VSOutput output;

    // Generate fullscreen triangle
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y;
    output.TexCoord = uv;

    return output;
}

// ============================================
// Pixel Shader - Starfield Warp Effect
// ============================================

float4 PSMain(VSOutput input) : SV_TARGET
{
    float2 screenPos = input.TexCoord * ViewportSize;
    float2 fromCenter = screenPos - MousePosition;

    float distFromCenter = length(fromCenter);

    // Early out if outside effect radius
    if (distFromCenter > EffectRadius)
        discard;

    // Normalized distance and direction
    float normDist = distFromCenter / EffectRadius;
    float2 dirFromCenter = normalize(fromCenter);

    float3 finalColor = float3(0, 0, 0);
    float totalIntensity = 0.0;

    // Render stars for each depth layer
    int starsPerLayer = StarCount / max(DepthLayers, 1);

    for (int layer = 0; layer < DepthLayers; layer++)
    {
        for (int i = 0; i < starsPerLayer; i++)
        {
            int starIndex = layer * starsPerLayer + i;

            // Generate star properties
            float2 direction;
            float distance;
            float speed;
            float starSize;
            float brightness;
            GenerateStar(starIndex, layer, direction, distance, speed, starSize, brightness);

            // Animate distance based on time and speed
            float animatedDist = frac(distance + Time * speed * 0.1);

            // Convert to screen space position
            float starDist = animatedDist * EffectRadius;
            float2 starPos = MousePosition + direction * starDist;

            // Calculate velocity for streak
            float2 velocity = direction * speed * WarpSpeed;

            // Calculate star intensity at this pixel
            float intensity = CalculateStarIntensity(starPos, screenPos, velocity, starSize);

            if (intensity > 0.001)
            {
                // Fade in stars as they appear
                float fadeIn = smoothstep(0.0, 0.1, animatedDist);

                // Fade out stars as they approach edge
                float fadeOut = smoothstep(1.0, 0.8, animatedDist);

                // Combine fades
                intensity *= fadeIn * fadeOut;

                // Apply pulse effect if enabled
                if (PulseEffect > 0.5)
                {
                    float pulsePhase = Time * PulseSpeed + float(starIndex) * 0.1;
                    float pulse = 0.7 + 0.3 * sin(pulsePhase * TAU);
                    intensity *= pulse;
                }

                // Apply brightness variation
                intensity *= brightness * StarBrightness;

                // Depth-based brightness (closer stars brighter)
                float depthBrightness = 1.0 - float(layer) / float(max(DepthLayers, 1)) * 0.5;
                intensity *= depthBrightness;

                totalIntensity += intensity;

                // Base star color (white with optional tint)
                float3 starColor = float3(1, 1, 1);

                // Apply color tint if enabled
                if (ColorTintEnabled > 0.5)
                {
                    // Blue shift for hyperspace effect
                    starColor = lerp(starColor, ColorTint.rgb, 0.6);

                    // Add slight color variation based on distance
                    starColor = lerp(starColor, ColorTint.rgb * 1.2, animatedDist * 0.3);
                }

                finalColor += starColor * intensity;
            }
        }
    }

    // Apply tunnel effect (darken center)
    if (TunnelEffect > 0.5)
    {
        // Radial gradient from center
        float tunnelMask = smoothstep(0.0, 0.5, normDist);
        float tunnelFactor = lerp(TunnelDarkness, 1.0, tunnelMask);
        finalColor *= tunnelFactor;
    }

    // Calculate alpha based on total intensity
    float alpha = saturate(totalIntensity);

    // Fade at edges of effect radius
    float edgeFade = smoothstep(1.0, 0.7, normDist);
    alpha *= edgeFade;

    // Discard fully transparent pixels
    if (alpha < 0.01)
        discard;

    return float4(finalColor, alpha);
}
