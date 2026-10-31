using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// A team kept by name (plan 12 · Q27): up to six of the player's Pokémon by their own <see cref="Pokemon.Id"/> (plan 07 · O2),
/// in the team's order. Kept in the save as it is (<c>SaveData.Teams</c>, null with none).
/// </summary>
public sealed class SavedTeam
{
    public string Name { get; set; } = "";
    public List<Guid> Members { get; set; } = new();

    public SavedTeam Copy() => new() { Name = Name, Members = Members.ToList() };
}

/// <summary>What swapping a team in came to: done or refused (with the words' key and its values), and who was skipped.</summary>
public sealed record TeamSwap(bool Done, string? Refusal, IReadOnlyList<(string Key, object Value)> Values, IReadOnlyList<int> Missing, IReadOnlyList<Pokemon> Brought);

/// <summary>
/// The teams kept and how one is swapped in from the PC in one step (plan 12 · Q27; style guide, "PC boxes", "Teams"). GPU-free:
/// the rules take the team, the PC and what a run allows and change nothing until a swap is sure to go through whole.
/// </summary>
public static class SavedTeams
{
    /// <summary>The most teams kept at once.</summary>
    public const int Most = 8;

    /// <summary>The longest name a team may have.</summary>
    public const int NameLength = 10;

    /// <summary>A team of the party as it stands now, named. Its members must have their ids (<c>GameEngine.GiveIds</c> runs first).</summary>
    public static SavedTeam Of(string name, Party party)
    {
        if (party.Members.Any(p => p.Id == Guid.Empty)) throw new InvalidOperationException("A team is kept by its members' ids, and one of them has none.");
        return new SavedTeam { Name = name, Members = party.Members.Select(p => p.Id).ToList() };
    }

    /// <summary>Where a Pokémon of the given id is now: on the team (its index), in a box (its place), or nowhere.</summary>
    public static (int PartyIndex, BoxPlace? Place) Find(Guid id, Party party, PcBoxes pc)
    {
        int at = party.Members.FindIndex(p => p.Id == id);
        if (at >= 0) return (at, null);
        for (int b = 0; b < pc.Boxes.Count; b++)
            for (int s = 0; s < PcBoxes.BoxSize; s++)
                if (pc[b, s] is { } p && p.Id == id) return (-1, new BoxPlace(b, s));
        return (-1, null);
    }

    /// <summary>
    /// Brings a kept team onto the team in its order: each member found in a box comes out as withdrawing does
    /// (<see cref="PcBoxes.AsWithdrawn"/>), each already on the team stays, and whoever was on the team and isn't of it
    /// goes into the places the members left, in order, then into the first empty places. A member no longer stored is
    /// skipped (its index in <see cref="TeamSwap.Missing"/>), as is one the run won't let on the team (<paramref name="whyNot"/>);
    /// at most <paramref name="limit"/> come. Refused whole, changing nothing, where none of the team is here, where the team
    /// to come would have none able to battle, where someone leaving holds Mail or a capsule, or where the boxes haven't room.
    /// </summary>
    public static TeamSwap SwapIn(SavedTeam team, Party party, PcBoxes pc, Func<Pokemon, string?>? whyNot = null, int limit = Party.MaxSize)
    {
        var missing = new List<int>();
        var coming = new List<(Pokemon Pokemon, BoxPlace? From)>();
        for (int i = 0; i < team.Members.Count; i++)
        {
            var (at, place) = Find(team.Members[i], party, pc);
            Pokemon? p = at >= 0 ? party.Members[at] : place is { } bp ? pc[bp.Box, bp.Slot] : null;
            if (p == null || (place != null && whyNot?.Invoke(p) != null) || coming.Count >= limit)
            {
                if (p == null) missing.Add(i);
                continue;
            }
            coming.Add((p, place));
        }

        TeamSwap Refused(string key, params (string, object)[] values) => new(false, key, values, missing, Array.Empty<Pokemon>());
        if (coming.Count == 0) return Refused("boxTeams.swap.noneHere", ("team", team.Name));
        // Those that come from a box are healed as they come out; the team must have one able to battle
        if (!coming.Any(c => !c.Pokemon.IsEgg && (c.From != null || c.Pokemon.CurrentHP > 0)))
            return Refused("boxes.lastAble");

        var leaving = party.Members.Where(p => !coming.Any(c => c.Pokemon == p)).ToList();
        foreach (var p in leaving)
        {
            if (PcBoxes.HoldsMail(p)) return Refused("boxTeams.swap.mail", ("name", p.DisplayName));
            if (p.CapsuleId != 0) return Refused("boxTeams.swap.capsule", ("name", p.DisplayName));
        }
        var vacated = coming.Where(c => c.From != null).Select(c => c.From!.Value).ToList();
        if (leaving.Count > vacated.Count + pc.Free) return Refused("boxTeams.swap.noRoom", ("count", leaving.Count));

        // Sure to go through: take the members out, lay the team in its order, and those leaving where they were
        foreach (var place in vacated) pc.TakeOut(place.Box, place.Slot);
        party.Clear();
        foreach (var (p, from) in coming)
        {
            if (from != null) PcBoxes.AsWithdrawn(p);
            party.Members.Add(p);
        }
        int next = 0;
        foreach (var p in leaving)
        {
            if (next < vacated.Count) { pc.PutIn(vacated[next].Box, vacated[next].Slot, p); next++; }
            else pc.Store(p);
        }
        return new TeamSwap(true, null, Array.Empty<(string, object)>(), missing, coming.Select(c => c.Pokemon).ToList());
    }
}

/// <summary>The judge's six words for an IV (plan 12 · Q27), our own; the numbers themselves are never shown.</summary>
public enum IvGrade { Nothing, Meagre, Fair, Good, Splendid, Flawless }

/// <summary>The judge on the PC's side panel (plan 12 · Q27; style guide, "PC boxes", "The judge"). GPU-free.</summary>
public static class IvJudge
{
    /// <summary>The six stats in the summary's order.</summary>
    public static IReadOnlyList<StatType> Stats { get; } = new[] { StatType.HP, StatType.Attack, StatType.Defense, StatType.SpAttack, StatType.SpDefense, StatType.Speed };

    /// <summary>The word for an IV: 0 nothing, 1 to 10 meagre, 11 to 20 fair, 21 to 25 good, 26 to 30 splendid, 31 flawless.</summary>
    public static IvGrade Grade(int iv) => iv switch
    {
        <= 0 => IvGrade.Nothing,
        <= 10 => IvGrade.Meagre,
        <= 20 => IvGrade.Fair,
        <= 25 => IvGrade.Good,
        <= 30 => IvGrade.Splendid,
        _ => IvGrade.Flawless
    };

    public static int IvOf(Pokemon p, StatType stat) => stat switch
    {
        StatType.HP => p.IvHP,
        StatType.Attack => p.IvAttack,
        StatType.Defense => p.IvDefense,
        StatType.SpAttack => p.IvSpAttack,
        StatType.SpDefense => p.IvSpDefense,
        StatType.Speed => p.IvSpeed,
        _ => 0
    };

    /// <summary>The six words of a Pokémon, in the summary's order.</summary>
    public static IReadOnlyList<IvGrade> Of(Pokemon p) => Stats.Select(s => Grade(IvOf(p, s))).ToList();

    /// <summary>The stat with the best word (the first in the summary's order among equals), or null where all six are one word.</summary>
    public static StatType? Best(Pokemon p)
    {
        var grades = Of(p);
        if (grades.All(g => g == grades[0])) return null;
        var top = grades.Max();
        return Stats[grades.ToList().IndexOf(top)];
    }
}
