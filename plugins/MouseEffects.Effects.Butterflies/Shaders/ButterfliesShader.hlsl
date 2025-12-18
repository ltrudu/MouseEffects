// Butterflies Shader - Procedural butterfly rendering with wing animation

cbuffer FrameConstants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float4 Padding;
}

struct ButterflyInstance
{
    float2 Position;
    float2 Velocity;
    float4 Color;
    float Size;
    float WingFlapPhase;
    float WingFlapSpeed;
    float TargetDistance;
    float WanderAngle;
    float WanderSpeed;
    float BodyRotation;
    float GlowIntensity;
    float PatternVariant;
    float Lifetime;
    float Padding1;
    float Padding2;
};

StructuredBuffer<ButterflyInstance> Butterflies : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR0;
    float GlowIntensity : TEXCOORD1;
    float WingFlapPhase : TEXCOORD2;
    float BodyRotation : TEXCOORD3;
    float PatternVariant : TEXCOORD4;
};

// Vertex shader - Generate quad per butterfly instance
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;
    ButterflyInstance butterfly = Butterflies[instanceId];

    // Skip if butterfly doesn't exist (size = 0)
    if (butterfly.Size <= 0 || butterfly.Lifetime <= 0)
    {
        output.Position = float4(0, 0, 0, 0);
        output.UV = float2(0, 0);
        output.Color = float4(0, 0, 0, 0);
        output.GlowIntensity = 0;
        output.WingFlapPhase = 0;
        output.BodyRotation = 0;
        output.PatternVariant = 0;
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

    // Apply rotation based on body rotation
    float c = cos(butterfly.BodyRotation);
    float s = sin(butterfly.BodyRotation);
    float2x2 rotation = float2x2(c, -s, s, c);
    float2 rotatedUV = mul(rotation, quadUV);

    // Scale by butterfly size (make it wider for butterfly shape)
    float2 offset = rotatedUV * butterfly.Size * float2(1.5, 1.0);

    // Position in screen space
    float2 screenPos = butterfly.Position + offset;

    // Convert to NDC
    float2 ndc = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y;

    output.Position = float4(ndc, 0, 1);
    output.UV = quadUV;
    output.Color = butterfly.Color;
    output.GlowIntensity = butterfly.GlowIntensity;
    output.WingFlapPhase = butterfly.WingFlapPhase;
    output.BodyRotation = butterfly.BodyRotation;
    output.PatternVariant = butterfly.PatternVariant;

    return output;
}

// SDF helper functions
float sdEllipse(float2 p, float2 ab)
{
    p = abs(p);
    if (p.x > p.y)
    {
        p = p.yx;
        ab = ab.yx;
    }

    float l = ab.y * ab.y - ab.x * ab.x;
    float m = ab.x * p.x / l;
    float m2 = m * m;
    float n = ab.y * p.y / l;
    float n2 = n * n;
    float c = (m2 + n2 - 1.0) / 3.0;
    float c3 = c * c * c;
    float q = c3 + m2 * n2 * 2.0;
    float d = c3 + m2 * n2;
    float g = m + m * n2;
    float co;

    if (d < 0.0)
    {
        float h = acos(q / c3) / 3.0;
        float s = cos(h);
        float t = sin(h) * sqrt(3.0);
        float rx = sqrt(-c * (s + t + 2.0) + m2);
        float ry = sqrt(-c * (s - t + 2.0) + m2);
        co = (ry + sign(l) * rx + abs(g) / (rx * ry) - m) / 2.0;
    }
    else
    {
        float h = 2.0 * m * n * sqrt(d);
        float s = sign(q + h) * pow(abs(q + h), 1.0 / 3.0);
        float u = sign(q - h) * pow(abs(q - h), 1.0 / 3.0);
        float rx = -s - u - c * 4.0 + 2.0 * m2;
        float ry = (s - u) * sqrt(3.0);
        float rm = sqrt(rx * rx + ry * ry);
        co = (ry / sqrt(rm - rx) + 2.0 * g / rm - m) / 2.0;
    }

    float2 r = ab * float2(co, sqrt(1.0 - co * co));
    return length(r - p) * sign(p.y - r.y);
}

// Simple ellipse approximation (faster)
float sdEllipseSimple(float2 p, float2 size)
{
    float2 q = abs(p) / size;
    return (length(q) - 1.0) * min(size.x, size.y);
}

// Pixel shader - Render procedural butterfly with flapping wings
float4 PSMain(VSOutput input) : SV_TARGET
{
    static const float PI = 3.14159265359;

    // Calculate wing flap angle (0 to ~45 degrees)
    float flapAngle = sin(input.WingFlapPhase) * 0.4 + 0.4; // 0 to 0.8 radians

    // Mirror coordinates for left/right wings
    float2 p = input.UV;
    float wingSign = sign(p.x);
    p.x = abs(p.x);

    // Rotate wing position based on flap
    float cosFlap = cos(flapAngle * wingSign);
    float sinFlap = sin(flapAngle * wingSign);
    float2x2 flapRotation = float2x2(cosFlap, -sinFlap, sinFlap, cosFlap);
    float2 wingP = mul(flapRotation, p);

    // Wing shape - elongated ellipse
    float2 wingSize = float2(0.55, 0.85);
    float2 wingOffset = float2(0.35, 0.0);
    float wing = sdEllipseSimple(wingP - wingOffset, wingSize);

    // Body - thin vertical ellipse in center
    float body = sdEllipseSimple(p - float2(0.0, 0.0), float2(0.08, 0.5));

    // Combine body and wings
    float dist = min(wing, body);

    // Discard if outside butterfly
    if (dist > 0.2)
        discard;

    // Create smooth edges
    float alpha = 1.0 - smoothstep(-0.05, 0.0, dist);

    // Add wing patterns based on variant
    float wingPattern = 0.0;
    if (input.PatternVariant > 0.5)
    {
        // Spots on wings
        float2 spotPos1 = wingP - float2(0.4, 0.3);
        float2 spotPos2 = wingP - float2(0.5, -0.2);
        float spot1 = 1.0 - smoothstep(0.1, 0.15, length(spotPos1));
        float spot2 = 1.0 - smoothstep(0.08, 0.12, length(spotPos2));
        wingPattern = max(spot1, spot2) * 0.5;
    }
    else
    {
        // Stripes on wings
        float stripe = sin(wingP.y * 10.0) * 0.5 + 0.5;
        wingPattern = stripe * 0.3;
    }

    // Create glow layers
    float glow1 = 1.0 - smoothstep(-0.05, 0.15, dist);
    float glow2 = 1.0 - smoothstep(-0.05, 0.3, dist);
    float glow3 = 1.0 - smoothstep(-0.05, 0.5, dist);

    // Combine core and glow
    float coreIntensity = 1.0 - smoothstep(-0.05, 0.02, dist);
    float glowIntensity = glow1 * 0.6 + glow2 * 0.3 + glow3 * 0.15;

    // Apply pattern (darker spots/stripes on wings)
    float finalIntensity = lerp(coreIntensity, coreIntensity * (1.0 - wingPattern), step(0.0, wing));
    finalIntensity += glowIntensity * input.GlowIntensity;

    // Add slight shimmer
    float shimmer = 0.9 + 0.1 * sin(Time * 3.0 + input.WingFlapPhase);
    finalIntensity *= shimmer;

    // Apply color
    float4 color = input.Color;
    color.rgb *= finalIntensity;
    color.a = alpha * saturate(finalIntensity);

    // Apply HDR multiplier
    color.rgb *= HdrMultiplier;

    // Add subtle edge highlight
    float edgeHighlight = smoothstep(0.0, -0.02, dist) * (1.0 - smoothstep(-0.02, -0.05, dist));
    color.rgb += edgeHighlight * 0.3 * HdrMultiplier;

    return color;
}
