using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>The five qualities of the contest condition, in the original's order (<c>MON_DATA_COOL</c> on); the sheen is the sixth.</summary>
public enum ContestStat { Cool, Beauty, Cute, Smart, Tough }

/// <summary>
/// A Poffin's kind, in the original's order (<c>enum PoffinType</c>): the five flavours, each alone and before each of
/// the others (a flavour and the stronger first), then the rich (three flavours), the overripe (four or five), the
/// foul (a mistake) and the mild (a flavour of 50 or more).
/// </summary>
public enum PoffinType
{
    Spicy, SpicyDry, SpicySweet, SpicyBitter, SpicySour,
    DrySpicy, Dry, DrySweet, DryBitter, DrySour,
    SweetSpicy, SweetDry, Sweet, SweetBitter, SweetSour,
    BitterSpicy, BitterDry, BitterSweet, Bitter, BitterSour,
    SourSpicy, SourDry, SourSweet, SourBitter, Sour,
    Rich, Overripe, Foul, Mild
}

/// <summary>How a Pokémon takes a Poffin, by its nature (<c>PoffinCase_GetPoffinPreference</c>).</summary>
public enum PoffinTaste { Neutral, Liked, Disliked }

/// <summary>
/// A Poffin (plan 06 · R14c; the original's <c>Poffin</c>, <c>src/poffin.c</c>): its kind, its five flavours and its
/// smoothness, each a byte. Made in a pot at the Poffin House (<see cref="PoffinPot"/>) or given by someone, kept in
/// the Poffin Case (<see cref="PoffinCase"/>), and fed to a Pokémon (<see cref="Poffins.Feed"/>).
/// </summary>
public sealed record Poffin(PoffinType Type, int Spicy, int Dry, int Sweet, int Bitter, int Sour, int Smoothness)
{
    /// <summary>One of its five flavours.</summary>
    public int this[Flavor flavor] => flavor switch
    {
        Flavor.Spicy => Spicy,
        Flavor.Dry => Dry,
        Flavor.Sweet => Sweet,
        Flavor.Bitter => Bitter,
        _ => Sour
    };

    /// <summary>
    /// Its level (<c>Poffin_CalcLevel</c>): the flavour its kind is named for first, or for a rich, overripe, foul or
    /// mild one the strongest of the five; never over 99.
    /// </summary>
    public int Level
    {
        get
        {
            int level = ((int)Type / 5) switch
            {
                0 => Spicy,
                1 => Dry,
                2 => Sweet,
                3 => Bitter,
                4 => Sour,
                _ => Math.Max(Spicy, Math.Max(Dry, Math.Max(Sweet, Math.Max(Bitter, Sour))))
            };
            return Math.Min(level, Poffins.MaxLevel);
        }
    }

    /// <summary>Its name as the case lists it: "Spicy-Dry Poffin", "Mild Poffin".</summary>
    public string Name => Poffins.NameOf(Type);

    /// <summary>Whether it has any of the flavour at all: the case lists it under that flavour.</summary>
    public bool Has(Flavor flavor) => this[flavor] != 0;
}

/// <summary>
/// The rules of Poffins outside the pot (plan 06 · R14c), by the original's code: how one is made from its flavours
/// (<c>Poffin_MakePoffin</c>), how a nature takes it and what eating it does to a Pokémon's condition
/// (<c>PoffinCase_UpdateMonContestStats</c>, <c>src/applications/poffin_case/main.c</c>). No drawing or input.
/// </summary>
public static class Poffins
{
    /// <summary>The most any quality of the condition and the sheen can be (<c>MAX_CONTEST_STAT</c>, <c>MAX_POKEMON_SHEEN</c>).</summary>
    public const int MaxCondition = 255;

    /// <summary>The highest level a Poffin is shown at (<c>Poffin_CalcLevel</c>).</summary>
    public const int MaxLevel = 99;

    /// <summary>A flavour this strong makes the Poffin mild, whatever else it is.</summary>
    public const int MildFrom = 50;

    /// <summary>
    /// A Poffin of these flavours and this smoothness (<c>Poffin_MakePoffin</c>), each taken as a byte as the original
    /// keeps it. One flavour names it alone, two the stronger first (the first in the list on a tie), three make it
    /// rich and four or five overripe; any flavour of 50 or more makes it mild; none at all, or <paramref name="foul"/>,
    /// makes a foul one, which keeps the smoothness and is given three flavours of 2 drawn at random from those it lacks
    /// (<c>Poffin_MakeFoul</c>, on the game's generator).
    /// </summary>
    public static Poffin Make(IReadOnlyList<int> flavors, int smoothness, bool foul, Random rng)
    {
        if (flavors.Count != 5) throw new ArgumentException("A Poffin has five flavours.", nameof(flavors));
        var f = new int[5];
        for (int i = 0; i < 5; i++) f[i] = (byte)flavors[i];
        smoothness = (byte)smoothness;
        if (foul) return Foul(new int[5], smoothness, rng);

        var present = new List<int>();
        bool mild = false;
        for (int i = 0; i < 5; i++)
        {
            if (f[i] == 0) continue;
            if (f[i] >= MildFrom) mild = true;
            present.Add(i);
        }

        PoffinType type;
        switch (present.Count)
        {
            case 0:
                return Foul(f, smoothness, rng);
            case 1:
                type = (PoffinType)(present[0] * 5 + present[0]);
                break;
            case 2:
                type = f[present[0]] >= f[present[1]]
                    ? (PoffinType)(present[0] * 5 + present[1])
                    : (PoffinType)(present[1] * 5 + present[0]);
                break;
            case 3:
                type = PoffinType.Rich;
                break;
            default:
                type = PoffinType.Overripe;
                break;
        }
        if (mild) type = PoffinType.Mild;
        return new Poffin(type, f[0], f[1], f[2], f[3], f[4], smoothness);
    }

    // Poffin_MakeFoul: three of the flavours not there already set to 2, drawn LCRNG_Next() % 5 until three are
    private static Poffin Foul(int[] f, int smoothness, Random rng)
    {
        int set = 0;
        while (set < 3)
        {
            int i = rng.Next(5);
            if (f[i] != 0) continue;
            f[i] = 2;
            set++;
        }
        return new Poffin(PoffinType.Foul, f[0], f[1], f[2], f[3], f[4], smoothness);
    }

    private static readonly string[] FlavorNames = { "Spicy", "Dry", "Sweet", "Bitter", "Sour" };

    /// <summary>A kind's name (the original's <c>poffin_types</c> list): its flavours and "Poffin".</summary>
    public static string NameOf(PoffinType type) => type switch
    {
        PoffinType.Rich => "Rich Poffin",
        PoffinType.Overripe => "Overripe Poffin",
        PoffinType.Foul => "Foul Poffin",
        PoffinType.Mild => "Mild Poffin",
        _ when (int)type / 5 == (int)type % 5 => FlavorNames[(int)type / 5] + " Poffin",
        _ => FlavorNames[(int)type / 5] + "-" + FlavorNames[(int)type % 5] + " Poffin"
    };

    /// <summary>A flavour's name in capitals, as the case's tabs show it.</summary>
    public static string NameOf(Flavor flavor) => FlavorNames[(int)flavor].ToUpperInvariant();

    /// <summary>The quality of the condition a flavour raises: spicy cool, dry beauty, sweet cute, bitter smart, sour tough.</summary>
    public static ContestStat Raises(Flavor flavor) => (ContestStat)(int)flavor;

    // sFlavorPreferences: the flavour each nature likes and the one it dislikes; the five natures that change no
    // stat like and dislike nothing
    private static readonly (Flavor? Liked, Flavor? Disliked)[] Tastes =
    {
        (null, null),                    // Hardy
        (Flavor.Spicy, Flavor.Sour),     // Lonely
        (Flavor.Spicy, Flavor.Sweet),    // Brave
        (Flavor.Spicy, Flavor.Dry),      // Adamant
        (Flavor.Spicy, Flavor.Bitter),   // Naughty
        (Flavor.Sour, Flavor.Spicy),     // Bold
        (null, null),                    // Docile
        (Flavor.Sour, Flavor.Sweet),     // Relaxed
        (Flavor.Sour, Flavor.Dry),       // Impish
        (Flavor.Sour, Flavor.Bitter),    // Lax
        (Flavor.Sweet, Flavor.Spicy),    // Timid
        (Flavor.Sweet, Flavor.Sour),     // Hasty
        (null, null),                    // Serious
        (Flavor.Sweet, Flavor.Dry),      // Jolly
        (Flavor.Sweet, Flavor.Bitter),   // Naive
        (Flavor.Dry, Flavor.Spicy),      // Modest
        (Flavor.Dry, Flavor.Sour),       // Mild
        (Flavor.Dry, Flavor.Sweet),      // Quiet
        (null, null),                    // Bashful
        (Flavor.Dry, Flavor.Bitter),     // Rash
        (Flavor.Bitter, Flavor.Spicy),   // Calm
        (Flavor.Bitter, Flavor.Sour),    // Gentle
        (Flavor.Bitter, Flavor.Sweet),   // Sassy
        (Flavor.Bitter, Flavor.Dry),     // Careful
        (null, null)                     // Quirky
    };

    /// <summary>The flavour a nature likes and the one it dislikes; neither for the five that change no stat.</summary>
    public static (Flavor? Liked, Flavor? Disliked) TasteOf(Nature nature) => Tastes[(int)nature];

    /// <summary>
    /// How a Pokémon of this nature takes the Poffin (<c>PoffinCase_GetPoffinPreference</c>): liked when there is more
    /// of the flavour it likes than of the one it dislikes, disliked when less, and neither when they are the same or
    /// its nature has no taste.
    /// </summary>
    public static PoffinTaste TasteFor(Poffin poffin, Nature nature)
    {
        var (liked, disliked) = TasteOf(nature);
        if (liked is not { } like || disliked is not { } dislike) return PoffinTaste.Neutral;
        int a = poffin[like], b = poffin[dislike];
        return a == b ? PoffinTaste.Neutral : a > b ? PoffinTaste.Liked : PoffinTaste.Disliked;
    }

    /// <summary>
    /// Whether the Pokémon will eat a Poffin: not once its sheen is full (<c>TryFeedPoffin</c>: "won't eat any
    /// more"), however much room its five qualities have.
    /// </summary>
    public static bool WouldEat(Pokemon p) => p.Sheen < MaxCondition;

    /// <summary>
    /// What eating the Poffin does (<c>PoffinCase_UpdateMonContestStats</c>): each flavour is added to its quality and
    /// the smoothness to the sheen, each held to 255; the flavour the nature likes counts a tenth more and the one it
    /// dislikes a tenth less, worked out as the original does in single precision and cut to a whole byte; and the
    /// Pokémon likes its trainer a little more, one point under the most. The caller asks <see cref="WouldEat"/> first.
    /// </summary>
    public static void Feed(Poffin poffin, Pokemon p)
    {
        var amounts = new int[6] { poffin.Spicy, poffin.Dry, poffin.Sweet, poffin.Bitter, poffin.Sour, poffin.Smoothness };
        var (liked, disliked) = TasteOf(p.Nature);
        if (liked is { } like && disliked is { } dislike)
        {
            // u8 adjustedPoffinAttrs[..] = attr * 1.1f: a float product, converted to an integer and kept as a byte
            amounts[(int)like] = (byte)(int)(amounts[(int)like] * 1.1f);
            amounts[(int)dislike] = (byte)(int)(amounts[(int)dislike] * 0.9f);
        }

        p.Cool = Math.Min(MaxCondition, p.Cool + amounts[0]);
        p.Beauty = Math.Min(MaxCondition, p.Beauty + amounts[1]);
        p.Cute = Math.Min(MaxCondition, p.Cute + amounts[2]);
        p.Smart = Math.Min(MaxCondition, p.Smart + amounts[3]);
        p.Tough = Math.Min(MaxCondition, p.Tough + amounts[4]);
        p.Sheen = Math.Min(MaxCondition, p.Sheen + amounts[5]);
        if (p.Friendship < FriendshipRules.Max) p.Friendship++;
    }

    /// <summary>The summary's sparkles for a sheen (<c>PokemonSummaryScreen_InitSheenSprites</c>): none for none, all twelve when full, else twelve × sheen / 256 by the original's sum.</summary>
    public const int SheenSparkles = 12;

    public static int SparklesOf(int sheen) =>
        sheen <= 0 ? 0 : sheen >= MaxCondition ? SheenSparkles : (((SheenSparkles << 8) / MaxCondition) * sheen) >> 8;
}
