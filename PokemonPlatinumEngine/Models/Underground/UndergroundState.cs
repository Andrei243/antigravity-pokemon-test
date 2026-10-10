using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Models.Underground;

/// <summary>
/// The console's own generators (<c>MATH_Rand16</c> and <c>MATH_Rand32</c> of its software kit, which the
/// decompilation calls and doesn't hold): a number times a constant plus another, of which the upper half scaled to
/// the range asked for is the draw. The Underground spreads its walls and traps, grows its Spheres and stocks its
/// hikers with the 16-bit one, seeded by the number it saves, so a day's Underground is the same however often it is
/// gone down to; the wall's game draws with the 32-bit one.
/// </summary>
public sealed class Rand16
{
    private uint x;

    public Rand16(uint seed) => x = seed;

    /// <summary>A number from 0 to one less than <paramref name="max"/> (0 for a range of nothing or of one).</summary>
    public int Next(int max)
    {
        x = unchecked(x * 1566083941u + 2531011u);
        return (int)(((x >> 16) * (uint)Math.Max(0, max)) >> 16);
    }
}

/// <summary>The 32-bit generator (<see cref="Rand16"/>).</summary>
public sealed class Rand32
{
    private ulong x;

    public Rand32(ulong seed) => x = seed;

    public int Next(int max)
    {
        x = unchecked(x * 0x5D588B656C078965ul + 2531011ul);
        return (int)(((x >> 32) * (ulong)Math.Max(0, max)) >> 32);
    }
}

/// <summary>A Sphere in the bag: its kind and its size, 1 to 99.</summary>
public sealed class Sphere
{
    public SphereKind Kind { get; set; }
    public int Size { get; set; }

    public Sphere() { }

    public Sphere(SphereKind kind, int size)
    {
        Kind = kind;
        Size = size;
    }
}

/// <summary>A Sphere buried in the floor of a tunnel: its size as it was buried, and what it has grown since.</summary>
public sealed class BuriedSphere
{
    public SphereKind Kind { get; set; }
    public int X { get; set; }
    public int Z { get; set; }
    public int Size { get; set; }
    public int Growth { get; set; }

    /// <summary>What it is worth dug up: never more than 99.</summary>
    public int Worth => Math.Min(UndergroundState.MaxSphereSize, Size + Growth);
}

/// <summary>A tile of the Underground's map.</summary>
public sealed class Spot
{
    public int X { get; set; }
    public int Z { get; set; }

    public Spot() { }

    public Spot(int x, int z)
    {
        X = x;
        Z = z;
    }
}

/// <summary>A trap lying under a tile: one the day spread about, or one the player set.</summary>
public sealed class BuriedTrap
{
    public int Trap { get; set; }
    public int X { get; set; }
    public int Z { get; set; }
}

/// <summary>A good standing in the base: which, where its north-west tile is, and its place in the PC's list.</summary>
public sealed class PlacedGood
{
    public int Good { get; set; }
    public int X { get; set; }
    public int Z { get; set; }
    public int Slot { get; set; }
}

/// <summary>
/// The player's base (<c>SecretBase</c>): the wall it was drilled into and the way the driller faced, the big
/// boulders the cavern came with (each two tiles by two) and the goods put out in it. Its tiles are the room's own,
/// from 0 to 31 each way, as the original's are.
/// </summary>
public sealed class SavedBase
{
    public int EntranceX { get; set; }
    public int EntranceZ { get; set; }

    /// <summary>The way out of the entrance: opposite to the way the drill went in.</summary>
    public Direction EntranceFacing { get; set; }
    public List<Spot> Boulders { get; set; } = new();
    public List<PlacedGood> Goods { get; set; } = new();
}

/// <summary>
/// The Underground's records (<c>UndergroundRecord</c>), each stopping at 999,999. Those that count other players
/// (people met, gifts, others helped, flags taken from this base and won back) are kept at nought: nobody else is
/// ever down here.
/// </summary>
public sealed class UndergroundRecords
{
    public const int Most = 999999;

    public int SpheresDug { get; set; }
    public int FossilsDug { get; set; }
    public int TreasuresDug { get; set; }
    public int TrapsTriggered { get; set; }
    public int TrapHits { get; set; }
    public int TimesBaseMoved { get; set; }
    public int FlagsCaptured { get; set; }

    public static int Add(int value, int amount) => Math.Min(Most, value + Math.Max(0, amount));
}

/// <summary>Where the player stood above ground when they went down: where they come up again.</summary>
public sealed class SurfaceSpot
{
    public string Map { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public Direction Facing { get; set; }
}

/// <summary>
/// Everything the game keeps of the Underground (the original's <c>Underground</c> save block, plan 06 · R16a): the
/// bag's Spheres, treasures, traps and goods and the PC's goods, with the original's room for each; what lies buried
/// (the walls that hold something, the day's traps, the player's own traps and Spheres); the base; the records; and
/// the number the day's spread is drawn from. No drawing or input.
/// </summary>
public sealed class UndergroundState
{
    public const int MaxSpheres = 40, MaxTreasures = 40, MaxTraps = 40, MaxGoods = 40, MaxPcGoods = 200;
    public const int MaxBuriedSpheres = 100, MaxPlacedTraps = 16, MaxSpawnedTraps = 64, MaxMiningSpots = 250;
    public const int MaxSphereSize = 99;

    public List<Sphere> Spheres { get; set; } = new();
    public List<int> Treasures { get; set; } = new();
    public List<int> Traps { get; set; } = new();
    public List<int> Goods { get; set; } = new();
    public List<int> PcGoods { get; set; } = new();

    public List<BuriedSphere> Buried { get; set; } = new();
    public List<Spot> MiningSpots { get; set; } = new();
    public List<BuriedTrap> SpawnedTraps { get; set; } = new();
    public List<BuriedTrap> PlacedTraps { get; set; } = new();

    public SavedBase? Base { get; set; }
    public UndergroundRecords Records { get; set; } = new();

    /// <summary>The number the day's walls, traps, growth and stock are drawn from (<c>randomSeed</c>); a new day moves it on.</summary>
    public uint Seed { get; set; }

    /// <summary>Whether a wall has ever been dug at: the first is easier and comes with a word of advice.</summary>
    public bool HasMined { get; set; }

    /// <summary>Set by a new day: the walls and traps are spread afresh the next time the player goes down.</summary>
    public bool SpreadAnew { get; set; } = true;

    /// <summary>A bit for each plate dug up already: a plate is found once (<c>minedPlates</c>).</summary>
    public uint MinedPlates { get; set; }

    /// <summary>The steps taken below, 0 to 99 and round again (<c>stepCount</c>).</summary>
    public int Steps { get; set; }

    public SurfaceSpot? Surface { get; set; }

    /// <summary>The stand-in for other players' flags (<see cref="Diggers"/>): whose flag is carried now, whose were taken today, and the last five brought home.</summary>
    public int? HeldFlag { get; set; }

    /// <summary>Whose cavern the base's room shows while the player is in it: a digger's number, or null for the player's own.</summary>
    public int? Visiting { get; set; }
    public List<int> FlagsTakenToday { get; set; } = new();
    public List<string> FlagOwners { get; set; } = new();

    /// <summary>
    /// A new game's Underground: nothing in it, and a number to draw its first day from. The original takes the
    /// clock's; here it is a step on from a number the game has drawn already (the day's, of its wild Pokémon), so
    /// that beginning a game draws nothing more from its chance than it did before there was an Underground.
    /// </summary>
    public static UndergroundState NewGame(uint from) => new() { Seed = unchecked(from * 1812433253u + 1u) };

    // ------------------------------------------------------------------ the bag

    public bool TryAddSphere(SphereKind kind, int size)
    {
        if (kind == SphereKind.None || Spheres.Count >= MaxSpheres) return false;
        Spheres.Add(new Sphere(kind, Math.Clamp(size, 1, MaxSphereSize)));
        return true;
    }

    public bool TryAddTreasure(int treasure)
    {
        if (UndergroundDatabase.Treasure(treasure) == null || UndergroundDatabase.IsSphere(treasure) || Treasures.Count >= MaxTreasures) return false;
        Treasures.Add(treasure);
        return true;
    }

    public bool TryAddTrap(int trap)
    {
        if (UndergroundDatabase.Trap(trap) == null || Traps.Count >= MaxTraps) return false;
        Traps.Add(trap);
        return true;
    }

    public bool TryAddGood(int good)
    {
        if (UndergroundDatabase.Good(good) == null || Goods.Count >= MaxGoods) return false;
        Goods.Add(good);
        return true;
    }

    public bool TryAddPcGood(int good)
    {
        if (UndergroundDatabase.Good(good) == null || PcGoods.Count >= MaxPcGoods) return false;
        PcGoods.Add(good);
        return true;
    }

    /// <summary><c>Underground_IsGoodAtSlotPlacedInBase</c>.</summary>
    public bool IsDisplayed(int pcSlot) => Base?.Goods.Any(g => g.Slot == pcSlot) ?? false;

    /// <summary>A good taken out of the PC's list (<c>Underground_RemoveGoodAtSlotPC</c>): never one that stands in the base. Those listed after it move up, the displayed ones with them.</summary>
    public int RemovePcGood(int slot)
    {
        if (slot < 0 || slot >= PcGoods.Count || IsDisplayed(slot)) return 0;
        int good = PcGoods[slot];
        PcGoods.RemoveAt(slot);
        if (Base != null)
            foreach (var placed in Base.Goods)
                if (placed.Slot > slot) placed.Slot--;
        return good;
    }

    /// <summary>A good from the bag into the PC (<c>UndergroundPC_TryDepositGood</c>).</summary>
    public bool Deposit(int bagSlot)
    {
        if (bagSlot < 0 || bagSlot >= Goods.Count || !TryAddPcGood(Goods[bagSlot])) return false;
        Goods.RemoveAt(bagSlot);
        return true;
    }

    /// <summary>A good from the PC into the bag, unless it stands in the base or the bag is full.</summary>
    public bool Withdraw(int pcSlot)
    {
        if (pcSlot < 0 || pcSlot >= PcGoods.Count || IsDisplayed(pcSlot) || Goods.Count >= MaxGoods) return false;
        Goods.Add(RemovePcGood(pcSlot));
        return true;
    }

    // ------------------------------------------------------------------ plates

    /// <summary><c>Underground_HasPlateNeverBeenMined</c>: true for anything that is no plate.</summary>
    public bool NeverMined(int treasure)
    {
        int first = UndergroundDatabase.TreasureId("TREASURE_FLAME_PLATE");
        return !UndergroundDatabase.IsPlate(treasure) || (MinedPlates & (1u << (treasure - first))) == 0;
    }

    public void NotePlate(int treasure)
    {
        if (UndergroundDatabase.IsPlate(treasure)) MinedPlates |= 1u << (treasure - UndergroundDatabase.TreasureId("TREASURE_FLAME_PLATE"));
    }

    // ------------------------------------------------------------------ the day

    /// <summary>The least and the spread of what each kind of Sphere grows in a day (<c>baseGrowthRate</c>, <c>growthRateRange</c>).</summary>
    private static readonly int[] GrowthBase = { 0, 1, 1, 3, 3, 5 }, GrowthRange = { 0, 2, 2, 4, 4, 5 };

    /// <summary>
    /// <c>Underground_HandleDailyEvents</c>: every buried Sphere grows for each day gone by, 99 days at most, by a draw
    /// of its kind's range on the day's number; then the number moves on and the walls and traps are to be spread anew.
    /// </summary>
    public void DaysPass(int days)
    {
        if (days <= 0) return;
        var rand = new Rand16(Seed);
        for (int day = 0; day < Math.Min(days, 99); day++)
            foreach (var sphere in Buried)
            {
                int growth = rand.Next(GrowthRange[(int)sphere.Kind]) + GrowthBase[(int)sphere.Kind];
                sphere.Growth = sphere.Growth + growth < MaxSphereSize ? sphere.Growth + growth : MaxSphereSize;
            }
        Seed = Personality.NextAlternate(unchecked(Seed + (uint)days));
        SpreadAnew = true;
        FlagsTakenToday.Clear();
    }

    /// <summary><c>Underground_IncrementStepCount</c>.</summary>
    public void Step() => Steps = Steps >= 99 ? 0 : Steps + 1;
}
