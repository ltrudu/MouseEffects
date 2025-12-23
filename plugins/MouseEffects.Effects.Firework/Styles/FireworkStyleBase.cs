using System.Numerics;
using MouseEffects.Effects.Firework.Core;

namespace MouseEffects.Effects.Firework.Styles;

public abstract class FireworkStyleBase : IFireworkStyle
{
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract int StyleId { get; }

    protected Dictionary<string, object> Parameters { get; } = new();

    // Template method - defines the algorithm structure
    public virtual void SpawnExplosion(FireworkContext ctx, Vector2 position, float force, Vector4 baseColor, int particleCount, bool isSecondary)
    {
        int count = GetParticleCount(particleCount, force, isSecondary);
        float spreadRad = GetSpreadAngle(ctx) * MathF.PI / 180f;
        float startAngle = ctx.GetRandomFloat() * MathF.PI * 2f;

        for (int i = 0; i < count; i++)
        {
            float angle = ctx.SpreadAngle >= 360f
                ? startAngle + (float)i / count * MathF.PI * 2f
                : startAngle - spreadRad / 2f + ctx.GetRandomFloat() * spreadRad;

            float particleForce = force * (0.5f + ctx.GetRandomFloat() * 0.5f);
            Vector2 velocity = new(MathF.Cos(angle) * particleForce, MathF.Sin(angle) * particleForce);

            Vector4 color = GetParticleColor(ctx, baseColor, isSecondary, i, count);
            float size = GetParticleSize(ctx, isSecondary);
            float lifespan = GetParticleLifespan(ctx, isSecondary);

            var particle = new FireworkParticle
            {
                Position = position,
                Velocity = velocity,
                Color = color,
                Size = size,
                Life = lifespan,
                MaxLife = lifespan,
                CanExplode = !isSecondary && ctx.EnableSecondaryExplosion,
                HasExploded = false,
                StyleId = StyleId
            };

            CustomizeParticle(ref particle, ctx, i, count, isSecondary);
            ctx.Pool.Spawn(particle);
        }

        OnExplosionComplete(ctx, position, isSecondary);
    }

    // Hooks for subclasses to override
    protected virtual int GetParticleCount(int baseCount, float force, bool isSecondary)
        => isSecondary ? baseCount / 3 : baseCount;

    protected virtual float GetSpreadAngle(FireworkContext ctx) => ctx.SpreadAngle;

    protected virtual Vector4 GetParticleColor(FireworkContext ctx, Vector4 baseColor, bool isSecondary, int index, int total)
    {
        Vector4 color = baseColor;
        if (ctx.UseRandomColors && !isSecondary)
        {
            float mixFactor = ctx.GetRandomFloat() * 0.5f;
            color = Vector4.Lerp(baseColor, ctx.GetSecondaryColor(), mixFactor);
        }

        // Add slight variation
        color.X = MathF.Max(0f, MathF.Min(1f, color.X + (ctx.GetRandomFloat() - 0.5f) * 0.2f));
        color.Y = MathF.Max(0f, MathF.Min(1f, color.Y + (ctx.GetRandomFloat() - 0.5f) * 0.2f));
        color.Z = MathF.Max(0f, MathF.Min(1f, color.Z + (ctx.GetRandomFloat() - 0.5f) * 0.2f));

        return color;
    }

    protected virtual float GetParticleSize(FireworkContext ctx, bool isSecondary)
    {
        float size = ctx.GetRandomSize();
        return isSecondary ? size * 0.6f : size;
    }

    protected virtual float GetParticleLifespan(FireworkContext ctx, bool isSecondary)
    {
        float lifespan = ctx.GetRandomLifespan();
        return isSecondary ? lifespan * 0.5f : lifespan;
    }

    protected virtual void CustomizeParticle(ref FireworkParticle p, FireworkContext ctx, int index, int total, bool isSecondary) { }

    protected virtual void OnExplosionComplete(FireworkContext ctx, Vector2 position, bool isSecondary) { }

    // Default implementations
    public virtual void UpdateParticle(ref FireworkParticle particle, float dt, float time) { }

    public virtual void FillStyleData(ref ParticleGPU gpu, in FireworkParticle particle) { }

    public abstract StyleDefaults GetDefaults();

    public virtual IEnumerable<StyleParameter> GetParameters() => Enumerable.Empty<StyleParameter>();

    public virtual void SetParameter(string key, object value)
    {
        Parameters[key] = value;
    }

    public virtual object? GetParameter(string key)
    {
        return Parameters.TryGetValue(key, out var value) ? value : null;
    }

    // Trail particle spawning - default: no trails
    public virtual bool HasTrailParticles => false;

    public virtual bool ShouldSpawnTrail(ref FireworkParticle particle, float dt) => false;

    public virtual FireworkParticle CreateTrailParticle(in FireworkParticle parent, FireworkContext ctx)
    {
        // Default trail particle - should be overridden by styles with trails
        return new FireworkParticle
        {
            Position = parent.Position,
            Velocity = parent.Velocity * 0.1f,
            Color = parent.Color * 0.5f,
            Size = parent.Size * 0.3f,
            Life = 0.2f,
            MaxLife = 0.2f,
            CanExplode = false,
            HasExploded = true,
            StyleId = StyleId
        };
    }
}
