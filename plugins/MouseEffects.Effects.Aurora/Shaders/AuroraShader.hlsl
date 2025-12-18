// AuroraShader.hlsl - Beautiful northern lights ribbons with organic motion

static const float PI = 3.14159265359;
static const float TAU = 6.28318530718;

// Constant buffer
cbuffer Constants : register(b0)
{
    float2 ViewportSize;
    float2 MousePosition;

    float Time;
    float Height;
    float HorizontalSpread;
    float WaveSpeed;

    float WaveFrequency;
    int NumLayers;
    float ColorIntensity;
    float GlowStrength;

    float NoiseScale;
    float NoiseStrength;
    float VerticalFlow;
    float HdrMultiplier;

    float4 PrimaryColor;
    float4 SecondaryColor;
    float4 TertiaryColor;
    float4 AccentColor;

    float4 Padding;
}

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// ============================================
// Noise Functions
// ============================================

// Hash function for pseudo-random numbers
float hash(float2 p)
{
    p = frac(p * float2(443.897, 441.423));
    p += dot(p, p.yx + 19.19);
    return frac(p.x * p.y);
}

// Smooth noise
float noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);

    // Smooth interpolation
    f = f * f * (3.0 - 2.0 * f);

    // Four corners
    float a = hash(i);
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));

    // Bilinear interpolation
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// Fractal Brownian Motion for organic patterns
float fbm(float2 p, int octaves)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    for (int i = 0; i < octaves; i++)
    {
        value += amplitude * noise(p * frequency);
        amplitude *= 0.5;
        frequency *= 2.0;
    }

    return value;
}

// ============================================
// Aurora Generation
// ============================================

// Generate aurora ribbon pattern
float auroraPattern(float2 uv, float layerOffset, float time)
{
    // Vertical coordinate with offset from mouse
    float y = uv.y;

    // Create base wave pattern
    float wave = 0.0;

    // Multiple sine waves for complex movement
    wave += sin(uv.x * WaveFrequency + time * WaveSpeed + layerOffset) * 0.3;
    wave += sin(uv.x * WaveFrequency * 1.7 + time * WaveSpeed * 1.3 - layerOffset) * 0.2;
    wave += sin(uv.x * WaveFrequency * 0.5 + time * WaveSpeed * 0.7 + layerOffset * 2.0) * 0.15;

    // Add organic noise distortion
    float2 noiseCoord = float2(uv.x * NoiseScale, y * NoiseScale * 0.5 + time * VerticalFlow);
    float noiseValue = fbm(noiseCoord + float2(time * 0.1, layerOffset), 4);
    wave += (noiseValue - 0.5) * NoiseStrength;

    // Distance from wave center
    float dist = abs(y - wave);

    // Vertical falloff (aurora fades at top and bottom)
    float verticalFalloff = smoothstep(1.0, 0.0, abs(y) * 0.8);

    // Create ribbon shape with smooth edges
    float ribbonWidth = 0.15 + sin(time * 0.5 + layerOffset) * 0.05;
    float ribbon = exp(-dist * dist / (ribbonWidth * ribbonWidth));

    return ribbon * verticalFalloff;
}

// Get color for aurora based on position and layer
float4 getAuroraColor(float2 uv, float layerIndex, float intensity)
{
    // Color variation based on position
    float colorMix = sin(uv.x * 2.0 + Time * 0.3 + layerIndex) * 0.5 + 0.5;

    // Blend between colors based on position and time
    float4 color1 = lerp(PrimaryColor, SecondaryColor, colorMix);
    float4 color2 = lerp(TertiaryColor, AccentColor, 1.0 - colorMix);

    // Layer-based color variation
    float layerMix = layerIndex / max(float(NumLayers), 1.0);
    float4 finalColor = lerp(color1, color2, layerMix);

    // Add shimmering effect
    float shimmer = 1.0 + sin(Time * 4.0 + uv.x * 10.0 + layerIndex * 3.0) * 0.15;

    return finalColor * intensity * shimmer * ColorIntensity;
}

// ============================================
// Vertex Shader
// ============================================

VSOutput VSMain(uint vertexId : SV_VertexID)
{
    VSOutput output;

    // Fullscreen triangle
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y;
    output.TexCoord = uv;

    return output;
}

// ============================================
// Pixel Shader
// ============================================

float4 PSMain(VSOutput input) : SV_TARGET
{
    // Convert to world space centered on mouse
    float2 pixelPos = input.TexCoord * ViewportSize;
    float2 relativePos = pixelPos - MousePosition;

    // Normalize coordinates
    float2 uv;
    uv.x = relativePos.x / HorizontalSpread;
    uv.y = relativePos.y / Height;

    // Discard pixels outside the effect area
    if (abs(uv.x) > 1.0 || abs(uv.y) > 1.0)
        discard;

    float4 finalColor = float4(0, 0, 0, 0);

    // Render multiple overlapping aurora layers
    for (int layer = 0; layer < NumLayers; layer++)
    {
        float layerIndex = float(layer);
        float layerOffset = layerIndex * 1.5;

        // Generate aurora pattern for this layer
        float auroraIntensity = auroraPattern(uv, layerOffset, Time);

        // Get color for this layer
        float4 layerColor = getAuroraColor(uv, layerIndex, auroraIntensity);

        // Add glow effect
        float glowRadius = 0.3 + sin(Time + layerOffset) * 0.1;
        float distFromCenter = length(uv);
        float glow = exp(-distFromCenter * distFromCenter / (glowRadius * glowRadius)) * GlowStrength * 0.2;

        // Combine ribbon and glow
        layerColor.rgb *= (1.0 + glow);
        layerColor.a = auroraIntensity * (1.0 + glow * 0.5);

        // Additive blending for layers
        finalColor.rgb += layerColor.rgb * layerColor.a;
        finalColor.a = max(finalColor.a, layerColor.a);
    }

    // Apply HDR boost for brighter displays
    if (HdrMultiplier > 1.0)
    {
        float brightness = dot(finalColor.rgb, float3(0.299, 0.587, 0.114));
        float hdrBoost = 1.0 + brightness * (HdrMultiplier - 1.0) * 0.5;
        finalColor.rgb *= hdrBoost;
    }

    // Ensure alpha is not too high (for proper blending)
    finalColor.a = saturate(finalColor.a * 0.8);

    // Discard fully transparent pixels
    if (finalColor.a < 0.01)
        discard;

    return finalColor;
}
