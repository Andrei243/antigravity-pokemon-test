using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>One species in a Pokédex's list: the number it has in that Pokédex.</summary>
public readonly record struct PokedexEntry(int Number, PokemonSpecies Species);

/// <summary>
/// One look of a species the player has seen (plan 08 · P19): its model's name (<see cref="Pokemon.ModelName"/>, the
/// form's Showdown name or the species') and its gender. The original's Pokédex keeps which genders, which of Unown's
/// letters, seas, cloaks and formes have been seen (<c>Pokedex_Encounter</c>); this one keeps every form of every species.
/// </summary>
public readonly record struct SeenLook(string Name, Gender Gender)
{
    /// <summary>
    /// The look it is drawn in (plan 09 · L8): a female of a species with a female look of her own is drawn as hers
    /// (<c>Pikachu+f</c>), a shiny look keeps its colours; anyone else as <see cref="Name"/>. What is read of the data
    /// (its name, its entries) still goes by <see cref="Name"/>.
    /// </summary>
    public string Look => PokemonLook.Of(Name, Gender == Gender.Female, PokemonLook.IsShiny(Name));
}

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

    /// <summary>The species caught shiny, by National number (plan 09 · L1): an entry can show their shiny look. Saved as a list of numbers.</summary>
    public HashSet<int> CaughtShiny { get; } = new();

    /// <summary>A species caught shiny: caught, and its shiny look known (a save's list, read back).</summary>
    public void RegisterCaughtShiny(int dexNumber)
    {
        RegisterCaught(dexNumber);
        CaughtShiny.Add(dexNumber);
    }

    /// <summary>Whether one of a species has been caught shiny.</summary>
    public bool IsCaughtShiny(int dexNumber) => CaughtShiny.Contains(dexNumber);

    /// <summary>How many of the games' species have been seen; ours (plan 03 · D14) count toward no total of the Pokédex's.</summary>
    public int SeenCount => SeenSpecies.Count(n => n < PokemonDatabase.OursFrom);

    /// <summary>How many of the games' species have been caught; ours count toward no total.</summary>
    public int CaughtCount => CaughtSpecies.Count(n => n < PokemonDatabase.OursFrom);

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

    // ------------------------------------------------------------------ the looks seen

    private readonly Dictionary<int, List<SeenLook>> looks = new();

    /// <summary>
    /// The first Spinda met's personality, from which its spots are drawn (the original keeps it from the first
    /// sighting, whatever the Pokédex can show yet); null until one has been met.
    /// </summary>
    public uint? SpindaPersonality { get; private set; }

    /// <summary>Whether the Pokédex keeps the first personality met of a species: Spinda's, whose spots are drawn from it.</summary>
    public static bool KeepsPersonality(PokemonSpecies species) => species.Name == "Spinda";

    /// <summary>The looks of a species the player has seen, in the order they were first seen (Unown's letters in the order met); empty for a species met with no look recorded.</summary>
    public IReadOnlyList<SeenLook> LooksOf(int dexNumber) => looks.TryGetValue(dexNumber, out var list) ? list : System.Array.Empty<SeenLook>();

    /// <summary>A Pokémon seen: its species, and its look (plan 08 · P19). An Egg is seen as its species and has no look yet.</summary>
    public void RegisterSeen(Pokemon pokemon)
    {
        if (pokemon.IsEgg) { RegisterSeen(pokemon.Species.DexNumber); return; }
        RegisterSeen(pokemon.Species, pokemon.Form, pokemon.Gender, pokemon.Personality);
    }

    /// <summary>A Pokémon caught: seen, with its look, and caught.</summary>
    public void RegisterCaught(Pokemon pokemon)
    {
        RegisterSeen(pokemon);
        RegisterCaught(pokemon.Species.DexNumber);
        if (pokemon.IsShiny && !pokemon.IsEgg) CaughtShiny.Add(pokemon.Species.DexNumber);
    }

    /// <summary>A look met without a Pokémon in hand (the battle log's <see cref="Battle.Seen"/>: a species, its form and gender).</summary>
    public void RegisterSeen(PokemonSpecies species, string? form, Gender gender, uint? personality = null)
    {
        SeenSpecies.Add(species.DexNumber);
        AddLook(species.DexNumber, new SeenLook(form ?? species.Name, gender));
        if (personality != null && KeepsPersonality(species)) SpindaPersonality ??= personality;
    }

    private void AddLook(int dexNumber, SeenLook look)
    {
        if (!looks.TryGetValue(dexNumber, out var list)) looks[dexNumber] = list = new List<SeenLook>();
        if (!list.Contains(look)) list.Add(look);
    }

    /// <summary>Restores the looks a save kept, in order, and the first Spinda's personality; a species they name is seen.</summary>
    public void RestoreLooks(IEnumerable<(int Dex, string Name, Gender Gender)> saved, uint? spinda)
    {
        foreach (var (dex, name, gender) in saved)
        {
            SeenSpecies.Add(dex);
            AddLook(dex, new SeenLook(name, gender));
        }
        SpindaPersonality ??= spinda;
    }

    /// <summary>
    /// A save from before the looks were kept: the Pokémon the player has (team, boxes, Day Care) give theirs, and every
    /// other species seen its own form, no gender known. Never takes away or reorders a look already there.
    /// </summary>
    public void RebuildLooksFrom(IEnumerable<Pokemon> owned)
    {
        foreach (var pokemon in owned) RegisterSeen(pokemon);
        foreach (int dex in SeenSpecies)
            if (!looks.ContainsKey(dex) && PokemonDatabase.GetByDex(dex) is { } species)
                AddLook(dex, new SeenLook(species.Name, Gender.Genderless));
    }

    /// <summary>Every look, species by species in number order, then in the order seen: what a save keeps.</summary>
    public IEnumerable<(int Dex, SeenLook Look)> AllLooks() => looks.OrderBy(kv => kv.Key).SelectMany(kv => kv.Value.Select(l => (kv.Key, l)));

    public void Clear()
    {
        SeenSpecies.Clear();
        CaughtSpecies.Clear();
        CaughtShiny.Clear();
        looks.Clear();
        SpindaPersonality = null;
        NationalUnlocked = false;
        Diplomas.Clear();
        entered.Clear();
        entered.Add(Sinnoh);
    }

    public bool IsSeen(int dexNumber) => SeenSpecies.Contains(dexNumber);
    public bool IsCaught(int dexNumber) => CaughtSpecies.Contains(dexNumber);

    // ------------------------------------------------------------------ the Pokédexes

    private static readonly ConcurrentDictionary<string, IReadOnlyList<PokedexEntry>> lists = new();

    /// <summary>
    /// The species of a Pokédex in its own order, each with the number it shows there (empty for an identifier no species
    /// is numbered in). The National one lists the species of our own after the 1,025 (plan 03 · D14), which its
    /// totals and its diploma leave out (<see cref="Goal"/>, <see cref="SeenCount"/>); no regional one lists them.
    /// </summary>
    public static IReadOnlyList<PokedexEntry> Entries(string dex) => lists.GetOrAdd(dex, id => id == National
        ? PokemonDatabase.Everyone().OrderBy(s => s.DexNumber).Select(s => new PokedexEntry(s.DexNumber, s)).ToList()
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
        ? (Entries(dex).Select(e => e.Species).Where(s => !s.Mythical && !s.IsOurs).ToList(), true)
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
