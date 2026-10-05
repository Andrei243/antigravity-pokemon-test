using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>The two Pokédexes: Platinum's own 210 species, and every species, which an upgrade after the Hall of Fame opens.</summary>
public enum PokedexMode { Sinnoh, National }

/// <summary>One species in a Pokédex's list: the number it has in that Pokédex.</summary>
public readonly record struct PokedexEntry(int Number, PokemonSpecies Species);

/// <summary>
/// What the player has seen and caught, by national number, and what the Pokédex itself has become: the National
/// upgrade and the diplomas for a complete Pokédex (plan 03 · D10). Its rules follow Platinum: the upgrade needs the
/// Hall of Fame and every Sinnoh species seen; the Sinnoh Pokédex is complete when all of its species have been seen,
/// the National one when every species but the mythical ones (given only at events) has been caught.
/// </summary>
public class Pokedex
{
    public HashSet<int> SeenSpecies { get; } = new();
    public HashSet<int> CaughtSpecies { get; } = new();

    public int SeenCount => SeenSpecies.Count;
    public int CaughtCount => CaughtSpecies.Count;

    /// <summary>Whether the National Pokédex has been opened (<see cref="UnlockNational"/>).</summary>
    public bool NationalUnlocked { get; private set; }

    /// <summary>The Pokédexes whose diploma the player has been given.</summary>
    public HashSet<PokedexMode> Diplomas { get; } = new();

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
    }

    public bool IsSeen(int dexNumber) => SeenSpecies.Contains(dexNumber);
    public bool IsCaught(int dexNumber) => CaughtSpecies.Contains(dexNumber);

    // ------------------------------------------------------------------ the two Pokédexes

    private static IReadOnlyList<PokedexEntry>? sinnoh, national;

    /// <summary>The species of a Pokédex in its own order, each with the number it shows there.</summary>
    public static IReadOnlyList<PokedexEntry> Entries(PokedexMode mode) => mode == PokedexMode.Sinnoh
        ? sinnoh ??= PokemonDatabase.GetAll().Where(s => s.SinnohNumber != null).OrderBy(s => s.SinnohNumber)
            .Select(s => new PokedexEntry(s.SinnohNumber!.Value, s)).ToList()
        : national ??= PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).Select(s => new PokedexEntry(s.DexNumber, s)).ToList();

    /// <summary>The Pokédexes the player can open: the Sinnoh one, and the National one once it is unlocked.</summary>
    public IReadOnlyList<PokedexMode> Modes => NationalUnlocked ? new[] { PokedexMode.Sinnoh, PokedexMode.National } : new[] { PokedexMode.Sinnoh };

    public int SeenIn(PokedexMode mode) => mode == PokedexMode.National ? SeenCount : Entries(mode).Count(e => IsSeen(e.Species.DexNumber));
    public int CaughtIn(PokedexMode mode) => mode == PokedexMode.National ? CaughtCount : Entries(mode).Count(e => IsCaught(e.Species.DexNumber));

    // ------------------------------------------------------------------ the upgrade and the diplomas

    /// <summary>
    /// Whether Professor Rowan would upgrade the Pokédex now, as in Platinum: after the Sinnoh Hall of Fame, once
    /// every species of the Sinnoh Pokédex has been seen. The story's post-game asks this and calls
    /// <see cref="UnlockNational"/> (plan 02).
    /// </summary>
    public bool CanUnlockNational(Story.StoryState story) =>
        !NationalUnlocked && story.IsRegionComplete("Sinnoh") && IsComplete(PokedexMode.Sinnoh);

    public void UnlockNational() => NationalUnlocked = true;

    /// <summary>The species a Pokédex's diploma asks for, and whether they must be caught or only seen.</summary>
    public static (IReadOnlyList<PokemonSpecies> Species, bool Caught) Goal(PokedexMode mode) => mode == PokedexMode.Sinnoh
        ? (Entries(mode).Select(e => e.Species).ToList(), false)
        : (Entries(mode).Select(e => e.Species).Where(s => !s.Mythical).ToList(), true);

    /// <summary>How far the player is toward a Pokédex's diploma: how many of the species it asks for they have.</summary>
    public (int Have, int Need) Progress(PokedexMode mode)
    {
        var (species, caught) = Goal(mode);
        return (species.Count(s => caught ? IsCaught(s.DexNumber) : IsSeen(s.DexNumber)), species.Count);
    }

    public bool IsComplete(PokedexMode mode) => Progress(mode) is var (have, need) && have == need;

    /// <summary>
    /// The diplomas that are due and not yet given, which are recorded as given: the Sinnoh one (open to anyone) and
    /// the National one once that Pokédex is unlocked.
    /// </summary>
    public List<PokedexMode> AwardDiplomas()
    {
        var due = Modes.Where(m => !Diplomas.Contains(m) && IsComplete(m)).ToList();
        foreach (var mode in due) Diplomas.Add(mode);
        return due;
    }

    /// <summary>Restores what a save says the Pokédex has become.</summary>
    public void Restore(bool nationalUnlocked, IEnumerable<PokedexMode> diplomas)
    {
        NationalUnlocked = nationalUnlocked;
        Diplomas.Clear();
        foreach (var d in diplomas) Diplomas.Add(d);
    }
}
