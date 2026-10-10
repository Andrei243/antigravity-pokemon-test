using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.SuperContests;

/// <summary>
/// The Fashion Case (plan 06 · R17a; the original's <c>FashionCase</c>, <c>src/unk_020298BC.c</c>): the accessories a
/// Pokémon is dressed up with for the Visual Competition, how many of each. The first sixty-one kinds are kept up to
/// nine of each; the rest (the parasol, the barrettes, the stages) come one of a kind. Saved. Its backdrops are left
/// for whoever builds the dressing-up photographs (plan 06 · R17b). No drawing, no input.
/// </summary>
public sealed class FashionCase
{
    /// <summary><c>MAX_NON_UNIQUE_ACCESSORIES_PER_TYPE</c> and <c>MAX_UNIQUE_ACCESSORIES_PER_TYPE</c>.</summary>
    public const int MostOfAKind = 9, MostOfAUnique = 1;

    private readonly int[] counts = new int[SuperContestDatabase.Accessories.Count];

    /// <summary>The most of one accessory the case holds (<c>Accessory_CanHaveMultiple</c>).</summary>
    public static int Limit(int accessory) => accessory < SuperContestDatabase.FirstUniqueAccessory ? MostOfAKind : MostOfAUnique;

    public int Count(int accessory) => accessory >= 0 && accessory < counts.Length ? counts[accessory] : 0;

    /// <summary>How many accessories are in the case altogether, and whether there is none.</summary>
    public int Total => counts.Sum();
    public bool IsEmpty => Total == 0;

    /// <summary><c>FashionCase_CanFitAccessoryCount</c>.</summary>
    public bool CanFit(int accessory, int count = 1) =>
        accessory >= 0 && accessory < counts.Length && Count(accessory) + count <= Limit(accessory);

    /// <summary>Puts accessories in, all of them or none; false when they wouldn't fit.</summary>
    public bool Add(int accessory, int count = 1)
    {
        if (count <= 0 || !CanFit(accessory, count)) return false;
        counts[accessory] += count;
        return true;
    }

    public bool Add(string name, int count = 1) => Add(SuperContestDatabase.Accessory(name), count);

    /// <summary>The kinds in the case with how many of each, in the original's order.</summary>
    public IEnumerable<(int Accessory, int Count)> Held =>
        Enumerable.Range(0, counts.Length).Where(a => counts[a] > 0).Select(a => (a, counts[a]));

    /// <summary>What a save keeps: each kind's name and how many.</summary>
    public Dictionary<string, int> ToSave() => Held.ToDictionary(h => SuperContestDatabase.AccessoryName(h.Accessory), h => h.Count);

    /// <summary>Takes on what a save kept; anything it doesn't know is left out.</summary>
    public void Load(IReadOnlyDictionary<string, int>? saved, IEnumerable<string>? savedBackdrops = null)
    {
        Array.Clear(counts);
        foreach (var (name, count) in saved ?? new Dictionary<string, int>())
        {
            int accessory = SuperContestDatabase.Accessory(name);
            if (accessory >= 0) counts[accessory] = Math.Clamp(count, 0, Limit(accessory));
        }
        backdrops.Clear();
        foreach (string name in savedBackdrops ?? Array.Empty<string>())
        {
            int backdrop = SuperContestDatabase.Backdrop(name);
            if (backdrop >= 0) backdrops.Add(backdrop);
        }
    }

    // ---------------------------------------------------------------- backdrops (plan 06 · R17b)

    // The original keeps a place for each backdrop the player has, in the order they came (BackdropFlags)
    private readonly List<int> backdrops = new();

    /// <summary><c>FashionCase_HasBackdrop</c>.</summary>
    public bool HasBackdrop(int backdrop) => backdrops.Contains(backdrop);

    /// <summary>The backdrops in the case, in the order they came (<c>FashionCase_GetTotalBackdrops</c> counts them).</summary>
    public IReadOnlyList<int> Backdrops => backdrops;

    /// <summary><c>FashionCase_AddBackdrop</c>: one of each at most; false for one already there, or that is none.</summary>
    public bool AddBackdrop(int backdrop)
    {
        if (backdrop < 0 || backdrop >= SuperContestDatabase.Backdrops.Count || HasBackdrop(backdrop)) return false;
        backdrops.Add(backdrop);
        return true;
    }

    /// <summary>What a save keeps of the backdrops: their names in the order they came.</summary>
    public List<string> BackdropsToSave() => backdrops.Select(SuperContestDatabase.BackdropName).ToList();

    /// <summary>
    /// What comes with the Fashion Case (<c>JubilifeCity_GiveFashionCaseAndAccessories</c>): five draws among the six
    /// fluffs, five among the six feathers and moustaches that follow them, the Dress Up backdrop, and two different
    /// ones of the eight after it (a draw again while it is the one just drawn).
    /// </summary>
    public void FillAsGiven(Random chance)
    {
        int fluff = SuperContestDatabase.Accessory("White Fluff"), feather = SuperContestDatabase.Accessory("Blue Feather");
        for (int i = 0; i < 5; i++) Add(fluff + chance.Next(6));
        for (int i = 0; i < 5; i++) Add(feather + chance.Next(6));
        AddBackdrop(0);
        int last = -1;
        for (int i = 0; i < 2; i++)
        {
            int drawn;
            do drawn = chance.Next(8);
            while (drawn == last);
            last = drawn;
            AddBackdrop(1 + drawn);
        }
    }
}
