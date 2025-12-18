// InkBlot Shader - Organic ink blots with noise-based edges

cbuffer Constants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float EdgeIrregularity;

    float Opacity;
    int ActiveBlotCount;
    float HdrMultiplier;
    float Padding1;
};

struct BlotInstance
{
    float2 Position;
    float CurrentRadius;
    float MaxRadius;

    float BirthTime;
    float Lifetime;
    float Age;
    float Seed;

    float4 Color;

    float SpreadSpeed;
    float Padding1;
    float Padding2;
    float Padding3;
};

StructuredBuffer<BlotInstance> Blots : register(t0);

struct VSOutput
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
    float2 BillboardPos : TEXCOORD1;
    float Radius : TEXCOORD2;
    float Seed : TEXCOORD3;
    float Age : TEXCOORD4;
    float Lifetime : TEXCOORD5;
};

// Simple hash function for noise
float hash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.13);
    p3 += dot(p3, p3.yzx + 3.333);
    return frac((p3.x + p3.y) * p3.z);
}

// 2D noise function
float noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);

    // Smooth interpolation
    f = f * f * (3.0 - 2.0 * f);

    // 4 corners
    float a = hash(i);
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));

    // Bilinear interpolation
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// Fractional Brownian Motion (fbm) for organic edges
float fbm(float2 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    for (int i = 0; i < 4; i++)
    {
        value += amplitude * noise(p * frequency);
        frequency *= 2.0;
        amplitude *= 0.5;
    }

    return value;
}

VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;

    // Get blot data
    BlotInstance blot = Blots[instanceId];

    // Generate quad vertices (0,0), (1,0), (0,1), (1,0), (0,1), (1,1)
    float2 quadPos;
    quadPos.x = (vertexId == 1 || vertexId == 3 || vertexId == 5) ? 1.0 : 0.0;
    quadPos.y = (vertexId == 2 || vertexId == 4 || vertexId == 5) ? 1.0 : 0.0;

    // Billboard quad centered on blot
    float quadSize = blot.MaxRadius * 2.2; // Extra space for noise displacement
    float2 offset = (quadPos - 0.5) * quadSize;
    float2 worldPos = blot.Position + offset;

    // Convert to NDC
    float2 ndc = (worldPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y;

    output.Position = float4(ndc, 0.0, 1.0);
    output.TexCoord = quadPos;
    output.Color = blot.Color;
    output.BillboardPos = quadPos - 0.5; // -0.5 to 0.5
    output.Radius = blot.CurrentRadius;
    output.Seed = blot.Seed;
    output.Age = blot.Age;
    output.Lifetime = blot.Lifetime;

    return output;
}

float4 PSMain(VSOutput input) : SV_Target
{
    // Distance from center
    float2 centerOffset = input.BillboardPos * input.Radius * 2.2;
    float distFromCenter = length(centerOffset);

    // Angle for noise variation
    float angle = atan2(centerOffset.y, centerOffset.x);

    // Create organic edge using noise
    float noiseInput = angle * 3.0 + input.Seed;
    float edgeNoise = fbm(float2(noiseInput, input.Seed * 0.1)) * 2.0 - 1.0;

    // Apply irregularity
    float radiusModulation = input.Radius * (1.0 + edgeNoise * EdgeIrregularity);

    // Signed distance field for the blot
    float sdf = distFromCenter - radiusModulation;

    // Soft edge for watercolor effect
    float edgeSoftness = input.Radius * 0.15;
    float alpha = smoothstep(edgeSoftness, -edgeSoftness, sdf);

    // Fade out at end of lifetime
    float lifetimeProgress = input.Age / input.Lifetime;
    float fadeOut = 1.0 - smoothstep(0.7, 1.0, lifetimeProgress);

    // Inner variation for texture
    float innerNoise = fbm(centerOffset * 0.1 + input.Seed) * 0.3;
    float3 color = input.Color.rgb * (1.0 - innerNoise * 0.2);

    // Apply HDR multiplier for bright displays
    color *= HdrMultiplier;

    // Final alpha
    alpha *= Opacity * fadeOut;

    return float4(color, alpha);
}
