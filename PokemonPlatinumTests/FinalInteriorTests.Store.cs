using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>Veilstone's Department Store, the Pokémon Centers' other floors and the Global Terminal of plan 01 · M11, part d.</summary>
public partial class FinalInteriorTests
{
    private static readonly string[] Store = { "VeilstoneStoreB1F", "VeilstoneStore1F", "VeilstoneStore2F", "VeilstoneStore3F", "VeilstoneStore4F", "VeilstoneStore5F" };
    private static readonly string[] Terminal = { "GlobalTerminal1F", "GlobalTerminal2F", "GlobalTerminal3F" };

    private static IEnumerable<string> Centers => Directory.GetFiles(GameDataFiles.PathOf(MapDatabase.Folder), "*PokemonCenter.json").Select(Path.GetFileNameWithoutExtension)!;

    /// <summary>Whoever takes a way of a room is put down on a tile one can stand on, beside a way that leads back to the room.</summary>
    private static void LeadsBack(string from, Warp way)
    {
        var to = Room(way.TargetMap);
        string where = $"{from} ({way.SourceX},{way.SourceY}) to {way.TargetMap}";
        Assert.True(to.IsWalkable(way.TargetX, way.TargetY), $"{where} puts the player on a blocked tile ({way.TargetX},{way.TargetY})");
        Assert.True(to.Warps.Any(w => w.TargetMap == from && Math.Abs(w.SourceX - way.TargetX) + Math.Abs(w.SourceY - way.TargetY) <= 1),
            $"{where}: nothing beside ({way.TargetX},{way.TargetY}) leads back");
    }

    [Theory]
    [InlineData("veilstone_city", 3, "VeilstoneStore1F", 10, 12)]
    [InlineData("jubilife_city", 1, "GlobalTerminal1F", 13, 18)]
    public void TheStoresAndTheTerminalsDoorLeadsInAndBackOut(string area, int warp, string room, int matX, int matY)
    {
        var way = Sinnoh.Area(area)!.Warps[warp];
        var outside = BuiltMaps.Value["Sinnoh"];
        var door = outside.GetWarpAt(way.X, way.Z);
        Assert.Equal((room, matX, matY, Direction.Up), (door!.TargetMap, door.TargetX, door.TargetY, door.TargetFacing));
        var inside = Room(room);
        var exit = inside.GetWarpAt(matX, matY + 1)!;
        Assert.Equal(("Sinnoh", way.X, way.Z + 1, Direction.Down), (exit.TargetMap, exit.TargetX, exit.TargetY, exit.TargetFacing));
        Assert.True(outside.IsWalkable(way.X, way.Z + 1));
        // Veilstone has no door left shut, and Jubilife only the two of houses the original never used
        Assert.True(Sinnoh.Overlay("veilstone_city")!.Locked is not { Count: > 0 });
        Assert.Equal(new[] { 10, 11 }, Sinnoh.Overlay("jubilife_city")!.Locked);
    }

    [Fact]
    public void EveryoneOfTheStoreTheCentersFloorsAndTheTerminalStandsOnFloorWithSomethingToSay()
    {
        var rooms = Store.Append("VeilstoneStoreElevator").Concat(Terminal).Concat(Centers.SelectMany(c => new[] { c + "2F", c + "B1F" })).ToList();
        Assert.True(rooms.Count >= 46, $"only {rooms.Count} rooms");
        foreach (string name in rooms)
        {
            var map = Room(name);
            Assert.True(map.IsIndoors, name);
            Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap(name)!.Id);
            Assert.NotEmpty(map.Everyone);
            foreach (var who in map.Everyone)
            {
                string where = $"{name}: {who.Key} at {who.GridX},{who.GridY}";
                Assert.False(map.IsSolid(who.GridX, who.GridY), where + " stands in something solid");
                bool speaks = who.DialogLines.Count > 0 || who.IsPokeMartClerk || who.Script != null && ScriptLibrary.Default.Find(who.Script, name) != null;
                Assert.True(speaks, where + " has nothing to say");
            }
            foreach (var way in map.Warps.Where(w => w.TargetMap != "Sinnoh")) LeadsBack(name, way);
            Assert.DoesNotContain(ScriptLibrary.Default.Problems().ToList(), p => p.Contains(name + ".txt", StringComparison.Ordinal));
        }
    }

    // ------------------------------------------------------------------ the Department Store

    [Fact]
    public void TheStoresFloorsAreJoinedByEscalatorsBesideTheirFlights()
    {
        // B1F, 1F ... 5F: an escalator up on every floor but the roof and one down on every floor but the basement,
        // each a warp with its flight beside it (on the side the original's model stands) and its mat on the other
        for (int i = 0; i < Store.Length; i++)
        {
            var floor = Room(Store[i]);
            Assert.Equal(InteriorStyle.PokeMart, floor.Interior);
            var up = floor.Warps.SingleOrDefault(w => i + 1 < Store.Length && w.TargetMap == Store[i + 1]);
            var down = floor.Warps.SingleOrDefault(w => i > 0 && w.TargetMap == Store[i - 1]);
            Assert.Equal(i + 1 < Store.Length, up != null);
            Assert.Equal(i > 0, down != null);
            if (up != null)
            {
                Assert.Contains(floor.Props, p => p.Type == PropType.SideStairsUp && p.Y == up.SourceY && p.X + p.Width == up.SourceX);
                Assert.True(floor.IsWalkable(up.SourceX + 1, up.SourceY), $"{Store[i]}: nobody can step onto its way up");
                // One comes off it upstairs beside the way back down, walking away from it
                var back = Room(up.TargetMap).Warps.Single(w => w.TargetMap == Store[i]);
                Assert.Equal((back.SourceX - 1, back.SourceY, Direction.Left), (up.TargetX, up.TargetY, up.TargetFacing));
            }
            if (down != null)
            {
                Assert.Contains(floor.Props, p => p.Type == PropType.SideStairsDown && p.Y == down.SourceY && p.X == down.SourceX + 1);
                Assert.True(floor.IsWalkable(down.SourceX - 1, down.SourceY), $"{Store[i]}: nobody can step onto its way down");
                var back = Room(down.TargetMap).Warps.Single(w => w.TargetMap == Store[i]);
                Assert.Equal((back.SourceX + 1, back.SourceY, Direction.Right), (down.TargetX, down.TargetY, down.TargetFacing));
            }
        }
    }

    [Fact]
    public void TheStoresLiftStandsAtTheFloorOneCameInFromAndItsAttendantRidesToAnyOther()
    {
        var car = Room("VeilstoneStoreElevator");
        var ways = car.WarpsAt(3, 7).ToList();
        Assert.Equal(Store.Order(), ways.Select(w => w.TargetMap).Order());
        Assert.All(ways, w => Assert.StartsWith("FLAG_VEILSTONE_STORE_LIFT_AT_", w.From));
        Assert.Contains(car.Props, p => p.Type == PropType.LiftIndicator);
        string[] flags = { "B1F", "1F", "2F", "3F", "4F", "5F" };
        for (int i = 0; i < Store.Length; i++)
        {
            // Every floor's lift doors are a warp in its back wall on a tile of the behaviour Door, into the car
            var floor = Room(Store[i]);
            var doors = floor.Warps.Single(w => w.TargetMap == "VeilstoneStoreElevator");
            Assert.Equal(TileBehavior.Door, floor.BehaviourAt(doors.SourceX, doors.SourceY));
            Assert.Equal((3, 6, Direction.Up), (doors.TargetX, doors.TargetY, doors.TargetFacing));
            // Coming to a floor says where the car stands, and the car's door leads back out below that floor's doors
            var host = new HeadlessScriptHost();
            host.Story.Set("FLAG_VEILSTONE_STORE_LIFT_AT_" + flags[(i + 3) % 6]);
            Run("OnEnter", Store[i], host);
            Assert.Equal(i + 1, host.Story.Var("VAR_VEILSTONE_STORE_LIFT_FLOOR"));
            Assert.Equal(new[] { "FLAG_VEILSTONE_STORE_LIFT_AT_" + flags[i] }, flags.Select(f => "FLAG_VEILSTONE_STORE_LIFT_AT_" + f).Where(host.Story.Has));
            car.ApplyDoors(host.Story.Has);
            var open = car.GetWarpAt(3, 7)!;
            Assert.Equal((Store[i], doors.SourceX, doors.SourceY + 1, Direction.Down), (open.TargetMap, open.TargetX, open.TargetY, open.TargetFacing));
            Assert.True(floor.IsWalkable(open.TargetX, open.TargetY));
        }

        // From the ground floor up to the roof, down to the basement by the second question, and to where one is already
        HeadlessScriptHost Ride(int at, params int[] answers)
        {
            var host = new HeadlessScriptHost { Map = car };
            host.Story.SetVar("VAR_VEILSTONE_STORE_LIFT_FLOOR", at);
            foreach (int a in answers) host.Answers.Enqueue(a);
            return Run("Attendant", "VeilstoneStoreElevator", host);
        }
        var roof = Ride(2, 0);
        Assert.Contains("Going up", Said(roof));
        Assert.Contains(roof.Log, l => l.Contains("VeilstoneStore5F"));
        var basement = Ride(2, 4, 1);
        Assert.Contains("Going down", Said(basement));
        Assert.Contains(basement.Log, l => l.Contains("VeilstoneStoreB1F"));
        var same = Ride(4, 2);
        Assert.Contains("already", Said(same));
        Assert.DoesNotContain(same.Log, l => l.Contains("VeilstoneStore"));
        var never = Ride(4, 5);
        Assert.DoesNotContain(never.Log, l => l.Contains("VeilstoneStore"));
    }

    [Fact]
    public void TheStoresCountersSellWhatTheOriginalsDo()
    {
        // marts.json's specialties, from the decompilation's mart_items.h: who sells which
        foreach (var (floor, clerk, mart, some) in new[]
        {
            ("VeilstoneStore1F", "cashier_f", "veilstone_1f_right", "Full Heal"), ("VeilstoneStore1F", "cashier_m", "veilstone_1f_left", "Ultra Ball"),
            ("VeilstoneStore2F", "cashier_f_up", "veilstone_2f_up", "X Attack"), ("VeilstoneStore2F", "cashier_f_mid", "veilstone_2f_mid", "Protein"),
            ("VeilstoneStore3F", "cashier_f", "veilstone_3f_up", "TM17"), ("VeilstoneStore3F", "cashier_m", "veilstone_3f_down", "TM38"),
            ("VeilstoneStoreB1F", "cashier_f_berry", "veilstone_b1f", "Figy Berry"),
        })
        {
            var who = Room(floor).Everyone.Single(n => n.Key == clerk);
            Assert.True(who.IsPokeMartClerk, $"{floor}: {clerk} sells nothing");
            Assert.Equal(mart, who.Mart);
            Assert.Contains(some, MartDatabase.Specialties[mart]);
        }
    }

    [Fact]
    public void TheStoresFourthFloorSellsThingsForASecretBaseAtTheGoodsOwnPrices()
    {
        // MART_DECOR_ID_VEILSTONE_4F_UP and _DOWN: five furnishings and six dolls, priced by underground.json's "money"
        var goods = new[] { ("Yellow Cushion", 500), ("Cupboard", 1000), ("TV", 4500), ("Refrigerator", 1000), ("Pretty Sink", 3000) };
        var dolls = new[] { ("Munchlax Doll", 2000), ("Bonsly Doll", 2000), ("Mime Jr. Doll", 2000), ("Mantyke Doll", 3000), ("Buizel Doll", 3000), ("Chatot Doll", 3000) };
        foreach (var (name, price) in goods.Concat(dolls))
            Assert.Equal(price, UndergroundDatabase.Good(UndergroundDatabase.GoodId(name))!.Money);
        for (int i = 0; i < goods.Length; i++)
        {
            var host = new HeadlessScriptHost { Money = 5000 };
            host.Answers.Enqueue(i);
            Run("Goods", "VeilstoneStore4F", host);
            Assert.Equal(5000 - goods[i].Item2, host.Money);
            Assert.Contains(host.Underground.PcGoods, g => UndergroundDatabase.GoodName(g) == goods[i].Item1);
        }
        for (int i = 0; i < dolls.Length; i++)
        {
            var host = new HeadlessScriptHost { Money = 5000 };
            if (i < 3) host.Answers.Enqueue(i);
            else { host.Answers.Enqueue(3); host.Answers.Enqueue(i - 3); }
            Run("Dolls", "VeilstoneStore4F", host);
            Assert.Equal(5000 - dolls[i].Item2, host.Money);
        }
        // Too little money buys nothing
        var poor = new HeadlessScriptHost { Money = 400 };
        poor.Answers.Enqueue(0);
        Run("Goods", "VeilstoneStore4F", poor);
        Assert.Equal(400, poor.Money);
        Assert.Contains("money", Said(poor));
    }

    [Fact]
    public void TheRoofsVendingMachinesSellThreeDrinksAndNowAndThenASecondCan()
    {
        var roof = Room("VeilstoneStore5F");
        var machines = roof.Props.Where(p => p.Type == PropType.VendingMachine).ToList();
        Assert.Equal(new[] { (5, 3), (6, 3) }, machines.Select(p => (p.X, p.Y)).Order());
        Assert.All(machines, p => Assert.True(roof.IsSolid(p.X, p.Y) && !p.IsSolid));
        Assert.All(machines, p => Assert.Equal("VendingMachine", roof.SignScripts[(p.X, p.Y)]));
        foreach (var (answer, drink, price) in new[] { (0, "Fresh Water", 200), (1, "Soda Pop", 300), (2, "Lemonade", 350) })
        {
            // The die's 0 out of 64 drops a second can; any other face doesn't
            var host = new HeadlessScriptHost { Money = 1000 };
            host.Answers.Enqueue(answer);
            host.Rolls.Enqueue(5);
            Run("VendingMachine", "VeilstoneStore5F", host);
            Assert.Equal((1000 - price, 1), (host.Money, Have(host, drink)));
            var lucky = new HeadlessScriptHost { Money = 1000 };
            lucky.Answers.Enqueue(answer);
            lucky.Rolls.Enqueue(0);
            Run("VendingMachine", "VeilstoneStore5F", lucky);
            Assert.Equal((1000 - price, 2), (lucky.Money, Have(lucky, drink)));
            Assert.Contains("Bonus", Said(lucky));
        }
        var poor = new HeadlessScriptHost { Money = 150 };
        poor.Answers.Enqueue(0);
        Run("VendingMachine", "VeilstoneStore5F", poor);
        Assert.Equal((150, 0), (poor.Money, Have(poor, "Fresh Water")));
        // Its art: a lit window over three buttons, in whole texels of opaque colour but the glass's mark
        var c = new PixelCanvas(PropModels.VendingWidth, PropModels.VendingHeight);
        PropModels.PaintVendingFront(c);
        int lit = 0;
        for (int y = 0; y < c.Height; y++)
            for (int x = 0; x < c.Width; x++)
            {
                byte a = c.Get(x, y).A;
                Assert.True(a == 255 || a == ArtSheet.PublicLight, $"({x},{y}) is see-through");
                if (a == ArtSheet.PublicLight) lit++;
            }
        Assert.True(lit > 100, $"only {lit} texels of the window are lit");
        Assert.All(PropModels.VendingCans, can => Assert.Contains(Enumerable.Range(0, c.Width * c.Height), i => c.Get(i % c.Width, i / c.Width).Equals(can)));
    }

    [Fact]
    public void TheStoresPeopleGiveWhatTheOriginalsDo()
    {
        // The socialite's mask is of a starter the player didn't choose, once
        foreach (var (starter, mask) in new[] { ("Turtwig", "Piplup Mask"), ("Chimchar", "Turtwig Mask"), ("Piplup", "Chimchar Mask") })
        {
            var host = new HeadlessScriptHost();
            host.Story.ChooseStarter(starter);
            Run("Socialite", "VeilstoneStore1F", host);
            Assert.Equal(1, host.FashionCase.Count(SuperContestDatabase.Accessory(mask)));
            Assert.True(host.Story.Has("FLAG_RECEIVED_VEILSTONE_STORE_1F_ACCESSORY_STARTER_MASK"));
            Run("Socialite", "VeilstoneStore1F", host);
            Assert.Equal(1, host.FashionCase.Count(SuperContestDatabase.Accessory(mask)));
        }
        // The Counter app, to whoever has a Pokétch and says yes
        var watch = new HeadlessScriptHost();
        watch.Poketch.Enabled = true;
        Run("Poketch", "VeilstoneStore2F", watch);
        Assert.True(watch.Poketch.Has(PoketchApp.Counter));
        var none = Run("Poketch", "VeilstoneStore2F", new HeadlessScriptHost());
        Assert.False(none.Poketch.Has(PoketchApp.Counter));
        // The Sticky Barb once, and the Lava Cookies by the one and by the ten
        var roof = Run("Collector", "VeilstoneStore5F", new HeadlessScriptHost());
        Run("Collector", "VeilstoneStore5F", roof);
        Assert.Equal(1, Have(roof, "Sticky Barb"));
        var cookies = new HeadlessScriptHost { Money = 3000 };
        cookies.Answers.Enqueue(1);
        Run("LavaCookies", "VeilstoneStoreB1F", cookies);
        Assert.Equal((1000, 10), (cookies.Money, Have(cookies, "Lava Cookie")));
        cookies.Answers.Enqueue(0);
        Run("LavaCookies", "VeilstoneStoreB1F", cookies);
        Assert.Equal((800, 11), (cookies.Money, Have(cookies, "Lava Cookie")));
    }

    [Fact]
    public void TheBasementsPoffinsAreOfTwoFlavoursAtSixThousandFourHundred()
    {
        const string room = "VeilstoneStoreB1F";
        // No case, no Poffin; and none for less than the price
        var noCase = Run("Poffins", room, new HeadlessScriptHost { Money = 10000 });
        Assert.Equal(10000, noCase.Money);
        Assert.Contains("Poffin Case", Said(noCase));
        var poor = new HeadlessScriptHost { Money = 6399 };
        poor.Bag.AddItem(ItemDatabase.Get("Poffin Case")!, 1);
        Run("Poffins", room, poor);
        Assert.Equal((6399, 0), (poor.Money, poor.Poffins.Count));
        // Each of the ten kinds: 32 of two flavours, smoothness 20 (the original's GivePoffin numbers)
        var kinds = new (int[] Answers, int[] Flavours)[]
        {
            (new[] { 0 }, new[] { 32, 32, 0, 0, 0 }), (new[] { 1 }, new[] { 32, 0, 32, 0, 0 }), (new[] { 2 }, new[] { 32, 0, 0, 32, 0 }), (new[] { 3 }, new[] { 32, 0, 0, 0, 32 }),
            (new[] { 4, 0 }, new[] { 0, 32, 32, 0, 0 }), (new[] { 4, 1 }, new[] { 0, 32, 0, 32, 0 }), (new[] { 4, 2 }, new[] { 0, 32, 0, 0, 32 }),
            (new[] { 4, 3, 0 }, new[] { 0, 0, 32, 32, 0 }), (new[] { 4, 3, 1 }, new[] { 0, 0, 32, 0, 32 }), (new[] { 4, 3, 2 }, new[] { 0, 0, 0, 32, 32 }),
        };
        foreach (var (answers, flavours) in kinds)
        {
            var host = new HeadlessScriptHost { Money = 7000 };
            host.Bag.AddItem(ItemDatabase.Get("Poffin Case")!, 1);
            foreach (int a in answers) host.Answers.Enqueue(a);
            Run("Poffins", room, host);
            Assert.Equal(600, host.Money);
            var poffin = Assert.Single(host.Poffins.All);
            Assert.Equal(flavours, new[] { (int)poffin.Spicy, poffin.Dry, poffin.Sweet, poffin.Bitter, poffin.Sour });
            Assert.Equal(20, poffin.Smoothness);
        }
        // Backing out of any of the three questions buys nothing
        foreach (var answers in new[] { new[] { 5 }, new[] { 4, 4 }, new[] { 4, 3, 3 } })
        {
            var host = new HeadlessScriptHost { Money = 7000 };
            host.Bag.AddItem(ItemDatabase.Get("Poffin Case")!, 1);
            foreach (int a in answers) host.Answers.Enqueue(a);
            Run("Poffins", room, host);
            Assert.Equal((7000, 0), (host.Money, host.Poffins.Count));
        }
    }

    [Fact]
    public void TheProfessorStandsInTheBasementAfterTheHallOfFameUntilSpokenTo()
    {
        const string hidden = "FLAG_HIDE_VEILSTONE_STORE_B1F_PROF_ROWAN", spoken = "FLAG_TALKED_TO_VEILSTONE_STORE_B1F_PROF_ROWAN";
        var basement = Room("VeilstoneStoreB1F");
        var rowan = basement.Everyone.Single(n => n.Key == "prof_rowan");
        Assert.Equal((15, 4, hidden, "Rowan"), (rowan.GridX, rowan.GridY, rowan.HiddenBy, rowan.Script));
        // A new game hides him (common.NewGame), the victory shows him (PokemonLeagueHallOfFame.Victory), and his
        // own script hides him again for good
        var host = new HeadlessScriptHost { Map = basement };
        Run("Rowan", "VeilstoneStoreB1F", host, rowan);
        Assert.True(host.Story.Has(spoken) && host.Story.Has(hidden));
        Assert.Contains("Rage Candy Bars", Said(host));
        Assert.DoesNotContain(spoken, StoryTests.AskedAheadOfTheirScenes);
    }

    // ------------------------------------------------------------------ the Pokémon Centers' other floors

    [Fact]
    public void EveryPokemonCenterHasAFloorAboveAndAFloorBelowByItsEscalators()
    {
        var centers = Centers.ToList();
        Assert.True(centers.Count >= 18, $"only {centers.Count} Centers");
        foreach (string center in centers)
        {
            var ground = Room(center);
            var up = ground.Warps.Single(w => w.TargetMap == center + "2F");
            var down = ground.Warps.Single(w => w.TargetMap == center + "B1F");
            // On the ground floor: the way up at the west, its flight to the west of it, and the way down at the east
            Assert.True(up.SourceX < down.SourceX, center);
            Assert.Contains(ground.Props, p => p.Type == PropType.SideStairsUp && (p.X + p.Width, p.Y) == (up.SourceX, up.SourceY));
            Assert.Contains(ground.Props, p => p.Type == PropType.SideStairsDown && (p.X, p.Y) == (down.SourceX + 1, down.SourceY));
            Assert.True(ground.IsWalkable(up.SourceX + 1, up.SourceY) && ground.IsWalkable(down.SourceX - 1, down.SourceY), $"{center}: nobody can step onto its escalators");
            // The floors are the original's two plans (m_pc02 and m_pc07), each with its three attendants behind a counter
            foreach (var (way, suffix, back, facing) in new[] { (up, "2F", (up.SourceX + 1, up.SourceY), Direction.Right), (down, "B1F", (down.SourceX - 1, down.SourceY), Direction.Left) })
            {
                var floor = Room(center + suffix);
                Assert.Equal((17, 14, InteriorStyle.PokemonCenter), (floor.Width, floor.Height, floor.Interior));
                Assert.Equal(ground.BgmTrack, floor.BgmTrack);
                Assert.True(floor.IsWalkable(way.TargetX, way.TargetY));
                var only = Assert.Single(floor.Warps);
                Assert.Equal((center, back.Item1, back.Item2, facing), (only.TargetMap, only.TargetX, only.TargetY, only.TargetFacing));
                Assert.Equal(1, Math.Abs(only.SourceX - way.TargetX));
                Assert.Equal(3, floor.Everyone.Count(n => n.Name == "Attendant" && n.GridY == 4));
                Assert.All(floor.Everyone, n => Assert.Equal(TileBehavior.Counter, floor.BehaviourAt(n.GridX, n.GridY + 1)));
            }
            // The floor above: its way down at the west wall, its flight in the wall's own column
            var above = Room(center + "2F");
            Assert.Contains(above.Props, p => p.Type == PropType.SideStairsDownWest && (p.X, p.Y) == (1, 10));
            Assert.Contains(Room(center + "B1F").Props, p => p.Type == PropType.SideStairsUpEast && (p.X, p.Y) == (15, 10));
        }
        // Their attendants say that the wireless link is down (decision 06 · 7), each of her own desk
        var upstairs = Room("JubilifePokemonCenter2F").Everyone.Select(n => string.Join(" ", n.DialogLines)).ToList();
        Assert.Contains(upstairs, l => l.Contains("Union Room"));
        Assert.Contains(upstairs, l => l.Contains("Colosseum"));
        Assert.All(upstairs.Concat(Room("JubilifePokemonCenterB1F").Everyone.Select(n => string.Join(" ", n.DialogLines))), l => Assert.Contains("link", l));
        Assert.Contains(Room("JubilifePokemonCenterB1F").Everyone, n => string.Join(" ", n.DialogLines).Contains("Wi-Fi Club"));
    }

    // ------------------------------------------------------------------ the Global Terminal

    [Fact]
    public void TheGlobalTerminalsFloorsAreJoinedByWarpPanels()
    {
        // 1F to 2F and to 3F, 2F to 1F and to 3F, 3F to 1F and to 2F: each a panel whose warp names the far panel's tile
        foreach (string name in Terminal)
        {
            var floor = Room(name);
            Assert.Equal(InteriorStyle.Lab, floor.Interior);
            var panels = floor.Warps.Where(w => w.TargetMap != "Sinnoh").ToList();
            Assert.Equal(Terminal.Where(t => t != name).Order(), panels.Select(w => w.TargetMap).Order());
            foreach (var panel in panels)
            {
                Assert.Equal(TileBehavior.WarpPanel, floor.BehaviourAt(panel.SourceX, panel.SourceY));
                Assert.Contains(floor.Props, p => p.Type == PropType.WarpPanel && (p.X, p.Y) == (panel.SourceX, panel.SourceY));
                Assert.True(WarpPanels.Carries(floor, panel));
                var far = Room(panel.TargetMap);
                var back = Assert.Single(far.Warps, w => w.TargetMap == name);
                Assert.Equal((back.SourceX, back.SourceY), (panel.TargetX, panel.TargetY));
            }
        }
        // Its machines show no signal, and its receptionists say why
        var ground = Room("GlobalTerminal1F");
        Assert.True(ground.Signboards.Count(s => s.Value.Contains("NO SIGNAL")) >= 8);
        Assert.Contains("GTS", string.Join(" ", ground.Everyone.Single(n => n.Key == "receptionist_gts").DialogLines));
        Assert.All(ground.Everyone.Where(n => n.Key.StartsWith("receptionist", StringComparison.Ordinal)), n => Assert.Contains("link", string.Join(" ", n.DialogLines)));
        // The beauty guesses the first of the team
        var host = new HeadlessScriptHost();
        host.Party.Add(Mon("Bidoof"));
        Run("Favourite", "GlobalTerminal1F", host);
        Assert.Contains(host.Asked, a => a.Question.Contains("Bidoof"));
        Assert.Empty(Run("Favourite", "GlobalTerminal1F", new HeadlessScriptHost()).Asked);
    }

    // ------------------------------------------------------------------ Pal Park's viewing gallery

    [Fact]
    public void PalParksEscalatorLeadsUpToTheGalleryAndBackDown()
    {
        Map lobby = Room("PalParkLobby"), gallery = Room("PalParkGallery");
        // The lobby's escalator tile (the original's ESCALATOR at 10, 13, its slope east of it) is the way up
        var up = Assert.Single(lobby.Warps, w => w.TargetMap == "PalParkGallery");
        Assert.Equal((10, 13), (up.SourceX, up.SourceY));
        Assert.Contains(lobby.Props, p => p.Type == PropType.SideStairsUpEast && (p.X, p.Y) == (11, 13));
        Assert.DoesNotContain(lobby.Signboards, s => s.Value.Contains("rope"));
        // It puts the player down east of the flight's head, where the original's upper level begins, walking on
        Assert.Equal((14, 13, Direction.Right), (up.TargetX, up.TargetY, up.TargetFacing));
        Assert.False(gallery.IsSolid(14, 13));
        Assert.Null(gallery.GetWarpAt(14, 13));
        // And the gallery's one way is back down, to the tile west of the lobby's escalator
        var down = Assert.Single(gallery.Warps);
        Assert.Equal(("PalParkLobby", 9, 13, Direction.Left), (down.TargetMap, down.TargetX, down.TargetY, down.TargetFacing));
        Assert.Equal((13, 13), (down.SourceX, down.SourceY));
        Assert.Contains(gallery.Props, p => p.Type == PropType.SideStairsDownWest && (p.X, p.Y) == (11, 13));
        Assert.False(lobby.IsSolid(9, 13));
        Assert.Null(lobby.GetWarpAt(9, 13));
        // The upper level is the original's: three rows before the windows the length of the room, and the walk
        // two tiles wide down its east side to the escalator's head. Nothing else of it is walked
        for (int y = 0; y < gallery.Height; y++)
            for (int x = 0; x < gallery.Width; x++)
            {
                bool level = x is >= 1 and <= 15 && y is >= 4 and <= 6 || x is 14 or 15 && y is >= 7 and <= 14 || (x, y) == (13, 13);
                Assert.True(gallery.IsSolid(x, y) != level, $"the gallery's tile ({x},{y})");
            }
        Assert.Equal(3, gallery.Props.Count(p => p.Type == PropType.Window && p.Y == 3));
        // Its four spectators look out over the park, each with something to say; the lobby's own people stay below
        Assert.Equal(new[] { "lady", "lass_2", "pokefan_m", "youngster" }, gallery.Everyone.Select(n => n.Key).Order());
        Assert.All(gallery.Everyone, n => Assert.True(n.Facing == Direction.Up && n.DialogLines.Count > 0));
        Assert.Equal(lobby.BgmTrack, gallery.BgmTrack);
        Assert.Equal(RegionDatabase.Sinnoh, RegionDatabase.RegionOfMap("PalParkGallery")!.Id);
    }
}
