using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumTests;

/// <summary>
/// What a scenario test is made of (plan 06, "Verification"): Pokémon whose stats are the same on every run, a
/// battle with its own generator and its own rules, and a turn played to its end. A scenario checks one rule; the
/// rolls it depends on are fixed by kind (<see cref="BattleRandom.Force"/>), never by counting calls, and the
/// numbers it expects are worked out from the original's formulas in a comment beside them.
/// </summary>
internal static class Scenario
{
    /// <summary>A move that does nothing, so the Pokémon across the field stays out of the way.</summary>
    public static readonly MoveData Idle = new() { Name = "Idle", Type = PokemonType.Normal, Category = MoveCategory.Status, MaxPP = 40, Target = MoveTarget.User };

    /// <summary>
    /// A Pokémon with no IVs, no EVs and a neutral nature: its stats are 2 × base × level / 100 + 5 (and + level + 5
    /// more for HP). It knows the moves named, or only <see cref="Idle"/>; its ability is its species' first.
    /// </summary>
    public static Pokemon Mon(string species, int level, params string[] moves)
    {
        var p = new Pokemon(PokemonDatabase.Get(species)!, level, Gender.Male, Nature.Hardy, false);
        p.Moves.Clear();
        foreach (var name in moves) p.Moves.Add(new Move(MoveDatabase.Get(name)));
        if (moves.Length == 0) p.Moves.Add(new Move(Idle));
        return p;
    }

    /// <summary>
    /// A battle's rolls with nothing left to chance that a test didn't ask for: no critical hits, the strongest
    /// hit of the range, and every move that can miss hitting. A test fixes or frees what it is about.
    /// </summary>
    public static BattleRandom Steady(uint seed = 1) =>
        new BattleRandom(seed).Force(RollKind.Critical, 1).Force(RollKind.Damage, 0).Force(RollKind.Accuracy, 0);

    /// <summary>A wild battle between two Pokémon, played up to the first choice of a move.</summary>
    public static BattleEngine Battle(Pokemon mine, Pokemon foe, BattleRandom? rolls = null, Ruleset? rules = null)
    {
        var party = new Party();
        party.Add(mine);
        var battle = new BattleEngine(new BattleSetup
        {
            PlayerParty = party, Inventory = new Inventory(), Pokedex = new Pokedex(),
            WildPokemon = new List<Pokemon> { foe }, Random = rolls ?? Steady(), Rules = rules ?? Ruleset.Platinum
        });
        Settle(battle);
        return battle;
    }

    /// <summary>
    /// Confirms messages until a menu opens or the battle ends; returns what was said. A question about a move to
    /// learn is read on like a message, which keeps every move the Pokémon knows.
    /// </summary>
    public static List<string> Settle(BattleEngine battle)
    {
        var said = new List<string>();
        for (int i = 0; i < 400 && !battle.IsBattleOver; i++)
        {
            if (battle.HUD.MenuState is not (BattleMenuState.Message or BattleMenuState.LearnMove)) break;
            if (battle.IsWaitingForConfirm) said.Add(battle.CurrentMessage);
            battle.ConfirmMessage();
            battle.Update(1f / 60f);
        }
        return said;
    }

    /// <summary>Plays a turn in which the player's Pokémon uses one of its moves; returns what was said.</summary>
    public static List<string> Turn(BattleEngine battle, int move = 0)
    {
        battle.SelectMove(move);
        return Settle(battle);
    }

    /// <summary>The damage of one hit, straight from the calculator, by these rolls and rules.</summary>
    public static int Damage(Pokemon attacker, Pokemon defender, string move, BattleRandom? rolls = null, Ruleset? rules = null) =>
        DamageCalculator.CalculateDamage(attacker, defender, attacker.Moves.First(m => m.Name == move), rolls ?? Steady(), rules ?? Ruleset.Platinum).Damage;
}
