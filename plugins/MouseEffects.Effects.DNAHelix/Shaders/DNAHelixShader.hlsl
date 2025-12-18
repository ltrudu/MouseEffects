cbuffer Constants : register(b0)
{
    float2 ViewportSize;
    float2 MousePosition;
    float Time;
    float HelixHeight;
    float HelixRadius;
    float TwistRate;
    float RotationSpeed;
    float StrandThickness;
    float GlowIntensity;
    int BasePairCount;
    float3 Strand1Color;
    float Padding1;
    float3 Strand2Color;
    float Padding2;
    float3 BasePairColor1;
    float Padding3;
    float3 BasePairColor2;
    float Padding4;
}

struct VSOutput
{
    float4 Position : SV_Position;
    float2 TexCoord : TEXCOORD0;
};

VSOutput VS(uint vertexId : SV_VertexID)
{
    VSOutput output;
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y;
    output.TexCoord = uv;
    return output;
}

static const float PI = 3.14159265359;

// Calculate helix strand position
float3 GetHelixPosition(float y, float time, float offset)
{
    float angle = y * TwistRate + time * RotationSpeed + offset;
    float x = cos(angle) * HelixRadius;
    float z = sin(angle);
    return float3(x, y, z);
}

// Distance from point to line segment
float DistanceToSegment(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    return length(pa - ba * h);
}

// Smooth glow function
float Glow(float dist, float radius, float intensity)
{
    return pow(saturate(1.0 - dist / radius), 2.0) * intensity;
}

// Z-depth to alpha conversion for 3D effect
float DepthAlpha(float z)
{
    // z ranges from -1 (back) to 1 (front)
    // Map to alpha: back strands are dimmer
    return lerp(0.3, 1.0, (z + 1.0) * 0.5);
}

float4 PS(VSOutput input) : SV_Target
{
    float2 pixelPos = input.TexCoord * ViewportSize;
    float2 toMouse = pixelPos - MousePosition;

    float4 color = float4(0, 0, 0, 0);

    // Define vertical range of helix
    float helixStart = -HelixHeight * 0.5;
    float helixEnd = HelixHeight * 0.5;

    // Sample points along helix height
    int samples = 100;
    float stepSize = HelixHeight / float(samples);

    for (int i = 0; i < samples; i++)
    {
        float y = helixStart + i * stepSize;

        // Get 3D positions for both strands
        float3 pos1 = GetHelixPosition(y, Time, 0.0);
        float3 pos2 = GetHelixPosition(y, Time, PI);

        // Next sample for line segment
        float yNext = y + stepSize;
        float3 pos1Next = GetHelixPosition(yNext, Time, 0.0);
        float3 pos2Next = GetHelixPosition(yNext, Time, PI);

        // Project to 2D screen space (simple orthographic projection)
        float2 p1 = MousePosition + float2(pos1.x, pos1.y);
        float2 p1Next = MousePosition + float2(pos1Next.x, pos1Next.y);
        float2 p2 = MousePosition + float2(pos2.x, pos2.y);
        float2 p2Next = MousePosition + float2(pos2Next.x, pos2Next.y);

        // Distance to strand segments
        float dist1 = DistanceToSegment(pixelPos, p1, p1Next);
        float dist2 = DistanceToSegment(pixelPos, p2, p2Next);

        // Calculate depth-based alpha
        float alpha1 = DepthAlpha(pos1.z);
        float alpha2 = DepthAlpha(pos2.z);

        // Draw strand 1 (blue)
        if (dist1 < StrandThickness * 2.0)
        {
            float intensity1 = Glow(dist1, StrandThickness * 2.0, GlowIntensity);
            float3 strand1Glow = Strand1Color * intensity1 * alpha1;
            color.rgb += strand1Glow;
            color.a = max(color.a, intensity1 * alpha1);
        }

        // Draw strand 2 (red)
        if (dist2 < StrandThickness * 2.0)
        {
            float intensity2 = Glow(dist2, StrandThickness * 2.0, GlowIntensity);
            float3 strand2Glow = Strand2Color * intensity2 * alpha2;
            color.rgb += strand2Glow;
            color.a = max(color.a, intensity2 * alpha2);
        }
    }

    // Draw base pairs (connecting rungs)
    float basePairSpacing = HelixHeight / float(BasePairCount);

    for (int j = 0; j < BasePairCount; j++)
    {
        float y = helixStart + j * basePairSpacing;

        // Get positions of both strands at this height
        float3 pos1 = GetHelixPosition(y, Time, 0.0);
        float3 pos2 = GetHelixPosition(y, Time, PI);

        // Project to screen
        float2 p1 = MousePosition + float2(pos1.x, pos1.y);
        float2 p2 = MousePosition + float2(pos2.x, pos2.y);

        // Distance to base pair line
        float distBP = DistanceToSegment(pixelPos, p1, p2);

        // Average depth for base pair
        float avgZ = (pos1.z + pos2.z) * 0.5;
        float alphaBP = DepthAlpha(avgZ);

        // Alternate base pair colors (simulating A-T and G-C pairs)
        float3 basePairColor = (j % 2 == 0) ? BasePairColor1 : BasePairColor2;

        if (distBP < StrandThickness * 1.5)
        {
            float intensityBP = Glow(distBP, StrandThickness * 1.5, GlowIntensity * 0.7);
            float3 bpGlow = basePairColor * intensityBP * alphaBP;
            color.rgb += bpGlow;
            color.a = max(color.a, intensityBP * alphaBP * 0.8);
        }
    }

    // Add connection points (nucleotides) at strand-base pair junctions
    for (int k = 0; k < BasePairCount; k++)
    {
        float y = helixStart + k * basePairSpacing;

        float3 pos1 = GetHelixPosition(y, Time, 0.0);
        float3 pos2 = GetHelixPosition(y, Time, PI);

        float2 p1 = MousePosition + float2(pos1.x, pos1.y);
        float2 p2 = MousePosition + float2(pos2.x, pos2.y);

        // Draw small spheres at junction points
        float dist1Point = length(pixelPos - p1);
        float dist2Point = length(pixelPos - p2);

        float alpha1 = DepthAlpha(pos1.z);
        float alpha2 = DepthAlpha(pos2.z);

        float pointRadius = StrandThickness * 1.5;

        if (dist1Point < pointRadius)
        {
            float intensity = Glow(dist1Point, pointRadius, GlowIntensity * 1.2);
            color.rgb += Strand1Color * intensity * alpha1;
            color.a = max(color.a, intensity * alpha1);
        }

        if (dist2Point < pointRadius)
        {
            float intensity = Glow(dist2Point, pointRadius, GlowIntensity * 1.2);
            color.rgb += Strand2Color * intensity * alpha2;
            color.a = max(color.a, intensity * alpha2);
        }
    }

    // Clamp values
    color.rgb = saturate(color.rgb);
    color.a = saturate(color.a);

    return color;
}
