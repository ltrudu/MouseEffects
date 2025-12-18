// FlowerBloom Shader - Procedurally rendered flowers with organic bloom animation

cbuffer FrameConstants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float4 Padding;
}

struct FlowerInstance
{
    float2 Position;
    float BloomProgress;
    float TotalBloomTime;
    float4 PrimaryColor;
    float4 SecondaryColor;
    float Size;
    int PetalCount;
    int FlowerType;
    float RotationAngle;
    float Lifetime;
    float MaxLifetime;
    float FadeOutTime;
    int HasStem;
    float BirthTime;
    float PetalCurvature;
    float PetalWidth;
    float Padding;
};

StructuredBuffer<FlowerInstance> Flowers : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 PrimaryColor : COLOR0;
    float4 SecondaryColor : COLOR1;
    float BloomProgress : TEXCOORD1;
    int PetalCount : TEXCOORD2;
    int FlowerType : TEXCOORD3;
    float Alpha : TEXCOORD4;
    int HasStem : TEXCOORD5;
    float PetalCurvature : TEXCOORD6;
    float PetalWidth : TEXCOORD7;
    float RotationAngle : TEXCOORD8;
};

// Vertex shader - Generate quad per flower instance
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;
    FlowerInstance flower = Flowers[instanceId];

    // Skip dead flowers
    if (flower.Lifetime <= 0)
    {
        output.Position = float4(0, 0, 0, 0);
        output.UV = float2(0, 0);
        output.PrimaryColor = float4(0, 0, 0, 0);
        output.SecondaryColor = float4(0, 0, 0, 0);
        output.BloomProgress = 0;
        output.PetalCount = 0;
        output.FlowerType = 0;
        output.Alpha = 0;
        output.HasStem = 0;
        output.PetalCurvature = 0;
        output.PetalWidth = 0;
        output.RotationAngle = 0;
        return output;
    }

    // Calculate alpha based on lifetime (fade out at end)
    float lifeFraction = flower.Lifetime / flower.MaxLifetime;
    float fadeOut = saturate(lifeFraction / (flower.FadeOutTime / flower.MaxLifetime));
    float alpha = fadeOut;

    // Generate quad vertices (two triangles)
    float2 quadUV;
    if (vertexId == 0) quadUV = float2(-1, -1);
    else if (vertexId == 1) quadUV = float2(1, -1);
    else if (vertexId == 2) quadUV = float2(-1, 1);
    else if (vertexId == 3) quadUV = float2(-1, 1);
    else if (vertexId == 4) quadUV = float2(1, -1);
    else quadUV = float2(1, 1);

    // Scale by flower size (quad is 2x flower size for stem)
    float2 offset = quadUV * flower.Size * 1.2;

    // Position in screen space
    float2 screenPos = flower.Position + offset;

    // Convert to NDC
    float2 ndc = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y; // Flip Y for DirectX

    output.Position = float4(ndc, 0, 1);
    output.UV = quadUV;
    output.PrimaryColor = flower.PrimaryColor;
    output.SecondaryColor = flower.SecondaryColor;
    output.BloomProgress = flower.BloomProgress;
    output.PetalCount = flower.PetalCount;
    output.FlowerType = flower.FlowerType;
    output.Alpha = alpha;
    output.HasStem = flower.HasStem;
    output.PetalCurvature = flower.PetalCurvature;
    output.PetalWidth = flower.PetalWidth;
    output.RotationAngle = flower.RotationAngle;

    return output;
}

// Constants
static const float PI = 3.14159265359;
static const float TAU = 6.28318530718;

// SDF circle
float sdCircle(float2 p, float r)
{
    return length(p) - r;
}

// SDF ellipse for petal shape
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

// Simplified petal SDF using smoothed shapes
float sdPetal(float2 p, float width, float length, float curvature)
{
    // Transform point to petal space
    p.y += 0.2; // Offset from center

    // Create curved petal shape using multiple circles
    float baseCircle = sdCircle(p - float2(0, -length * 0.3), length * 0.6);
    float tipCircle = sdCircle(p - float2(0, length * 0.2), length * 0.4);

    // Combine with smooth min for organic shape
    float petal = min(baseCircle, tipCircle) - width * 0.3;

    // Narrow at base
    float baseNarrow = abs(p.x) - width * 0.2;
    if (p.y < -length * 0.3)
    {
        petal = max(petal, baseNarrow);
    }

    return petal;
}

// Heart-shaped petal for rose
float sdHeartPetal(float2 p, float size)
{
    p.y -= size * 0.3;
    p.x = abs(p.x);

    float heart = sdCircle(p - float2(size * 0.3, -size * 0.3), size * 0.4);
    float heart2 = sdCircle(p - float2(-size * 0.3, -size * 0.3), size * 0.4);
    heart = min(heart, heart2);

    // Bottom point
    float bottom = length(p - float2(0, size * 0.4)) - size * 0.5;
    heart = min(heart, bottom);

    return heart;
}

// Render a single petal
float RenderPetal(float2 p, float angle, float bloomProgress, float width, float length, float curvature, int flowerType)
{
    // Rotate point to petal angle
    float c = cos(-angle);
    float s = sin(-angle);
    float2x2 rot = float2x2(c, -s, s, c);
    float2 rp = mul(rot, p);

    // Animate unfurling from center (scale from 0 to full size)
    float scale = bloomProgress;
    rp /= max(scale, 0.01);

    // Different petal shapes per flower type
    float petalDist;
    if (flowerType == 0) // Rose - heart-shaped petals
    {
        petalDist = sdHeartPetal(rp, length);
    }
    else if (flowerType == 1) // Daisy - elongated narrow petals
    {
        petalDist = sdPetal(rp, width * 0.5, length, curvature);
    }
    else if (flowerType == 2) // Lotus - wide rounded petals
    {
        petalDist = sdPetal(rp, width * 1.2, length * 0.8, curvature);
    }
    else // Cherry blossom - notched petals
    {
        petalDist = sdPetal(rp, width, length, curvature);
        // Add notch at tip
        float notch = abs(rp.x) - width * 0.15;
        if (rp.y > length * 0.1)
        {
            petalDist = max(petalDist, -notch);
        }
    }

    return petalDist;
}

// Render flower center
float RenderCenter(float2 p, float size, float bloomProgress)
{
    float centerSize = size * 0.3 * bloomProgress;
    return sdCircle(p, centerSize);
}

// Render stem
float RenderStem(float2 p, float2 uv, float size)
{
    // Stem only visible in bottom half
    if (uv.y < 0)
        return 1000.0; // Far away

    // Vertical stem
    float stemWidth = size * 0.05;
    float stemDist = abs(p.x) - stemWidth;

    // Leaves at sides
    float leafY = size * 0.6;
    float2 leafPos1 = float2(size * 0.2, leafY);
    float2 leafPos2 = float2(-size * 0.2, leafY);

    float leaf1 = sdEllipse(p - leafPos1, float2(size * 0.15, size * 0.08));
    float leaf2 = sdEllipse(p - leafPos2, float2(size * 0.15, size * 0.08));

    float leaves = min(leaf1, leaf2);

    return min(stemDist, leaves);
}

// Pixel shader - Render procedural flower
float4 PSMain(VSOutput input) : SV_TARGET
{
    if (input.Alpha <= 0.001 || input.PetalCount == 0)
        discard;

    float2 uv = input.UV;
    float2 p = uv; // Position in flower space (-1 to 1)

    float4 color = float4(0, 0, 0, 0);

    // Rotate entire flower
    float c = cos(input.RotationAngle);
    float s = sin(input.RotationAngle);
    float2x2 flowerRot = float2x2(c, -s, s, c);
    p = mul(flowerRot, p);

    // Render stem first (background layer)
    float minDist = 1000.0;
    bool isStem = false;

    if (input.HasStem == 1)
    {
        float stemDist = RenderStem(p, uv, 1.0);
        if (stemDist < minDist)
        {
            minDist = stemDist;
            isStem = true;
        }
    }

    // Render petals
    float petalAngleStep = TAU / float(input.PetalCount);
    float minPetalDist = 1000.0;

    for (int i = 0; i < 12; i++) // Max 12 petals
    {
        if (i >= input.PetalCount)
            break;

        float angle = float(i) * petalAngleStep;
        float petalDist = RenderPetal(p, angle, input.BloomProgress, input.PetalWidth, 0.7, input.PetalCurvature, input.FlowerType);
        minPetalDist = min(minPetalDist, petalDist);
    }

    // Render center
    float centerDist = RenderCenter(p, 1.0, input.BloomProgress);

    // Determine what to draw
    bool isPetal = minPetalDist < 0.0;
    bool isCenter = centerDist < 0.0;

    if (isCenter)
    {
        // Draw center
        float edge = 1.0 - smoothstep(-0.05, 0.0, centerDist);
        color = input.SecondaryColor;
        color.a = edge * input.Alpha;

        // Add texture/detail to center
        float2 centerUV = p * 10.0;
        float dots = sin(centerUV.x * 2.0) * sin(centerUV.y * 2.0);
        color.rgb *= 0.8 + dots * 0.2;
    }
    else if (isPetal)
    {
        // Draw petal
        float edge = 1.0 - smoothstep(-0.05, 0.0, minPetalDist);
        color = input.PrimaryColor;

        // Add gradient from center to edge
        float distFromCenter = length(p);
        float gradient = saturate(distFromCenter * 0.8);
        color.rgb = lerp(input.SecondaryColor.rgb * 0.8, input.PrimaryColor.rgb, gradient);

        // Add veins
        float veinAngle = atan2(p.y, p.x);
        float veins = abs(sin(veinAngle * float(input.PetalCount) * 2.0));
        color.rgb *= 0.9 + veins * 0.1;

        color.a = edge * input.Alpha;
    }
    else if (isStem && minDist < 0.0)
    {
        // Draw stem/leaves
        float edge = 1.0 - smoothstep(-0.02, 0.0, minDist);
        color = float4(0.2, 0.6, 0.2, 1); // Green
        color.a = edge * input.Alpha * 0.8;
    }
    else
    {
        discard;
    }

    // Apply soft glow around edges for bloom effect
    if (input.BloomProgress < 1.0)
    {
        float glow = (1.0 - input.BloomProgress) * 0.3;
        color.rgb += input.PrimaryColor.rgb * glow;
    }

    // Apply HDR multiplier
    color.rgb *= HdrMultiplier;

    return color;
}
