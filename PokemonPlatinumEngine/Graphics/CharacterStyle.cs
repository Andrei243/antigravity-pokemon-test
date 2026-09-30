using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

internal enum Headwear { None, Beret, Cap, NurseCap }

internal enum HairCut { Short, Spiky, Long, Swept }

/// <summary>Outfit and colouring for one kind of character, shared by the 3D field models and the 2D trainer card art.</summary>
internal sealed class CharacterStyle
{
    public Color Skin = new(252, 218, 184, 255);
    public Color HairColor = new(72, 52, 44, 255);
    public HairCut Hair = HairCut.Short;
    public Headwear Hat = Headwear.None;
    public Color HatColor = Color.White;
    public Color HatBand = Color.White;
    public Color Top = new(80, 160, 96, 255);
    public Color Accent = new(240, 240, 240, 255);
    public Color Bottom = new(60, 64, 88, 255);
    public Color Shoes = new(72, 56, 52, 255);
    public Color Eyes = new(40, 36, 60, 255);
    public bool Skirt, Shorts, Mustache, Stripes, Coat, ShortSleeves;
    public Color? Bag;

    /// <summary>Relative size: adults stand a little taller than the kids.</summary>
    public float Height = 1f;

    public static CharacterStyle For(string npcType) => npcType.ToUpperInvariant() switch
    {
        "PLAYER" or "TRAINER" or "LUCAS" => new CharacterStyle
        {
            HairColor = new(70, 56, 78, 255),
            Hat = Headwear.Beret, HatColor = new(220, 56, 60, 255), HatBand = new(246, 246, 250, 255),
            Top = new(58, 78, 138, 255), Accent = new(236, 70, 70, 255),
            Bottom = new(48, 50, 70, 255), Shoes = new(200, 70, 60, 255),
            Bag = new(242, 196, 70, 255)
        },
        "RIVAL" => new CharacterStyle
        {
            HairColor = new(250, 212, 80, 255), Hair = HairCut.Spiky,
            Top = new(244, 132, 52, 255), Accent = new(72, 176, 104, 255), Stripes = true,
            Bottom = new(58, 70, 108, 255), Shoes = new(236, 236, 240, 255), Eyes = new(96, 70, 40, 255)
        },
        "ROWAN" => new CharacterStyle
        {
            HairColor = new(236, 236, 242, 255), Hair = HairCut.Swept, Mustache = true, Coat = true,
            Top = new(126, 92, 66, 255), Accent = new(236, 236, 242, 255),
            Bottom = new(84, 66, 56, 255), Shoes = new(52, 42, 40, 255), Height = 1.12f
        },
        "NURSE" => new CharacterStyle
        {
            HairColor = new(248, 150, 190, 255), Hair = HairCut.Long,
            Hat = Headwear.NurseCap, HatColor = Color.White, HatBand = new(232, 72, 96, 255),
            Top = new(252, 252, 255, 255), Accent = new(248, 170, 200, 255),
            Bottom = new(248, 170, 200, 255), Skirt = true, Shoes = new(248, 248, 252, 255),
            Eyes = new(70, 120, 200, 255), Height = 1.06f
        },
        "MOM" => new CharacterStyle
        {
            HairColor = new(176, 84, 60, 255), Hair = HairCut.Long,
            Top = new(246, 166, 120, 255), Accent = new(252, 244, 232, 255),
            Bottom = new(120, 96, 176, 255), Skirt = true, Shoes = new(120, 70, 60, 255), Height = 1.08f
        },
        "LADY" => new CharacterStyle
        {
            HairColor = new(244, 206, 104, 255), Hair = HairCut.Long,
            Top = new(118, 186, 132, 255), Accent = new(252, 248, 236, 255),
            Bottom = new(96, 140, 110, 255), Skirt = true, Shoes = new(110, 76, 60, 255), Height = 1.08f
        },
        "CLERK" => new CharacterStyle
        {
            HairColor = new(96, 64, 48, 255),
            Top = new(76, 132, 222, 255), Accent = new(246, 246, 250, 255), Stripes = true,
            Bottom = new(56, 60, 80, 255), Height = 1.06f
        },
        "YOUNGSTER" => new CharacterStyle
        {
            HairColor = new(84, 56, 44, 255),
            Hat = Headwear.Cap, HatColor = new(250, 200, 60, 255), HatBand = new(60, 110, 200, 255),
            Top = new(250, 250, 252, 255), Accent = new(60, 110, 200, 255), ShortSleeves = true,
            Bottom = new(64, 104, 190, 255), Shorts = true, Shoes = new(220, 72, 64, 255)
        },
        "LASS" => new CharacterStyle
        {
            HairColor = new(96, 60, 52, 255), Hair = HairCut.Long,
            Top = new(236, 104, 132, 255), Accent = new(252, 240, 244, 255), ShortSleeves = true,
            Bottom = new(72, 92, 168, 255), Skirt = true, Shoes = new(84, 56, 52, 255)
        },
        _ => new CharacterStyle()
    };
}
