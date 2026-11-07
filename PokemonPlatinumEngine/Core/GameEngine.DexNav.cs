using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The DexNav as the game plays it (plan 10 · F11, F12; <see cref="DexNavRules"/>): USE switches it on or off; on,
/// a step on foot now and then finds a Pokémon hiding in the grass in sight (a rustle), which the player creeps up on
/// with Run held (<see cref="Pace.Sneak"/>) and meets by stepping onto it, with the search's bonus; running, riding or
/// hopping near it scares it off and breaks the chain. Off, nothing here runs or draws. The rustle is not drawn yet
/// (its cell of <c>LifeArt</c> waits for a session that can look at it), and the item is given by nobody yet.
/// </summary>
public partial class GameEngine
{
    /// <summary>The Pokémon the DexNav has found in the grass, or null.</summary>
    private Rustle? rustle;

    /// <summary>The species of the battle the DexNav's find began, for the chain as it ends.</summary>
    private string? dexNavMet;

    /// <summary>The DexNav's find in the grass now, for a tool (the harness, the tests).</summary>
    internal Rustle? DexNavRustle => rustle;

    /// <summary>The DexNav is in the bag (plan 10 · F11): the National Pokédex gives it, and a save that has the one is given the other.</summary>
    private bool HasTheDexNav => ItemDatabase.Get(DexNavRules.Item) is { } item && flow.Bag.GetQuantity(item) > 0;

    /// <summary>The National Pokédex's script gives the DexNav, once.</summary>
    private void GiveTheDexNav()
    {
        if (ItemDatabase.Get(DexNavRules.Item) is { } item && !HasTheDexNav) flow.Bag.AddItem(item, 1);
    }

    /// <summary>A save that has the National Pokédex and no DexNav is owed it as it loads (no story version).</summary>
    private void OweTheDexNav()
    {
        if (flow.Pokedex.NationalUnlocked) GiveTheDexNav();
    }

    /// <summary>
    /// The Pokédex's HERE list (plan 10 · F11): what lives at the tile the player stands on, by the hour, with what the
    /// Pokédex knows of each and its search level; null without the DexNav, empty where the place is no streamed overworld.
    /// </summary>
    internal IReadOnlyList<DexNavEntry>? DexNavHereList()
    {
        if (!HasTheDexNav) return null;
        if (!flow.Map.IsStreamed || Habitats.Of(Here) is not { } habitats) return Array.Empty<DexNavEntry>();
        return DexNavRules.Here(habitats, flow.Player.GridX, flow.Player.GridY, GameClock.Now, flow.Pokedex, flow.DexNav);
    }

    /// <summary>The DexNav is switched on, or off; the find in sight goes with it.</summary>
    private void SwitchDexNav()
    {
        flow.DexNav.On = !flow.DexNav.On;
        if (!flow.DexNav.On) rustle = null;
        flow.Player.SneaksInsteadOfRunning = false;
        ShowNotification(Text.Of(flow.DexNav.On ? "engine.dexNav.on" : "engine.dexNav.off"));
    }

    /// <summary>Whether the DexNav listens here: switched on, outdoors or in a cave, never below ground, in the Great Marsh's game or the Bug-Catching Contest.</summary>
    private bool DexNavHere => flow.DexNav.On && !IsUnderground && !InBugContest && !flow.Safari.Active && !flow.Map.IsIndoors;

    /// <summary>
    /// A step with the DexNav on: the find in sight stays, flees or is met; with none, one may be found. True when a
    /// battle begins.
    /// </summary>
    private bool DexNavStep()
    {
        if (!DexNavHere)
        {
            rustle = null;
            flow.Player.SneaksInsteadOfRunning = false;
            return false;
        }
        int px = flow.Player.GridX, py = flow.Player.GridY;
        if (rustle is { } found)
        {
            var pace = flow.Player.IsSneaking ? Overworld.Pace.Sneak : flow.Player.IsRunning ? Overworld.Pace.Fast : Overworld.Pace.Walk;
            switch (DexNavRules.AfterStep(found, px, py, pace, flow.Player.Mode, flow.Player.JustLanded))
            {
                case RustleAfterStep.Reached:
                    rustle = null;
                    flow.Player.SneaksInsteadOfRunning = false;
                    return MeetTheFind(found);
                case RustleAfterStep.Scared:
                    rustle = null;
                    flow.DexNav.Break();
                    ShowNotification(Text.Of("engine.dexNav.scared"));
                    break;
                case RustleAfterStep.Lost:
                    rustle = null;
                    break;
            }
        }
        else if (flow.Player.Mode == TravelMode.OnFoot && DexNavRules.Finds(fieldRandom))
            rustle = DexNavRules.Place(flow.Map, px, py, fieldRandom,
                (x, y) => flow.Map.DrawOutWild(x, y, false, flow.Player.Lead, EncounterMomentNow()));
        flow.Player.SneaksInsteadOfRunning = rustle != null;
        return false;
    }

    /// <summary>The find is met: the Pokémon the grass would have given, with the search's bonus.</summary>
    private bool MeetTheFind(Rustle found)
    {
        if (!flow.Party.HasUsablePokemon) return false;
        var area = flow.Map.AreaAt(flow.Player.GridX, flow.Player.GridY);
        var wild = MakeWild(found.Entry, area);
        string species = wild.Species.Name;
        DexNavRules.Bonus(wild, flow.DexNav.LevelOf(species), flow.DexNav.ChainFor(species), Ruleset.Current, fieldRandom);
        flow.FieldEncounter = true;
        flow.Player.Encounters.Reset();
        dexNavMet = species;
        MeetWildPokemon(wild, BattleKind.Normal, nuzlockeCatch: MeetUnderTheRun(wild));
        return true;
    }

    /// <summary>A battle the DexNav's find began has ended: beaten or caught, its level and the chain go on; otherwise the chain is broken.</summary>
    private void DexNavAfterBattle(BattleEngine fought, bool defeat)
    {
        if (dexNavMet is not { } species) return;
        dexNavMet = null;
        bool won = fought.Result == BattleResult.PlayerVictory, caught = fought.Result == BattleResult.EnemyCaught;
        if (!defeat && (won || caught)) flow.DexNav.Found(species);
        else flow.DexNav.Break();
    }
}
