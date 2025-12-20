// Fire Trail Shader - GPU-accelerated particle rendering with realistic fire effects
// Uses fullscreen quad approach with structured buffer for particles

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
    int ParticleCount;        // Active particle count
    float3 Padding;           // Padding to align
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
};

StructuredBuffer<FireParticle> Particles : register(t0);

// Vertex Shader - Fullscreen quad
VSOutput VSMain(uint vertexId : SV_VertexID)
{
    VSOutput output;

    // Generate fullscreen triangle strip: 0,1,2,3 -> positions
    float2 uv = float2((vertexId << 1) & 2, vertexId & 2);
    output.Position = float4(uv * 2.0 - 1.0, 0.0, 1.0);
    output.Position.y = -output.Position.y; // Flip Y for DirectX
    output.TexCoord = uv;

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

// Temperature to color mapping - clamped to red/orange/yellow (no white)
float3 temperatureToColor(float temp, float saturation)
{
    // Fire color gradient: dark red -> red -> orange -> bright yellow (NO white)
    float3 color;

    if (temp < 0.33)
    {
        // Dark red to red
        float t = temp / 0.33;
        color = lerp(float3(0.3, 0.0, 0.0), float3(1.0, 0.15, 0.0), t);
    }
    else if (temp < 0.66)
    {
        // Red to orange
        float t = (temp - 0.33) / 0.33;
        color = lerp(float3(1.0, 0.15, 0.0), float3(1.0, 0.5, 0.0), t);
    }
    else
    {
        // Orange to bright yellow (cap here, no white)
        float t = (temp - 0.66) / 0.34;
        color = lerp(float3(1.0, 0.5, 0.0), float3(1.0, 0.85, 0.1), t);
    }

    // Apply saturation (bias toward keeping fire colors)
    float gray = dot(color, float3(0.299, 0.587, 0.114));
    color = lerp(float3(gray, gray, gray), color, saturation);

    return color;
}

// Pixel Shader - Renders all particles
float4 PSMain(VSOutput input) : SV_TARGET
{
    float2 pixelPos = input.TexCoord * ViewportSize;
    float4 finalColor = float4(0, 0, 0, 0);

    // Loop through all active particles
    for (int i = 0; i < ParticleCount; i++)
    {
        FireParticle particle = Particles[i];

        // Skip dead particles
        if (particle.Lifetime <= 0)
            continue;

        // Distance from pixel to particle center
        float2 toPixel = pixelPos - particle.Position;
        float dist = length(toPixel);

        // Skip if too far from particle
        float maxRadius = particle.Size * 2.0;
        if (dist > maxRadius)
            continue;

        // Calculate life factor (0 = dead, 1 = just born)
        float lifeFactor = saturate(particle.Lifetime / particle.MaxLifetime);

        // Create soft circular particle
        float radius = particle.Size;
        float softEdge = radius * 0.5;
        float alpha = 1.0 - smoothstep(radius - softEdge, radius + softEdge, dist);

        float4 particleColor = particle.Color;
        float particleType = particle.ParticleType;

        // Fire particle (type 0)
        if (particleType < 0.5)
        {
            // Apply temperature-based color
            float3 fireColor = temperatureToColor(particle.Temperature, ColorSaturation);

            // Add flickering using noise
            float flicker = fbm(particle.Position * 0.05 + Time * FlickerSpeed);
            flicker = 0.7 + flicker * 0.3;

            // Apply fire style modulation
            if (FireStyle > 0.5 && FireStyle < 1.5) // Torch
            {
                flicker *= 0.9;
                fireColor *= 1.1;
            }
            else if (FireStyle > 1.5) // Inferno
            {
                flicker *= 1.3;
                fireColor *= 1.3;
                fireColor = saturate(fireColor);
            }

            particleColor.rgb = fireColor * flicker * particle.Brightness * GlowIntensity;

            // Add core glow (brighter center)
            float coreFactor = 1.0 - dist / radius;
            coreFactor = saturate(coreFactor);
            coreFactor = pow(coreFactor, 2.0);
            particleColor.rgb += fireColor * coreFactor * 0.5;

            // Apply HDR multiplier to fire
            particleColor.rgb *= HdrMultiplier;
        }
        // Smoke particle (type 1)
        else if (particleType > 0.5 && particleType < 1.5)
        {
            // Smoke is darker, less saturated
            float3 smokeColor = float3(0.2, 0.2, 0.25);

            // Add turbulence to smoke
            float smokeTurb = fbm(particle.Position * 0.03 + Time * 2.0);
            smokeColor *= 0.5 + smokeTurb * 0.5;

            particleColor.rgb = smokeColor * particle.Brightness;
            alpha *= 0.5; // Smoke is more transparent
        }
        // Ember particle (type 2)
        else
        {
            // Embers are small, bright, orange-red
            float3 emberColor = float3(1.0, 0.4, 0.1);

            // Embers flicker rapidly
            float emberFlicker = noise(particle.Position * 0.1 + Time * 20.0);
            emberFlicker = 0.5 + emberFlicker * 0.5;

            particleColor.rgb = emberColor * emberFlicker * particle.Brightness * 2.0;

            // Make embers really bright for HDR
            particleColor.rgb *= HdrMultiplier;
        }

        // Apply alpha with life factor fade
        float finalAlpha = alpha * lifeFactor * particle.Color.a;

        // Additive blend
        finalColor.rgb += particleColor.rgb * finalAlpha;
        finalColor.a = saturate(finalColor.a + finalAlpha * 0.5);
    }

    // Clamp final color to fire spectrum (red/orange/yellow, avoid white)
    // Keep red channel high, limit green to create orange/yellow, minimize blue
    if (finalColor.r > 0.01)
    {
        // Preserve the fire color ratio - red should dominate
        float maxIntensity = max(finalColor.r, max(finalColor.g, finalColor.b));
        if (maxIntensity > 1.0)
        {
            // Normalize but bias toward fire colors
            float3 normalized = finalColor.rgb / maxIntensity;
            // Clamp green to not exceed red (keeps orange/yellow, prevents white)
            normalized.g = min(normalized.g, normalized.r * 0.85);
            // Keep blue very low for fire
            normalized.b = min(normalized.b, normalized.r * 0.15);
            // Scale back up with controlled intensity
            finalColor.rgb = normalized * min(maxIntensity, 3.0);
        }
    }

    return finalColor;
}
