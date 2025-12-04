// Space Invaders shader with neon glow effects
// Entity types: 0=particle, 1=rocket, 2=invader small (squid), 3=invader medium (crab), 4=invader big (octopus)
// Entity types: 5-14=digits 0-9, 15=colon (:), 16-41=letters A-Z, 42=space, 43=slash (/), 50=background rect

cbuffer FrameData : register(b0)
{
    float2 ViewportSize;
    float Time;
    float GlowIntensity;
    float EnableTrails;
    float TrailLength;
    float NeonIntensity;
    float AnimSpeed;
    float Padding1;
    float Padding2;
    float Padding3;
    float Padding4;
    float Padding5;
    float Padding6;
    float Padding7;
    float Padding8;
};

struct EntityInstance
{
    float2 Position;
    float2 Velocity;
    float4 Color;
    float Size;
    float Life;
    float MaxLife;
    float EntityType;
};

StructuredBuffer<EntityInstance> Entities : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
    float2 TexCoord : TEXCOORD0;
    float LifeFactor : TEXCOORD1;
    float EntityType : TEXCOORD2;
    float AnimPhase : TEXCOORD3;
};

// Classic Space Invaders squid shape (small, 200 pts)
// Returns float4: rgb = color tint, a = alpha
float4 DrawSquidColored(float2 uv, float anim, float4 baseColor)
{
    float2 p = uv * 2.0 - 1.0;

    // Body (rounded rectangle)
    float2 bodySize = float2(0.6, 0.5);
    float2 d = abs(p) - bodySize;
    float body = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
    body = smoothstep(0.1, 0.0, body);

    // Head bump
    float head = length(p - float2(0, -0.3)) - 0.35;
    head = smoothstep(0.05, 0.0, head);

    // Eyes (two dots) - bright cyan/white
    float eyeL = length(p - float2(-0.25, -0.15)) - 0.12;
    float eyeR = length(p - float2(0.25, -0.15)) - 0.12;
    float eyes = min(eyeL, eyeR);
    eyes = smoothstep(0.02, 0.0, eyes);

    // Tentacles (bottom wavy lines) - animated, different color
    float tentacles = 0.0;
    for (int i = -2; i <= 2; i++)
    {
        float xOff = i * 0.25;
        float tentY = 0.5 + sin(anim + i * 1.5) * 0.1;
        float2 tentPos = float2(xOff, tentY);
        float tent = length(p - tentPos) - 0.08;
        tentacles = max(tentacles, smoothstep(0.03, 0.0, tent));
    }

    // Combine with different colors
    float bodyAlpha = saturate(body + head);
    float totalAlpha = saturate(bodyAlpha + tentacles + eyes);

    // Color mixing: body=base, tentacles=accent (shifted hue), eyes=bright
    float3 bodyCol = baseColor.rgb;
    float3 tentacleCol = baseColor.rgb * float3(0.5, 1.2, 1.5); // Cyan-ish tint
    float3 eyeCol = float3(1.0, 1.0, 0.8); // Bright yellow-white

    float3 finalCol = bodyCol * bodyAlpha + tentacleCol * tentacles + eyeCol * eyes;
    finalCol = finalCol / max(totalAlpha, 0.001);

    return float4(finalCol, totalAlpha);
}

// Legacy wrapper for compatibility
float DrawSquid(float2 uv, float anim)
{
    return DrawSquidColored(uv, anim, float4(1,1,1,1)).a;
}

// Classic Space Invaders crab shape (medium, 100 pts)
// Returns float4: rgb = color tint, a = alpha
float4 DrawCrabColored(float2 uv, float anim, float4 baseColor)
{
    float2 p = uv * 2.0 - 1.0;

    // Body (hexagonal-ish)
    float body = length(p * float2(1.0, 1.3)) - 0.5;
    body = smoothstep(0.08, 0.0, body);

    // Claws (animated) - orange/red accent
    float clawAnim = sin(anim) * 0.2;
    float2 clawL = float2(-0.65, -0.1 + clawAnim);
    float2 clawR = float2(0.65, -0.1 - clawAnim);
    float clawLDist = length(p - clawL) - 0.18;
    float clawRDist = length(p - clawR) - 0.18;
    float claws = min(clawLDist, clawRDist);
    claws = smoothstep(0.04, 0.0, claws);

    // Eyes - bright
    float eyeL = length(p - float2(-0.2, -0.15)) - 0.1;
    float eyeR = length(p - float2(0.2, -0.15)) - 0.1;
    float eyes = min(eyeL, eyeR);
    eyes = smoothstep(0.02, 0.0, eyes);

    // Legs (animated) - slightly different color
    float legs = 0.0;
    for (int i = -1; i <= 1; i++)
    {
        float xOff = i * 0.35;
        float legY = 0.45 + abs(sin(anim + i * 2.0)) * 0.1;
        float2 legPos = float2(xOff, legY);
        float leg = length(p - legPos) - 0.1;
        legs = max(legs, smoothstep(0.03, 0.0, leg));
    }

    // Combine with different colors
    float totalAlpha = saturate(body + claws + legs + eyes);

    // Color mixing: body=base, claws=orange accent, legs=lighter, eyes=bright
    float3 bodyCol = baseColor.rgb;
    float3 clawCol = baseColor.rgb * float3(1.5, 0.7, 0.3); // Orange-ish
    float3 legCol = baseColor.rgb * float3(0.8, 1.3, 0.8); // Lighter green tint
    float3 eyeCol = float3(1.0, 0.9, 0.5); // Yellow-white

    float3 finalCol = bodyCol * body + clawCol * claws + legCol * legs + eyeCol * eyes;
    finalCol = finalCol / max(totalAlpha, 0.001);

    return float4(finalCol, totalAlpha);
}

// Legacy wrapper
float DrawCrab(float2 uv, float anim)
{
    return DrawCrabColored(uv, anim, float4(1,1,1,1)).a;
}

// Classic Space Invaders octopus shape (big, 50 pts)
// Returns float4: rgb = color tint, a = alpha
float4 DrawOctopusColored(float2 uv, float anim, float4 baseColor)
{
    float2 p = uv * 2.0 - 1.0;

    // Large rounded body
    float body = length(p * float2(1.0, 0.9)) - 0.55;
    body = smoothstep(0.1, 0.0, body);

    // Dome top - slightly brighter
    float dome = length(p - float2(0, -0.3)) - 0.4;
    dome = smoothstep(0.08, 0.0, dome);

    // Eyes (larger, menacing) - red/orange glow
    float eyeL = length(p - float2(-0.22, -0.1)) - 0.12;
    float eyeR = length(p - float2(0.22, -0.1)) - 0.12;
    float eyes = min(eyeL, eyeR);
    eyes = smoothstep(0.02, 0.0, eyes);

    // Tentacles (multiple, wavy) - gradient colors based on position
    float tentacles = 0.0;
    float tentacleColorMix = 0.0;
    for (int i = -3; i <= 3; i++)
    {
        float xOff = i * 0.2;
        float phase = anim + i * 0.8;
        float tentY = 0.5 + sin(phase) * 0.15;
        float tentX = xOff + cos(phase) * 0.05;
        float2 tentPos = float2(tentX, tentY);
        float tent = length(p - tentPos) - 0.07;
        float tentAlpha = smoothstep(0.025, 0.0, tent);
        tentacles = max(tentacles, tentAlpha);
        // Mix color based on tentacle index for variety
        tentacleColorMix = max(tentacleColorMix, tentAlpha * (float(i + 3) / 6.0));
    }

    // Combine with different colors
    float bodyAlpha = saturate(body + dome);
    float totalAlpha = saturate(bodyAlpha + tentacles + eyes);

    // Color mixing: body=base, dome=lighter, tentacles=purple/pink gradient, eyes=red
    float3 bodyCol = baseColor.rgb;
    float3 domeCol = baseColor.rgb * 1.3; // Brighter
    float3 tentacleCol1 = baseColor.rgb * float3(1.3, 0.6, 1.4); // Purple tint
    float3 tentacleCol2 = baseColor.rgb * float3(0.6, 1.2, 1.3); // Teal tint
    float3 tentacleCol = lerp(tentacleCol1, tentacleCol2, tentacleColorMix);
    float3 eyeCol = float3(1.0, 0.3, 0.2); // Red-orange glow

    float3 finalCol = bodyCol * body + domeCol * dome + tentacleCol * tentacles + eyeCol * eyes;
    finalCol = finalCol / max(totalAlpha, 0.001);

    return float4(finalCol, totalAlpha);
}

// Legacy wrapper
float DrawOctopus(float2 uv, float anim)
{
    return DrawOctopusColored(uv, anim, float4(1,1,1,1)).a;
}

// Rocket shape (elongated with pointed tip)
float DrawRocket(float2 uv, float2 velocity)
{
    float2 p = uv * 2.0 - 1.0;

    // Stretch along velocity direction
    float speed = length(velocity);
    float stretch = 1.0 + min(speed * 0.002, 1.5);

    // Rotate to face velocity
    float2 dir = speed > 0.01 ? normalize(velocity) : float2(0, -1);
    float2x2 rot = float2x2(dir.y, -dir.x, dir.x, dir.y);
    p = mul(rot, p);

    // Rocket body (elongated ellipse)
    float2 bodyScale = float2(0.3, 0.7 * stretch);
    float body = length(p / bodyScale) - 1.0;
    body = smoothstep(0.1, 0.0, body);

    // Pointed tip
    float tip = length(p - float2(0, -0.7 * stretch)) - 0.15;
    tip = smoothstep(0.05, 0.0, tip);

    // Flame trail (at back)
    float flame = 0.0;
    if (p.y > 0.3 * stretch)
    {
        float flameY = (p.y - 0.3 * stretch) / (0.5 * stretch);
        float flameWidth = 0.2 * (1.0 - flameY);
        float flameDist = abs(p.x) - flameWidth;
        flame = smoothstep(0.05, 0.0, flameDist) * (1.0 - flameY);
    }

    return saturate(body + tip + flame * 0.7);
}

// Explosion particle (circular with soft falloff)
float DrawParticle(float2 uv, float lifeFactor)
{
    float2 center = uv - 0.5;
    float dist = length(center) * 2.0;

    float alpha = 1.0 - smoothstep(0.3, 1.0, dist);
    return alpha * lifeFactor;
}

// 7-segment digit display helper
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
    float2 p = uv * 2.0 - 1.0; // -1 to 1
    p.y = -p.y; // Flip Y for correct orientation

    float w = 0.15; // segment width
    float result = 0.0;

    // Segment positions (7-segment display layout)
    // Top horizontal (a)
    float2 a1 = float2(-0.4, -0.8), a2 = float2(0.4, -0.8);
    // Upper right vertical (b)
    float2 b1 = float2(0.5, -0.7), b2 = float2(0.5, -0.1);
    // Lower right vertical (c)
    float2 c1 = float2(0.5, 0.1), c2 = float2(0.5, 0.7);
    // Bottom horizontal (d)
    float2 d1 = float2(-0.4, 0.8), d2 = float2(0.4, 0.8);
    // Lower left vertical (e)
    float2 e1 = float2(-0.5, 0.1), e2 = float2(-0.5, 0.7);
    // Upper left vertical (f)
    float2 f1 = float2(-0.5, -0.7), f2 = float2(-0.5, -0.1);
    // Middle horizontal (g)
    float2 g1 = float2(-0.4, 0.0), g2 = float2(0.4, 0.0);

    // Segments for each digit: a,b,c,d,e,f,g
    // 0: a,b,c,d,e,f
    // 1: b,c
    // 2: a,b,g,e,d
    // 3: a,b,g,c,d
    // 4: f,g,b,c
    // 5: a,f,g,c,d
    // 6: a,f,e,d,c,g
    // 7: a,b,c
    // 8: all
    // 9: a,b,c,d,f,g

    if (digit == 0) {
        result = max(result, DrawSegment(p, a1, a2, w)); // a
        result = max(result, DrawSegment(p, b1, b2, w)); // b
        result = max(result, DrawSegment(p, c1, c2, w)); // c
        result = max(result, DrawSegment(p, d1, d2, w)); // d
        result = max(result, DrawSegment(p, e1, e2, w)); // e
        result = max(result, DrawSegment(p, f1, f2, w)); // f
    }
    else if (digit == 1) {
        result = max(result, DrawSegment(p, b1, b2, w)); // b
        result = max(result, DrawSegment(p, c1, c2, w)); // c
    }
    else if (digit == 2) {
        result = max(result, DrawSegment(p, a1, a2, w)); // a
        result = max(result, DrawSegment(p, b1, b2, w)); // b
        result = max(result, DrawSegment(p, g1, g2, w)); // g
        result = max(result, DrawSegment(p, e1, e2, w)); // e
        result = max(result, DrawSegment(p, d1, d2, w)); // d
    }
    else if (digit == 3) {
        result = max(result, DrawSegment(p, a1, a2, w)); // a
        result = max(result, DrawSegment(p, b1, b2, w)); // b
        result = max(result, DrawSegment(p, g1, g2, w)); // g
        result = max(result, DrawSegment(p, c1, c2, w)); // c
        result = max(result, DrawSegment(p, d1, d2, w)); // d
    }
    else if (digit == 4) {
        result = max(result, DrawSegment(p, f1, f2, w)); // f
        result = max(result, DrawSegment(p, g1, g2, w)); // g
        result = max(result, DrawSegment(p, b1, b2, w)); // b
        result = max(result, DrawSegment(p, c1, c2, w)); // c
    }
    else if (digit == 5) {
        result = max(result, DrawSegment(p, a1, a2, w)); // a
        result = max(result, DrawSegment(p, f1, f2, w)); // f
        result = max(result, DrawSegment(p, g1, g2, w)); // g
        result = max(result, DrawSegment(p, c1, c2, w)); // c
        result = max(result, DrawSegment(p, d1, d2, w)); // d
    }
    else if (digit == 6) {
        result = max(result, DrawSegment(p, a1, a2, w)); // a
        result = max(result, DrawSegment(p, f1, f2, w)); // f
        result = max(result, DrawSegment(p, e1, e2, w)); // e
        result = max(result, DrawSegment(p, d1, d2, w)); // d
        result = max(result, DrawSegment(p, c1, c2, w)); // c
        result = max(result, DrawSegment(p, g1, g2, w)); // g
    }
    else if (digit == 7) {
        result = max(result, DrawSegment(p, a1, a2, w)); // a
        result = max(result, DrawSegment(p, b1, b2, w)); // b
        result = max(result, DrawSegment(p, c1, c2, w)); // c
    }
    else if (digit == 8) {
        result = max(result, DrawSegment(p, a1, a2, w)); // a
        result = max(result, DrawSegment(p, b1, b2, w)); // b
        result = max(result, DrawSegment(p, c1, c2, w)); // c
        result = max(result, DrawSegment(p, d1, d2, w)); // d
        result = max(result, DrawSegment(p, e1, e2, w)); // e
        result = max(result, DrawSegment(p, f1, f2, w)); // f
        result = max(result, DrawSegment(p, g1, g2, w)); // g
    }
    else if (digit == 9) {
        result = max(result, DrawSegment(p, a1, a2, w)); // a
        result = max(result, DrawSegment(p, b1, b2, w)); // b
        result = max(result, DrawSegment(p, c1, c2, w)); // c
        result = max(result, DrawSegment(p, d1, d2, w)); // d
        result = max(result, DrawSegment(p, f1, f2, w)); // f
        result = max(result, DrawSegment(p, g1, g2, w)); // g
    }

    return result;
}

// Draw a colon (:) for timer display
float DrawColon(float2 uv)
{
    float2 p = uv * 2.0 - 1.0;
    p.y = -p.y;

    float result = 0.0;
    float dotSize = 0.2;

    // Upper dot
    float upperDot = length(p - float2(0, -0.4)) - dotSize;
    result = max(result, smoothstep(0.05, 0.0, upperDot));

    // Lower dot
    float lowerDot = length(p - float2(0, 0.4)) - dotSize;
    result = max(result, smoothstep(0.05, 0.0, lowerDot));

    return result;
}

// Draw a letter (A-Z) using segment-based style
// letter: 0=A, 1=B, 2=C, ... 25=Z
float DrawLetter(float2 uv, int letter)
{
    float2 p = uv * 2.0 - 1.0;
    p.y = -p.y;

    float w = 0.15; // segment width
    float result = 0.0;

    // Common segment positions
    float2 top1 = float2(-0.4, -0.8), top2 = float2(0.4, -0.8);     // top horizontal
    float2 mid1 = float2(-0.4, 0.0), mid2 = float2(0.4, 0.0);       // middle horizontal
    float2 bot1 = float2(-0.4, 0.8), bot2 = float2(0.4, 0.8);       // bottom horizontal
    float2 ltop1 = float2(-0.5, -0.7), ltop2 = float2(-0.5, -0.1);  // left top vertical
    float2 lbot1 = float2(-0.5, 0.1), lbot2 = float2(-0.5, 0.7);    // left bottom vertical
    float2 rtop1 = float2(0.5, -0.7), rtop2 = float2(0.5, -0.1);    // right top vertical
    float2 rbot1 = float2(0.5, 0.1), rbot2 = float2(0.5, 0.7);      // right bottom vertical

    // Diagonal segments for letters like N, M, W
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
    else if (letter == 8) // I
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, bot1, bot2, w));
        result = max(result, DrawSegment(p, float2(0, -0.7), float2(0, 0.7), w));
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
    else if (letter == 17) // R
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, mid1, mid2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, float2(0.1, 0.1), diagBR, w)); // diagonal leg
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
    else if (letter == 22) // W
    {
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, rbot1, rbot2, w));
        result = max(result, DrawSegment(p, diagBL, float2(0, 0.2), w));
        result = max(result, DrawSegment(p, diagBR, float2(0, 0.2), w));
    }
    else if (letter == 24) // Y
    {
        result = max(result, DrawSegment(p, diagTL, diagMid, w));
        result = max(result, DrawSegment(p, diagTR, diagMid, w));
        result = max(result, DrawSegment(p, diagMid, float2(0, 0.7), w));
    }
    // Space (letter 26) returns 0 - nothing drawn

    return result;
}

// Draw a forward slash (/)
float DrawSlash(float2 uv)
{
    float2 p = uv * 2.0 - 1.0;
    p.y = -p.y;

    float w = 0.15; // segment width
    // Diagonal line from bottom-left to top-right
    float2 a = float2(-0.3, 0.7);
    float2 b = float2(0.3, -0.7);
    return DrawSegment(p, a, b, w);
}

// Draw a filled background rectangle with rounded corners
float DrawBackground(float2 uv)
{
    float2 p = uv * 2.0 - 1.0; // -1 to 1

    // Rounded rectangle SDF
    float2 size = float2(0.95, 0.95);
    float radius = 0.1;
    float2 d = abs(p) - size + radius;
    float dist = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - radius;

    // Soft edge
    return 1.0 - smoothstep(-0.05, 0.05, dist);
}

VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    EntityInstance entity = Entities[instanceId];

    // Skip dead entities
    if (entity.Life <= 0 || entity.Size <= 0)
    {
        VSOutput output;
        output.Position = float4(0, 0, -2, 1);
        output.Color = float4(0, 0, 0, 0);
        output.TexCoord = float2(0, 0);
        output.LifeFactor = 0;
        output.EntityType = 0;
        output.AnimPhase = 0;
        return output;
    }

    // Generate quad vertices
    float2 offsets[6] = {
        float2(-1, -1), float2(1, -1), float2(-1, 1),
        float2(-1, 1), float2(1, -1), float2(1, 1)
    };
    float2 texCoords[6] = {
        float2(0, 1), float2(1, 1), float2(0, 0),
        float2(0, 0), float2(1, 1), float2(1, 0)
    };

    float2 offset = offsets[vertexId];
    float2 texCoord = texCoords[vertexId];

    float lifeFactor = entity.Life / max(entity.MaxLife, 0.001);
    float size = entity.Size;

    // Particles shrink as they die
    if (entity.EntityType < 0.5) // Particle
    {
        size *= (0.3 + 0.7 * lifeFactor);
    }

    // Background uses velocity.xy for width/height
    float2 quadSize = float2(size, size);
    if (entity.EntityType >= 50.0 && entity.EntityType < 51.0)
    {
        quadSize = entity.Velocity; // velocity.x = width, velocity.y = height
    }

    // Convert to screen position
    float2 screenPos = entity.Position + offset * quadSize;
    float2 ndcPos = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndcPos.y = -ndcPos.y;

    VSOutput output;
    output.Position = float4(ndcPos, 0, 1);
    output.Color = entity.Color;
    output.TexCoord = texCoord;
    output.LifeFactor = lifeFactor;
    output.EntityType = entity.EntityType;
    output.AnimPhase = entity.Velocity.x; // Animation phase passed in velocity.x for invaders
    return output;
}

float4 PSMain(VSOutput input) : SV_TARGET
{
    float entityType = input.EntityType;
    float2 uv = input.TexCoord;
    float lifeFactor = input.LifeFactor;
    float4 baseColor = input.Color;

    float shape = 0.0;
    float4 coloredResult = float4(0, 0, 0, 0);
    bool useColoredResult = false;

    if (entityType < 0.5) // Particle
    {
        shape = DrawParticle(uv, lifeFactor);
    }
    else if (entityType < 1.5) // Rocket
    {
        shape = DrawRocket(uv, float2(0, -1));
    }
    else if (entityType < 2.5) // Small invader (squid) - multicolored
    {
        coloredResult = DrawSquidColored(uv, input.AnimPhase, baseColor);
        shape = coloredResult.a;
        useColoredResult = true;
    }
    else if (entityType < 3.5) // Medium invader (crab) - multicolored
    {
        coloredResult = DrawCrabColored(uv, input.AnimPhase, baseColor);
        shape = coloredResult.a;
        useColoredResult = true;
    }
    else if (entityType < 4.5) // Big invader (octopus) - multicolored
    {
        coloredResult = DrawOctopusColored(uv, input.AnimPhase, baseColor);
        shape = coloredResult.a;
        useColoredResult = true;
    }
    else if (entityType >= 5.0 && entityType < 15.0) // Digits 0-9
    {
        int digit = (int)(entityType - 5.0);
        shape = DrawDigit(uv, digit);
    }
    else if (entityType >= 15.0 && entityType < 16.0) // Colon
    {
        shape = DrawColon(uv);
    }
    else if (entityType >= 16.0 && entityType < 42.0) // Letters A-Z
    {
        int letter = (int)(entityType - 16.0);
        shape = DrawLetter(uv, letter);
    }
    else if (entityType >= 43.0 && entityType < 44.0) // Forward slash
    {
        shape = DrawSlash(uv);
    }
    else if (entityType >= 50.0 && entityType < 51.0) // Background rectangle
    {
        shape = DrawBackground(uv);
        // For background, return early with simple alpha blend (no glow effects)
        float4 bgColor = baseColor;
        bgColor.a = shape * baseColor.a;
        if (bgColor.a < 0.01)
            discard;
        return bgColor;
    }
    // entityType 42 = space (nothing drawn, shape stays 0)

    if (shape < 0.01)
        discard;

    // For multicolored invaders, use the colored result
    if (useColoredResult)
    {
        baseColor.rgb = coloredResult.rgb;
    }

    // Neon glow effect
    float2 center = uv - 0.5;
    float dist = length(center) * 2.0;

    // Core glow
    float coreGlow = exp(-dist * dist * 2.0) * GlowIntensity;

    // Edge glow (neon outline effect)
    float edgeGlow = 0.0;
    if (entityType >= 1.5) // Invaders get edge glow
    {
        float edgeDist = abs(shape - 0.5);
        edgeGlow = exp(-edgeDist * 20.0) * NeonIntensity * 0.5;
    }

    // Combine
    float4 color = baseColor;

    // Brighten core for hot center
    float coreBrightness = 1.0 + (1.0 - dist) * 0.3 * shape;
    color.rgb *= coreBrightness;

    // Add white hot center for particles and rockets
    if (entityType < 1.5)
    {
        float coreWhite = (1.0 - smoothstep(0.0, 0.4, dist)) * 0.4 * lifeFactor;
        color.rgb += float3(coreWhite, coreWhite, coreWhite);
    }

    // Neon scanline effect for invaders
    if (entityType >= 1.5)
    {
        float scanline = sin(uv.y * 40.0 + Time * 5.0) * 0.1 + 0.9;
        color.rgb *= scanline;

        // Outer glow
        color.rgb += baseColor.rgb * edgeGlow;
    }

    // Final alpha
    float finalAlpha = (shape + coreGlow * 0.5) * lifeFactor;
    color.a = saturate(finalAlpha);

    if (color.a < 0.01)
        discard;

    return color;
}
