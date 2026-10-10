using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models;

/// <summary>A berry plant's stage, in the original's order (<c>BERRY_GROWTH_STAGE_*</c>): nothing growing, then five stages.</summary>
public enum BerryStage { None, Planted, Sprouted, Growing, Blooming, Fruit }

/// <summary>How wet a patch's soil is, as the original tells it (<c>enum SoilMoisture</c>).</summary>
public enum SoilMoisture { VeryDry, Dry, Moist }

/// <summary>The four mulches, in the original's order (<c>enum MulchType</c>); none is 0.</summary>
public enum Mulch { None, Growth, Damp, Stable, Gooey }

/// <summary>
/// One patch of soft soil as the save keeps it (<c>BerryPatch</c>, <c>berry_patches.h</c>): the berry growing in it
/// and its stage, the minutes left in the stage and towards the next hour of drying, how often it has seeded itself
/// again, the berries on it, the soil's moisture out of 100, the yield rating out of 5 the next crop is counted by,
/// the mulch laid on it, and whether it grows at all (a plant the player has never seen doesn't: below).
/// </summary>
public sealed class BerryPatch
{
    /// <summary>The berry, by the game's name for it; null for nothing planted.</summary>
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

    /// <summary>Empties the patch (<c>ZeroBerryPatch</c>): no berry, no stage, no mulch, not growing.</summary>
    public void Clear()
    {
        Berry = null;
        Stage = BerryStage.None;
        StageMinutes = MoistureMinutes = Replants = Yield = Moisture = YieldRating = 0;
        Mulch = Mulch.None;
        Growing = false;
    }
}

/// <summary>
/// Platinum's berry patches (plan 06 · R14a; <c>src/berry_patches.c</c> and <c>berry_patch_manager.c</c>): the 128
/// patches of soft soil the original numbers (the area files give each soil object its number,
/// <see cref="Data.AreaObject.Patch"/>), what a new game finds growing in them, planting, mulch, watering, the
/// minutes going by and picking. No drawing or input; saved whole (<see cref="Core.SaveData.Berries"/>).
/// </summary>
public sealed class BerryPatches
{
    /// <summary>The original's number of patches (<c>MAX_BERRY_PATCHES</c>).</summary>
    public const int Count = 128;

    /// <summary>The soil's moisture when watered or planted, and the most a yield rating is (<c>MAX_MOISTURE_RATING</c>, <c>MAX_YIELD_RATING</c>).</summary>
    public const int MostMoisture = 100, MostYieldRating = 5;

    /// <summary>The stages a plant stays in fruit, and the times it seeds itself again, without mulch (<c>BASE_HARVEST_STAGES</c>, <c>BASE_MAX_REPLANT_COUNT</c>).</summary>
    public const int HarvestStages = 4, MostReplants = 10;

    /// <summary>The patches, by the original's numbers.</summary>
    public List<BerryPatch> Patches { get; set; } = Enumerable.Range(0, Count).Select(_ => new BerryPatch()).ToList();

    /// <summary>
    /// What a new game finds in the soil (<c>BerryPatches_Init</c> with <c>sBerryInitTable</c>, <c>include/data/berry_init.h</c>):
    /// patch by patch, a plant in fruit with this many berries, its fruit lasting four times a stage, its soil moist
    /// and its yield rating 3; none of them grows until the player sees it. The original's table has 119 rows and its
    /// loop reads past them for patches 119 to 127, which no soil of the map uses: here they start empty.
    /// </summary>
    public static readonly (string Berry, int Yield)[] FirstCrop =
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
        ("Bluk Berry", 2), ("Wiki Berry", 1), ("Mago Berry", 1), ("Rawst Berry", 1), ("Persim Berry", 1), ("Figy Berry", 1),
        ("Pinap Berry", 2), ("Leppa Berry", 1), ("Pecha Berry", 1), ("Mago Berry", 1), ("Hondew Berry", 1), ("Wiki Berry", 2),
        ("Mago Berry", 2), ("Aguav Berry", 2), ("Qualot Berry", 1), ("Sitrus Berry", 2), ("Bluk Berry", 3), ("Nanab Berry", 3),
        ("Wepear Berry", 3), ("Pomeg Berry", 1), ("Pomeg Berry", 1), ("Hondew Berry", 2), ("Hondew Berry", 2), ("Kelpsy Berry", 1),
        ("Kelpsy Berry", 1), ("Tamato Berry", 1), ("Tamato Berry", 1), ("Qualot Berry", 1), ("Qualot Berry", 1), ("Pomeg Berry", 1),
        ("Pomeg Berry", 1), ("Hondew Berry", 1), ("Hondew Berry", 1), ("Tamato Berry", 1), ("Tamato Berry", 1), ("Grepa Berry", 1),
        ("Grepa Berry", 1), ("Qualot Berry", 1), ("Qualot Berry", 1), ("Lum Berry", 1), ("Leppa Berry", 1), ("Qualot Berry", 2),
        ("Grepa Berry", 2), ("Kelpsy Berry", 2), ("Kelpsy Berry", 2), ("Grepa Berry", 1), ("Grepa Berry", 1)
    };

    /// <summary>The patches a new game begins with (<see cref="FirstCrop"/>).</summary>
    public static BerryPatches NewGame()
    {
        var patches = new BerryPatches();
        for (int i = 0; i < FirstCrop.Length && i < Count; i++)
        {
            var (berry, yield) = FirstCrop[i];
            var p = patches.Patches[i];
            p.Clear();
            p.Berry = berry;
            p.Stage = BerryStage.Fruit;
            p.StageMinutes = StageMinutesOf(berry, Mulch.None) * HarvestStages;
            p.Yield = yield;
            p.Moisture = MostMoisture;
            p.YieldRating = 3;
        }
        return patches;
    }

    /// <summary>The patch of a number; null for a number the original doesn't have.</summary>
    public BerryPatch? this[int patch] => patch >= 0 && patch < Patches.Count ? Patches[patch] : null;

    // ------------------------------------------------------------------ what a berry and a mulch are

    /// <summary>A berry's growing numbers; null for anything that can't be planted (not one of Platinum's 64).</summary>
    public static BerryData? GrowthOf(string? berry) => berry != null ? ItemDatabase.Get(berry)?.Berry : null;

    /// <summary>Whether an item can be planted: one of the berries the original's berry table has.</summary>
    public static bool CanPlant(ItemData item) => item.Pocket == ItemPocket.Berries && item.Berry != null;

    /// <summary>The mulch an item is, by its name (the original's order from <c>FIRST_MULCH_IDX</c>); none for anything else.</summary>
    public static Mulch MulchOf(ItemData? item) => item?.Name switch
    {
        "Growth Mulch" => Mulch.Growth,
        "Damp Mulch" => Mulch.Damp,
        "Stable Mulch" => Mulch.Stable,
        "Gooey Mulch" => Mulch.Gooey,
        _ => Mulch.None
    };

    /// <summary>The item a mulch is; null for none.</summary>
    public static string? NameOf(Mulch mulch) => mulch == Mulch.None ? null : mulch + " Mulch";

    /// <summary>
    /// The minutes a stage of a berry takes under a mulch (<c>CalcMinutesRemainingInStage</c>): its hours, three
    /// quarters of them with Growth Mulch, half as long again with Damp Mulch.
    /// </summary>
    public static int StageMinutesOf(string? berry, Mulch mulch)
    {
        int minutes = (GrowthOf(berry)?.StageHours ?? 0) * 60;
        return mulch switch
        {
            Mulch.Growth => minutes * 3 / 4,
            Mulch.Damp => minutes + minutes / 2,
            _ => minutes
        };
    }

    /// <summary>The moisture a berry drinks in an hour under a mulch (<c>CalcMoistureDrainRate</c>): half with Damp Mulch, half as much again with Growth Mulch.</summary>
    public static int DrainOf(string? berry, Mulch mulch)
    {
        int drain = GrowthOf(berry)?.Drain ?? 0;
        return mulch switch
        {
            Mulch.Damp => drain / 2,
            Mulch.Growth => drain + drain / 2,
            _ => drain
        };
    }

    /// <summary>The stages a plant stays in fruit: half as many again with Stable Mulch (<c>GetHarvestTimeWithMulch</c>).</summary>
    public static int HarvestStagesOf(Mulch mulch) => mulch == Mulch.Stable ? HarvestStages + HarvestStages / 2 : HarvestStages;

    /// <summary>The times a plant seeds itself again before it is gone: half as many again with Gooey Mulch (<c>GetTotalReplantCountWithMulch</c>).</summary>
    public static int ReplantsOf(Mulch mulch) => mulch == Mulch.Gooey ? MostReplants + MostReplants / 2 : MostReplants;

    /// <summary>
    /// The stages a planting lives through, in stage lengths (<c>CalcTotalStageCount</c>): the first, then for each
    /// replanting three stages to grow and its time in fruit.
    /// </summary>
    public static int LifetimeStages(Mulch mulch) => 1 + (3 + HarvestStagesOf(mulch)) * ReplantsOf(mulch);

    // ------------------------------------------------------------------ what the scripts ask

    /// <summary>The stage of a patch (<c>BerryPatches_GetPatchGrowthStage</c>); none for a number the original doesn't have.</summary>
    public BerryStage StageOf(int patch) => this[patch]?.Stage ?? BerryStage.None;

    /// <summary>How wet a patch's soil is (<c>BerryPatches_GetPatchMoisture</c>): very dry at nothing, dry up to half, moist above.</summary>
    public SoilMoisture MoistureOf(int patch) => this[patch]?.Moisture switch
    {
        null or 0 => SoilMoisture.VeryDry,
        <= 50 => SoilMoisture.Dry,
        _ => SoilMoisture.Moist
    };

    /// <summary>A patch with nothing growing in it, where a berry can be planted (<c>BERRY_PATCH_FLAG_EMPTY</c>).</summary>
    public bool IsEmpty(int patch) => this[patch] is { Stage: BerryStage.None };

    /// <summary>An empty patch with no mulch on it yet, where mulch can be laid (<c>BERRY_PATCH_FLAG_CAN_MULCH</c>).</summary>
    public bool CanMulch(int patch) => this[patch] is { Stage: BerryStage.None, Mulch: Mulch.None };

    /// <summary>A patch with something growing in it, which the Sprayduck waters (<c>BERRY_PATCH_FLAG_HAS_BERRY</c>).</summary>
    public bool HasBerry(int patch) => this[patch] is { Stage: not BerryStage.None };

    // ------------------------------------------------------------------ what the player does

    /// <summary>
    /// Plants a berry (<c>BerryPatches_PlantInPatch</c>): its first stage under whatever mulch is down, the soil
    /// wet, the yield rating at its best, growing at once. The berry is the caller's to take from the bag.
    /// </summary>
    public void Plant(int patch, string berry)
    {
        var p = this[patch] ?? throw new ArgumentOutOfRangeException(nameof(patch));
        if (GrowthOf(berry) == null) throw new ArgumentException($"{berry} can't be planted.", nameof(berry));
        p.Berry = berry;
        p.Stage = BerryStage.Planted;
        p.StageMinutes = StageMinutesOf(berry, p.Mulch);
        p.MoistureMinutes = 0;
        p.Replants = 0;
        p.Yield = 0;
        p.Moisture = MostMoisture;
        p.YieldRating = MostYieldRating;
        p.Growing = true;
    }

    /// <summary>Lays mulch on the soil (<c>BerryPatches_SetPatchMulchType</c>); it stays until the patch is picked or dies.</summary>
    public void LayMulch(int patch, Mulch mulch)
    {
        var p = this[patch] ?? throw new ArgumentOutOfRangeException(nameof(patch));
        p.Mulch = mulch;
    }

    /// <summary>Waters the soil (<c>BerryPatches_ResetPatchMoisture</c>): its moisture back to the full 100.</summary>
    public void Water(int patch)
    {
        if (this[patch] is { } p) p.Moisture = MostMoisture;
    }

    /// <summary>
    /// Picks what the plant bears (<c>BerryPatches_HarvestPatch</c>): the berry and how many, and the patch is
    /// empty again, mulch and all. Nothing (null, 0) from a patch not in fruit.
    /// </summary>
    public (string? Berry, int Count) Pick(int patch)
    {
        var p = this[patch];
        if (p is not { Stage: BerryStage.Fruit, Berry: { } berry }) return (null, 0);
        int count = p.Yield;
        p.Clear();
        return (berry, count);
    }

    /// <summary>
    /// The player has seen the patch (<c>BerryPatches_UpdateGrowthStates</c>): it grows from now on. The original
    /// asks this of every patch in view as the player comes into a place, so what a new game finds growing waits,
    /// in fruit, for the player to come by.
    /// </summary>
    public void See(int patch)
    {
        if (this[patch] is { } p) p.Growing = true;
    }

    /// <summary>The half-width and half-height, in tiles, of what the original's field shows round the player: the screen's eight tiles to either side and six up and down.</summary>
    public const int ViewHalfWidth = 8, ViewHalfHeight = 6;

    /// <summary>Whether a patch at a tile is in view of a player at another.</summary>
    public static bool InView(int px, int py, int x, int y) => Math.Abs(x - px) <= ViewHalfWidth && Math.Abs(y - py) <= ViewHalfHeight;

    // ------------------------------------------------------------------ time

    /// <summary>
    /// The minutes go by (<c>BerryPatches_ElapseMinutes</c>), for every patch that grows. A patch that has lived out
    /// its whole life in them is gone; otherwise it goes stage by stage, its soil drying as each stage's minutes
    /// pass, its crop counted as it comes into fruit, and in fruit for four stages (six with Stable Mulch) before it
    /// drops its berries and seeds itself again, ten times (fifteen with Gooey Mulch).
    /// </summary>
    public void MinutesPass(int minutes)
    {
        if (minutes <= 0) return;
        foreach (var p in Patches)
        {
            if (p.Berry == null || p.Stage == BerryStage.None || !p.Growing) continue;

            int lifetime = StageMinutesOf(p.Berry, p.Mulch) * LifetimeStages(p.Mulch);
            if (minutes >= lifetime)
            {
                p.Clear();
                continue;
            }

            int left = minutes;
            while (p.Stage != BerryStage.None && left != 0)
            {
                if (p.StageMinutes > left)
                {
                    Dry(p, left);
                    p.StageMinutes -= left;
                    break;
                }
                // The stage runs out: the soil dries for what was left of it, and the plant moves on. A plant gone
                // after its last seeding has no minutes left to count (the original subtracts its cleared nought,
                // and then reads the growth table at berry -1, which is never used)
                Dry(p, p.StageMinutes);
                int spent = p.StageMinutes;
                Advance(p);
                left -= p.Stage == BerryStage.None ? 0 : spent;
                if (p.Stage == BerryStage.None) break;
                p.StageMinutes = StageMinutesOf(p.Berry, p.Mulch);
                if (p.Stage == BerryStage.Fruit) p.StageMinutes *= HarvestStagesOf(p.Mulch);
            }
        }
    }

    /// <summary>
    /// The next stage (<c>AdvancePatchGrowth</c>): the plant grows; coming into fruit it bears its berry's yield for
    /// each point of its rating, two at the least; out of fruit it drops them and sprouts again with its rating
    /// whole, until it has done so as often as it can.
    /// </summary>
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
                p.Yield = Math.Max(2, (GrowthOf(p.Berry)?.Yield ?? 0) * p.YieldRating);
                p.Stage++;
                break;
            case BerryStage.Fruit:
                p.Yield = 0;
                p.Stage = BerryStage.Sprouted;
                p.YieldRating = MostYieldRating;
                p.Replants++;
                if (p.Replants == ReplantsOf(p.Mulch)) p.Clear();
                break;
        }
    }

    /// <summary>
    /// The soil dries (<c>DrainPatchMoisture</c>), never while the plant is in fruit: for each whole hour it loses the
    /// berry's drain, and each hour spent bone dry costs the crop a point of its yield rating. The minutes short of
    /// an hour are kept for the next time.
    /// </summary>
    private static void Dry(BerryPatch p, int minutes)
    {
        if (p.Stage == BerryStage.Fruit) return;
        int drain = DrainOf(p.Berry, p.Mulch);
        minutes += p.MoistureMinutes;
        int hours = minutes / 60;
        p.MoistureMinutes = minutes % 60;
        if (hours == 0) return;
        if (p.Moisture >= drain * hours)
        {
            p.Moisture -= drain * hours;
            return;
        }
        if (p.Moisture > 0)
        {
            int untilDry = (p.Moisture + (drain - 1)) / drain;
            hours -= untilDry;
            p.Moisture = 0;
        }
        p.YieldRating = p.YieldRating > hours ? p.YieldRating - hours : 0;
    }
}
