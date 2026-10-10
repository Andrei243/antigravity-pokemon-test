using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>How far a berry plant has come (<c>BerryGrowthStage</c>): soil with nothing in it, then planted, sprouted, growing, in bloom and in fruit.</summary>
public enum BerryStage { None, Planted, Sprouted, Growing, Blooming, Fruit }

/// <summary>The four mulches, which change how a plant grows (<c>MulchType</c>), in the original's order.</summary>
public enum Mulch { None, Growth, Damp, Stable, Gooey }

/// <summary>How wet a patch's soil looks (<c>SoilMoisture</c>).</summary>
public enum SoilMoisture { VeryDry, Dry, Moist }

/// <summary>
/// One patch of soft soil as the save keeps it (<c>BerryPatch</c>): the berry in it and how far it has come, the
/// minutes left in the stage, the water in the soil and the minutes towards its next hour of drying, how many times
/// it has grown again from its own fallen fruit, the berries on it, how well it has been kept watered (the rating
/// the yield is multiplied by), the mulch laid, and whether it is growing at all: the original's patches stand still
/// until the player first sees them.
/// </summary>
public sealed class BerryPatch
{
    public string? Berry { get; set; }
    public BerryStage Stage { get; set; }
    public int StageMinutes { get; set; }
    public int MoistureMinutes { get; set; }
    public int Replants { get; set; }
    public int Yield { get; set; }
    public int Moisture { get; set; }
    public int YieldRating { get; set; }
    public Mulch Mulch { get; set; }
    public bool Growing { get; set; }

    internal void Clear()
    {
        Berry = null;
        Stage = BerryStage.None;
        StageMinutes = MoistureMinutes = Replants = Yield = Moisture = YieldRating = 0;
        Mulch = Mulch.None;
        Growing = false;
    }
}

/// <summary>
/// Sinnoh's 128 berry patches (plan 06 · R14a), the original's <c>berry_patches.c</c> in its own arithmetic: what is
/// planted, watered, mulched and picked, and how the minutes grow it. A stage lasts the berry's own hours (a quarter
/// shorter with Growth Mulch, half as long again with Damp Mulch); the soil loses the berry's own share of its water
/// each hour (half with Damp Mulch, half as much again with Growth Mulch), and each hour it spends dry costs the yield
/// rating a point, from 5; in bloom the plant fruits with its base yield times the rating, at least two; the fruit
/// stays four stages (six with Stable Mulch) and then falls and grows again, ten times (fifteen with Gooey Mulch)
/// before the patch is bare. A new game finds the patches in fruit with the original's berries
/// (<c>sBerryInitTable</c>). No drawing or input; saved whole (<c>SaveData.Berries</c>).
/// </summary>
public sealed class BerryPatches
{
    public const int Count = 128;
    private const int HarvestStages = 4, ReplantCount = 10, MaxMoisture = 100, MaxYieldRating = 5;

    public List<BerryPatch> Patches { get; set; } = Enumerable.Range(0, Count).Select(_ => new BerryPatch()).ToList();

    /// <summary>The moment the minutes were last counted to; null until the clock is first read.</summary>
    public DateTime? Clock { get; set; }

    /// <summary>Counts every change, so whoever draws the patches knows when to look again. Not saved.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public int Revision { get; private set; }

    /// <summary>What each patch holds as a new game begins (<c>include/data/berry_init.h</c>): the berry and how many are on it.</summary>
    public static readonly (string Berry, int Yield)[] NewGameBerries =
    {
        ("Oran Berry", 1), ("Cheri Berry", 1), ("Chesto Berry", 1), ("Pecha Berry", 1), ("Oran Berry", 1), ("Pecha Berry", 1),
        ("Razz Berry", 2), ("Bluk Berry", 2), ("Cheri Berry", 1), ("Oran Berry", 2), ("Sitrus Berry", 1), ("Wepear Berry", 2),
        ("Wepear Berry", 2), ("Kelpsy Berry", 1), ("Cheri Berry", 1), ("Pecha Berry", 1), ("Oran Berry", 1), ("Oran Berry", 1),
        ("Rawst Berry", 1), ("Rawst Berry", 1), ("Razz Berry", 1), ("Razz Berry", 1), ("Cheri Berry", 1), ("Oran Berry", 1),
        ("Oran Berry", 1), ("Bluk Berry", 1), ("Nanab Berry", 2), ("Razz Berry", 2), ("Bluk Berry", 2), ("Pinap Berry", 2),
        ("Leppa Berry", 1), ("Chesto Berry", 1), ("Razz Berry", 1), ("Razz Berry", 1), ("Persim Berry", 1), ("Nanab Berry", 1),
        ("Nanab Berry", 1), ("Figy Berry", 1), ("Aspear Berry", 1), ("Aspear Berry", 1), ("Razz Berry", 1), ("Pinap Berry", 1),
        ("Sitrus Berry", 1), ("Chesto Berry", 1), ("Wiki Berry", 1), ("Aguav Berry", 1), ("Pecha Berry", 1), ("Aspear Berry", 1),
        ("Iapapa Berry", 1), ("Grepa Berry", 1), ("Sitrus Berry", 1), ("Aspear Berry", 1), ("Tamato Berry", 1), ("Lum Berry", 1),
        ("Pecha Berry", 1), ("Pinap Berry", 1), ("Pinap Berry", 1), ("Pinap Berry", 1), ("Persim Berry", 1), ("Persim Berry", 1),
        ("Nanab Berry", 1), ("Nanab Berry", 1), ("Aguav Berry", 1), ("Iapapa Berry", 1), ("Rawst Berry", 1), ("Rawst Berry", 1),
        ("Cheri Berry", 1), ("Sitrus Berry", 1), ("Chesto Berry", 1), ("Pomeg Berry", 1), ("Pecha Berry", 2), ("Bluk Berry", 2),
        ("Wiki Berry", 1), ("Mago Berry", 1), ("Rawst Berry", 1), ("Persim Berry", 1), ("Figy Berry", 1), ("Pinap Berry", 2),
        ("Leppa Berry", 1), ("Pecha Berry", 1), ("Mago Berry", 1), ("Hondew Berry", 1), ("Wiki Berry", 2), ("Mago Berry", 2),
        ("Aguav Berry", 2), ("Qualot Berry", 1), ("Sitrus Berry", 2), ("Bluk Berry", 3), ("Nanab Berry", 3), ("Wepear Berry", 3),
        ("Pomeg Berry", 1), ("Pomeg Berry", 1), ("Hondew Berry", 2), ("Hondew Berry", 2), ("Kelpsy Berry", 1), ("Kelpsy Berry", 1),
        ("Tamato Berry", 1), ("Tamato Berry", 1), ("Qualot Berry", 1), ("Qualot Berry", 1), ("Pomeg Berry", 1), ("Pomeg Berry", 1),
        ("Hondew Berry", 1), ("Hondew Berry", 1), ("Tamato Berry", 1), ("Tamato Berry", 1), ("Grepa Berry", 1), ("Grepa Berry", 1),
        ("Qualot Berry", 1), ("Qualot Berry", 1), ("Lum Berry", 1), ("Leppa Berry", 1), ("Qualot Berry", 2), ("Grepa Berry", 2),
        ("Kelpsy Berry", 2), ("Kelpsy Berry", 2), ("Grepa Berry", 1), ("Grepa Berry", 1)
    };

    /// <summary>
    /// The patches as a new game finds them (<c>BerryPatches_Init</c>): each of the table's in fruit, for four
    /// stages, with its berries, its soil wet and a rating of 3; not growing until it is seen. The original reads on
    /// past its table for the last ten, which no soil in the world names, so they are left bare here.
    /// </summary>
    public static BerryPatches NewGame()
    {
        var patches = new BerryPatches();
        for (int i = 0; i < NewGameBerries.Length; i++)
        {
            var (berry, yield) = NewGameBerries[i];
            var patch = patches.Patches[i];
            patch.Berry = berry;
            patch.Stage = BerryStage.Fruit;
            patch.StageMinutes = StageMinutesOf(berry, Mulch.None) * HarvestStages;
            patch.Yield = yield;
            patch.Moisture = MaxMoisture;
            patch.YieldRating = 3;
        }
        return patches;
    }

    public BerryPatch this[int patch] => Patches[patch];

    // ---------------------------------------------------------------- the berries' own numbers

    /// <summary>A berry's growing, from the item table; a berry the table has no growing for counts as an Oran Berry.</summary>
    public static BerryData Growth(string berry) =>
        ItemDatabase.Get(berry)?.Berry ?? ItemDatabase.Get("Oran Berry")!.Berry!;

    /// <summary>The minutes a stage lasts (<c>CalcMinutesRemainingInStage</c>).</summary>
    public static int StageMinutesOf(string berry, Mulch mulch)
    {
        int minutes = Growth(berry).StageDuration * 60;
        return mulch switch
        {
            Mulch.Growth => minutes * 3 / 4,
            Mulch.Damp => minutes + minutes / 2,
            _ => minutes
        };
    }

    /// <summary>The water the soil loses an hour (<c>CalcMoistureDrainRate</c>).</summary>
    public static int DrainOf(string berry, Mulch mulch)
    {
        int rate = Growth(berry).MoistureDrainRate;
        return mulch switch
        {
            Mulch.Damp => rate / 2,
            Mulch.Growth => rate + rate / 2,
            _ => rate
        };
    }

    private static int HarvestStagesOf(Mulch mulch) => mulch == Mulch.Stable ? HarvestStages + HarvestStages / 2 : HarvestStages;

    private static int ReplantsOf(Mulch mulch) => mulch == Mulch.Gooey ? ReplantCount + ReplantCount / 2 : ReplantCount;

    /// <summary>How many stages a patch lives through at most (<c>CalcTotalStageCount</c>).</summary>
    private static int StagesOf(Mulch mulch) => 1 + (3 + HarvestStagesOf(mulch)) * ReplantsOf(mulch);

    /// <summary>The mulch an item is, by name ("Growth Mulch"); None for anything else.</summary>
    public static Mulch MulchOf(string? item) => item switch
    {
        "Growth Mulch" => Mulch.Growth,
        "Damp Mulch" => Mulch.Damp,
        "Stable Mulch" => Mulch.Stable,
        "Gooey Mulch" => Mulch.Gooey,
        _ => Mulch.None
    };

    /// <summary>The item a mulch is.</summary>
    public static string? NameOf(Mulch mulch) => mulch == Mulch.None ? null : $"{mulch} Mulch";

    // ---------------------------------------------------------------- what the player does

    /// <summary>How wet the soil looks (<c>BerryPatches_GetPatchMoisture</c>).</summary>
    public SoilMoisture MoistureOf(int patch) => Patches[patch].Moisture switch
    {
        0 => SoilMoisture.VeryDry,
        <= 50 => SoilMoisture.Dry,
        _ => SoilMoisture.Moist
    };

    /// <summary>Lays a mulch on bare soil, before anything is planted (<c>BerryPatches_SetMulchType</c>).</summary>
    public void LayMulch(int patch, Mulch mulch)
    {
        Patches[patch].Mulch = mulch;
        Revision++;
    }

    /// <summary>Plants a berry (<c>BerryPatches_PlantInPatch</c>): wet soil, the best rating, growing at once.</summary>
    public void Plant(int patch, string berry)
    {
        var p = Patches[patch];
        p.Berry = berry;
        p.Stage = BerryStage.Planted;
        p.StageMinutes = StageMinutesOf(berry, p.Mulch);
        p.MoistureMinutes = 0;
        p.Replants = 0;
        p.Yield = 0;
        p.Moisture = MaxMoisture;
        p.YieldRating = MaxYieldRating;
        p.Growing = true;
        Revision++;
    }

    /// <summary>Waters the soil to the full (<c>BerryPatches_ResetPatchMoisture</c>).</summary>
    public void Water(int patch)
    {
        Patches[patch].Moisture = MaxMoisture;
        Revision++;
    }

    /// <summary>Picks the berries, leaving bare soil with no mulch (<c>BerryPatches_HarvestPatch</c>): the berry and how many.</summary>
    public (string? Berry, int Count) Pick(int patch)
    {
        var p = Patches[patch];
        var picked = (p.Berry, p.Yield);
        p.Clear();
        Revision++;
        return picked;
    }

    /// <summary>The player has seen the patch (<c>BerryPatches_UpdateGrowthStates</c>): from now on it grows.</summary>
    public void Seen(int patch)
    {
        var p = Patches[patch];
        if (p.Growing || p.Berry == null) return;
        p.Growing = true;
        Revision++;
    }

    // ---------------------------------------------------------------- time

    /// <summary>The clock has moved on to <paramref name="now"/>: the patches grow by the whole minutes since it was last read. A clock turned back counts nothing.</summary>
    public void ClockTo(DateTime now)
    {
        if (Clock is { } before && now > before)
        {
            int minutes = (int)Math.Min(int.MaxValue, (now - before).TotalMinutes);
            if (minutes <= 0) return;
            MinutesPass(minutes);
            Clock = before.AddMinutes(minutes);
            return;
        }
        Clock = now;
    }

    /// <summary><c>BerryPatches_ElapseMinutes</c>: each growing patch through so many minutes, stage by stage.</summary>
    public void MinutesPass(int minutes)
    {
        if (minutes <= 0) return;
        Revision++;
        foreach (var p in Patches)
        {
            if (p.Berry == null || p.Stage == BerryStage.None || !p.Growing) continue;

            // Longer than the patch could ever live leaves it bare
            if ((long)minutes >= (long)StageMinutesOf(p.Berry, p.Mulch) * StagesOf(p.Mulch))
            {
                p.Clear();
                continue;
            }

            int left = minutes;
            while (p.Stage != BerryStage.None && left != 0)
            {
                if (p.StageMinutes > left)
                {
                    Drain(p, left);
                    p.StageMinutes -= left;
                    break;
                }
                Drain(p, p.StageMinutes);
                Advance(p);
                left -= p.StageMinutes;
                // A patch that has grown again for the last time is bare (the original reads a berry of nought's
                // numbers here, and the loop ends all the same)
                if (p.Stage == BerryStage.None) break;
                p.StageMinutes = StageMinutesOf(p.Berry!, p.Mulch);
                if (p.Stage == BerryStage.Fruit) p.StageMinutes *= HarvestStagesOf(p.Mulch);
            }
        }
    }

    /// <summary><c>AdvancePatchGrowth</c>.</summary>
    private static void Advance(BerryPatch p)
    {
        switch (p.Stage)
        {
            case BerryStage.Planted:
            case BerryStage.Sprouted:
            case BerryStage.Growing:
                p.Stage++;
                break;
            case BerryStage.Blooming:
                p.Yield = Math.Max(2, Growth(p.Berry!).BaseYield * p.YieldRating);
                p.Stage++;
                break;
            case BerryStage.Fruit:
                // The fruit falls and grows again from where it fell
                p.Yield = 0;
                p.Stage = BerryStage.Sprouted;
                p.YieldRating = MaxYieldRating;
                p.Replants++;
                if (p.Replants == ReplantsOf(p.Mulch)) p.Clear();
                break;
        }
    }

    /// <summary><c>DrainPatchMoisture</c>: water lost by the hour, and a point of the rating for each hour dry. Fruit drinks nothing.</summary>
    private static void Drain(BerryPatch p, int minutes)
    {
        if (p.Stage == BerryStage.Fruit) return;
        int rate = DrainOf(p.Berry!, p.Mulch);
        minutes += p.MoistureMinutes;
        int hours = minutes / 60;
        p.MoistureMinutes = minutes % 60;
        if (hours == 0) return;

        if (p.Moisture >= rate * hours)
        {
            p.Moisture -= rate * hours;
            return;
        }
        if (p.Moisture > 0)
        {
            // The hours the water lasted cost nothing
            int lasted = (p.Moisture + (rate - 1)) / rate;
            hours -= lasted;
            p.Moisture = 0;
        }
        p.YieldRating = p.YieldRating > hours ? p.YieldRating - hours : 0;
    }
}
