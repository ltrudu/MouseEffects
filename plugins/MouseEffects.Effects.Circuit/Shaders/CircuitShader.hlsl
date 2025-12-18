// Circuit Shader - PCB-style circuit traces
// Renders horizontal/vertical traces with nodes and energy flow

cbuffer Constants : register(b0)
{
    float2 ViewportSize;      // 8 bytes
    float Time;               // 4 bytes
    float GlowIntensity;      // 4 bytes
    float NodeSize;           // 4 bytes
    float LineThickness;      // 4 bytes
    float HdrMultiplier;      // 4 bytes
    float Padding;            // 4 bytes
    float4 TraceColor;        // 16 bytes
}

struct TraceSegment
{
    float2 Start;             // Start position
    float2 End;               // End position
    float Progress;           // Growth progress (0->1)
    float Lifetime;           // Current lifetime
    float MaxLifetime;        // Total lifetime
    int Direction;            // 0=right, 1=down, 2=left, 3=up
    float4 Color;             // Trace color
};

StructuredBuffer<TraceSegment> Segments : register(t0);

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

// SDF for line segment
float lineSDF(float2 p, float2 a, float2 b, float thickness)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    return length(pa - ba * h) - thickness;
}

// SDF for circle (node)
float circleSDF(float2 p, float2 center, float radius)
{
    return length(p - center) - radius;
}

// Hash function for pseudo-random numbers
float hash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}

// Smooth pulse animation along trace
float energyPulse(float t, float speed, float frequency)
{
    return 0.5 + 0.5 * sin(t * speed + frequency);
}

float4 PSMain(VSOutput input) : SV_TARGET
{
    float2 pixelPos = input.TexCoord * ViewportSize;
    float4 finalColor = float4(0, 0, 0, 0);

    // Process all segments
    for (int i = 0; i < 512; i++)
    {
        TraceSegment seg = Segments[i];

        // Skip inactive segments
        if (seg.Lifetime <= 0)
            continue;

        // Calculate visible portion of segment based on growth progress
        float2 visibleEnd = lerp(seg.Start, seg.End, seg.Progress);

        // Calculate fade based on lifetime
        float lifetimeFade = seg.Lifetime / seg.MaxLifetime;
        float fadeAlpha = smoothstep(0.0, 0.2, lifetimeFade);

        // Line segment rendering
        float lineDist = lineSDF(pixelPos, seg.Start, visibleEnd, LineThickness);

        if (lineDist < 10.0)
        {
            // Core line
            float lineCore = 1.0 - smoothstep(0.0, 0.5, lineDist);

            // Glow layers
            float glow1 = 1.0 - smoothstep(0.0, LineThickness * 2.0, lineDist);
            float glow2 = 1.0 - smoothstep(0.0, LineThickness * 4.0, lineDist);
            float glow3 = 1.0 - smoothstep(0.0, LineThickness * 6.0, lineDist);

            // Energy pulse effect
            float segmentLength = length(visibleEnd - seg.Start);
            float2 lineVec = visibleEnd - seg.Start;
            float2 pixelVec = pixelPos - seg.Start;
            float alongLine = segmentLength > 0 ? dot(pixelVec, normalize(lineVec)) / segmentLength : 0;

            // Multiple pulses traveling along the trace
            float pulse1 = energyPulse(alongLine * 10.0 - Time * 8.0, 1.0, hash(seg.Start) * 100.0);
            float pulse2 = energyPulse(alongLine * 15.0 - Time * 12.0, 1.0, hash(seg.End) * 100.0);
            float pulseEffect = (pulse1 + pulse2) * 0.5;

            // Combine layers with pulse
            float intensity = lineCore * 1.5 + glow1 * 0.8 + glow2 * 0.4 + glow3 * 0.2;
            intensity *= (0.7 + pulseEffect * 0.3);
            intensity *= GlowIntensity;
            intensity *= fadeAlpha;

            float4 color = seg.Color * intensity;
            finalColor += color;
        }

        // Node at start position
        float nodeDistStart = circleSDF(pixelPos, seg.Start, NodeSize);
        if (nodeDistStart < NodeSize * 2.0)
        {
            float nodeCore = 1.0 - smoothstep(0.0, 0.5, nodeDistStart);
            float nodeGlow = 1.0 - smoothstep(0.0, NodeSize * 2.0, nodeDistStart);

            // Pulsing node
            float nodePulse = energyPulse(Time * 5.0, 1.0, hash(seg.Start) * 100.0);
            float nodeIntensity = (nodeCore * 2.0 + nodeGlow) * (0.8 + nodePulse * 0.2);
            nodeIntensity *= GlowIntensity;
            nodeIntensity *= fadeAlpha;

            finalColor += seg.Color * nodeIntensity;
        }

        // Node at current end position (growing edge)
        if (seg.Progress > 0.1)
        {
            float nodeDistEnd = circleSDF(pixelPos, visibleEnd, NodeSize * 1.2);
            if (nodeDistEnd < NodeSize * 2.5)
            {
                float nodeCore = 1.0 - smoothstep(0.0, 0.5, nodeDistEnd);
                float nodeGlow = 1.0 - smoothstep(0.0, NodeSize * 2.5, nodeDistEnd);

                // Brighter at growing edge
                float edgePulse = energyPulse(Time * 8.0, 1.0, hash(visibleEnd) * 100.0);
                float nodeIntensity = (nodeCore * 3.0 + nodeGlow * 1.5) * (0.9 + edgePulse * 0.1);
                nodeIntensity *= GlowIntensity;
                nodeIntensity *= fadeAlpha;

                finalColor += seg.Color * nodeIntensity;
            }
        }
    }

    // Apply HDR multiplier
    finalColor.rgb *= HdrMultiplier;

    // Clamp to prevent excessive brightness
    finalColor = saturate(finalColor);

    return finalColor;
}
