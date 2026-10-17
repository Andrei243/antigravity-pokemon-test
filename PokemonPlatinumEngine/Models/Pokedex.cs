using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>One species in a Pokédex's list: the number it has in that Pokédex.</summary>
public readonly record struct PokedexEntry(int Number, PokemonSpecies Species);

/// <summary>
/// What the player has seen and caught, by national number, and what the Pokédex itself has become: the National
/// upgrade and the diplomas for a complete Pokédex (plan 03 · D10, plan 18 · W3). Seen and caught are shared by
/// every Pokédex (a species seen in Sinnoh is seen in Kanto's list if Kanto lists it); a Pokédex is a PokeAPI
/// identifier (<see cref="National"/>, <see cref="Sinnoh"/>, <c>kanto</c>…) numbering the species it lists
/// (<see cref="PokemonSpecies.DexNumbers"/>). Its rules follow the games': a regional Pokédex is complete when all of
/// its species have been seen, the National one when every species but the mythical ones (given only at events) has
/// been caught; what opens the National one is the region's own rule (<see cref="NationalUnlock"/>).
/// </summary>
public class Pokedex
{
    /// <summary>The National Pokédex's identifier.</summary>
    public const string National = "national";

    /// <summary>Platinum's Sinnoh Pokédex (210 species): PokeAPI's <c>extended-sinnoh</c>.</summary>
    public const string Sinnoh = "extended-sinnoh";

    public HashSet<int> SeenSpecies { get; } = new();
    public HashSet<int> CaughtSpecies { get; } = new();

    public int SeenCount => SeenSpecies.Count;
    public int CaughtCount => CaughtSpecies.Count;

    /// <summary>Whether the National Pokédex has been opened (<see cref="UnlockNational"/>).</summary>
    public bool NationalUnlocked { get; private set; }

    /// <summary>The Pokédexes whose diploma the player has been given, by dex identifier.</summary>
    public HashSet<string> Diplomas { get; } = new();

    /// <summary>The regional Pokédexes of the regions the player has entered (Sinnoh's from the start), in the order they were.</summary>
    private readonly List<string> entered = new() { Sinnoh };

    public void RegisterSeen(int dexNumber)
    {
        SeenSpecies.Add(dexNumber);
    }

    public void RegisterCaught(int dexNumber)
    {
        SeenSpecies.Add(dexNumber);
        CaughtSpecies.Add(dexNumber);
    }

    public void Clear()
    {
        SeenSpecies.Clear();
        CaughtSpecies.Clear();
        NationalUnlocked = false;
        Diplomas.Clear();
        entered.Clear();
        entered.Add(Sinnoh);
    }

    public bool IsSeen(int dexNumber) => SeenSpecies.Contains(dexNumber);
    public bool IsCaught(int dexNumber) => CaughtSpecies.Contains(dexNumber);

    // ------------------------------------------------------------------ the Pokédexes

    private static readonly ConcurrentDictionary<string, IReadOnlyList<PokedexEntry>> lists = new();

    /// <summary>The species of a Pokédex in its own order, each with the number it shows there (empty for an identifier no species is numbered in).</summary>
    public static IReadOnlyList<PokedexEntry> Entries(string dex) => lists.GetOrAdd(dex, id => id == National
        ? PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).Select(s => new PokedexEntry(s.DexNumber, s)).ToList()
        : PokemonDatabase.GetAll().Where(s => s.DexNumbers.ContainsKey(id)).OrderBy(s => s.DexNumbers[id]).ThenBy(s => s.DexNumber)
            .Select(s => new PokedexEntry(s.DexNumbers[id], s)).ToList());

    /// <summary>The key the text table names a Pokédex by (<c>pokedex.mode.sinnoh</c>): its identifier, but for Sinnoh's, which is the region's name.</summary>
    public static string KeyOf(string dex) => dex == Sinnoh ? "sinnoh" : dex.Replace("-", "");

    /// <summary>The key a diploma is saved under: the names the first saves used for Sinnoh's and the National one, else the identifier.</summary>
    public static string DiplomaKey(string dex) => dex switch { Sinnoh => "Sinnoh", National => "National", _ => dex };

    /// <summary>The Pokédex a saved diploma key names; the identifier itself for any other.</summary>
    public static string FromDiplomaKey(string key) => key switch { "Sinnoh" => Sinnoh, "National" => National, _ => key };

    /// <summary>The region whose own Pokédex this is (<c>RegionFile.Dex</c>); null for the National one or one no region plays by.</summary>
    public static Region? RegionOf(string dex) => dex == National ? null : RegionDatabase.All.FirstOrDefault(r => r.Dex == dex);

    /// <summary>Notes that the player has entered a region, which gives them its regional Pokédex (a region with none gives nothing).</summary>
    public void Enter(Region region)
    {
        if (region.Dex is { } dex && !entered.Contains(dex)) entered.Add(dex);
    }

    /// <summary>The regional Pokédexes the player has, in the order of the chain of regions.</summary>
    public IReadOnlyList<string> Regional => RegionDatabase.All.Select(r => r.Dex).Where(d => d != null && entered.Contains(d)).Select(d => d!).Distinct().ToList();

    /// <summary>The Pokédexes the player can open: the regional ones of the regions they have entered, then the National one once it is unlocked.</summary>
    public IReadOnlyList<string> Dexes => NationalUnlocked ? Regional.Append(National).ToList() : Regional;

    public int SeenIn(string dex) => dex == National ? SeenCount : Entries(dex).Count(e => IsSeen(e.Species.DexNumber));
    public int CaughtIn(string dex) => dex == National ? CaughtCount : Entries(dex).Count(e => IsCaught(e.Species.DexNumber));

    // ------------------------------------------------------------------ the upgrade and the diplomas

    /// <summary>
    /// Whether the National Pokédex would be opened now by <paramref name="region"/>'s rule (<see cref="Region.NationalUnlock"/>):
    /// Sinnoh's is as in Platinum, after its Hall of Fame once every species of the Sinnoh Pokédex has been seen; Kanto's is
    /// sixty species caught. The story's post-game asks this and calls <see cref="UnlockNational"/> (plan 02).
    /// </summary>
    public bool CanUnlockNational(Story.StoryState story, Region region)
    {
        if (NationalUnlocked) return false;
        var rule = region.NationalUnlock;
        return rule.Rule switch
        {
            NationalUnlockRule.HallOfFameAndSeenAll => story.IsRegionComplete(region.Id) && region.Dex is { } dex && IsComplete(dex),
            NationalUnlockRule.Caught => CaughtCount >= rule.Count,
            _ => story.IsRegionComplete(region.Id)
        };
    }

    /// <summary>Platinum's rule, Sinnoh's: the Hall of Fame and every species of its Pokédex seen.</summary>
    public bool CanUnlockNational(Story.StoryState story) => CanUnlockNational(story, RegionDatabase.Get("Sinnoh")!);

    public void UnlockNational() => NationalUnlocked = true;

    /// <summary>The species a Pokédex's diploma asks for, and whether they must be caught or only seen.</summary>
    public static (IReadOnlyList<PokemonSpecies> Species, bool Caught) Goal(string dex) => dex == National
        ? (Entries(dex).Select(e => e.Species).Where(s => !s.Mythical).ToList(), true)
        : (Entries(dex).Select(e => e.Species).ToList(), false);

    /// <summary>How far the player is toward a Pokédex's diploma: how many of the species it asks for they have.</summary>
    public (int Have, int Need) Progress(string dex)
    {
        var (species, caught) = Goal(dex);
        return (species.Count(s => caught ? IsCaught(s.DexNumber) : IsSeen(s.DexNumber)), species.Count);
    }

    public bool IsComplete(string dex) => Progress(dex) is var (have, need) && need > 0 && have == need;

    /// <summary>
    /// The diplomas that are due and not yet given, which are recorded as given: those of the Pokédexes the player has
    /// (the regional ones of the regions entered, and the National one once that Pokédex is unlocked).
    /// </summary>
    public List<string> AwardDiplomas()
    {
        var due = Dexes.Where(d => !Diplomas.Contains(d) && IsComplete(d)).ToList();
        foreach (var dex in due) Diplomas.Add(dex);
        return due;
    }

    /// <summary>The regional Pokédexes a save says the player has entered (Sinnoh's always), in the order of the chain.</summary>
    public IReadOnlyList<string> Entered => entered;

    /// <summary>Restores what a save says the Pokédex has become.</summary>
    public void Restore(bool nationalUnlocked, IEnumerable<string> diplomas, IEnumerable<string>? entered = null)
    {
        NationalUnlocked = nationalUnlocked;
        Diplomas.Clear();
        foreach (var d in diplomas) Diplomas.Add(d);
        this.entered.Clear();
        this.entered.Add(Sinnoh);
        foreach (var d in entered ?? System.Array.Empty<string>()) if (!this.entered.Contains(d)) this.entered.Add(d);
    }
}
