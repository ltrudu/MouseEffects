// Tesla Lightning Effect Shader
// Renders procedural lightning bolts at mouse position

static const float TAU = 6.28318530718;
static const float PI = 3.14159265359;

cbuffer TeslaConstants : register(b0)
{
    float2 ViewportSize;
    float2 MousePosition;
    float Time;
    float BoltIntensity;
    float BoltThickness;
    float FlickerSpeed;
    float CoreRadius;
    float CoreEnabled;
    float GlowIntensity;
    float FadeDuration;
    float4 CoreColor;
};

struct BoltInstance
{
    float2 Position;
    float Angle;
    float Length;
    float4 Color;
    float Lifetime;
    float MaxLifetime;
    float BranchSeed;
    float Padding;
};

StructuredBuffer<BoltInstance> Bolts : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float2 ScreenPos : TEXCOORD1;
};

// ===== Helper Functions =====

float2x2 Rotate(float angle)
{
    float c = cos(angle);
    float s = sin(angle);
    return float2x2(c, s, -s, c);
}

float CircleSDF(float2 p, float r)
{
    return length(p) - r;
}

float LineSDF(float2 p, float2 a, float2 b, float thickness)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = clamp(dot(pa, ba) / dot(ba, ba), 0.0, 1.0);
    return length(pa - ba * h) - thickness;
}

float RandomFloat(float2 seed)
{
    seed = sin(seed * float2(123.45, 546.23)) * 345.21 + 12.57;
    return frac(seed.x * seed.y);
}

float SimpleNoise(float2 uv, float octaves)
{
    float sn = 0.0;
    float amplitude = 3.0;
    float deno = 0.0;
    octaves = clamp(octaves, 1.0, 6.0);

    for (float i = 1.0; i <= octaves; i++)
    {
        float2 grid = smoothstep(0.0, 1.0, frac(uv));
        float2 id = floor(uv);
        float2 offs = float2(0.0, 1.0);
        float bl = RandomFloat(id);
        float br = RandomFloat(id + offs.yx);
        float tl = RandomFloat(id + offs);
        float tr = RandomFloat(id + offs.yy);
        sn += lerp(lerp(bl, br, grid.x), lerp(tl, tr, grid.x), grid.y) * amplitude;
        deno += amplitude;
        uv *= 3.5;
        amplitude *= 0.5;
    }
    return sn / deno;
}

// Render a single lightning bolt segment
float3 RenderBoltSegment(float2 uv, float len, float seed, float3 color, float branchProb, float timeOffset)
{
    float2 t = float2(0.0, fmod(Time + timeOffset, 200.0) * 2.0);

    // Main bolt with noise distortion
    float sn = SimpleNoise(uv * 20.0 - t * 3.0 + float2(seed * 2.5, 0.0), 2.0) * 2.0 - 1.0;
    uv.x += sn * 0.02 * smoothstep(0.02, 0.1, abs(uv.y));

    float thickness = BoltThickness * 0.001;
    float3 l = LineSDF(uv, float2(0.0, 0.0), float2(0.0, len), thickness);
    l = BoltIntensity / max(0.001, l) * color;
    l = saturate(1.0 - exp(l * -0.02)) * smoothstep(len - 0.01, 0.0, abs(uv.y));
    float3 bolt = l;

    // Secondary branch (diagonal)
    if (branchProb > 0.3)
    {
        float2 branchUV = mul(Rotate(TAU * 0.125), uv);
        sn = SimpleNoise(branchUV * 25.0 - t * 4.0, 2.0) * 2.0 - 1.0;
        branchUV.x += sn * branchUV.y * 0.8 * smoothstep(0.1, 0.25, len) * branchProb;
        float branchLen = len * 0.5;
        l = LineSDF(branchUV, float2(0.0, 0.0), float2(0.0, branchLen), thickness * 0.8);
        l = (BoltIntensity * 0.7) / max(0.001, l) * color;
        l = saturate(1.0 - exp(l * -0.03)) * smoothstep(branchLen * 0.7, 0.0, abs(branchUV.y));
        bolt += l;
    }

    // Flicker effect
    float hz = FlickerSpeed * Time * TAU;
    float r = RandomFloat(float2(seed, seed * 1.23)) * 0.5 * TAU;
    float flicker = sin(hz + r) * 0.5 + 0.5;

    return bolt * smoothstep(0.5, 0.0, flicker);
}

// ===== Vertex Shader =====
// Fullscreen triangle (no vertex buffer needed)
VSOutput VSMain(uint vertexId : SV_VertexID)
{
    VSOutput output;
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y;
    output.TexCoord = uv;
    output.ScreenPos = uv * ViewportSize;
    return output;
}

// ===== Pixel Shader =====
float4 PSMain(VSOutput input) : SV_TARGET
{
    float2 screenPos = input.ScreenPos;
    float3 finalColor = float3(0.0, 0.0, 0.0);

    // Process each bolt
    uint boltCount;
    uint stride;
    Bolts.GetDimensions(boltCount, stride);

    for (uint i = 0; i < boltCount; i++)
    {
        BoltInstance bolt = Bolts[i];

        // Skip dead bolts
        if (bolt.Lifetime <= 0.0)
            continue;

        // Calculate fade based on lifetime
        float lifeFactor = bolt.Lifetime / bolt.MaxLifetime;
        float fadeStart = FadeDuration / bolt.MaxLifetime;
        float fade = smoothstep(0.0, fadeStart + 0.01, lifeFactor);

        // Transform screen position relative to bolt origin
        float2 localPos = (screenPos - bolt.Position) / ViewportSize.y;

        // Rotate to align bolt with angle (bolt points outward from origin)
        float2 rotatedPos = mul(Rotate(-bolt.Angle - PI * 0.5), localPos);

        // Normalize length
        float normalizedLen = bolt.Length / ViewportSize.y;

        // Render bolt
        float3 boltColor = RenderBoltSegment(
            rotatedPos,
            normalizedLen,
            bolt.BranchSeed,
            bolt.Color.rgb,
            bolt.BranchSeed,
            bolt.BranchSeed * 10.0
        );

        finalColor += boltColor * fade * bolt.Color.a;
    }

    // Render core glow if enabled
    if (CoreEnabled > 0.5)
    {
        float2 coreUV = (screenPos - MousePosition) / ViewportSize.y;
        float coreNormRadius = CoreRadius / ViewportSize.y;

        // Animated core with noise
        float r = coreNormRadius * SimpleNoise(coreUV * 50.0 - float2(0.0, fmod(Time, 200.0) * 5.0), 3.0);
        float coreDist = CircleSDF(coreUV, r);
        float3 core = GlowIntensity / max(0.001, coreDist) * CoreColor.rgb;
        core = 1.0 - exp(core * -0.05);
        finalColor += core * CoreColor.a;
    }

    // Apply overall glow intensity
    finalColor *= GlowIntensity;

    float alpha = saturate(length(finalColor) * 1.5);

    if (alpha < 0.01)
        discard;

    return float4(finalColor, alpha);
}
