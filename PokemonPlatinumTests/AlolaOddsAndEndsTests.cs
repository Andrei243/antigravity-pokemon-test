using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Facilities;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// The odds and ends Alola's last two waves left (plan 23, "Sharpedo's currents, Mantine Surf, the day and night cells, the
/// widow's Spooky Plate and the Battle Tree's Multi room"; rulings, the same section): a reef's gap with a strong current
/// that only Sharpedo's Jet stems, Mantine Surf and its course of gates, twelve cells that show by day or by night only, the
/// old woman of Hau'oli Cemetery and the Battle Tree's partner room. Played headless.
/// </summary>
[Collection("Shared clocks and dice")]
public class AlolaOddsAndEndsTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Alola"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);

    // ---- Sharpedo's currents

    /// <summary>The gap of the reef off the Secluded Shore: a strong current flowing west, the islet's sand to its east.</summary>
    private const int GapX = 176, GapY = 344, WaterX = 175;

    private static void Press(Player player, Map map, Direction dir)
    {
        player.Facing = dir;
        bool started = false;
        for (int frame = 0; frame < 600; frame++)
        {
            player.Advance(1f / 60f, map, started ? null : dir, false, _ => { }, _ => { });
            started |= player.IsMoving;
            if (started && !player.IsMoving && !player.IsSliding) return;
            if (!started && frame > 2) return;
        }
    }

    [Fact]
    public void AReefRingsAnIsletAndItsOneGapHoldsACurrentThatCarriesALaprasBackOut()
    {
        var map = MapNamed("Ulaula");
        Assert.Equal(TileBehavior.CurrentWest, map.BehaviourAt(GapX, GapY));
        Assert.True(TileBehaviors.IsSurfable(TileBehavior.CurrentWest));
        // The reef shuts the ring everywhere else, and the sand lies inside it
        foreach (var (x, y) in new[] { (GapX, GapY - 1), (GapX, GapY + 1), (GapX + 8, GapY), (GapX + 4, GapY - 3), (GapX + 4, GapY + 3) })
            Assert.False(map.IsWalkable(x, y), $"{x},{y} is open");
        Assert.True(map.IsWalkable(GapX + 1, GapY));
        // A Rare Candy lies on the islet and nothing else shuts it
        var ball = map.Everyone.Single(n => n.IsItemBall && n.Item == "Rare Candy" && n.GridX > GapX && n.GridX < GapX + 9 && Math.Abs(n.GridY - GapY) < 4);
        Assert.True(map.IsWalkable(ball.GridX, ball.GridY) || map.GetNpcAt(ball.GridX, ball.GridY) != null);

        // A surfer on a Lapras's back steps onto the gap and is carried back out the way the current flows
        var lapras = new Player(WaterX, GapY) { Carrier = "Lapras" };
        lapras.SetMode(TravelMode.Surfing);
        Press(lapras, map, Direction.Right);
        Assert.Equal((WaterX, GapY), (lapras.GridX, lapras.GridY));
        Assert.False(lapras.IsSliding);
    }

    [Fact]
    public void SharpedoStemsTheCurrentAndLandsOnTheIslet()
    {
        var map = MapNamed("Ulaula");
        Assert.True(RidePager.StemsCurrents("Sharpedo"));
        Assert.False(RidePager.StemsCurrents("Mantine"));
        Assert.False(RidePager.StemsCurrents("Lapras"));
        var jet = new Player(WaterX, GapY) { Carrier = "Lapras" };
        jet.SetMode(TravelMode.Surfing);
        Assert.True(jet.SetJet("Sharpedo"));
        Press(jet, map, Direction.Right);
        // On the gap's own tile and carried nowhere
        Assert.Equal((GapX, GapY), (jet.GridX, jet.GridY));
        Assert.False(jet.IsSliding);
        Press(jet, map, Direction.Right);
        Assert.Equal(GapX + 1, jet.GridX);
        // A Mantine's rider is no better off than a Lapras's
        var mantine = new Player(WaterX, GapY) { Carrier = "Lapras" };
        mantine.SetMode(TravelMode.Surfing);
        Assert.True(mantine.SetJet("Mantine"));
        Press(mantine, map, Direction.Right);
        Assert.Equal((WaterX, GapY), (mantine.GridX, mantine.GridY));
        // Coming out the other way, a surfer set down on the gap itself (as a Lapras's rider is who is let off at it) is carried west
        var out0 = new Player(GapX, GapY) { Carrier = "Lapras" };
        out0.ComeDownOn(map);
        Assert.True(out0.IsSliding);
        for (int frame = 0; frame < 600 && (out0.IsMoving || out0.IsSliding || frame < 3); frame++)
            out0.Advance(1f / 60f, map, null, false, _ => { }, _ => { });
        Assert.Equal(WaterX, out0.GridX);
    }

    // ---- Mantine Surf

    private sealed class Game
    {
        public readonly StoryState Story = new();
        public readonly Party Party = new();
        public readonly Inventory Bag = new();
        public TravelMode Mode = TravelMode.OnFoot;
        public string? Riding;
        private readonly Func<string, Map> rooms = MapDatabase.Fresh();
        private readonly Dictionary<string, Map> cache = new();

        public Game()
        {
            Story.ChooseStarter("Litten");
            Story.SetVar("VAR_ALOLA_STORY", 54);
            Story.SetVar("VAR_ALOLA_AKALA", 8);
            Story.Set("AlolaHallOfFame");
            Party.Add(new Pokemon(PokemonDatabase.Get("Litten")!, 60));
        }

        private Dictionary<string, Map>? built;

        public Map Fresh(string name)
        {
            if (cache.TryGetValue(name, out var m)) return m;
            built ??= LoadedWorld.Value.BuildMaps().ToDictionary(b => b.Name);
            return cache[name] = built.TryGetValue(name, out var b) ? b : rooms(name);
        }

        public HeadlessScriptHost Play(string file, string script, string map, string? person = null, int[]? answers = null,
            DayOfWeek weekday = DayOfWeek.Monday, TimeOfDay time = TimeOfDay.Day)
        {
            var at = Fresh(map);
            var host = new HeadlessScriptHost(Story, Party, Bag) { Map = at, MapNamed = Fresh, Region = RegionDatabase.Alola, Money = 50000, PlayerMode = Mode, Weekday = weekday, TimeOfDay = time };
            foreach (int a in answers ?? Array.Empty<int>()) host.Answers.Enqueue(a);
            if (Riding != null) host.RidePokemon(Riding);
            var library = ScriptLibrary.Default;
            var runner = new ScriptRunner(library, host);
            runner.Start(library.Find(script, file) ?? throw new Xunit.Sdk.XunitException($"{file}.{script} is missing"),
                person == null ? null : at.Everyone.Single(n => n.Key == person && (at.AreaAt(n.GridX, n.GridY)?.Key ?? file) == file));
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }
    }

    private static Game BeachGame()
    {
        var game = new Game();
        game.Story.Set("FLAG_ALOLA_BIG_WAVE_THANKS");
        return game;
    }

    [Fact]
    public void TheSurferRegistersMantineOnlyForAPagerAndMantineIsAWaterRide()
    {
        var game = BeachGame();
        // Without the Ride Pager the surfer has only his waves to talk about
        game.Play("big_wave_beach", "Surfer", "Melemele", "surfer");
        Assert.False(game.Story.Has(RidePager.MantineFlag));
        game.Bag.AddItem(ItemDatabase.Get(RidePager.Item)!, 1);
        game.Play("big_wave_beach", "Surfer", "Melemele", "surfer");
        Assert.True(game.Story.Has(RidePager.MantineFlag));
        Assert.True(RidePager.IsWaterRide("Mantine"));
        Assert.Equal("Mantine", RidePager.SpeciesOf("Mantine"));
        // Called under a surfer, like Sharpedo
        game.Mode = TravelMode.Surfing;
        var host = new HeadlessScriptHost(game.Story, game.Party, game.Bag) { Region = RegionDatabase.Alola, Map = MapNamed("Melemele"), PlayerTile = (160, 175), PlayerMode = TravelMode.OnFoot };
        Assert.False(host.RidePokemon("Mantine"));
        host.PlayerMode = TravelMode.Surfing;
        Assert.True(host.RidePokemon("Mantine"));
        Assert.Equal("Mantine", host.Riding);
        // a run's pace over the water, and no rock-ramming
        Assert.Equal(Pace.Fast, FieldMovement.PaceOn(TileBehavior.Sea, new Walker(TravelMode.Surfing, Steed: "Mantine")));
        Assert.True(RidePager.Skims("Mantine"));
        Assert.False(RidePager.Rams("Mantine"));
        // The pager's menu offers it
        var pager = ScriptLibrary.Default.Find("RidePager", null) ?? throw new Xunit.Sdk.XunitException("no RidePager");
        Assert.Contains(pager.Everything(), i => i.Op == Op.RidePokemon && i.Name == "Mantine");
    }

    [Fact]
    public void ASurferOnMantineMeetsNoWildPokemonInTheWaterAndALaprasRiderDoes()
    {
        Dice.Seed(7);
        var map = MapNamed("Melemele");
        int Meetings(string steed)
        {
            var player = new Player(160, 175) { Carrier = "Lapras", Lead = null };
            player.SetMode(TravelMode.Surfing);
            if (steed != "Lapras") Assert.True(player.SetJet(steed));
            int wild = 0;
            for (int n = 0; n < 160; n++)
            {
                var dir = (n / 8) % 2 == 0 ? Direction.Right : Direction.Left;
                player.Facing = dir;
                bool started = false;
                for (int frame = 0; frame < 600; frame++)
                {
                    player.Advance(1f / 60f, map, started ? null : dir, false, _ => wild++, _ => { });
                    started |= player.IsMoving;
                    if (started && !player.IsMoving) break;
                    if (!started && frame > 2) break;
                }
            }
            return wild;
        }
        Assert.True(Meetings("Lapras") > 0, "a Lapras's rider should meet something in 160 steps of the swell");
        Assert.Equal(0, Meetings("Mantine"));
    }

    [Fact]
    public void TheCourseIsFourGatesRiddenInOrderOnMantineAndPaysOnTheFirstAndThirdRun()
    {
        var game = BeachGame();
        game.Bag.AddItem(ItemDatabase.Get(RidePager.Item)!, 1);
        game.Play("big_wave_beach", "Surfer", "Melemele", "surfer");
        Assert.Equal(0, game.Story.Var("VAR_ALOLA_MANTINE"));
        // Asked about the course, he starts a run
        game.Play("big_wave_beach", "Surfer", "Melemele", "surfer", answers: new[] { 1 });
        Assert.Equal(0, game.Story.Var("VAR_ALOLA_MANTINE"));
        game.Play("big_wave_beach", "Surfer", "Melemele", "surfer", answers: new[] { 0 });
        Assert.Equal(1, game.Story.Var("VAR_ALOLA_MANTINE"));
        // The gates are triggers on the swell that wait for their number
        var triggers = MapNamed("Melemele").Triggers.Where(t => t.Variable == "VAR_ALOLA_MANTINE").OrderBy(t => t.Value).ToList();
        Assert.Equal(new[] { 1, 2, 3, 4 }, triggers.Select(t => t.Value));
        Assert.All(triggers, t => Assert.True(TileBehaviors.IsSurfable(MapNamed("Melemele").BehaviourAt(t.X + 1, t.Y))));

        // A run on Mantine's back
        game.Mode = TravelMode.Surfing;
        game.Riding = "Mantine";
        for (int gate = 1; gate <= 4; gate++)
        {
            Assert.Equal(gate, game.Story.Var("VAR_ALOLA_MANTINE"));
            game.Play("big_wave_beach", "MantineGate" + gate, "Melemele");
        }
        Assert.Equal(0, game.Story.Var("VAR_ALOLA_MANTINE"));
        Assert.Equal(1, game.Story.Var("VAR_ALOLA_MANTINE_RUNS"));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Big Pearl")!));

        // A second run pays nothing, a third a Star Piece
        for (int run = 2; run <= 3; run++)
        {
            game.Story.SetVar("VAR_ALOLA_MANTINE", 1);
            for (int gate = 1; gate <= 4; gate++) game.Play("big_wave_beach", "MantineGate" + gate, "Melemele");
        }
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Big Pearl")!));
        Assert.Equal(1, game.Bag.GetQuantity(ItemDatabase.Get("Star Piece")!));
    }

    [Fact]
    public void ARunEndsAtOnceForSomeoneNotOnMantineAndSharpedoPloughsThroughTheBuoys()
    {
        var game = BeachGame();
        game.Story.SetVar("VAR_ALOLA_MANTINE", 2);
        // On foot or on a Lapras (no ride at all)
        game.Mode = TravelMode.Surfing;
        game.Play("big_wave_beach", "MantineGate2", "Melemele");
        Assert.Equal(0, game.Story.Var("VAR_ALOLA_MANTINE"));
        // On Sharpedo, which rams rocks and so ploughs the buoys, the run is over too
        game.Story.SetVar("VAR_ALOLA_MANTINE", 1);
        game.Story.Set(RidePager.SharpedoFlag);
        game.Riding = "Sharpedo";
        game.Play("big_wave_beach", "MantineGate1", "Melemele");
        Assert.Equal(0, game.Story.Var("VAR_ALOLA_MANTINE"));
        Assert.Equal(0, game.Story.Var("VAR_ALOLA_MANTINE_RUNS"));
        // RAMMING is Sharpedo's alone: the sea's rocks crumble for it and no Mantine
        game.Story.Set(RidePager.MantineFlag);
        game.Riding = "Mantine";
        game.Mode = TravelMode.Surfing;
        Assert.DoesNotContain(game.Play("ulaula_route_15", "SeaRock", "Ulaula", "sea_rock").Log, l => l.StartsWith("setflag"));
    }

    // ---- The day and night cells

    [Fact]
    public void TwelveCellsShowByDayOrByNightOnlyAndTheClockKeepsTheirFlags()
    {
        var cells = BuiltMaps.Value.Values.Concat(new[] { MapNamed("BlushMountain_PowerPlant") })
            .SelectMany(m => m.Everyone.Where(n => n.NpcType == NPC.ZygardeCellType)).ToList();
        Assert.Equal(100, cells.Count);
        Assert.Equal(6, cells.Count(c => c.ShownBy == ClockFlags.Daylight));
        Assert.Equal(6, cells.Count(c => c.ShownBy == ClockFlags.Nightfall));
        Assert.DoesNotContain(cells, c => c.Script == "ZygardeCore" && c.ShownBy != null);

        var story = new StoryState();
        // Nothing is kept before the Cube is had
        Assert.False(ClockFlags.Keep(story, 12));
        Assert.False(story.Has(ClockFlags.Daylight) || story.Has(ClockFlags.Nightfall));
        story.Set(ClockFlags.CubeGiven);
        foreach (int hour in new[] { 6, 12, 17 })
        {
            ClockFlags.Keep(story, hour);
            Assert.True(story.Has(ClockFlags.Daylight), $"hour {hour}");
            Assert.False(story.Has(ClockFlags.Nightfall), $"hour {hour}");
        }
        foreach (int hour in new[] { 18, 23, 0, 5 })
        {
            ClockFlags.Keep(story, hour);
            Assert.False(story.Has(ClockFlags.Daylight), $"hour {hour}");
            Assert.True(story.Has(ClockFlags.Nightfall), $"hour {hour}");
        }
        // Written only when it changes: four times a day
        Assert.Equal(4, Enumerable.Range(0, 48).Count(h => ClockFlags.Keep(story, h % 24)));

        // A daylight cell is on its map by day, a night one by night
        var map = BuiltMaps.Value["Melemele"];
        var day = map.Everyone.Single(n => n.NpcType == NPC.ZygardeCellType && n.ShownBy == ClockFlags.Daylight);
        var night = map.Everyone.Single(n => n.NpcType == NPC.ZygardeCellType && n.ShownBy == ClockFlags.Nightfall);
        ClockFlags.Keep(story, 12);
        map.ApplyPresence(story.Has);
        Assert.Contains(day, map.NPCs);
        Assert.DoesNotContain(night, map.NPCs);
        ClockFlags.Keep(story, 22);
        map.ApplyPresence(story.Has);
        Assert.DoesNotContain(day, map.NPCs);
        Assert.Contains(night, map.NPCs);
    }

    [Fact]
    public void ACellSetsTheCubeFlagSoAnOlderSaveWithTheCubeFindsTheTimedOnes()
    {
        var game = new Game();
        game.Bag.AddItem(ItemDatabase.Get("Zygarde Cube")!, 1);
        Assert.False(game.Story.Has(ClockFlags.CubeGiven));
        game.Play("kalae_bay", "ZygardeCell", "Melemele", "zygarde_cell_1");
        Assert.True(game.Story.Has(ClockFlags.CubeGiven));
        var fresh = new Game();
        fresh.Play("ulaula_route_16", "Dexio", "Ulaula", "dexio");
        Assert.True(fresh.Story.Has(ClockFlags.CubeGiven));
    }

    // ---- The Cores' own moves

    [Fact]
    public void EachCoreUnlocksOneOfTheCubesFiveMovesAndTheCubeOffersOnlyTheOnesFound()
    {
        // The five Cores' flags are the five Cores' own (what a pickup sets), one to a move of the Cube
        var cores = BuiltMaps.Value.Values.Concat(new[] { MapNamed("BlushMountain_PowerPlant") })
            .SelectMany(m => m.Everyone.Where(n => n.NpcType == NPC.ZygardeCellType && n.Script == "ZygardeCore")).Select(n => n.HiddenBy).ToList();
        Assert.Equal(5, cores.Count);
        Assert.Equal(cores.OrderBy(c => c), FormChanges.CoreMoves.Select(c => c.Core).OrderBy(c => c));
        Assert.Equal(FormChanges.CubeMoves.OrderBy(m => m), FormChanges.CoreMoves.Select(c => c.Move).OrderBy(m => m));

        var cube = ItemDatabase.Get("Zygarde Cube")!;
        var zygarde = new Pokemon(PokemonDatabase.Get("Zygarde")!, 60);
        var story = new StoryState();
        // None found: only the change of Forme is offered
        var none = FormChanges.OptionsFor(cube, zygarde, FormChanges.CubeMovesFor(story.Has))!;
        Assert.Single(none);
        // Two found: those two after it, in the Cube's order
        story.Set(FormChanges.CoreMoves.Single(c => c.Move == "Extreme Speed").Core);
        story.Set(FormChanges.CoreMoves.Single(c => c.Move == "Thousand Waves").Core);
        var moves = FormChanges.CubeMovesFor(story.Has);
        Assert.Equal(new[] { "Thousand Waves", "Extreme Speed" }, moves);
        Assert.Equal(3, FormChanges.OptionsFor(cube, zygarde, moves)!.Count);
        var party = new Party();
        party.Add(zygarde);
        // (a Zygarde with four moves already is asked which to forget: the move comes back as the one wanted)
        var taught = FormChanges.Choose(cube, zygarde, party, 2, moves);
        Assert.True(taught.Applied);
        Assert.True(zygarde.Knows("Extreme Speed") || taught.Wanted.Contains("Extreme Speed"));
        Assert.False(zygarde.Knows("Core Enforcer"));
        Assert.DoesNotContain("Core Enforcer", taught.Wanted);
        // A tool or a test handed no list offers all five, as the Cube always did
        Assert.Equal(6, FormChanges.OptionsFor(cube, new Pokemon(PokemonDatabase.Get("Zygarde")!, 60))!.Count);
    }

    // ---- The old woman and her Machamp

    [Fact]
    public void TheOldWomanGivesTheSpookyPlateOnceTheGhostGirlHasFoundHerFriend()
    {
        var game = new Game();
        var plate = ItemDatabase.Get("Spooky Plate")!;
        game.Play("hauoli_cemetery", "Widow", "Melemele", "widow");
        Assert.Equal(0, game.Bag.GetQuantity(plate));
        game.Story.SetVar("VAR_ALOLA_CEMETERY_GIRL", 2);
        game.Play("hauoli_cemetery", "Widow", "Melemele", "widow");
        Assert.Equal(0, game.Bag.GetQuantity(plate));
        game.Story.SetVar("VAR_ALOLA_CEMETERY_GIRL", 3);
        game.Play("hauoli_cemetery", "Widow", "Melemele", "widow");
        Assert.Equal(1, game.Bag.GetQuantity(plate));
        Assert.True(game.Story.Has("FLAG_ALOLA_SPOOKY_PLATE"));
        game.Play("hauoli_cemetery", "Widow", "Melemele", "widow");
        Assert.Equal(1, game.Bag.GetQuantity(plate));
        // Her Machamp stands beside her as a Pokémon of the map, which only cries
        var machamp = game.Fresh("Melemele").Everyone.Single(n => n.Key == "widow_machamp");
        Assert.Equal(NPC.PokemonType, machamp.NpcType);
        Assert.Equal("Machamp", machamp.Species);
        Assert.Contains(game.Play("hauoli_cemetery", "WidowMachamp", "Melemele", "widow_machamp").Log, l => l.Contains("Machamp"));
    }

    // ---- The Battle Tree's Multi room

    private static Party Team(int count)
    {
        var party = new Party();
        foreach (string species in new[] { "Decidueye", "Incineroar", "Primarina", "Lycanroc" }.Take(count))
            party.Add(new Pokemon(PokemonDatabase.Get(species)!, 60, new Random(party.Count + 1)));
        return party;
    }

    private static Func<int> Draws(int seed)
    {
        var rng = new Random(seed);
        return () => rng.Next(0x10000);
    }

    [Fact]
    public void TheMultiRoomPutsTwoOfThePlayersBesideAPartnersTwoAgainstTwoTrainersWithTwoEach()
    {
        Assert.Contains("multi", BattleTree.Ways);
        Assert.False(BattleTree.HasHost("multi"));
        var room = FacilityPlaces.RoomOf(BattleTree.Place, "multi", false);
        Assert.True(room.Multi);
        Assert.Equal(2, room.Entered);
        Assert.Equal(BattleFormat.Double, room.Format);
        Assert.Equal(0, room.HostAt);
        Assert.Null(room.HostTrainer);
        // A Super room asked for is the normal room: there is none
        Assert.False(FacilityPlaces.RoomOf(BattleTree.Place, "multi", true).Hard);
        Assert.Equal(1, FacilityPlaces.TrophyWays(BattleTree.Place) - 1);
        Assert.Equal(2, FacilityPlaces.TrophyWays(BattleTree.Place));
        Assert.Equal(5, BattleTree.Rooms.Count());

        var records = new FrontierRecords();
        var party = Team(2);
        var challenge = StreakChallenge.Begin(records, room, new[] { 0, 1 }, party, Draws(11));
        var foes = challenge.Opponents(records);
        Assert.Equal(2, foes.Count);
        Assert.All(foes, f => Assert.Equal(2, f.Pokemon.Count));
        // The pair share no species and no item
        var all = foes.SelectMany(f => f.Pokemon).ToList();
        Assert.Equal(4, all.Select(p => p.Species.Name).Distinct().Count());
        Assert.Equal(4, all.Select(p => p.HeldItem?.Name).Distinct().Count());
        var trainers = challenge.OpponentsAsTrainers(records);
        Assert.Equal(2, trainers.Count);
        Assert.NotEqual(trainers[0].Id, trainers[1].Id);
        var partner = challenge.PartnerAsTrainer()!;
        Assert.Equal(2, partner.Party.Count);
        Assert.NotEmpty(challenge.PartnerName);
        // The same two come until the battle is won, and no host ever comes
        Assert.Same(foes[0], challenge.Opponents(records)[0]);
        for (int battle = 1; battle <= 55; battle++)
        {
            Assert.False(challenge.HostComes(records));
            challenge.Opponents(records);
            var won = challenge.Won(records, false);
            Assert.False(won.Host || won.OpenedHard || won.Trophy);
            Assert.Equal(battle <= 10 ? 2 : battle <= 20 ? 3 : 4, won.Points);
        }
    }

    [Fact]
    public void ThePartnerIsTheSameAcrossBattlesAndAfterAChallengeIsPutDownAndTakenUp()
    {
        var room = FacilityPlaces.RoomOf(BattleTree.Place, "multi", false);
        var records = new FrontierRecords();
        var party = Team(2);
        var challenge = StreakChallenge.Begin(records, room, new[] { 0, 1 }, party, Draws(23));
        string partnerName = challenge.PartnerName;
        var partnerSpecies = challenge.Partner!.Pokemon.Select(p => p.Species.Name).ToList();
        var first = challenge.Opponents(records).Select(o => o.ShownName).ToList();
        challenge.Won(records, false);
        Assert.Equal(partnerName, challenge.PartnerName);
        var second = challenge.Opponents(records).Select(o => o.ShownName).ToList();
        challenge.Pause(records);
        var resumed = StreakChallenge.Resume(records, room, party)!;
        Assert.NotNull(resumed);
        Assert.Equal(partnerName, resumed.PartnerName);
        Assert.Equal(partnerSpecies, resumed.Partner!.Pokemon.Select(p => p.Species.Name));
        // The trainers come again as they were drawn
        Assert.Equal(second, resumed.Opponents(records).Select(o => o.ShownName));
        Assert.Equal(2, records.Facility(BattleTree.Place, "multi", false).Streak + 1);
        Assert.NotEqual(first, second);
        // Another challenge draws its own partner from its own seed
        var other = StreakChallenge.Begin(new FrontierRecords(), room, new[] { 0, 1 }, party, Draws(99));
        Assert.NotEqual(partnerSpecies, other.Partner!.Pokemon.Select(p => p.Species.Name));
    }

    [Fact]
    public void TheMultiRoomsBoardHasARowAndItsHallStandsThePartnerAndThePairOfTrainers()
    {
        var records = new FrontierRecords();
        var board = FrontierBoard.ForFacility(BattleTree.Place, records, new StoryState());
        Assert.Equal(3, board.Rows.Count);
        // A trophy in each room that has one is gold: the Multi room has none to win
        var story = new StoryState();
        story.SetVar(BattleTree.TrophyVariable("single"), 1);
        story.SetVar(BattleTree.TrophyVariable("double"), 1);
        Assert.Equal(FrontierPrint.Gold, FrontierBoard.ForFacility(BattleTree.Place, records, story).Print);

        var hall = MapNamed("PoniGauntlet_BattleTreeHall");
        Assert.Equal("FLAG_ALOLA_TREE_SHOW_PARTNER", hall.Everyone.Single(n => n.Key == "partner").ShownBy);
        Assert.Equal("FLAG_ALOLA_TREE_SHOW_OPPONENT_2", hall.Everyone.Single(n => n.Key == "opponent_2").ShownBy);
        Assert.Equal(50, FrontierRules.Level);
    }

    [Fact]
    public void ABattleOfTheMultiRoomIsFoughtAsADoubleWithThePartnerBesideThePlayerAgainstTwoTrainers()
    {
        var party = Team(2);
        var story = new StoryState();
        var host = new HeadlessScriptHost(story, party, new Inventory()) { Region = RegionDatabase.Alola, Map = MapNamed("PoniGauntlet_BattleTreeHall"), MapNamed = MapNamed, Money = 50000, Rng = new Random(5) };
        host.Entering.Enqueue(new[] { 0, 1 });
        var library = ScriptLibrary.Default;
        // The desk's entry, as its script has it: the room, those entered, the challenge begun
        var desk = new ScriptRunner(library, host);
        desk.Start(ScriptParser.Parse("PoniGauntlet_BattleTree", "script T\n  facility tree way multi\n  facility tree hardopen\n  facility tree size\n  facility tree choose\n  facility tree check\n  facility tree begin\n  end\n").Single());
        desk.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.NotNull(host.Facility);
        Assert.True(host.Facility!.Room.Multi);

        // One battle of the hall's own script: the partner stands, a pair come down, the battle is a double with a partner
        var hallRunner = new ScriptRunner(library, host);
        host.Answers.Enqueue(2);   // retire after the first win
        hallRunner.Start(library.Find("OnEnter", "PoniGauntlet_BattleTreeHall")!);
        hallRunner.RunToEnd();
        Assert.Empty(host.Problems);
        Assert.NotEmpty(host.FrontierBattles);
        var (team, foes, partner, doubles) = host.FrontierBattles[0];
        Assert.True(doubles);
        Assert.Equal(2, team.Count);
        Assert.Equal(2, foes.Count);
        Assert.NotNull(partner);
        Assert.Equal(2, partner!.Party.Count);
        Assert.All(foes, f => Assert.Equal(2, f.Party.Count));
        Assert.Equal(BattleFormat.Double, host.FrontierFields[0]!.Format);
        // The hall is left with the partner gone and the challenge over
        Assert.Null(host.Facility);
        Assert.False(story.Has("FLAG_ALOLA_TREE_SHOW_PARTNER"));
        Assert.False(story.Has("FLAG_ALOLA_TREE_SHOW_OPPONENT_2"));
    }
}
