using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models.PoketchApps;

/// <summary>One of the three best Poké Radar chains (<c>ChainRecord</c>): how long it grew and the species of it.</summary>
public readonly record struct ChainRecord(int Count, string? Species);

/// <summary>
/// The Trainer Counter, which the script that gives it calls the radar chain counter
/// (<c>trainer_counter/main.c</c>, <c>graphics.c</c>): the Poké Radar's chain going now (its species and its count
/// in three figures), and the three best chains there have been, best first, each with its species. Touching one of
/// the three plays its species' cry and makes it hop (<c>Task_PlayCry</c>: sixteen frames, 24 pixels up and down).
///
/// The best chains are the original's <c>RadarChainRecords</c>, kept as the chain grows (<c>pokeradar.c</c>):
/// a chain takes, as it begins, the empty record or the lowest (<c>GetLowestChainRecordSlot</c>), and each time it
/// grows past that record's count it writes itself there and the records are sorted, best first
/// (<c>TryReplaceLowestChainRecord</c>, <c>RadarChainRecords_SortSavedRecords</c>). The original keeps them in the
/// save's special encounters; here the Pokétch keeps them in its memory (<see cref="Follow"/>).
/// </summary>
public sealed class TrainerCounterApp : PoketchAppState
{
    /// <summary>The records kept (<c>NUM_RADAR_RECORDS</c>).</summary>
    public const int Records = 3;

    /// <summary>The hop of a Pokémon touched: sixteen frames.</summary>
    public const float HopSeconds = 16f / 60f;

    /// <summary>
    /// The three records' Pokémon, in blocks (<c>sMonHitboxes</c> and the icons' places): the best in the middle and
    /// highest, the second to its right a little lower, the third to its left lower still.
    /// </summary>
    public static readonly PoketchButton[] RecordButtons =
    {
        new(0, 16, 16, 13, 16),
        new(1, 31, 18, 13, 16),
        new(2, 1, 20, 13, 16)
    };

    // What the Pokétch's memory holds, by place: three records (count, National Pokédex number), the record the chain
    // going now writes into (unk_D0), and the count and species last seen of the chain
    private const int SlotAt = Records * 2, LastCountAt = SlotAt + 1, LastSpeciesAt = SlotAt + 2, Kept = SlotAt + 3;

    private readonly float[] hops = new float[Records];

    public override PoketchApp App => PoketchApp.TrainerCounter;

    public override IReadOnlyList<PoketchButton> Buttons(PoketchContext context) => RecordButtons;

    /// <summary>The chain going now: its species and count; null when there is none (<c>activeSpecies</c> none).</summary>
    public static (string Species, int Count)? Active(RadarChain? chain) =>
        chain is { Active: true, Count: > 0, Species: { } species } ? (species, chain.Count) : null;

    /// <summary>The records, best first (<c>RadarChainRecords_GetSpecies</c>, <c>_GetChainCount</c>, sorted as <c>SortChainRecords</c> sorts).</summary>
    public static IReadOnlyList<ChainRecord> Best(Poketch poketch)
    {
        var kept = Memory(poketch);
        return Sorted(kept).Select(i => new ChainRecord(kept[2 * i], SpeciesOf(kept[2 * i + 1]))).ToList();
    }

    /// <summary>
    /// <c>RadarChainRecords_GetNumFilledSlots</c>, with its slip: it asks the first record three times, so the records
    /// count as all filled or none. Only a record with a count can be touched all the same.
    /// </summary>
    public static int Filled(Poketch poketch)
    {
        var kept = Memory(poketch);
        int filled = 0;
        for (int i = 0; i < Records; i++)
            if (kept[0] != 0) filled++;
        return filled;
    }

    /// <summary>How far through its hop a record's Pokémon is, 0 standing to 1 landed.</summary>
    public float Hop(int record) => hops[record] <= 0f ? 0f : 1f - hops[record] / HopSeconds;

    public override void Press(int button, PoketchContext context)
    {
        // State_UpdateApp: not while a cry is playing; a record counted as filled and with a chain to it
        if (button < 0 || button >= Records || hops.Any(h => h > 0f)) return;
        if (button >= Filled(context.Poketch)) return;
        var best = Best(context.Poketch)[button];
        if (best.Count == 0 || best.Species == null || PokemonDatabase.Get(best.Species) is not { } species) return;
        hops[button] = HopSeconds;
        // A Pokémon of the species only to cry: made on a generator of its own, so the field's chance is untouched
        context.Cry(new Pokemon(species, 5, new Random(0)));
    }

    public override void Update(float dt, PoketchContext context)
    {
        for (int i = 0; i < hops.Length; i++) hops[i] = Math.Max(0f, hops[i] - dt);
        if (context.Radar != null) Follow(context.Poketch, context.Radar);
    }

    /// <summary>
    /// Keeps the records up to date with the radar's chain: to be called whenever the chain may have changed (the
    /// app calls it each frame it is on the screen). A chain that has just begun takes its record
    /// (<c>GetLowestChainRecordSlot</c>, as <c>PokeRadar_ShouldDoRadarEncounter</c> does on the chain's first patch);
    /// each count it reaches past that record's is written into it (<c>RadarChain_Increment</c>,
    /// <c>TryReplaceLowestChainRecord</c>).
    /// </summary>
    public static void Follow(Poketch poketch, RadarChain chain)
    {
        var kept = Memory(poketch);
        int count = chain.Active && chain.Species != null ? chain.Count : 0;
        int species = chain.Species is { } name ? PokemonDatabase.Get(name)?.DexNumber ?? 0 : 0;
        int lastCount = kept[LastCountAt], lastSpecies = kept[LastSpeciesAt];
        if (count == lastCount && species == lastSpecies) return;
        if (count > 0 && species != 0)
        {
            if (lastCount == 0 || count < lastCount || species != lastSpecies) kept[SlotAt] = LowestSlot(kept);
            if (count > lastCount || species != lastSpecies) TryReplace(kept, count, species);
        }
        kept[LastCountAt] = count;
        kept[LastSpeciesAt] = count > 0 ? species : 0;
        poketch.Keep(PoketchApp.TrainerCounter, kept);
    }

    // The Pokétch's memory of the app, as long as it should be
    private static List<int> Memory(Poketch poketch)
    {
        var kept = poketch.Recall(PoketchApp.TrainerCounter)?.ToList() ?? new List<int>();
        while (kept.Count < Kept) kept.Add(0);
        if (kept[SlotAt] < 0 || kept[SlotAt] >= Records) kept[SlotAt] = 0;
        return kept;
    }

    private static string? SpeciesOf(int dex) => dex <= 0 ? null : PokemonDatabase.GetByDex(dex)?.Name;

    // GetLowestChainRecordSlot: the first record with no species, else the lowest (the original's two comparisons)
    private static int LowestSlot(List<int> kept)
    {
        for (int i = 0; i < Records; i++)
            if (kept[2 * i + 1] == 0) return i;
        int slot = kept[0] < kept[2] ? 0 : 1;
        if (!(kept[2 * slot] < kept[4])) slot = 2;
        return slot;
    }

    // TryReplaceLowestChainRecord
    private static void TryReplace(List<int> kept, int count, int species)
    {
        int slot = kept[SlotAt];
        if (kept[2 * slot] >= count) return;
        kept[2 * slot] = count;
        kept[2 * slot + 1] = species;
        // RadarChainRecords_SortSavedRecords
        var order = Sorted(kept);
        var copy = order.Select(i => (kept[2 * i], kept[2 * i + 1])).ToList();
        for (int i = 0; i < Records; i++)
        {
            kept[2 * i] = copy[i].Item1;
            kept[2 * i + 1] = copy[i].Item2;
        }
        // The chain follows its record to wherever the sort put it: the last with its count
        if (kept[2 * slot] <= count)
            for (int i = Records - 1; i >= 0; i--)
                if (kept[2 * i] == count)
                {
                    kept[SlotAt] = i;
                    return;
                }
    }

    // SortChainRecords: the three by count, highest first, ties as the original's comparisons leave them
    private static int[] Sorted(List<int> kept)
    {
        int c0 = kept[0], c1 = kept[2], c2 = kept[4];
        if (c0 < c1)
        {
            if (c1 < c2) return new[] { 2, 1, 0 };
            if (c0 < c2) return new[] { 1, 2, 0 };
            return new[] { 1, 0, 2 };
        }
        if (c0 < c2) return new[] { 2, 0, 1 };
        if (c1 < c2) return new[] { 0, 2, 1 };
        return new[] { 0, 1, 2 };
    }
}
