using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The colours that mean something in the games themselves: a status condition's and a type's.
/// The interface's own colours are <c>ModernUi</c>'s tokens.
/// </summary>
public static class Palette
{
    // Status colors
    public static readonly Color StatusPoison = new(160, 64, 160, 255);
    public static readonly Color StatusBurn = new(232, 80, 40, 255);
    public static readonly Color StatusParalyze = new(240, 192, 40, 255);
    public static readonly Color StatusSleep = new(144, 152, 168, 255);
    public static readonly Color StatusFreeze = new(88, 200, 232, 255);
    public static readonly Color StatusFaint = new(160, 40, 40, 255);

    // Pokemon Type Colors
    public static Color GetTypeColor(string typeName)
    {
        return typeName.ToUpperInvariant() switch
        {
            "NORMAL" => new Color(168, 168, 120, 255),
            "FIRE" => new Color(240, 128, 48, 255),
            "WATER" => new Color(104, 144, 240, 255),
            "GRASS" => new Color(120, 200, 80, 255),
            "ELECTRIC" => new Color(248, 208, 48, 255),
            "ICE" => new Color(152, 216, 216, 255),
            "FIGHTING" => new Color(192, 48, 40, 255),
            "POISON" => new Color(160, 64, 160, 255),
            "GROUND" => new Color(224, 192, 104, 255),
            "FLYING" => new Color(168, 144, 240, 255),
            "PSYCHIC" => new Color(248, 88, 136, 255),
            "BUG" => new Color(168, 184, 32, 255),
            "ROCK" => new Color(184, 160, 56, 255),
            "GHOST" => new Color(112, 88, 152, 255),
            "DRAGON" => new Color(112, 56, 248, 255),
            "STEEL" => new Color(184, 184, 208, 255),
            "DARK" => new Color(112, 88, 72, 255),
            "FAIRY" => new Color(238, 153, 172, 255),
            _ => new Color(160, 160, 160, 255)
        };
    }
}
