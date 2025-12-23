namespace MouseEffects.Effects.Firework.Styles;

public abstract class StyleParameter
{
    public required string Key { get; init; }
    public required string DisplayName { get; init; }
    public string? Description { get; init; }
}

public class FloatStyleParameter : StyleParameter
{
    public float MinValue { get; init; }
    public float MaxValue { get; init; }
    public float DefaultValue { get; init; }
    public float Step { get; init; } = 0.1f;
}

public class IntStyleParameter : StyleParameter
{
    public int MinValue { get; init; }
    public int MaxValue { get; init; }
    public int DefaultValue { get; init; }
}

public class BoolStyleParameter : StyleParameter
{
    public bool DefaultValue { get; init; }
}
