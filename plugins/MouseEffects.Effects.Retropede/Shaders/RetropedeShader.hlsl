// Retropede shader with Modern (neon glow) and Retro (Atari 2600) rendering modes
// Entity types: 0=particle, 1=laser, 2=cannon
// Entity types: 3=retropede head, 4=retropede body
// Entity types: 5-8=mushroom (health 4 to 1)
// Entity types: 9=spider, 10=DDT bomb, 11=DDT gas
// Entity types: 12-21=digits 0-9, 22=colon, 23-48=letters A-Z, 49=space, 50=background rect

cbuffer FrameData : register(b0)
{
    float2 ViewportSize;
    float Time;
    float RenderStyle;      // 0=Modern, 1=Retro
    float GlowIntensity;
    float NeonIntensity;
    float AnimSpeed;
    float HdrMultiplier;
    float PlayerZoneY;
    float RetroScanlines;
    float RetroPixelScale;
    float Padding1, Padding2, Padding3;
    float2 Padding4;
    float2 Padding5;
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
    float RenderStyle;
    float AnimPhase;
    float Health;
    float Padding;
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
    float Health : TEXCOORD4;
    float RenderStyle : TEXCOORD5;
};

// Helper: segment drawing for digits/letters
float DrawSegment(float2 p, float2 a, float2 b, float width)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    float d = length(pa - ba * h);
    return smoothstep(width, width * 0.3, d);
}

// Modern Mode Rendering Functions (SDF-based with smooth gradients)

// Retropede head with mandibles and eyes
float4 DrawRetropedeHeadModern(float2 uv, float anim, float4 baseColor)
{
    float2 p = uv * 2.0 - 1.0;

    // Rounded elongated body
    float2 bodySize = float2(0.55, 0.65);
    float2 d = abs(p) - bodySize;
    float body = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - 0.1;
    body = smoothstep(0.08, 0.0, body);

    // Mandibles (animated open/close)
    float mandibleAnim = sin(anim * 3.0) * 0.15;
    float2 mandibleL = float2(-0.4, -0.6 - mandibleAnim);
    float2 mandibleR = float2(0.4, -0.6 - mandibleAnim);
    float mandL = length(p - mandibleL) - 0.12;
    float mandR = length(p - mandibleR) - 0.12;
    float mandibles = min(mandL, mandR);
    mandibles = smoothstep(0.04, 0.0, mandibles);

    // Eyes (red glow)
    float eyeL = length(p - float2(-0.25, -0.25)) - 0.1;
    float eyeR = length(p - float2(0.25, -0.25)) - 0.1;
    float eyes = min(eyeL, eyeR);
    eyes = smoothstep(0.02, 0.0, eyes);

    float totalAlpha = saturate(body + mandibles + eyes);

    // Color mixing
    float3 bodyCol = baseColor.rgb;
    float3 mandCol = baseColor.rgb * float3(0.7, 1.2, 0.7); // Lighter green
    float3 eyeCol = float3(1.0, 0.2, 0.2); // Red glow

    float3 finalCol = bodyCol * body + mandCol * mandibles + eyeCol * eyes;
    finalCol = finalCol / max(totalAlpha, 0.001);

    return float4(finalCol, totalAlpha);
}

// Retropede body segment with legs
float4 DrawRetropedeBodyModern(float2 uv, float anim, float4 baseColor)
{
    float2 p = uv * 2.0 - 1.0;

    // Rounded segment
    float2 segmentSize = float2(0.5, 0.6);
    float2 d = abs(p) - segmentSize;
    float segment = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - 0.1;
    segment = smoothstep(0.08, 0.0, segment);

    // Legs (alternating animation)
    float legs = 0.0;
    float legAnim = sin(anim * 2.0);

    // Left legs
    for (int i = 0; i < 3; i++)
    {
        float yOff = (i - 1) * 0.4;
        float legExtend = -0.6 + legAnim * 0.1;
        float2 legPos = float2(legExtend, yOff);
        float leg = length(p - legPos) - 0.08;
        legs = max(legs, smoothstep(0.03, 0.0, leg));
    }

    // Right legs (opposite phase)
    for (int j = 0; j < 3; j++)
    {
        float yOff = (j - 1) * 0.4;
        float legExtend = 0.6 - legAnim * 0.1;
        float2 legPos = float2(legExtend, yOff);
        float leg = length(p - legPos) - 0.08;
        legs = max(legs, smoothstep(0.03, 0.0, leg));
    }

    float totalAlpha = saturate(segment + legs);

    // Color mixing (slightly darker than head)
    float3 segmentCol = baseColor.rgb * 0.85;
    float3 legCol = baseColor.rgb * float3(0.6, 1.1, 0.6);

    float3 finalCol = segmentCol * segment + legCol * legs;
    finalCol = finalCol / max(totalAlpha, 0.001);

    return float4(finalCol, totalAlpha);
}

// Mushroom with damage states (health 4-1)
float4 DrawMushroomModern(float2 uv, float health, float4 baseColor)
{
    float2 p = uv * 2.0 - 1.0;

    // Cap (gets smaller with damage)
    float capSize = 0.5 + (health - 1) * 0.1;
    float cap = length(p - float2(0, -0.3)) - capSize;
    cap = smoothstep(0.08, 0.0, cap);

    // Damage holes based on health
    if (health < 4.0)
    {
        float hole1 = length(p - float2(-0.2, -0.3)) - 0.15;
        cap *= smoothstep(0.0, 0.05, hole1);
    }
    if (health < 3.0)
    {
        float hole2 = length(p - float2(0.2, -0.3)) - 0.15;
        cap *= smoothstep(0.0, 0.05, hole2);
    }
    if (health < 2.0)
    {
        float hole3 = length(p - float2(0, -0.1)) - 0.2;
        cap *= smoothstep(0.0, 0.05, hole3);
    }

    // Stem (always visible)
    float2 stemSize = float2(0.15, 0.4);
    float2 stemD = abs(p - float2(0, 0.3)) - stemSize;
    float stem = length(max(stemD, 0.0)) + min(max(stemD.x, stemD.y), 0.0);
    stem = smoothstep(0.05, 0.0, stem);

    float totalAlpha = saturate(cap + stem);

    // Color mixing
    float3 capCol = baseColor.rgb;
    float3 stemCol = baseColor.rgb * float3(0.8, 0.6, 0.4); // Brown tint

    float3 finalCol = capCol * cap + stemCol * stem;
    finalCol = finalCol / max(totalAlpha, 0.001);

    return float4(finalCol, totalAlpha);
}

// Spider with 8 animated legs
float4 DrawSpiderModern(float2 uv, float anim, float4 baseColor)
{
    float2 p = uv * 2.0 - 1.0;

    // Bulbous body
    float body = length(p * float2(1.0, 1.3)) - 0.45;
    body = smoothstep(0.08, 0.0, body);

    // Eyes (menacing)
    float eyeL = length(p - float2(-0.2, -0.2)) - 0.1;
    float eyeR = length(p - float2(0.2, -0.2)) - 0.1;
    float eyes = min(eyeL, eyeR);
    eyes = smoothstep(0.02, 0.0, eyes);

    // 8 legs with walk animation
    float legs = 0.0;
    float legPhase = anim * 2.0;

    for (int side = 0; side < 2; side++)
    {
        float xSign = side == 0 ? -1.0 : 1.0;
        for (int i = 0; i < 4; i++)
        {
            float yOff = (i - 1.5) * 0.3;
            float legAnim = sin(legPhase + i * 1.5) * 0.15;
            float2 legPos = float2(xSign * (0.6 + legAnim), yOff);
            float leg = length(p - legPos) - 0.07;
            legs = max(legs, smoothstep(0.03, 0.0, leg));
        }
    }

    float totalAlpha = saturate(body + eyes + legs);

    // Color mixing (purple/magenta)
    float3 bodyCol = baseColor.rgb;
    float3 legCol = baseColor.rgb * 0.8;
    float3 eyeCol = float3(1.0, 0.9, 0.2); // Yellow eyes

    float3 finalCol = bodyCol * body + legCol * legs + eyeCol * eyes;
    finalCol = finalCol / max(totalAlpha, 0.001);

    return float4(finalCol, totalAlpha);
}

// DDT Bomb (cylindrical canister)
float4 DrawDDTBombModern(float2 uv, float4 baseColor)
{
    float2 p = uv * 2.0 - 1.0;

    // Cylindrical body
    float2 bodySize = float2(0.35, 0.6);
    float2 d = abs(p) - bodySize;
    float body = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - 0.08;
    body = smoothstep(0.05, 0.0, body);

    // Top cap
    float2 capSize = float2(0.4, 0.15);
    float2 capD = abs(p - float2(0, -0.7)) - capSize;
    float cap = length(max(capD, 0.0)) + min(max(capD.x, capD.y), 0.0);
    cap = smoothstep(0.05, 0.0, cap);

    // Label stripes
    float stripes = 0.0;
    if (abs(p.y) < 0.3)
    {
        stripes = step(0.5, frac(p.y * 3.0));
    }

    float totalAlpha = saturate(body + cap);

    float3 bodyCol = baseColor.rgb;
    float3 stripeCol = float3(0.2, 0.2, 0.2);

    float3 finalCol = lerp(bodyCol, stripeCol, stripes * 0.5);
    finalCol = finalCol * (body + cap);

    return float4(finalCol, totalAlpha);
}

// DDT Gas (expanding translucent cloud)
float4 DrawDDTGasModern(float2 uv, float lifeFactor, float4 baseColor)
{
    float2 center = uv - 0.5;
    float dist = length(center) * 2.0;

    // Expanding radial gradient
    float cloudSize = 0.5 + lifeFactor * 1.5;
    float cloud = 1.0 - smoothstep(0.0, cloudSize, dist);

    // Wispy pattern
    float noise = sin(uv.x * 10.0 + lifeFactor * 3.0) * sin(uv.y * 8.0 - lifeFactor * 2.0) * 0.3 + 0.7;
    cloud *= noise;

    // Fade out as it expands
    cloud *= (1.0 - lifeFactor * 0.7);

    float3 gasCol = baseColor.rgb * float3(0.5, 1.2, 0.5); // Green tint
    return float4(gasCol, cloud * 0.6);
}

// Cannon (player ship)
float4 DrawCannonModern(float2 uv, float4 baseColor)
{
    float2 p = uv * 2.0 - 1.0;

    // Ship hull pointing up
    float hull = max(abs(p.x) * 0.8 + p.y * 0.6 - 0.4, -p.y - 0.7);
    hull = smoothstep(0.05, 0.0, hull);

    // Base
    float2 baseSize = float2(0.5, 0.2);
    float2 d = abs(p - float2(0, 0.6)) - baseSize;
    float shipBase = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0);
    shipBase = smoothstep(0.05, 0.0, shipBase);

    float totalAlpha = saturate(hull + shipBase);
    return float4(baseColor.rgb, totalAlpha);
}

// Laser (elongated projectile)
float4 DrawLaserModern(float2 uv, float4 baseColor)
{
    float2 p = uv * 2.0 - 1.0;

    // Elongated ellipse
    float2 laserSize = float2(0.2, 0.8);
    float laser = length(p / laserSize) - 1.0;
    laser = smoothstep(0.05, 0.0, laser);

    // Bright core
    float core = length(p / laserSize * 0.5) - 1.0;
    core = smoothstep(0.1, 0.0, core);

    float totalAlpha = saturate(laser + core);

    float3 finalCol = baseColor.rgb * laser + float3(1.5, 1.5, 1.5) * core;
    return float4(finalCol, totalAlpha);
}

// Retro Mode Rendering Functions (pixel-based with hard edges)

// Quantize UV to pixel grid
float2 QuantizeUV(float2 uv, float pixelScale)
{
    return floor(uv * pixelScale) / pixelScale;
}

// Retro color quantization (limited palette)
float3 QuantizeColor(float3 color)
{
    // Quantize to 4 levels per channel (Atari-style)
    return floor(color * 4.0) / 4.0;
}

// Retro versions use step functions instead of smoothstep
float4 DrawRetropedeHeadRetro(float2 uv, float anim, float4 baseColor)
{
    uv = QuantizeUV(uv, 8.0);
    float2 p = uv * 2.0 - 1.0;

    float body = step(length(p * float2(1.2, 1.0)) - 0.6, 0.0);

    float mandibleAnim = sin(anim * 3.0) * 0.15;
    float mandL = step(length(p - float2(-0.4, -0.6 - mandibleAnim)) - 0.12, 0.0);
    float mandR = step(length(p - float2(0.4, -0.6 - mandibleAnim)) - 0.12, 0.0);

    float eyeL = step(length(p - float2(-0.25, -0.25)) - 0.1, 0.0);
    float eyeR = step(length(p - float2(0.25, -0.25)) - 0.1, 0.0);

    float alpha = saturate(body + mandL + mandR + eyeL + eyeR);
    float3 color = QuantizeColor(baseColor.rgb);

    // Red eyes in retro
    if (eyeL > 0.5 || eyeR > 0.5)
        color = float3(1.0, 0.0, 0.0);

    return float4(color, alpha);
}

float4 DrawRetropedeBodyRetro(float2 uv, float anim, float4 baseColor)
{
    uv = QuantizeUV(uv, 8.0);
    float2 p = uv * 2.0 - 1.0;

    float segment = step(length(p * float2(1.1, 1.0)) - 0.55, 0.0);

    float legs = 0.0;
    float legAnim = sin(anim * 2.0);
    for (int i = -1; i <= 1; i++)
    {
        legs = max(legs, step(length(p - float2(-0.6 + legAnim * 0.1, i * 0.4)) - 0.08, 0.0));
        legs = max(legs, step(length(p - float2(0.6 - legAnim * 0.1, i * 0.4)) - 0.08, 0.0));
    }

    float alpha = saturate(segment + legs);
    float3 color = QuantizeColor(baseColor.rgb * 0.85);

    return float4(color, alpha);
}

float4 DrawMushroomRetro(float2 uv, float health, float4 baseColor)
{
    uv = QuantizeUV(uv, 8.0);
    float2 p = uv * 2.0 - 1.0;

    float capSize = 0.5 + (health - 1) * 0.1;
    float cap = step(length(p - float2(0, -0.3)) - capSize, 0.0);

    float stem = step(abs(p.x) - 0.15, 0.0) * step(abs(p.y - 0.3) - 0.4, 0.0);

    float alpha = saturate(cap + stem);
    float3 color = QuantizeColor(baseColor.rgb);

    return float4(color, alpha);
}

// Explosion particle (circular with soft falloff)
float DrawParticle(float2 uv, float lifeFactor)
{
    float2 center = uv - 0.5;
    float dist = length(center) * 2.0;
    float alpha = 1.0 - smoothstep(0.3, 1.0, dist);
    return alpha * lifeFactor;
}

// 7-segment digit display (reused from Invaders)
float DrawDigit(float2 uv, int digit)
{
    float2 p = uv * 2.0 - 1.0;
    p.y = -p.y;

    float w = 0.15;
    float result = 0.0;

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

// Colon for timer display
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

// Letter rendering (A-Z) using segment style
float DrawLetter(float2 uv, int letter)
{
    float2 p = uv * 2.0 - 1.0;
    p.y = -p.y;

    float w = 0.15;
    float result = 0.0;

    float2 top1 = float2(-0.4, -0.8), top2 = float2(0.4, -0.8);
    float2 mid1 = float2(-0.4, 0.0), mid2 = float2(0.4, 0.0);
    float2 bot1 = float2(-0.4, 0.8), bot2 = float2(0.4, 0.8);
    float2 ltop1 = float2(-0.5, -0.7), ltop2 = float2(-0.5, -0.1);
    float2 lbot1 = float2(-0.5, 0.1), lbot2 = float2(-0.5, 0.7);
    float2 rtop1 = float2(0.5, -0.7), rtop2 = float2(0.5, -0.1);
    float2 rbot1 = float2(0.5, 0.1), rbot2 = float2(0.5, 0.7);

    if (letter == 0) // A
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, mid1, mid2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
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
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, float2(-0.4, -0.8), float2(0.3, -0.8), w));
        result = max(result, DrawSegment(p, float2(-0.4, 0.8), float2(0.3, 0.8), w));
        result = max(result, DrawSegment(p, float2(0.3, -0.7), float2(0.5, -0.4), w));
        result = max(result, DrawSegment(p, float2(0.5, -0.4), float2(0.5, 0.4), w));
        result = max(result, DrawSegment(p, float2(0.5, 0.4), float2(0.3, 0.7), w));
    }
    else if (letter == 4) // E
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, mid1, mid2, w));
        result = max(result, DrawSegment(p, bot1, bot2, w));
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
    else if (letter == 12) // M
    {
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, rbot1, rbot2, w));
        result = max(result, DrawSegment(p, float2(-0.4, -0.7), float2(0, -0.2), w));
        result = max(result, DrawSegment(p, float2(0.4, -0.7), float2(0, -0.2), w));
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
    else if (letter == 17) // R
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, mid1, mid2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, lbot1, lbot2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, float2(0.1, 0.1), float2(0.4, 0.7), w));
    }
    else if (letter == 18) // S
    {
        result = max(result, DrawSegment(p, top1, top2, w));
        result = max(result, DrawSegment(p, mid1, mid2, w));
        result = max(result, DrawSegment(p, bot1, bot2, w));
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, rbot1, rbot2, w));
    }
    else if (letter == 21) // V
    {
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, lbot1, float2(0, 0.7), w));
        result = max(result, DrawSegment(p, rbot1, float2(0, 0.7), w));
    }
    else if (letter == 24) // Y
    {
        result = max(result, DrawSegment(p, ltop1, ltop2, w));
        result = max(result, DrawSegment(p, rtop1, rtop2, w));
        result = max(result, DrawSegment(p, float2(-0.5, -0.1), float2(0, 0.2), w));
        result = max(result, DrawSegment(p, float2(0.5, -0.1), float2(0, 0.2), w));
        result = max(result, DrawSegment(p, float2(0, 0.2), float2(0, 0.7), w));
    }

    return result;
}

// Background rectangle
float DrawBackground(float2 uv)
{
    float2 p = uv * 2.0 - 1.0;
    float2 size = float2(0.95, 0.95);
    float radius = 0.1;
    float2 d = abs(p) - size + radius;
    float dist = length(max(d, 0.0)) + min(max(d.x, d.y), 0.0) - radius;
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
        output.Health = 0;
        output.RenderStyle = 0;
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
    output.AnimPhase = entity.AnimPhase;
    output.Health = entity.Health;
    output.RenderStyle = entity.RenderStyle;
    return output;
}

float4 PSMain(VSOutput input) : SV_TARGET
{
    float entityType = input.EntityType;
    float2 uv = input.TexCoord;
    float lifeFactor = input.LifeFactor;
    float4 baseColor = input.Color;
    float renderStyle = input.RenderStyle;
    float health = input.Health;
    float anim = input.AnimPhase;

    float4 coloredResult = float4(0, 0, 0, 0);
    bool isRetro = renderStyle > 0.5;

    // Dispatch to appropriate rendering function
    if (entityType < 0.5) // Particle
    {
        float shape = DrawParticle(uv, lifeFactor);
        coloredResult = float4(baseColor.rgb, shape);
    }
    else if (entityType < 1.5) // Laser
    {
        coloredResult = DrawLaserModern(uv, baseColor);
    }
    else if (entityType < 2.5) // Cannon
    {
        coloredResult = DrawCannonModern(uv, baseColor);
    }
    else if (entityType < 3.5) // Retropede head
    {
        coloredResult = isRetro ? DrawRetropedeHeadRetro(uv, anim, baseColor) : DrawRetropedeHeadModern(uv, anim, baseColor);
    }
    else if (entityType < 4.5) // Retropede body
    {
        coloredResult = isRetro ? DrawRetropedeBodyRetro(uv, anim, baseColor) : DrawRetropedeBodyModern(uv, anim, baseColor);
    }
    else if (entityType >= 5.0 && entityType < 9.0) // Mushroom (health 4-1)
    {
        float mushroomHealth = 9.0 - entityType; // 5->4, 6->3, 7->2, 8->1
        coloredResult = isRetro ? DrawMushroomRetro(uv, mushroomHealth, baseColor) : DrawMushroomModern(uv, mushroomHealth, baseColor);
    }
    else if (entityType < 9.5) // Spider
    {
        coloredResult = DrawSpiderModern(uv, anim, baseColor);
    }
    else if (entityType < 10.5) // DDT Bomb
    {
        coloredResult = DrawDDTBombModern(uv, baseColor);
    }
    else if (entityType < 11.5) // DDT Gas
    {
        coloredResult = DrawDDTGasModern(uv, lifeFactor, baseColor);
    }
    else if (entityType >= 12.0 && entityType < 22.0) // Digits 0-9
    {
        int digit = (int)(entityType - 12.0);
        float shape = DrawDigit(uv, digit);
        coloredResult = float4(baseColor.rgb, shape);
    }
    else if (entityType >= 22.0 && entityType < 23.0) // Colon
    {
        float shape = DrawColon(uv);
        coloredResult = float4(baseColor.rgb, shape);
    }
    else if (entityType >= 23.0 && entityType < 49.0) // Letters A-Z
    {
        int letter = (int)(entityType - 23.0);
        float shape = DrawLetter(uv, letter);
        coloredResult = float4(baseColor.rgb, shape);
    }
    else if (entityType >= 50.0 && entityType < 51.0) // Background
    {
        float shape = DrawBackground(uv);
        float4 bgColor = baseColor;
        bgColor.a = shape * baseColor.a;
        if (bgColor.a < 0.01)
            discard;
        return bgColor;
    }
    // entityType 49 = space (nothing drawn)

    if (coloredResult.a < 0.01)
        discard;

    float4 finalColor = coloredResult;

    // Apply Modern or Retro post-processing
    if (isRetro)
    {
        // Retro mode: scanlines and color quantization
        float scanline = step(0.5, frac(uv.y * ViewportSize.y * RetroScanlines));
        finalColor.rgb = QuantizeColor(finalColor.rgb);
        finalColor.rgb *= (0.85 + scanline * 0.15);
    }
    else
    {
        // Modern mode: neon glow effects
        float2 center = uv - 0.5;
        float dist = length(center) * 2.0;

        // Core glow
        float coreGlow = exp(-dist * dist * 2.0) * GlowIntensity;

        // Edge glow for game entities
        float edgeGlow = 0.0;
        if (entityType >= 2.5)
        {
            float edgeDist = abs(coloredResult.a - 0.5);
            edgeGlow = exp(-edgeDist * 20.0) * NeonIntensity * 0.5;
        }

        // Brighten core
        float coreBrightness = 1.0 + (1.0 - dist) * 0.3 * coloredResult.a;
        finalColor.rgb *= coreBrightness;

        // Add white hot center for particles/lasers
        if (entityType < 1.5)
        {
            float coreWhite = (1.0 - smoothstep(0.0, 0.4, dist)) * 0.4 * lifeFactor;
            finalColor.rgb += float3(coreWhite, coreWhite, coreWhite);
        }

        // Neon scanline effect
        if (entityType >= 2.5)
        {
            float scanline = sin(uv.y * 40.0 + Time * 5.0) * 0.1 + 0.9;
            finalColor.rgb *= scanline;
            finalColor.rgb += baseColor.rgb * edgeGlow;
        }

        // HDR boost
        float hdrBoost = 1.0 + coreGlow * HdrMultiplier * 2.0;
        finalColor.rgb *= hdrBoost;

        // Final alpha with glow
        finalColor.a = saturate(coloredResult.a + coreGlow * 0.5) * lifeFactor;
    }

    if (finalColor.a < 0.01)
        discard;

    return finalColor;
}
