// Fire Trail Shader - GPU-accelerated particle rendering with realistic fire effects
// Features: Temperature-based color gradients, flickering, glow, and HDR support

cbuffer FireConstants : register(b0)
{
    float2 ViewportSize;      // Screen dimensions
    float2 MousePosition;     // Current mouse position
    float Time;               // Animation time
    float Intensity;          // Overall intensity
    float FlameHeight;        // Flame rise height
    float FlameWidth;         // Trail width
    float TurbulenceAmount;   // Chaos amount
    float SmokeAmount;        // Smoke particle ratio
    float EmberAmount;        // Ember particle ratio
    float GlowIntensity;      // Glow brightness
    float HdrMultiplier;      // HDR peak brightness
    float FireStyle;          // 0=Campfire, 1=Torch, 2=Inferno
    float ColorSaturation;    // Color vibrancy
    float FlickerSpeed;       // Flicker animation speed
    float4 Padding;           // Padding to 80 bytes
};

struct FireParticle
{
    float2 Position;          // World position
    float2 Velocity;          // Movement vector
    float Lifetime;           // Current life
    float MaxLifetime;        // Total lifetime
    float Size;               // Particle size
    float Temperature;        // Heat (affects color)
    float4 Color;             // Base color
    float Rotation;           // Rotation angle
    float RotationSpeed;      // Angular velocity
    float ParticleType;       // 0=Fire, 1=Smoke, 2=Ember
    float Brightness;         // Intensity multiplier
};

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
    float4 Color : COLOR0;
    float Size : TEXCOORD1;
    float ParticleType : TEXCOORD2;
    float Temperature : TEXCOORD3;
    float Brightness : TEXCOORD4;
};

// Vertex Shader - Converts point particles to screen quads
VSOutput VSMain(FireParticle input, uint vertexId : SV_VertexID)
{
    VSOutput output;

    // Calculate life factor (0 = dead, 1 = just born)
    float lifeFactor = saturate(input.Lifetime / input.MaxLifetime);

    // Convert world position to NDC
    float2 ndcPos = (input.Position / ViewportSize) * 2.0 - 1.0;
    ndcPos.y = -ndcPos.y; // Flip Y for screen space

    // Expand point to quad (geometry shader emulation)
    // We'll use point sprites and let pixel shader handle the shape
    output.Position = float4(ndcPos, 0.0, 1.0);
    output.TexCoord = float2(0.5, 0.5); // Center of particle
    output.Color = input.Color;
    output.Color.a *= lifeFactor; // Fade out over lifetime
    output.Size = input.Size;
    output.ParticleType = input.ParticleType;
    output.Temperature = input.Temperature;
    output.Brightness = input.Brightness;

    return output;
}

// Hash function for noise
float hash(float2 p)
{
    float h = dot(p, float2(127.1, 311.7));
    return frac(sin(h) * 43758.5453123);
}

// 2D noise function
float noise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    f = f * f * (3.0 - 2.0 * f); // Smoothstep

    float a = hash(i);
    float b = hash(i + float2(1.0, 0.0));
    float c = hash(i + float2(0.0, 1.0));
    float d = hash(i + float2(1.0, 1.0));

    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}

// Fractal Brownian Motion for realistic fire turbulence
float fbm(float2 p)
{
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;

    for (int i = 0; i < 4; i++)
    {
        value += amplitude * noise(p * frequency);
        frequency *= 2.0;
        amplitude *= 0.5;
    }

    return value;
}

// Temperature to color mapping (blackbody radiation)
float3 temperatureToColor(float temp, float saturation)
{
    // Fire color gradient: dark red -> red -> orange -> yellow -> white
    float3 color;

    if (temp < 0.25)
    {
        // Dark red to red
        float t = temp / 0.25;
        color = lerp(float3(0.2, 0.0, 0.0), float3(1.0, 0.1, 0.0), t);
    }
    else if (temp < 0.5)
    {
        // Red to orange
        float t = (temp - 0.25) / 0.25;
        color = lerp(float3(1.0, 0.1, 0.0), float3(1.0, 0.5, 0.0), t);
    }
    else if (temp < 0.75)
    {
        // Orange to yellow
        float t = (temp - 0.5) / 0.25;
        color = lerp(float3(1.0, 0.5, 0.0), float3(1.0, 0.9, 0.2), t);
    }
    else
    {
        // Yellow to white hot
        float t = (temp - 0.75) / 0.25;
        color = lerp(float3(1.0, 0.9, 0.2), float3(1.0, 1.0, 1.0), t);
    }

    // Apply saturation
    float gray = dot(color, float3(0.299, 0.587, 0.114));
    color = lerp(float3(gray, gray, gray), color, saturation);

    return color;
}

// Pixel Shader - Renders particle with fire effects
float4 PSMain(VSOutput input) : SV_TARGET
{
    // Render particles as point sprites
    // For proper quads, we'd need geometry shader or instancing
    // Simplified version using screen-space distance

    float2 pixelPos = input.Position.xy;
    float2 particleCenter = input.Position.xy;
    float dist = length(pixelPos - particleCenter);

    // Create soft circular particle
    float radius = input.Size;
    float softEdge = 0.5;
    float alpha = 1.0 - smoothstep(radius - softEdge, radius + softEdge, dist);

    float4 finalColor = input.Color;
    float particleType = input.ParticleType;

    // Fire particle (type 0)
    if (particleType < 0.5)
    {
        // Apply temperature-based color
        float3 fireColor = temperatureToColor(input.Temperature, ColorSaturation);

        // Add flickering using noise
        float flicker = fbm(input.Position * 0.05 + Time * FlickerSpeed);
        flicker = 0.7 + flicker * 0.3;

        // Apply fire style modulation
        if (FireStyle > 0.5 && FireStyle < 1.5) // Torch
        {
            // Torch: more vertical, less turbulent
            flicker *= 0.9;
            fireColor *= 1.1;
        }
        else if (FireStyle > 1.5) // Inferno
        {
            // Inferno: intense, bright, chaotic
            flicker *= 1.3;
            fireColor *= 1.3;
            fireColor = saturate(fireColor);
        }

        finalColor.rgb = fireColor * flicker * input.Brightness * GlowIntensity;

        // Add core glow (brighter center)
        float coreFactor = 1.0 - dist / radius;
        coreFactor = pow(coreFactor, 2.0);
        finalColor.rgb += fireColor * coreFactor * 0.5;
    }
    // Smoke particle (type 1)
    else if (particleType > 0.5 && particleType < 1.5)
    {
        // Smoke is darker, less saturated
        float3 smokeColor = float3(0.2, 0.2, 0.25);

        // Add turbulence to smoke
        float smokeTurb = fbm(input.Position * 0.03 + Time * 2.0);
        smokeColor *= 0.5 + smokeTurb * 0.5;

        finalColor.rgb = smokeColor * input.Brightness;
        finalColor.a *= 0.5; // Smoke is more transparent
    }
    // Ember particle (type 2)
    else
    {
        // Embers are small, bright, orange-red
        float3 emberColor = float3(1.0, 0.4, 0.1);

        // Embers flicker rapidly
        float emberFlicker = noise(input.Position * 0.1 + Time * 20.0);
        emberFlicker = 0.5 + emberFlicker * 0.5;

        finalColor.rgb = emberColor * emberFlicker * input.Brightness * 2.0;

        // Make embers really bright for HDR
        finalColor.rgb *= HdrMultiplier;
    }

    // Apply alpha
    finalColor.a *= alpha * input.Color.a;

    // Apply HDR multiplier to fire
    if (particleType < 0.5)
    {
        finalColor.rgb *= HdrMultiplier;
    }

    // Clamp alpha to prevent over-blending
    finalColor.a = saturate(finalColor.a);

    return finalColor;
}
