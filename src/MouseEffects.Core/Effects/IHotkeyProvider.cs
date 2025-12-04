namespace MouseEffects.Core.Effects;

/// <summary>
/// Modifier keys for hotkey combinations.
/// </summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Ctrl = 1,
    Shift = 2,
    Alt = 4
}

/// <summary>
/// Virtual key codes for hotkey definitions.
/// </summary>
public enum HotkeyKey
{
    None = 0,
    A = 0x41, B = 0x42, C = 0x43, D = 0x44, E = 0x45, F = 0x46, G = 0x47, H = 0x48,
    I = 0x49, J = 0x4A, K = 0x4B, L = 0x4C, M = 0x4D, N = 0x4E, O = 0x4F, P = 0x50,
    Q = 0x51, R = 0x52, S = 0x53, T = 0x54, U = 0x55, V = 0x56, W = 0x57, X = 0x58,
    Y = 0x59, Z = 0x5A,
    D0 = 0x30, D1 = 0x31, D2 = 0x32, D3 = 0x33, D4 = 0x34,
    D5 = 0x35, D6 = 0x36, D7 = 0x37, D8 = 0x38, D9 = 0x39,
    F1 = 0x70, F2 = 0x71, F3 = 0x72, F4 = 0x73, F5 = 0x74, F6 = 0x75,
    F7 = 0x76, F8 = 0x77, F9 = 0x78, F10 = 0x79, F11 = 0x7A, F12 = 0x7B,
    Space = 0x20, Enter = 0x0D, Escape = 0x1B, Tab = 0x09,
    Left = 0x25, Up = 0x26, Right = 0x27, Down = 0x28
}

/// <summary>
/// Defines a hotkey that an effect wants to register.
/// </summary>
public sealed class HotkeyDefinition
{
    /// <summary>Unique identifier for this hotkey within the effect.</summary>
    public required string Id { get; init; }

    /// <summary>Display name shown in UI.</summary>
    public required string DisplayName { get; init; }

    /// <summary>Modifier keys (Ctrl, Shift, Alt).</summary>
    public HotkeyModifiers Modifiers { get; init; }

    /// <summary>The main key.</summary>
    public HotkeyKey Key { get; init; }

    /// <summary>Whether this hotkey is currently enabled.</summary>
    public bool IsEnabled { get; init; }

    /// <summary>Action to execute when the hotkey is triggered.</summary>
    public required Action Callback { get; init; }

    /// <summary>Gets a formatted string representation of the hotkey combination.</summary>
    public string GetDisplayString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Ctrl)) parts.Add("Ctrl");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
        parts.Add(Key.ToString());
        return string.Join("+", parts);
    }
}

/// <summary>
/// Interface for effects that want to register global hotkeys.
/// </summary>
public interface IHotkeyProvider
{
    /// <summary>
    /// Gets the hotkeys this effect wants to register.
    /// Called periodically to check for enabled hotkeys.
    /// </summary>
    /// <returns>Collection of hotkey definitions.</returns>
    IEnumerable<HotkeyDefinition> GetHotkeys();
}
