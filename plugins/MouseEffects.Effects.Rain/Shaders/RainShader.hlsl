// Rain Shader - Raindrop streaks with splash effects

cbuffer FrameConstants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float4 Padding;
}

struct RaindropInstance
{
    float2 Position;
    float2 Velocity;
    float4 Color;
    float Size;
    float Length;
    float Lifetime;
    float MaxLifetime;
    float Intensity;
    float IsSplash;
    float SplashRadius;
    float SplashAge;
    float4 Padding;
};

StructuredBuffer<RaindropInstance> Raindrops : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR0;
    float Alpha : TEXCOORD1;
    float Intensity : TEXCOORD2;
    float IsSplash : TEXCOORD3;
    float2 Velocity : TEXCOORD4;
    float DropLength : TEXCOORD5;
    float SplashRadius : TEXCOORD6;
};

// Vertex shader - Generate quad per raindrop/splash instance
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;
    RaindropInstance drop = Raindrops[instanceId];

    // Skip dead raindrops
    if (drop.Lifetime <= 0)
    {
        output.Position = float4(0, 0, 0, 0);
        output.UV = float2(0, 0);
        output.Color = float4(0, 0, 0, 0);
        output.Alpha = 0;
        output.Intensity = 0;
        output.IsSplash = 0;
        output.Velocity = float2(0, 0);
        output.DropLength = 0;
        output.SplashRadius = 0;
        return output;
    }

    // Calculate alpha based on lifetime (fade in and out)
    float lifeFraction = drop.Lifetime / drop.MaxLifetime;
    float fadeIn = saturate((1.0 - lifeFraction) * 5.0);
    float fadeOut = saturate(lifeFraction * 2.0);
    float alpha = min(fadeIn, fadeOut) * drop.Intensity;

    // Generate quad vertices (two triangles)
    float2 quadUV;
    if (vertexId == 0) quadUV = float2(-1, -1);
    else if (vertexId == 1) quadUV = float2(1, -1);
    else if (vertexId == 2) quadUV = float2(-1, 1);
    else if (vertexId == 3) quadUV = float2(-1, 1);
    else if (vertexId == 4) quadUV = float2(1, -1);
    else quadUV = float2(1, 1);

    float2 offset;

    if (drop.IsSplash > 0.5)
    {
        // Splash - circular expansion
        float radius = drop.SplashRadius * 1.2; // Extra size for glow
        offset = quadUV * radius;
    }
    else
    {
        // Raindrop - elongated in direction of velocity
        float2 velocityDir = normalize(drop.Velocity);
        float2 perpDir = float2(-velocityDir.y, velocityDir.x);

        // Create elongated shape
        offset = perpDir * quadUV.x * drop.Size * 2.0 + velocityDir * quadUV.y * drop.Length;
    }

    // Position in screen space
    float2 screenPos = drop.Position + offset;

    // Convert to NDC
    float2 ndc = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y; // Flip Y for DirectX

    output.Position = float4(ndc, 0, 1);
    output.UV = quadUV;
    output.Color = drop.Color;
    output.Alpha = alpha;
    output.Intensity = drop.Intensity;
    output.IsSplash = drop.IsSplash;
    output.Velocity = normalize(drop.Velocity);
    output.DropLength = drop.Length;
    output.SplashRadius = drop.SplashRadius;

    return output;
}

// Line segment SDF
float sdLineSegment(float2 p, float2 a, float2 b, float width)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    return length(pa - ba * h) - width;
}

// Circle SDF
float sdCircle(float2 p, float radius)
{
    return length(p) - radius;
}

// Raindrop SDF - elongated shape in direction of velocity
float RaindropSDF(float2 p, float2 velocityDir, float width, float length)
{
    // Create perpendicular direction
    float2 perpDir = float2(-velocityDir.y, velocityDir.x);

    // Transform point to raindrop space
    float2 localP = float2(dot(p, perpDir), dot(p, velocityDir));

    // Main body - elongated ellipse
    float bodyDist = length(float2(localP.x / width, localP.y / (length * 0.5))) - 1.0;

    // Add teardrop shape at tail
    if (localP.y < 0)
    {
        float tailDist = length(localP - float2(0, -length * 0.3)) - width * 0.7;
        bodyDist = min(bodyDist, tailDist);
    }

    return bodyDist;
}

// Splash ring SDF
float SplashSDF(float2 p, float radius)
{
    float dist = abs(length(p) - radius * 0.7);
    return dist;
}

// Pixel shader - Render raindrop or splash
float4 PSMain(VSOutput input) : SV_TARGET
{
    if (input.Alpha <= 0.001)
        discard;

    float4 color = input.Color;
    float intensity = 0;

    if (input.IsSplash > 0.5)
    {
        // Render splash effect
        float dist = SplashSDF(input.UV, 1.0);

        // Create main ring
        float ring = 1.0 - smoothstep(0.0, 0.15, dist);

        // Create glow around ring
        float glow1 = 1.0 - smoothstep(0.0, 0.3, dist);
        float glow2 = 1.0 - smoothstep(0.0, 0.5, dist);

        // Combine layers
        intensity = ring * 2.5 + glow1 * 1.0 + glow2 * 0.4;

        // Add some variation to splash
        float angle = atan2(input.UV.y, input.UV.x);
        float variation = 0.8 + 0.2 * sin(angle * 8.0 + Time * 2.0);
        intensity *= variation;
    }
    else
    {
        // Render raindrop streak
        float dist = RaindropSDF(input.UV, input.Velocity, 0.3, 0.8);

        // Create sharp core
        float core = 1.0 - smoothstep(-0.05, 0.0, dist);

        // Create glow
        float glow1 = 1.0 - smoothstep(-0.05, 0.15, dist);
        float glow2 = 1.0 - smoothstep(-0.05, 0.3, dist);

        // Combine layers
        intensity = core * 3.0 + glow1 * 1.5 + glow2 * 0.6;

        // Add motion blur effect (brighter at front, dimmer at tail)
        float motionGradient = saturate(input.UV.y * 0.5 + 0.5);
        intensity *= 0.7 + motionGradient * 0.3;
    }

    // Apply intensity and alpha
    intensity *= input.Intensity;
    color.rgb *= intensity;
    color.a = saturate(intensity * input.Alpha);

    // Apply HDR multiplier
    color.rgb *= HdrMultiplier;

    return color;
}
