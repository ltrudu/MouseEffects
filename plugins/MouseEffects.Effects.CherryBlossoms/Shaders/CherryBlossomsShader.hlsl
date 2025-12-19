// Cherry Blossoms Shader - Beautiful sakura petal with soft glow

cbuffer FrameConstants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float4 Padding;
}

struct PetalInstance
{
    float2 Position;          // 8 bytes, offset 0
    float2 Velocity;          // 8 bytes, offset 8
    float4 Color;             // 16 bytes, offset 16
    float Size;               // 4 bytes, offset 32
    float Lifetime;           // 4 bytes, offset 36
    float MaxLifetime;        // 4 bytes, offset 40
    float RotationAngle;      // 4 bytes, offset 44
    float SpinSpeed;          // 4 bytes, offset 48
    float SwayPhase;          // 4 bytes, offset 52
    float SwayAmplitude;      // 4 bytes, offset 56
    float GlowIntensity;      // 4 bytes, offset 60
    float FallSpeed;          // 4 bytes, offset 64
    float ColorVariant;       // 4 bytes, offset 68
    float Padding1;           // 4 bytes, offset 72
    float Padding2;           // 4 bytes, offset 76 = 80 bytes total
};

StructuredBuffer<PetalInstance> Petals : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR0;
    float Alpha : TEXCOORD1;
    float GlowIntensity : TEXCOORD2;
};

// Vertex shader - Generate quad per petal instance
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;
    PetalInstance petal = Petals[instanceId];

    // Skip dead petals - move them completely off-screen
    if (petal.Lifetime <= 0)
    {
        output.Position = float4(-10.0, -10.0, 0.0, 1.0); // Off-screen with valid w
        output.UV = float2(0, 0);
        output.Color = float4(0, 0, 0, 0);
        output.Alpha = 0;
        output.GlowIntensity = 0;
        return output;
    }

    // Calculate alpha based on screen Y position (full at top, zero at bottom)
    float normalizedY = petal.Position.y / ViewportSize.y;
    float alpha = saturate(1.0 - normalizedY); // 1.0 at top (y=0), 0.0 at bottom (y=viewport height)

    // Generate quad vertices (two triangles)
    // Vertex order: 0-1-2, 2-1-3
    float2 quadUV;
    if (vertexId == 0) quadUV = float2(-1, -1);
    else if (vertexId == 1) quadUV = float2(1, -1);
    else if (vertexId == 2) quadUV = float2(-1, 1);
    else if (vertexId == 3) quadUV = float2(-1, 1);
    else if (vertexId == 4) quadUV = float2(1, -1);
    else quadUV = float2(1, 1);

    // Apply rotation
    float c = cos(petal.RotationAngle);
    float s = sin(petal.RotationAngle);
    float2x2 rotation = float2x2(c, -s, s, c);
    float2 rotatedUV = mul(rotation, quadUV);

    // Scale by petal size
    float2 offset = rotatedUV * petal.Size;

    // Position in screen space
    float2 screenPos = petal.Position + offset;

    // Convert to NDC
    float2 ndc = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y; // Flip Y for DirectX

    output.Position = float4(ndc, 0, 1);
    output.UV = quadUV; // Keep unrotated UV for SDF
    output.Color = petal.Color;
    output.Alpha = alpha;
    output.GlowIntensity = petal.GlowIntensity;

    return output;
}

// Helper functions for SDF
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
        float s = sign(q + h) * pow(abs(q + h), 0.3333);
        float u = sign(q - h) * pow(abs(q - h), 0.3333);
        float rx = -s - u - c * 4.0 + 2.0 * m2;
        float ry = (s - u) * sqrt(3.0);
        float rm = sqrt(rx * rx + ry * ry);
        co = (ry / sqrt(rm - rx) + 2.0 * g / rm - m) / 2.0;
    }

    float2 r = ab * float2(co, sqrt(1.0 - co * co));
    return length(r - p) * sign(p.y - r.y);
}

// Teardrop petal shape - elongated ellipse with pointed end
float PetalSDF(float2 p, float size)
{
    // Shift point to position the tip
    p.y -= size * 0.3;

    // Create elongated ellipse for petal body
    float ellipseDist = sdEllipse(p, float2(0.5 * size, 0.9 * size));

    // Create pointed tip at top
    float tipDist = p.y - size * 0.9;

    // Blend ellipse and tip to create teardrop shape
    float petal = max(ellipseDist, tipDist);

    return petal;
}

// Add subtle petal texture/detail
float PetalTexture(float2 p, float size)
{
    static const float PI = 3.14159265359;

    // Create vein-like lines radiating from center
    float angle = atan2(p.y, p.x);
    float radius = length(p);

    // Central vein
    float centerVein = abs(p.x) - 0.02 * size;

    // Side veins (subtle)
    float veinPattern = sin(angle * 5.0) * 0.5 + 0.5;
    float veins = veinPattern * 0.1;

    return smoothstep(0.0, 0.1, centerVein) * (1.0 - veins);
}

// Pixel shader - Render petal with soft glow
float4 PSMain(VSOutput input) : SV_TARGET
{
    // Discard dead/invisible petals
    if (input.Alpha <= 0.001 || input.Color.a <= 0.001 || input.GlowIntensity <= 0.001)
        discard;

    // Calculate petal SDF
    float dist = PetalSDF(input.UV, 0.8);

    // Create sharp core
    float core = 1.0 - smoothstep(0.0, 0.05, dist);

    // Create glow layers (soft pink glow)
    float glow1 = 1.0 - smoothstep(0.0, 0.15, dist);
    float glow2 = 1.0 - smoothstep(0.0, 0.25, dist);
    float glow3 = 1.0 - smoothstep(0.0, 0.4, dist);

    // Combine layers with different intensities
    float intensity = core * 1.0 + glow1 * 0.6 + glow2 * 0.3 + glow3 * 0.15;
    intensity *= input.GlowIntensity;

    // Add subtle petal texture
    float petalTex = PetalTexture(input.UV, 0.8);
    intensity *= (0.95 + petalTex * 0.05);

    // Add very subtle shimmer effect
    float shimmer = 0.98 + 0.02 * sin(Time * 2.0 + input.Position.x * 0.05);
    intensity *= shimmer;

    // Apply color - preserve saturation by not over-brightening
    float4 color = input.Color;
    float brightness = saturate(intensity);
    color.rgb *= brightness;
    color.a = saturate(brightness * input.Alpha);

    // Apply HDR multiplier for bright displays (with limit to prevent washout)
    color.rgb *= min(HdrMultiplier, 1.5);

    return color;
}
