using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Effects;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>A Pokéblock's colour, in Emerald's order (<c>PBLOCK_CLR_*</c>): what the berries blended came to.</summary>
public enum PokeblockColor
{
    None, Red, Blue, Pink, Green, Yellow, Purple, Indigo, Brown, LiteBlue, Olive, Gray, Black, White, Gold
}

/// <summary>
/// A Pokéblock (plan 21 · H11; Emerald's <c>struct Pokeblock</c>): its colour, its five flavours and its feel (the
/// smoothness the blend came to), each a byte. Made in the Berry Blender, kept in the Pokéblock Case
/// (<see cref="PokeblockCase"/>) and fed to a Pokémon (<see cref="Pokeblocks.Feed"/>).
/// </summary>
public sealed record Pokeblock(PokeblockColor Color, int Spicy, int Dry, int Sweet, int Bitter, int Sour, int Feel)
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

    /// <summary>Its level: the strongest of its flavours (<c>GetHighestPokeblocksFlavorLevel</c>).</summary>
    public int Level => Math.Max(Spicy, Math.Max(Dry, Math.Max(Sweet, Math.Max(Bitter, Sour))));

    /// <summary>Its feel, held to 99 (<c>POKEBLOCK_MAX_FEEL</c>, <c>GetPokeblocksFeel</c>).</summary>
    public int ShownFeel => Math.Min(Feel, Pokeblocks.MaxFeel);

    /// <summary>The flavour it is mostly of (<c>GetPokeblocksFlavor</c>: the first of the strongest).</summary>
    public Flavor Strongest
    {
        get
        {
            int best = 0;
            for (int i = 1; i < 5; i++)
                if (this[(Flavor)i] > this[(Flavor)best]) best = i;
            return (Flavor)best;
        }
    }

    /// <summary>Its name as the case lists it: "Red Pokéblock".</summary>
    public string Name => Pokeblocks.NameOf(Color);
}

/// <summary>A berry as the blender takes it: its flavours and smoothness (the berry's own data, <c>BerryData</c>).</summary>
public sealed record BlenderBerry(string Name, int Spicy, int Dry, int Sweet, int Bitter, int Sour, int Smoothness)
{
    public static BlenderBerry Of(ItemData berry)
    {
        var b = berry.Berry ?? throw new ArgumentException($"{berry.Name} is no berry.", nameof(berry));
        return new BlenderBerry(berry.Name, b.Spiciness, b.Dryness, b.Sweetness, b.Bitterness, b.Sourness, b.Smoothness);
    }

    internal int Flavor(int i) => i switch { 0 => Spicy, 1 => Dry, 2 => Sweet, 3 => Bitter, _ => Sour };
}

/// <summary>
/// The Pokéblock Case (plan 21 · H11; the original's <c>gSaveBlock1Ptr->pokeblocks</c>): forty places, filled from the
/// first empty one, and a Pokéblock taken out leaves its place empty (<c>ClearPokeblock</c>, no closing up). Saved. No
/// drawing, no input.
/// </summary>
public sealed class PokeblockCase
{
    public const int Capacity = 40;

    private readonly Pokeblock?[] slots = new Pokeblock?[Capacity];

    /// <summary>The places, empty ones null.</summary>
    public IReadOnlyList<Pokeblock?> Slots => slots;

    public int Count => slots.Count(s => s != null);
    public bool IsFull => Count >= Capacity;

    /// <summary>Puts a Pokéblock in the first empty place (<c>AddPokeblock</c>): false, with nothing changed, when the case is full.</summary>
    public bool Add(Pokeblock block)
    {
        int free = Array.FindIndex(slots, s => s == null);
        if (free < 0) return false;
        slots[free] = block;
        return true;
    }

    /// <summary>Takes out the Pokéblock at a place.</summary>
    public void RemoveAt(int index)
    {
        if (index >= 0 && index < Capacity) slots[index] = null;
    }

    /// <summary>The filled places in order, with their place in the case.</summary>
    public IReadOnlyList<(int Slot, Pokeblock Block)> Listed() =>
        slots.Select((b, i) => (Slot: i, Block: b)).Where(p => p.Block != null).Select(p => (p.Slot, p.Block!)).ToList();

    public void Clear() => Array.Clear(slots);

    /// <summary>The case as a save has it: the filled places in order, packed from the first.</summary>
    public void Restore(IEnumerable<Pokeblock>? saved)
    {
        Array.Clear(slots);
        if (saved == null) return;
        int i = 0;
        foreach (var b in saved.Take(Capacity)) slots[i++] = b;
    }

    /// <summary>What a save keeps of the case.</summary>
    public List<Pokeblock> ToSaved() => slots.OfType<Pokeblock>().ToList();
}

/// <summary>
/// The rules of Pokéblocks (plan 21 · H11; Emerald's <c>src/berry_blender.c</c> and <c>src/pokeblock.c</c>): what the
/// Berry Blender makes of the berries put in it and the speed it was stirred at, how a nature takes a Pokéblock, and
/// what eating one does to a Pokémon's condition and sheen. No drawing, no input.
/// </summary>
public static class Pokeblocks
{
    public const int MaxFeel = 99;

    /// <summary>The colour's name as the case lists it.</summary>
    public static string NameOf(PokeblockColor color) => Text.Of($"pokeblock.color.{color}");

    // sBlackPokeblockFlavorFlags: the ten ways three of the five flavours are picked for a black Pokéblock
    private static readonly int[] BlackFlavorFlags =
    {
        (1 << 4) | (1 << 3) | (1 << 2), (1 << 4) | (1 << 2) | (1 << 1), (1 << 4) | (1 << 1) | (1 << 0),
        (1 << 4) | (1 << 3) | (1 << 1), (1 << 4) | (1 << 3) | (1 << 0), (1 << 3) | (1 << 2) | (1 << 1),
        (1 << 3) | (1 << 2) | (1 << 0), (1 << 3) | (1 << 1) | (1 << 0), (1 << 2) | (1 << 1) | (1 << 0),
        (1 << 4) | (1 << 2) | (1 << 0)
    };

    /// <summary>
    /// What the blender makes (<c>CalculatePokeblock</c>): the berries' flavours and smoothness added up, each flavour
    /// taken from the next one's (so only the strongest stand out), every negative one counted and taken from the
    /// rest, the whole scaled by the speed (<paramref name="maxRpm"/> in hundredths of a turn a minute, a hundred and
    /// a three hundred and thirty-third of it, in tenths), and the colour and feel read from the result. Two of one
    /// berry, or no flavour left, or four of the five taken away, make a black one whose flavours are two in three of
    /// the five, drawn on <paramref name="rng"/>.
    /// </summary>
    public static Pokeblock Blend(IReadOnlyList<BlenderBerry> berries, int maxRpm, Random rng)
    {
        if (berries.Count is < 1 or > 4) throw new ArgumentException("The blender takes one to four berries.", nameof(berries));
        int numPlayers = berries.Count;
        var f = new int[6];
        foreach (var b in berries)
        {
            for (int j = 0; j < 5; j++) f[j] += b.Flavor(j);
            f[5] += b.Smoothness;
        }

        // Each flavour is taken from the next one's; order matters
        int first = f[0];
        f[0] -= f[1];
        f[1] -= f[2];
        f[2] -= f[3];
        f[3] -= f[4];
        f[4] -= first;

        int negatives = 0;
        for (int i = 0; i < 5; i++)
        {
            if (f[i] < 0)
            {
                f[i] = 0;
                negatives++;
            }
        }
        // The count of negatives is taken from each flavour that is left (never below nothing)
        for (int i = 0; i < 5; i++)
        {
            if (f[i] > 0) f[i] = f[i] < negatives ? 0 : f[i] - negatives;
        }

        // The speed: maxRPM / 333 + 100, in tenths of a percent... applied as flavor * factor / 10, rounded at tenths
        int factor = maxRpm / 333 + 100;
        for (int i = 0; i < 5; i++)
        {
            int flavor = f[i] * factor / 10;
            int remainder = flavor % 10;
            flavor /= 10;
            if (remainder > 4) flavor++;
            f[i] = flavor;
        }

        var color = ColorOf(berries, f, negatives);
        f[5] = Math.Max(0, f[5] / numPlayers - numPlayers);

        if (color == PokeblockColor.Black)
        {
            // Black ones get their flavours reassigned: three of the five at 2
            int pick = rng.Next(BlackFlavorFlags.Length);
            for (int i = 0; i < 5; i++) f[i] = ((BlackFlavorFlags[pick] >> i) & 1) != 0 ? 2 : 0;
        }
        for (int i = 0; i < 6; i++) f[i] = Math.Min(255, f[i]);
        return new Pokeblock(color, f[0], f[1], f[2], f[3], f[4], f[5]);
    }

    private static PokeblockColor ColorOf(IReadOnlyList<BlenderBerry> berries, int[] flavors, int negatives)
    {
        int zero = 0;
        for (int i = 0; i < 5; i++)
            if (flavors[i] == 0) zero++;
        // All five flavours nothing, four or five taken away, or two of one berry: black
        if (zero == 5 || negatives > 3) return PokeblockColor.Black;
        for (int i = 0; i < berries.Count; i++)
            for (int j = 0; j < berries.Count; j++)
                if (i != j && berries[i].Name == berries[j].Name) return PokeblockColor.Black;

        int numFlavors = 0;
        for (int i = 0; i < 5; i++)
            if (flavors[i] > 0) numFlavors++;
        if (numFlavors > 3) return PokeblockColor.White;
        if (numFlavors == 3) return PokeblockColor.Gray;
        for (int i = 0; i < 5; i++)
            if (flavors[i] > 50) return PokeblockColor.Gold;

        if (numFlavors == 1)
        {
            if (flavors[0] > 0) return PokeblockColor.Red;
            if (flavors[1] > 0) return PokeblockColor.Blue;
            if (flavors[2] > 0) return PokeblockColor.Pink;
            if (flavors[3] > 0) return PokeblockColor.Green;
            if (flavors[4] > 0) return PokeblockColor.Yellow;
        }
        if (numFlavors == 2)
        {
            var present = Enumerable.Range(0, 5).Where(i => flavors[i] > 0).ToArray();
            // The stronger flavour (the first on a tie) gives the colour
            int stronger = flavors[present[0]] >= flavors[present[1]] ? present[0] : present[1];
            return stronger switch
            {
                0 => PokeblockColor.Purple,
                1 => PokeblockColor.Indigo,
                2 => PokeblockColor.Brown,
                3 => PokeblockColor.LiteBlue,
                _ => PokeblockColor.Olive
            };
        }
        return PokeblockColor.None;
    }

    // gPokeblockFlavorCompatibilityTable: for each nature, how it takes each flavour (+1 liked, -1 disliked), spicy to sour
    private static readonly sbyte[,] Compatibility =
    {
        { 0, 0, 0, 0, 0 }, { 1, 0, 0, 0, -1 }, { 1, 0, -1, 0, 0 }, { 1, -1, 0, 0, 0 }, { 1, 0, 0, -1, 0 },
        { -1, 0, 0, 0, 1 }, { 0, 0, 0, 0, 0 }, { 0, 0, -1, 0, 1 }, { 0, -1, 0, 0, 1 }, { 0, 0, 0, -1, 1 },
        { -1, 0, 1, 0, 0 }, { 0, 0, 1, 0, -1 }, { 0, 0, 0, 0, 0 }, { 0, -1, 1, 0, 0 }, { 0, 0, 1, -1, 0 },
        { -1, 1, 0, 0, 0 }, { 0, 1, 0, 0, -1 }, { 0, 1, -1, 0, 0 }, { 0, 0, 0, 0, 0 }, { 0, 1, 0, -1, 0 },
        { -1, 0, 0, 1, 0 }, { 0, 0, 0, 1, -1 }, { 0, 0, -1, 1, 0 }, { 0, -1, 0, 1, 0 }, { 0, 0, 0, 0, 0 }
    };

    /// <summary>
    /// How much a nature likes a Pokéblock (<c>PokeblockGetGain</c>): each flavour it has, at its level, counted for the
    /// nature, liked as a plus and disliked as a minus. Positive when it is liked, negative when not, 0 when the nature
    /// has no taste or the flavours cancel.
    /// </summary>
    public static int Gain(Nature nature, Pokeblock block)
    {
        int total = 0;
        for (int i = 0; i < 5; i++)
        {
            int level = block[(Flavor)i];
            if (level > 0) total += level * Compatibility[(int)nature, i];
        }
        return total;
    }

    /// <summary>
    /// The colour of Pokéblock a nature likes best (<c>GetPokeblockNameByMonNature</c>, Mossdeep's black belt): the colour of
    /// the one flavour it likes (spicy red, dry blue, sweet pink, bitter green, sour yellow); the five natures with no taste
    /// like none (<see cref="PokeblockColor.None"/>).
    /// </summary>
    public static PokeblockColor FavouriteOf(Nature nature)
    {
        for (int i = 0; i < 5; i++)
            if (Compatibility[(int)nature, i] > 0)
                return i switch { 0 => PokeblockColor.Red, 1 => PokeblockColor.Blue, 2 => PokeblockColor.Pink, 3 => PokeblockColor.Green, _ => PokeblockColor.Yellow };
        return PokeblockColor.None;
    }

    /// <summary>How a nature takes the Pokéblock, as the message says: gladly, plainly, or with disdain.</summary>
    public static PoffinTaste TasteFor(Nature nature, Pokeblock block)
    {
        int gain = Gain(nature, block);
        return gain == 0 ? PoffinTaste.Neutral : gain > 0 ? PoffinTaste.Liked : PoffinTaste.Disliked;
    }

    /// <summary>Whether the Pokémon will eat one: not an Egg, and not once its sheen is full.</summary>
    public static bool WouldEat(Pokemon p) => !p.IsEgg && p.Sheen < Poffins.MaxCondition;

    /// <summary>
    /// What eating a Pokéblock does: each flavour's level is added to its quality and the feel to the sheen, and the
    /// whole Pokémon takes the block's gain for its nature (the flavours it likes and dislikes) on every quality, each
    /// held to 0 and 255. As the original's feeding does, from what its menus show; the caller asks
    /// <see cref="WouldEat"/> first.
    /// </summary>
    public static void Feed(Pokeblock block, Pokemon p)
    {
        int gain = Gain(p.Nature, block);
        int Next(int stat, int level) => Math.Clamp(stat + level + gain, 0, Poffins.MaxCondition);
        p.Cool = Next(p.Cool, block.Spicy);
        p.Beauty = Next(p.Beauty, block.Dry);
        p.Cute = Next(p.Cute, block.Sweet);
        p.Smart = Next(p.Smart, block.Bitter);
        p.Tough = Next(p.Tough, block.Sour);
        p.Sheen = Math.Clamp(p.Sheen + block.ShownFeel, 0, Poffins.MaxCondition);
    }
}
