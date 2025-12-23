// Firework particle shader with instancing, glow, trail effects, and style support

cbuffer FrameData : register(b0)
{
    float2 ViewportSize;
    float Time;
    float GlowIntensity;
    float EnableTrails;
    float TrailLength;
    float HdrMultiplier;
    float Padding1;
    float Padding2;
    float Padding3;
    float Padding4;
    float Padding5;
};

// Style IDs
static const uint STYLE_CLASSIC = 0;
static const uint STYLE_SPINNER = 1;
static const uint STYLE_WILLOW = 2;
static const uint STYLE_CRACKLING = 3;
static const uint STYLE_CHRYSANTHEMUM = 4;
static const uint STYLE_RANDOM = 5;
static const uint STYLE_BROCADE = 6;
static const uint STYLE_COMET = 7;
static const uint STYLE_CROSSETTE = 8;
static const uint STYLE_PALM = 9;
static const uint STYLE_PEONY = 10;
static const uint STYLE_PEARLS = 11;
static const uint STYLE_FISH = 12;
static const uint STYLE_GREENBEES = 13;
static const uint STYLE_PISTIL = 14;
static const uint STYLE_STARS = 15;
static const uint STYLE_TAIL = 16;
static const uint STYLE_STROBE = 17;
static const uint STYLE_GLITTER = 18;

// Helper functions
uint GetStyleId(uint flags) { return flags & 0xFF; }
bool HasSparkTrails(uint flags) { return (flags & 0x100) != 0; }

struct ParticleInstance
{
    float2 Position;      // 8
    float2 Velocity;      // 8
    float4 Color;         // 16 = 32
    float Size;           // 4
    float Life;           // 4
    float MaxLife;        // 4
    float StyleData1;     // 4 = 48 (angular velocity / flash phase)
    float StyleData2;     // 4 (spin radius / flash frequency)
    float StyleData3;     // 4 (spawn time / jitter seed)
    uint StyleFlags;      // 4 (style ID in low bits)
    float Padding;        // 4 = 64
};

StructuredBuffer<ParticleInstance> Particles : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
    float2 TexCoord : TEXCOORD0;
    float LifeFactor : TEXCOORD1;
    float IsTrail : TEXCOORD2;
    uint StyleId : TEXCOORD3;
    float3 StyleData : TEXCOORD4;  // Pass style data to pixel shader
};

// Vertex shader for particle quads
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    ParticleInstance particle = Particles[instanceId];

    // Skip dead particles
    if (particle.Life <= 0)
    {
        VSOutput output;
        output.Position = float4(0, 0, -2, 1);
        output.Color = float4(0, 0, 0, 0);
        output.TexCoord = float2(0, 0);
        output.LifeFactor = 0;
        output.IsTrail = 0;
        output.StyleId = 0;
        output.StyleData = float3(0, 0, 0);
        return output;
    }

    // Generate quad vertex (2 triangles = 6 vertices)
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

    // Calculate life factor for fade
    float lifeFactor = particle.Life / particle.MaxLife;

    // Size with fade - particles shrink as they die
    float size = particle.Size * (0.3 + 0.7 * lifeFactor);

    uint styleId = GetStyleId(particle.StyleFlags);
    float isTrail = 0.0;

    // Style-specific vertex processing
    if (styleId == STYLE_SPINNER)
    {
        // Spinner: Add rotation offset
        float angularVel = particle.StyleData1;
        float spinRadius = particle.StyleData2;
        float phase = particle.StyleData3 + angularVel * Time;

        // Slight offset for spinning effect
        float spinOffsetX = cos(phase) * spinRadius * 0.02;
        float spinOffsetY = sin(phase) * spinRadius * 0.02;
        offset.x += spinOffsetX;
        offset.y += spinOffsetY;

        // Elongate in direction of spin
        if (HasSparkTrails(particle.StyleFlags))
        {
            float stretch = 1.0 + abs(angularVel) * 0.05;
            offset *= stretch;
        }
    }
    else if (styleId == STYLE_WILLOW)
    {
        // Willow: Elongate vertically for drooping effect
        float droopFactor = particle.StyleData1;
        offset.y *= 1.0 + droopFactor * 0.3 * (1.0 - lifeFactor);

        // Fade more at the tips
        size *= 0.8 + 0.2 * lifeFactor;
    }
    else if (styleId == STYLE_CRACKLING)
    {
        // Crackling: Size pulse based on flash
        float flashPhase = particle.StyleData1;
        float flashFreq = particle.StyleData2;
        float flash = sin(flashPhase + Time * flashFreq) * 0.5 + 0.5;

        // Pulse size with flash
        size *= 0.7 + flash * 0.6;
    }
    else if (styleId == STYLE_CHRYSANTHEMUM)
    {
        // Chrysanthemum: Brighter, slightly larger heads
        float spawnTime = particle.StyleData3;
        float age = Time - spawnTime;

        // Trail particles (high StyleData2) are smaller
        if (particle.StyleData2 >= 8.0)
        {
            size *= 0.6;
        }
    }
    else if (styleId == STYLE_BROCADE)
    {
        // Brocade: Metallic glittering effect with size variation
        float glitterPhase = particle.StyleData1;
        float glitter = sin(glitterPhase + Time * 15.0) * 0.5 + 0.5;
        size *= 0.8 + glitter * 0.4;
    }
    else if (styleId == STYLE_COMET)
    {
        // Comet: Larger bright particles (trails are real particles now)
        size *= 1.3;
    }
    else if (styleId == STYLE_CROSSETTE)
    {
        // Crossette: Particles that split into cross pattern
        float splitPhase = particle.StyleData1;
        float crossAngle = particle.StyleData2;
        // Slight size variation based on split generation
        size *= 0.85 + splitPhase * 0.3;
    }
    else if (styleId == STYLE_PALM)
    {
        // Palm: Drooping fronds effect
        float droopFactor = particle.StyleData1;
        offset.y *= 1.0 + droopFactor * 0.4 * (1.0 - lifeFactor);
        size *= 1.1;
    }
    else if (styleId == STYLE_PEONY)
    {
        // Peony: Round, full bloom - slightly larger, uniform
        size *= 1.15;
    }
    else if (styleId == STYLE_PEARLS)
    {
        // Pearls: Small spherical uniform particles
        size *= 0.7;
    }
    else if (styleId == STYLE_FISH)
    {
        // Fish: Swimming wavy motion
        float swimPhase = particle.StyleData1;
        float wiggle = sin(swimPhase + Time * 8.0) * 0.3;
        offset.x += wiggle * (1.0 - lifeFactor);
    }
    else if (styleId == STYLE_GREENBEES)
    {
        // Green Bees: Small, erratic motion
        float buzzPhase = particle.StyleData1;
        float buzzX = sin(buzzPhase + Time * 25.0) * 0.2;
        float buzzY = cos(buzzPhase * 1.3 + Time * 20.0) * 0.2;
        offset.x += buzzX;
        offset.y += buzzY;
        size *= 0.6;
    }
    else if (styleId == STYLE_PISTIL)
    {
        // Pistil: Core particles are larger and brighter
        float isCore = 1.0 - particle.StyleData1;
        size *= 0.9 + isCore * 0.5;
    }
    else if (styleId == STYLE_STARS)
    {
        // Stars: Pulsing glow effect
        float flamePhase = particle.StyleData1;
        float pulse = sin(flamePhase * 0.01 + Time * 3.0) * 0.3 + 1.0;
        size *= pulse;
    }
    else if (styleId == STYLE_TAIL)
    {
        // Tail: Normal size (trails are real particles now)
    }
    else if (styleId == STYLE_STROBE)
    {
        // Strobe: Rapid on/off flashing
        float strobePhase = particle.StyleData1;
        float strobeFreq = particle.StyleData2;
        float strobe = step(0.5, frac(strobePhase + Time * strobeFreq * 0.1));
        size *= 0.5 + strobe * 0.8;
    }
    else if (styleId == STYLE_GLITTER)
    {
        // Glitter: Random sparkle size variation
        float sparklePhase = particle.StyleData1;
        float sparkle = frac(sin(sparklePhase + Time * 10.0) * 43758.5453);
        size *= 0.6 + sparkle * 0.8;
    }

    // Note: Trail effects now use actual trail particles spawned by C# code
    // instead of stretching particles in the shader

    // Convert to normalized device coordinates
    float2 screenPos = particle.Position + offset * size;
    float2 ndcPos = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndcPos.y = -ndcPos.y;

    VSOutput output;
    output.Position = float4(ndcPos, 0, 1);
    output.Color = particle.Color;
    output.TexCoord = texCoord;
    output.LifeFactor = lifeFactor;
    output.IsTrail = isTrail;
    output.StyleId = styleId;
    output.StyleData = float3(particle.StyleData1, particle.StyleData2, particle.StyleData3);
    return output;
}

// Pixel shader - renders particle with glow effect and style variations
float4 PSMain(VSOutput input) : SV_TARGET
{
    float2 center = input.TexCoord - 0.5;
    float dist = length(center) * 2.0;

    // Particle edge falloff
    float alpha = 1.0 - smoothstep(0.3, 1.0, dist);

    // Add glow effect
    float glow = exp(-dist * dist * 2.0) * GlowIntensity;

    float finalAlpha = (alpha + glow) * input.LifeFactor;

    float4 color = input.Color;

    // Style-specific pixel effects
    uint styleId = input.StyleId;

    if (styleId == STYLE_SPINNER)
    {
        // Spinner: Enhanced glow, slight color shift
        float angularVel = input.StyleData.x;
        float phase = input.StyleData.z + angularVel * Time;

        // Color shimmer based on spin
        float shimmer = sin(phase * 2.0) * 0.1 + 1.0;
        color.rgb *= shimmer;

        // Extra glow for spinning particles
        glow *= 1.3;
    }
    else if (styleId == STYLE_WILLOW)
    {
        // Willow: Softer edges, golden tint towards tips
        float droopFactor = input.StyleData.x;

        // Softer falloff
        alpha = 1.0 - smoothstep(0.2, 1.0, dist);

        // Add warm tint as particles fall
        float warmth = (1.0 - input.LifeFactor) * 0.2;
        color.r = min(1.0, color.r + warmth);
        color.g = max(0.0, color.g - warmth * 0.5);
    }
    else if (styleId == STYLE_CRACKLING)
    {
        // Crackling: Sharp flashing effect
        float flashPhase = input.StyleData.x;
        float flashFreq = input.StyleData.y;

        // Sharp flash calculation
        float flash = sin(flashPhase + Time * flashFreq);
        float sharpFlash = step(0.3, flash);  // Binary flash

        // Flashing brightness
        color.rgb *= 0.5 + sharpFlash * 1.0;

        // Sharper edges for crackling
        alpha = 1.0 - smoothstep(0.1, 0.6, dist);

        // Random sparkle
        float sparkle = frac(sin(input.StyleData.z + Time * 100.0) * 43758.5453);
        if (sparkle > 0.95)
        {
            color.rgb += float3(0.5, 0.5, 0.5);
        }
    }
    else if (styleId == STYLE_CHRYSANTHEMUM)
    {
        // Chrysanthemum: Brighter core for trail heads
        float isTrailSpark = step(8.0, input.StyleData.y);

        if (isTrailSpark < 0.5)
        {
            // Main particles: very bright core
            float coreBright = 1.0 - smoothstep(0.0, 0.4, dist);
            color.rgb += float3(coreBright, coreBright, coreBright) * 0.5;
        }
        else
        {
            // Trail sparks: softer, dimmer
            alpha *= 0.7;
        }
    }
    else if (styleId == STYLE_BROCADE)
    {
        // Brocade: GOLD woven clusters - force golden color
        float glitterPhase = input.StyleData.x;
        float shimmer = sin(glitterPhase + Time * 20.0) * 0.5 + 0.5;

        // FORCE GOLD color
        color.rgb = float3(1.0, 0.85, 0.3);
        color.rgb *= 0.7 + shimmer * 0.5;

        // Intense sparkle effect for "woven" look
        float sparkle = frac(sin(glitterPhase * 100.0 + Time * 80.0) * 43758.5453);
        if (sparkle > 0.85)
        {
            color.rgb = float3(1.0, 1.0, 0.8); // White-gold flash
        }

        // Clustered appearance - sharper edges
        alpha = 1.0 - smoothstep(0.3, 0.7, dist);
    }
    else if (styleId == STYLE_COMET)
    {
        // Comet: Bright white-hot head with long glittering trail
        float coreGlow = 1.0 - smoothstep(0.0, 0.3, dist);

        // White-hot center
        color.rgb = lerp(color.rgb, float3(1.0, 1.0, 1.0), coreGlow * 0.8);

        // Elongated soft trail
        alpha = 1.0 - smoothstep(0.1, 0.95, dist);

        // Extra bright
        color.rgb *= 1.5;
        glow *= 2.0;
    }
    else if (styleId == STYLE_CROSSETTE)
    {
        // Crossette: Stars that split - show as bright splitting sparks
        float splitGen = input.StyleData.x; // Generation of split

        // Brighter for later generations (newly split)
        float brightness = 1.0 + splitGen * 0.5;
        color.rgb *= brightness;

        // Sharp star-like points
        float2 centered = center;
        float angle = atan2(centered.y, centered.x);
        float starPoints = pow(abs(cos(angle * 2.5)), 3.0);
        alpha = (1.0 - dist) * (0.6 + starPoints * 0.4);

        // Trailing sparks
        color.rgb += float3(0.2, 0.2, 0.2) * (1.0 - dist);
    }
    else if (styleId == STYLE_PALM)
    {
        // Palm: GOLD cascading palm tree branches
        // FORCE GOLD/SILVER color
        color.rgb = float3(1.0, 0.9, 0.4);

        // Softer, longer falloff for cascading effect
        alpha = 1.0 - smoothstep(0.0, 1.0, dist * 0.7);

        // Fade to orange as they fall
        float age = 1.0 - input.LifeFactor;
        color.r = min(1.0, color.r);
        color.g *= 1.0 - age * 0.3;
        color.b *= 1.0 - age * 0.5;

        // Elongated drooping shape
        glow *= 1.3;
    }
    else if (styleId == STYLE_PEONY)
    {
        // Peony: Perfect sphere that CHANGES COLOR as it expands
        float age = 1.0 - input.LifeFactor;

        // Color shift: starts one color, changes to another
        float3 startColor = color.rgb;
        float3 endColor = float3(1.0 - startColor.r, 1.0 - startColor.g * 0.5, startColor.b);
        color.rgb = lerp(startColor, endColor, age * 0.7);

        // Soft round edges
        alpha = 1.0 - smoothstep(0.2, 0.9, dist);

        // Bright center
        float coreBright = 1.0 - smoothstep(0.0, 0.4, dist);
        color.rgb += float3(coreBright, coreBright, coreBright) * 0.3;
    }
    else if (styleId == STYLE_PEARLS)
    {
        // Pearls: Small bright dots, NO TRAILS, sharp spheres
        // Very sharp edge - almost binary
        alpha = 1.0 - smoothstep(0.4, 0.5, dist);

        // Bright white-ish glow
        float sheen = 1.0 - smoothstep(0.0, 0.3, dist);
        color.rgb = lerp(color.rgb, float3(1.0, 1.0, 0.95), sheen * 0.6);

        // No trail effect - keep them round and distinct
        glow *= 0.5;
    }
    else if (styleId == STYLE_FISH)
    {
        // Silver Fish: SILVER color, wriggling, blue tracer
        float swimPhase = input.StyleData.x;
        float isTracer = input.StyleData.y; // Blue tracer marker

        if (isTracer > 0.5)
        {
            // Blue tracer
            color.rgb = float3(0.3, 0.5, 1.0);
        }
        else
        {
            // SILVER color - force it
            color.rgb = float3(0.9, 0.92, 0.95);
        }

        // Wriggling shimmer
        float wiggle = sin(swimPhase + Time * 12.0) * 0.3 + 0.7;
        color.rgb *= wiggle;

        // Elongated fish shape
        alpha = 1.0 - smoothstep(0.2, 0.8, dist);
    }
    else if (styleId == STYLE_GREENBEES)
    {
        // Green Bees: BRIGHT GREEN, small, erratic swarm
        float buzzPhase = input.StyleData.x;
        float buzz = sin(buzzPhase + Time * 30.0);

        // FORCE BRIGHT GREEN
        color.rgb = float3(0.2, 1.0, 0.3);

        // Erratic brightness - buzzing effect
        float erratic = frac(sin(buzzPhase * 50.0 + Time * 40.0) * 43758.5453);
        color.rgb *= 0.5 + erratic * 0.7;

        // Small sharp particles
        alpha = 1.0 - smoothstep(0.3, 0.5, dist);

        // Occasional bright flash
        if (erratic > 0.9)
        {
            color.rgb = float3(0.5, 1.0, 0.6);
        }
    }
    else if (styleId == STYLE_PISTIL)
    {
        // Pistil: VERY BRIGHT central core effect
        float isCore = 1.0 - input.StyleData.x;
        float brightness = input.StyleData.y;

        if (isCore > 0.5)
        {
            // Core: WHITE-HOT center, very intense
            float coreBright = 1.0 - smoothstep(0.0, 0.4, dist);
            color.rgb = lerp(color.rgb, float3(1.0, 1.0, 1.0), coreBright * 0.9);
            glow *= 2.5;
            alpha = 1.0 - smoothstep(0.1, 0.8, dist);
        }
        else
        {
            // Outer ring - dimmer
            color.rgb *= 0.7;
            alpha = 1.0 - smoothstep(0.3, 0.7, dist);
        }
    }
    else if (styleId == STYLE_STARS)
    {
        // Stars: Large FLAMING glowing balls
        float flamePhase = input.StyleData.x;

        // Flickering flame effect
        float flicker1 = sin(flamePhase * 0.01 + Time * 5.0) * 0.2;
        float flicker2 = sin(flamePhase * 0.02 + Time * 7.0) * 0.15;
        float flame = 1.0 + flicker1 + flicker2;

        // Fiery orange-yellow-white gradient
        float coreDist = smoothstep(0.0, 0.6, dist);
        color.rgb = lerp(
            float3(1.0, 1.0, 0.9),  // White-hot center
            float3(1.0, 0.5, 0.1),  // Orange edge
            coreDist
        );
        color.rgb *= flame;

        // Large soft glow
        alpha = 1.0 - smoothstep(0.1, 1.0, dist * 0.8);
        glow *= 2.0;
    }
    else if (styleId == STYLE_TAIL)
    {
        // Tail: LONG comet-like trail behind
        // Elongated fade
        alpha = pow(1.0 - dist, 0.5);

        // Gradient: bright at head, fading along tail
        float headGlow = 1.0 - smoothstep(0.0, 0.3, dist);
        color.rgb = lerp(color.rgb * 0.5, color.rgb * 1.5, headGlow);

        // Soft trailing edge
        alpha *= input.LifeFactor;
    }
    else if (styleId == STYLE_STROBE)
    {
        // Strobe: STRONG binary ON/OFF flashing like flash bulbs
        float strobePhase = input.StyleData.x;
        float strobeFreq = input.StyleData.y;

        // Sharp binary strobe - fully ON or almost OFF
        float strobeTime = strobePhase + Time * strobeFreq * 0.15;
        float strobe = step(0.6, frac(strobeTime));

        if (strobe > 0.5)
        {
            // ON: Very bright white flash
            color.rgb = float3(1.0, 1.0, 1.0);
            alpha = 1.0 - smoothstep(0.0, 0.6, dist);
            glow *= 3.0;
        }
        else
        {
            // OFF: Nearly invisible
            color.rgb *= 0.1;
            alpha *= 0.05;
        }
    }
    else if (styleId == STYLE_GLITTER)
    {
        // Glitter: Constant spray of STROBING glitter sparkles
        float sparklePhase = input.StyleData.x;

        // Multiple overlapping sparkle frequencies
        float sparkle1 = frac(sin(sparklePhase + Time * 15.0) * 43758.5453);
        float sparkle2 = frac(sin(sparklePhase * 1.3 + Time * 23.0) * 28473.2847);
        float sparkle = max(sparkle1, sparkle2);

        // Intense sparkle when triggered
        if (sparkle > 0.7)
        {
            float intensity = (sparkle - 0.7) / 0.3;
            color.rgb = lerp(color.rgb, float3(1.0, 1.0, 1.0), intensity * 0.8);
            alpha = 1.0 - smoothstep(0.0, 0.5, dist);
        }
        else
        {
            // Dim between sparkles
            color.rgb *= 0.4;
            alpha = (1.0 - dist) * 0.5;
        }

        // Sharp small points
        alpha *= 1.0 - smoothstep(0.4, 0.6, dist);
    }

    // Brighten core
    float coreBrightness = 1.0 + (1.0 - dist) * 0.5;
    color.rgb *= coreBrightness;

    // White hot core
    float coreWhite = (1.0 - smoothstep(0.0, 0.3, dist)) * 0.3 * input.LifeFactor;
    color.rgb += float3(coreWhite, coreWhite, coreWhite);

    // HDR boost - amplify bright areas for HDR displays
    float hdrBoost = 1.0 + glow * HdrMultiplier * 2.0;
    color.rgb *= hdrBoost;

    finalAlpha = (alpha + glow) * input.LifeFactor;
    color.a = finalAlpha;

    if (color.a < 0.01)
        discard;

    return color;
}
