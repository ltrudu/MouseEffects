// Hearts Shader - Floating heart particles with glow and sparkle

cbuffer FrameConstants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float4 Padding;
}

struct HeartInstance
{
    float2 Position;
    float2 Velocity;
    float4 Color;
    float Size;
    float Lifetime;
    float MaxLifetime;
    float RotationAngle;
    float WobblePhase;
    float WobbleAmplitude;
    float FloatSpeed;
    float GlowIntensity;
    float SparklePhase;
    float ColorVariant;
    float Padding1;
    float Padding2;
};

StructuredBuffer<HeartInstance> Hearts : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR0;
    float Alpha : TEXCOORD1;
    float GlowIntensity : TEXCOORD2;
    float SparklePhase : TEXCOORD3;
};

// Vertex shader - Generate quad per heart instance
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;
    HeartInstance heart = Hearts[instanceId];

    // Skip dead hearts
    if (heart.Lifetime <= 0)
    {
        output.Position = float4(0, 0, 0, 0);
        output.UV = float2(0, 0);
        output.Color = float4(0, 0, 0, 0);
        output.Alpha = 0;
        output.GlowIntensity = 0;
        output.SparklePhase = 0;
        return output;
    }

    // Calculate alpha based on lifetime (fade in and out)
    float lifeFraction = heart.Lifetime / heart.MaxLifetime;
    float fadeIn = saturate((1.0 - lifeFraction) * 4.0); // Quick fade in
    float fadeOut = saturate(lifeFraction * 1.5); // Slower fade out
    float alpha = min(fadeIn, fadeOut);

    // Generate quad vertices (two triangles)
    float2 quadUV;
    if (vertexId == 0) quadUV = float2(-1, -1);
    else if (vertexId == 1) quadUV = float2(1, -1);
    else if (vertexId == 2) quadUV = float2(-1, 1);
    else if (vertexId == 3) quadUV = float2(-1, 1);
    else if (vertexId == 4) quadUV = float2(1, -1);
    else quadUV = float2(1, 1);

    // Apply rotation
    float c = cos(heart.RotationAngle);
    float s = sin(heart.RotationAngle);
    float2x2 rotation = float2x2(c, -s, s, c);
    float2 rotatedUV = mul(rotation, quadUV);

    // Scale by heart size
    float2 offset = rotatedUV * heart.Size;

    // Position in screen space
    float2 screenPos = heart.Position + offset;

    // Convert to NDC
    float2 ndc = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y; // Flip Y for DirectX

    output.Position = float4(ndc, 0, 1);
    output.UV = quadUV; // Keep unrotated UV for SDF
    output.Color = heart.Color;
    output.Alpha = alpha;
    output.GlowIntensity = heart.GlowIntensity;
    output.SparklePhase = heart.SparklePhase;

    return output;
}

// Heart SDF - Creates a proper heart shape (based on Inigo Quilez's formula)
float sdHeart(float2 p, float size)
{
    // Normalize coordinates
    p = p / size;

    // Flip and center the heart (point at bottom)
    p.y = -p.y + 0.5;

    // Mirror for symmetry
    p.x = abs(p.x);

    // Heart shape using parametric boundary
    if (p.y + p.x > 1.0)
    {
        // Upper lobe region - circle centered at (0.25, 0.75)
        float2 c = float2(0.25, 0.75);
        float r = 0.3536; // sqrt(2)/4
        return (length(p - c) - r) * size;
    }
    else
    {
        // Lower region - distance to corner/point
        float d1 = length(p - float2(0.0, 1.0));

        float2 q = p - 0.5 * max(p.x + p.y, 0.0);
        float d2 = length(q);

        float d = min(d1, d2);
        float s = sign(p.x - p.y);

        return d * s * size;
    }
}

// Sparkle pattern - creates twinkling effect
float sparklePattern(float2 p, float phase)
{
    // Create a rotating 4-pointed star pattern
    float angle = atan2(p.y, p.x);
    float dist = length(p);

    // Radial segments
    float segments = sin(angle * 4.0 + phase) * 0.5 + 0.5;

    // Pulsing based on distance
    float pulse = sin(dist * 10.0 - phase * 2.0) * 0.5 + 0.5;

    return segments * pulse;
}

// Pixel shader - Render heart with glow and sparkle
float4 PSMain(VSOutput input) : SV_TARGET
{
    if (input.Alpha <= 0.001)
        discard;

    // Heart SDF
    float dist = sdHeart(input.UV, 0.5);

    // Create solid heart core
    float core = 1.0 - smoothstep(-0.05, 0.0, dist);

    // Create glow layers
    float glow1 = 1.0 - smoothstep(-0.05, 0.15, dist);
    float glow2 = 1.0 - smoothstep(-0.05, 0.3, dist);
    float glow3 = 1.0 - smoothstep(-0.05, 0.5, dist);

    // Combine layers with different intensities
    float intensity = core * 1.5 + glow1 * 0.6 + glow2 * 0.3 + glow3 * 0.15;
    intensity *= input.GlowIntensity;

    // Add sparkle effect on the surface
    float sparkle = sparklePattern(input.UV, input.SparklePhase);
    float sparkleContribution = sparkle * core * 0.3; // Only sparkle on the solid heart
    intensity += sparkleContribution;

    // Add subtle pulsing
    float pulse = 0.9 + 0.1 * sin(Time * 2.0 + input.SparklePhase);
    intensity *= pulse;

    // Apply color
    float4 color = input.Color;
    color.rgb *= intensity;
    color.a = intensity * input.Alpha;

    // Boost brightness slightly for vibrant look
    color.rgb *= 1.2;

    // Apply HDR multiplier for bright displays
    color.rgb *= HdrMultiplier;

    return color;
}
