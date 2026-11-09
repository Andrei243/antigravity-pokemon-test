using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// Plan 40 · GE6, legendaries met again: every legendary or mythical met by a script in Kanto, Johto, Hoenn and Unova
/// keeps a caught flag of its own, and its region's Hall of Fame brings it back unless that flag is set; its knocked-out
/// roamers are set loose again; and a save from before has the flags set from its Pokédex (story version 30).
/// </summary>
public class SecondChanceTests
{
    private static ScriptLibrary Scripts => ScriptLibrary.Default;

    /// <summary>Each region's Hall of Fame script that brings its legendaries back.</summary>
    private static readonly Dictionary<string, string> Again = new()
    {
        ["kanto"] = "pokemon_league_hall_of_fame.LegendsAgain",
        ["johto"] = "johto_pokemon_league_hall_of_fame.LegendsAgain",
        ["hoenn"] = "ever_grande_city_hall_of_fame.LegendsAgain",
        ["unova"] = "unova_pokemon_league_10.LegendsAgain",
    };

    /// <summary>The meeting places of the four regions, by their scripts' files.</summary>
    private static readonly Dictionary<string, string> RegionOfFile = new()
    {
        ["cerulean_cave_b1f"] = "kanto", ["seafoam_islands_b4f"] = "kanto", ["power_plant"] = "kanto", ["mt_ember_summit"] = "kanto",
        ["johto_cerulean_cave_b1f"] = "johto", ["johto_route_10"] = "johto", ["johto_seafoam_islands_b4f"] = "johto",
        ["mount_silver_cave_moltres_chamber"] = "johto", ["embedded_tower_groudon_room"] = "johto", ["embedded_tower_kyogre_room"] = "johto",
        ["embedded_tower_rayquaza_room"] = "johto", ["johto_route_25"] = "johto", ["ilex_forest"] = "johto",
        ["bell_tower_roof"] = "johto", ["whirl_islands_b3f_lugia_cave"] = "johto",
        ["desert_ruins"] = "hoenn", ["island_cave"] = "hoenn", ["ancient_tomb"] = "hoenn", ["sky_pillar_top"] = "hoenn",
        ["marine_cave_end"] = "hoenn", ["terra_cave_end"] = "hoenn", ["meteor_falls_stevens_cave"] = "hoenn",
        ["navel_rock_top"] = "hoenn", ["navel_rock_bottom"] = "hoenn", ["birth_island_exterior"] = "hoenn",
        ["faraway_island_interior"] = "hoenn", ["southern_island_interior"] = "hoenn",
        ["abundant_shrine"] = "unova", ["dragonspiral_tower_9"] = "unova", ["giant_chasm_6"] = "unova", ["iceberg_chamber"] = "unova",
        ["iron_chamber"] = "unova", ["rock_peak_chamber"] = "unova", ["liberty_garden"] = "unova", ["musical_theater"] = "unova",
        ["ns_castle_5"] = "unova", ["p2_laboratory_2"] = "unova", ["reversal_mountain_5"] = "unova", ["twist_mountain_8"] = "unova",
        ["unova_route_11"] = "unova", ["unova_route_13"] = "unova", ["unova_route_22"] = "unova", ["celestial_tower"] = "unova",
        ["unova_route_23"] = "unova",
    };

    /// <summary>
    /// Meetings of the four regions that are met once only, with the reason. Deoxys keeps no caught flag of its own (its
    /// <c>FLAG_BATTLED_DEOXYS</c> is the caught one), and the Hall of Fame clears its beaten flag outright.
    /// </summary>
    private static readonly Dictionary<string, string> OnceOnly = new()
    {
        ["pewter"] = "Steven's friend's Latias or Latios in Pewter City, a gift of a meeting once only",
    };

    private static bool IsLegend(string name) =>
        (PokemonDatabase.Get(name) ?? PokemonDatabase.SpeciesOfForm(name)) is { } s && (s.Legendary || s.Mythical);

    private static IEnumerable<Instruction> Flat(Script script) =>
        script.Code.SelectMany(i => i.Then == null ? new[] { i } : new[] { i, i.Then! });

    /// <summary>Every legendary meeting of a script, with the flags its script sets.</summary>
    private static IEnumerable<(Script Script, string Species)> Meetings() =>
        Scripts.All.SelectMany(s => Flat(s).Where(i => i.Op == Op.WildBattle && IsLegend(i.Name)).Select(i => (s, i.Name)));

    /// <summary>The script and every script it calls, its flags read and written.</summary>
    private static IEnumerable<Instruction> WithCalls(Script script, HashSet<string>? seen = null)
    {
        seen ??= new();
        if (!seen.Add(script.FullName)) yield break;
        foreach (var i in Flat(script))
        {
            yield return i;
            if (i.Op == Op.Call && Scripts.Find(i.Name, script.File) is { } called)
                foreach (var j in WithCalls(called, seen)) yield return j;
        }
    }

    [Fact]
    public void EveryMeetingOfTheFourRegionsSetsACaughtFlagTheirHallOfFameAsks()
    {
        var problems = new List<string>();
        foreach (var (script, species) in Meetings())
        {
            if (!RegionOfFile.TryGetValue(script.File, out var region))
                continue;
            var caught = Flat(script).Where(i => i.Op == Op.SetFlag && i.Name.Contains("CAUGHT") || i.Op == Op.SetFlag && i.Name == "FLAG_BATTLED_DEOXYS")
                .Select(i => i.Name).Distinct().ToList();
            if (caught.Count == 0) { problems.Add($"{script.FullName} {species}: no caught flag"); continue; }
            var again = Scripts.Find(Again[region]);
            if (again == null) { problems.Add($"{region}: no {Again[region]}"); continue; }
            var hall = Scripts.Find("OnEnter", Again[region].Split('.')[0])!;
            var asked = WithCalls(hall).Concat(WithCalls(again)).Where(i => i.Condition != null).Select(i => i.Condition!.Name).ToHashSet();
            // Deoxys's stone comes back whatever, its beaten flag cleared outright; Celebi by its beaten flag, which a catch clears
            if (species.StartsWith("Deoxys") || species == "Celebi" && asked.Contains("FLAG_JOHTO_CELEBI_BEATEN")) continue;
            if (!caught.Any(asked.Contains)) problems.Add($"{script.FullName} {species}: {string.Join(", ", caught)} not asked by {region}'s Hall of Fame");
        }
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void EveryPlaceListedHasItsMeeting()
    {
        var files = Meetings().Select(m => m.Script.File).ToHashSet();
        var stale = RegionOfFile.Keys.Concat(OnceOnly.Keys).Where(f => !files.Contains(f)).ToList();
        Assert.True(stale.Count == 0, "Listed with no legendary meeting:\n" + string.Join("\n", stale));
    }

    private static readonly Lazy<Dictionary<string, World>> Worlds = new(() => World.LoadAll().ToDictionary(w => w.Index.Region));

    /// <summary>A game of one region's maps with no screen, a Champion's team, where a place's arrival is played.</summary>
    private sealed class Game
    {
        private readonly Dictionary<string, Map> maps = new(StringComparer.OrdinalIgnoreCase);
        public StoryState Story { get; } = new();
        public Party Party { get; } = new();
        public Inventory Bag { get; } = new();
        public Map Map { get; private set; } = null!;
        public (int X, int Y) Tile { get; set; }
        public BattleOutcome Fight { get; set; } = BattleOutcome.Won;
        public HeadlessScriptHost Last { get; private set; } = null!;

        public Game(string region)
        {
            foreach (var m in Worlds.Value[region].BuildMaps()) maps[m.Name] = m;
            StoryMigration.BeginNewGame(Story, Scripts);
            Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 80, new Random(1)));
        }

        /// <summary>Comes to a place, its arrival played, as the game comes to it.</summary>
        public void Arrive(string name, int x, int y)
        {
            Map = maps[name];
            Map.ForgetForced();
            Tile = (x, y);
            if (Scripts.In(Map.ScriptFileAt(x, y) ?? "", "OnEnter") is { } arrival) Play(arrival, null);
            Map.ApplyPresence(Story.Has);
            Map.ApplyDoors(Story.Has);
            Wandering.SendHome(Map);
        }

        public NPC? Present(string key) => Map.NPCs.FirstOrDefault(n => n.Key == key);

        public HeadlessScriptHost Talk(string key)
        {
            var npc = Map.NPCs.First(n => n.Key == key);
            Tile = (npc.GridX, npc.GridY + 1);
            return Play(Scripts.Find(FieldScripts.For(npc)!, npc.ScriptFile ?? Map.ScriptFileAt(npc.GridX, npc.GridY))!, npc);
        }

        public HeadlessScriptHost Read(int x, int y)
        {
            Tile = (x, y + 1);
            return Play(Scripts.Find(Map.SignScripts[(x, y)], Map.ScriptFileAt(x, y))!, null, new[] { Map.GetSignboardAt(x, y)! });
        }

        /// <summary>The region's Hall of Fame entered again, as far as its legendaries go.</summary>
        public HeadlessScriptHost HallOfFame(string region) => Play(Scripts.Find(Again[region])!, null);

        private HeadlessScriptHost Play(Script script, NPC? subject, IReadOnlyList<string>? own = null)
        {
            var host = new HeadlessScriptHost(Story, Party, Bag)
            {
                Map = Map,
                PlayerTile = Tile,
                MapNamed = name => maps.GetValueOrDefault(name),
                Fight = _ => Fight,
                NeedsPokemon = true,
                NationalDex = true,
            };
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(script, subject, own);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            Map.ApplyPresence(Story.Has);
            Tile = host.PlayerTile;
            return Last = host;
        }
    }

    [Theory]
    [InlineData(BattleOutcome.Won, true)]
    [InlineData(BattleOutcome.Caught, false)]
    public void AMewtwoKnockedOutInCeruleanCaveIsBackAfterKantosHallOfFameAndOneCaughtNeverIs(BattleOutcome outcome, bool back)
    {
        var game = new Game(RegionDatabase.Kanto) { Fight = outcome };
        game.Story.Set("KantoHallOfFame");
        game.Arrive("CeruleanCave_B1F", 7, 14);
        Assert.NotNull(game.Present("mewtwo"));
        game.Talk("mewtwo");
        Assert.Contains(game.Last.Log, l => l.StartsWith("wildbattle Mewtwo 70"));
        Assert.True(game.Story.Has("FLAG_FOUGHT_MEWTWO"));
        Assert.Equal(!back, game.Story.Has("FLAG_KANTO_CAUGHT_MEWTWO"));
        game.Arrive("CeruleanCave_B1F", 7, 14);
        Assert.Null(game.Present("mewtwo"));

        game.HallOfFame("kanto");
        game.Arrive("CeruleanCave_B1F", 7, 14);
        Assert.Equal(back, game.Present("mewtwo") != null);
    }

    [Theory]
    [InlineData(BattleOutcome.Won, true)]
    [InlineData(BattleOutcome.Caught, false)]
    public void TornadusStillWaitsWhenLandorusIsBackAfterUnovasHallOfFame(BattleOutcome outcome, bool back)
    {
        var game = new Game(RegionDatabase.Unova) { Fight = outcome };
        game.Story.Set("UnovaHallOfFame");
        game.Arrive("AbundantShrine", 22, 16);
        game.Talk("landorus");
        Assert.True(game.Story.Has("FLAG_UNOVA_MET_LANDORUS"));
        Assert.Null(game.Present("landorus"));

        game.HallOfFame("unova");
        game.Arrive("AbundantShrine", 22, 16);
        Assert.Equal(back, game.Present("landorus") != null);
        // Its two fellows wait on its having been met, which the Hall of Fame never forgets
        Assert.NotNull(game.Present("tornadus"));
        Assert.NotNull(game.Present("thundurus"));
    }

    [Fact]
    public void CelebiBeatenComesBackToTheShrineWithNoGsBall()
    {
        var game = new Game(RegionDatabase.Johto);
        game.Story.Set("JohtoHallOfFame");
        game.Story.Set("FLAG_JOHTO_GS_BALL_ON_SHRINE");
        game.Arrive("IlexForest", 16, 56);
        game.Read(16, 55);
        Assert.Contains("wildbattle Celebi 50 Won", game.Last.Log);
        Assert.True(game.Story.Has("FLAG_JOHTO_CELEBI_BEATEN"));
        game.Read(16, 55);
        Assert.DoesNotContain(game.Last.Log, l => l.StartsWith("wildbattle"));

        game.HallOfFame("johto");
        game.Fight = BattleOutcome.Caught;
        game.Read(16, 55);
        Assert.Contains("wildbattle Celebi 50 Caught", game.Last.Log);
        Assert.True(game.Story.Has("FLAG_JOHTO_CAUGHT_CELEBI"));

        game.HallOfFame("johto");
        game.Read(16, 55);
        Assert.DoesNotContain(game.Last.Log, l => l.StartsWith("wildbattle"));
    }

    [Fact]
    public void TheEmbeddedTowersGroudonComesBackWithoutTheTowersWaysChanging()
    {
        var game = new Game(RegionDatabase.Johto);
        game.Story.Set("JohtoHallOfFame");
        game.Story.Set("FLAG_JOHTO_GOT_ORBS");
        const string groudon = "d52r0101_tsure_poke_static_groudon";
        game.Arrive("EmbeddedTower_GroudonRoom", 16, 28);
        game.Talk(groudon);
        game.Arrive("Johto", 87, 390);
        Assert.Equal("EmbeddedTower_KyogreRoom", game.Map.GetWarpAt(87, 389)!.TargetMap);

        game.HallOfFame("johto");
        // The fought flag, which the hole's way goes by, stays; the room shows Groudon by a flag of its own
        Assert.True(game.Story.Has("FLAG_JOHTO_FOUGHT_GROUDON"));
        game.Arrive("Johto", 87, 390);
        Assert.Equal("EmbeddedTower_KyogreRoom", game.Map.GetWarpAt(87, 389)!.TargetMap);
        game.Arrive("EmbeddedTower_GroudonRoom", 16, 28);
        Assert.NotNull(game.Present(groudon));
        game.Fight = BattleOutcome.Caught;
        game.Talk(groudon);
        game.HallOfFame("johto");
        game.Arrive("EmbeddedTower_GroudonRoom", 16, 28);
        Assert.Null(game.Present(groudon));
    }

    [Fact]
    public void AKnockedOutRoamerOfJohtoRoamsAgain()
    {
        var story = new StoryState();
        story.SetVar("VAR_ROAMING_RAIKOU_STATE", Roamers.Defeated);
        story.SetVar("VAR_ROAMING_ENTEI_STATE", Roamers.Captured);
        var host = new HeadlessScriptHost(story);
        var runner = new ScriptRunner(Scripts, host);
        runner.Start(Scripts.Find(Again["johto"])!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        int raikou = Roamers.SlotOf("Raikou")!.Value, entei = Roamers.SlotOf("Entei")!.Value;
        Assert.True(host.Encounters.Roamers[raikou].Active);
        Assert.Equal(Roamers.Roaming, story.Var("VAR_ROAMING_RAIKOU_STATE"));
        Assert.False(host.Encounters.Roamers[entei].Active);
        Assert.Equal(Roamers.Captured, story.Var("VAR_ROAMING_ENTEI_STATE"));
    }

    [Fact]
    public void AnOlderSaveThatCaughtALegendaryMeetsItNoMore()
    {
        var story = new StoryState();
        story.Set("FLAG_FOUGHT_MEWTWO");
        story.Set("FLAG_FOUGHT_ZAPDOS");
        story.Set("FLAG_UNOVA_FOUGHT_LANDORUS");
        story.Set("FLAG_JOHTO_CELEBI_MET");
        var dex = new Pokedex();
        dex.RegisterCaught(PokemonDatabase.Get("Mewtwo")!.DexNumber);
        StoryMigration.Upgrade(story, 29, Array.Empty<Pokemon>(), Scripts, null, dex);
        Assert.True(story.Has("FLAG_KANTO_CAUGHT_MEWTWO"));
        Assert.False(story.Has("FLAG_KANTO_CAUGHT_ZAPDOS"));
        // ...only a meeting the save has had
        Assert.False(story.Has("FLAG_JOHTO_CAUGHT_MEWTWO"));
        Assert.True(story.Has("FLAG_UNOVA_MET_LANDORUS"));
        Assert.True(story.Has("FLAG_JOHTO_CELEBI_BEATEN"));

        // With no Pokédex handed over, the Pokémon the save owns are read as caught
        var owned = new StoryState();
        owned.Set("FLAG_FOUGHT_MEWTWO");
        StoryMigration.Upgrade(owned, 29, new[] { new Pokemon(PokemonDatabase.Get("Mewtwo")!, 70) }, Scripts);
        Assert.True(owned.Has("FLAG_KANTO_CAUGHT_MEWTWO"));
    }

    [Fact]
    public void CaughtAsksThePokedex()
    {
        var library = ScriptLibrary.FromSources(("test", """
            script Test
              if caught "Mewtwo" setflag FLAG_YES
              if not caught "Mew" setflag FLAG_NO
              end
            """));
        var story = new StoryState();
        var dex = new Pokedex();
        dex.RegisterCaught(PokemonDatabase.Get("Mewtwo")!.DexNumber);
        var host = new HeadlessScriptHost(story) { Pokedex = dex };
        var runner = new ScriptRunner(library, host);
        runner.Start(library.Find("test.Test")!, null);
        runner.RunToEnd();
        Assert.True(story.Has("FLAG_YES"));
        Assert.True(story.Has("FLAG_NO"));
        // ...and no Pokédex, no catch
        var none = new StoryState();
        var bare = new ScriptRunner(library, new HeadlessScriptHost(none));
        bare.Start(library.Find("test.Test")!, null);
        bare.RunToEnd();
        Assert.False(none.Has("FLAG_YES"));
    }
}
