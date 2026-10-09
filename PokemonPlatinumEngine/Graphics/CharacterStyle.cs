using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

internal enum Headwear { None, Beret, Cap, NurseCap }

internal enum HairCut { Short, Spiky, Long, Swept }

/// <summary>Body proportions: chibi children, and adults with a longer body and a slightly smaller head.</summary>
internal enum BodyBuild { Kid, Adult }

/// <summary>
/// Outfit and colouring for one kind of character, shared by the 3D field models and the 2D trainer card art. Each
/// look is one of these written as values in <c>Data/characters.json</c>, under the fields' own names (plan 11 · C1).
/// </summary>
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

    /// <summary>
    /// The style of a character type: its look in <c>Data/characters.json</c> (<see cref="Data.CharacterStyles"/>; the
    /// plain default for a name the table doesn't have). A type dressed in an outfit (<c>PLAYER@red_cap.-.-.-.-</c>,
    /// <see cref="Core.Outfit.Dress"/>) is its look's style with the outfit's garments put on (<see cref="Dressed"/>).
    /// </summary>
    public static CharacterStyle For(string npcType)
    {
        var (character, outfit) = Core.Outfit.Undress(npcType);
        var look = Data.CharacterStyles.Default.Get(character);
        return outfit == null ? look : Dressed(look, outfit);
    }

    /// <summary>
    /// <paramref name="look"/> with the outfit's garments put on: each changes only what its slot covers (a hat its
    /// kind and colours, a top its colours and shape, and so on), so the body, the hair, the face and the slots left
    /// to the look's own clothes stay as they are. Garments the data doesn't know are left off.
    /// </summary>
    public static CharacterStyle Dressed(CharacterStyle look, Core.Outfit outfit)
    {
        var s = look.Clone();
        foreach (var (slot, id) in outfit.Garments())
        {
            if (Data.ClothingDatabase.Get(id) is not { } g || g.Slot != slot) continue;
            switch (slot)
            {
                case Core.ClothingSlot.Hat:
                    if (g.IsNothing) { s.Hat = Headwear.None; break; }
                    s.Hat = Enum.TryParse<Headwear>(g.Hat, true, out var hat) && hat != Headwear.None ? hat : Headwear.Cap;
                    s.HatColor = ColorOf(g.Color!);
                    s.HatBand = g.Accent != null ? ColorOf(g.Accent) : Color.White;
                    break;
                case Core.ClothingSlot.Top:
                    s.Top = ColorOf(g.Color ?? "#f0f0f0");
                    s.Accent = g.Accent != null ? ColorOf(g.Accent) : s.Top;
                    (s.Coat, s.ShortSleeves, s.Stripes, s.Scarf) = (g.Coat, g.ShortSleeves, g.Stripes, g.Scarf);
                    break;
                case Core.ClothingSlot.Bottoms:
                    s.Bottom = ColorOf(g.Color ?? "#40404c");
                    (s.Skirt, s.Shorts) = (g.Skirt, g.Shorts && !g.Skirt);
                    break;
                case Core.ClothingSlot.Shoes:
                    s.Shoes = ColorOf(g.Color ?? "#404040");
                    s.Sole = g.Accent != null ? ColorOf(g.Accent) : null;
                    break;
                case Core.ClothingSlot.Bag:
                    s.Bag = g.IsNothing ? null : ColorOf(g.Color!);
                    break;
            }
        }
        return s;
    }

    /// <summary>A copy to change without changing this one.</summary>
    public CharacterStyle Clone() => (CharacterStyle)MemberwiseClone();

    private static Color ColorOf(string hex)
    {
        var (r, g, b) = Data.Garment.Rgb(hex);
        return new Color(r, g, b, (byte)255);
    }
}
