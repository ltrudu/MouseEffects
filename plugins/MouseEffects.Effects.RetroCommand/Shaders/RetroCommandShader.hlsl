// Retro Missile Command shader with Modern (neon glow) and Retro (arcade) rendering modes
// Entity types: 0=particle, 1=explosion, 2=enemy missile, 3=counter missile, 4=city intact, 5=city destroyed
// Entity types: 6=missile base, 7=crosshair, 8=cooldown bar, 12-21=digits 0-9, 22=colon, 23-48=letters A-Z, 49=space, 50=background rect

cbuffer FrameData : register(b0)
{
    float2 ViewportSize;
    float Time;
    float RenderStyle;         // 0=Modern, 1=Retro
    float GlowIntensity;
    float NeonIntensity;
    float AnimSpeed;
    float HdrMultiplier;
    float ExplosionMaxRadius;
    float CityZoneY;           // Y position of city row
    float RetroScanlines;
    float RetroPixelScale;
    float Padding1;
    float Padding2;
    float Padding3;
    float Padding4;
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
    float2 StartPos : TEXCOORD3;
    float2 VelocityDir : TEXCOORD4;
};

// ========== HELPER FUNCTIONS ==========

// Quantize UV to pixel grid for blocky retro look
float2 QuantizeUV(float2 uv, float pixelScale)
{
    return floor(uv * pixelScale) / pixelScale;
}

// Retro color quantization (limited palette like classic arcade)
float3 QuantizeColor(float3 color)
{
    // Quantize to 4 levels per channel (retro arcade style)
    return floor(color * 4.0) / 4.0;
}

// Segment drawing for 7-segment display
float DrawSegment(float2 p, float2 a, float2 b, float width)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    float d = length(pa - ba * h);
    return smoothstep(width, width * 0.3, d);
}

// ========== MODERN DRAWING FUNCTIONS ==========

// Modern particle - soft glowing circle
float DrawParticleModern(float2 uv, float lifeFactor)
{
    float2 center = uv - 0.5;
    float dist = length(center) * 2.0;
    float alpha = 1.0 - smoothstep(0.3, 1.0, dist);
    return alpha * lifeFactor;
}

// Modern explosion - filled circle with bright core
float DrawExplosionModern(float2 uv, float lifeFactor)
{
    float2 center = uv - 0.5;
    float dist = length(center) * 2.0;

    // Filled circle - alpha falls off from center to edge
    float alpha = 1.0 - smoothstep(0.0, 1.0, dist);

    return alpha;
}

// Modern enemy missile - pointed warhead with glowing trail
float DrawEnemyMissileModern(float2 uv, float2 startPos, float2 currentPos, float2 velocity)
{
    float2 p = uv * 2.0 - 1.0;

    // Warhead (pointed tip)
    float2 dir = length(velocity) > 0.01 ? normalize(velocity) : float2(0, 1);
    float2x2 rot = float2x2(dir.y, -dir.x, dir.x, dir.y);
    float2 rotP = mul(rot, p);

    // Warhead body (triangle-ish)
    float warhead = smoothstep(0.3, 0.0, abs(rotP.x) - rotP.y * 0.3);
    warhead = max(warhead, smoothstep(0.1, 0.0, length(rotP - float2(0, -0.4)) - 0.15));

    return warhead;
}

// Modern counter missile - smaller missile with bright trail
float DrawCounterMissileModern(float2 uv, float2 velocity)
{
    float2 p = uv * 2.0 - 1.0;

    // Rocket body
    float2 dir = length(velocity) > 0.01 ? normalize(velocity) : float2(0, -1);
    float2x2 rot = float2x2(dir.y, -dir.x, dir.x, dir.y);
    float2 rotP = mul(rot, p);

    // Small rocket shape
    float body = smoothstep(0.15, 0.0, abs(rotP.x)) * smoothstep(0.5, 0.0, abs(rotP.y));
    float tip = smoothstep(0.08, 0.0, length(rotP - float2(0, -0.5)) - 0.1);

    return saturate(body + tip);
}

// Modern city - 3-4 buildings of varying heights with lit windows
float DrawCityModern(float2 uv)
{
    float2 p = uv * 2.0 - 1.0;
    float result = 0.0;

    // Building 1 (left, tall)
    float b1 = step(abs(p.x + 0.5), 0.25) * step(abs(p.y + 0.1), 0.6);

    // Building 2 (center-left, medium)
    float b2 = step(abs(p.x + 0.1), 0.2) * step(abs(p.y + 0.3), 0.4);

    // Building 3 (center-right, tall)
    float b3 = step(abs(p.x - 0.3), 0.22) * step(abs(p.y - 0.0), 0.7);

    // Building 4 (right, short)
    float b4 = step(abs(p.x - 0.65), 0.15) * step(abs(p.y + 0.5), 0.2);

    result = max(max(max(b1, b2), b3), b4);

    // Windows (grid pattern)
    float2 windowGrid = frac(p * float2(8, 12));
    float windows = step(0.3, windowGrid.x) * step(windowGrid.x, 0.7) * step(0.3, windowGrid.y) * step(windowGrid.y, 0.7);

    return result * (1.0 - windows * 0.3);
}

// Modern city destroyed - rubble pile with smoke
float DrawCityDestroyedModern(float2 uv)
{
    float2 p = uv * 2.0 - 1.0;

    // Rubble pile (irregular mounds)
    float rubble = 0.0;
    rubble = max(rubble, smoothstep(0.3, 0.0, abs(p.x + 0.4) - 0.2 + abs(p.y - 0.5)));
    rubble = max(rubble, smoothstep(0.25, 0.0, abs(p.x) - 0.25 + abs(p.y - 0.4)));
    rubble = max(rubble, smoothstep(0.2, 0.0, abs(p.x - 0.3) - 0.18 + abs(p.y - 0.6)));

    return rubble * step(p.y, 0.7);
}

// Modern missile base - trapezoid bunker with antenna
float DrawMissileBaseModern(float2 uv)
{
    float2 p = uv * 2.0 - 1.0;

    // Trapezoid bunker
    float bunker = step(abs(p.x) - abs(p.y) * 0.3, 0.6) * step(abs(p.y), 0.6);

    // Antenna on top
    float antenna = step(abs(p.x), 0.05) * step(p.y, -0.5) * step(-0.9, p.y);

    return saturate(bunker + antenna);
}

// Modern crosshair - cross pattern with circle, animated pulse
float DrawCrosshairModern(float2 uv, float time)
{
    float2 p = uv * 2.0 - 1.0;

    // Cross arms
    float cross = min(step(abs(p.x), 0.08), step(abs(p.y), 0.08));
    cross = max(cross, step(length(p), 0.08));

    // Outer circle
    float circle = abs(length(p) - 0.7);
    circle = smoothstep(0.06, 0.02, circle);

    // Pulse animation
    float pulse = 0.8 + 0.2 * sin(time * 4.0);

    return saturate(cross + circle) * pulse;
}

// Modern cooldown bar - circular arc progress indicator
float DrawCooldownBarModern(float2 uv, float progress)
{
    float2 p = uv * 2.0 - 1.0;

    float radius = length(p);
    float angle = atan2(p.y, p.x);

    // Circular arc (top starts at -PI/2)
    float arcProgress = (angle + 3.14159 * 0.5) / (3.14159 * 2.0);
    if (arcProgress < 0.0) arcProgress += 1.0;

    float ring = abs(radius - 0.6);
    float arc = step(arcProgress, progress) * smoothstep(0.15, 0.05, ring);

    return arc;
}

// ========== RETRO DRAWING FUNCTIONS ==========

// Retro particle - big 8-bit square pixel
float DrawParticleRetro(float2 uv, float lifeFactor)
{
    float2 p = uv * 2.0 - 1.0;
    float alpha = step(abs(p.x), 0.75) * step(abs(p.y), 0.75) * lifeFactor;
    return alpha;
}

// Retro explosion - blocky filled circle
float DrawExplosionRetro(float2 uv, float lifeFactor)
{
    uv = QuantizeUV(uv, 8.0);
    float2 p = uv * 2.0 - 1.0;

    // Filled circle
    float dist = length(p);
    return step(dist, 1.0);
}

// Retro enemy missile - blocky warhead
float DrawEnemyMissileRetro(float2 uv)
{
    uv = QuantizeUV(uv, 6.0);
    float2 p = uv * 2.0 - 1.0;

    // Simple blocky triangle
    float warhead = step(abs(p.x), 0.4 - p.y * 0.5) * step(p.y, 0.5);

    return warhead;
}

// Retro counter missile - blocky rocket
float DrawCounterMissileRetro(float2 uv)
{
    uv = QuantizeUV(uv, 6.0);
    float2 p = uv * 2.0 - 1.0;

    // Simple blocky rectangle with tip
    float body = step(abs(p.x), 0.3) * step(abs(p.y), 0.6);
    float tip = step(abs(p.x), 0.15) * step(p.y, -0.5) * step(-0.8, p.y);

    return saturate(body + tip);
}

// Retro city - blocky buildings
float DrawCityRetro(float2 uv)
{
    uv = QuantizeUV(uv, 8.0);
    float2 p = uv * 2.0 - 1.0;

    // Blocky buildings
    float b1 = step(abs(p.x + 0.5), 0.25) * step(abs(p.y + 0.1), 0.6);
    float b2 = step(abs(p.x + 0.1), 0.2) * step(abs(p.y + 0.3), 0.4);
    float b3 = step(abs(p.x - 0.3), 0.22) * step(abs(p.y - 0.0), 0.7);
    float b4 = step(abs(p.x - 0.65), 0.15) * step(abs(p.y + 0.5), 0.2);

    return max(max(max(b1, b2), b3), b4);
}

// Retro city destroyed - blocky rubble
float DrawCityDestroyedRetro(float2 uv)
{
    uv = QuantizeUV(uv, 8.0);
    float2 p = uv * 2.0 - 1.0;

    // Blocky rubble piles
    float r1 = step(abs(p.x + 0.4), 0.3) * step(abs(p.y - 0.5), 0.2);
    float r2 = step(abs(p.x), 0.25) * step(abs(p.y - 0.4), 0.3);
    float r3 = step(abs(p.x - 0.3), 0.2) * step(abs(p.y - 0.6), 0.15);

    return max(max(r1, r2), r3) * step(p.y, 0.7);
}

// Retro missile base - blocky trapezoid
float DrawMissileBaseRetro(float2 uv)
{
    uv = QuantizeUV(uv, 8.0);
    float2 p = uv * 2.0 - 1.0;

    // Blocky bunker
    float bunker = step(abs(p.x) - abs(p.y) * 0.3, 0.6) * step(abs(p.y), 0.6);

    // Antenna
    float antenna = step(abs(p.x), 0.1) * step(p.y, -0.5) * step(-0.9, p.y);

    return saturate(bunker + antenna);
}

// Retro crosshair - blocky cross
float DrawCrosshairRetro(float2 uv)
{
    uv = QuantizeUV(uv, 8.0);
    float2 p = uv * 2.0 - 1.0;

    // Blocky cross
    float cross = step(abs(p.x), 0.15) * step(abs(p.y), 0.6);
    cross = max(cross, step(abs(p.y), 0.15) * step(abs(p.x), 0.6));

    return cross;
}

// Retro cooldown bar - blocky arc
float DrawCooldownBarRetro(float2 uv, float progress)
{
    uv = QuantizeUV(uv, 12.0);
    float2 p = uv * 2.0 - 1.0;

    float radius = length(p);
    float angle = atan2(p.y, p.x);

    float arcProgress = (angle + 3.14159 * 0.5) / (3.14159 * 2.0);
    if (arcProgress < 0.0) arcProgress += 1.0;

    float ring = abs(radius - 0.6);
    return step(arcProgress, progress) * step(ring, 0.2);
}

// ========== TEXT RENDERING ==========

// 7-segment digit display (0-9)
float DrawDigit(float2 uv, int digit)
{
    float2 p = uv * 2.0 - 1.0;
    p.y = -p.y;

    float w = 0.15;
    float result = 0.0;

    // Segment positions
    float2 a1 = float2(-0.4, -0.8), a2 = float2(0.4, -0.8);
    float2 b1 = float2(0.5, -0.7), b2 = float2(0.5, -0.1);
    float2 c1 = float2(0.5, 0.1), c2 = float2(0.5, 0.7);
    float2 d1 = float2(-0.4, 0.8), d2 = float2(0.4, 0.8);
    float2 e1 = float2(-0.5, 0.1), e2 = float2(-0.5, 0.7);
    float2 f1 = float2(-0.5, -0.7), f2 = float2(-0.5, -0.1);
    float2 g1 = float2(-0.4, 0.0), g2 = float2(0.4, 0.0);

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

// Draw colon (:)
float DrawColon(float2 uv)
{
    float2 p = uv * 2.0 - 1.0;
    p.y = -p.y;

    float result = 0.0;
    float dotSize = 0.2;

    float upperDot = length(p - float2(0, -0.4)) - dotSize;
    result = max(result, smoothstep(0.05, 0.0, upperDot));

    float lowerDot = length(p - float2(0, 0.4)) - dotSize;
    result = max(result, smoothstep(0.05, 0.0, lowerDot));

    return result;
}

// Draw letters A-Z using segment-based style
float DrawLetter(float2 uv, int letter)
{
    float2 p = uv * 2.0 - 1.0;
    p.y = -p.y;

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
    else if (letter == 11) // L
    {
        result = max(result, DrawSegment(p, bot1, bot2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
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
    else if (letter == 24) // Y
    {
        result = max(result, DrawSegment(p, diagTL, diagMid, w));
        result = max(result, DrawSegment(p, diagTR, diagMid, w));
        result = max(result, DrawSegment(p, diagMid, float2(0, 0.7), w));
    }

    return result;
}

// Draw filled background rectangle
float DrawBackground(float2 uv)
{
    float2 p = uv * 2.0 - 1.0;
    float2 size = float2(0.95, 0.95);
    float radius = 0.1;
    float2 d = abs(p) - size + radius;
    float dist = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - radius;
    return 1.0 - smoothstep(-0.05, 0.05, dist);
}

// ========== VERTEX SHADER ==========

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
        output.StartPos = float2(0, 0);
        output.VelocityDir = float2(0, 0);
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
    if (entity.EntityType < 0.5)
    {
        size *= (0.3 + 0.7 * lifeFactor);
    }

    // Explosions: size is already set to CurrentRadius from C#, don't override

    // Background uses velocity.xy for width/height
    float2 quadSize = float2(size, size);
    if (entity.EntityType >= 50.0 && entity.EntityType < 51.0)
    {
        quadSize = entity.Velocity;
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
    output.StartPos = entity.Velocity; // For trails, stores start position
    output.VelocityDir = normalize(entity.Velocity);
    return output;
}

// ========== PIXEL SHADER ==========

float4 PSMain(VSOutput input) : SV_TARGET
{
    float entityType = input.EntityType;
    float2 uv = input.TexCoord;
    float lifeFactor = input.LifeFactor;
    float4 baseColor = input.Color;
    bool isRetro = RenderStyle > 0.5;

    float shape = 0.0;

    if (entityType < 0.5) // Particle
    {
        shape = isRetro ? DrawParticleRetro(uv, lifeFactor) : DrawParticleModern(uv, lifeFactor);
    }
    else if (entityType < 1.5) // Explosion
    {
        shape = isRetro ? DrawExplosionRetro(uv, lifeFactor) : DrawExplosionModern(uv, lifeFactor);
    }
    else if (entityType < 2.5) // Enemy missile
    {
        shape = isRetro ? DrawEnemyMissileRetro(uv) : DrawEnemyMissileModern(uv, input.StartPos, input.Position.xy, input.VelocityDir);
    }
    else if (entityType < 3.5) // Counter missile
    {
        shape = isRetro ? DrawCounterMissileRetro(uv) : DrawCounterMissileModern(uv, input.VelocityDir);
    }
    else if (entityType < 4.5) // City intact
    {
        shape = isRetro ? DrawCityRetro(uv) : DrawCityModern(uv);
    }
    else if (entityType < 5.5) // City destroyed
    {
        shape = isRetro ? DrawCityDestroyedRetro(uv) : DrawCityDestroyedModern(uv);
    }
    else if (entityType < 6.5) // Missile base
    {
        shape = isRetro ? DrawMissileBaseRetro(uv) : DrawMissileBaseModern(uv);
    }
    else if (entityType < 7.5) // Crosshair
    {
        shape = isRetro ? DrawCrosshairRetro(uv) : DrawCrosshairModern(uv, Time);
    }
    else if (entityType < 8.5) // Cooldown bar
    {
        float progress = lifeFactor; // Progress stored in lifeFactor
        shape = isRetro ? DrawCooldownBarRetro(uv, progress) : DrawCooldownBarModern(uv, progress);
    }
    else if (entityType >= 12.0 && entityType < 22.0) // Digits 0-9
    {
        int digit = (int)(entityType - 12.0);
        shape = DrawDigit(uv, digit);
    }
    else if (entityType >= 22.0 && entityType < 23.0) // Colon
    {
        shape = DrawColon(uv);
    }
    else if (entityType >= 23.0 && entityType < 49.0) // Letters A-Z
    {
        int letter = (int)(entityType - 23.0);
        shape = DrawLetter(uv, letter);
    }
    else if (entityType >= 50.0 && entityType < 51.0) // Background rectangle
    {
        shape = DrawBackground(uv);
        float4 bgColor = baseColor;
        bgColor.a = shape * baseColor.a;
        if (bgColor.a < 0.01)
            discard;
        return bgColor;
    }
    // entityType 49 = space (nothing drawn)

    if (shape < 0.01)
        discard;

    float4 color = baseColor;

    // Apply Retro or Modern post-processing
    if (isRetro)
    {
        // Retro mode: scanlines and flat colors
        float scanline = RetroScanlines > 0.5 ? step(0.5, frac(uv.y * ViewportSize.y * 0.01)) : 1.0;
        color.rgb = QuantizeColor(color.rgb);
        color.rgb *= (0.85 + scanline * 0.15);
        color.a = shape * lifeFactor;
    }
    else
    {
        // Modern mode: neon glow effects
        float2 center = uv - 0.5;
        float dist = length(center) * 2.0;

        // Core glow
        float coreGlow = exp(-dist * dist * 2.0) * GlowIntensity;

        // Edge glow for game objects
        float edgeGlow = 0.0;
        if (entityType >= 1.5 && entityType < 8.5)
        {
            float edgeDist = abs(shape - 0.5);
            edgeGlow = exp(-edgeDist * 20.0) * NeonIntensity * 0.5;
        }

        // Brighten core
        float coreBrightness = 1.0 + (1.0 - dist) * 0.3 * shape;
        color.rgb *= coreBrightness;

        // Add white hot center for particles and explosions
        if (entityType < 1.5)
        {
            float coreWhite = (1.0 - smoothstep(0.0, 0.4, dist)) * 0.4 * lifeFactor;
            color.rgb += float3(coreWhite, coreWhite, coreWhite);
        }

        // Outer glow
        color.rgb += baseColor.rgb * edgeGlow;

        // HDR boost
        float hdrBoost = 1.0 + coreGlow * HdrMultiplier * 2.0;
        color.rgb *= hdrBoost;

        // Final alpha
        float finalAlpha = (shape + coreGlow * 0.5) * lifeFactor;
        color.a = saturate(finalAlpha);
    }

    if (color.a < 0.01)
        discard;

    return color;
}
