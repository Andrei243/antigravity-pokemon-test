using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models.Underground;

/// <summary>The flag on a base's PC (<c>BaseFlagType</c>): what the flags brought home have earned.</summary>
public enum BaseFlag
{
    Normal,
    Bronze,
    Silver,
    Gold,
    Platinum
}

/// <summary>
/// The secret base's rules (plan 06 · R16a; <c>src/underground.c</c>, <c>src/underground/secret_bases.c</c> and
/// <c>base_decoration.c</c>): where the Digger Drill opens one, the cavern it opens with its sixteen big boulders,
/// what the flags brought home unlock, which tiles goods block and where one may stand. The base is a room of 32
/// tiles each way; its floor is the twelve by twelve in its middle, its PC stands at the back and its way out is at
/// the front. No drawing or input.
/// </summary>
public static class SecretBaseRules
{
    public const int Size = 32;

    /// <summary>The floor goods may stand on (<c>SECRET_BASE_PLAYABLE_AREA</c>).</summary>
    public const int FloorLeft = 10, FloorRight = 21, FloorTop = 12, FloorBottom = 23;

    /// <summary>The PC's two tiles (<c>PC_COORDINATE</c>) and the mat that leads out (<c>BASE_EXIT</c>).</summary>
    public const int PcX = 15, PcZ = 12, ExitX = 15, ExitZ = 24;

    public const int MaxGoods = 15, MaxBoulders = 16;

    /// <summary>The flags that earn each flag in turn (<c>FLAG_CAPTURED_COUNT_…</c>).</summary>
    public static BaseFlag FlagFor(int captured) =>
        captured >= 50 ? BaseFlag.Platinum : captured >= 10 ? BaseFlag.Gold : captured >= 3 ? BaseFlag.Silver : captured >= 1 ? BaseFlag.Bronze : BaseFlag.Normal;

    /// <summary><c>SecretBase_GetUnremovableBoulderCount</c>: how many of the sixteen boulders must stay.</summary>
    public static int BouldersThatStay(int captured) => FlagFor(captured) switch
    {
        BaseFlag.Platinum => 0,
        BaseFlag.Gold => 6,
        BaseFlag.Silver => 11,
        BaseFlag.Bronze => 15,
        _ => MaxBoulders
    };

    /// <summary><c>SecretBase_GetPlacedGoodsLimit</c>: ten goods, twelve with a bronze flag, fifteen from the silver one.</summary>
    public static int GoodsLimit(int captured) => FlagFor(captured) switch
    {
        BaseFlag.Normal => 10,
        BaseFlag.Bronze => 12,
        _ => MaxGoods
    };

    /// <summary>The good a PC is, by its flag (<c>SecretBase_GetPCGoodID</c>).</summary>
    public static int PcGood(int captured) => UndergroundDatabase.GoodId(FlagFor(captured) switch
    {
        BaseFlag.Platinum => "UG_GOOD_PC_PLATINUM_FLAG",
        BaseFlag.Gold => "UG_GOOD_PC_GOLD_FLAG",
        BaseFlag.Silver => "UG_GOOD_PC_SILVER_FLAG",
        BaseFlag.Bronze => "UG_GOOD_PC_BRONZE_FLAG",
        _ => "UG_GOOD_PC_NORMAL_FLAG"
    });

    public static int BoulderGood => UndergroundDatabase.GoodId("UG_GOOD_BIG_BOULDER");

    /// <summary>
    /// <c>SecretBases_ProcessBaseCreateRequest</c>: the drill opens a base in the tunnels' own rock, where the tile
    /// ahead and the two beside it across the way faced are all rock.
    /// </summary>
    public static bool CanDrill(Func<int, int, bool> rock, int x, int z, Direction facing)
    {
        if (UndergroundField.InABase(x, z)) return false;
        bool upOrDown = facing is Direction.Up or Direction.Down;
        var (ax, az) = upOrDown ? (x + 1, z) : (x, z + 1);
        var (bx, bz) = upOrDown ? (x - 1, z) : (x, z - 1);
        return rock(x, z) && rock(ax, az) && rock(bx, bz);
    }

    /// <summary>
    /// A new cavern (<c>SecretBases_CreateBase</c>): sixteen boulders on a lattice of two tiles, none in the two
    /// columns that lead from the way in to the PC, no two on one place; whatever stood in the old base goes back
    /// into the PC's list.
    /// </summary>
    public static SavedBase Create(int entranceX, int entranceZ, Direction drilled, Rand32 rand)
    {
        var made = new SavedBase { EntranceX = entranceX, EntranceZ = entranceZ, EntranceFacing = FieldMovement.Opposite(drilled) };
        const int rightOfPc = 18, xRange = 5, zRange = 6;
        var taken = new List<(int, int)> { (PcX + 1, PcZ) };
        while (made.Boulders.Count < MaxBoulders)
        {
            int x = rand.Next(xRange);
            x = x < 2 ? x * 2 + FloorLeft : (x - 3) * 2 + rightOfPc;
            int z = rand.Next(zRange) * 2 + FloorTop;
            if (taken.Contains((x, z))) continue;
            taken.Add((x, z));
            made.Boulders.Add(new Spot(x, z));
        }
        return made;
    }

    /// <summary>The tiles of the room a good's shape blocks, by its nine numbers, three to a row (<c>SecretBases_AddGoodCollisionToBaseCollision</c>).</summary>
    public static IEnumerable<(int X, int Z)> Blocked(int good, int x, int z)
    {
        string collision = UndergroundDatabase.Good(good)?.Collision ?? "";
        for (int i = 0; i < collision.Length; i++)
            if (collision[i] != '0') yield return (x + i % 3, z + i / 3);
    }

    /// <summary>Every tile of the base that can't be walked for what stands in it: the PC's two, the boulders' four each, and each good's own.</summary>
    public static HashSet<(int X, int Z)> BlockedTiles(SavedBase room)
    {
        var blocked = new HashSet<(int, int)> { (PcX, PcZ), (PcX + 1, PcZ) };
        foreach (var boulder in room.Boulders)
            foreach (var tile in Blocked(BoulderGood, boulder.X, boulder.Z)) blocked.Add(tile);
        foreach (var good in room.Goods)
            foreach (var tile in Blocked(good.Good, good.X, good.Z)) blocked.Add(tile);
        return blocked;
    }

    /// <summary>The good whose blocked tile this is (<c>SecretBases_GetGoodWithCollisionAtCoordinates</c>): what is read when faced.</summary>
    public static PlacedGood? GoodBlocking(SavedBase room, int x, int z) => room.Goods.FirstOrDefault(g => Blocked(g.Good, g.X, g.Z).Contains((x, z)));

    /// <summary>The good whose north-west tile this is (<c>SecretBases_GetPlacedGoodAtCoordinates</c>): a tool goes off under whoever steps there.</summary>
    public static PlacedGood? GoodStandingAt(SavedBase room, int x, int z) => room.Goods.FirstOrDefault(g => g.X == x && g.Z == z);

    /// <summary>The trap a tool of the base is (<c>SecretBases_GetTrapIDFromGoodID</c>); 0 for any other good.</summary>
    public static int ToolTrap(int good)
    {
        string id = UndergroundDatabase.Good(good)?.Id ?? "";
        if (!id.Contains("_TOOL", StringComparison.Ordinal)) return 0;
        // UG_GOOD_ALERT_TOOL_1 is TRAP_ALERT_1, UG_GOOD_BIG_SMOKE_TOOL is TRAP_BIG_SMOKE
        return UndergroundDatabase.TrapId("TRAP_" + id["UG_GOOD_".Length..].Replace("_TOOL", ""));
    }

    /// <summary>
    /// The player's base as the decorating screen is given it (<c>base_decoration.c</c>): the PC and the boulders as
    /// fixtures, the goods on display as pieces out of the PC's store, the tiles before the PC and inside the way out
    /// kept clear (<c>BaseDecoration_CanPlaceGood</c>), as many goods out as the flag allows, the PC never moved or
    /// put away, a boulder never moved and taken away only while more stand than the flag keeps.
    /// </summary>
    public static DecoratingSession Decorating(UndergroundState state)
    {
        var room = state.Base ?? new SavedBase();
        int flags = state.Records.FlagsCaptured, stay = BouldersThatStay(flags);
        int boulder = BoulderGood, pc = PcGood(flags);
        var pieces = new List<RoomPiece> { new() { Kind = pc, X = PcX, Y = PcZ, Width = 2, Depth = 1, Name = "PC" } };
        pieces.AddRange(room.Boulders.Select(b => new RoomPiece { Kind = boulder, X = b.X, Y = b.Z, Width = 2, Depth = 2, Name = "Big Boulder" }));
        pieces.AddRange(room.Goods.Select(g => new RoomPiece
        {
            Kind = g.Good, X = g.X, Y = g.Z, Slot = g.Slot, Name = UndergroundDatabase.GoodName(g.Good),
            Width = UndergroundDatabase.Good(g.Good)?.Width ?? 1, Depth = UndergroundDatabase.Good(g.Good)?.Depth ?? 1
        }));
        int Boulders() => pieces.Count(p => p.Kind == boulder && p.IsFixture);
        var decorated = new DecorationRoom
        {
            Left = FloorLeft, Top = FloorTop, Width = FloorRight - FloorLeft + 1, Height = FloorBottom - FloorTop + 1,
            KeptClear = { (PcX, PcZ + 1), (PcX + 1, PcZ + 1), (ExitX, ExitZ - 1) },
            Pieces = pieces,
            Limit = GoodsLimit(flags),
            Start = (PcX + 1, PcZ + 1),
            WhyNotMove = piece => piece.Kind == pc ? "The PC can't be moved." : piece.Kind == boulder && piece.IsFixture ? "The big boulder can't be moved." : null,
            WhyNotTakeAway = piece =>
                piece.Kind == pc ? "The PC can't be put away."
                : piece.Kind != boulder || !piece.IsFixture ? null
                : stay == MaxBoulders ? "The big boulder can't be taken away. Bring a flag home first."
                : Boulders() - 1 < stay ? "No more boulders can be taken away for now." : null,
            AfterTakingAway = _ =>
            {
                int more = Boulders() - stay;
                return more <= 0 ? "The big boulder was taken away." : more == 1 ? "The big boulder was taken away. One more can go." : $"The big boulder was taken away. {more} more can go.";
            }
        };
        var store = state.PcGoods.Select((good, slot) => new StoredDecoration { Kind = good, Out = state.IsDisplayed(slot) }).ToList();
        return new DecoratingSession(decorated, store, kind =>
        {
            var record = UndergroundDatabase.Good(kind);
            return new DecorationKind(kind, record?.Name ?? "", record?.Width ?? 1, record?.Depth ?? 1);
        });
    }

    /// <summary>What stands in the decorated room is the base's from now on (<c>BaseDecoration_SaveBaseGoods</c>).</summary>
    public static void Keep(SavedBase room, DecorationRoom decorated)
    {
        room.Boulders = decorated.Pieces.Where(p => p.IsFixture && p.Kind == BoulderGood).Select(p => new Spot(p.X, p.Y)).ToList();
        room.Goods = decorated.Pieces.Where(p => !p.IsFixture).Select(p => new PlacedGood { Good = p.Kind, X = p.X, Z = p.Y, Slot = p.Slot }).ToList();
    }

    /// <summary>What a good shows of the records when read, in our own words; null for one with nothing written on it.</summary>
    public static string? RecordLine(int good, UndergroundRecords records) => UndergroundDatabase.Good(good)?.Record switch
    {
        "FossilsDug" => $"Something is written on it... Fossils dug up: {records.FossilsDug}",
        "SpheresDug" => $"Something is written on it... Spheres dug up: {records.SpheresDug}",
        "TrapHits" => $"Something is written on it... Traps that caught someone: {records.TrapHits}",
        "TrapsHit" => $"Something is written on it... Traps stepped on: {records.TrapsTriggered}",
        "TimesBaseMoved" => $"Something is written on it... Times the base was moved: {records.TimesBaseMoved}",
        "FlagsObtained" or "FlagsRegistered" => $"Something is written on it... Flags brought home: {records.FlagsCaptured}",
        "TotalScore" => $"Something is written on it... Treasures dug up: {records.TreasuresDug}",
        null => null,
        // What counted other players stays at nought: nobody else comes down here
        _ => "Something is written on it... 0"
    };
}

/// <summary>What a hiker has on offer: the thing, and the Sphere he asks for it.</summary>
public readonly record struct VendorOffer(int Thing, SphereKind Kind, int Size);

/// <summary>Why a trade with a hiker didn't happen (<c>RESULT_…</c> in <c>vendors.c</c>).</summary>
public enum TradeResult
{
    Done,
    WrongSphere,
    SphereTooSmall,
    BagFull,
    NotForTrade
}

/// <summary>
/// The twenty hikers of the tunnels (<c>src/underground/vendors.c</c>): eight trade goods and eight trade traps for
/// Spheres, five things each, drawn from the day's number and the hiker's own, so the stock changes with the day;
/// four give Spheres for treasures. Each also gives a Sphere for what he trades in, half the size he would ask.
/// </summary>
public static class UndergroundVendors
{
    public const int StockSize = 5;

    public static string KindOf(int vendor) => UndergroundDatabase.Vendors[vendor].Kind;

    /// <summary>The hiker standing on a tile, or -1 (<c>UndergroundVendors_FindVendorAtCoordinates</c>).</summary>
    public static int At(int x, int z)
    {
        for (int i = 0; i < UndergroundDatabase.Vendors.Count; i++)
            if (UndergroundDatabase.Vendors[i].X == x && UndergroundDatabase.Vendors[i].Z == z) return i;
        return -1;
    }

    private static SphereKind AnyKind(Rand16 rand) => (SphereKind)(rand.Next(5) + 1);

    /// <summary>
    /// The day's stock (<c>UndergroundVendors_InitTrapsVendorInventory</c>, <c>…InitGoodsVendorInventory</c>): a trap
    /// hiker's first is always the Digger Drill; nothing twice, nothing that has no price, and no good that waits for
    /// the National Pokédex before it is had. The draws are the original's, in its order.
    /// </summary>
    public static List<VendorOffer> Stock(uint seed, int vendor, bool national)
    {
        var rand = new Rand16(unchecked(seed + (uint)vendor));
        var stock = new List<VendorOffer>();
        string kind = KindOf(vendor);
        if (kind == "Traps")
        {
            while (stock.Count < StockSize)
            {
                int trap = stock.Count == 0 ? UndergroundDatabase.TrapId("TRAP_DIGGER_DRILL") : rand.Next(UndergroundDatabase.TrapCount) + 1;
                if (stock.Any(o => o.Thing == trap)) continue;
                var price = UndergroundDatabase.Trap(trap)!.Price;
                if (price.Min == 0 && price.Max == 0) continue;
                var asked = price.Any ? AnyKind(rand) : price.Kind;
                stock.Add(new VendorOffer(trap, asked, rand.Next(price.Max - price.Min) + price.Min));
            }
        }
        else if (kind == "Goods")
        {
            while (stock.Count < StockSize)
            {
                int good = rand.Next(UndergroundDatabase.GoodCount) + 1;
                if (stock.Any(o => o.Thing == good)) continue;
                var record = UndergroundDatabase.Good(good)!;
                if (!national && record.National) continue;
                if (!record.Price.ForTrade) continue;
                var asked = record.Price.Any ? AnyKind(rand) : record.Price.Kind;
                stock.Add(new VendorOffer(good, asked, rand.Next(record.Price.Max - record.Price.Min) + record.Price.Min));
            }
        }
        return stock;
    }

    /// <summary><c>UndergroundVendors_TryBuyItem</c>: the Sphere at a place of the bag for an offer, if it is of the kind asked and no smaller.</summary>
    public static TradeResult Buy(UndergroundState state, int vendor, VendorOffer offer, int sphereSlot)
    {
        if (sphereSlot < 0 || sphereSlot >= state.Spheres.Count) return TradeResult.WrongSphere;
        var sphere = state.Spheres[sphereSlot];
        if (sphere.Kind != offer.Kind) return TradeResult.WrongSphere;
        if (offer.Size > sphere.Size) return TradeResult.SphereTooSmall;
        bool added = KindOf(vendor) == "Traps" ? state.TryAddTrap(offer.Thing) : state.TryAddGood(offer.Thing);
        if (!added) return TradeResult.BagFull;
        state.Spheres.RemoveAt(sphereSlot);
        return TradeResult.Done;
    }

    /// <summary>
    /// What a hiker gives for a thing of the kind he trades in (<c>UndergroundVendors_PrintItemDescriptionAndSellPrice</c>):
    /// a Sphere of the kind and a size drawn from the day's number, the hiker's and the thing's; half that size for a
    /// trap or a good, the whole for a treasure. Null for a thing nobody trades.
    /// </summary>
    public static Sphere? Worth(uint seed, int vendor, int thing)
    {
        string kind = KindOf(vendor);
        var price = kind switch
        {
            "Traps" => UndergroundDatabase.Trap(thing)?.Price,
            "Goods" => UndergroundDatabase.Good(thing)?.Price,
            _ => UndergroundDatabase.Treasure(thing)?.Price
        };
        if (price == null || !price.ForTrade) return null;
        var rand = new Rand16(unchecked(seed + (uint)vendor + (uint)thing));
        var given = price.Any ? AnyKind(rand) : price.Kind;
        int size = rand.Next(price.Max - price.Min) + price.Min;
        return new Sphere(given, kind == "Treasures" ? size : size / 2);
    }

    /// <summary><c>UndergroundVendors_TrySellItem</c>: the thing at a place of its list goes, and its Sphere comes into the bag.</summary>
    public static TradeResult Sell(UndergroundState state, int vendor, int slot)
    {
        string kind = KindOf(vendor);
        var list = kind switch { "Traps" => state.Traps, "Goods" => state.Goods, _ => state.Treasures };
        if (slot < 0 || slot >= list.Count) return TradeResult.NotForTrade;
        var worth = Worth(state.Seed, vendor, list[slot]);
        if (worth == null || worth.Size < 1) return TradeResult.NotForTrade;
        if (!state.TryAddSphere(worth.Kind, worth.Size)) return TradeResult.BagFull;
        list.RemoveAt(slot);
        return TradeResult.Done;
    }
}

/// <summary>One of the day's other diggers: a name, the wall their base is behind, and the cavern itself.</summary>
public sealed record Digger(int Number, string Name, SavedBase Base);

/// <summary>
/// The stand-in for other players' flags (plan 06 · R16a; decision 06 · 7 leaves wireless play out): each day up to
/// seven other diggers have a base somewhere in the tunnels, drawn from the day's number. Their flag is taken from
/// their PC and carried home, as another player's was; each gives one a day.
/// </summary>
public static class Diggers
{
    /// <summary>As many as a wireless session held beside the player.</summary>
    public const int Count = 7;

    private static readonly string[] Names =
    {
        "Bram", "Odette", "Corwin", "Mirela", "Tavish", "Ilse", "Rurik", "Nadia", "Per", "Zofia", "Emrys", "Linnea", "Dov", "Sabela",
        "Kito", "Marit", "Anselm", "Yara", "Wim", "Tindra", "Oskar", "Beatrix", "Jarek", "Solveig"
    };

    /// <summary>
    /// The day's diggers and where each has drilled: a wall of the tunnels' own rock with floor to the south of it,
    /// found from the day's number (none where the search fails, so a day may have fewer).
    /// </summary>
    public static List<Digger> Today(uint seed, Func<int, int, bool> rock)
    {
        var rand = new Rand16(unchecked(seed ^ 0x5F1A6u));
        var diggers = new List<Digger>();
        for (int n = 0; n < Count; n++)
        {
            string name = Names[rand.Next(Names.Length)];
            int x = 0, z = 0;
            bool found = false;
            for (int attempt = 0; attempt < 200 && !found; attempt++)
            {
                x = rand.Next(UndergroundField.MaxX - UndergroundField.MainLeft - 2) + UndergroundField.MainLeft + 1;
                z = rand.Next(UndergroundField.MaxZ - UndergroundField.MainTop - 2) + UndergroundField.MainTop + 1;
                found = SecretBaseRules.CanDrill(rock, x, z, Direction.Up) && !rock(x, z + 1) && diggers.All(d => d.Base.EntranceX != x || d.Base.EntranceZ != z);
            }
            if (!found || diggers.Any(d => d.Name == name)) continue;
            var cavern = SecretBaseRules.Create(x, z, Direction.Up, new Rand32(unchecked(seed * 31u + (uint)n)));
            // A digger has cleared some of the boulders and put three to seven goods out, where the boulders left room
            int cleared = rand.Next(9);
            cavern.Boulders.RemoveRange(0, Math.Min(cleared, cavern.Boulders.Count));
            int goods = rand.Next(5) + 3;
            var taken = SecretBaseRules.BlockedTiles(cavern);
            for (int tries = 0; tries < goods * 6 && cavern.Goods.Count < goods; tries++)
            {
                int good = rand.Next(UndergroundDatabase.GoodCount) + 1;
                var record = UndergroundDatabase.Good(good)!;
                int gx = rand.Next(SecretBaseRules.FloorRight - SecretBaseRules.FloorLeft - 2) + SecretBaseRules.FloorLeft;
                int gz = rand.Next(SecretBaseRules.FloorBottom - SecretBaseRules.FloorTop - 3) + SecretBaseRules.FloorTop + 1;
                if (!record.Price.ForTrade || SecretBaseRules.ToolTrap(good) != 0) continue;
                var rect = Enumerable.Range(gx, record.Width).SelectMany(cx => Enumerable.Range(gz, record.Depth).Select(cz => (cx, cz))).ToList();
                // Clear of everything, of the tiles before the PC and of the way in from the mat
                if (rect.Any(t => taken.Contains(t) || t.cx is SecretBaseRules.PcX or SecretBaseRules.PcX + 1 && t.cz == SecretBaseRules.PcZ + 1
                                  || t.cx == SecretBaseRules.ExitX || t.cx > SecretBaseRules.FloorRight || t.cz > SecretBaseRules.FloorBottom)) continue;
                cavern.Goods.Add(new PlacedGood { Good = good, X = gx, Z = gz, Slot = -1 });
                foreach (var t in rect) taken.Add(t);
            }
            diggers.Add(new Digger(n, name, cavern));
        }
        return diggers;
    }

    /// <summary>Whether a digger's flag is still on their PC today: one flag is carried at a time, and each digger gives one a day.</summary>
    public static bool CanTake(UndergroundState state, int digger) => state.HeldFlag == null && !state.FlagsTakenToday.Contains(digger);

    public static bool Take(UndergroundState state, int digger)
    {
        if (!CanTake(state, digger)) return false;
        state.HeldFlag = digger;
        state.FlagsTakenToday.Add(digger);
        return true;
    }

    /// <summary>
    /// The flag carried is put on the player's own PC (<c>UndergroundPlayer_ProcessRegisteredFlag</c>): one more flag
    /// brought home, and its owner's name among the last five. Returns the base's flag if this one earned a better.
    /// </summary>
    public static BaseFlag? BringHome(UndergroundState state, string owner)
    {
        if (state.HeldFlag == null) return null;
        var before = SecretBaseRules.FlagFor(state.Records.FlagsCaptured);
        state.HeldFlag = null;
        state.Records.FlagsCaptured = UndergroundRecords.Add(state.Records.FlagsCaptured, 1);
        state.FlagOwners.Insert(0, owner);
        if (state.FlagOwners.Count > 5) state.FlagOwners.RemoveRange(5, state.FlagOwners.Count - 5);
        var after = SecretBaseRules.FlagFor(state.Records.FlagsCaptured);
        return after != before ? after : null;
    }
}
