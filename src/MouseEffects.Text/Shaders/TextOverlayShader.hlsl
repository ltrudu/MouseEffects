// TextOverlayShader.hlsl - Centralized text rendering for MouseEffects
// Renders on top of effect output as a separate pass
//
// Entity Types:
// 0-9:   Digits 0-9
// 10:    Colon (:)
// 11:    Slash (/)
// 12:    Dot (.)
// 13:    Dash (-)
// 20-45: Letters A-Z
// 50:    Space (not rendered)
// 60:    Background rectangle

cbuffer FrameData : register(b0)
{
    float2 ViewportSize;
    float Time;
    float GlowIntensity;
    float HdrMultiplier;
    float Padding1;
    float Padding2;
    float Padding3;
    float2 Padding4;
    float2 Padding5;
};

struct TextEntity
{
    float2 Position;
    float2 Size;           // For background: half-size; otherwise unused
    float4 Color;
    float CharacterSize;
    float GlowIntensity;
    float EntityType;
    float AnimationType;   // 0=none, 1=pulse, 2=wave, 3=rainbow, 4=breathing, 5=shake
    float AnimationPhase;
    float AnimationSpeed;
    float AnimationIntensity;
    float Padding;
};

StructuredBuffer<TextEntity> Entities : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
    float2 TexCoord : TEXCOORD0;
    float CharacterSize : TEXCOORD1;
    float GlowIntensity : TEXCOORD2;
    float EntityType : TEXCOORD3;
    float AnimationType : TEXCOORD4;
    float AnimationPhase : TEXCOORD5;
    float AnimationSpeed : TEXCOORD6;
    float AnimationIntensity : TEXCOORD7;
};

// ========== Character Drawing Functions ==========

// Draw a line segment from point a to point b with given width
float DrawSegment(float2 p, float2 a, float2 b, float width)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    float d = length(pa - ba * h);
    return smoothstep(width, width * 0.3, d);
}

// Draw a single digit (0-9) using 7-segment style
float DrawDigit(float2 uv, int digit)
{
    float2 p = uv * 2.0 - 1.0;

    float w = 0.15;
    float result = 0.0;

    // Segment positions (7-segment display layout)
    float2 a1 = float2(-0.4, -0.8), a2 = float2(0.4, -0.8);   // top
    float2 b1 = float2(0.5, -0.7), b2 = float2(0.5, -0.1);    // upper right
    float2 c1 = float2(0.5, 0.1), c2 = float2(0.5, 0.7);      // lower right
    float2 d1 = float2(-0.4, 0.8), d2 = float2(0.4, 0.8);     // bottom
    float2 e1 = float2(-0.5, 0.1), e2 = float2(-0.5, 0.7);    // lower left
    float2 f1 = float2(-0.5, -0.7), f2 = float2(-0.5, -0.1);  // upper left
    float2 g1 = float2(-0.4, 0.0), g2 = float2(0.4, 0.0);     // middle

    if (digit == 0) {
        result = max(result, DrawSegment(p, a1, a2, w));
        result = max(result, DrawSegment(p, b1, b2, w));
        result = max(result, DrawSegment(p, c1, c2, w));
        result = max(result, DrawSegment(p, d1, d2, w));
        result = max(result, DrawSegment(p, e1, e2, w));
        result = max(result, DrawSegment(p, f1, f2, w));
    }
    else if (digit == 1) {
        result = max(result, DrawSegment(p, b1, b2, w));
        result = max(result, DrawSegment(p, c1, c2, w));
    }
    else if (digit == 2) {
        result = max(result, DrawSegment(p, a1, a2, w));
        result = max(result, DrawSegment(p, b1, b2, w));
        result = max(result, DrawSegment(p, g1, g2, w));
        result = max(result, DrawSegment(p, e1, e2, w));
        result = max(result, DrawSegment(p, d1, d2, w));
    }
    else if (digit == 3) {
        result = max(result, DrawSegment(p, a1, a2, w));
        result = max(result, DrawSegment(p, b1, b2, w));
        result = max(result, DrawSegment(p, g1, g2, w));
        result = max(result, DrawSegment(p, c1, c2, w));
        result = max(result, DrawSegment(p, d1, d2, w));
    }
    else if (digit == 4) {
        result = max(result, DrawSegment(p, f1, f2, w));
        result = max(result, DrawSegment(p, g1, g2, w));
        result = max(result, DrawSegment(p, b1, b2, w));
        result = max(result, DrawSegment(p, c1, c2, w));
    }
    else if (digit == 5) {
        result = max(result, DrawSegment(p, a1, a2, w));
        result = max(result, DrawSegment(p, f1, f2, w));
        result = max(result, DrawSegment(p, g1, g2, w));
        result = max(result, DrawSegment(p, c1, c2, w));
        result = max(result, DrawSegment(p, d1, d2, w));
    }
    else if (digit == 6) {
        result = max(result, DrawSegment(p, a1, a2, w));
        result = max(result, DrawSegment(p, f1, f2, w));
        result = max(result, DrawSegment(p, e1, e2, w));
        result = max(result, DrawSegment(p, d1, d2, w));
        result = max(result, DrawSegment(p, c1, c2, w));
        result = max(result, DrawSegment(p, g1, g2, w));
    }
    else if (digit == 7) {
        result = max(result, DrawSegment(p, a1, a2, w));
        result = max(result, DrawSegment(p, b1, b2, w));
        result = max(result, DrawSegment(p, c1, c2, w));
    }
    else if (digit == 8) {
        result = max(result, DrawSegment(p, a1, a2, w));
        result = max(result, DrawSegment(p, b1, b2, w));
        result = max(result, DrawSegment(p, c1, c2, w));
        result = max(result, DrawSegment(p, d1, d2, w));
        result = max(result, DrawSegment(p, e1, e2, w));
        result = max(result, DrawSegment(p, f1, f2, w));
        result = max(result, DrawSegment(p, g1, g2, w));
    }
    else if (digit == 9) {
        result = max(result, DrawSegment(p, a1, a2, w));
        result = max(result, DrawSegment(p, b1, b2, w));
        result = max(result, DrawSegment(p, c1, c2, w));
        result = max(result, DrawSegment(p, d1, d2, w));
        result = max(result, DrawSegment(p, f1, f2, w));
        result = max(result, DrawSegment(p, g1, g2, w));
    }

    return result;
}

// Draw a colon (:)
float DrawColon(float2 uv)
{
    float2 p = uv * 2.0 - 1.0;

    float result = 0.0;
    float dotSize = 0.2;

    float upperDot = length(p - float2(0, -0.4)) - dotSize;
    result = max(result, smoothstep(0.05, 0.0, upperDot));

    float lowerDot = length(p - float2(0, 0.4)) - dotSize;
    result = max(result, smoothstep(0.05, 0.0, lowerDot));

    return result;
}

// Draw a forward slash (/)
float DrawSlash(float2 uv)
{
    float2 p = uv * 2.0 - 1.0;

    float w = 0.15;
    float2 a = float2(-0.3, 0.7);
    float2 b = float2(0.3, -0.7);
    return DrawSegment(p, a, b, w);
}

// Draw a dot (.)
float DrawDot(float2 uv)
{
    float2 p = uv * 2.0 - 1.0;

    float dotSize = 0.2;
    float dot = length(p - float2(0, 0.6)) - dotSize;
    return smoothstep(0.05, 0.0, dot);
}

// Draw a dash (-)
float DrawDash(float2 uv)
{
    float2 p = uv * 2.0 - 1.0;

    float w = 0.15;
    float2 a = float2(-0.4, 0.0);
    float2 b = float2(0.4, 0.0);
    return DrawSegment(p, a, b, w);
}

// Draw a letter (A-Z) using segment-based style
// letter: 0=A, 1=B, 2=C, ... 25=Z
float DrawLetter(float2 uv, int letter)
{
    float2 p = uv * 2.0 - 1.0;

    float w = 0.15;
    float result = 0.0;

    // Common segment positions
    float2 top1 = float2(-0.4, -0.8), top2 = float2(0.4, -0.8);
    float2 mid1 = float2(-0.4, 0.0), mid2 = float2(0.4, 0.0);
    float2 bot1 = float2(-0.4, 0.8), bot2 = float2(0.4, 0.8);
    float2 ltop1 = float2(-0.5, -0.7), ltop2 = float2(-0.5, -0.1);
    float2 lbot1 = float2(-0.5, 0.1), lbot2 = float2(-0.5, 0.7);
    float2 rtop1 = float2(0.5, -0.7), rtop2 = float2(0.5, -0.1);
    float2 rbot1 = float2(0.5, 0.1), rbot2 = float2(0.5, 0.7);

    // Diagonal segments
    float2 diagTL = float2(-0.4, -0.7);
    float2 diagTR = float2(0.4, -0.7);
    float2 diagBL = float2(-0.4, 0.7);
    float2 diagBR = float2(0.4, 0.7);
    float2 diagMid = float2(0.0, 0.0);

    if (letter == 0) // A
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, mid1, mid2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, rbot1, rbot2, w));
    }
    else if (letter == 1) // B
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, mid1, mid2, w));
        result = max(result, DrawSegment(p, bot1, bot2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, rbot1, rbot2, w));
    }
    else if (letter == 2) // C
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, bot1, bot2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
    }
    else if (letter == 3) // D
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, bot1, bot2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, rbot1, rbot2, w));
    }
    else if (letter == 4) // E
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, mid1, mid2, w));
        result = max(result, DrawSegment(p, bot1, bot2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
    }
    else if (letter == 5) // F
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, mid1, mid2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
    }
    else if (letter == 6) // G
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, bot1, bot2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, rbot1, rbot2, w));
        result = max(result, DrawSegment(p, mid2, float2(0.5, 0.0), w));
    }
    else if (letter == 7) // H
    {
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, rbot1, rbot2, w));
        result = max(result, DrawSegment(p, mid1, mid2, w));
    }
    else if (letter == 8) // I
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, bot1, bot2, w));
        result = max(result, DrawSegment(p, float2(0, -0.7), float2(0, 0.7), w));
    }
    else if (letter == 9) // J
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, rbot1, rbot2, w));
        result = max(result, DrawSegment(p, bot1, bot2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
    }
    else if (letter == 10) // K
    {
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, float2(-0.3, 0.0), diagTR, w));
        result = max(result, DrawSegment(p, float2(-0.3, 0.0), diagBR, w));
    }
    else if (letter == 11) // L
    {
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, bot1, bot2, w));
    }
    else if (letter == 12) // M
    {
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, rbot1, rbot2, w));
        result = max(result, DrawSegment(p, diagTL, float2(0, -0.2), w));
        result = max(result, DrawSegment(p, diagTR, float2(0, -0.2), w));
    }
    else if (letter == 13) // N
    {
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, rbot1, rbot2, w));
        result = max(result, DrawSegment(p, diagTL, diagBR, w));
    }
    else if (letter == 14) // O
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, bot1, bot2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, rbot1, rbot2, w));
    }
    else if (letter == 15) // P
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, mid1, mid2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
    }
    else if (letter == 16) // Q
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, bot1, bot2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, rbot1, rbot2, w));
        result = max(result, DrawSegment(p, float2(0.2, 0.4), float2(0.6, 0.9), w));
    }
    else if (letter == 17) // R
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, mid1, mid2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, float2(0.1, 0.1), diagBR, w));
    }
    else if (letter == 18) // S
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, mid1, mid2, w));
        result = max(result, DrawSegment(p, bot1, bot2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, rbot1, rbot2, w));
    }
    else if (letter == 19) // T
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, float2(0, -0.7), float2(0, 0.7), w));
    }
    else if (letter == 20) // U
    {
        result = max(result, DrawSegment(p, bot1, bot2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, rbot1, rbot2, w));
    }
    else if (letter == 21) // V
    {
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, lbot1, float2(0, 0.7), w));
        result = max(result, DrawSegment(p, rbot1, float2(0, 0.7), w));
    }
    else if (letter == 22) // W
    {
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, rbot1, rbot2, w));
        result = max(result, DrawSegment(p, diagBL, float2(0, 0.2), w));
        result = max(result, DrawSegment(p, diagBR, float2(0, 0.2), w));
    }
    else if (letter == 23) // X
    {
        result = max(result, DrawSegment(p, diagTL, diagBR, w));
        result = max(result, DrawSegment(p, diagTR, diagBL, w));
    }
    else if (letter == 24) // Y
    {
        result = max(result, DrawSegment(p, diagTL, diagMid, w));
        result = max(result, DrawSegment(p, diagTR, diagMid, w));
        result = max(result, DrawSegment(p, diagMid, float2(0, 0.7), w));
    }
    else if (letter == 25) // Z
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, bot1, bot2, w));
        result = max(result, DrawSegment(p, diagTR, diagBL, w));
    }

    return result;
}

// Draw a filled background rectangle with rounded corners
float DrawBackground(float2 uv)
{
    float2 p = uv * 2.0 - 1.0;

    float2 size = float2(0.95, 0.95);
    float radius = 0.1;
    float2 d = abs(p) - size + radius;
    float dist = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - radius;

    return 1.0 - smoothstep(-0.05, 0.05, dist);
}

// ========== Animation Functions ==========

float3 HsvToRgb(float3 hsv)
{
    float h = hsv.x * 6.0;
    float s = hsv.y;
    float v = hsv.z;

    float c = v * s;
    float x = c * (1.0 - abs(fmod(h, 2.0) - 1.0));
    float m = v - c;

    float3 rgb;
    if (h < 1.0) rgb = float3(c, x, 0);
    else if (h < 2.0) rgb = float3(x, c, 0);
    else if (h < 3.0) rgb = float3(0, c, x);
    else if (h < 4.0) rgb = float3(0, x, c);
    else if (h < 5.0) rgb = float3(x, 0, c);
    else rgb = float3(c, 0, x);

    return rgb + m;
}

// ========== Vertex Shader ==========

VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    TextEntity entity = Entities[instanceId];

    // Skip invisible entities
    if (entity.CharacterSize <= 0 || entity.Color.a <= 0)
    {
        VSOutput output;
        output.Position = float4(0, 0, -2, 1);
        output.Color = float4(0, 0, 0, 0);
        output.TexCoord = float2(0, 0);
        output.CharacterSize = 0;
        output.GlowIntensity = 0;
        output.EntityType = 0;
        output.AnimationType = 0;
        output.AnimationPhase = 0;
        output.AnimationSpeed = 0;
        output.AnimationIntensity = 0;
        return output;
    }

    // Quad vertices (two triangles)
    float2 quadVerts[6] = {
        float2(-1, -1), float2(1, -1), float2(-1, 1),
        float2(-1, 1), float2(1, -1), float2(1, 1)
    };

    float2 texCoords[6] = {
        float2(0, 0), float2(1, 0), float2(0, 1),
        float2(0, 1), float2(1, 0), float2(1, 1)
    };

    float2 vertex = quadVerts[vertexId];
    float2 texCoord = texCoords[vertexId];

    // Calculate size
    float2 quadSize;
    if (entity.EntityType >= 60.0 && entity.EntityType < 61.0)
    {
        // Background uses Size for half-size
        quadSize = entity.Size;
    }
    else
    {
        // Characters use CharacterSize
        quadSize = float2(entity.CharacterSize, entity.CharacterSize);
    }

    // Apply animation offsets
    float2 position = entity.Position;
    int animType = (int)entity.AnimationType;
    float animTime = Time * entity.AnimationSpeed + entity.AnimationPhase;

    if (animType == 2) // Wave
    {
        position.y += sin(animTime) * entity.AnimationIntensity;
    }
    else if (animType == 5) // Shake
    {
        position.x += sin(animTime * 17.3) * entity.AnimationIntensity;
        position.y += cos(animTime * 13.7) * entity.AnimationIntensity;
    }

    // Transform to screen space
    float2 worldPos = position + vertex * quadSize;
    float2 ndcPos = (worldPos / ViewportSize) * 2.0 - 1.0;
    ndcPos.y = -ndcPos.y;

    VSOutput output;
    output.Position = float4(ndcPos, 0.5, 1.0);
    output.Color = entity.Color;
    output.TexCoord = texCoord;
    output.CharacterSize = entity.CharacterSize;
    output.GlowIntensity = entity.GlowIntensity;
    output.EntityType = entity.EntityType;
    output.AnimationType = entity.AnimationType;
    output.AnimationPhase = entity.AnimationPhase;
    output.AnimationSpeed = entity.AnimationSpeed;
    output.AnimationIntensity = entity.AnimationIntensity;

    return output;
}

// ========== Pixel Shader ==========

float4 PSMain(VSOutput input) : SV_TARGET
{
    float entityType = input.EntityType;
    float2 uv = input.TexCoord;
    float4 baseColor = input.Color;
    int animType = (int)input.AnimationType;
    float animTime = Time * input.AnimationSpeed + input.AnimationPhase;

    // Apply color animations
    if (animType == 1) // Pulse
    {
        float pulse = 0.6 + 0.4 * sin(animTime);
        baseColor.a *= pulse;
    }
    else if (animType == 2) // Wave - also apply rainbow colors per character
    {
        float hue = frac(animTime * 0.3 + input.AnimationPhase * 2.0);
        float3 rainbowColor = HsvToRgb(float3(hue, 0.9, 1.0));
        baseColor.rgb = rainbowColor;
    }
    else if (animType == 3) // Rainbow
    {
        float hue = frac(animTime * 0.3 + input.AnimationPhase * 2.0);
        float3 rainbowColor = HsvToRgb(float3(hue, 0.9, 1.0));
        baseColor.rgb = rainbowColor;
    }
    else if (animType == 4) // Breathing (size handled in VS, color pulse here)
    {
        float breath = 0.9 + 0.1 * sin(animTime);
        baseColor.rgb *= breath;
    }

    float shape = 0.0;

    // Dispatch to appropriate drawing function
    if (entityType >= 0.0 && entityType < 10.0) // Digits 0-9
    {
        int digit = (int)entityType;
        shape = DrawDigit(uv, digit);
    }
    else if (entityType >= 10.0 && entityType < 11.0) // Colon
    {
        shape = DrawColon(uv);
    }
    else if (entityType >= 11.0 && entityType < 12.0) // Slash
    {
        shape = DrawSlash(uv);
    }
    else if (entityType >= 12.0 && entityType < 13.0) // Dot
    {
        shape = DrawDot(uv);
    }
    else if (entityType >= 13.0 && entityType < 14.0) // Dash
    {
        shape = DrawDash(uv);
    }
    else if (entityType >= 20.0 && entityType < 46.0) // Letters A-Z
    {
        int letter = (int)(entityType - 20.0);
        shape = DrawLetter(uv, letter);
    }
    else if (entityType >= 50.0 && entityType < 51.0) // Space (skip)
    {
        discard;
    }
    else if (entityType >= 60.0 && entityType < 61.0) // Background
    {
        shape = DrawBackground(uv);
    }

    if (shape <= 0.001)
        discard;

    // Apply glow
    float glow = input.GlowIntensity * GlowIntensity;
    float3 finalColor = baseColor.rgb * (1.0 + glow * 0.5);
    float finalAlpha = shape * baseColor.a;

    // HDR output
    finalColor *= HdrMultiplier;

    return float4(finalColor, finalAlpha);
}
