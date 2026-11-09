using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Kalos's last species (plan 23; docs/mechanics/rulings.md, "Kalos's last species: the two Fossils, the three mythicals,
/// the village's water mouth and Lumiose's last doors"): the Glittering Cave's fossil seeker and the two Fossils of Kalos
/// restored, Diancie, Hoopa and Volcanion after the Hall of Fame, the Unknown Dungeon's mouth over the pond, and Lumiose's
/// last thirteen doors with the Battle Institute's test. Played headless.
/// </summary>
public class KalosLastSpeciesTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Kalos"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);

    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);

    private static readonly (int X, int Z, string Room)[] Doors =
    {
        (342, 317, "LumioseCity_BattleInstitute"), (349, 317, "LumioseCity_PrStudio"), (333, 327, "LumioseCity_CafeIntroversion"),
        (323, 337, "LumioseCity_CafeUltimo"), (332, 337, "LumioseCity_HatShop"), (313, 347, "LumioseCity_CafeWoof"),
        (322, 347, "LumioseCity_CafeSoleil"), (397, 347, "LumioseCity_ShutterbugCafe"), (332, 357, "LumioseCity_CafeKizahashi"),
        (340, 357, "LumioseCity_CoiffureClips"), (312, 377, "LumioseCity_VertApartment"), (321, 377, "LumioseCity_PainterHouse"),
        (321, 387, "LumioseCity_OldCoupleHouse")
    };

    [Fact]
    public void TheJawAndSailFossilsAreRestoredAsTyruntAndAmaura()
    {
        Assert.NotNull(ItemDatabase.Get("Jaw Fossil"));
        Assert.NotNull(ItemDatabase.Get("Sail Fossil"));
        Assert.Equal("Tyrunt", Fossils.SpeciesOf("Jaw Fossil"));
        Assert.Equal("Amaura", Fossils.SpeciesOf("Sail Fossil"));
    }

    [Theory]
    [InlineData(0, "Jaw Fossil", "Sail Fossil")]
    [InlineData(1, "Sail Fossil", "Jaw Fossil")]
    public void TheFossilSeekerGivesTheOneChosenAndTheOtherOnALaterVisit(int answer, string first, string second)
    {
        var cave = MapNamed("GlitteringCave");
        var seeker = cave.Everyone.Single(n => n.Key == "fossil_seeker");
        var story = new StoryState();
        var bag = new Inventory();
        Play(story, "glittering_cave", "FossilSeeker", cave, seeker, bag, answers: new[] { answer });
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get(first)!));
        Assert.Equal(0, bag.GetQuantity(ItemDatabase.Get(second)!));
        // Spoken to again before the player has gone, he is still digging
        Play(story, "glittering_cave", "FossilSeeker", cave, seeker, bag);
        Assert.Equal(0, bag.GetQuantity(ItemDatabase.Get(second)!));
        // Away and back: the other one, once
        story.ClearLocal();
        Play(story, "glittering_cave", "FossilSeeker", cave, seeker, bag);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get(second)!));
        Play(story, "glittering_cave", "FossilSeeker", cave, seeker, bag);
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get(first)!));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get(second)!));
        // Refused, nothing is given
        var none = new Inventory();
        Play(new StoryState(), "glittering_cave", "FossilSeeker", cave, seeker, none, answers: new[] { 2 });
        Assert.Equal(0, Fossils.Count(none));
    }

    [Fact]
    public void DiancieComesOutOnlyToATrainerWithACarbinkAfterTheHallOfFame()
    {
        var cave = MapNamed("ReflectionCave");
        var story = new StoryState();
        cave.ForgetForced();
        cave.ApplyPresence(story.Has);
        Assert.DoesNotContain(cave.NPCs, n => n.Key == "diancie");
        story.Set("KalosHallOfFame");
        cave.ApplyPresence(story.Has);
        var diancie = cave.NPCs.Single(n => n.Key == "diancie");
        var shy = Play(story, "reflection_cave", "Diancie", cave, diancie);
        Assert.DoesNotContain(shy.Log, l => l.StartsWith("wildbattle", StringComparison.Ordinal));
        var met = Play(story, "reflection_cave", "Diancie", cave, diancie, extra: "Carbink");
        Assert.Contains("wildbattle Diancie 70 Won", met.Log);
        Assert.True(story.Has("FLAG_KALOS_DIANCIE_GONE"));
        cave.ForgetForced();
        cave.ApplyPresence(story.Has);
        Assert.DoesNotContain(cave.NPCs, n => n.Key == "diancie");
    }

    [Theory]
    [InlineData("LostHotel", "lost_hotel", "hoopa", "Hoopa", "FLAG_KALOS_MEGA_RING_UPGRADED", "FLAG_KALOS_HOOPA_GONE")]
    [InlineData("Kalos", "couriway_town", "volcanion", "Volcanion", "KalosHallOfFame", "FLAG_KALOS_VOLCANION_GONE")]
    public void HoopaAndVolcanionWaitInTheirPlacesAndAreGoneOnceBeaten(string mapName, string file, string key, string species, string shownBy, string gone)
    {
        var map = MapNamed(mapName);
        var story = new StoryState();
        map.ForgetForced();
        map.ApplyPresence(story.Has);
        Assert.DoesNotContain(map.NPCs, n => n.Key == key);
        story.Set(shownBy);
        map.ApplyPresence(story.Has);
        var who = map.NPCs.Single(n => n.Key == key);
        Assert.True(map.IsWalkable(who.GridX, who.GridY + 1), $"{species} is spoken to from a blocked tile");
        // Run from, it stays
        var ran = Play(story, file, species, map, who, fight: BattleOutcome.Fled);
        Assert.Contains($"wildbattle {species} 70 Fled", ran.Log);
        Assert.False(story.Has(gone));
        var met = Play(story, file, species, map, who);
        Assert.Contains($"wildbattle {species} 70 Won", met.Log);
        Assert.True(story.Has(gone));
        map.ForgetForced();
        map.ApplyPresence(story.Has);
        Assert.DoesNotContain(map.NPCs, n => n.Key == key);
    }

    [Theory]
    [InlineData("ReflectionCave", "reflection_cave", "diancie", "Diancie", 26, 10, "Carbink")]
    [InlineData("LostHotel", "lost_hotel", "hoopa", "Hoopa", 40, 14, null)]
    [InlineData("Kalos", "couriway_town", "volcanion", "Volcanion", 654, 378, null)]
    public void APlayerWhoSpeaksFromTheTileSouthStepsOutOfTheWayFirst(string mapName, string file, string key, string species, int x, int y, string? brings)
    {
        // One tile south of it, the player would hide it behind their head (plan 29 · LS11d's rule)
        var map = MapNamed(mapName);
        var story = new StoryState();
        story.Set("KalosHallOfFame");
        story.Set("FLAG_KALOS_MEGA_RING_UPGRADED");
        map.ForgetForced();
        map.ApplyPresence(story.Has);
        var who = map.NPCs.Single(n => n.Key == key);
        var host = Play(story, file, species, map, who, extra: brings, from: (who.GridX, who.GridY + 1));
        Assert.Equal((x, y), host.PlayerTile);
        Assert.True(map.IsWalkable(x, y), $"{species}'s scene walks the player onto a blocked tile");
    }

    [Fact]
    public void TheUnknownDungeonsMouthIsComeToOnlyAcrossThePond()
    {
        var kalos = MapNamed("Kalos");
        // The mouth, the landing before it, and water on the landing's three other sides
        Assert.Equal("UnknownDungeon", kalos.GetWarpAt(698, 107)?.TargetMap);
        Assert.True(kalos.IsWalkable(698, 108));
        foreach (var (x, y) in new[] { (697, 108), (699, 108), (698, 109) })
        {
            Assert.False(kalos.IsWalkable(x, y), $"{x},{y} beside the landing can be walked on");
            Assert.True(TileBehaviors.IsSurfable(kalos.BehaviourAt(x, y)), $"{x},{y} beside the landing is no water");
        }
    }

    [Fact]
    public void LumiosesLastDoorsLeadIntoTheirRoomsAndBackAndNoDoorOfTheCityIsShut()
    {
        var kalos = RegionDatabase.Get(RegionDatabase.Kalos)!;
        var from = MapNamed("Kalos");
        foreach (var (x, z, room) in Doors)
        {
            Assert.Contains(room, kalos.Maps);
            var warp = from.GetWarpAt(x, z);
            Assert.Equal(room, warp?.TargetMap);
            var inside = MapNamed(room);
            Assert.True(inside.IsWalkable(warp!.TargetX, warp.TargetY), $"{room}: the way in lands on a blocked tile");
            var back = inside.Warps.Single(w => w.TargetMap == "Kalos");
            Assert.Equal((x, z + 1), (back.TargetX, back.TargetY));
            Assert.True(from.IsWalkable(back.TargetX, back.TargetY), $"{room}: the way out lands on a blocked tile");
        }
        // Every door of the city's own area file leads into a room of Kalos (some only once the story opens them)
        var area = LoadedWorld.Value.Area("lumiose_city")!;
        foreach (var (w, i) in area.Warps.Select((w, i) => (w, i)))
        {
            var way = from.WarpsAt(w.X, w.Z).FirstOrDefault();
            Assert.True(way != null && kalos.Maps.Contains(way.TargetMap), $"Lumiose's door {i} leads nowhere");
        }
    }

    [Theory]
    [InlineData(9, "PP Up", 1)]
    [InlineData(15, "Protein", 1)]
    [InlineData(21, "Hyper Potion", 2)]
    [InlineData(22, "Super Potion", 2)]
    public void TheBattleInstitutesTestIsThreeBattlesMarkedByTheirTurns(int turns, string item, int count)
    {
        var room = Rooms.Value("LumioseCity_BattleInstitute");
        var desk = room.Everyone.Single(n => n.Key == "receptionist");
        var story = new StoryState();
        var bag = new Inventory();
        int battle = 0;
        var host = Play(story, "LumioseCity_BattleInstitute", "Receptionist", room, desk, bag, answers: new[] { 0 },
            turns: h => h.BattleTurns = turns / 3 + (battle++ < turns % 3 ? 1 : 0));
        Assert.Equal(new[] { "kalos_institute_ombeline", "kalos_institute_eustache", "kalos_institute_philippine" }.Select(id => $"battle {id} Won"),
            host.Log.Where(l => l.StartsWith("battle", StringComparison.Ordinal)));
        Assert.Equal(count, bag.GetQuantity(ItemDatabase.Get(item)!));
        Assert.True(story.Has("FLAG_DAILY_KALOS_BATTLE_INSTITUTE"));
        Assert.Equal(turns <= 9, story.Has("FLAG_KALOS_BATTLE_INSTITUTE_TOP"));
        // Once a day
        var again = Play(story, "LumioseCity_BattleInstitute", "Receptionist", room, desk, bag);
        Assert.DoesNotContain(again.Log, l => l.StartsWith("battle", StringComparison.Ordinal));
        // The examiners are Kalos's own, at 36 to 40
        foreach (string id in new[] { "kalos_institute_ombeline", "kalos_institute_eustache", "kalos_institute_philippine" })
        {
            var trainer = new Trainer { Id = id };
            TrainerDatabase.Fill(trainer, TrainerDatabase.Get(id, "kalos"));
            Assert.All(trainer.Party.Members, p => Assert.InRange(p.Level, 36, 40));
        }
    }

    [Fact]
    public void ATestLostIsTheDaysTestWithNoPrize()
    {
        var room = Rooms.Value("LumioseCity_BattleInstitute");
        var story = new StoryState();
        var bag = new Inventory();
        var host = Play(story, "LumioseCity_BattleInstitute", "Receptionist", room, room.Everyone.Single(n => n.Key == "receptionist"), bag,
            answers: new[] { 0 }, fight: BattleOutcome.Lost);
        Assert.Single(host.Log, l => l.StartsWith("battle", StringComparison.Ordinal));
        Assert.True(story.Has("FLAG_DAILY_KALOS_BATTLE_INSTITUTE"));
        Assert.Equal(0, bag.AllItems.Sum(i => i.Quantity));
    }

    [Fact]
    public void KizahashisOwnerServesTheChampionAndTheChapellerieSellsHats()
    {
        var room = Rooms.Value("LumioseCity_CafeKizahashi");
        var owner = room.Everyone.Single(n => n.Key == "owner");
        var before = Play(new StoryState(), "LumioseCity_CafeKizahashi", "Kizahashi", room, owner);
        Assert.Contains(before.Transcript, l => l.Text.Contains("belongs to a regular", StringComparison.Ordinal));
        var champion = new StoryState();
        champion.Set("KalosHallOfFame");
        var after = Play(champion, "LumioseCity_CafeKizahashi", "Kizahashi", room, owner);
        Assert.Contains(after.Transcript, l => l.Text.Contains("on the house", StringComparison.Ordinal));
        Assert.NotEmpty(ClothingDatabase.Stock("lumiose_hats"));
        Assert.All(ClothingDatabase.Stock("lumiose_hats"), g => Assert.Equal(ClothingSlot.Hat, g.Slot));
    }

    // ------------------------------------------------------------------ helpers

    private static HeadlessScriptHost Play(StoryState story, string file, string script, Map at, NPC? who = null, Inventory? bag = null,
        int[]? answers = null, BattleOutcome fight = BattleOutcome.Won, Action<HeadlessScriptHost>? turns = null, string? extra = null, (int X, int Y)? from = null)
    {
        var party = new Party();
        party.Add(new Pokemon(PokemonDatabase.Get("Greninja")!, 70));
        party.Add(new Pokemon(PokemonDatabase.Get("Talonflame")!, 70));
        if (extra != null) party.Add(new Pokemon(PokemonDatabase.Get(extra)!, 50));
        var host = new HeadlessScriptHost(story, party, bag ?? new Inventory())
        {
            Map = at, Region = RegionDatabase.Kalos, MapNamed = MapNamed, Money = 3000
        };
        if (from is { } tile) host.PlayerTile = tile;
        host.Fight = _ =>
        {
            turns?.Invoke(host);
            return fight;
        };
        foreach (int a in answers ?? Array.Empty<int>()) host.Answers.Enqueue(a);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find(script, file) ?? throw new Xunit.Sdk.XunitException($"{file}.{script} is missing"), who);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        return host;
    }
}
