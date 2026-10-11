using PokemonPlatinumEngine.Models.Underground;

partial class Harness
{
    // ---------------------------------------------------------------- The Underground (plan 06 · R16a)

    // The way down with the Explorer Kit and Roark's welcome, the tunnels and the radar's pulse, a wall dug through its
    // stages with the pick and the hammer, a second wall brought down, every kind of trap at work, the bag below
    // ground, a Sphere buried, grown and dug up, a trap set and stepped on, the three kinds of hiker and their trade,
    // the Digger Drill and the secret base, its PC and its decorating, another digger's base and its flag brought
    // home, the way back up and the Underground Man (ug*; not part of "all"). Everything goes through the game's own
    // scripts, steps, screens and buttons; it prints what each left behind.
    public void UndergroundMode()
    {
        game.State = GameState.Overworld;
        game.LocationSign.Hide();
        var bag = game.Bag;
        bag.AddItem(ItemDatabase.Get("Explorer Kit")!, 1);
        var below = MapDatabase.Get(GameEngine.UndergroundMap);
        var story = game.Story;
        UndergroundState State() => game.Underground;
        int Trap(string id) => UndergroundDatabase.TrapId(id);
        int Good(string id) => UndergroundDatabase.GoodId(id);
        int Treasure(string id) => UndergroundDatabase.TreasureId(id);
        bool Rock(int x, int z) => !below.InBounds(x, z) || below.IsSolid(x, z);

        DialogueManager Box() => game.Dialogue;
        void Until(Func<bool> holds, string what, int most = 900)
        {
            for (int i = 0; i < most && !holds(); i++) Frames(1);
            if (!holds()) Console.WriteLine($"  !! never happened: {what}");
        }
        void Whole() { Until(() => Box().IsActive, "text on the screen"); Box().FinishLine(); Frames(2); }
        void Next() { Box().Advance(); Frames(2); }
        // Reads every line on to the end of the script, shooting the line that holds a word and answering yes
        void ReadOn(string? word = null, string? shot = null)
        {
            for (int i = 0; i < 60 && (Box().IsActive || engine.Choice.IsOpen || game.State == GameState.Dialogue); i++)
            {
                if (engine.Choice.IsOpen) { engine.Choice.Confirm(); Frames(4); continue; }
                Whole();
                if (word != null && shot != null && Box().VisibleText.Contains(word)) { Shot(shot); shot = null; }
                Next();
            }
            Until(() => game.State is GameState.Overworld or GameState.Underground or GameState.Mining && !Box().IsActive, "the field again");
        }
        // The open tile beside a tile, and the way to look at the tile from there: from the south first, so it shows
        (int X, int Z, Direction Facing)? Before(int x, int z)
        {
            foreach (var (dx, dz, facing) in new[] { (0, 1, Direction.Up), (1, 0, Direction.Left), (-1, 0, Direction.Right), (0, -1, Direction.Down) })
                if (!Rock(x + dx, z + dz) && below.GetNpcAt(x + dx, z + dz) == null) return (x + dx, z + dz, facing);
            return null;
        }
        int Far(int x, int z) => Math.Abs(x - game.Player.GridX) + Math.Abs(z - game.Player.GridY);
        void Tell(string what) => Console.WriteLine($"  underground: {what}");

        // ---- The way down: the Explorer Kit used in the street of Eterna City
        At("Sinnoh", 310, 533, Direction.Down);
        game.LocationSign.Hide();
        Frames(6);
        Shot("ug01_above_ground");
        var above = (game.Player.GridX, game.Player.GridY);
        game.UseFieldItem(ItemDatabase.Get("Explorer Kit")!);
        Frames(14);
        Shot("ug02_going_down");
        Until(() => game.Map == below && game.State is GameState.Overworld or GameState.Dialogue, "the Underground", 300);
        Tell($"went down at {above} to {game.Player.GridX}, {game.Player.GridY} (below: {UndergroundField.Below(above.Item1, above.Item2)}); entered {story.Has(GameEngine.EnteredUndergroundFlag)}");

        // ---- Roark, beside whoever comes down for the first time
        Until(() => Box().IsActive, "Roark's welcome", 400);
        ReadOn("beneath the whole of Sinnoh", "ug03_roark");
        Frames(30);
        game.LocationSign.Hide();
        Frames(4);
        Shot("ug04_tunnels");
        Tell($"Roark's welcome seen {story.Var("VAR_HAS_SEEN_UNDERGROUND_ROARK_INTRO")}, hidden {story.Has("FLAG_HIDE_ROARK_UNDERGROUND")}; {State().MiningSpots.Count} walls and {State().SpawnedTraps.Count} traps spread today");

        // What the bag below ground starts with: the Underground Man's first gifts, and a little more to show
        foreach (string trap in new[] { "TRAP_DIGGER_DRILL", "TRAP_HOLE", "TRAP_MOVE_UP", "TRAP_BUBBLE", "TRAP_LEAF" }) State().TryAddTrap(Trap(trap));
        State().TryAddSphere(SphereKind.Prism, 1);
        State().TryAddSphere(SphereKind.Red, 12);
        State().TryAddSphere(SphereKind.Blue, 30);
        State().TryAddSphere(SphereKind.Green, 7);
        State().TryAddSphere(SphereKind.Pale, 44);
        foreach (string good in new[] { "UG_GOOD_YELLOW_CUSHION", "UG_GOOD_POTTED_PLANT" }) State().TryAddGood(Good(good));
        State().TryAddTreasure(Treasure("TREASURE_HEART_SCALE"));

        // ---- The radar's pulse, beside a wall that holds something
        var wall = State().MiningSpots.Where(s => Before(s.X, s.Z) is { Facing: Direction.Up }).OrderBy(s => Far(s.X, s.Z)).First();
        var stand = Before(wall.X, wall.Z)!.Value;
        At(GameEngine.UndergroundMap, stand.X, stand.Z, stand.Facing);
        Frames(10);
        game.PulseRadar();
        Frames(10);
        Shot("ug05_radar");
        var pulse = game.Tunnels.Pulse(stand.X, stand.Z);
        Tell($"the radar at {stand.X}, {stand.Z}: {pulse.Walls.Count} walls, {pulse.Traps.Count} traps, {pulse.Spheres.Count} Spheres");
        Frames(90);

        // ---- The wall: its question, and its game from the first blow to what was dug out
        var mining = game.Mining;
        void Strike(int x, int y)
        {
            mining.Move(x - mining.CursorX, y - mining.CursorY);
            mining.PressConfirm();
            Frames(3);
        }
        void DigOut(WallObject thing)
        {
            for (int y = thing.Y; y < thing.Y + thing.Object.Height; y++)
                for (int x = thing.X; x < thing.X + thing.Object.Width; x++)
                    while (!mining.Game.Over && thing.Covers(x, y) && mining.Game.DirtAt(x, y) > 0) Strike(x, y);
        }
        void ToTheEnd(string? shot)
        {
            Until(() => mining.Phase is MiningPhase.Ended or MiningPhase.Finds or MiningPhase.Done, "the wall's end", 200);
            Frames(40);
            if (shot != null) Shot(shot);
        }
        void ReadTheFinds(string? shot)
        {
            for (int i = 0; i < 60 && mining.IsActive; i++)
            {
                Frames(40);
                if (shot != null && mining.Phase == MiningPhase.Finds) { Shot(shot); shot = null; }
                mining.PressConfirm();
                Frames(2);
            }
            Until(() => game.State == GameState.Overworld, "the tunnels again", 200);
        }

        game.Interact(); Frames(3);
        Whole(); Next();
        Until(() => engine.Choice.IsOpen, "the wall's question"); Frames(20);
        Shot("ug06_wall_question");
        engine.Choice.Confirm(); Frames(4);
        Until(() => game.State == GameState.Mining, "the wall's game", 300);
        Frames(30);
        Shot("ug07_wall_intro");
        mining.PressConfirm(); Frames(30);
        if (mining.Phase == MiningPhase.Intro) { Shot("ug08_wall_lesson"); mining.PressConfirm(); Frames(20); }
        Shot("ug09_wall_untouched");
        Tell($"the first wall: {mining.Game.ItemCount} things, {mining.Game.Objects.Count - mining.Game.ItemCount} dark rocks, the wall at {mining.Game.Integrity}; dug for fossils {story.Has(GameEngine.DugForFossilsFlag)}");
        // A few blows of the pick about the first thing, then the hammer
        var first = mining.Game.Objects[0];
        Strike(first.X, first.Y);
        Strike(first.X, first.Y);
        Frames(1);
        Shot("ug10_wall_pick");
        mining.PressTool(); Frames(6);
        Strike(Math.Min(MiningGame.Width - 2, first.X + first.Object.Width), Math.Min(MiningGame.Height - 2, first.Y + 1));
        Frames(1);
        Shot("ug11_wall_hammer");
        mining.PressTool(); Frames(6);
        DigOut(first);
        Frames(8);
        Shot("ug12_wall_first_thing_bare");
        Tell($"after the first thing: the wall at {mining.Game.Integrity} of {MiningGame.InitialIntegrity}, bare {mining.Game.IsBare(0)}");
        foreach (var thing in mining.Game.Objects.Skip(1).Take(mining.Game.ItemCount - 1))
        {
            DigOut(thing);
            Frames(6);
        }
        Shot("ug13_wall_cracking");
        ToTheEnd("ug14_wall_ended");
        Tell($"the first wall ended: all dug {mining.Game.AllDug}, collapsed {mining.Game.Collapsed}, the wall at {mining.Game.Integrity}; found {string.Join(", ", mining.Game.Finds().Select(f => UndergroundDatabase.TreasureName(f.Id) + (f.SphereSize > 0 ? $" ({f.SphereSize})" : "")))}");
        ReadTheFinds("ug15_wall_finds");
        Tell($"in the bag: {State().Spheres.Count} Spheres, {State().Treasures.Count} treasures; records {State().Records.SpheresDug} Spheres, {State().Records.FossilsDug} fossils, {State().Records.TreasuresDug} treasures");

        // A second wall, with dark rock in it, hammered until it comes down
        wall = State().MiningSpots.Where(s => Before(s.X, s.Z) is { Facing: Direction.Up }).OrderBy(s => Far(s.X, s.Z)).First();
        stand = Before(wall.X, wall.Z)!.Value;
        At(GameEngine.UndergroundMap, stand.X, stand.Z, stand.Facing);
        Frames(6);
        game.Interact(); Frames(3);
        Whole(); Next();
        Until(() => engine.Choice.IsOpen, "the second wall's question"); Frames(6);
        engine.Choice.Confirm(); Frames(4);
        Until(() => game.State == GameState.Mining, "the second wall's game", 300);
        Frames(20);
        mining.PressConfirm(); Frames(20);
        if (mining.Phase == MiningPhase.Intro) { mining.PressConfirm(); Frames(20); }
        Tell($"the second wall: {mining.Game.ItemCount} things, {mining.Game.Objects.Count - mining.Game.ItemCount} dark rocks");
        mining.PressTool(); Frames(4);
        // Over the whole wall, a blow to every third square, so the rock shows
        for (int y = 1; y < MiningGame.Height && !mining.Game.Over && mining.Game.Integrity > 60; y += 3)
            for (int x = 1; x < MiningGame.Width && !mining.Game.Over && mining.Game.Integrity > 60; x += 3) Strike(x, y);
        Frames(6);
        Shot("ug16_second_wall_rock");
        while (!mining.Game.Over) Strike(6, 5);
        Frames(4);
        Shot("ug17_wall_falling_in");
        ToTheEnd("ug18_wall_fell_in");
        Tell($"the second wall: collapsed {mining.Game.Collapsed}, found {mining.Game.Finds().Count}");
        ReadTheFinds(null);

        // ---- Traps at work, each sprung under the player and got out of as its family asks
        engine.TrapKeys = (null, false, false);
        void Spring(string id) { game.SpringTrap(Trap(id)); Frames(24); }
        void Keys(Direction? way = null, bool tap = false, bool held = false, int frames = 2)
        {
            engine.TrapKeys = (way, tap, held);
            Frames(frames);
            engine.TrapKeys = (null, false, false);
        }
        void Free(string what, Action act, int most = 1500)
        {
            for (int i = 0; i < most && game.Trap != null; i++) act();
            if (game.Trap != null) Tell($"!! still in the trap: {what}");
            Frames(20);
        }
        // A stretch of tunnel to stand in, with room to be carried along it
        var hall = Enumerable.Range(34, 440).SelectMany(x => Enumerable.Range(66, 400).Select(z => (X: x, Z: z)))
            .Where(t => Enumerable.Range(0, 12).All(d => !Rock(t.X + d, t.Z) && below.GetNpcAt(t.X + d, t.Z) == null) && Rock(t.X, t.Z - 1))
            .OrderBy(t => Far(t.X, t.Z)).First();
        At(GameEngine.UndergroundMap, hall.X + 1, hall.Z, Direction.Down);
        Frames(10);

        Spring("TRAP_REVERSE");
        Shot("ug20_trap_reverse");
        Keys(Direction.Left, frames: 30);
        Tell($"steering turned round: pressed left, went to {game.Player.GridX}, {game.Player.GridY} from {hall.X + 1}, {hall.Z}");

        At(GameEngine.UndergroundMap, hall.X + 1, hall.Z, Direction.Down);
        Spring("TRAP_PIT");
        Shot("ug21_trap_pit");
        int turns = 0;
        Free("the pit", () =>
        {
            Keys(turns % 2 == 0 ? Direction.Left : Direction.Right);
            if (++turns == 9) Shot("ug22_trap_pit_climbing");
        });
        Shot("ug23_trap_free");
        Tell($"out of the pit after {turns} turns");

        Spring("TRAP_BIG_SMOKE");
        Shot("ug24_trap_smoke");
        // The hand to the top left corner, then band by band: across, down its own height, and back
        for (int i = 0; i < 6; i++) Keys(Direction.Up);
        for (int i = 0; i < 8; i++) Keys(Direction.Left);
        int swept = 0;
        Free("the smoke", () =>
        {
            int row = swept / 18;
            Keys(swept % 18 < 16 ? (row % 2 == 0 ? Direction.Right : Direction.Left) : Direction.Down);
            if (++swept == 46) Shot("ug25_trap_smoke_wiped");
        }, 400);
        Tell($"the smoke cleared after {swept} strokes");

        Spring("TRAP_ROCK");
        Shot("ug26_trap_rock");
        int blows = 0;
        Free("the boulder", () =>
        {
            Keys(tap: true);
            Keys(frames: 4);
            if (++blows == 7) Shot("ug27_trap_rock_cracking");
        });
        Tell($"the boulder broke after {blows} blows");

        Spring("TRAP_BUBBLE");
        Shot("ug28_trap_bubbles");
        int burst = 0;
        Free("the bubbles", () =>
        {
            Keys(tap: true);
            Keys(frames: 2);
            if (++burst == 10) Shot("ug29_trap_bubbles_bursting");
        });
        Tell($"{burst} bubbles burst");

        Spring("TRAP_FLOWER");
        Shot("ug2a_trap_flowers");
        int breath = 0;
        Free("the flowers", () =>
        {
            Keys(held: true);
            if (++breath == 8) Shot("ug2b_trap_flowers_blown");
        });

        Spring("TRAP_FIRE");
        Shot("ug2c_trap_fire");
        breath = 0;
        Free("the fire", () =>
        {
            Keys(held: true);
            if (++breath == 28) Shot("ug2d_trap_fire_low");
        });
        Tell($"the fire went out after {breath * 2} frames of one breath");

        Spring("TRAP_HURL_RIGHT");
        Frames(10);
        Shot("ug2e_trap_hurl");
        Free("the slide", () => Frames(1));
        Tell($"hurled to {game.Player.GridX}, {game.Player.GridY}");

        Spring("TRAP_ALERT_2");
        Shot("ug2f_trap_alert");
        Free("the alert", () => Frames(1));
        engine.TrapKeys = null;
        Frames(200);

        // ---- The bag below ground, tab by tab
        var lists = game.UndergroundLists;
        At(GameEngine.UndergroundMap, hall.X + 1, hall.Z, Direction.Right);
        game.UseFieldItem(ItemDatabase.Get("Explorer Kit")!);
        Until(() => game.State == GameState.Underground, "the bag below ground", 60);
        Frames(30);
        Shot("ug30_bag_traps");
        foreach (string tab in new[] { "ug31_bag_spheres", "ug32_bag_goods", "ug33_bag_treasures", "ug34_bag_surface" })
        {
            lists.Move(1, 0); Frames(20);
            Shot(tab);
        }
        // A treasure put in the bag above ground
        lists.Move(-1, 0); Frames(6);
        lists.Confirm(); Frames(12);
        Shot("ug35_bag_treasure_actions");
        string put = lists.Selected?.Name ?? "";
        lists.Confirm(); Frames(12);
        Shot("ug36_bag_treasure_put_away");
        lists.Confirm(); Frames(4);
        Tell($"put in the bag: {put} ({(ItemDatabase.Get(put) is { } item ? bag.GetQuantity(item) : 0)} above ground)");
        lists.Cancel(); Frames(4);
        Until(() => game.State == GameState.Overworld, "the tunnels again", 60);

        // ---- A Sphere buried, grown for five days, found by the radar and dug up
        void OpenBag(int tab, int row)
        {
            game.OpenUndergroundBag(tab);
            Frames(20);
            for (int i = 0; i < row; i++) lists.Move(0, 1);
            Frames(6);
        }
        int red = State().Spheres.FindIndex(s => s.Kind == SphereKind.Red);
        OpenBag(1, red);
        lists.Confirm(); Frames(12);
        Shot("ug37_sphere_actions");
        lists.Confirm(); Frames(12);
        Until(() => game.State == GameState.Overworld, "the Sphere buried", 60);
        Frames(6);
        Shot("ug38_sphere_buried");
        var buried = State().Buried.LastOrDefault();
        Tell($"buried: {buried?.Kind} of {buried?.Size} at {buried?.X}, {buried?.Z}; the mission's flag {story.Has(GameEngine.SphereBuriedFlag)}");
        Frames(120);
        State().DaysPass(5);
        game.Tunnels.SpreadIfDue();
        Tell($"five days on: grown by {buried?.Growth}");
        game.PulseRadar(); Frames(10);
        Shot("ug39_sphere_on_the_radar");
        Frames(60);
        game.Interact(); Frames(8);
        Shot("ug3a_sphere_dug_up");
        Tell($"dug up: {State().Spheres[^1].Kind} of {State().Spheres[^1].Size}");
        Frames(120);

        // ---- A trap set in the floor ahead, and stepped on
        OpenBag(0, State().Traps.IndexOf(Trap("TRAP_HOLE")));
        lists.Confirm(); Frames(12);
        Shot("ug3b_trap_actions");
        lists.Confirm(); Frames(12);
        Until(() => game.State == GameState.Overworld, "the trap set", 60);
        Frames(6);
        Shot("ug3c_trap_set");
        Tell($"set: {State().PlacedTraps.Count} trap of the player's own in the floor");
        Frames(120);
        engine.TrapKeys = (null, false, false);
        engine.Steering = (Direction.Right, false);
        Until(() => game.Trap != null, "the trap going off", 120);
        engine.Steering = null;
        Frames(20);
        Shot("ug3d_trap_stepped_on");
        turns = 0;
        Free("the hole", () => Keys(turns++ % 2 == 0 ? Direction.Up : Direction.Down));
        engine.TrapKeys = null;
        Frames(200);
        Tell($"stepped on: {State().Records.TrapsTriggered} trap in the records, {State().PlacedTraps.Count} left in the floor");

        // ---- The hikers: goods for Spheres, traps for Spheres, Spheres for treasures
        void TalkToHiker(int vendor, string shot)
        {
            var who = UndergroundDatabase.Vendors[vendor];
            var spot = Before(who.X, who.Z)!.Value;
            At(GameEngine.UndergroundMap, spot.X, spot.Z, spot.Facing);
            Frames(10);
            Shot(shot);
            game.Interact(); Frames(3);
            Whole(); Next();
            Until(() => game.State == GameState.Underground, "the hiker's trade", 200);
            Frames(30);
        }
        void Leave()
        {
            lists.Cancel(); Frames(4);
            ReadOn();
            Until(() => game.State == GameState.Overworld && !Box().IsActive, "the tunnels again", 200);
        }
        // The nearest hiker of a kind who is in the tunnels (all twenty stand there since plan 01 · M11, part a)
        int Hiker(string kind) => Enumerable.Range(0, UndergroundDatabase.Vendors.Count)
            .Where(i => UndergroundDatabase.Vendors[i].Kind == kind && below.GetNpcAt(UndergroundDatabase.Vendors[i].X, UndergroundDatabase.Vendors[i].Z) != null)
            .OrderBy(i => Far(UndergroundDatabase.Vendors[i].X, UndergroundDatabase.Vendors[i].Z)).First();
        int goodsHiker = Hiker("Goods"), trapHiker = Hiker("Traps"), treasureHiker = Hiker("Treasures");
        Tell($"hikers: goods {goodsHiker}, traps {trapHiker}, treasures {treasureHiker}; {below.NPCs.Count(n => n.Key != null && n.Key.Contains("_vendor_"))} in the tunnels");
        // A Sphere he will take for his first ware
        var wares = UndergroundVendors.Stock(State().Seed, goodsHiker, false);
        State().TryAddSphere(wares[0].Kind, wares[0].Size + 2);
        TalkToHiker(goodsHiker, "ug40_hiker_goods");
        Shot("ug41_hiker_wares");
        lists.Confirm(); Frames(12);
        Shot("ug42_hiker_which_sphere");
        while (lists.Paying && lists.PayCursor < State().Spheres.Count - 1) lists.Move(0, 1);
        Frames(6);
        lists.Confirm(); Frames(12);
        Shot("ug43_hiker_traded");
        lists.Confirm(); Frames(4);
        lists.Move(1, 0); Frames(20);
        Shot("ug44_hiker_your_goods");
        lists.Confirm(); Frames(12);
        Shot("ug45_hiker_sell_question");
        lists.Confirm(); Frames(12);
        lists.Confirm(); Frames(4);
        Leave();
        Tell($"the goods hiker: goods {string.Join(", ", State().Goods.Select(UndergroundDatabase.GoodName))}; {State().Spheres.Count} Spheres");

        var traps = UndergroundVendors.Stock(State().Seed, trapHiker, false);
        State().TryAddSphere(traps[0].Kind == SphereKind.None ? SphereKind.Red : traps[0].Kind, traps[0].Size + 1);
        TalkToHiker(trapHiker, "ug46_hiker_traps");
        Shot("ug47_hiker_trap_wares");
        lists.Confirm(); Frames(12);
        while (lists.Paying && lists.PayCursor < State().Spheres.Count - 1) lists.Move(0, 1);
        lists.Confirm(); Frames(12);
        lists.Confirm(); Frames(4);
        Leave();
        Tell($"the trap hiker: traps {string.Join(", ", State().Traps.Select(UndergroundDatabase.TrapName))}");

        State().TryAddTreasure(Treasure("TREASURE_SKULL_FOSSIL"));
        TalkToHiker(treasureHiker, "ug48_hiker_treasures");
        Shot("ug49_hiker_your_treasures");
        lists.Confirm(); Frames(12);
        Shot("ug4a_hiker_treasure_question");
        lists.Confirm(); Frames(12);
        Shot("ug4b_hiker_treasure_traded");
        lists.Confirm(); Frames(4);
        Leave();
        Tell($"the treasure hiker: {State().Treasures.Count} treasures left, {State().Spheres.Count} Spheres");

        // ---- The Digger Drill: a cavern opened in solid rock, and the base behind it
        var site = Enumerable.Range(34, 440).SelectMany(x => Enumerable.Range(66, 400).Select(z => (X: x, Z: z)))
            .Where(t => !Rock(t.X, t.Z + 1) && below.GetNpcAt(t.X, t.Z + 1) == null && SecretBaseRules.CanDrill(Rock, t.X, t.Z, Direction.Up)
                && game.Tunnels.MiningSpotAt(t.X, t.Z) == null)
            .OrderBy(t => Far(t.X, t.Z)).First();
        At(GameEngine.UndergroundMap, site.X, site.Z + 1, Direction.Up);
        Frames(10);
        Shot("ug50_before_the_drill");
        OpenBag(0, State().Traps.IndexOf(Trap("TRAP_DIGGER_DRILL")));
        lists.Confirm(); Frames(12);
        Shot("ug51_drill_actions");
        lists.Confirm(); Frames(12);
        Until(() => game.State == GameState.Overworld, "the drill used", 60);
        Frames(10);
        Shot("ug52_cavern_opened");
        Tell($"the base: way in at {State().Base?.EntranceX}, {State().Base?.EntranceZ}, {State().Base?.Boulders.Count} boulders; created {story.Has(GameEngine.CreatedBaseFlag)}; drills left {State().Traps.Count(t => t == Trap("TRAP_DIGGER_DRILL"))}");
        Frames(120);
        void Into(Direction way, Func<bool> there, string what)
        {
            engine.Steering = (way, false);
            Until(there, what, 300);
            engine.Steering = null;
            Until(() => game.State == GameState.Overworld, "the fade's end", 300);
            Frames(50);
        }
        bool InARoom() => game.Player.GridX < SecretBaseRules.Size && game.Player.GridY < SecretBaseRules.Size;
        Into(Direction.Up, InARoom, "the way into the base");
        Shot("ug53_base_bare");
        At(GameEngine.UndergroundMap, 15, 17, Direction.Up);
        Frames(10);
        Shot("ug54_base_from_the_middle");

        // The PC: what the Underground Man sends, and more to show the art
        foreach (string good in new[]
                 {
                     "UG_GOOD_PLAIN_TABLE", "UG_GOOD_WOODEN_CHAIR", "UG_GOOD_SMALL_BOOKSHELF", "UG_GOOD_BUNEARY_DOLL", "UG_GOOD_TURTWIG_DOLL", "UG_GOOD_PRETTY_GEM",
                     "UG_GOOD_TV", "UG_GOOD_RED_TENT", "UG_GOOD_GLOBE", "UG_GOOD_BLUE_CRYSTAL", "UG_GOOD_GOLD_TROPHY", "UG_GOOD_BIG_SMOKE_TOOL", "UG_GOOD_WIDE_SOFA",
                     "UG_GOOD_BIG_OIL_DRUM", "UG_GOOD_RED_BIKE", "UG_GOOD_GYM_STATUE"
                 }) State().TryAddPcGood(Good(good));
        At(GameEngine.UndergroundMap, 15, 13, Direction.Up);
        Frames(6);
        game.Interact(); Frames(4);
        Until(() => game.State == GameState.Underground, "the base's PC", 200);
        Frames(30);
        Shot("ug55_pc_decorate");
        foreach (string tab in new[] { "ug56_pc_store", "ug57_pc_take", "ug58_pc_flags" })
        {
            lists.Move(1, 0); Frames(20);
            Shot(tab);
        }
        lists.Move(1, 0); Frames(6);
        lists.Confirm(); Frames(4);
        Until(() => game.State == GameState.Decoration, "the decorating", 60);
        Frames(30);

        // ---- Decorating: a good chosen, carried over the floor and set down; a boulder that stays
        var deco = game.Decoration;
        var session = deco.Session;
        Shot("ug60_decorate_menu");
        void Carry(int x, int y)
        {
            for (int i = 0; i < 40 && session.X != x; i++) deco.Move(Math.Sign(x - session.X), 0);
            for (int i = 0; i < 40 && session.Y != y; i++) deco.Move(0, Math.Sign(y - session.Y));
        }
        bool PutOut(int slot, string? shotChoosing = null, string? shotBlocked = null, string? shotFits = null)
        {
            while (session.MenuCursor != 0) deco.Move(0, -1);
            deco.PressConfirm(); Frames(8);
            if (session.Step != DecoratingStep.Choosing) return false;
            for (int i = 0; i < 40 && session.StoreCursor != slot; i++) deco.Move(0, Math.Sign(slot - session.StoreCursor));
            Frames(8);
            if (shotChoosing != null) Shot(shotChoosing);
            deco.PressConfirm(); Frames(8);
            if (session.Held is not { } held) return false;
            if (shotBlocked != null) Shot(shotBlocked);
            // The free place nearest the middle of the floor
            var room = session.Room;
            var free = Enumerable.Range(room.Left, room.Width).SelectMany(x => Enumerable.Range(room.Top + 1, room.Height - 1).Select(y => (X: x, Y: y)))
                .Where(t => room.CanStand(t.X, t.Y, held.Width, held.Depth, held) && !(t.X <= 15 && t.X + held.Width > 14 && t.Y > 12))
                .OrderBy(t => Math.Abs(t.X - 15) + Math.Abs(t.Y - 18)).Cast<(int X, int Y)?>().FirstOrDefault();
            if (free == null) { deco.PressCancel(); Frames(4); deco.PressCancel(); Frames(4); return false; }
            Carry(free.Value.X, free.Value.Y);
            Frames(8);
            if (shotFits != null) Shot(shotFits);
            deco.PressConfirm(); Frames(8);
            return true;
        }
        PutOut(0, "ug61_decorate_choose", "ug62_decorate_cannot_stand_here", "ug63_decorate_fits");
        Shot("ug64_decorate_set_down");
        int put10 = 1;
        for (int slot = 1; slot < State().PcGoods.Count && put10 < session.Room.Limit; slot++)
            if (PutOut(slot)) put10++;
        Frames(10);
        Shot("ug65_decorate_ten_out");
        // The eleventh is turned away, and a boulder stays until a flag is brought home
        while (session.MenuCursor != 0) deco.Move(0, -1);
        deco.PressConfirm(); Frames(10);
        Shot("ug66_decorate_no_more");
        deco.Move(0, 1); deco.PressConfirm(); Frames(8);
        var boulder = session.Room.Pieces.First(p => p.Kind == SecretBaseRules.BoulderGood);
        Carry(boulder.X, boulder.Y);
        Frames(8);
        deco.PressConfirm(); Frames(10);
        Shot("ug67_decorate_boulder_stays");
        deco.PressCancel(); Frames(6);
        // MOVE: the first good picked up again
        deco.Move(0, 1); deco.PressConfirm(); Frames(6);
        var moved = session.Room.Pieces.First(p => !p.IsFixture);
        Carry(moved.X, moved.Y);
        Frames(6);
        deco.PressConfirm(); Frames(8);
        Shot("ug68_decorate_moving");
        deco.PressCancel(); Frames(6);
        deco.PressCancel(); Frames(6);
        deco.PressCancel(); Frames(6);
        Until(() => game.State == GameState.Underground, "the PC again", 120);
        lists.Cancel(); Frames(6);
        Until(() => game.State == GameState.Overworld, "the base again", 120);
        Frames(40);
        Tell($"decorated: {State().Base?.Goods.Count} goods out of {State().PcGoods.Count} in the PC, {State().Base?.Boulders.Count} boulders; the mission's flag {story.Has(GameEngine.DecoratedBaseFlag)}");
        Shot("ug69_base_decorated_from_the_pc");
        At(GameEngine.UndergroundMap, 15, 22, Direction.Up);
        Frames(20);
        Shot("ug6a_base_decorated");
        At(GameEngine.UndergroundMap, 12, 18, Direction.Right);
        Frames(20);
        Shot("ug6b_base_decorated_west");
        At(GameEngine.UndergroundMap, 19, 16, Direction.Left);
        Frames(20);
        Shot("ug6c_base_decorated_east");

        // ---- Out by the mat, to another digger's base; its flag taken and brought home
        At(GameEngine.UndergroundMap, 15, 23, Direction.Down);
        Into(Direction.Down, () => !InARoom(), "the way out of the base");
        Shot("ug70_outside_the_base");
        Tell($"out of the base at {game.Player.GridX}, {game.Player.GridY}");
        var digger = game.Diggers.OrderBy(d => Far(d.Base.EntranceX, d.Base.EntranceZ)).First();
        Tell($"today's diggers: {string.Join(", ", game.Diggers.Select(d => $"{d.Name} at {d.Base.EntranceX}, {d.Base.EntranceZ}"))}");
        At(GameEngine.UndergroundMap, digger.Base.EntranceX, digger.Base.EntranceZ + 1, Direction.Up);
        Frames(20);
        Shot("ug71_diggers_way_in");
        Into(Direction.Up, InARoom, "the way into the digger's base");
        Shot("ug72_diggers_base");
        At(GameEngine.UndergroundMap, 15, 13, Direction.Up);
        Frames(6);
        game.Interact(); Frames(4);
        Until(() => engine.Choice.IsOpen, "the flag's question", 200); Frames(20);
        Shot("ug73_take_the_flag");
        engine.Choice.Confirm(); Frames(4);
        ReadOn("flag", "ug74_flag_taken");
        Tell($"carrying the flag of digger {State().HeldFlag}");
        At(GameEngine.UndergroundMap, 15, 23, Direction.Down);
        Into(Direction.Down, () => !InARoom(), "the way out of the digger's base");
        Shot("ug75_carrying_the_flag");
        var home = State().Base!;
        At(GameEngine.UndergroundMap, home.EntranceX, home.EntranceZ + 1, Direction.Up);
        Into(Direction.Up, InARoom, "the way home");
        At(GameEngine.UndergroundMap, 15, 13, Direction.Up);
        Frames(6);
        game.Interact(); Frames(4);
        Until(() => Box().IsActive, "the flag put up", 200);
        Whole();
        Shot("ug76_flag_brought_home");
        ReadOn();
        Until(() => game.State == GameState.Underground, "the PC", 200);
        lists.Move(-1, 0); Frames(20);
        Shot("ug77_pc_flags_after");
        lists.Cancel(); Frames(6);
        Until(() => game.State == GameState.Overworld, "the base again", 120);
        Frames(40);
        Shot("ug78_base_with_a_bronze_flag");
        Tell($"flags brought home {State().Records.FlagsCaptured}: the base's flag {SecretBaseRules.FlagFor(State().Records.FlagsCaptured)}, {SecretBaseRules.BouldersThatStay(State().Records.FlagsCaptured)} boulders stay, {SecretBaseRules.GoodsLimit(State().Records.FlagsCaptured)} goods may stand; delivered {story.Has(GameEngine.DeliveredFlagFlag)}");

        // ---- Back up, by the bag's last tab
        game.OpenUndergroundBag(4);
        Frames(20);
        lists.Confirm(); Frames(4);
        Until(() => engine.Choice.IsOpen, "the way up's question", 200); Frames(20);
        Shot("ug80_go_up_question");
        engine.Choice.Confirm(); Frames(4);
        Until(() => game.Map != below && game.State == GameState.Overworld, "the surface", 400);
        Frames(40);
        game.LocationSign.Hide();
        Frames(4);
        Shot("ug81_above_ground_again");
        Tell($"came up at {game.Player.GridX}, {game.Player.GridY} of {game.Map.Name} (went down at {above})");

        // ---- The Underground Man: the kit's giver, and the first mission's reward
        var house = MapDatabase.Get("EternaUndergroundManHouse");
        var man = house.Everyone.First(n => n.Key == "underground_man");
        void TalkToTheMan(string word, string shot)
        {
            At("EternaUndergroundManHouse", man.GridX, man.GridY + 1, Direction.Up);
            Frames(6);
            game.Interact(); Frames(3);
            ReadOn(word, shot);
            Frames(10);
        }
        int trapsBefore = State().Traps.Count;
        TalkToTheMan("Explorer Kit", "ug90_underground_man");
        TalkToTheMan("Trap", "ug91_underground_man_reward");
        Tell($"the Underground Man: mission {story.Var("VAR_CURRENT_UNDERGROUND_MISSION")}, active {story.Has("FLAG_HAS_ACTIVE_UNDERGROUND_MISSION")}, traps {trapsBefore} to {State().Traps.Count}");
    }
}
