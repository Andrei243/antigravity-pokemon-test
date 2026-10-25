using System;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// SOS battles (plan 14 · B5; <c>docs/mechanics/rulings.md</c>, "SOS calls"): Sun and Moon's calls for help. A wild
/// battle fought as a <see cref="BattleFormat.Sos"/> has a second, empty place on the wild side. When a wild Pokémon's
/// action comes and a place on its side is empty, it may spend its turn calling for help instead of moving
/// (<see cref="RollKind.Call"/>, against <see cref="CallChance"/>); the call is answered or not (<see cref="AnswerChance"/>),
/// and an ally that answers comes into the empty place with an <see cref="Entered"/> not out of a ball, made on the
/// battle's own generator (<see cref="RollKind.Ally"/>) with the chain's bonus (<see cref="SosChain.Of"/>). Its own
/// species, or another of its line or a pair (<see cref="SosAllies"/>). So a single becomes one against two, never more.
/// </summary>
public sealed partial class BattleCore
{
    /// <summary>An SOS battle: one wild Pokémon against the player's one, and an empty place beside it for an ally.</summary>
    public bool IsSos => Format == BattleFormat.Sos;

    /// <summary>The abilities of the player's Pokémon that make a wild Pokémon call twice as readily (Sun and Moon's).</summary>
    public static readonly string[] CallingAbilities = { "Intimidate", "Unnerve", "Pressure" };

    /// <summary>
    /// The chance in a hundred that a wild Pokémon of <paramref name="rate"/> calls for help, as Sun and Moon count it:
    /// its rate, three times over at half its HP or less and five times at a fifth or less, twice that when an ally of
    /// its fell this turn or the last, and twice again when the player's Pokémon has one of <see cref="CallingAbilities"/>.
    /// </summary>
    public static int CallChance(int rate, int hp, int maxHp, bool allyFell, bool calling)
    {
        if (rate <= 0) return 0;
        int factor = hp * 5 <= maxHp ? 5 : hp * 2 <= maxHp ? 3 : 1;
        int chance = rate * factor;
        if (allyFell) chance *= 2;
        if (calling) chance *= 2;
        return Math.Min(100, chance);
    }

    /// <summary>The chance in a hundred that a call of <paramref name="rate"/> is answered: four times the rate.</summary>
    public static int AnswerChance(int rate) => Math.Min(100, Math.Max(0, rate) * 4);

    /// <summary>
    /// A wild Pokémon whose action has come in an SOS battle may call for help in its place: it does where its side has an
    /// empty place, it is free to act (not asleep, frozen, flinching or held to a move of several turns) and the roll is
    /// under its chance. Returns whether it called, which is its action for the turn, answered or not.
    /// </summary>
    private bool CallsForHelp(Act act)
    {
        if (!IsSos || IsTrainerBattle) return false;
        var caller = act.User;
        if (caller.IsPlayerSide || !caller.IsActive || caller.IsElsewhere || caller.IsHeldToItsMove || caller.Flinched || act.Choice.Move == BattleChoice.HeldMove) return false;
        var empty = EnemySlots.FirstOrDefault(b => b != caller && !b.IsActive);
        if (empty == null) return false;
        var pokemon = caller.Pokemon!;
        if (pokemon.Status is StatusCondition.Sleep or StatusCondition.Freeze) return false;
        int rate = SosAllies.RateOf(pokemon.Species);
        if (rate <= 0) return false;
        bool fell = faintedOnTurn[BattleSide.Enemy] >= 0 && faintedOnTurn[BattleSide.Enemy] >= Turn - 1;
        bool calling = PlayerSlots.Any(b => b.IsActive && b.Ability?.Name is { } name && CallingAbilities.Contains(name));
        if (rng.Roll(RollKind.Call, 100) >= CallChance(rate, pokemon.CurrentHP, pokemon.MaxHP, fell, calling)) return false;

        bool answered = rng.Roll(RollKind.Call, 100) < AnswerChance(rate);
        Say(Text.Of("battle.calledForHelp", ("pokemon", caller.Name))).With(new CalledForHelp(caller.Place, answered));
        if (!answered)
        {
            Say(Text.Of("battle.itsHelpDidntAppear"));
            return true;
        }

        Field.SosChain = Math.Min(SosChain.Longest, Field.SosChain + 1);
        var ally = MakeAlly(pokemon, Field.SosChain);
        Emit(new AllyJoined(empty.Place, ally, ally.Clone(), Field.SosChain));
        Arrive(empty, ally, Text.Of("battle.wildAppeared", ("pokemon", ally.DisplayName)), fromBall: false);
        return true;
    }

    /// <summary>
    /// The ally that answers a call of <paramref name="caller"/>'s, the <paramref name="chain"/>th of the battle: its level
    /// the caller's less nought to two, its species drawn by weight from those that may come (<see cref="SosAllies.Candidates"/>),
    /// in the caller's regional form where its species has that form, its personality and IVs made from a number drawn on
    /// the battle's generator, and the chain's bonus laid on: IVs made whole, its hidden ability, more rolls to be shiny.
    /// </summary>
    private Pokemon MakeAlly(Pokemon caller, int chain)
    {
        int level = Math.Max(1, caller.Level - rng.Roll(RollKind.Ally, 3));
        var candidates = SosAllies.Candidates(caller.Species, level);
        int roll = rng.Roll(RollKind.Ally, candidates.Sum(c => c.Weight));
        var species = candidates[^1].Species;
        foreach (var (s, weight) in candidates)
        {
            if (roll < weight) { species = s; break; }
            roll -= weight;
        }
        if (!ReferenceEquals(Rules, Ruleset.Current)) species = species.Under(Rules);
        uint seed = (uint)rng.Roll(RollKind.Ally, 65536) << 16 | (uint)rng.Roll(RollKind.Ally, 65536);
        var maker = new BattleRandom(seed);
        // An Alolan caller calls Alolan allies, where the ally's species has the form (Sun and Moon's own are all Alola's)
        string? regional = caller.FormData is { Kind: FormKind.Regional } form && form.Name.StartsWith(caller.Species.Name)
            ? species.Name + form.Name[caller.Species.Name.Length..] : null;
        var ally = regional != null && species.Form(regional) is { Kind: FormKind.Regional }
            ? Pokemon.InForm(species, regional, level, maker)
            : new Pokemon(species, level, maker);

        var bonus = SosChain.Of(chain);
        if (bonus.PerfectIvs > 0)
        {
            var stats = Enumerable.Range(0, 6).ToList();
            for (int i = 0; i < bonus.PerfectIvs && stats.Count > 0; i++)
            {
                int stat = stats[maker.Next(stats.Count)];
                stats.Remove(stat);
                switch (stat)
                {
                    case 0: ally.IvHP = 31; break;
                    case 1: ally.IvAttack = 31; break;
                    case 2: ally.IvDefense = 31; break;
                    case 3: ally.IvSpAttack = 31; break;
                    case 4: ally.IvSpDefense = 31; break;
                    default: ally.IvSpeed = 31; break;
                }
            }
            ally.RecalculateStats();
            ally.CurrentHP = ally.MaxHP;
        }
        // The hidden ability, under rules that have calls for help (the modern ones alone)
        if (bonus.HiddenAbilityChance > 0 && Rules.SosCalls && ally.HiddenAbilityName is { } hidden && maker.Next(100) < bonus.HiddenAbilityChance)
            ally.AbilityName = hidden;
        if (bonus.ShinyRolls > 0) ally.RerollForShiny(maker, bonus.ShinyRolls);
        return ally;
    }
}
