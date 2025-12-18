// Paint Splatter Shader - Jackson Pollock style artistic paint drops

cbuffer Constants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float EdgeNoisiness;
    float Opacity;
    int EnableDrips;
    float Padding1;
};

struct SplatParticle
{
    float2 Position;
    float MainRadius;
    float Lifetime;
    float MaxLifetime;
    float BirthTime;
    float Seed;
    int DropletCount;
    float4 Color;
    float DripLength;
    float DripSpeed;
    float CurrentDripLength;
    float Padding1;
};

StructuredBuffer<SplatParticle> Splats : register(t0);

struct VSOutput
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
    float2 BillboardPos : TEXCOORD1;
    float MainRadius : TEXCOORD2;
    float Seed : TEXCOORD3;
    float Lifetime : TEXCOORD4;
    float MaxLifetime : TEXCOORD5;
    float2 SplatCenter : TEXCOORD6;
    int DropletCount : TEXCOORD7;
    float DripLength : TEXCOORD8;
};

// Hash function for noise
float hash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.13);
    p3 += dot(p3, p3.yzx + 3.333);
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

// Fractional Brownian Motion for organic paint texture
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

    SplatParticle splat = Splats[instanceId];

    // Skip inactive splats
    if (splat.Lifetime <= 0.0)
    {
        output.Position = float4(0, 0, 0, 0);
        output.TexCoord = float2(0, 0);
        output.Color = float4(0, 0, 0, 0);
        output.BillboardPos = float2(0, 0);
        output.MainRadius = 0.0;
        output.Seed = 0.0;
        output.Lifetime = 0.0;
        output.MaxLifetime = 1.0;
        output.SplatCenter = float2(0, 0);
        output.DropletCount = 0;
        output.DripLength = 0.0;
        return output;
    }

    // Generate quad vertices
    float2 quadPos;
    quadPos.x = (vertexId == 1 || vertexId == 3 || vertexId == 5) ? 1.0 : 0.0;
    quadPos.y = (vertexId == 2 || vertexId == 4 || vertexId == 5) ? 1.0 : 0.0;

    // Billboard quad needs to encompass main splat + droplets + drips
    float maxSpread = splat.MainRadius * 3.0; // Space for droplets
    float dripSpace = EnableDrips ? splat.CurrentDripLength : 0.0;
    float2 quadSize = float2(maxSpread * 2.0, maxSpread * 2.0 + dripSpace);

    // Offset quad to center main splat but extend down for drips
    float2 offset = (quadPos - float2(0.5, 0.3)) * quadSize;
    float2 worldPos = splat.Position + offset;

    // Convert to NDC
    float2 ndc = (worldPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y;

    output.Position = float4(ndc, 0.0, 1.0);
    output.TexCoord = quadPos;
    output.Color = splat.Color;
    output.BillboardPos = quadPos - float2(0.5, 0.3);
    output.MainRadius = splat.MainRadius;
    output.Seed = splat.Seed;
    output.Lifetime = splat.Lifetime;
    output.MaxLifetime = splat.MaxLifetime;
    output.SplatCenter = float2(0, 0); // Center in billboard space
    output.DropletCount = splat.DropletCount;
    output.DripLength = splat.CurrentDripLength;

    return output;
}

float4 PSMain(VSOutput input) : SV_Target
{
    if (input.Lifetime <= 0.0)
        discard;

    float maxSpread = input.MainRadius * 3.0;
    float dripSpace = EnableDrips ? input.DripLength : 0.0;
    float2 quadSize = float2(maxSpread * 2.0, maxSpread * 2.0 + dripSpace);
    float2 pixelPos = input.BillboardPos * quadSize;

    float alpha = 0.0;
    float3 color = input.Color.rgb;

    // Main splat with irregular edges
    float2 mainOffset = pixelPos - input.SplatCenter;
    float distFromCenter = length(mainOffset);
    float angle = atan2(mainOffset.y, mainOffset.x);

    // Organic edge using noise
    float edgeNoise = fbm(float2(angle * 3.0 + input.Seed, input.Seed * 0.1)) * 2.0 - 1.0;
    float radiusModulation = input.MainRadius * (1.0 + edgeNoise * EdgeNoisiness);

    // Main splat SDF
    float mainSDF = distFromCenter - radiusModulation;
    float mainAlpha = smoothstep(5.0, -2.0, mainSDF);
    alpha = max(alpha, mainAlpha);

    // Paint texture variation
    if (mainAlpha > 0.01)
    {
        float paintTexture = fbm(mainOffset * 0.1 + input.Seed);
        color *= 1.0 - paintTexture * 0.15;
    }

    // Droplets radiating outward
    static const float PI = 3.14159265;
    for (int i = 0; i < input.DropletCount; i++)
    {
        float dropletAngle = (float(i) / float(input.DropletCount)) * 2.0 * PI;
        float dropletDist = input.MainRadius * 1.3 + hash(float2(input.Seed + float(i), 0)) * input.MainRadius * 0.8;

        float2 dropletCenter = float2(
            cos(dropletAngle) * dropletDist,
            sin(dropletAngle) * dropletDist
        );

        float dropletRadius = input.MainRadius * 0.15 * (0.7 + hash(float2(input.Seed, float(i))) * 0.6);
        float dropletDist2 = length(pixelPos - dropletCenter);

        // Irregular droplet edge
        float dropletNoise = noise(float2(dropletAngle * 5.0, input.Seed + float(i))) * 2.0 - 1.0;
        float dropletRadiusMod = dropletRadius * (1.0 + dropletNoise * EdgeNoisiness * 0.5);

        float dropletSDF = dropletDist2 - dropletRadiusMod;
        float dropletAlpha = smoothstep(2.0, -1.0, dropletSDF);
        alpha = max(alpha, dropletAlpha);
    }

    // Drip trails running down
    if (EnableDrips && input.DripLength > 0.0)
    {
        // Multiple drip trails
        int dripCount = 3;
        for (int j = 0; j < dripCount; j++)
        {
            float dripAngleOffset = (float(j) - 1.0) * 0.3;
            float dripX = sin(dripAngleOffset) * input.MainRadius * 0.5;
            float dripY = pixelPos.y;

            // Drip only below the main splat
            if (dripY > input.MainRadius * 0.3)
            {
                float dripStart = input.MainRadius * 0.3;
                float dripEnd = dripStart + input.DripLength;

                if (dripY >= dripStart && dripY <= dripEnd)
                {
                    float dripProgress = (dripY - dripStart) / input.DripLength;
                    float dripWidth = input.MainRadius * 0.1 * (1.0 - dripProgress * 0.7);

                    // Wiggle in the drip
                    float dripWiggle = noise(float2(dripY * 0.1, input.Seed + float(j))) * dripWidth * 0.5;
                    float dripCenterX = dripX + dripWiggle;

                    float distFromDrip = abs(pixelPos.x - dripCenterX);
                    float dripAlpha = smoothstep(dripWidth, dripWidth * 0.5, distFromDrip);

                    // Fade out towards the tip
                    dripAlpha *= 1.0 - smoothstep(0.7, 1.0, dripProgress);

                    alpha = max(alpha, dripAlpha * 0.8);
                }
            }
        }
    }

    // Fade out at end of lifetime
    float lifetimeProgress = 1.0 - (input.Lifetime / input.MaxLifetime);
    float fadeOut = 1.0 - smoothstep(0.8, 1.0, lifetimeProgress);

    // Apply HDR multiplier
    color *= HdrMultiplier;

    // Final alpha
    alpha *= Opacity * fadeOut;

    if (alpha < 0.01)
        discard;

    return float4(color, alpha);
}
