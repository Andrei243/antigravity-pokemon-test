using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// What finished Kalos (plan 23, after its last species): the Friend Safari in Kiloude City, three grounds of the
/// player's own drawn from the trainer number, and the League's second round, the Four and Diantha fighting rematch
/// teams of ours once the region's Hall of Fame is entered. Played headless.
/// </summary>
public class KalosFinishedTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Kalos"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Kalos)!.Start!));

    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);

    [Fact]
    public void EveryTrainerHasThreeGroundsOfThreeTypesEachWithThreeSpeciesOfItsType()
    {
        foreach (uint number in new uint[] { 0, 1, 12345, 0xDEADBEEF, 987654321, 42 })
        {
            var grounds = FriendSafari.Of(number);
            Assert.Equal(FriendSafari.GroundsCount, grounds.Count);
            Assert.Equal(FriendSafari.GroundsCount, grounds.Select(g => g.Type).Distinct().Count());
            foreach (var g in grounds)
            {
                Assert.Equal(FriendSafari.SpeciesPerGrounds, g.Species.Distinct().Count());
                Assert.All(g.Species, s => Assert.Contains(s, FriendSafari.SpeciesOfType[g.Type]));
            }
            // The same Trainer always finds the same grounds
            Assert.Equal(grounds.Select(g => (g.Type, string.Join(",", g.Species))), FriendSafari.Of(number).Select(g => (g.Type, string.Join(",", g.Species))));
        }
        // Different Trainers find different grounds
        Assert.True(Enumerable.Range(0, 40).Select(i => string.Join("|", FriendSafari.Of((uint)i * 7919u).Select(g => g.Type))).Distinct().Count() > 20);
    }

    [Fact]
    public void EveryTypeHasSixSpeciesOfItsOwnTypeUnderTheModernRules()
    {
        Assert.Equal(Enum.GetValues<PokemonType>().Length, FriendSafari.SpeciesOfType.Count);
        foreach (var (type, species) in FriendSafari.SpeciesOfType)
        {
            Assert.Equal(6, species.Distinct().Count());
            foreach (string name in species)
            {
                var s = PokemonDatabase.Get(name);
                Assert.True(s != null, $"{name} is no species");
                var modern = s!.Under(Ruleset.Modern);
                Assert.True(modern.PrimaryType == type || modern.SecondaryType == type, $"{name} is not of the {type} type");
                Assert.False(s.Legendary || s.Mythical, $"{name} is legendary");
            }
        }
    }

    [Fact]
    public void TheGroundsTableHoldsItsThreeSpeciesAtTheSafarisLevel()
    {
        var grounds = FriendSafari.Chosen(77, 1);
        Assert.Equal(FriendSafari.Of(77)[1].Species, grounds.Species);
        Assert.Equal(FriendSafari.Of(77)[0].Species, FriendSafari.Chosen(77, 9).Species);
        var table = FriendSafari.Table(grounds);
        Assert.Equal(grounds.Species, table.Select(e => e.SpeciesName));
        Assert.All(table, e => Assert.Equal((FriendSafari.Level, FriendSafari.Level), (e.MinLevel, e.MaxLevel)));
        Assert.Equal(1, table.Select(e => e.Weight).Distinct().Count());
    }

    [Fact]
    public void TheGateOpensWithTheHallOfFameAndTheGroundsAreWalkedFromIt()
    {
        var kalos = RegionDatabase.Get(RegionDatabase.Kalos)!;
        Assert.Contains(GameEngine.FriendSafariGate, kalos.Maps);
        Assert.Contains(GameEngine.FriendSafariMap, kalos.Maps);
        var map = LoadedWorld.Value.BuildMaps().Single(m => m.Name == "Kalos");
        var story = new StoryState();
        map.ApplyDoors(story.Has);
        Assert.True(map.IsSolid(575, 364), "the Friend Safari is shut before the Hall of Fame");
        story.Set("KalosHallOfFame");
        map.ApplyDoors(story.Has);
        Assert.False(map.IsSolid(575, 364));
        var warp = map.GetWarpAt(575, 364);
        Assert.Equal(GameEngine.FriendSafariGate, warp?.TargetMap);
        var gate = MapNamed(GameEngine.FriendSafariGate);
        Assert.True(gate.IsWalkable(warp!.TargetX, warp.TargetY));
        var back = gate.Warps.Single(w => w.TargetMap == "Kalos");
        Assert.True(map.IsWalkable(back.TargetX, back.TargetY));
        // The grounds: tall grass and a way back to the gate, and nothing in the file's table, which the engine lays
        var grounds = MapNamed(GameEngine.FriendSafariMap);
        Assert.Empty(grounds.WildEncounters);
        Assert.Contains(Enumerable.Range(0, grounds.Width).SelectMany(x => Enumerable.Range(0, grounds.Height).Select(y => (x, y))), p => grounds.IsTallGrass(p.x, p.y));
        Assert.All(grounds.Warps, w => Assert.Equal(GameEngine.FriendSafariGate, w.TargetMap));
        Assert.True(grounds.IsWalkable(11, 16));
        // The walk of the whole game, with every flag set, comes to the grounds
        Assert.Contains(Walked.Value, p => p.Key.Name == GameEngine.FriendSafariMap && p.Value.Contains((11, 16)));
    }

    [Fact]
    public void TheGuideSendsThePlayerToTheGroundsChosen()
    {
        var gate = Rooms.Value(GameEngine.FriendSafariGate);
        var guide = gate.Everyone.Single(n => n.Key == "guide");
        var story = new StoryState();
        story.Set("KalosHallOfFame");
        IReadOnlyList<string> offered = Array.Empty<string>();
        var host = Play(story, GameEngine.FriendSafariGate, "Guide", gate, guide, decide: (_, answers) => { offered = answers; return 2; });
        Assert.Equal(2, story.Var(FriendSafari.ChosenVar));
        Assert.Contains(host.Log, l => l.StartsWith($"warp {GameEngine.FriendSafariMap} 11 16", StringComparison.Ordinal));
        // The answers name the player's own three types
        var types = FriendSafari.Of(PlayerIdentity.Number).Select(g => Words.Type(g.Type)).ToList();
        Assert.Equal(4, offered.Count);
        for (int i = 0; i < 3; i++) Assert.Contains(types[i], offered[i]);
        // Not today: nowhere
        var later = Play(new StoryState(), GameEngine.FriendSafariGate, "Guide", gate, guide, answers: new[] { 3 });
        Assert.DoesNotContain(later.Log, l => l.StartsWith("warp", StringComparison.Ordinal));
    }

    [Fact]
    public void ParfumPalaceOpensWithTheFluteAndItsOwnerThanksTheFinderOfHisFurfrou()
    {
        Assert.Contains("KalosRoute6_ParfumPalace", RegionDatabase.Get(RegionDatabase.Kalos)!.Maps);
        var map = LoadedWorld.Value.BuildMaps().Single(m => m.Name == "Kalos");
        var story = new StoryState();
        map.ApplyDoors(story.Has);
        Assert.True(map.IsSolid(201, 265), "the palace is shut before the flute");
        story.Set("FLAG_KALOS_RECEIVED_HM01");
        map.ApplyDoors(story.Has);
        var warp = map.GetWarpAt(201, 265);
        Assert.Equal("KalosRoute6_ParfumPalace", warp?.TargetMap);
        var hall = MapNamed("KalosRoute6_ParfumPalace");
        Assert.True(hall.IsWalkable(warp!.TargetX, warp.TargetY));
        var front = hall.Warps.Single(w => w.TargetMap == "Kalos");
        Assert.True(map.IsWalkable(front.TargetX, front.TargetY));
        Assert.Contains(Walked.Value, p => p.Key.Name == "KalosRoute6_ParfumPalace");

        var owner = hall.Everyone.Single(n => n.Key == "owner");
        var bag = new Inventory();
        Play(story, "KalosRoute6_ParfumPalace", "Owner", hall, owner, bag: bag);
        Assert.Equal(0, bag.GetQuantity(ItemDatabase.Get("Sun Stone")!));
        Play(story, "KalosRoute6_ParfumPalace", "FurfrouAway", hall, hall.Everyone.Single(n => n.Key == "furfrou_away"), bag: bag);
        Assert.True(story.Has("FLAG_KALOS_PALACE_FURFROU_FOUND"));
        hall.ApplyPresence(story.Has);
        Assert.DoesNotContain(hall.NPCs, n => n.Key == "furfrou_away");
        Assert.Contains(hall.NPCs, n => n.Key == "furfrou_home");
        Play(story, "KalosRoute6_ParfumPalace", "Owner", hall, owner, bag: bag);
        Play(story, "KalosRoute6_ParfumPalace", "Owner", hall, owner, bag: bag);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Sun Stone")!));
    }

    [Fact]
    public void XAndYsLastMegaStonesAreAllInKalos()
    {
        // The three that lie at the stones' hour stand on ground the walk of the whole game comes to
        foreach (var (map, key, item) in new[]
                 {
                     ("Kalos", "mega_gengarite", "Gengarite"), ("Kalos", "mega_ampharosite", "Ampharosite"), ("FrostCavern", "mega_abomasite", "Abomasite")
                 })
        {
            var on = MapNamed(map);
            var ball = on.Everyone.Single(n => n.Key == key);
            Assert.Equal(item, ball.Item);
            Assert.Equal("FLAG_KALOS_STONES_GLINT", ball.ShownBy);
            var beside = new[] { (0, 1), (0, -1), (1, 0), (-1, 0) };
            Assert.Contains(Walked.Value, p => p.Key.Name == map && beside.Any(d => p.Value.Contains((ball.GridX + d.Item1, ball.GridY + d.Item2))));
        }
        // The laboratory's assistant gives the Aerodactylite once the Mega Ring is upgraded, and only once
        var lab = Rooms.Value("AmbretteTown_FossilLab");
        var assistant = lab.Everyone.Single(n => n.Key == "assistant");
        var story = new StoryState();
        story.Set("FLAG_KALOS_RECEIVED_OLD_AMBER");
        var bag = new Inventory();
        Play(story, "AmbretteTown_FossilLab", "Assistant", lab, assistant, bag: bag);
        Assert.Equal(0, bag.GetQuantity(ItemDatabase.Get("Aerodactylite")!));
        story.Set("FLAG_KALOS_MEGA_RING_UPGRADED");
        Play(story, "AmbretteTown_FossilLab", "Assistant", lab, assistant, bag: bag);
        Play(story, "AmbretteTown_FossilLab", "Assistant", lab, assistant, bag: bag);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Aerodactylite")!));
        // Diantha gives the Gardevoirite after her first second-round battle, and only once
        var champion = Rooms.Value("KalosLeague_ChampionRoom");
        var diantha = champion.Everyone.Single(n => n.Key == "diantha");
        var after = new StoryState();
        after.Set("KalosHallOfFame");
        Play(after, "KalosLeague_ChampionRoom", "Diantha", champion, diantha, bag: bag);
        after.Unset("FLAG_KALOS_DEFEATED_DIANTHA");
        Play(after, "KalosLeague_ChampionRoom", "Diantha", champion, diantha, bag: bag);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Gardevoirite")!));
        // Diancie and Hoopa leave their stone and their bottle
        Assert.Contains(ScriptLibrary.Default.Find("Diancie", "reflection_cave")!.Everything(), i => i.Op == Op.Find && i.Name == "Diancite");
        Assert.Contains(ScriptLibrary.Default.Find("Hoopa", "lost_hotel")!.Everything(), i => i.Op == Op.Find && i.Name == "Prison Bottle");
    }

    [Fact]
    public void TheBicycleShopGivesABicycleOnceThePowerIsBackToABagWithoutOne()
    {
        var shop = Rooms.Value("CyllageCity_BikeShop");
        var owner = shop.Everyone.Single(n => n.Key == "owner");
        var bicycle = ItemDatabase.Get("Bicycle")!;
        var story = new StoryState();
        var bag = new Inventory();
        Play(story, "CyllageCity_BikeShop", "Owner", shop, owner, bag: bag);
        Assert.Equal(0, bag.GetQuantity(bicycle));
        story.SetVar("VAR_KALOS_LUMIOSE_ROADS", 2);
        Play(story, "CyllageCity_BikeShop", "Owner", shop, owner, bag: bag);
        Assert.Equal(1, bag.GetQuantity(bicycle));
        Play(story, "CyllageCity_BikeShop", "Owner", shop, owner, bag: bag);
        Assert.Equal(1, bag.GetQuantity(bicycle));
    }

    [Theory]
    [InlineData("KalosLeague_MalvaRoom", "Malva", "malva")]
    [InlineData("KalosLeague_SieboldRoom", "Siebold", "siebold")]
    [InlineData("KalosLeague_WikstromRoom", "Wikstrom", "wikstrom")]
    [InlineData("KalosLeague_DrasnaRoom", "Drasna", "drasna")]
    [InlineData("KalosLeague_ChampionRoom", "Diantha", "diantha")]
    public void TheLeagueFightsItsRematchTeamsOnceTheHallOfFameIsEntered(string room, string script, string who)
    {
        var map = Rooms.Value(room);
        var person = map.Everyone.Single(n => n.Key == who);
        var first = Play(new StoryState(), room, script, map, person);
        Assert.Contains(first.Log, l => l.Contains($"kalos_{who}_1", StringComparison.Ordinal));
        var again = new StoryState();
        again.Set("KalosHallOfFame");
        var second = Play(again, room, script, map, person);
        Assert.Contains(second.Log, l => l.Contains($"kalos_{who}_rematch", StringComparison.Ordinal));
        Assert.True(again.Has($"FLAG_KALOS_DEFEATED_{who.ToUpperInvariant()}"));
        // Each rematch team is a rematch's (met at the furthest region's levels once Kalos is gone past), stronger than the first
        Assert.True(TrainerDatabase.IsRematchTeam($"kalos_{who}_rematch"));
        var t1 = TrainerDatabase.Get($"kalos_{who}_1")!;
        var t2 = TrainerDatabase.Get($"kalos_{who}_rematch")!;
        Assert.True(t2.Party.Max(p => p.Level) > t1.Party.Max(p => p.Level));
        Assert.True(t2.Party.Count >= t1.Party.Count);
    }

    private static HeadlessScriptHost Play(StoryState story, string file, string script, Map at, NPC? who = null, int[]? answers = null,
        Func<string, IReadOnlyList<string>, int>? decide = null, Inventory? bag = null)
    {
        at.ForgetForced();
        at.ApplyPresence(story.Has);
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Greninja")!, 80));
        var host = new HeadlessScriptHost(story, party, bag ?? new Inventory()) { Map = at, Region = RegionDatabase.Kalos, MapNamed = MapNamed, Decide = decide };
        foreach (int a in answers ?? Array.Empty<int>()) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, file) ?? throw new Xunit.Sdk.XunitException($"{file}.{script} is missing"), who);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }
}
