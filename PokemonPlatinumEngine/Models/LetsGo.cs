using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>How a Let's Go battle came out (plan 34 · PD5).</summary>
public enum LetsGoEnd
{
    /// <summary>The wild Pokémon fainted: the lead's EXP is given, and the Pokémon is gone from the field.</summary>
    Felled,
    /// <summary>The lead fainted: it comes back unable to fight, and the wild Pokémon stays.</summary>
    Fainted,
    /// <summary>Neither fell within <see cref="LetsGoBattle.MostTurns"/>, or the wild Pokémon fled: the lead comes back as it is.</summary>
    BrokenOff
}

/// <summary>What a Let's Go battle is fought under, as the game hands it over (plan 34 · PD5).</summary>
public sealed class LetsGoTerms
{
    /// <summary>The battle's seed: the field's chance draws it, once, as the lead is sent.</summary>
    public required uint Seed { get; init; }

    /// <summary>The rules the battle is fought by (the game's in force unless given).</summary>
    public Ruleset? Rules { get; init; }

    /// <summary>The place's conditions (its ground, the time, the charms): what a real battle there would be handed.</summary>
    public BattleConditions Conditions { get; init; } = new();

    public string PlayerName { get; init; } = "Player";
    public Difficulty Difficulty { get; init; } = Difficulty.Normal;
    public int RunLevelCap { get; init; }
    public bool CrossedExp { get; init; }
}

/// <summary>
/// Let's Go (plan 34 · PD5; Scarlet and Violet's auto battle): the player's lead sent at a wild Pokémon of the open
/// field with a press, and the battle fought there and then by <see cref="BattleCore"/> with nobody watching. The lead's
/// moves are chosen as AUTO chooses them (<see cref="MoveSuggestion"/>), from outside, so each turn's answer is the
/// record's; the wild side chooses as a wild Pokémon does, inside the rules, on the battle's own generator. The log is
/// read for the EXP and the faint and is never shown: the field shows the lead running out, the target hit and fading.
/// <para>
/// The lead handed over is the team's own object, alone on a party of its own, so what the battle does to it stays: its
/// HP, PP, EXP, levels and friendship. A move that would want a place among four already known is passed by (the
/// Move Relearner remembers it). A battle never catches; nothing from the bag is used; neither side runs. One that
/// comes to no end within <see cref="MostTurns"/> turns is broken off. The record (<see cref="File"/>: both sides as
/// they began, the seed, the answers) replays the battle to the same log. GPU-free, and it draws nothing from <c>Dice</c>.
/// </para>
/// </summary>
public sealed class LetsGoBattle
{
    /// <summary>Turns a Let's Go battle may run before it is broken off.</summary>
    public const int MostTurns = 12;

    /// <summary>The record: both sides as they began, the seed and every answer given (<see cref="BattleRecordFile.Replay"/> fights it again).</summary>
    public BattleRecordFile File { get; }

    /// <summary>The rules as they ended (its log, its result, the Pok�mon as the battle left them).</summary>
    public BattleCore Core { get; }

    /// <summary>The whole log, as the rules wrote it (nobody reads it on screen).</summary>
    public IReadOnlyList<BattleEvent> Log { get; }

    public LetsGoEnd End { get; }

    /// <summary>Turns played to their end.</summary>
    public int Turns { get; }

    /// <summary>The EXP the lead was given (the sum of the log's <see cref="ExpGained"/> for it).</summary>
    public int Exp { get; }

    /// <summary>Whether the lead gained a level (evolution is the game's to ask after).</summary>
    public bool LeveledUp { get; }

    /// <summary>The moves the lead would have learned but for its four (passed by).</summary>
    public IReadOnlyList<string> MovesPassedBy { get; }

    private LetsGoBattle(BattleCore core, BattleRecordFile file, IReadOnlyList<BattleEvent> log, LetsGoEnd end, int turns, int exp, bool leveled, IReadOnlyList<string> passed)
    {
        Core = core;
        File = file;
        Log = log;
        End = end;
        Turns = turns;
        Exp = exp;
        LeveledUp = leveled;
        MovesPassedBy = passed;
    }

    /// <summary>
    /// Whether a lead may be sent at a wild Pokémon by Let's Go: it can fight, the wild one stands no higher than it,
    /// and isn't shiny (one higher, or shiny, is met in a real battle, as the originals have it).
    /// </summary>
    public static bool MayFight(Pokemon lead, Pokemon wild) =>
        !lead.IsEgg && !lead.IsFainted && !wild.IsShiny && wild.Level <= lead.Level;

    /// <summary>The setup the rules start from (the team's own lead, alone; the wild Pokémon; the terms).</summary>
    public static CoreSetup SetupOf(Pokemon lead, Pokemon wild, LetsGoTerms terms)
    {
        var party = new Party();
        party.Add(lead);
        return new CoreSetup
        {
            PlayerParty = party,
            WildPokemon = new List<Pokemon> { wild },
            Random = new BattleRandom(terms.Seed),
            Rules = terms.Rules,
            Conditions = terms.Conditions,
            PlayerName = terms.PlayerName,
            Difficulty = terms.Difficulty,
            RunLevelCap = terms.RunLevelCap,
            CrossedExp = terms.CrossedExp,
            NoBattleItems = true,
            CannotFlee = true
        };
    }

    /// <summary>Fights the battle to its end (or breaks it off) at once. <paramref name="lead"/> is changed as the battle changes it.</summary>
    public static LetsGoBattle Fight(Pokemon lead, Pokemon wild, LetsGoTerms terms)
    {
        if (lead.IsEgg || lead.IsFainted) throw new ArgumentException("A Let's Go battle is fought by a lead able to fight");
        var setup = SetupOf(lead, wild, terms);
        // The record's opening: both sides as they are before the rules touch them
        var opening = BattleRecordFile.Opening(setup);
        var core = new BattleCore(setup);
        core.Start();
        while (core.Request is { } request && core.Turn < MostTurns)
            core.Submit(Answer(core, request));
        var file = opening.Closing(core, DateTime.UnixEpoch) ?? throw new InvalidOperationException("A Let's Go battle's chance is its own BattleRandom");
        return Read(core, file, lead);
    }

    /// <summary>The answer to what the rules ask: each place's move as AUTO would choose it; Battle Style's offer declined.</summary>
    public static IReadOnlyList<BattleChoice> Answer(BattleCore core, BattleRequest request)
    {
        switch (request)
        {
            case ActionRequest action:
                return action.Places.Select(place => MoveFor(core, place)).ToList();
            case SwitchOfferRequest offer:
                return new[] { BattleChoice.Stay(offer.Place) };
            case ReplacementRequest replace:
                // A party of one has no one to send in; the rules end the battle before they ask, so this is never reached
                throw new InvalidOperationException($"A Let's Go battle was asked to replace {replace.Place}");
            default:
                throw new InvalidOperationException($"A Let's Go battle can't answer {request}");
        }
    }

    private static BattleChoice MoveFor(BattleCore core, Place place)
    {
        var mine = core.At(place);
        int move = MoveSuggestion.For(core, mine);
        var choice = BattleChoice.Fight(place, move);
        if (core.WhyNot(choice) == null) return choice;
        // A move the rules refuse (it shouldn't be, being usable): any other they allow, else Struggle
        for (int i = 0; i < mine.Pokemon!.Moves.Count; i++)
        {
            var other = BattleChoice.Fight(place, i);
            if (core.WhyNot(other) == null) return other;
        }
        return BattleChoice.Fight(place, -1);
    }

    private static LetsGoBattle Read(BattleCore core, BattleRecordFile file, Pokemon lead)
    {
        var log = core.Log;
        var result = core.Result;
        int turns = core.Turn;
        var end = result switch
        {
            BattleResult.PlayerVictory => LetsGoEnd.Felled,
            BattleResult.PlayerDefeat => LetsGoEnd.Fainted,
            _ => LetsGoEnd.BrokenOff
        };
        int exp = log.OfType<ExpGained>().Where(e => ReferenceEquals(e.Pokemon, lead)).Sum(e => e.Amount);
        bool leveled = log.OfType<LevelRose>().Any(e => ReferenceEquals(e.Pokemon, lead));
        var passed = log.OfType<MoveWanted>().Where(e => ReferenceEquals(e.Pokemon, lead)).Select(e => e.Move).ToList();
        return new LetsGoBattle(core, file, log.ToList(), end, turns, exp, leveled, passed);
    }
}
