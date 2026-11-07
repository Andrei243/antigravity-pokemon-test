using System;
using Raylib_cs;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>What is worn on the head (style guide, "Characters"; Helmet to Headband came with plan 11 · C2).</summary>
internal enum Headwear { None, Beret, Cap, NurseCap, Bow, Mask, Fedora, Helmet, HardHat, Beanie, Bandana, Headband, Cowboy, Visor, WideHat }

/// <summary>How the hair is cut (style guide, "Characters"; Bowl to Bald came with plan 11 · C2).</summary>
internal enum HairCut { Short, Spiky, Long, Swept, Afro, Bun, Braids, Bunches, Bowl, Ponytail, Curls, Bald }

/// <summary>
/// Body proportions. The chibi kit's four: children, adults with a longer body and a slightly smaller head, little
/// children with a shorter one (plan 11 · C2) and the elderly, shorter still with the head forward (C3). The grown
/// kit's five (plan 30 · FM4; style guide, "Grown characters"): teenagers, most adults, broad adults, children and
/// the elderly. A build belongs to one kit (<see cref="CharacterStyle.KitOf"/>), and a look's build is read in the kit
/// it is made in (<see cref="CharacterStyle.BuildIn"/>).
/// </summary>
internal enum BodyBuild { Kid, Adult, Little, Elder, Teen, Grown, GrownBroad, Child, GrownElder }

/// <summary>Something a person holds (plan 11 · C3; style guide, "Characters", "The second trainers' parts").</summary>
internal enum HeldProp { None, Rod, Parasol, Guitar, Net, Pickaxe, FloatRing, Camera }

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

    /// <summary>
    /// The kit the look is made in (plan 30 · FM4; style guide, "Grown characters"): the chibi kit unless it says the
    /// grown one, or is asked for in a grown build by name (<c>PLAYER~Teen</c>, <see cref="For"/>).
    /// </summary>
    public Data.CharacterKit Kit = Data.CharacterKit.Chibi;

    /// <summary>
    /// The grown kit's figure (plan 30 · FM4): a waist, hips as wide as the rib cage and a bust (true) or a straight
    /// one (false); left out, it follows <see cref="Lashes"/>, which the table gives its women and girls. The chibi kit
    /// has no figure, and a child's never curves.
    /// </summary>
    public bool? Curved;

    /// <summary>Colour of the shoes' soles (darker shoe colour when left out).</summary>
    public Color? Sole;

    /// <summary>Heavy eyebrows sculpted on the face (the professor); long lashes; rosy cheeks.</summary>
    public bool BushyBrows, Lashes, Blush;

    /// <summary>The accent colour is a scarf that hangs down in front, not a collar.</summary>
    public bool Scarf;

    /// <summary>A full beard round the chin and jaw in the hair's colour, below the mouth (plan 11 · C6).</summary>
    public bool Beard;

    /// <summary>Glasses in this frame colour, drawn by the faces rather than sculpted (none when left out; plan 11 · C6).</summary>
    public Color? Glasses;

    /// <summary>Team Galactic's mark on the chest of the uniform (plan 11 · C2; <see cref="Graphics.GalacticMark"/>).</summary>
    public bool GalacticMark;

    /// <summary>Clothes over the rest (plan 11 · C3): a bib and straps in the bottom's colour, an apron and a vest in their own, a
    /// robe closed to the ankles, and a swimsuit (bare arms and legs).</summary>
    public bool Overalls, Robe, Swimsuit;
    public Color? Apron, Vest;

    /// <summary>
    /// The top is a jacket (style guide, "Characters", "Layered clothes"): a ribbed band at its hem and cuffs, a zip down
    /// its front and two pocket flaps. A coat that is <see cref="Buttoned"/> is closed down its front with three buttons in
    /// the accent colour; any other coat stands open over the shirt.
    /// </summary>
    public bool Jacket, Buttoned;

    /// <summary>What the person holds (plan 11 · C3), in its own colour or the thing's.</summary>
    public HeldProp Holds;
    public Color? PropColor;

    /// <summary>Relative size: adults stand a little taller than the kids.</summary>
    public float Height = 1f;

    /// <summary>
    /// The style of a character type: its look in <c>Data/characters.json</c> (<see cref="Data.CharacterStyles"/>; the
    /// plain default for a name the table doesn't have). A type dressed in an outfit (<c>PLAYER@red_cap.-.-.-.-</c>,
    /// <see cref="Core.Outfit.Dress"/>) is its look's style with the outfit's garments put on (<see cref="Dressed"/>).
    /// </summary>
    public static CharacterStyle For(string npcType)
    {
        // A look holding something (plan 11 · C8): the player fishing is "PLAYER+Rod"; a look asked in a build of the
        // other kit (plan 30 · FM4): the player crossed into Kalos is "PLAYER~Teen"
        var (character, outfit, held, build) = Parse(npcType);
        // Asked in the grown kit, a look is the table's grown look of that name where it has one
        var look = build is { } asked ? Data.CharacterStyles.Default.Get(character, KitOf(asked)) : Data.CharacterStyles.Default.Get(character);
        var style = outfit == null ? look : Dressed(look, outfit);
        if (held is { } h) style.Holds = h;
        if (build is { } b) (style.Kit, style.Build) = (KitOf(b), b);
        style.Build = BuildIn(style.Build, style.Kit);
        return style;
    }

    /// <summary>The name of a look holding something (plan 11 · C8), which <see cref="For"/> reads back: <c>PLAYER+Rod</c>.</summary>
    public static string Holding(string npcType, HeldProp held) => $"{npcType}+{held}";

    /// <summary>
    /// The name of a look in a kit (plan 30 · FM4, decision 4: the player's body follows the region), which
    /// <see cref="For"/> reads back: the name as it is where the look is made in that kit already, else with the build
    /// the look stands as there (<c>PLAYER~Teen</c> in the grown kit, its <c>~Teen</c> taken off again in the chibi).
    /// The build is the table's grown look's where it has one (<see cref="Data.CharacterStyles.Get(string, Data.CharacterKit)"/>).
    /// </summary>
    public static string InKit(string npcType, Data.CharacterKit kit)
    {
        string plain = WithoutBuild(npcType);
        if (For(plain).Kit == kit) return plain;
        var (character, _, _, _) = Parse(plain);
        return $"{plain}~{BuildIn(Data.CharacterStyles.Default.Get(character, kit).Build, kit)}";
    }

    /// <summary>The kit a build belongs to (style guide, "Grown characters").</summary>
    public static Data.CharacterKit KitOf(BodyBuild build) =>
        build is BodyBuild.Teen or BodyBuild.Grown or BodyBuild.GrownBroad or BodyBuild.Child or BodyBuild.GrownElder
            ? Data.CharacterKit.Grown : Data.CharacterKit.Chibi;

    /// <summary>
    /// A build as the other kit reads it (style guide, "Grown characters"): the child is a teenager when grown, the
    /// adult a grown adult, the little child a child, the elder a grown elder, and back again (the broad build an adult).
    /// A build of the kit asked for is itself.
    /// </summary>
    public static BodyBuild BuildIn(BodyBuild build, Data.CharacterKit kit) => kit == Data.CharacterKit.Grown
        ? build switch
        {
            BodyBuild.Kid => BodyBuild.Teen,
            BodyBuild.Adult => BodyBuild.Grown,
            BodyBuild.Little => BodyBuild.Child,
            BodyBuild.Elder => BodyBuild.GrownElder,
            _ => build
        }
        : build switch
        {
            BodyBuild.Teen or BodyBuild.Child => BodyBuild.Kid,
            BodyBuild.Grown or BodyBuild.GrownBroad => BodyBuild.Adult,
            BodyBuild.GrownElder => BodyBuild.Elder,
            _ => build
        };

    /// <summary>Whether a grown body has a waist, hips and a bust: as the look says, else as its lashes do; a child never.</summary>
    public bool CurvedFigure() => Build != BodyBuild.Child && (Curved ?? Lashes);

    private static readonly char[] Marks = { '+', '~' };

    /// <summary>
    /// A character type taken apart: the look's own name, the outfit it is dressed in, what it holds and the build it
    /// is asked in, the last two written after it with <c>+</c> and <c>~</c> in either order.
    /// </summary>
    private static (string Character, Core.Outfit? Outfit, HeldProp? Held, BodyBuild? Build) Parse(string npcType)
    {
        HeldProp? held = null;
        BodyBuild? build = null;
        while (npcType.LastIndexOfAny(Marks) is var at and > 0)
        {
            string tail = npcType[(at + 1)..];
            if (npcType[at] == '+' && held == null && Named(tail, out HeldProp h)) held = h;
            else if (npcType[at] == '~' && build == null && Named(tail, out BodyBuild b)) build = b;
            else break;
            npcType = npcType[..at];
        }
        var (character, outfit) = Core.Outfit.Undress(npcType);
        return (character, outfit, held, build);
    }

    /// <summary>A type's name with the build it is asked in taken off (what it holds and its outfit kept).</summary>
    private static string WithoutBuild(string npcType)
    {
        int at = npcType.LastIndexOf('~');
        if (at <= 0 || !Named(npcType[(at + 1)..].Split('+')[0], out BodyBuild _)) return npcType;
        int end = npcType.IndexOf('+', at);
        return end < 0 ? npcType[..at] : npcType[..at] + npcType[end..];
    }

    /// <summary>A value of an enum by its name (never by a number, which a name of the game could end in).</summary>
    private static bool Named<T>(string text, out T value) where T : struct, Enum =>
        Enum.TryParse(text, true, out value) && text.Length > 0 && !char.IsAsciiDigit(text[0]) && Enum.IsDefined(value);

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
