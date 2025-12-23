using System.Numerics;
using MouseEffects.Effects.Firework.Styles;

namespace MouseEffects.Effects.Firework.Core;

public class ExplosionBuilder
{
    private Vector2 _position;
    private float _force = 300f;
    private Vector4? _color;
    private bool _useRainbow;
    private bool _isSecondary;
    private IFireworkStyle? _overrideStyle;
    private int _particleCount = 50;

    public ExplosionBuilder At(Vector2 position) { _position = position; return this; }
    public ExplosionBuilder WithForce(float force) { _force = force; return this; }
    public ExplosionBuilder WithColor(Vector4 color) { _color = color; return this; }
    public ExplosionBuilder WithParticleCount(int count) { _particleCount = count; return this; }
    public ExplosionBuilder AsRainbow() { _useRainbow = true; return this; }
    public ExplosionBuilder AsSecondary() { _isSecondary = true; return this; }
    public ExplosionBuilder UsingStyle(IFireworkStyle style) { _overrideStyle = style; return this; }

    public void Spawn(FireworkContext ctx)
    {
        var style = _overrideStyle ?? ctx.CurrentStyle;
        Vector4 color;

        if (_useRainbow)
            color = ctx.GetRainbowColor();
        else if (_color.HasValue)
            color = _color.Value;
        else
            color = ctx.GetRandomColor();

        style.SpawnExplosion(ctx, _position, _force, color, _particleCount, _isSecondary);
    }

    public static ExplosionBuilder Create() => new();
}
