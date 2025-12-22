// FireParticleShader.hlsl - GPU instanced fire particle rendering
// Uses StructuredBuffer for particle data and renders quads via SV_InstanceID

cbuffer Constants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float FadeAlpha;
    float Padding1;
    float Padding2;
    float Padding3;
};

struct FireParticle
{
    float2 Position;
    float2 Velocity;
    float4 Color;
    float Size;
    float Lifetime;
    float MaxLifetime;
    float Heat;
    float FlickerPhase;
    float SpawnAngle;
    float Padding1;
    float Padding2;
};

StructuredBuffer<FireParticle> Particles : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoord : TEXCOORD0;
    float Heat : TEXCOORD1;
    float FlickerPhase : TEXCOORD2;
};

// Vertex shader - generates quads for each particle instance
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;
    FireParticle p = Particles[instanceId];

    // Skip dead particles
    if (p.Lifetime <= 0)
    {
        output.Position = float4(0, 0, -2, 1); // Behind camera
        output.Color = float4(0, 0, 0, 0);
        output.TexCoord = float2(0, 0);
        output.Heat = 0;
        output.FlickerPhase = 0;
        return output;
    }

    // Quad vertices (2 triangles = 6 vertices)
    float2 offsets[6] = {
        float2(-1, -1), float2(1, -1), float2(-1, 1),
        float2(-1, 1), float2(1, -1), float2(1, 1)
    };
    float2 texCoords[6] = {
        float2(0, 1), float2(1, 1), float2(0, 0),
        float2(0, 0), float2(1, 1), float2(1, 0)
    };

    float2 offset = offsets[vertexId];
    float2 texCoord = texCoords[vertexId];

    // Size shrinks as particle ages
    float lifeRatio = p.Lifetime / p.MaxLifetime;
    float size = p.Size * (0.3 + 0.7 * lifeRatio);

    // Screen position
    float2 screenPos = p.Position + offset * size;

    // Convert to NDC
    float2 ndc = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y; // Flip Y for screen coords

    output.Position = float4(ndc, 0, 1);
    output.Color = p.Color * FadeAlpha;
    output.TexCoord = texCoord;
    output.Heat = p.Heat;
    output.FlickerPhase = p.FlickerPhase;

    return output;
}

// Pixel shader - renders fire particle with glow
float4 PSMain(VSOutput input) : SV_TARGET
{
    // Early discard for invisible particles
    if (input.Color.a <= 0.001)
        discard;

    // Distance from center
    float2 center = input.TexCoord - 0.5;
    float dist = length(center) * 2.0;

    // Fire shape - soft core with glow falloff
    float core = 1.0 - smoothstep(0.0, 0.4, dist);
    float glow = exp(-dist * dist * 2.5);

    // Flicker effect
    float flicker = 0.8 + 0.2 * sin(Time * 15.0 + input.FlickerPhase);

    // Combine shape and flicker
    float intensity = (core * 1.2 + glow * 0.5) * flicker;

    float4 color = input.Color;

    // Brighter core when hot
    color.rgb *= intensity * (1.0 + input.Heat * 0.8);

    // Alpha based on glow shape
    color.a *= glow * input.Color.a;

    // HDR boost for bright areas
    float hdrBoost = 1.0 + glow * HdrMultiplier * 1.5;
    color.rgb *= hdrBoost;

    // Discard nearly transparent pixels
    if (color.a < 0.01)
        discard;

    return color;
}
