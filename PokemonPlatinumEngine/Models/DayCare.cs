using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>What the Day Care has to say for itself (<c>Daycare_GetState</c>, the original's <c>DAYCARE_*</c> numbers).</summary>
public enum DayCareState
{
    Empty = 0,
    EggWaiting = 1,
    OnePokemon = 2,
    TwoPokemon = 3
}

/// <summary>
/// Solaceon Town's Pokémon Day Care, as the original keeps it (plan 06 · R15; <c>struct Daycare</c>,
/// <c>src/overlay005/daycare.c</c>): two places, each a Pokémon left there and the steps the player has walked since
/// (which it gains as EXP), the personality of an Egg the couple has found (nought for none) and the steps of the
/// egg cycle under way. <see cref="Step"/> is the original's <c>Daycare_Update</c>, run on every step the player
/// takes. No drawing or input; saved (<see cref="Core.DayCareSave"/>).
/// </summary>
public sealed class DayCare
{
    public const int Places = 2;

    /// <summary>The fee for a Pokémon taken back: 100, and 100 more for each level it grew (<c>DaycareMon_BufferDaycarePrice</c>).</summary>
    public const int BaseFee = 100, FeePerLevel = 100;

    private readonly Pokemon?[] pokemon = new Pokemon?[Places];
    private readonly int[] steps = new int[Places];

    /// <summary>The personality of the Egg the couple has found: nought while there is none (<c>offspringPersonality</c>).</summary>
    public uint Offspring { get; private set; }

    /// <summary>The steps of the egg cycle under way, 0 to 254 (<c>stepCounter</c>, one byte).</summary>
    public int StepCounter { get; private set; }

    /// <summary>The Pokémon left in a place; null for an empty one.</summary>
    public Pokemon? this[int place] => place is >= 0 and < Places ? pokemon[place] : null;

    /// <summary>The steps walked since the Pokémon in a place was left there: the EXP it has gained.</summary>
    public int StepsOf(int place) => place is >= 0 and < Places ? steps[place] : 0;

    /// <summary>The Pokémon left, in their places (none to two).</summary>
    public IReadOnlyList<Pokemon> Left => pokemon.OfType<Pokemon>().ToList();

    public int Count => pokemon.Count(p => p != null);

    /// <summary>Whether an Egg is waiting for the player (<c>Daycare_HasEgg</c>).</summary>
    public bool HasEgg => Offspring != 0;

    public DayCareState State => HasEgg ? DayCareState.EggWaiting : Count switch
    {
        0 => DayCareState.Empty,
        1 => DayCareState.OnePokemon,
        _ => DayCareState.TwoPokemon
    };

    /// <summary>How well the two left get on; none while fewer than two are.</summary>
    public Compatibility Compatibility =>
        pokemon[0] is { } a && pokemon[1] is { } b ? Breeding.Compatibility(a, b) : Compatibility.None;

    // ------------------------------------------------------------------ leaving and taking back

    /// <summary>
    /// Why a Pokémon of the team can't be left (the Day-Care Lady's script): an Egg, or the last one able to fight.
    /// Null when it can.
    /// </summary>
    public static string? WhyNot(Party party, int slot)
    {
        if (slot < 0 || slot >= party.Count) return "none";
        var chosen = party.Members[slot];
        if (chosen.IsEgg) return "egg";
        if (!party.Members.Where((p, i) => i != slot).Any(p => !p.IsFainted)) return "last";
        return null;
    }

    /// <summary>
    /// Leaves a Pokémon of the team in the first free place (<c>Daycare_MoveToEmptySlotFromParty</c>): a Shaymin
    /// goes back to its Land Forme, and its steps start from nought. False, with nothing changed, when the Day Care is
    /// full or the place isn't on the team.
    /// </summary>
    public bool Leave(Party party, int slot)
    {
        int place = Array.IndexOf(pokemon, null);
        if (place < 0 || slot < 0 || slot >= party.Count) return false;
        var left = party.Members[slot];
        if (left.Species.Name == "Shaymin" && left.Form != null) left.ChangeForm(null);
        pokemon[place] = left;
        steps[place] = 0;
        party.RemoveAt(slot);
        return true;
    }

    /// <summary>The level a Pokémon in a place would be at with its steps as EXP (<c>BoxPokemon_GiveExperience</c>), nothing changed.</summary>
    public int LevelNow(int place)
    {
        if (this[place] is not { } p) return 0;
        return LevelForExp(p, p.CurrentExp + steps[place]);
    }

    /// <summary>The levels a Pokémon in a place has grown (the Day-Care Lady's "grown by N levels").</summary>
    public int LevelsGained(int place) => this[place] is { } p ? LevelNow(place) - p.Level : 0;

    /// <summary>What taking a Pokémon back costs (<c>DaycareMon_BufferDaycarePrice</c>): 100, and 100 for each level grown.</summary>
    public int Fee(int place) => LevelsGained(place) * FeePerLevel + BaseFee;

    /// <summary>
    /// Gives a Pokémon back to the team (<c>Daycare_MoveToPartyFromDaycareMon</c>): below level 100 its steps are
    /// added to its EXP and it grows into the levels they reach, learning each level's moves into a free place or
    /// pushing its first move out; its steps start again from nought and the other Pokémon moves into the first place
    /// (<c>Daycare_ShiftMonSlots</c>). The fee is the script's to take. Null, with nothing changed, when the place is
    /// empty or the team full.
    /// </summary>
    public Pokemon? TakeBack(Party party, int place)
    {
        if (this[place] is not { } p || party.IsFull) return null;
        if (p.Level < 100) Grow(p, steps[place]);
        pokemon[place] = null;
        steps[place] = 0;
        party.Add(p);
        if (pokemon[0] == null && pokemon[1] != null)
        {
            (pokemon[0], pokemon[1]) = (pokemon[1], null);
            (steps[0], steps[1]) = (steps[1], 0);
        }
        return p;
    }

    /// <summary>
    /// A Pokémon given EXP by the Day Care and grown into the levels it reaches (<c>ov5_021E63E0</c>): no friendship
    /// for it, unlike a battle's; each level's moves learned into a free place or pushing the first move out.
    /// </summary>
    public static void Grow(Pokemon p, int exp)
    {
        if (exp <= 0 || p.Level >= 100) return;
        int target = LevelForExp(p, p.CurrentExp + exp);
        int max = Pokemon.GetExpForLevel(100, p.Species.GrowthRate);
        int oldMax = p.MaxHP;
        p.CurrentExp = Math.Min(p.CurrentExp + exp, max);
        while (p.Level < target)
        {
            p.LevelTo(p.Level + 1);
            foreach (var m in p.Learnset.Where(m => m.Level == p.Level)) Breeding.Push(p, m.MoveName);
        }
        p.RecalculateStats();
        if (p.CurrentHP > 0) p.CurrentHP = Math.Clamp(p.CurrentHP + p.MaxHP - oldMax, 1, p.MaxHP);
    }

    /// <summary>The level a Pokémon's EXP reaches, at most 100 (<c>Pokemon_GetLevelAt</c>).</summary>
    public static int LevelForExp(Pokemon p, int exp)
    {
        int level = p.Level;
        while (level < 100 && exp >= Pokemon.GetExpForLevel(level + 1, p.Species.GrowthRate)) level++;
        return level;
    }

    // ------------------------------------------------------------------ the Egg

    /// <summary>
    /// The Egg the couple found, handed to the team (<c>Daycare_GiveEggFromDaycare</c>; <see cref="Breeding.MakeEgg"/>)
    /// and the cycle started afresh. Null, with nothing changed, when there is no Egg or no room.
    /// </summary>
    public Pokemon? GiveEgg(Party party, uint trainer, Random rng, Ruleset rules)
    {
        if (!HasEgg || party.IsFull || pokemon[0] is not { } a || pokemon[1] is not { } b) return null;
        var egg = Breeding.MakeEgg(a, b, Offspring, trainer, rng, rules);
        party.Add(egg);
        Offspring = 0;
        StepCounter = 0;
        return egg;
    }

    /// <summary>The Egg turned down (<c>ResetDaycarePersonalityAndStepCounter</c>): the couple keep it, and nobody sees it again.</summary>
    public void KeepEgg()
    {
        Offspring = 0;
        StepCounter = 0;
    }

    /// <summary>
    /// One step of the player's (<c>Daycare_Update</c>): each Pokémon left walks it; with two left and no Egg waiting,
    /// every 256th step of the second the couple may find one (their compatibility out of a hundred,
    /// <see cref="Breeding.OffspringPersonality"/>); and at the end of each egg cycle (255 steps, 230 on the original's
    /// special days) the team's Eggs are counted down (<see cref="Breeding.CountDown"/>). The Egg that hatches now is
    /// returned (the field's <c>CommonScript_HatchEgg</c>), null when none does.
    /// </summary>
    public Pokemon? Step(Party party, Random rng, DateTime today, Ruleset rules)
    {
        int count = 0;
        for (int i = 0; i < Places; i++)
            if (pokemon[i] != null)
            {
                steps[i]++;
                count++;
            }

        if (!HasEgg && count == Places && (steps[1] & 0xFF) == 0xFF)
        {
            int score = (int)Compatibility;
            int roll = rng.Next(1 << 16) * 100 / 0xFFFF;
            if (score > roll) Offspring = Breeding.OffspringPersonality(pokemon[0]!, pokemon[1]!, rng, rules);
        }

        StepCounter = (StepCounter + 1) & 0xFF;
        if (StepCounter != Breeding.CycleLength(today)) return null;
        StepCounter = 0;
        return Breeding.CountDown(party);
    }

    // ------------------------------------------------------------------ saving

    /// <summary>Puts the Day Care back as a save had it.</summary>
    public void Restore(IReadOnlyList<(Pokemon? Pokemon, int Steps)> places, uint offspring, int stepCounter)
    {
        for (int i = 0; i < Places; i++)
        {
            pokemon[i] = i < places.Count ? places[i].Pokemon : null;
            steps[i] = i < places.Count && pokemon[i] != null ? Math.Max(0, places[i].Steps) : 0;
        }
        Offspring = offspring;
        StepCounter = Math.Clamp(stepCounter, 0, 255);
    }

    public void Clear() => Restore(Array.Empty<(Pokemon?, int)>(), 0, 0);
}
