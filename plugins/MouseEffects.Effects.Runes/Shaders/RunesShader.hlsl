cbuffer FrameConstants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float4 Padding;
}

struct RuneInstance
{
    float2 Position;
    float2 FloatOffset;
    float4 Color;
    float Size;
    float Rotation;
    float RotationSpeed;
    float Lifetime;
    float MaxLifetime;
    int RuneType;
    float GlowIntensity;
    float FloatPhase;
    float BirthTime;
    float FloatDistance;
    float Padding1;
    float Padding2;
};

StructuredBuffer<RuneInstance> Runes : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR0;
    float Alpha : TEXCOORD1;
    float GlowIntensity : TEXCOORD2;
    int RuneType : TEXCOORD3;
};

VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;
    RuneInstance rune = Runes[instanceId];

    if (rune.Lifetime <= 0)
    {
        output.Position = float4(0, 0, 0, 0);
        output.UV = float2(0, 0);
        output.Color = float4(0, 0, 0, 0);
        output.Alpha = 0;
        output.GlowIntensity = 0;
        output.RuneType = 0;
        return output;
    }

    float lifeFraction = rune.Lifetime / rune.MaxLifetime;
    float fadeIn = saturate((1.0 - lifeFraction) * 3.0);
    float fadeOut = saturate(lifeFraction * 1.5);
    float alpha = min(fadeIn, fadeOut);

    float2 quadUV;
    if (vertexId == 0) quadUV = float2(-1, -1);
    else if (vertexId == 1) quadUV = float2(1, -1);
    else if (vertexId == 2) quadUV = float2(-1, 1);
    else if (vertexId == 3) quadUV = float2(-1, 1);
    else if (vertexId == 4) quadUV = float2(1, -1);
    else quadUV = float2(1, 1);

    float c = cos(rune.Rotation);
    float s = sin(rune.Rotation);
    float2x2 rotation = float2x2(c, -s, s, c);
    float2 rotatedUV = mul(rotation, quadUV);

    float floatX = sin(rune.FloatPhase + rune.FloatOffset.x) * rune.FloatDistance;
    float floatY = cos(rune.FloatPhase * 0.7 + rune.FloatOffset.y) * rune.FloatDistance * 0.5;
    float2 floatOffset = float2(floatX, floatY);

    float2 offset = rotatedUV * rune.Size;
    float2 screenPos = rune.Position + offset + floatOffset;

    float2 ndc = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y;

    output.Position = float4(ndc, 0, 1);
    output.UV = quadUV;
    output.Color = rune.Color;
    output.Alpha = alpha;
    output.GlowIntensity = rune.GlowIntensity;
    output.RuneType = rune.RuneType;

    return output;
}

float LineSDF(float2 p, float2 a, float2 b, float thickness)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    return length(pa - ba * h) - thickness;
}

float CircleSDF(float2 p, float r)
{
    return length(p) - r;
}

float RuneSDF(float2 p, int type, float lineWidth)
{
    float result = 1e10;

    if (type == 0)
    {
        result = min(result, LineSDF(p, float2(0, -0.7), float2(0, 0.7), lineWidth));
        result = min(result, LineSDF(p, float2(-0.5, 0), float2(0.5, 0), lineWidth));
        result = min(result, LineSDF(p, float2(-0.3, -0.4), float2(0.3, -0.4), lineWidth));
        result = min(result, LineSDF(p, float2(-0.3, 0.4), float2(0.3, 0.4), lineWidth));
    }
    else if (type == 1)
    {
        result = min(result, LineSDF(p, float2(0, -0.7), float2(-0.6, 0.5), lineWidth));
        result = min(result, LineSDF(p, float2(-0.6, 0.5), float2(0.6, 0.5), lineWidth));
        result = min(result, LineSDF(p, float2(0.6, 0.5), float2(0, -0.7), lineWidth));
        result = min(result, CircleSDF(p, 0.3));
    }
    else if (type == 2)
    {
        result = min(result, LineSDF(p, float2(-0.5, -0.5), float2(0.5, 0.5), lineWidth));
        result = min(result, LineSDF(p, float2(-0.5, 0.5), float2(0.5, -0.5), lineWidth));
        result = min(result, CircleSDF(p, 0.5));
    }
    else if (type == 3)
    {
        result = min(result, LineSDF(p, float2(0, -0.7), float2(0, 0.7), lineWidth));
        result = min(result, LineSDF(p, float2(-0.6, -0.6), float2(-0.6, 0.6), lineWidth));
        result = min(result, LineSDF(p, float2(0.6, -0.6), float2(0.6, 0.6), lineWidth));
        result = min(result, LineSDF(p, float2(-0.6, -0.6), float2(0, -0.3), lineWidth));
        result = min(result, LineSDF(p, float2(0.6, -0.6), float2(0, -0.3), lineWidth));
    }
    else if (type == 4)
    {
        result = min(result, CircleSDF(p, 0.6));
        result = min(result, CircleSDF(p, 0.4));
        result = min(result, LineSDF(p, float2(-0.7, 0), float2(0.7, 0), lineWidth));
        result = min(result, LineSDF(p, float2(0, -0.7), float2(0, 0.7), lineWidth));
    }
    else if (type == 5)
    {
        result = min(result, LineSDF(p, float2(0, -0.7), float2(0, 0), lineWidth));
        result = min(result, LineSDF(p, float2(0, 0), float2(-0.5, 0.5), lineWidth));
        result = min(result, LineSDF(p, float2(0, 0), float2(0.5, 0.5), lineWidth));
        result = min(result, CircleSDF(p + float2(0, -0.3), 0.3));
    }
    else if (type == 6)
    {
        result = min(result, LineSDF(p, float2(-0.6, -0.6), float2(0.6, -0.6), lineWidth));
        result = min(result, LineSDF(p, float2(0.6, -0.6), float2(0.6, 0.6), lineWidth));
        result = min(result, LineSDF(p, float2(0.6, 0.6), float2(-0.6, 0.6), lineWidth));
        result = min(result, LineSDF(p, float2(-0.6, 0.6), float2(-0.6, -0.6), lineWidth));
        result = min(result, LineSDF(p, float2(-0.6, -0.6), float2(0.6, 0.6), lineWidth));
        result = min(result, LineSDF(p, float2(-0.6, 0.6), float2(0.6, -0.6), lineWidth));
    }
    else if (type == 7)
    {
        result = min(result, LineSDF(p, float2(0, -0.7), float2(0.5, 0), lineWidth));
        result = min(result, LineSDF(p, float2(0.5, 0), float2(0, 0.7), lineWidth));
        result = min(result, LineSDF(p, float2(0, 0.7), float2(-0.5, 0), lineWidth));
        result = min(result, LineSDF(p, float2(-0.5, 0), float2(0, -0.7), lineWidth));
        result = min(result, CircleSDF(p, 0.25));
    }
    else if (type == 8)
    {
        result = min(result, LineSDF(p, float2(-0.4, -0.7), float2(-0.4, 0.7), lineWidth));
        result = min(result, LineSDF(p, float2(0.4, -0.7), float2(0.4, 0.7), lineWidth));
        result = min(result, LineSDF(p, float2(-0.4, 0), float2(0.4, 0), lineWidth));
        result = min(result, LineSDF(p, float2(-0.6, -0.4), float2(-0.2, -0.4), lineWidth));
        result = min(result, LineSDF(p, float2(0.2, 0.4), float2(0.6, 0.4), lineWidth));
    }
    else
    {
        result = min(result, LineSDF(p, float2(0, -0.7), float2(0, 0.7), lineWidth));
        result = min(result, LineSDF(p, float2(-0.5, -0.3), float2(0.5, -0.3), lineWidth));
        result = min(result, LineSDF(p, float2(-0.5, 0.3), float2(0.5, 0.3), lineWidth));
        result = min(result, CircleSDF(p + float2(0, -0.5), 0.15));
        result = min(result, CircleSDF(p + float2(0, 0.5), 0.15));
    }

    return result;
}

float4 PSMain(VSOutput input) : SV_TARGET
{
    if (input.Alpha <= 0.001)
        discard;

    float lineWidth = 0.08;
    float dist = RuneSDF(input.UV, input.RuneType, lineWidth);

    float core = 1.0 - smoothstep(-0.02, 0.02, dist);
    float glow1 = 1.0 - smoothstep(-0.02, 0.15, dist);
    float glow2 = 1.0 - smoothstep(-0.02, 0.3, dist);
    float glow3 = 1.0 - smoothstep(-0.02, 0.5, dist);

    float intensity = core * 2.5 + glow1 * 1.2 + glow2 * 0.6 + glow3 * 0.3;
    intensity *= input.GlowIntensity;

    float twinkle = 0.85 + 0.15 * sin(Time * 3.0 + input.Position.x * 0.05 + input.Position.y * 0.05);
    intensity *= twinkle;

    float4 color = input.Color;
    color.rgb *= intensity;
    color.a = intensity * input.Alpha;

    color.rgb *= HdrMultiplier;

    return color;
}
