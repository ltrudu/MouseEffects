// Metaball Ink Shader - Organic ink drops with physics simulation
// Uses metaball implicit surfaces for smooth merging

cbuffer Constants : register(b0)
{
    float2 ViewportSize;
    float Time;
    int ActiveDropCount;

    float MetaballThreshold;
    float EdgeSoftness;
    float HdrMultiplier;
    float Opacity;

    float4 InkColor;

    float GlowIntensity;
    float InnerDarkening;
    int ColorMode;        // 0-3 = solid colors, 4 = rainbow
    float RainbowSpeed;

    int AnimateGlow;      // 0 = static, 1 = animated
    float GlowMin;
    float GlowMax;
    float GlowAnimSpeed;

    float4 Padding;
};

struct InkDrop
{
    float2 Position;
    float2 Velocity;

    float Radius;
    float Age;
    float MaxAge;
    float Seed;
};

StructuredBuffer<InkDrop> Drops : register(t0);

struct VSOutput
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
};

// Fullscreen triangle vertex shader
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

// Simple hash for noise
float hash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

// 2D noise
float noise(float2 p)
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

// FBM for texture
float fbm(float2 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    for (int i = 0; i < 3; i++)
    {
        value += amplitude * noise(p);
        p *= 2.0;
        amplitude *= 0.5;
    }
    return value;
}

// HSV to RGB conversion for rainbow effect
float3 hsv2rgb(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}

// Get rainbow color based on time
float3 getRainbowColor(float time, float speed)
{
    float hue = frac(time * speed * 0.1);
    return hsv2rgb(float3(hue, 0.9, 0.8)); // High saturation, good brightness
}

// Calculate metaball field value at a point
float calculateMetaballField(float2 pixelPos)
{
    float field = 0.0;

    for (int i = 0; i < ActiveDropCount; i++)
    {
        InkDrop drop = Drops[i];

        float2 delta = pixelPos - drop.Position;
        float distSq = dot(delta, delta);

        // Avoid division by zero
        if (distSq < 0.1) distSq = 0.1;

        // Metaball contribution: r^2 / d^2
        float radiusSq = drop.Radius * drop.Radius;
        field += radiusSq / distSq;
    }

    return field;
}

// Calculate gradient for normal estimation (for glow effect)
float2 calculateGradient(float2 pixelPos)
{
    float eps = 1.0;
    float fx = calculateMetaballField(pixelPos + float2(eps, 0)) - calculateMetaballField(pixelPos - float2(eps, 0));
    float fy = calculateMetaballField(pixelPos + float2(0, eps)) - calculateMetaballField(pixelPos - float2(0, eps));
    return normalize(float2(fx, fy) + 0.0001);
}

float4 PSMain(VSOutput input) : SV_Target
{
    // Convert to pixel coordinates
    float2 pixelPos = input.TexCoord * ViewportSize;

    // Calculate metaball field
    float field = calculateMetaballField(pixelPos);

    // No drops nearby - fully transparent
    if (field < MetaballThreshold * 0.1)
    {
        return float4(0, 0, 0, 0);
    }

    // Smooth edge using threshold and softness
    float edgeStart = MetaballThreshold * (1.0 - EdgeSoftness);
    float edgeEnd = MetaballThreshold * (1.0 + EdgeSoftness * 0.5);
    float inkAmount = smoothstep(edgeStart, edgeEnd, field);

    // Inside the ink blob - choose color based on mode
    float3 color;
    if (ColorMode == 4) // Rainbow mode
    {
        color = getRainbowColor(Time, RainbowSpeed);
    }
    else
    {
        color = InkColor.rgb;
    }

    // Add inner darkening for depth (darker in center where field is higher)
    float innerDepth = saturate((field - MetaballThreshold) / (MetaballThreshold * 2.0));
    color *= 1.0 - innerDepth * InnerDarkening;

    // Add subtle texture variation
    float textureNoise = fbm(pixelPos * 0.02 + Time * 0.1) * 0.15;
    color *= 1.0 - textureNoise;

    // Glow effect around edges
    float glowField = saturate(1.0 - abs(field - MetaballThreshold) / (MetaballThreshold * EdgeSoftness * 2.0));

    // Calculate glow intensity (animated or static)
    float currentGlow;
    if (AnimateGlow == 1)
    {
        // Animate glow between min and max using sine wave
        float glowPhase = sin(Time * GlowAnimSpeed) * 0.5 + 0.5; // 0 to 1
        currentGlow = lerp(GlowMin, GlowMax, glowPhase);
    }
    else
    {
        currentGlow = GlowIntensity;
    }

    float glow = glowField * currentGlow;

    // Apply glow as slight brightening
    color += glow * 0.3;

    // Apply HDR multiplier
    color *= HdrMultiplier;

    // Calculate alpha with fade at edges
    float alpha = inkAmount * Opacity;

    // Add slight transparency variation for watercolor feel
    alpha *= 0.9 + fbm(pixelPos * 0.03) * 0.1;

    return float4(color, alpha);
}
