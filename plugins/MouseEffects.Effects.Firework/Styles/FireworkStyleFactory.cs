namespace MouseEffects.Effects.Firework.Styles;

public static class FireworkStyleFactory
{
    private static readonly Dictionary<string, Func<IFireworkStyle>> _creators = new()
    {
        ["Classic Burst"] = () => new ClassicBurstStyle(),
        ["Spinner"] = () => new SpinnerStyle(),
        ["Willow"] = () => new WillowStyle(),
        ["Crackling"] = () => new CracklingStyle(),
        ["Chrysanthemum"] = () => new ChrysanthemumStyle(),
        ["Brocade"] = () => new BrocadeStyle(),
        ["Comet"] = () => new CometStyle(),
        ["Crossette"] = () => new CrossetteStyle(),
        ["Palm"] = () => new PalmStyle(),
        ["Peony"] = () => new PeonyStyle(),
        ["Pearls"] = () => new PearlsStyle(),
        ["Fish"] = () => new FishStyle(),
        ["Green Bees"] = () => new GreenBeesStyle(),
        ["Pistil"] = () => new PistilStyle(),
        ["Stars"] = () => new StarsStyle(),
        ["Tail"] = () => new TailStyle(),
        ["Strobe"] = () => new StrobeStyle(),
        ["Glitter"] = () => new GlitterStyle(),
        ["Random"] = () => new RandomStyle()
    };

    public static IFireworkStyle Create(string name)
        => _creators.TryGetValue(name, out var creator) ? creator() : new ClassicBurstStyle();

    public static IEnumerable<string> AvailableStyles => _creators.Keys;

    public static IReadOnlyList<string> StyleNames { get; } = _creators.Keys.ToList().AsReadOnly();
}
