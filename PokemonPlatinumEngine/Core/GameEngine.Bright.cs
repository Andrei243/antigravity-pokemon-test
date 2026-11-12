using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// What is seen in the grass as the game plays it (plan 38 · EX3; rules in <see cref="BrightPokemon"/> and
/// <see cref="CatchStreak"/>): a Pokémon in sight is made with the count's shiny rolls and the streak's bonus, and may be
/// bright; a battle met in sight goes on the streak or breaks it; a hook goes on the chain or breaks it. With the WILD POKéMON
/// row at HIDDEN none of it runs, and a Pokémon of the grass is made as it always was.
/// </summary>
public partial class GameEngine
{
    // The battle about to be fought was met in sight (plan 38 · EX3): its end goes on the catch streak or breaks it
    private bool metInSight;

    // The shiny rolls more the Pokémon a rod has just hooked has, by the chain of hooks before it (plan 38 · EX3)
    private int hookRolls;

    /// <summary>
    /// A Pokémon to stand in sight (plan 10 · F5): made as the grass makes one, then given the count's shiny rolls (from fifty
    /// of its species beaten or caught), the streak's bonus (from eleven of it caught in a row) and its brightness. The first
    /// two draw on the field's generator only from their steps; brightness draws nothing.
    /// </summary>
    internal Pokemon MakeSeenWild(WildEncounterEntry entry, MapArea? area)
    {
        var wild = MakeWild(entry, area);
        if (wild.IsEgg) return wild;
        var band = BrightPokemon.BandOf(BrightPokemon.CountOf(flow.Pokedex, wild.Species));
        if (!entry.Shiny && band.ShinyRolls > 0) wild.RerollForShiny(fieldRandom, band.ShinyRolls, entry.Gender, entry.Nature);
        if (flow.CatchStreak.StreakFor(wild.Species.Name) is >= CatchStreak.FirstStep and var streak)
            CatchStreak.Bonus(wild, streak, fieldRandom, entry.Gender, entry.Nature);
        BrightPokemon.Apply(wild, band);
        return wild;
    }

    /// <summary>
    /// A wild battle met in sight is over (from <c>EndBattle</c>): a catch goes on the streak (or begins it), the chained
    /// species running away breaks it, and anything else leaves it as it is.
    /// </summary>
    private void KeepTheStreak(BattleEngine fought)
    {
        if (!metInSight) return;
        metInSight = false;
        if (fought.EnemyPokemon is not { } foe) return;
        if (fought.Result == BattleResult.EnemyCaught) flow.CatchStreak.Caught(foe.Species.Name);
        else if (fought.Result == BattleResult.EnemyFled) flow.CatchStreak.Fled(foe.Species.Name);
    }

    /// <summary>
    /// What the field's corner shows of the streak and the chain (plan 38 · EX3): the streak with the WILD POKéMON row at
    /// VISIBLE and two or more caught in a row, the chain of hooks from two in a row while the player stands at its spot; null
    /// when neither shows.
    /// </summary>
    internal (string Species, int Streak, int Hooks)? StreakShown
    {
        get
        {
            var streak = flow.CatchStreak;
            int count = Settings.WildInSight && streak.Count >= 2 ? streak.Count : 0;
            int hooks = streak.Hooks >= 2 && streak.HookMap == flow.Map.Name && streak.HookX == flow.Player.GridX && streak.HookY == flow.Player.GridY ? streak.Hooks : 0;
            if (count == 0 && hooks == 0) return null;
            return (count > 0 ? streak.Species! : streak.HookSpecies!, count, hooks);
        }
    }

    /// <summary>Whether hidden items glint now: the options' row, read by the rules in force as the other later-game helps are.</summary>
    internal bool ItemGlintsOn => FieldHelp.On(Settings.ItemGlints, Data.Ruleset.Current.Preset);

    /// <summary>
    /// Hidden items within reach glint once every two seconds of the field's clock, and a bright Pokémon in sight shimmers
    /// at the same beat (plan 38 · EX3; style guide, "Life"). Nothing is drawn from the dice, and nothing glints in a room.
    /// </summary>
    private void KeepTheGlints(double before)
    {
        if (!Glints.Beats(before, world.Life.Now) || flow.State != GameState.Overworld) return;
        if (ItemGlintsOn && !flow.Map.IsIndoors)
            foreach (var (x, y) in Glints.Near(flow.Map, flow.Player.GridX, flow.Player.GridY, flow.Story))
                world.Life.ItemGlint(flow.Map, x, y);
        foreach (var seen in wildInSight.All)
            if (seen.Pokemon.Bright) world.Life.Shimmer(flow.Map, seen.Body.GridX, seen.Body.GridY);
    }

    /// <summary>
    /// A rod's cast is over (plan 38 · EX3): a Pokémon hooked goes on the chain at its spot or begins one, and a cast that
    /// hooks nothing breaks it. Returns the shiny rolls more the hooked Pokémon has (two for each hook before it).
    /// </summary>
    private int KeepTheChain(WildEncounterEntry? hooked)
    {
        if (hooked == null)
        {
            flow.CatchStreak.BreakChain();
            return 0;
        }
        int before = flow.CatchStreak.ChainFor(hooked.SpeciesName, flow.Map.Name, flow.Player.GridX, flow.Player.GridY);
        flow.CatchStreak.Hooked(hooked.SpeciesName, flow.Map.Name, flow.Player.GridX, flow.Player.GridY);
        return CatchStreak.HookRolls(before);
    }
}
