// Lightning Storm shader - renders jagged lightning bolts with branches and sparks

cbuffer LightningConstants : register(b0)
{
    float2 ViewportSize;
    float2 MousePosition;
    float Time;
    float BoltThickness;
    float FlickerSpeed;
    float FlashIntensity;
    float GlowIntensity;
    float BranchIntensity;
    float HdrMultiplier;
    float Padding1;
    float4 Padding2;
};

struct LightningBolt
{
    float2 StartPos;
    float2 EndPos;
    float4 Color;
    float Lifetime;
    float MaxLifetime;
    float Intensity;
    float BranchCount;
    float4 Padding;
};

struct ImpactSpark
{
    float2 Position;
    float2 Velocity;
    float4 Color;
};

StructuredBuffer<LightningBolt> Bolts : register(t0);
StructuredBuffer<ImpactSpark> Sparks : register(t1);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

// Hash function for pseudo-random numbers
float hash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

// Noise function for lightning jaggedness
float noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);

    float a = hash(i);
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// Distance from point to line segment
float distanceToSegment(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    return length(pa - ba * h);
}

// Flicker function for lightning intensity
float flicker(float t, float speed)
{
    float f1 = sin(t * speed) * 0.5 + 0.5;
    float f2 = sin(t * speed * 2.7) * 0.5 + 0.5;
    float f3 = sin(t * speed * 4.3) * 0.5 + 0.5;
    return (f1 * 0.5 + f2 * 0.3 + f3 * 0.2) * 0.6 + 0.4;
}

// Fullscreen triangle vertex shader
VSOutput VSMain(uint vertexId : SV_VertexID)
{
    float2 texcoord = float2((vertexId << 1) & 2, vertexId & 2);
    VSOutput output;
    output.Position = float4(texcoord * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y;
    output.TexCoord = texcoord;
    return output;
}

// Pixel shader - renders all lightning bolts and sparks
float4 PSMain(VSOutput input) : SV_TARGET
{
    float2 pixelPos = input.TexCoord * ViewportSize;
    float4 finalColor = float4(0, 0, 0, 0);

    // Render all lightning bolts
    for (int i = 0; i < 256; i++) // MaxBolts
    {
        LightningBolt bolt = Bolts[i];

        // Skip dead bolts
        if (bolt.Lifetime <= 0)
            continue;

        // Calculate life factor for fade
        float lifeFactor = bolt.Lifetime / bolt.MaxLifetime;

        // Flicker effect
        float flickerValue = flicker(Time + i * 0.1, FlickerSpeed);

        // Calculate main bolt path
        float2 boltDir = bolt.EndPos - bolt.StartPos;
        float boltLength = length(boltDir);
        float2 boltNorm = boltDir / boltLength;
        float2 boltPerp = float2(-boltNorm.y, boltNorm.x);

        // Sample jagged displacement along the bolt
        float t = dot(pixelPos - bolt.StartPos, boltNorm) / boltLength;
        float2 samplePos = bolt.StartPos + boltNorm * t * boltLength;

        // Create jagged lightning path using noise
        float noiseScale = 20.0;
        float displacement = (noise(samplePos * 0.1 + float2(Time * 10.0, i)) - 0.5) * noiseScale;
        displacement += (noise(samplePos * 0.3 + float2(Time * 20.0, i * 2)) - 0.5) * noiseScale * 0.5;

        // Calculate distance to jagged bolt
        float2 jaggedOffset = boltPerp * displacement;
        float2 jaggedPos = lerp(bolt.StartPos, bolt.EndPos, saturate(t)) + jaggedOffset;
        float dist = length(pixelPos - jaggedPos);

        // Main bolt rendering
        if (t >= 0 && t <= 1.0)
        {
            float thickness = BoltThickness * (0.5 + lifeFactor * 0.5);

            // Core bolt (bright)
            float core = 1.0 - smoothstep(0.0, thickness, dist);

            // Glow around bolt
            float glow = exp(-dist * dist / (thickness * thickness * 4.0)) * GlowIntensity;

            // Combine core and glow
            float intensity = (core + glow) * lifeFactor * flickerValue * bolt.Intensity;

            // Apply color
            float4 boltColor = bolt.Color * intensity;

            // HDR boost for bright core
            boltColor.rgb *= 1.0 + core * HdrMultiplier * 2.0;

            // Additive blending
            finalColor.rgb += boltColor.rgb;
            finalColor.a = max(finalColor.a, boltColor.a);
        }

        // Render branches
        if (bolt.BranchCount > 0)
        {
            float branchStep = 1.0 / (bolt.BranchCount + 1);

            for (int b = 1; b <= bolt.BranchCount; b++)
            {
                float branchT = branchStep * b;
                float2 branchStart = lerp(bolt.StartPos, bolt.EndPos, branchT);

                // Random branch angle and length
                float branchSeed = hash(float2(i, b));
                float branchAngle = (branchSeed - 0.5) * 1.5; // +/- 85 degrees
                float branchLength = boltLength * 0.3 * (0.5 + branchSeed * 0.5);

                // Calculate branch direction and perpendicular
                float2 branchDir = float2(
                    boltNorm.x * cos(branchAngle) - boltNorm.y * sin(branchAngle),
                    boltNorm.x * sin(branchAngle) + boltNorm.y * cos(branchAngle)
                );
                float2 branchPerp = float2(-branchDir.y, branchDir.x);

                float2 branchEnd = branchStart + branchDir * branchLength;

                // Calculate position along branch for jagged displacement
                float branchLocalT = dot(pixelPos - branchStart, branchDir) / branchLength;

                // Only render if we're in the branch range
                if (branchLocalT >= 0 && branchLocalT <= 1.0)
                {
                    // Sample jagged displacement along the branch (similar to main bolt)
                    float2 branchSamplePos = branchStart + branchDir * branchLocalT * branchLength;
                    float branchNoiseScale = 12.0;
                    float branchDisplacement = (noise(branchSamplePos * 0.15 + float2(Time * 10.0, i + b * 10)) - 0.5) * branchNoiseScale;
                    branchDisplacement += (noise(branchSamplePos * 0.4 + float2(Time * 20.0, i * 2 + b * 5)) - 0.5) * branchNoiseScale * 0.5;

                    // Calculate jagged branch position
                    float2 jaggedBranchOffset = branchPerp * branchDisplacement;
                    float2 jaggedBranchPos = lerp(branchStart, branchEnd, saturate(branchLocalT)) + jaggedBranchOffset;
                    float branchDist = length(pixelPos - jaggedBranchPos);

                    float branchThickness = BoltThickness * 0.5;

                    // Branch core and glow
                    float branchCore = 1.0 - smoothstep(0.0, branchThickness, branchDist);
                    float branchGlow = exp(-branchDist * branchDist / (branchThickness * branchThickness * 4.0)) * GlowIntensity * 0.7;

                    float branchIntensity = (branchCore + branchGlow) * lifeFactor * flickerValue * BranchIntensity;

                    float4 branchColor = bolt.Color * branchIntensity;
                    branchColor.rgb *= 1.0 + branchCore * HdrMultiplier * 1.5;

                    finalColor.rgb += branchColor.rgb;
                    finalColor.a = max(finalColor.a, branchColor.a);
                }
            }
        }
    }

    // Render impact sparks
    for (int s = 0; s < 512; s++) // MaxSparks
    {
        ImpactSpark spark = Sparks[s];

        // Skip dead sparks (alpha = 0)
        if (spark.Color.w <= 0)
            continue;

        float sparkDist = length(pixelPos - spark.Position);
        float sparkSize = 3.0;

        // Spark rendering with glow
        float sparkCore = 1.0 - smoothstep(0.0, sparkSize, sparkDist);
        float sparkGlow = exp(-sparkDist * sparkDist / (sparkSize * sparkSize * 2.0)) * 0.5;

        float sparkIntensity = (sparkCore + sparkGlow) * spark.Color.w;

        float4 sparkColor = float4(spark.Color.rgb, 1.0) * sparkIntensity;
        sparkColor.rgb *= 1.0 + sparkCore * HdrMultiplier;

        finalColor.rgb += sparkColor.rgb;
        finalColor.a = max(finalColor.a, sparkColor.a);
    }

    // Screen flash effect
    if (FlashIntensity > 0.01)
    {
        float flashDist = length(pixelPos - MousePosition);
        float flashRadius = 800.0;
        float flash = (1.0 - smoothstep(0.0, flashRadius, flashDist)) * FlashIntensity * 0.3;
        finalColor.rgb += float3(flash, flash, flash) * 0.5;
    }

    // Ensure we don't exceed reasonable values
    finalColor = saturate(finalColor);

    if (finalColor.a < 0.01)
        discard;

    return finalColor;
}
