using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumTests;

/// <summary>
/// Alola's second part (plan 23 · Z5): the rest of Melemele (Hau'oli's east end and marina, Routes 2 and 3, Melemele
/// Meadow, Verdant Cavern and Ten Carat Hill), the Ride Pager's grants, the ferry to Akala and Akala as far as Brooklet
/// Hill, and the story from Hau'oli City to Lana's trial played headless. Floors, not counts, wherever a later session
/// draws more.
/// </summary>
[Collection("Shared clocks and dice")]
public class AlolaIslandsTests
{
    private static readonly Lazy<World> LoadedWorld = new(() => World.LoadAll().Single(w => w.Index.Region == "Alola"));
    private static readonly Lazy<Dictionary<string, Map>> BuiltMaps = new(() => LoadedWorld.Value.BuildMaps().ToDictionary(m => m.Name));
    private static readonly Lazy<Func<string, Map>> Rooms = new(MapDatabase.Fresh);
    private static Map MapNamed(string name) => BuiltMaps.Value.TryGetValue(name, out var map) ? map : Rooms.Value(name);
    private static readonly Lazy<Dictionary<Map, HashSet<(int X, int Y)>>> Walked =
        new(() => WorldWalk.From(MapNamed, RegionDatabase.Get(RegionDatabase.Alola)!.Start!));

    private static readonly string[] Melemele = { "hauoli_city", "alola_route_2", "alola_route_3", "melemele_meadow" };
    private static readonly string[] Akala = { "heahea_city", "akala_route_4", "paniola_town", "paniola_ranch", "akala_route_5", "brooklet_hill" };

    [Fact]
    public void EveryNewPlaceIsReachedFromTheNewHomeOnFootByTheFerryAndOverTheWater()
    {
        var walked = Walked.Value;
        foreach (string key in Melemele)
            Assert.True(walked.Any(p => p.Key.Name == "Melemele" && p.Value.Any(t => p.Key.AreaAt(t.X, t.Y)?.Key == key)), $"{key} is never reached");
        foreach (string key in Akala)
            Assert.True(walked.Any(p => p.Key.Name == "Akala" && p.Value.Any(t => p.Key.AreaAt(t.X, t.Y)?.Key == key)), $"{key} is never reached");
        Assert.Contains(walked.Keys, m => m.Name == "TenCaratHill");
        Assert.Contains(walked.Keys, m => m.Name == "VerdantCavern");
        // The totem's den at the top of Verdant Cavern, and Brooklet Hill's far shore, which only a surfer reaches
        var cavern = walked.Single(p => p.Key.Name == "VerdantCavern").Value;
        Assert.Contains((29, 9), cavern);
        var akala = walked.Single(p => p.Key.Name == "Akala").Value;
        Assert.Contains((89, 97), akala);
    }

    [Fact]
    public void TheSpawnsOfBothIslandsStandBeforeTheirPokemonCentersDoors()
    {
        var region = RegionDatabase.Get(RegionDatabase.Alola)!;
        Assert.True(region.File!.Spawns.Count >= 6);
        foreach (var spawn in region.File.Spawns)
        {
            var map = MapNamed(spawn.Map ?? "Melemele");
            Assert.True(map.IsWalkable(spawn.X, spawn.Y), $"spawn {spawn.Id} stands on a blocked tile");
            Assert.Equal(spawn.Room, map.GetWarpAt(spawn.X, spawn.Y - 1)?.TargetMap);
        }
    }

    [Fact]
    public void TheRidePagerGrantsItsMovesInAlolaAloneAndWhateverTheOptionsSay()
    {
        var story = new StoryState();
        Assert.Empty(RidePager.Granted(story, RegionDatabase.Alola));
        story.Set("FLAG_ALOLA_RIDE_TAUROS");
        Assert.Equal(new[] { FieldMove.RockSmash }, RidePager.Granted(story, RegionDatabase.Alola));
        Assert.Empty(RidePager.Granted(story, RegionDatabase.Sinnoh));
        // With the options' helpers off, the ride is still granted; the helper who does it is the ride's species
        var granted = FieldHelp.Granted(RulesDefault.Off, RulesPreset.Modern, story, new Inventory(), RegionDatabase.Alola);
        Assert.Equal(new[] { FieldMove.RockSmash }, granted);
        Assert.Equal("Tauros", FieldHelp.HelperFor(FieldMove.RockSmash, story, RegionDatabase.Alola).Species.Name);
        Assert.Equal("Bidoof", FieldHelp.HelperFor(FieldMove.RockSmash, story, RegionDatabase.Sinnoh).Species.Name);
        Assert.Empty(FieldHelp.Granted(RulesDefault.Off, RulesPreset.Modern, story, new Inventory(), RegionDatabase.Sinnoh));
        story.Set("FLAG_ALOLA_RIDE_LAPRAS");
        Assert.Equal(new[] { FieldMove.Surf, FieldMove.RockSmash }, FieldHelp.Granted(RulesDefault.Off, RulesPreset.Modern, story, new Inventory(), RegionDatabase.Alola));
        Assert.Equal(FieldMoves.Surf, FieldHelp.Moves(RidePager.Granted(story, RegionDatabase.Alola)) & FieldMoves.Surf);
    }

    [Fact]
    public void TaurosChargesTheRockBeforeTenCaratHillWithNoPokemonThatKnowsRockSmash()
    {
        var map = MapNamed("Melemele");
        var rock = map.Everyone.Single(n => n.Key == "rock_ten_carat");
        Assert.True(rock.IsThing);

        (HeadlessScriptHost Host, bool Gone) Charge(bool tauros)
        {
            var host = new HeadlessScriptHost { Map = map, PlayerTile = (rock.GridX + 1, rock.GridY), PlayerFacing = Direction.Left, Region = RegionDatabase.Alola };
            host.Party.Add(new Pokemon(PokemonDatabase.Get("Rowlet")!, 15));
            if (tauros) host.Story.Set("FLAG_ALOLA_RIDE_TAUROS");
            host.Answers.Enqueue(0);
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find(FieldScripts.For(rock)!)!, rock);
            runner.RunToEnd();
            return (host, host.Story.Has(rock.HiddenBy!));
        }

        var (without, goneWithout) = Charge(tauros: false);
        Assert.False(goneWithout);
        Assert.DoesNotContain(without.Log, l => l.StartsWith("usemove"));

        var (with, gone) = Charge(tauros: true);
        Assert.True(gone);
        Assert.Contains(with.Log, l => l.StartsWith("usemove Rock Smash Tauros"));
        Assert.Contains(with.Transcript, t => t.Text.StartsWith("Tauros came at the Ride Pager's call"));
    }

    [Fact]
    public void TheStoryRunsFromHauoliToLanasTrialHeadless()
    {
        var story = new StoryState();
        var party = new Party();
        var bag = new Inventory();
        var library = ScriptLibrary.Default;
        // Someone spoken to is the script's own person (its self), found by their key among everyone the place has
        void Play(string file, string script, Map map, string? who = null)
        {
            var host = new HeadlessScriptHost(story, party, bag) { Map = map, Region = RegionDatabase.Alola };
            var runner = new ScriptRunner(library, host);
            var subject = who == null ? null : map.Everyone.Single(n => n.Key == who && (map.AreaAt(n.GridX, n.GridY)?.Key ?? file) == file);
            runner.Start(library.Find(script, file)!, subject);
            runner.RunToEnd();
        }
        string? Next() => Objectives.Current(story, RegionDatabase.Alola)?.Name;
        var melemele = MapNamed("Melemele");
        var akala = MapNamed("Akala");
        var cavern = MapNamed("VerdantCavern");

        Play("common", "AlolaNewGame", MapNamed("AlolaRoute1_PlayersHouse"));
        party.Add(new Pokemon(PokemonDatabase.Get("Litten")!, 15));
        story.SetVar("VAR_ALOLA_STORY", 7);
        story.ChooseStarter("Litten");
        Assert.Equal("alolaMarina", Next());
        Play("hauoli_city", "Marina", melemele);
        Assert.Equal(8, story.Var("VAR_ALOLA_STORY"));
        Assert.Equal("alolaFestival", Next());
        Play("iki_town", "Festival", melemele);
        Assert.Equal(9, story.Var("VAR_ALOLA_STORY"));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Z-Ring")!));
        Assert.True(story.Has("FLAG_ALOLA_FESTIVAL_DONE"));
        // Ilima at the cavern lets the trial begin; the totem waits until the three dens are clear
        Play("alola_route_2", "Ilima", melemele, "ilima");
        Assert.True(story.Has("FLAG_ALOLA_VERDANT_TRIAL"));
        Play("verdant_cavern", "Totem", cavern, "totem");
        Assert.Equal(9, story.Var("VAR_ALOLA_STORY"));
        Play("verdant_cavern", "Den1", cavern, "den_1");
        Play("verdant_cavern", "Den2", cavern, "den_2");
        Play("verdant_cavern", "Den3", cavern, "den_3");
        Play("verdant_cavern", "Totem", cavern, "totem");
        Assert.Equal(10, story.Var("VAR_ALOLA_STORY"));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Normalium Z")!));
        Play("melemele_meadow", "Nebby", melemele);
        Assert.Equal(11, story.Var("VAR_ALOLA_STORY"));
        Play("alola_route_3", "HauOnRoute3", melemele);
        Assert.Equal(12, story.Var("VAR_ALOLA_STORY"));
        Assert.Equal("alolaGrandTrial", Next());
        Play("iki_town", "GrandTrial", melemele);
        Assert.Equal(13, story.Var("VAR_ALOLA_STORY"));
        Assert.True(story.HasBadge(RegionDatabase.Alola, "Melemele"));
        Assert.True(story.Has("FLAG_ALOLA_RIDE_TAUROS"));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Fightinium Z")!));
        Play("hauoli_city", "SailToAkala", melemele);
        Assert.Equal(14, story.Var("VAR_ALOLA_STORY"));
        Assert.Equal("alolaPaniola", Next());
        Play("paniola_town", "HauInPaniola", akala);
        Assert.Equal(15, story.Var("VAR_ALOLA_STORY"));
        Play("paniola_ranch", "Mallow", akala);
        Assert.Equal(16, story.Var("VAR_ALOLA_STORY"));
        Assert.True(story.Has(RidePager.StoutlandFlag));
        Play("akala_route_5", "Gladion", akala);
        Assert.Equal(17, story.Var("VAR_ALOLA_STORY"));
        Play("brooklet_hill", "Lana", akala, "lana");
        Assert.True(story.Has("FLAG_ALOLA_RIDE_LAPRAS"));
        Assert.Contains(FieldMove.Surf, RidePager.Granted(story, RegionDatabase.Alola));
        Play("brooklet_hill", "Splash1", akala, "splash");
        Play("brooklet_hill", "Splash2", akala, "splash_2");
        Play("brooklet_hill", "Totem", akala, "totem");
        Assert.Equal(18, story.Var("VAR_ALOLA_STORY"));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Waterium Z")!));
        Assert.Equal("alolaFree", Next());
    }
}
