// Fireflies Shader - Pulsing glowing particles with soft halos

cbuffer FrameConstants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float4 Padding;
}

struct FireflyInstance
{
    float2 Position;
    float2 Velocity;
    float4 Color;
    float Size;
    float PulsePhase;
    float PulseSpeed;
    float Brightness;
    float WanderAngle;
    float TargetDistance;
    float Padding1;
    float Padding2;
};

StructuredBuffer<FireflyInstance> Fireflies : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR0;
    float Brightness : TEXCOORD1;
    float Size : TEXCOORD2;
};

// Vertex shader - Generate quad per firefly instance
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;
    FireflyInstance firefly = Fireflies[instanceId];

    // Skip if firefly doesn't exist (size = 0)
    if (firefly.Size <= 0 || firefly.Brightness <= 0)
    {
        output.Position = float4(0, 0, 0, 0);
        output.UV = float2(0, 0);
        output.Color = float4(0, 0, 0, 0);
        output.Brightness = 0;
        output.Size = 0;
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

    // Scale by firefly size (make glow area larger than core)
    float2 offset = quadUV * firefly.Size * 1.5;

    // Position in screen space
    float2 screenPos = firefly.Position + offset;

    // Convert to NDC
    float2 ndc = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y;

    output.Position = float4(ndc, 0, 1);
    output.UV = quadUV;
    output.Color = firefly.Color;
    output.Brightness = firefly.Brightness;
    output.Size = firefly.Size;

    return output;
}

// Pixel shader - Render soft glowing particles
float4 PSMain(VSOutput input) : SV_TARGET
{
    if (input.Brightness <= 0.001)
        discard;

    // Distance from center of particle
    float dist = length(input.UV);

    // Small bright core
    float core = 1.0 - smoothstep(0.0, 0.15, dist);

    // Multiple glow layers for soft halo effect
    // Using exponential falloff for natural glow: exp(-dist^2)
    float glow1 = exp(-dist * dist * 4.0);      // Inner glow
    float glow2 = exp(-dist * dist * 2.0);      // Medium glow
    float glow3 = exp(-dist * dist * 0.8);      // Outer halo

    // Combine layers with different intensities
    float intensity = core * 3.0 + glow1 * 1.5 + glow2 * 0.8 + glow3 * 0.3;

    // Apply pulsing brightness
    intensity *= input.Brightness;

    // Add slight shimmer/twinkle effect
    float twinkle = 0.9 + 0.1 * sin(Time * 8.0 + input.Position.x * 0.1);
    intensity *= twinkle;

    // Apply color
    float4 color = input.Color;
    color.rgb *= intensity;
    color.a = saturate(intensity);

    // Apply HDR multiplier for bright displays
    color.rgb *= HdrMultiplier;

    // Add warm glow center (fireflies have warmer centers)
    float centerGlow = 1.0 - smoothstep(0.0, 0.08, dist);
    color.rgb += centerGlow * float3(1.0, 0.9, 0.6) * input.Brightness * 0.5 * HdrMultiplier;

    return color;
}
