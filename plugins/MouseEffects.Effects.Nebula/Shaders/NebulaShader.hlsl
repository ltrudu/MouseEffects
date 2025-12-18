// NebulaShader.hlsl - Volumetric cosmic gas clouds with twinkling stars

static const float PI = 3.14159265359;
static const float TAU = 6.28318530718;

// Constant buffer
cbuffer Constants : register(b0)
{
    float2 ViewportSize;
    float2 MousePosition;

    float Time;
    float CloudDensity;
    float SwirlSpeed;
    int LayerCount;

    float GlowIntensity;
    float StarDensity;
    float EffectRadius;
    float NoiseScale;

    float ColorVariation;
    float HdrMultiplier;
    int ColorPalette;
    float CloudSpeed;

    float4 CustomColor1;
    float4 CustomColor2;
    float4 CustomColor3;

    float4 PaletteColor1;
    float4 PaletteColor2;
    float4 PaletteColor3;

    float Alpha;
    float GlowAnimationSpeed;
    float Padding1b;
    float Padding1c;
    float4 Padding2;
}

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// ============================================
// Noise Functions for Cloud Generation
// ============================================

// Hash function for pseudo-random numbers
float hash(float2 p)
{
    p = frac(p * float2(443.897, 441.423));
    p += dot(p, p.yx + 19.19);
    return frac(p.x * p.y);
}

float hash13(float3 p3)
{
    p3 = frac(p3 * 0.1031);
    p3 += dot(p3, p3.zyx + 31.32);
    return frac((p3.x + p3.y) * p3.z);
}

// 2D Smooth noise
float noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);

    // Smooth interpolation (smoothstep)
    f = f * f * (3.0 - 2.0 * f);

    // Four corners
    float a = hash(i);
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));

    // Bilinear interpolation
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// 3D Smooth noise for volumetric feel
float noise3d(float3 p)
{
    float3 i = floor(p);
    float3 f = frac(p);

    // Smooth interpolation
    f = f * f * (3.0 - 2.0 * f);

    // Eight corners of cube
    float n000 = hash13(i);
    float n100 = hash13(i + float3(1, 0, 0));
    float n010 = hash13(i + float3(0, 1, 0));
    float n110 = hash13(i + float3(1, 1, 0));
    float n001 = hash13(i + float3(0, 0, 1));
    float n101 = hash13(i + float3(1, 0, 1));
    float n011 = hash13(i + float3(0, 1, 1));
    float n111 = hash13(i + float3(1, 1, 1));

    // Trilinear interpolation
    float nx00 = lerp(n000, n100, f.x);
    float nx10 = lerp(n010, n110, f.x);
    float nx01 = lerp(n001, n101, f.x);
    float nx11 = lerp(n011, n111, f.x);

    float nxy0 = lerp(nx00, nx10, f.y);
    float nxy1 = lerp(nx01, nx11, f.y);

    return lerp(nxy0, nxy1, f.z);
}

// Fractal Brownian Motion for organic cloud patterns
float fbm(float3 p, int octaves)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    for (int i = 0; i < octaves; i++)
    {
        value += amplitude * noise3d(p * frequency);
        amplitude *= 0.5;
        frequency *= 2.0;
    }

    return value;
}

// 2D FBM for additional details
float fbm2d(float2 p, int octaves)
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
// Cloud Generation
// ============================================

// Generate volumetric nebula cloud density
float nebulaCloud(float3 p, float layerOffset)
{
    // Apply swirl motion
    float angle = Time * SwirlSpeed + layerOffset;
    float ca = cos(angle);
    float sa = sin(angle);
    float2 rotated = float2(
        p.x * ca - p.y * sa,
        p.x * sa + p.y * ca
    );

    // 3D position for volumetric noise
    float3 pos3d = float3(rotated * NoiseScale, p.z + Time * CloudSpeed + layerOffset);

    // Multiple octaves of noise for cloud detail
    float cloud = fbm(pos3d, 5);

    // Add turbulence for billowing effect
    float turbulence = fbm(pos3d * 2.0 + float3(Time * 0.2, Time * 0.1, 0), 3) * 0.3;
    cloud += turbulence;

    // Create billowy cloud shapes
    cloud = smoothstep(0.3, 0.8, cloud);

    return cloud * CloudDensity;
}

// ============================================
// Star Generation
// ============================================

// Generate twinkling stars
float stars(float2 p, float seed)
{
    float2 starPos = floor(p) + 0.5;
    float h = hash(starPos + seed);

    // Only create stars at certain positions based on density
    if (h > 1.0 - StarDensity * 0.1)
    {
        float2 localPos = frac(p) - 0.5;
        float dist = length(localPos);

        // Create star with twinkling
        float twinkle = sin(Time * 3.0 + h * TAU) * 0.5 + 0.5;
        float star = exp(-dist * dist * 200.0) * twinkle;

        return star;
    }

    return 0.0;
}

// ============================================
// Color Functions
// ============================================

// Get nebula color based on position and density
float4 getNebulaColor(float2 uv, float density, float layerIndex)
{
    // Create color variation across the nebula
    float colorMix1 = sin(uv.x * ColorVariation * 2.0 + Time * 0.2 + layerIndex) * 0.5 + 0.5;
    float colorMix2 = cos(uv.y * ColorVariation * 3.0 + Time * 0.15 - layerIndex) * 0.5 + 0.5;

    // Blend between three palette colors
    float4 color = lerp(PaletteColor1, PaletteColor2, colorMix1);
    color = lerp(color, PaletteColor3, colorMix2);

    // Add some color variation based on density
    float densityHue = density * 0.3;
    color.rgb = lerp(color.rgb, PaletteColor3.rgb, densityHue);

    return color * density;
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
    float2 uv = relativePos / EffectRadius;

    // Distance from center (mouse cursor)
    float dist = length(uv);

    // Discard pixels outside effect radius
    if (dist > 1.0)
        discard;

    // Radial falloff for nebula (stronger in center)
    float radialFalloff = smoothstep(1.0, 0.3, dist);

    float4 finalColor = float4(0, 0, 0, 0);
    float totalDensity = 0.0;

    // Render multiple overlapping cloud layers for depth
    for (int layer = 0; layer < LayerCount; layer++)
    {
        float layerIndex = float(layer);
        float layerOffset = layerIndex * 1.7;

        // Different depth for each layer (z coordinate)
        float layerDepth = layerIndex * 0.3;

        // Generate cloud density for this layer
        float3 cloudPos = float3(uv, layerDepth);
        float cloudDensity = nebulaCloud(cloudPos, layerOffset);

        // Apply radial falloff
        cloudDensity *= radialFalloff;

        // Get color for this layer
        float4 layerColor = getNebulaColor(uv, cloudDensity, layerIndex);

        // Accumulate with depth-based blending
        float layerDepthFactor = 1.0 - (layerIndex / float(LayerCount));
        layerColor.a = cloudDensity * layerDepthFactor * 0.7;

        // Additive blending for volumetric look
        finalColor.rgb += layerColor.rgb * layerColor.a;
        totalDensity += cloudDensity;
    }

    // Add glow effect around dense areas
    float glowValue = GlowIntensity;
    if (GlowAnimationSpeed > 0.0)
    {
        // Animate glow intensity with a pulsing effect
        float pulse = sin(Time * GlowAnimationSpeed) * 0.5 + 0.5;
        glowValue *= (0.5 + pulse); // Varies between 50% and 150% of base intensity
    }
    float glow = smoothstep(0.0, 0.5, totalDensity) * glowValue * radialFalloff;
    finalColor.rgb *= (1.0 + glow * 0.5);

    // Add twinkling stars
    if (StarDensity > 0.01)
    {
        float2 starUV = uv * 20.0 + Time * 0.1;
        float starField = 0.0;

        // Multiple layers of stars at different scales
        starField += stars(starUV * 1.0, 0.0);
        starField += stars(starUV * 1.5, 10.0) * 0.7;
        starField += stars(starUV * 2.0, 20.0) * 0.5;

        // Stars are white/yellow
        float3 starColor = float3(1.0, 0.95, 0.8);
        finalColor.rgb += starColor * starField * StarDensity * radialFalloff;
    }

    // Calculate final alpha
    finalColor.a = saturate(totalDensity * 0.5 + glow * 0.2);

    // Apply HDR boost for brighter displays
    if (HdrMultiplier > 1.0)
    {
        float brightness = dot(finalColor.rgb, float3(0.299, 0.587, 0.114));
        float hdrBoost = 1.0 + brightness * (HdrMultiplier - 1.0) * 0.6;
        finalColor.rgb *= hdrBoost;
    }

    // Soft edge falloff
    finalColor.a *= smoothstep(1.0, 0.8, dist);

    // Apply global alpha
    finalColor *= Alpha;

    // Discard fully transparent pixels
    if (finalColor.a < 0.01)
        discard;

    return finalColor;
}
