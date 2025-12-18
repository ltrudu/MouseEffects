// Crystal Growth Shader - Geometric ice crystal structures with refraction sparkles
// Renders angular branches with hexagonal symmetry and light effects

cbuffer Constants : register(b0)
{
    float2 ViewportSize;      // 8 bytes
    float Time;               // 4 bytes
    float BranchThickness;    // 4 bytes
    float SparkleIntensity;   // 4 bytes
    float GlowIntensity;      // 4 bytes
    float HdrMultiplier;      // 4 bytes
    float Padding;            // 4 bytes
    float4 Padding2;          // 16 bytes
}

struct CrystalBranch
{
    float2 Start;             // Start position
    float2 End;               // End position (at full growth)
    float Progress;           // Growth progress (0->1)
    float Lifetime;           // Current lifetime
    float MaxLifetime;        // Total lifetime
    float Angle;              // Branch angle (radians)
    float4 Color;             // Crystal color
    float MaxLength;          // Maximum branch length
    float Generation;         // Branch generation (0=main, 1=sub, etc)
    float SparklePhase;       // Random phase for sparkle animation
    float Padding;
    float4 Padding2;
};

StructuredBuffer<CrystalBranch> Branches : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// Fullscreen triangle vertex shader
VSOutput VSMain(uint vertexId : SV_VertexID)
{
    VSOutput output;
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y;
    output.TexCoord = uv;
    return output;
}

// SDF for line segment with sharp edges
float lineSDF(float2 p, float2 a, float2 b, float thickness)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    return length(pa - ba * h) - thickness;
}

// SDF for hexagon (crystal node)
float hexagonSDF(float2 p, float2 center, float radius)
{
    const float3 k = float3(-0.866025404, 0.5, 0.577350269);
    p = abs(p - center);
    p -= 2.0 * min(dot(k.xy, p), 0.0) * k.xy;
    p -= float2(clamp(p.x, -k.z * radius, k.z * radius), radius);
    return length(p) * sign(p.y);
}

// Hash function for pseudo-random numbers
float hash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

// 2D value noise for sparkle effect
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

// Sparkle effect for light refraction
float sparkle(float2 p, float time, float phase)
{
    float n1 = noise(p * 0.5 + time * 0.5 + phase);
    float n2 = noise(p * 1.0 - time * 0.7 + phase * 2.0);
    float n3 = noise(p * 2.0 + time * 0.3 + phase * 3.0);

    float sparkleValue = n1 * 0.5 + n2 * 0.3 + n3 * 0.2;
    sparkleValue = pow(sparkleValue, 3.0);

    return sparkleValue;
}

// Frost/ice texture pattern
float frostPattern(float2 p)
{
    float pattern = 0.0;
    pattern += noise(p * 3.0) * 0.5;
    pattern += noise(p * 6.0) * 0.3;
    pattern += noise(p * 12.0) * 0.2;
    return pattern;
}

static const float PI = 3.14159265359;

float4 PSMain(VSOutput input) : SV_TARGET
{
    float2 pixelPos = input.TexCoord * ViewportSize;
    float4 finalColor = float4(0, 0, 0, 0);

    // Process all branches
    for (int i = 0; i < 1024; i++)
    {
        CrystalBranch branch = Branches[i];

        // Skip inactive branches
        if (branch.Lifetime <= 0)
            continue;

        // Calculate visible portion of branch based on growth progress
        float2 visibleEnd = lerp(branch.Start, branch.End, branch.Progress);
        float visibleLength = length(visibleEnd - branch.Start);

        // Calculate fade based on lifetime
        float lifetimeFade = branch.Lifetime / branch.MaxLifetime;
        float fadeAlpha = smoothstep(0.0, 0.3, lifetimeFade);

        // Melt effect - crystals shrink from the tips
        float meltFade = smoothstep(0.0, 0.3, lifetimeFade);
        float meltShrink = 1.0 - (1.0 - meltFade) * 0.5;

        // Line segment rendering (crystal branch)
        float branchThick = BranchThickness * (1.0 - branch.Generation * 0.2) * meltShrink;
        float lineDist = lineSDF(pixelPos, branch.Start, visibleEnd, branchThick);

        if (lineDist < 15.0)
        {
            // Core crystal branch - sharp edges
            float lineCore = 1.0 - smoothstep(0.0, 0.3, lineDist);
            float lineEdge = 1.0 - smoothstep(0.0, branchThick * 0.5, lineDist);

            // Multi-layer glow
            float glow1 = 1.0 - smoothstep(0.0, branchThick * 2.0, lineDist);
            float glow2 = 1.0 - smoothstep(0.0, branchThick * 4.0, lineDist);
            float glow3 = 1.0 - smoothstep(0.0, branchThick * 6.0, lineDist);

            // Calculate position along branch for effects
            float2 lineVec = visibleEnd - branch.Start;
            float2 pixelVec = pixelPos - branch.Start;
            float alongLine = visibleLength > 0 ? dot(pixelVec, normalize(lineVec)) / visibleLength : 0;

            // Frost pattern along the branch
            float2 localPos = pixelPos - branch.Start;
            float frost = frostPattern(localPos * 0.1);
            frost = frost * 0.3 + 0.7;

            // Sparkle effect (light refraction)
            float sparkleEffect = sparkle(pixelPos, Time, branch.SparklePhase);
            sparkleEffect *= lineEdge;
            sparkleEffect *= SparkleIntensity;

            // Growth shimmer at the tip
            float tipDistance = length(pixelPos - visibleEnd);
            float tipShimmer = exp(-tipDistance * 0.1) * (0.5 + 0.5 * sin(Time * 10.0 + branch.SparklePhase));
            tipShimmer *= step(branch.Progress, 0.99);

            // Combine layers
            float intensity = lineCore * 2.0 + lineEdge * 1.5 + glow1 * 0.6 + glow2 * 0.3 + glow3 * 0.15;
            intensity *= frost;
            intensity *= GlowIntensity;
            intensity *= fadeAlpha;
            intensity += sparkleEffect * 2.0;
            intensity += tipShimmer * 1.5;

            float4 color = branch.Color * intensity;
            finalColor += color;
        }

        // Hexagonal node at start position
        float nodeSize = (3.0 + branch.Generation) * meltShrink;
        float nodeDistStart = hexagonSDF(pixelPos, branch.Start, nodeSize);

        if (abs(nodeDistStart) < nodeSize * 2.0)
        {
            float nodeCore = 1.0 - smoothstep(-0.5, 0.5, nodeDistStart);
            float nodeEdge = 1.0 - smoothstep(-0.5, nodeSize * 0.5, nodeDistStart);
            float nodeGlow = 1.0 - smoothstep(0.0, nodeSize * 2.0, abs(nodeDistStart));

            // Node sparkle
            float nodeSparkle = sparkle(branch.Start, Time, branch.SparklePhase);

            float nodeIntensity = nodeCore * 2.5 + nodeEdge * 1.5 + nodeGlow * 0.8;
            nodeIntensity *= GlowIntensity;
            nodeIntensity *= fadeAlpha;
            nodeIntensity += nodeSparkle * SparkleIntensity;

            finalColor += branch.Color * nodeIntensity;
        }

        // Hexagonal node at current end position (growing tip)
        if (branch.Progress > 0.1)
        {
            float tipNodeSize = (3.5 + branch.Generation) * meltShrink;
            float nodeDistEnd = hexagonSDF(pixelPos, visibleEnd, tipNodeSize);

            if (abs(nodeDistEnd) < tipNodeSize * 2.5)
            {
                float nodeCore = 1.0 - smoothstep(-0.5, 0.5, nodeDistEnd);
                float nodeEdge = 1.0 - smoothstep(-0.5, tipNodeSize * 0.5, nodeDistEnd);
                float nodeGlow = 1.0 - smoothstep(0.0, tipNodeSize * 2.5, abs(nodeDistEnd));

                // Brighter sparkle at growing tip
                float tipSparkle = sparkle(visibleEnd, Time * 1.5, branch.SparklePhase + 1.0);
                float tipPulse = 0.7 + 0.3 * sin(Time * 8.0 + branch.SparklePhase);

                float nodeIntensity = (nodeCore * 3.0 + nodeEdge * 2.0 + nodeGlow * 1.2) * tipPulse;
                nodeIntensity *= GlowIntensity;
                nodeIntensity *= fadeAlpha;
                nodeIntensity += tipSparkle * SparkleIntensity * 1.5;

                finalColor += branch.Color * nodeIntensity;
            }
        }
    }

    // Apply HDR multiplier
    finalColor.rgb *= HdrMultiplier;

    // Soft clamp to preserve HDR highlights
    finalColor = saturate(finalColor);

    return finalColor;
}
