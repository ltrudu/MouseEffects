// Falling Leaves Shader - Procedural autumn leaf shapes with tumbling animation

cbuffer FrameConstants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float4 Padding;
}

struct LeafInstance
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
    float TumblePhase;
    float TumbleSpeed;
    int LeafVariant;
    float SwayAmplitude;
    float FallSpeedMod;
    float Padding;
};

StructuredBuffer<LeafInstance> Leaves : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR0;
    float Alpha : TEXCOORD1;
    float TumblePhase : TEXCOORD2;
    int LeafVariant : TEXCOORD3;
};

// Vertex shader - Generate quad per leaf instance
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;
    LeafInstance leaf = Leaves[instanceId];

    // Skip dead leaves
    if (leaf.Lifetime <= 0)
    {
        output.Position = float4(0, 0, 0, 0);
        output.UV = float2(0, 0);
        output.Color = float4(0, 0, 0, 0);
        output.Alpha = 0;
        output.TumblePhase = 0;
        output.LeafVariant = 0;
        return output;
    }

    // Calculate alpha based on lifetime (fade in and out)
    float lifeFraction = leaf.Lifetime / leaf.MaxLifetime;
    float fadeIn = saturate((1.0 - lifeFraction) * 5.0);
    float fadeOut = saturate(lifeFraction * 2.0);
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
    float c = cos(leaf.RotationAngle);
    float s = sin(leaf.RotationAngle);
    float2x2 rotation = float2x2(c, -s, s, c);
    float2 rotatedUV = mul(rotation, quadUV);

    // Scale by leaf size
    float2 offset = rotatedUV * leaf.Size;

    // Position in screen space
    float2 screenPos = leaf.Position + offset;

    // Convert to NDC
    float2 ndc = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y;

    output.Position = float4(ndc, 0, 1);
    output.UV = quadUV;
    output.Color = leaf.Color;
    output.Alpha = alpha;
    output.TumblePhase = leaf.TumblePhase;
    output.LeafVariant = leaf.LeafVariant;

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
float sdLineSegment(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    return length(pa - ba * h);
}

// Ellipse SDF
float sdEllipse(float2 p, float2 ab)
{
    p = abs(p);
    if (p.x > p.y) { float t = p.x; p.x = p.y; p.y = t; float t2 = ab.x; ab.x = ab.y; ab.y = t2; }
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

// Maple-style leaf SDF (5-pointed star shape)
float MapleLeafSDF(float2 p, float size)
{
    static const float PI = 3.14159265359;

    // Main body - elongated shape
    float body = sdEllipse(p, float2(size * 0.5, size * 0.7));

    // Create 5 pointed lobes using star pattern
    float angle = atan2(p.y, p.x);
    float r = length(p);
    int points = 5;
    float starAngle = fmod(angle + PI / float(points), 2.0 * PI / float(points)) - PI / float(points);
    float starR = size * 0.8 * (0.5 + 0.5 * cos(starAngle * float(points)));
    float star = r - starR;

    // Combine body and star pattern
    float result = max(body, star);

    // Add center vein
    float vein = sdLineSegment(p, float2(0, -size * 0.6), float2(0, size * 0.7)) - size * 0.03;
    result = min(result, vein);

    return result;
}

// Oak-style leaf SDF (rounded lobes)
float OakLeafSDF(float2 p, float size)
{
    // Main elongated body
    float body = sdEllipse(p * float2(1.2, 1.0), float2(size * 0.4, size * 0.65));

    // Create rounded lobes on sides
    float lobe1 = length(p - float2(size * 0.3, size * 0.2)) - size * 0.25;
    float lobe2 = length(p - float2(-size * 0.3, size * 0.2)) - size * 0.25;
    float lobe3 = length(p - float2(size * 0.35, -size * 0.1)) - size * 0.22;
    float lobe4 = length(p - float2(-size * 0.35, -size * 0.1)) - size * 0.22;

    float lobes = min(min(lobe1, lobe2), min(lobe3, lobe4));
    float result = min(body, lobes);

    // Center vein
    float vein = sdLineSegment(p, float2(0, -size * 0.5), float2(0, size * 0.6)) - size * 0.025;
    result = min(result, vein);

    return result;
}

// Simple pointed leaf SDF
float SimpleLeafSDF(float2 p, float size)
{
    // Pointed ellipse shape
    p.y -= size * 0.1;
    float body = sdEllipse(p, float2(size * 0.4, size * 0.7));

    // Add pointed tip
    float2 tipP = p - float2(0, size * 0.7);
    float tip = length(tipP) - size * 0.15;
    float tipMask = step(tipP.y, 0);
    tip = lerp(1000, tip, tipMask);

    float result = min(body, tip);

    // Center vein
    float vein = sdLineSegment(p, float2(0, -size * 0.6), float2(0, size * 0.7)) - size * 0.025;
    result = min(result, vein);

    // Side veins
    float vein1 = sdLineSegment(p, float2(0, 0), float2(size * 0.3, size * 0.2)) - size * 0.015;
    float vein2 = sdLineSegment(p, float2(0, 0), float2(-size * 0.3, size * 0.2)) - size * 0.015;
    result = min(result, min(vein1, vein2));

    return result;
}

// Main leaf SDF selector
float LeafSDF(float2 p, float size, int variant)
{
    if (variant == 0)
        return MapleLeafSDF(p, size);
    else if (variant == 1)
        return OakLeafSDF(p, size);
    else
        return SimpleLeafSDF(p, size);
}

// Pixel shader - Render leaf with tumbling animation
float4 PSMain(VSOutput input) : SV_TARGET
{
    if (input.Alpha <= 0.001)
        discard;

    // Apply tumble effect (simulates 3D rotation by squashing horizontally)
    float tumble = cos(input.TumblePhase);
    float2 p = input.UV;
    p.x *= abs(tumble) * 0.7 + 0.3; // Scale X based on tumble (0.3 to 1.0)

    // Darken when edge-on (simulate lighting)
    float edgeDarkening = abs(tumble) * 0.5 + 0.5;

    // Calculate leaf SDF
    float dist = LeafSDF(p, 0.65, input.LeafVariant);

    // Create sharp edge
    float core = 1.0 - smoothstep(-0.02, 0.02, dist);

    // Create soft edge for anti-aliasing
    float softEdge = 1.0 - smoothstep(-0.05, 0.05, dist);

    // Create subtle glow
    float glow = 1.0 - smoothstep(0.0, 0.15, dist);

    // Combine
    float intensity = core * 1.2 + softEdge * 0.5 + glow * 0.2;

    // Apply edge darkening from tumbling
    intensity *= edgeDarkening;

    // Apply color
    float4 color = input.Color;
    color.rgb *= intensity;
    color.a = saturate(intensity * input.Alpha);

    // Add subtle color variation based on distance to center vein
    float veinDist = abs(p.x) * 2.0;
    color.rgb *= 0.85 + 0.15 * (1.0 - saturate(veinDist));

    // Apply HDR multiplier
    color.rgb *= HdrMultiplier;

    return color;
}
