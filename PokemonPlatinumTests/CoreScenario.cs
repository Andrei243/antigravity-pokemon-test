using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// What a scenario on the battle's rules alone is made of (plan 06 · R3): a wild or a trainer battle with no
/// screen and both sides answered for from outside, so a test says exactly what each Pokémon does; a turn
/// played with each side's move; a switch; what was said, in order. <see cref="Scenario"/> has the Pokémon and
/// the dice.
/// </summary>
internal static class CoreScenario
{
    public static readonly Place Mine = new(BattleSide.Player, 0), Foe = new(BattleSide.Enemy, 0);

    /// <summary>The second places of a double battle.</summary>
    public static readonly Place Mine2 = new(BattleSide.Player, 1), Foe2 = new(BattleSide.Enemy, 1);

    /// <summary>A double battle against a trainer, both sides answered for from outside.</summary>
    public static BattleCore Doubles(Pokemon[] mine, Pokemon[] theirs, BattleRandom? rolls = null, Ruleset? rules = null)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100, DoubleBattle = true };
        foreach (var p in theirs) trainer.Party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Format = BattleFormat.Double, Random = rolls ?? Calm(), Rules = rules ?? Ruleset.Platinum,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        core.Start();
        return core;
    }

    /// <summary>A turn of a double battle: each place that is asked uses the move given for it, at the target given (null: whoever the rules pick).</summary>
    public static List<string> DoubleTurn(BattleCore core, params (Place Who, int Move, Place? At)[] actions)
    {
        var asked = Assert.IsType<ActionRequest>(core.Request).Places;
        var choices = actions.Where(a => asked.Contains(a.Who)).Select(a => BattleChoice.Fight(a.Who, a.Move, a.At)).ToList();
        core.Submit(choices);
        return Lines(core.TakeLog());
    }

    /// <summary>Nothing left to chance, side effects included: a test frees or fixes what it is about.</summary>
    public static BattleRandom Calm() => Steady().Force(RollKind.SideEffect, 99);

    /// <summary>A wild battle on the rules alone, both sides answered for from outside.</summary>
    public static BattleCore Wild(Pokemon mine, Pokemon foe, BattleRandom? rolls = null, Ruleset? rules = null,
        BattleWeather sky = BattleWeather.None, BattleTerrain ground = BattleTerrain.Plain, params Pokemon[] bench)
    {
        var party = new Party();
        party.Add(mine);
        foreach (var p in bench) party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, WildPokemon = new List<Pokemon> { foe }, Random = rolls ?? Calm(), Rules = rules ?? Ruleset.Platinum,
            Conditions = new BattleConditions { Weather = sky, Terrain = ground }, PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        core.Start();
        return core;
    }

    /// <summary>A battle against a trainer, both sides answered for from outside.</summary>
    public static BattleCore Against(Pokemon[] mine, Pokemon[] theirs, BattleRandom? rolls = null, Ruleset? rules = null)
    {
        var party = new Party();
        foreach (var p in mine) party.Add(p);
        var trainer = new Trainer { Id = "ace", Name = "Vera", TrainerClass = "Ace Trainer", PrizeMoney = 100 };
        foreach (var p in theirs) trainer.Party.Add(p);
        var core = new BattleCore(new CoreSetup
        {
            PlayerParty = party, Trainers = new List<Trainer> { trainer }, Random = rolls ?? Calm(), Rules = rules ?? Ruleset.Platinum,
            PlayerController = null, EnemyController = null, PlayerName = "Lucas"
        });
        core.Start();
        return core;
    }

    public static Pokemon Of(Gender gender, string species, int level, params string[] moves)
    {
        var p = new Pokemon(PokemonDatabase.Get(species)!, level, gender, Nature.Hardy, false);
        p.Moves.Clear();
        foreach (var name in moves) p.Moves.Add(new Move(MoveDatabase.Get(name)));
        if (moves.Length == 0) p.Moves.Add(new Move(Idle));
        return p;
    }

    /// <summary>Adds the move that does nothing after the Pokémon's own, so it can sit a turn out.</summary>
    public static Pokemon Idling(Pokemon p)
    {
        p.Moves.Add(new Move(Idle));
        return p;
    }

    public static Pokemon With(Pokemon p, string? ability = null, string? item = null)
    {
        if (ability != null) p.AbilityName = ability;
        if (item != null) p.HeldItem = ItemDatabase.Get(item) ?? throw new ArgumentException(item);
        return p;
    }

    public static List<string> Lines(IEnumerable<BattleEvent> log) => log.OfType<Said>().Select(s => s.Text).ToList();

    /// <summary>
    /// Plays a turn: each side uses the move in the given position, if it is asked (a Pokémon in the middle of a
    /// move isn't). Returns what was said.
    /// </summary>
    public static List<string> Turn(BattleCore core, int mine = 0, int foe = 0)
    {
        var asked = Assert.IsType<ActionRequest>(core.Request).Places;
        var choices = new List<BattleChoice>();
        if (asked.Contains(Mine)) choices.Add(BattleChoice.Fight(Mine, mine));
        if (asked.Contains(Foe)) choices.Add(BattleChoice.Fight(Foe, foe));
        core.Submit(choices);
        return Lines(core.TakeLog());
    }

    /// <summary>A turn in which the player's Pokémon is switched for the one at a place in the party.</summary>
    public static List<string> SwitchTo(BattleCore core, int partyIndex, int foe = 0)
    {
        core.Submit(BattleChoice.Switch(Mine, partyIndex), BattleChoice.Fight(Foe, foe));
        return Lines(core.TakeLog());
    }

    /// <summary>Sends a Pokémon into the place the rules are asking about, in the middle of a turn.</summary>
    public static List<string> SendIn(BattleCore core, int partyIndex)
    {
        Assert.Equal(Mine, Assert.IsType<ReplacementRequest>(core.Request).Place);
        core.Submit(BattleChoice.Switch(Mine, partyIndex));
        return Lines(core.TakeLog());
    }

    public static void InOrder(List<string> said, params string[] lines)
    {
        int at = -1;
        foreach (string line in lines)
        {
            int next = said.FindIndex(at + 1, s => s == line);
            Assert.True(next >= 0, $"\"{line}\" isn't said after \"{(at < 0 ? "the start" : said[at])}\" in:\n{string.Join("\n", said)}");
            at = next;
        }
    }

}
