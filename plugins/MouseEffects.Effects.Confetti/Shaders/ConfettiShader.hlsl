// Confetti particle shader with multiple shapes (rectangles, circles, ribbons)

cbuffer FrameData : register(b0)
{
    float2 ViewportSize;
    float Time;
    float GravityStrength;
    float FlutterAmount;
    float HdrMultiplier;
    float Padding1;
    float Padding2;
    float4 Padding3;
    float4 Padding4;
};

struct ParticleInstance
{
    float2 Position;
    float2 Velocity;
    float4 Color;
    float Size;
    float Life;
    float MaxLife;
    float Rotation;
    int ShapeType;
    float Padding1;
    float Padding2;
    float Padding3;
};

StructuredBuffer<ParticleInstance> Particles : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR;
    float2 LocalPos : TEXCOORD0;
    float LifeFactor : TEXCOORD1;
    int ShapeType : TEXCOORD2;
};

static const float PI = 3.14159265359;

// Shape type constants
static const int SHAPE_RECTANGLE = 0;
static const int SHAPE_CIRCLE = 1;
static const int SHAPE_RIBBON = 2;

// Vertex shader for particle quads with rotation
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    ParticleInstance particle = Particles[instanceId];

    // Skip dead particles
    if (particle.Life <= 0)
    {
        VSOutput output;
        output.Position = float4(0, 0, -2, 1);
        output.Color = float4(0, 0, 0, 0);
        output.LocalPos = float2(0, 0);
        output.LifeFactor = 0;
        output.ShapeType = 0;
        return output;
    }

    // Generate quad vertex (2 triangles = 6 vertices)
    float2 offsets[6] = {
        float2(-1, -1), float2(1, -1), float2(-1, 1),
        float2(-1, 1), float2(1, -1), float2(1, 1)
    };

    float2 offset = offsets[vertexId];

    // Calculate life factor for fade
    float lifeFactor = particle.Life / particle.MaxLife;

    // Apply rotation
    float c = cos(particle.Rotation);
    float s = sin(particle.Rotation);
    float2x2 rotationMatrix = float2x2(c, -s, s, c);
    float2 rotatedOffset = mul(rotationMatrix, offset);

    // Size adjustment based on shape
    float2 size = float2(particle.Size, particle.Size);
    if (particle.ShapeType == SHAPE_RIBBON)
    {
        // Ribbons are thin and tall
        size = float2(particle.Size * 0.2, particle.Size * 2.0);
    }

    // Convert to normalized device coordinates
    float2 screenPos = particle.Position + rotatedOffset * size;
    float2 ndcPos = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndcPos.y = -ndcPos.y;

    VSOutput output;
    output.Position = float4(ndcPos, 0, 1);
    output.Color = particle.Color;
    output.LocalPos = offset;
    output.LifeFactor = lifeFactor;
    output.ShapeType = particle.ShapeType;
    return output;
}

// Rectangle SDF
float RectangleSDF(float2 p, float2 size)
{
    float2 d = abs(p) - size;
    return length(max(d, 0)) + min(max(d.x, d.y), 0);
}

// Circle SDF
float CircleSDF(float2 p, float r)
{
    return length(p) - r;
}

// Ribbon SDF (thin tall rectangle)
float RibbonSDF(float2 p, float length)
{
    return RectangleSDF(p, float2(length * 0.1, length));
}

// Pixel shader - renders different confetti shapes
float4 PSMain(VSOutput input) : SV_TARGET
{
    float dist = 0;

    if (input.ShapeType == SHAPE_RECTANGLE)
    {
        dist = RectangleSDF(input.LocalPos, float2(0.8, 0.8));
    }
    else if (input.ShapeType == SHAPE_CIRCLE)
    {
        dist = CircleSDF(input.LocalPos, 0.8);
    }
    else if (input.ShapeType == SHAPE_RIBBON)
    {
        dist = RibbonSDF(input.LocalPos, 0.8);
    }

    // Smooth edge
    float alpha = 1.0 - smoothstep(-0.05, 0.05, dist);

    // Fade out based on lifetime
    alpha *= input.LifeFactor;

    // Add slight brightness variation for depth
    float brightness = 0.9 + 0.2 * (input.LocalPos.x + input.LocalPos.y) * 0.5;

    float4 color = input.Color;
    color.rgb *= brightness;

    // HDR boost for bright displays
    color.rgb *= 1.0 + HdrMultiplier * 0.5;

    color.a = alpha;

    if (color.a < 0.01)
        discard;

    return color;
}
