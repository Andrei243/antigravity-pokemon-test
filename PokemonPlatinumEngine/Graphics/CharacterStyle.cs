using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

internal enum Headwear { None, Beret, Cap, NurseCap }

internal enum HairCut { Short, Spiky, Long, Swept }

/// <summary>Body proportions: chibi children, and adults with a longer body and a slightly smaller head.</summary>
internal enum BodyBuild { Kid, Adult }

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

    public BodyBuild Build = BodyBuild.Kid;

    /// <summary>Colour of the shoes' soles (darker shoe colour when left out).</summary>
    public Color? Sole;

    /// <summary>Heavy eyebrows sculpted on the face (the professor); long lashes; rosy cheeks.</summary>
    public bool BushyBrows, Lashes, Blush;

    /// <summary>The accent colour is a scarf that hangs down in front, not a collar.</summary>
    public bool Scarf;

    /// <summary>Relative size: adults stand a little taller than the kids.</summary>
    public float Height = 1f;

    public static CharacterStyle For(string npcType) => npcType.ToUpperInvariant() switch
    {
        "PLAYER" or "TRAINER" or "LUCAS" => new CharacterStyle
        {
            HairColor = new(70, 56, 78, 255),
            Hat = Headwear.Beret, HatColor = new(220, 56, 60, 255), HatBand = new(246, 246, 250, 255),
            Top = new(58, 78, 138, 255), Accent = new(236, 70, 70, 255),
            Bottom = new(48, 50, 70, 255), Shoes = new(200, 70, 60, 255), Sole = new(240, 236, 228, 255),
            Bag = new(242, 196, 70, 255), Blush = true, Scarf = true
        },
        // The girl the player can be instead (and the professor's assistant when they aren't): our own take on
        // Platinum's winter clothes, a white knitted hat, a long pink coat, a white scarf and pink boots
        "DAWN" => new CharacterStyle
        {
            HairColor = new(54, 62, 104, 255), Hair = HairCut.Long,
            Hat = Headwear.Beret, HatColor = new(248, 248, 252, 255), HatBand = new(238, 124, 156, 255),
            Top = new(226, 84, 118, 255), Accent = new(248, 248, 252, 255), Coat = true,
            Bottom = new(226, 84, 118, 255), Skirt = true, Shoes = new(238, 124, 156, 255), Sole = new(244, 240, 236, 255),
            Bag = new(250, 246, 236, 255), Eyes = new(70, 86, 140, 255), Lashes = true, Blush = true, Scarf = true
        },
        "RIVAL" => new CharacterStyle
        {
            HairColor = new(250, 212, 80, 255), Hair = HairCut.Spiky,
            Top = new(244, 132, 52, 255), Accent = new(72, 176, 104, 255), Stripes = true,
            Bottom = new(58, 70, 108, 255), Shoes = new(236, 236, 240, 255), Sole = new(150, 150, 166, 255), Eyes = new(96, 70, 40, 255),
            Blush = true, Scarf = true
        },
        "ROWAN" => new CharacterStyle
        {
            HairColor = new(190, 194, 210, 255), Hair = HairCut.Swept, Mustache = true, Coat = true,
            Top = new(126, 92, 66, 255), Accent = new(236, 236, 242, 255),
            Bottom = new(84, 66, 56, 255), Shoes = new(52, 42, 40, 255), Height = 1.04f, Build = BodyBuild.Adult, BushyBrows = true
        },
        "NURSE" => new CharacterStyle
        {
            HairColor = new(248, 150, 190, 255), Hair = HairCut.Long,
            Hat = Headwear.NurseCap, HatColor = Color.White, HatBand = new(232, 72, 96, 255),
            Top = new(252, 252, 255, 255), Accent = new(248, 170, 200, 255),
            Bottom = new(248, 170, 200, 255), Skirt = true, Shoes = new(248, 248, 252, 255),
            Eyes = new(70, 120, 200, 255), Height = 1.0f, Build = BodyBuild.Adult, Lashes = true, Blush = true
        },
        "MOM" => new CharacterStyle
        {
            HairColor = new(176, 84, 60, 255), Hair = HairCut.Long,
            Top = new(246, 166, 120, 255), Accent = new(252, 244, 232, 255),
            Bottom = new(120, 96, 176, 255), Skirt = true, Shoes = new(120, 70, 60, 255), Height = 1.0f, Build = BodyBuild.Adult, Lashes = true
        },
        "LADY" => new CharacterStyle
        {
            HairColor = new(244, 206, 104, 255), Hair = HairCut.Long,
            Top = new(118, 186, 132, 255), Accent = new(252, 248, 236, 255),
            Bottom = new(96, 140, 110, 255), Skirt = true, Shoes = new(110, 76, 60, 255), Height = 1.0f, Build = BodyBuild.Adult, Lashes = true
        },
        "CLERK" => new CharacterStyle
        {
            HairColor = new(96, 64, 48, 255),
            Top = new(76, 132, 222, 255), Accent = new(246, 246, 250, 255), Stripes = true,
            Bottom = new(56, 60, 80, 255), Height = 0.98f, Build = BodyBuild.Adult
        },
        "YOUNGSTER" => new CharacterStyle
        {
            HairColor = new(84, 56, 44, 255),
            Hat = Headwear.Cap, HatColor = new(250, 200, 60, 255), HatBand = new(60, 110, 200, 255),
            Top = new(250, 250, 252, 255), Accent = new(60, 110, 200, 255), ShortSleeves = true, Stripes = true,
            Bottom = new(64, 104, 190, 255), Shorts = true, Shoes = new(220, 72, 64, 255), Sole = new(244, 240, 232, 255), Blush = true
        },
        "LASS" => new CharacterStyle
        {
            HairColor = new(96, 60, 52, 255), Hair = HairCut.Long,
            Top = new(236, 104, 132, 255), Accent = new(252, 240, 244, 255), ShortSleeves = true,
            Bottom = new(72, 92, 168, 255), Skirt = true, Shoes = new(84, 56, 52, 255), Lashes = true, Blush = true
        },
        "CLOWN" => new CharacterStyle
        {
            HairColor = new(236, 72, 60, 255), Hair = HairCut.Spiky,
            Top = new(250, 214, 64, 255), Accent = new(236, 72, 60, 255), Stripes = true,
            Bottom = new(70, 120, 220, 255), Shoes = new(236, 72, 60, 255), Height = 1.0f, Build = BodyBuild.Adult
        },
        "LOOKER" => new CharacterStyle
        {
            HairColor = new(58, 46, 44, 255), Hair = HairCut.Swept, Coat = true,
            Top = new(196, 168, 118, 255), Accent = new(84, 70, 64, 255),
            Bottom = new(70, 66, 74, 255), Shoes = new(52, 42, 40, 255), Height = 1.04f, Build = BodyBuild.Adult
        },
        // The leader of Team Galactic, met by the shore of Lake Verity (plan 02 · S4): our own take, a tall man with
        // spiky blue hair in a long grey coat with a dark collar
        "CYRUS" => new CharacterStyle
        {
            HairColor = new(66, 92, 156, 255), Hair = HairCut.Spiky, Coat = true,
            Top = new(132, 136, 150, 255), Accent = new(56, 58, 74, 255),
            Bottom = new(60, 62, 78, 255), Shoes = new(44, 44, 54, 255), Eyes = new(44, 48, 70, 255), Height = 1.1f, Build = BodyBuild.Adult
        },
        // Oreburgh's Gym Leader (plan 02 · S5): our own take, a young miner with red hair under a yellow safety
        // helmet, in a work coat and jeans
        "ROARK" => new CharacterStyle
        {
            HairColor = new(170, 66, 50, 255), Hair = HairCut.Spiky,
            Hat = Headwear.Cap, HatColor = new(246, 196, 62, 255), HatBand = new(120, 92, 56, 255),
            Top = new(112, 116, 98, 255), Accent = new(70, 72, 62, 255), Coat = true,
            Bottom = new(58, 74, 116, 255), Shoes = new(92, 62, 44, 255), Height = 1.04f, Build = BodyBuild.Adult
        },
        // The Leaders of the Gyms rebuilt in plan 01 · M9, each our own take. Hearthome's: long violet hair and a long
        // gown in purples, a dancer's
        "FANTINA" => new CharacterStyle
        {
            HairColor = new(130, 78, 156, 255), Hair = HairCut.Long,
            Top = new(118, 72, 150, 255), Accent = new(220, 196, 236, 255), Coat = true,
            Bottom = new(96, 56, 128, 255), Skirt = true, Shoes = new(64, 40, 80, 255),
            Eyes = new(110, 60, 130, 255), Height = 1.04f, Build = BodyBuild.Adult, Lashes = true
        },
        // Veilstone's: short pink hair, a fighter's white top with a blue sash, and bare feet
        "MAYLENE" => new CharacterStyle
        {
            HairColor = new(236, 132, 168, 255), Hair = HairCut.Short,
            Top = new(246, 246, 250, 255), Accent = new(64, 104, 196, 255), ShortSleeves = true,
            Bottom = new(64, 104, 196, 255), Shorts = true, Shoes = new(240, 206, 178, 255),
            Eyes = new(150, 70, 100, 255), Height = 0.96f, Lashes = true, Blush = true
        },
        // Her black belts: close-cropped dark hair, a white training jacket and trousers, the black belt, bare feet
        "BLACKBELT" => new CharacterStyle
        {
            HairColor = new(44, 40, 46, 255), Hair = HairCut.Short,
            Top = new(244, 244, 240, 255), Accent = new(36, 34, 40, 255),
            Bottom = new(236, 236, 230, 255), Shoes = new(222, 172, 132, 255),
            Eyes = new(60, 50, 44, 255), Height = 1.06f, Build = BodyBuild.Adult, BushyBrows = true
        },
        // Team Galactic's grunts (plan 02 · S5): our own take, teal hair cut in a bowl and a pale grey uniform with a
        // dark collar
        "GRUNT" => new CharacterStyle
        {
            HairColor = new(64, 150, 158, 255), Hair = HairCut.Short,
            Top = new(208, 212, 222, 255), Accent = new(56, 60, 80, 255),
            Bottom = new(150, 156, 172, 255), Shoes = new(52, 54, 66, 255), Height = 1.0f, Build = BodyBuild.Adult
        },
        // Team Galactic's Commander met at the Valley Windworks (plan 02 · S6): our own take, short crimson hair and the
        // team's pale uniform, its dark collar and a short grey skirt
        "MARS" => new CharacterStyle
        {
            HairColor = new(196, 54, 70, 255), Hair = HairCut.Short,
            Top = new(222, 224, 232, 255), Accent = new(56, 60, 80, 255),
            Bottom = new(132, 138, 156, 255), Skirt = true, Shoes = new(52, 54, 66, 255), Sole = new(200, 202, 212, 255),
            Eyes = new(150, 50, 70, 255), Height = 1.0f, Build = BodyBuild.Adult, Lashes = true
        },
        // The team's old scientist (plan 02 · S6): our own take, a short man with thin grey hair in a long white lab coat
        "CHARON" => new CharacterStyle
        {
            HairColor = new(178, 180, 190, 255), Hair = HairCut.Swept, Coat = true,
            Top = new(236, 238, 244, 255), Accent = new(84, 86, 108, 255),
            Bottom = new(76, 78, 96, 255), Shoes = new(52, 42, 40, 255), Height = 0.94f, Build = BodyBuild.Adult
        },
        "GENTLEMAN" => new CharacterStyle
        {
            HairColor = new(150, 146, 156, 255), Hair = HairCut.Swept, Mustache = true, Coat = true,
            Top = new(66, 70, 96, 255), Accent = new(246, 246, 250, 255),
            Bottom = new(56, 58, 76, 255), Shoes = new(52, 42, 40, 255), Height = 1.02f, Build = BodyBuild.Adult
        },
        _ => new CharacterStyle()
    };
}
