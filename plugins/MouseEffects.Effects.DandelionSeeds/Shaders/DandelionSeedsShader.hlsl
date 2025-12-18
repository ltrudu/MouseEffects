// Dandelion Seeds Shader - Delicate floating seeds with pappus (fluffy parachute)

cbuffer FrameConstants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float4 Padding;
}

struct SeedInstance
{
    float2 Position;
    float2 Velocity;
    float4 Color;
    float Size;
    float Lifetime;
    float MaxLifetime;
    float RotationAngle;
    float RotationSpeed;
    float WindPhase;
    float GlowIntensity;
    float PappusPhase;
    float UpwardDrift;
    float Opacity;
    float Padding1;
    float Padding2;
};

StructuredBuffer<SeedInstance> Seeds : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR0;
    float Alpha : TEXCOORD1;
    float GlowIntensity : TEXCOORD2;
    float PappusPhase : TEXCOORD3;
};

// Vertex shader - Generate quad per seed instance
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;
    SeedInstance seed = Seeds[instanceId];

    // Skip dead seeds
    if (seed.Lifetime <= 0)
    {
        output.Position = float4(0, 0, 0, 0);
        output.UV = float2(0, 0);
        output.Color = float4(0, 0, 0, 0);
        output.Alpha = 0;
        output.GlowIntensity = 0;
        output.PappusPhase = 0;
        return output;
    }

    // Generate quad vertices (two triangles)
    float2 quadUV;
    if (vertexId == 0) quadUV = float2(-1, -1);
    else if (vertexId == 1) quadUV = float2(1, -1);
    else if (vertexId == 2) quadUV = float2(-1, 1);
    else if (vertexId == 3) quadUV = float2(-1, 1);
    else if (vertexId == 4) quadUV = float2(1, -1);
    else quadUV = float2(1, 1);

    // Apply rotation
    float c = cos(seed.RotationAngle);
    float s = sin(seed.RotationAngle);
    float2x2 rotation = float2x2(c, -s, s, c);
    float2 rotatedUV = mul(rotation, quadUV);

    // Scale by seed size
    float2 offset = rotatedUV * seed.Size;

    // Position in screen space
    float2 screenPos = seed.Position + offset;

    // Convert to NDC
    float2 ndc = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y; // Flip Y for DirectX

    output.Position = float4(ndc, 0, 1);
    output.UV = quadUV; // Keep unrotated UV for SDF
    output.Color = seed.Color;
    output.Alpha = seed.Opacity;
    output.GlowIntensity = seed.GlowIntensity;
    output.PappusPhase = seed.PappusPhase;

    return output;
}

// Helper function: Rotate a 2D vector
float2 rotate2D(float2 p, float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return float2(c * p.x - s * p.y, s * p.x + c * p.y);
}

// Line segment SDF
float sdLineSegment(float2 p, float2 a, float2 b, float thickness)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    return length(pa - ba * h) - thickness;
}

// Dandelion Seed SDF - stem + fluffy pappus
float DandelionSeedSDF(float2 p, float size, float pappusPhase)
{
    static const float PI = 3.14159265359;
    float result = 1e10;

    // Stem - thin vertical line from bottom to center
    float stemLength = size * 0.6;
    float2 stemStart = float2(0, stemLength * 0.5);
    float2 stemEnd = float2(0, -stemLength * 0.1);
    result = min(result, sdLineSegment(p, stemStart, stemEnd, 0.015));

    // Pappus (fluffy parachute part) - radiating fine lines
    int numFilaments = 16;
    float pappusRadius = size * 0.5;

    for (int i = 0; i < numFilaments; i++)
    {
        float angle = (float(i) / float(numFilaments)) * 2.0 * PI;

        // Add gentle wave motion to filaments
        float wave = sin(pappusPhase + float(i) * 0.5) * 0.1;
        angle += wave;

        float2 dir = float2(cos(angle), sin(angle));

        // Start point (center of pappus)
        float2 start = float2(0, -stemLength * 0.1);

        // End point (tip of filament)
        // Make filaments slightly uneven in length
        float lengthVariation = 0.8 + (sin(float(i) * 2.3) * 0.5 + 0.5) * 0.4;
        float2 end = start + dir * pappusRadius * lengthVariation;

        // Main filament
        float filamentDist = sdLineSegment(p, start, end, 0.008);
        result = min(result, filamentDist);

        // Add tiny branches to filaments (every other filament)
        if (i % 2 == 0)
        {
            float branchPos = 0.7; // Position along filament
            float2 branchStart = start + dir * pappusRadius * lengthVariation * branchPos;

            // Two tiny side branches
            float branchAngle = 0.4;
            float branchLength = pappusRadius * 0.15;

            float2 branchDir1 = rotate2D(dir, branchAngle);
            float2 branchEnd1 = branchStart + branchDir1 * branchLength;
            result = min(result, sdLineSegment(p, branchStart, branchEnd1, 0.005));

            float2 branchDir2 = rotate2D(dir, -branchAngle);
            float2 branchEnd2 = branchStart + branchDir2 * branchLength;
            result = min(result, sdLineSegment(p, branchStart, branchEnd2, 0.005));
        }
    }

    // Small sphere at center where pappus connects to stem
    float centerDist = length(p - float2(0, -stemLength * 0.1)) - size * 0.04;
    result = min(result, centerDist);

    return result;
}

// Pixel shader - Render delicate seed with soft glow
float4 PSMain(VSOutput input) : SV_TARGET
{
    if (input.Alpha <= 0.001)
        discard;

    // Calculate seed SDF
    float dist = DandelionSeedSDF(input.UV, 0.8, input.PappusPhase);

    // Create sharp core
    float core = 1.0 - smoothstep(0.0, 0.03, dist);

    // Create soft glow layers (very delicate)
    float glow1 = 1.0 - smoothstep(0.0, 0.1, dist);
    float glow2 = 1.0 - smoothstep(0.0, 0.2, dist);
    float glow3 = 1.0 - smoothstep(0.0, 0.35, dist);

    // Combine layers with delicate intensities
    float intensity = core * 1.8 + glow1 * 0.8 + glow2 * 0.4 + glow3 * 0.2;
    intensity *= input.GlowIntensity;

    // Add subtle shimmer (very gentle)
    float shimmer = 0.95 + 0.05 * sin(Time * 2.0 + input.Position.x * 0.05 + input.Position.y * 0.05);
    intensity *= shimmer;

    // Apply color
    float4 color = input.Color;
    color.rgb *= intensity;

    // Very soft, delicate alpha
    color.a = saturate(intensity * input.Alpha * 0.7);

    // Apply HDR multiplier for bright displays
    color.rgb *= HdrMultiplier;

    return color;
}
