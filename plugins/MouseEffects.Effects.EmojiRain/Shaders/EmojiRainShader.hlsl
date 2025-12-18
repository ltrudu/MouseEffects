// Emoji Rain Shader - Procedurally drawn emoji faces with SDF

cbuffer FrameConstants : register(b0)
{
    float2 ViewportSize;
    float Time;
    float HdrMultiplier;
    float4 Padding;
}

struct EmojiInstance
{
    float2 Position;
    float2 Velocity;
    float4 Color;
    float Size;
    float Lifetime;
    float MaxLifetime;
    float RotationAngle;
    float RotationSpeed;
    int EmojiType;
    float Padding1;
    float Padding2;
};

StructuredBuffer<EmojiInstance> Emojis : register(t0);

struct VSOutput
{
    float4 Position : SV_POSITION;
    float2 UV : TEXCOORD0;
    float4 Color : COLOR0;
    float Alpha : TEXCOORD1;
    int EmojiType : TEXCOORD2;
};

// Emoji type constants
static const int EMOJI_HAPPY = 0;
static const int EMOJI_SAD = 1;
static const int EMOJI_WINK = 2;
static const int EMOJI_HEART_EYES = 3;
static const int EMOJI_STAR_EYES = 4;
static const int EMOJI_SURPRISED = 5;

// Vertex shader - Generate quad per emoji instance
VSOutput VSMain(uint vertexId : SV_VertexID, uint instanceId : SV_InstanceID)
{
    VSOutput output;
    EmojiInstance emoji = Emojis[instanceId];

    // Skip dead emojis
    if (emoji.Lifetime <= 0)
    {
        output.Position = float4(0, 0, 0, 0);
        output.UV = float2(0, 0);
        output.Color = float4(0, 0, 0, 0);
        output.Alpha = 0;
        output.EmojiType = 0;
        return output;
    }

    // Calculate alpha based on lifetime (fade in and out)
    float lifeFraction = emoji.Lifetime / emoji.MaxLifetime;
    float fadeIn = saturate((1.0 - lifeFraction) * 4.0); // Quick fade in
    float fadeOut = saturate(lifeFraction * 1.5); // Slower fade out
    float alpha = min(fadeIn, fadeOut);

    // Generate quad vertices (two triangles)
    float2 quadUV;
    if (vertexId == 0) quadUV = float2(-1, -1);
    else if (vertexId == 1) quadUV = float2(1, -1);
    else if (vertexId == 2) quadUV = float2(-1, 1);
    else if (vertexId == 3) quadUV = float2(-1, 1);
    else if (vertexId == 4) quadUV = float2(1, -1);
    else quadUV = float2(1, 1);

    // Apply rotation
    float c = cos(emoji.RotationAngle);
    float s = sin(emoji.RotationAngle);
    float2x2 rotation = float2x2(c, -s, s, c);
    float2 rotatedUV = mul(rotation, quadUV);

    // Scale by emoji size
    float2 offset = rotatedUV * emoji.Size;

    // Position in screen space
    float2 screenPos = emoji.Position + offset;

    // Convert to NDC
    float2 ndc = (screenPos / ViewportSize) * 2.0 - 1.0;
    ndc.y = -ndc.y; // Flip Y for DirectX

    output.Position = float4(ndc, 0, 1);
    output.UV = quadUV; // Keep unrotated UV for SDF
    output.Color = emoji.Color;
    output.Alpha = alpha;
    output.EmojiType = emoji.EmojiType;

    return output;
}

// SDF Helper Functions
float CircleSDF(float2 p, float r)
{
    return length(p) - r;
}

float RectangleSDF(float2 p, float2 size)
{
    float2 d = abs(p) - size;
    return length(max(d, 0)) + min(max(d.x, d.y), 0);
}

// Heart shape SDF for heart eyes
float HeartSDF(float2 p, float size)
{
    p.y -= size * 0.3;
    p.x = abs(p.x);
    float circle = length(p - float2(size * 0.25, size * 0.25)) - size * 0.35;
    float2 q = p - float2(0, -size * 0.5);
    float tri = max(q.x * 0.866 + q.y * 0.5, -q.y) - size * 0.5;
    return min(circle, tri);
}

// Star shape SDF for star eyes
float StarSDF(float2 p, float size)
{
    static const float PI = 3.14159265359;
    float angle = atan2(p.y, p.x);
    float dist = length(p);

    // 5-pointed star
    float a = fmod(angle + PI, 2.0 * PI / 5.0) - PI / 5.0;
    float s = size * (0.5 + 0.3 * cos(5.0 * a));

    return dist - s;
}

// Emoji Face SDFs - Returns distance field for each emoji type
float EmojiHappyFace(float2 p, float size)
{
    // Eyes - two small circles
    float leftEye = CircleSDF(p - float2(-size * 0.3, size * 0.2), size * 0.12);
    float rightEye = CircleSDF(p - float2(size * 0.3, size * 0.2), size * 0.12);
    float eyes = min(leftEye, rightEye);

    // Smile - arc shape (circle bottom half)
    float2 smileP = p - float2(0, -size * 0.1);
    float smileCircle = abs(length(smileP) - size * 0.4) - 0.03;
    float smile = max(smileCircle, -smileP.y); // Only bottom half

    return min(eyes, smile);
}

float EmojiSadFace(float2 p, float size)
{
    // Eyes - two small circles
    float leftEye = CircleSDF(p - float2(-size * 0.3, size * 0.2), size * 0.12);
    float rightEye = CircleSDF(p - float2(size * 0.3, size * 0.2), size * 0.12);
    float eyes = min(leftEye, rightEye);

    // Frown - arc shape (circle top half, inverted)
    float2 frownP = p - float2(0, -size * 0.5);
    float frownCircle = abs(length(frownP) - size * 0.4) - 0.03;
    float frown = max(frownCircle, frownP.y); // Only top half

    return min(eyes, frown);
}

float EmojiWinkFace(float2 p, float size)
{
    // Left eye - circle
    float leftEye = CircleSDF(p - float2(-size * 0.3, size * 0.2), size * 0.12);

    // Right eye - winking (horizontal line)
    float2 rightEyeP = p - float2(size * 0.3, size * 0.2);
    float rightEye = RectangleSDF(rightEyeP, float2(size * 0.15, size * 0.03));

    float eyes = min(leftEye, rightEye);

    // Smile - arc shape
    float2 smileP = p - float2(0, -size * 0.1);
    float smileCircle = abs(length(smileP) - size * 0.4) - 0.03;
    float smile = max(smileCircle, -smileP.y);

    return min(eyes, smile);
}

float EmojiHeartEyesFace(float2 p, float size)
{
    // Left heart eye
    float2 leftHeartP = (p - float2(-size * 0.3, size * 0.2)) / (size * 0.25);
    float leftHeart = HeartSDF(leftHeartP, 0.8);

    // Right heart eye
    float2 rightHeartP = (p - float2(size * 0.3, size * 0.2)) / (size * 0.25);
    float rightHeart = HeartSDF(rightHeartP, 0.8);

    float hearts = min(leftHeart * size * 0.25, rightHeart * size * 0.25);

    // Smile - arc shape
    float2 smileP = p - float2(0, -size * 0.1);
    float smileCircle = abs(length(smileP) - size * 0.4) - 0.03;
    float smile = max(smileCircle, -smileP.y);

    return min(hearts, smile);
}

float EmojiStarEyesFace(float2 p, float size)
{
    // Left star eye
    float2 leftStarP = p - float2(-size * 0.3, size * 0.2);
    float leftStar = StarSDF(leftStarP, size * 0.2);

    // Right star eye
    float2 rightStarP = p - float2(size * 0.3, size * 0.2);
    float rightStar = StarSDF(rightStarP, size * 0.2);

    float stars = min(leftStar, rightStar);

    // Smile - arc shape
    float2 smileP = p - float2(0, -size * 0.1);
    float smileCircle = abs(length(smileP) - size * 0.4) - 0.03;
    float smile = max(smileCircle, -smileP.y);

    return min(stars, smile);
}

float EmojiSurprisedFace(float2 p, float size)
{
    // Eyes - two small circles
    float leftEye = CircleSDF(p - float2(-size * 0.3, size * 0.2), size * 0.15);
    float rightEye = CircleSDF(p - float2(size * 0.3, size * 0.2), size * 0.15);
    float eyes = min(leftEye, rightEye);

    // Mouth - open circle
    float mouth = CircleSDF(p - float2(0, -size * 0.2), size * 0.2);

    return min(eyes, mouth);
}

// Pixel shader - Render emoji face
float4 PSMain(VSOutput input) : SV_TARGET
{
    if (input.Alpha <= 0.001)
        discard;

    float size = 0.5; // Normalized size for UV space

    // Face circle SDF (yellow background)
    float faceDist = CircleSDF(input.UV, size);

    // Face outline (darker yellow)
    float faceOutline = 1.0 - smoothstep(-0.05, -0.02, faceDist);
    float faceFill = 1.0 - smoothstep(-0.05, 0.0, faceDist);

    // Get features distance based on emoji type
    float featureDist = 1.0;
    if (input.EmojiType == EMOJI_HAPPY)
        featureDist = EmojiHappyFace(input.UV, size);
    else if (input.EmojiType == EMOJI_SAD)
        featureDist = EmojiSadFace(input.UV, size);
    else if (input.EmojiType == EMOJI_WINK)
        featureDist = EmojiWinkFace(input.UV, size);
    else if (input.EmojiType == EMOJI_HEART_EYES)
        featureDist = EmojiHeartEyesFace(input.UV, size);
    else if (input.EmojiType == EMOJI_STAR_EYES)
        featureDist = EmojiStarEyesFace(input.UV, size);
    else if (input.EmojiType == EMOJI_SURPRISED)
        featureDist = EmojiSurprisedFace(input.UV, size);

    // Features (black eyes/mouth)
    float features = 1.0 - smoothstep(-0.02, 0.02, featureDist);

    // Combine: yellow face with black features
    float4 color = input.Color;

    // Apply face fill
    color.rgb *= faceFill;

    // Apply outline (darker)
    color.rgb *= lerp(0.7, 1.0, 1.0 - faceOutline);

    // Apply features (black)
    float3 featureColor = float3(0, 0, 0);

    // Special colors for heart and star eyes
    if (input.EmojiType == EMOJI_HEART_EYES)
        featureColor = float3(1.0, 0.0, 0.3); // Pink hearts
    else if (input.EmojiType == EMOJI_STAR_EYES)
        featureColor = float3(1.0, 0.9, 0.0); // Yellow stars

    color.rgb = lerp(color.rgb, featureColor, features);

    // Apply alpha
    color.a = faceFill * input.Alpha;

    // Apply HDR multiplier
    color.rgb *= HdrMultiplier;

    if (color.a < 0.01)
        discard;

    return color;
}
