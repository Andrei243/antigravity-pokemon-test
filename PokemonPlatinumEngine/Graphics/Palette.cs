using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Authentic color palettes inspired by Pokemon Platinum (Sinnoh Gen 4 DS).
/// </summary>
public static class Palette
{
    // Platinum UI colors
    public static readonly Color UiBackground = new(240, 240, 248, 255);
    public static readonly Color UiDarkBorder = new(40, 48, 72, 255);
    public static readonly Color UiLightBorder = new(136, 152, 192, 255);
    public static readonly Color UiPanelBg = new(224, 232, 248, 240);
    public static readonly Color UiAccent = new(208, 48, 48, 255);       // Platinum Red
    public static readonly Color UiAccentSecondary = new(56, 120, 216, 255); // Platinum Blue
    public static readonly Color TextDark = new(48, 48, 56, 255);
    public static readonly Color TextLight = new(248, 248, 248, 255);
    public static readonly Color TextShadow = new(160, 168, 184, 255);

    // HP Bar colors
    public static readonly Color HpGreen = new(48, 208, 88, 255);
    public static readonly Color HpYellow = new(248, 200, 48, 255);
    public static readonly Color HpRed = new(240, 64, 56, 255);
    public static readonly Color HpBg = new(72, 80, 96, 255);
    public static readonly Color ExpBlue = new(56, 168, 248, 255);

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

    // Overworld Sinnoh Colors
    public static readonly Color GrassGreen = new(92, 180, 74, 255);
    public static readonly Color GrassDark = new(64, 144, 52, 255);
    public static readonly Color TallGrass = new(52, 160, 68, 255);
    public static readonly Color TallGrassTip = new(136, 216, 96, 255);
    public static readonly Color DirtPath = new(216, 188, 128, 255);
    public static readonly Color DirtPathDark = new(184, 152, 96, 255);
    public static readonly Color WaterBlue = new(72, 144, 224, 255);
    public static readonly Color WaterDeep = new(48, 112, 192, 255);
    public static readonly Color RoofRed = new(208, 56, 56, 255);
    public static readonly Color RoofBlue = new(56, 112, 208, 255);
    public static readonly Color RoofGreen = new(64, 168, 80, 255);
    public static readonly Color WoodBrown = new(144, 104, 64, 255);
    public static readonly Color WallBeige = new(236, 228, 208, 255);
}
