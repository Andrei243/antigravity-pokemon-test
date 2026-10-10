using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// A Seal stuck on a Ball Capsule (the original's <c>BallSeal</c>): which Seal, and where, in the dots of the screen
/// it was put on (256 by 192), the ball's middle at <see cref="CentreX"/>, <see cref="CentreY"/>.
/// </summary>
public readonly record struct BallSeal(int Seal, int X, int Y)
{
    /// <summary>The middle of the ball on the capsule's screen, and how far from it a Seal still sticks (<c>SealPlacement_SealIsValid</c>).</summary>
    public const int CentreX = 190, CentreY = 70, Reach = 60;

    /// <summary>How far from the ball's middle it is, in whole dots (<c>ov12_02237E80</c>).</summary>
    public int Distance => CapsuleRules.Distance(X, Y, CentreX, CentreY);

    /// <summary>Whether it is on the ball: one let go further out goes back into the case.</summary>
    public bool OnTheBall => Distance <= Reach;
}

/// <summary>
/// A Ball Capsule (the original's <c>BallCapsule</c>): up to eight Seals, the first in front. It never changes: an
/// edit makes another. A Pokémon carries a copy of the capsule put on its ball (<see cref="Pokemon.Capsule"/>).
/// </summary>
public sealed class BallCapsule : IEquatable<BallCapsule>
{
    /// <summary><c>SEALS_PER_CAPSULE</c>.</summary>
    public const int MostSeals = 8;

    public static readonly BallCapsule Empty = new(Array.Empty<BallSeal>());

    public IReadOnlyList<BallSeal> Seals { get; }

    public BallCapsule(IEnumerable<BallSeal> seals)
    {
        Seals = seals.Where(s => SealDatabase.Exists(s.Seal)).Take(MostSeals).ToArray();
    }

    /// <summary><c>CapsuleManager_AnySealsOnCapsule</c>, the other way round.</summary>
    public bool IsEmpty => Seals.Count == 0;

    /// <summary>What a save and the data files keep: <c>"HEART_SEAL_A,190,70"</c> for each Seal, the first in front.</summary>
    public List<string> ToSave() => Seals.Select(s => $"{SealDatabase.Id(s.Seal)},{s.X},{s.Y}").ToList();

    /// <summary>A capsule read back; a Seal it doesn't know is left off.</summary>
    public static BallCapsule Parse(IEnumerable<string>? saved)
    {
        var seals = new List<BallSeal>();
        foreach (string entry in saved ?? Array.Empty<string>())
        {
            var parts = entry.Split(',');
            if (parts.Length != 3 || !int.TryParse(parts[1], out int x) || !int.TryParse(parts[2], out int y)) continue;
            int seal = SealDatabase.Seal(parts[0].Trim());
            if (seal != 0) seals.Add(new BallSeal(seal, Math.Clamp(x, 0, 255), Math.Clamp(y, 0, 255)));
        }
        return seals.Count == 0 ? Empty : new BallCapsule(seals);
    }

    public bool Equals(BallCapsule? other) => other != null && Seals.SequenceEqual(other.Seals);
    public override bool Equals(object? obj) => Equals(obj as BallCapsule);
    public override int GetHashCode() => Seals.Aggregate(Seals.Count, (hash, s) => unchecked(hash * 31 + s.Seal * 65536 + s.X * 256 + s.Y));
}

/// <summary>
/// The Seal Case (plan 06 · R17b; the original's <c>SealCase</c>, <c>src/ball_seal_info.c</c>): how many of each of the
/// eighty Seals lie loose in it, and the twelve Ball Capsules with the Seals stuck on them. A kind is kept up to
/// ninety-nine, those on capsules counted. Saved. No drawing, no input.
/// </summary>
public sealed class SealCase
{
    /// <summary><c>CAPSULE_NUM</c> and <c>MAX_SEALS_PER_TYPE</c>.</summary>
    public const int Capsules = 12, MostOfAKind = 99;

    private readonly int[] counts = new int[SealDatabase.Count + 1];
    private readonly BallCapsule[] capsules = Enumerable.Repeat(BallCapsule.Empty, Capsules).ToArray();

    /// <summary>How many of a Seal lie loose in the case (<c>SealCase_GetSealCount</c>).</summary>
    public int Count(int seal) => SealDatabase.Exists(seal) ? counts[seal] : 0;

    /// <summary>How many of a Seal are stuck on the twelve capsules (<c>SealCase_CountSealOccurrenceInUse</c>).</summary>
    public int InUse(int seal) => capsules.Sum(c => c.Seals.Count(s => s.Seal == seal));

    /// <summary>Loose and stuck on together (<c>SealCase_CountSealOccurrenceAnywhere</c>).</summary>
    public int Anywhere(int seal) => InUse(seal) + Count(seal);

    /// <summary>The kinds the player has any of, in the original's order: the pages the capsule's screen turns through.</summary>
    public IReadOnlyList<int> Owned => Enumerable.Range(1, SealDatabase.Count).Where(s => Anywhere(s) != 0).ToList();

    /// <summary>How many kinds the player has (<c>SealCase_CountUniqueSeals</c>): none, and the PC won't open the capsules.</summary>
    public int Unique => Owned.Count;

    /// <summary>Every Seal the player has, as the bag's word on the case counts them (<c>CalcTotalBallSeals</c>).</summary>
    public int Total => Enumerable.Range(1, SealDatabase.Count).Sum(Anywhere);

    /// <summary><c>SealCase_CheckSealCount</c> for Seals coming in: whether so many more of a kind still fit.</summary>
    public bool CanFit(int seal, int count = 1) => SealDatabase.Exists(seal) && count > 0 && Anywhere(seal) + count <= MostOfAKind;

    /// <summary>The most of a kind that still fit.</summary>
    public int Room(int seal) => SealDatabase.Exists(seal) ? Math.Max(0, MostOfAKind - Anywhere(seal)) : 0;

    /// <summary><c>GiveOrTakeSeal</c> with a number to give: all of them into the case, or none when they wouldn't fit.</summary>
    public bool Give(int seal, int count = 1)
    {
        if (!CanFit(seal, count)) return false;
        counts[seal] += count;
        return true;
    }

    /// <summary><c>GiveOrTakeSeal</c> with a number to take: out of the loose ones, or nothing when there aren't so many.</summary>
    public bool Take(int seal, int count = 1)
    {
        if (!SealDatabase.Exists(seal) || count <= 0 || counts[seal] < count) return false;
        counts[seal] -= count;
        return true;
    }

    /// <summary>
    /// A Seal taken off a capsule being edited goes back among the loose ones (<c>GiveOrTakeSeal2</c>, which asks only
    /// whether those stuck on leave room, and never lets the loose ones pass ninety-nine).
    /// </summary>
    public bool PutBack(int seal)
    {
        if (!SealDatabase.Exists(seal) || InUse(seal) + 1 > MostOfAKind) return false;
        counts[seal] = Math.Min(MostOfAKind, counts[seal] + 1);
        return true;
    }

    public BallCapsule Capsule(int index) => capsules[Math.Clamp(index, 0, Capsules - 1)];

    public void SetCapsule(int index, BallCapsule capsule)
    {
        if (index >= 0 && index < Capsules) capsules[index] = capsule;
    }

    /// <summary>The loose Seals as they stand, for an edit to put back if it is given up (<c>CapsuleManager_GetSealCounts</c>).</summary>
    public int[] LooseCounts() => (int[])counts.Clone();

    public void RestoreLoose(int[] kept) => Array.Copy(kept, counts, Math.Min(kept.Length, counts.Length));

    /// <summary>What a save keeps: the loose Seals by their constants, and the twelve capsules.</summary>
    public SavedSealCase ToSave() => new()
    {
        Seals = Enumerable.Range(1, SealDatabase.Count).Where(s => counts[s] > 0).ToDictionary(SealDatabase.Id, s => counts[s]),
        Capsules = capsules.Select(c => c.ToSave()).ToList()
    };

    /// <summary>Takes on what a save kept; a Seal it doesn't know is left out.</summary>
    public void Load(SavedSealCase? saved)
    {
        Array.Clear(counts);
        Array.Fill(capsules, BallCapsule.Empty);
        if (saved == null) return;
        for (int i = 0; i < Math.Min(Capsules, saved.Capsules.Count); i++) capsules[i] = BallCapsule.Parse(saved.Capsules[i]);
        foreach (var (id, count) in saved.Seals)
        {
            int seal = SealDatabase.Seal(id);
            if (seal != 0) counts[seal] = Math.Clamp(count, 0, Math.Max(0, MostOfAKind - InUse(seal)));
        }
    }
}

/// <summary>The Seal Case in a save.</summary>
public sealed class SavedSealCase
{
    public Dictionary<string, int> Seals { get; set; } = new();
    public List<List<string>> Capsules { get; set; } = new();
}

/// <summary>
/// A Ball Capsule being changed (the original's capsule screen, <c>src/applications/capsule_menu/placement.c</c>): Seals
/// are taken out of the case and put on the ball, moved about, lifted to the front, or let go off the ball, which
/// puts them back. The case's loose Seals change as it goes; <see cref="Save"/> writes the capsule, and
/// <see cref="GiveUp"/> puts capsule and case back as they were.
/// </summary>
public sealed class CapsuleEdit
{
    private readonly SealCase sealCase;
    private readonly int[] looseBefore;
    private readonly List<BallSeal> seals;

    /// <summary>The capsule's place among the twelve.</summary>
    public int Capsule { get; }

    public CapsuleEdit(SealCase from, int capsule)
    {
        sealCase = from;
        Capsule = Math.Clamp(capsule, 0, SealCase.Capsules - 1);
        looseBefore = from.LooseCounts();
        seals = from.Capsule(Capsule).Seals.ToList();
    }

    /// <summary>The Seals on the ball now, the first in front.</summary>
    public IReadOnlyList<BallSeal> Seals => seals;

    /// <summary><c>SealPlacement_NotMaxSeals</c>, the other way round.</summary>
    public bool Full => seals.Count >= BallCapsule.MostSeals;

    /// <summary><c>SealPlacement_AnySealsChanged</c>.</summary>
    public bool Changed => !seals.SequenceEqual(sealCase.Capsule(Capsule).Seals);

    /// <summary>
    /// Takes a Seal out of the case onto the ball's middle, in front of the rest (<c>SealPlacement_GetTouchedSeal</c>):
    /// its place on the capsule, or -1 when eight are on already or none of the kind lies loose.
    /// </summary>
    public int Take(int seal)
    {
        if (Full || !sealCase.Take(seal)) return -1;
        seals.Insert(0, new BallSeal(seal, BallSeal.CentreX, BallSeal.CentreY));
        return 0;
    }

    /// <summary>Brings a Seal to the front (<c>SealPlacement_SetSealRenderPriorities</c>); its new place, 0.</summary>
    public int Lift(int index)
    {
        if (index <= 0 || index >= seals.Count) return Math.Max(0, Math.Min(index, seals.Count - 1));
        var lifted = seals[index];
        seals.RemoveAt(index);
        seals.Insert(0, lifted);
        return 0;
    }

    /// <summary>Moves a Seal to a dot of the screen (a byte each way, as the original keeps them; the screen is 192 dots tall).</summary>
    public void Move(int index, int x, int y)
    {
        if (index < 0 || index >= seals.Count) return;
        seals[index] = seals[index] with { X = Math.Clamp(x, 0, 255), Y = Math.Clamp(y, 0, 191) };
    }

    /// <summary>
    /// Lets a Seal go where it is (<c>SealPlacement_HandleTouchscreen</c>'s release): on the ball it stays, off it it
    /// goes back into the case. True when it stayed.
    /// </summary>
    public bool Drop(int index)
    {
        if (index < 0 || index >= seals.Count) return false;
        if (seals[index].OnTheBall) return true;
        Remove(index);
        return false;
    }

    /// <summary>Takes a Seal off the ball and puts it back into the case (<c>SealPlacement_FreeSeal</c>, <c>GiveOrTakeSeal2</c>).</summary>
    public void Remove(int index)
    {
        if (index < 0 || index >= seals.Count) return;
        sealCase.PutBack(seals[index].Seal);
        seals.RemoveAt(index);
    }

    /// <summary>The capsule as it would be saved now.</summary>
    public BallCapsule Result => seals.Count == 0 ? BallCapsule.Empty : new BallCapsule(seals);

    /// <summary>Writes the capsule into the case (<c>SealPlacement_LoadCapsuleSeals</c>).</summary>
    public void Save() => sealCase.SetCapsule(Capsule, Result);

    /// <summary>Gives the edit up: the case's loose Seals are as before it began, and the capsule was never written.</summary>
    public void GiveUp()
    {
        sealCase.RestoreLoose(looseBefore);
        seals.Clear();
        seals.AddRange(sealCase.Capsule(Capsule).Seals);
    }
}

/// <summary>
/// The rules of Ball Capsules and Seals beyond the case's own (plan 06 · R17b): a capsule put on a Pokémon's ball and
/// taken off, two capsules changing places, how a Seal's burst is timed and placed as its Pokémon is sent out, and how
/// Seals are come by. From the original's <c>capsule_menu</c>, <c>ov12_02235E94.c</c>, <c>ov12_02237E54.c</c> and the
/// scripts of Solaceon Town's east house and the Sunyshore Market. No drawing, no input.
/// </summary>
public static class CapsuleRules
{
    /// <summary>The frames a second of the original has.</summary>
    public const float FramesPerSecond = 30f;

    /// <summary>
    /// <c>SealAnim_GetDistance</c>: the whole dots between two places (the fixed-point square root, with what is past
    /// the point dropped).
    /// </summary>
    public static int Distance(int x1, int y1, int x2, int y2)
    {
        int dx = x1 - x2, dy = y1 - y2;
        return (int)Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// How many frames after the ball opens a Seal bursts (<c>ov12_02237EA0</c>): a letter after eight; any other by how
    /// far from the ball's middle it is, at once within twenty dots, after eight, fourteen and twenty frames further out.
    /// </summary>
    public static int BurstDelay(BallSeal seal)
    {
        ReadOnlySpan<int> delays = stackalloc[] { 0, 8, 14, 20 };
        if (SealDatabase.IsLetter(seal.Seal)) return delays[1];
        int distance = seal.Distance;
        int step = distance >= BallSeal.Reach - 4 ? 3 : Math.Min(3, (distance + 1) / 20);
        return delays[step];
    }

    /// <summary>
    /// Where a Seal bursts, in dots from the Pokémon's place, right and up (<c>ov12_022365D4</c>): as far from it as the
    /// Seal is from the ball's middle, and thirty dots higher.
    /// </summary>
    public static (int Right, int Up) BurstOffset(BallSeal seal) => (seal.X - BallSeal.CentreX, BallSeal.CentreY - seal.Y + 30);

    /// <summary>Whether a Pokémon's ball has a capsule on it (<c>MON_DATA_BALL_CAPSULE_ID</c> other than nought).</summary>
    public static bool HasCapsule(Pokemon pokemon) => pokemon.CapsuleId != 0;

    /// <summary>
    /// The Pokémon of the team a capsule is on: the last whose ball carries its number, as the capsule's screen reads
    /// the team (<c>CapsuleMenu_Init</c>). Null for a capsule on nobody's ball.
    /// </summary>
    public static Pokemon? HolderOf(Party party, int capsule) => party.Members.LastOrDefault(p => p.CapsuleId == capsule + 1);

    /// <summary>
    /// Puts a capsule on a Pokémon's ball (<c>CapsuleMenu_FieldTaskCB</c>'s attach): the Pokémon takes the capsule's
    /// number and a copy of its Seals. False for an Egg, or a capsule with no Seal on it, which the screen never offers.
    /// </summary>
    public static bool Attach(SealCase sealCase, int capsule, Pokemon pokemon)
    {
        if (pokemon.IsEgg || capsule < 0 || capsule >= SealCase.Capsules || sealCase.Capsule(capsule).IsEmpty) return false;
        pokemon.CapsuleId = capsule + 1;
        pokemon.Capsule = sealCase.Capsule(capsule);
        return true;
    }

    /// <summary>Takes the capsule off a Pokémon's ball (<c>Pokemon_ClearBallCapsuleData</c>). The capsule itself stays in the case.</summary>
    public static void Detach(Pokemon pokemon)
    {
        pokemon.CapsuleId = 0;
        pokemon.Capsule = null;
    }

    /// <summary>Takes a capsule off whichever Pokémon of the team carries it (<c>CapsuleManager_AssignCapsuleMon</c>).</summary>
    public static void Free(Party party, int capsule)
    {
        foreach (var p in party.Members.Where(p => p.CapsuleId == capsule + 1)) Detach(p);
    }

    /// <summary>
    /// Two capsules change places (<c>CapsuleGraphics_SwapCapsules</c>): their Seals with them, and the Pokémon each
    /// was on now carries the other's number.
    /// </summary>
    public static void Swap(SealCase sealCase, Party party, int first, int second)
    {
        if (first == second) return;
        var onFirst = party.Members.Where(p => p.CapsuleId == first + 1).ToList();
        var onSecond = party.Members.Where(p => p.CapsuleId == second + 1).ToList();
        foreach (var p in onFirst) p.CapsuleId = second + 1;
        foreach (var p in onSecond) p.CapsuleId = first + 1;
        var kept = sealCase.Capsule(first);
        sealCase.SetCapsule(first, sealCase.Capsule(second));
        sealCase.SetCapsule(second, kept);
    }

    /// <summary>
    /// A capsule's edit is kept (the capsule screen's CONFIRM): written into the case, and the Pokémon whose ball it
    /// is on takes the new Seals. True when anything changed.
    /// </summary>
    public static bool Keep(CapsuleEdit edit, Party party)
    {
        bool changed = edit.Changed;
        edit.Save();
        if (HolderOf(party, edit.Capsule) is { } holder) holder.Capsule = edit.Result;
        return changed;
    }

    // ---------------------------------------------------------------- where Seals come from

    /// <summary>The first of the letter Seals, which Unown's shapes follow (<c>A_SEAL</c>).</summary>
    public static int FirstLetterSeal => SealDatabase.Seal("A_SEAL");

    /// <summary>
    /// The Seal an Unown's shape looks like (Solaceon Town's east house: fifty and the shape's number): A to Z, then
    /// the Shock Seal for "!" and the Mystery Seal for "?". 0 for anything that is no Unown.
    /// </summary>
    public static int UnownSeal(Pokemon pokemon)
    {
        if (pokemon.IsEgg || pokemon.Species.Name != "Unown") return 0;
        string letter = pokemon.Form is { } form && form.StartsWith("Unown-", StringComparison.Ordinal) ? form["Unown-".Length..] : "A";
        int shape = letter switch
        {
            "Exclamation" => 26,
            "Question" => 27,
            _ when letter.Length == 1 && letter[0] is >= 'A' and <= 'Z' => letter[0] - 'A',
            _ => 0
        };
        return FirstLetterSeal + shape;
    }

    /// <summary>
    /// How many of an Unown's Seal the boy hands over (<c>SolaceonTownEastHouse_TryGiveSealsForUnownForm</c>): ten, or
    /// as many as bring the kind to ninety-nine; none once it is there.
    /// </summary>
    public static int UnownSeals(SealCase sealCase, int seal)
    {
        int have = sealCase.Anywhere(seal);
        if (have >= SealCase.MostOfAKind) return 0;
        return have <= 89 ? 10 : 10 - (have - 89);
    }

    /// <summary>
    /// The five Seals that come with the Seal Case (<c>SolaceonTownEastHouse_GiveRandomSeals</c>): one of the six Heart
    /// Seals, two of the six Star Seals, one of the four Foamy Seals and one of the seven Song Seals, a draw each.
    /// </summary>
    public static List<int> FirstSeals(Random chance) => new()
    {
        1 + chance.Next(6),
        7 + chance.Next(6),
        7 + chance.Next(6),
        25 + chance.Next(4),
        43 + chance.Next(7)
    };
}
