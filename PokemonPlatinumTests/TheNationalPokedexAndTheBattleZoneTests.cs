using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;

namespace PokemonPlatinumTests;

/// <summary>
/// The first part of the time after the Hall of Fame (plan 02 · S15, part a) played through with no screen, in the
/// order a Champion meets it: the professor's rating of the Pokédex and Professor Oak's visit to the lab, which
/// upgrades it and brings the Poké Radar; who asks for the National Pokédex where they asked for the Hall of Fame;
/// Pal Park's doors, Professor Oak in its lobby and in Eterna City, Byron on Iron Island; the assistant's lesson with
/// the Poké Radar; the ship from Snowpoint City with Cynthia on the quay; the rival on the Fight Area's pier, the tag
/// battle before the Battle Frontier's gate, Palmer and Buck, and the two blockades; Route 227; the Survival Area;
/// Marley in Victory Road's back room; Lucian in Canalave's library and Looker at the Game Corner; and saves from
/// before. Each scene is started as the game starts it, on maps of its own (<see cref="OpeningTests.Game"/>).
/// </summary>
public class TheNationalPokedexAndTheBattleZoneTests
{
    private static readonly ScriptLibrary Scripts = ScriptLibrary.Default;
    private const string Completed = "FLAG_GAME_COMPLETED", Hall = "PokemonLeagueHallOfFame";
    private const string FightArea = "VAR_FIGHT_AREA_STATE", Blockade = "FLAG_HIDE_FIGHT_AREA_BLOCKADE";
    private const string MarleyState = "VAR_VICTORY_ROAD_1F_ROOM_2_FOLLOWER_MARLEY_STATE";

    /// <summary>What a new game hides until the time after the Hall of Fame (common.ChapterTwelve), part a's and the later parts'.</summary>
    private static readonly string[] HiddenFromTheStart =
    {
        "FLAG_HIDE_FIGHT_AREA_PALMER", "FLAG_HIDE_SURVIVAL_AREA_BUCK", "FLAG_HIDE_SURVIVAL_AREA_RIVAL", "FLAG_HIDE_ROUTE_224_PROF_OAK", "FLAG_HIDE_ROUTE_224_MARLEY",
        "FLAG_HIDE_IRON_ISLAND_HOUSE_BYRON", "FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_LOOKER", "FLAG_HIDE_STARK_MOUNTAIN_OUTSIDE_BUCK", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_2_BUCK",
        "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_LOOKER", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_POLICEMEN", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_CROAGUNK", "FLAG_HIDE_STARK_MOUNTAIN_ROOM_3_HEATRAN",
        "FLAG_HIDE_BATTLEGROUND_BUCK", "FLAG_HIDE_CANALAVE_CITY_SAILOR_ELDRITCH_HOUSE_SAILOR_ELDRITCH", "FLAG_HIDE_ROTOMS_ROOM_PROF_ROWAN",
        "FLAG_OBTAINED_FULLMOON_ISLAND_FOREST_LUNAR_WING"
    };

    /// <summary>A game with a team and the eight Badges, the first chapter's rooms as that chapter leaves them.</summary>
    private static OpeningTests.Game Challenger(int starter = 0, BattleOutcome fight = BattleOutcome.Won, PlayerLook look = PlayerLook.Boy)
    {
        var game = new OpeningTests.Game(starter, fight) { Look = look };
        game.Story.ChooseStarter(StoryState.Starters[starter]);
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Garchomp")!, 62));
        game.Party.Add(new Pokemon(PokemonDatabase.Get("Staraptor")!, 58));
        game.Story.SetBadges(0xFF);
        game.Story.SetVar("VAR_PLAYER_HOUSE_STATE", 6);
        // The lab as the tenth chapter leaves it: the professor behind his desk, the assistant gone out
        game.Story.SetVar("VAR_SANDGEM_TOWN_STATE", 2);
        game.Story.SetVar("VAR_SANDGEM_TOWN_LAB_STATE", 4);
        game.Story.Set(StoryState.PokedexFlag);
        game.Story.Set("FLAG_HIDE_SANDGEM_TOWN_LAB_COUNTERPART");
        return game;
    }

    /// <summary>A Champion, as the Hall of Fame's own scene makes one (plan 02 · S14): home again, with everything a victory sets.</summary>
    private static OpeningTests.Game Champion(int starter = 0, BattleOutcome fight = BattleOutcome.Won, PlayerLook look = PlayerLook.Boy)
    {
        var game = Challenger(starter, fight, look);
        game.Facing = Direction.Up;
        game.Arrive(Hall, 8, 15);
        Assert.True(game.Story.Has(Completed));
        Assert.Equal("PlayerHouse2F", game.Map.Name);
        return game;
    }

    private static Script Find(string name, string file) => Scripts.Find(name, file) ?? throw new InvalidOperationException($"no {file}.{name}");

    private static ItemData Item(string name) => ItemDatabase.Get(name)!;

    private static HeadlessScriptHost Run(OpeningTests.Game game, string place, string lines) =>
        game.Play(ScriptParser.Parse(place, "script Test\n" + lines)[0]);

    private static string? AreaKey(Map map, int x, int y) => map.AreaAt(x, y)?.Key;

    /// <summary>Speaks to someone of a place from the side the player faces them from.</summary>
    private static HeadlessScriptHost TalkFrom(OpeningTests.Game game, string key, Direction facing, string? place = null)
    {
        var npc = game.Present(key, place) ?? throw new InvalidOperationException($"{key} isn't there");
        var (dx, dy) = FieldMovement.Delta(facing);
        game.Tile = (npc.GridX - dx, npc.GridY - dy);
        game.Facing = facing;
        Assert.False(game.Map.IsSolid(game.Tile.X, game.Tile.Y), $"nobody can stand at {game.Tile} to speak to {key}");
        return game.Play(Find(FieldScripts.For(npc)!, npc.ScriptFile ?? place ?? game.Map.Name), npc);
    }

    /// <summary>A step onto a tile of a trigger, which must start its script now.</summary>
    private static HeadlessScriptHost StepOnto(OpeningTests.Game game, int x, int y, string script)
    {
        game.Tile = (x, y);
        var trigger = FieldScripts.TriggerAt(game.Map, x, y, game.Story);
        Assert.NotNull(trigger);
        Assert.Equal(script, trigger!.Script);
        return game.Play(Find(trigger.Script, trigger.ScriptFile ?? game.Map.Name));
    }

    /// <summary>The professor spoken to from before him, on the tile Professor Oak's visit brings the player back to.</summary>
    private static HeadlessScriptHost AskTheProfessor(OpeningTests.Game game, params int[] answers)
    {
        if (game.Map.Name != "RowanLab") game.Arrive("RowanLab", 7, 15);
        var rowan = game.Present("prof_rowan")!;
        Assert.Equal((7, 5), (rowan.GridX, rowan.GridY));
        game.Tile = (7, 6);
        game.Facing = Direction.Up;
        foreach (int answer in answers) game.Answers.Enqueue(answer);
        return game.Play(Find("Rowan", "RowanLab"), rowan);
    }

    // ------------------------------------------------------------------ before any of it

    [Fact]
    public void ANewGameHidesWhoTheTimeAfterTheHallOfFameBringsOn()
    {
        var game = new OpeningTests.Game();
        Assert.True(StoryState.CurrentVersion >= 18);
        Assert.All(HiddenFromTheStart, flag => Assert.True(game.Story.Has(flag), flag));
        // Those the original leaves standing from the start, whom nobody comes to before the Hall of Fame
        foreach (string flag in new[] { "FLAG_HIDE_FIGHT_AREA_RIVAL", "FLAG_HIDE_FIGHT_AREA_BUCK", "FLAG_HIDE_FIGHT_AREA_BATTLERS_AND_SPECTATORS", Blockade,
                     "FLAG_HIDE_ROUTE_227_RIVAL", "FLAG_HIDE_ROUTE_227_CRASHER_WAKE", "FLAG_HIDE_ROUTE_227_BUCK", "FLAG_HIDE_VICTORY_ROAD_1F_ROOM_2_MARLEY", "FLAG_HIDE_PAL_PARK_LOBBY_PROF_OAK" })
            Assert.False(game.Story.Has(flag), flag);

        var sinnoh = game.Maps["Sinnoh"];
        NPC Of(string place, string key) => Assert.Single(sinnoh.Everyone, n => n.Key == key && AreaKey(sinnoh, n.GridX, n.GridY) == place);
        // The Fight Area: the rival on the pier, the two before the gate with their crowd, Palmer in the gate's door, Buck by the Mart
        Assert.Equal((628, 434, "FLAG_HIDE_FIGHT_AREA_RIVAL", "Rival"), (Of("fight_area", "rival").GridX, Of("fight_area", "rival").GridY, Of("fight_area", "rival").HiddenBy, Of("fight_area", "rival").Script));
        Assert.Equal((654, 424, "elite_four_flint_fight_area"), (Of("fight_area", "flint").GridX, Of("fight_area", "flint").GridY, Of("fight_area", "flint").TrainerData!.Id));
        // (Volkner's team is named by the script, as a Gym Leader's is; neither looks out for anybody)
        Assert.Equal((655, 424, "Volkner"), (Of("fight_area", "volkner").GridX, Of("fight_area", "volkner").GridY, Of("fight_area", "volkner").Script));
        Assert.Null(Of("fight_area", "volkner").TrainerData);
        Assert.Equal(0, Of("fight_area", "flint").TrainerData!.SightRange);
        Assert.NotNull(TrainerDatabase.Get("leader_volkner_fight_area"));
        Assert.Equal((655, 423, "FLAG_HIDE_FIGHT_AREA_PALMER"), (Of("fight_area", "palmer").GridX, Of("fight_area", "palmer").GridY, Of("fight_area", "palmer").HiddenBy));
        Assert.Equal((657, 426, "FLAG_HIDE_FIGHT_AREA_BUCK"), (Of("fight_area", "buck").GridX, Of("fight_area", "buck").GridY, Of("fight_area", "buck").HiddenBy));
        foreach (string onlooker in new[] { "ace_trainer_m_3", "ace_trainer_f_2", "black_belt_3", "battle_girl_2", "sailor_3" })
            Assert.Equal("FLAG_HIDE_FIGHT_AREA_BATTLERS_AND_SPECTATORS", Of("fight_area", onlooker).HiddenBy);
        // The two blockades, side by side across their roads
        Assert.Equal(new[] { (629, 429), (630, 429) }, new[] { "ace_trainer_m_2", "black_belt_2" }.Select(k => (Of("fight_area", k).GridX, Of("fight_area", k).GridY)));
        Assert.Equal(new[] { (682, 430), (682, 431) }, new[] { "fisherman", "rich_boy" }.Select(k => (Of("route_230", k).GridX, Of("route_230", k).GridY)));
        Assert.All(new[] { Of("fight_area", "ace_trainer_m_2"), Of("fight_area", "black_belt_2"), Of("route_230", "fisherman"), Of("route_230", "rich_boy") }, n => Assert.Equal(Blockade, n.HiddenBy));

        // Route 227 and its two triggers; the Survival Area's two, hidden; Cynthia off Snowpoint's quay
        Assert.Equal((739, 305), (Of("route_227", "rival").GridX, Of("route_227", "rival").GridY));
        Assert.Equal((738, 305), (Of("route_227", "crasher_wake").GridX, Of("route_227", "crasher_wake").GridY));
        Assert.Equal((758, 275), (Of("route_227", "buck").GridX, Of("route_227", "buck").GridY));
        Assert.Contains(sinnoh.Triggers, t => (t.X, t.Y, t.Width, t.Depth, t.Script, t.Variable, t.Value) == (738, 306, 2, 1, "WakeAndRival", "VAR_ROUTE_227_WAKE_RIVAL_STATE", 0));
        Assert.Contains(sinnoh.Triggers, t => (t.X, t.Y, t.Script, t.Variable, t.Value) == (758, 277, "BuckAsks", "VAR_ROUTE_227_BUCK_STATE", 0));
        Assert.Equal("FLAG_HIDE_SURVIVAL_AREA_BUCK", Of("survival_area", "buck").HiddenBy);
        Assert.Equal("FLAG_HIDE_SURVIVAL_AREA_RIVAL", Of("survival_area", "rival").HiddenBy);
        Assert.Equal("FLAG_HIDE_SNOWPOINT_CITY_CYNTHIA", Of("snowpoint_city", "cynthia").HiddenBy);
        Assert.True(game.Story.Has("FLAG_HIDE_SNOWPOINT_CITY_CYNTHIA"));

        // Marley in Victory Road's back room, with her three coordinate events
        var room = game.Maps["VictoryRoad1FRoom2"];
        var marley = Assert.Single(room.Everyone, n => n.Key == "marley");
        Assert.Equal((7, 58, Direction.Right, "FLAG_HIDE_VICTORY_ROAD_1F_ROOM_2_MARLEY"), (marley.GridX, marley.GridY, marley.Facing, marley.HiddenBy));
        Assert.Equal(new[] { ("MarleyJoins", 5, 55, 1, 6, 0), ("MarleyParts", 51, 19, 4, 1, 1), ("PlayerTurnsBack", 4, 55, 1, 6, 1) },
            room.Triggers.Select(t => (t.Script, t.X, t.Y, t.Width, t.Depth, t.Value)).OrderBy(t => t.Script));
        Assert.All(room.Triggers, t => Assert.Equal(MarleyState, t.Variable));

        // The rooms' guests: Professor Oak three times, Byron, Lucian
        foreach (var (name, key, x, y, flag) in new[]
                 {
                     // (in the lab he comes in on the original's tile, a step inside the mat: the tile whoever comes in stands on is nobody's)
                     ("RowanLab", "prof_oak", 7, 14, "FLAG_HIDE_SANDGEM_TOWN_LAB_PROF_OAK"), ("PalParkLobby", "prof_oak", 7, 15, "FLAG_HIDE_PAL_PARK_LOBBY_PROF_OAK"),
                     ("EternaCitySouthHouse", "prof_oak", 4, 5, "FLAG_HIDE_ETERNA_CITY_SOUTH_HOUSE_PROF_OAK"), ("IronIslandHouse", "byron", 5, 5, "FLAG_HIDE_IRON_ISLAND_HOUSE_BYRON"),
                     ("CanalaveLibrary1F", "lucian", 16, 5, "FLAG_HIDE_CANALAVE_LIBRARY_1F_LUCIAN")
                 })
        {
            var guest = Assert.Single(game.Maps[name].Everyone, n => n.Key == key);
            Assert.Equal((x, y, flag), (guest.GridX, guest.GridY, guest.HiddenBy));
        }
        foreach (string flag in new[] { "FLAG_HIDE_SANDGEM_TOWN_LAB_PROF_OAK", "FLAG_HIDE_ETERNA_CITY_SOUTH_HOUSE_PROF_OAK", "FLAG_HIDE_CANALAVE_LIBRARY_1F_LUCIAN" })
            Assert.True(game.Story.Has(flag), flag);
        Assert.False(game.NationalDex);

        // The chapter's script is the last link of the chain a new game runs, and may only set things up
        Assert.Contains(Find("ChapterEleven", ScriptLibrary.Common).Everything(), i => i.Op == Op.Call && i.Name == "ChapterTwelve");
        Assert.All(Find("ChapterTwelve", ScriptLibrary.Common).Everything(), i => Assert.Contains(i.Op, new[] { Op.SetFlag, Op.ClearFlag, Op.SetVar, Op.If, Op.Return }));
        Assert.Equal(ScriptLibrary.ChapterTwelve, Find("ChapterTwelve", ScriptLibrary.Common).FullName);
    }

    // ------------------------------------------------------------------ the lab: the rating, Professor Oak, the Poké Radar

    [Fact]
    public void TheProfessorRatesThePokedexByHowManyOfSinnohsPokemonItHasSeen()
    {
        var game = Challenger();
        // The original's thirteen steps, each with words of its own
        var said = new List<string>();
        foreach (int seen in new[] { 15, 30, 45, 60, 80, 100, 120, 140, 160, 180, 200, 209, 210 })
        {
            game.Seen = seen;
            var rated = AskTheProfessor(game);
            Assert.Contains(rated.Transcript, l => l.Speaker == "Prof. Rowan" && l.Text.Contains($"{seen} kinds"));
            said.Add(rated.Transcript[^1].Text);
        }
        Assert.Equal(13, said.Distinct().Count());
        // The step's upper end is its own: one more is the next step's
        game.Seen = 16;
        Assert.Equal(said[1], AskTheProfessor(game).Transcript[^1].Text);
        game.Seen = 1;
        Assert.Equal(said[0], AskTheProfessor(game).Transcript[^1].Text);

        // Not shown, nothing is rated
        var shy = AskTheProfessor(game, 1);
        Assert.DoesNotContain(shy.Transcript, l => l.Text.Contains("kinds"));
        // Every Pokémon of Sinnoh seen is not yet the upgrade for one who hasn't entered the Hall of Fame
        game.Seen = 210;
        var early = AskTheProfessor(game);
        Assert.Contains("Pokémon League", early.Transcript[^1].Text);
        Assert.DoesNotContain("nationaldex", early.Log);
        Assert.False(game.NationalDex);

        // Before the assistant has shown the player round the town he only sends them on their way
        game.Story.SetVar("VAR_SANDGEM_TOWN_STATE", 1);
        Assert.Single(AskTheProfessor(game).Transcript);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void TheUpgradeIsHadAsThePokedexsOwnRuleHasIt(bool champion, bool seenAll)
    {
        // Plan 03's decision 5 (Pokedex.CanUnlockNational): the Hall of Fame, and every species of the Sinnoh Pokédex seen
        var dex = new Pokedex();
        var story = new StoryState();
        foreach (var entry in Pokedex.Entries(PokedexMode.Sinnoh).Skip(seenAll ? 0 : 1)) dex.RegisterSeen(entry.Species.DexNumber);
        if (champion) story.CompleteRegion("Sinnoh");
        bool rule = dex.CanUnlockNational(story);
        Assert.Equal(champion && seenAll, rule);
        Assert.Equal(210, Pokedex.Entries(PokedexMode.Sinnoh).Count);

        var game = Challenger();
        if (champion)
        {
            game.Story.Set(Completed);
            game.Story.CompleteRegion("Sinnoh");
        }
        game.Seen = dex.SeenIn(PokedexMode.Sinnoh);
        AskTheProfessor(game);
        Assert.Equal(rule, game.NationalDex);
    }

    [Theory]
    [InlineData(PlayerLook.Boy)]
    [InlineData(PlayerLook.Girl)]
    public void ProfessorOakUpgradesThePokedexAndTheProfessorGivesThePokeRadar(PlayerLook look)
    {
        var game = Champion(look: look);
        game.Seen = 210;
        // The assistant, before the lab, sends the Champion in
        game.Arrive("Sinnoh", 168, 846);
        Assert.Contains("Professor Rowan", TalkFrom(game, "sandgem_counterpart", Direction.Up, "sandgem_town").Transcript[^1].Text);

        // Saying there is nothing to show changes nothing
        var nothing = AskTheProfessor(game, 1);
        Assert.DoesNotContain("nationaldex", nothing.Log);
        Assert.Null(game.Present("prof_oak"));

        var visit = AskTheProfessor(game, 0);
        Assert.Contains(visit.Transcript, l => l.Speaker == "Prof. Rowan" && l.Text.Contains("210 kinds"));
        // Professor Oak comes in by the door, and the player has stepped aside for him
        Assert.Contains(visit.Transcript, l => l.Speaker == "Prof. Oak");
        Assert.Contains(visit.Transcript, l => l.Speaker == null && l.Text.Contains("National Mode"));
        Assert.True(game.NationalDex);
        int upgraded = visit.Log.IndexOf("nationaldex"), radar = visit.Log.FindIndex(l => l.StartsWith("give Poké Radar", StringComparison.Ordinal) || l.Contains("Poké Radar"));
        Assert.True(upgraded >= 0);
        Assert.Equal(1, game.Bag.GetQuantity(Item("Poké Radar")));
        Assert.True(radar < 0 || upgraded < radar);
        // He has gone again, for good, and the player is back before the professor
        Assert.Null(game.Present("prof_oak"));
        Assert.True(game.Story.Has("FLAG_HIDE_SANDGEM_TOWN_LAB_PROF_OAK"));
        Assert.Equal((7, 6), game.Tile);
        // Nobody had won the tag battle yet: the Fight Area's blockades stand
        Assert.False(game.Story.Has(Blockade));

        // From then on the professor speaks of the world's Pokémon, and gives nothing more
        var after = AskTheProfessor(game);
        Assert.Contains("many more", Assert.Single(after.Transcript).Text);
        Assert.Equal(1, game.Bag.GetQuantity(Item("Poké Radar")));
    }

    [Fact]
    public void TheUpgradeLiftsTheBlockadesWhereTheTagBattleIsAlreadyWon()
    {
        var game = Champion();
        game.Seen = 210;
        game.Story.SetVar(FightArea, 2);
        game.Arrive("Sinnoh", 623, 434);
        Assert.NotNull(game.Present("ace_trainer_m_2"));
        Assert.NotNull(game.Present("rich_boy", "route_230"));
        AskTheProfessor(game);
        Assert.True(game.Story.Has(Blockade));
        game.Arrive("Sinnoh", 623, 434);
        Assert.Null(game.Present("ace_trainer_m_2"));
        Assert.Null(game.Present("black_belt_2"));
        Assert.Null(game.Present("fisherman", "route_230"));
        Assert.Null(game.Present("rich_boy", "route_230"));
    }

    // ------------------------------------------------------------------ who asks for the National Pokédex

    [Fact]
    public void WhoTheOriginalHasAskForTheNationalPokedexAsksForItAndNotForTheHallOfFame()
    {
        var game = Challenger();
        game.Story.Set(Completed);
        string Says(string map, string key, int x, int y)
        {
            game.Arrive(map, x, y);
            return string.Join(" ", TalkFrom(game, key, Direction.Up).Transcript.Select(l => l.Text));
        }

        // The assistant's grandfather, Bebe (once her Eevee is given) and Mr. Backlot say what they said to anyone
        game.Story.Set("FLAG_RECEIVED_HEARTHOME_CITY_NORTHWEST_HOUSE_EEVEE");
        string grandfather = Says("SandgemTownCounterpartHouse1F", "expert_m", 4, 8), bebe = Says("HearthomeCityNorthwestHouse", "bebe", 4, 8);
        Assert.Contains("Rowan is back", grandfather);
        Assert.Contains("Bill", bebe);
        game.NationalDex = true;
        Assert.Contains("far more than that", Says("SandgemTownCounterpartHouse1F", "expert_m", 4, 8));
        Assert.Contains("whole Sinnoh Pokédex", Says("HearthomeCityNorthwestHouse", "bebe", 4, 8));

        // None of their scripts reads the Hall of Fame's flag any more
        foreach (var (file, name) in new[] { ("SandgemTownCounterpartHouse1F", "Grandfather"), ("HearthomeCityNorthwestHouse", "Bebe"), ("PokemonMansionOffice", "Backlot") })
            Assert.DoesNotContain(Find(name, file).Everything(), i => i.Condition is { Query: Query.Flag, Name: Completed });
        // ...and those the original gates on both read both: the assistant's sister, the butler, Victory Road's floor
        foreach (var (file, name) in new[] { ("SandgemTownCounterpartHouse1F", "LittleSister"), ("PokemonMansionOffice", "Butler"), ("victory_road_1f", ScriptLibrary.OnEnter) })
        {
            var asked = Find(name, file).Everything().Select(i => i.Condition).OfType<Condition>().ToList();
            Assert.Contains(asked, c => c is { Query: Query.Flag, Name: Completed });
            Assert.Contains(asked, c => c.Query == Query.NationalDex);
        }
    }

    [Fact]
    public void TheAssistantHasMoreToSayOnceThePokedexIsTheNationalOne()
    {
        var game = Champion();
        game.NationalDex = true;
        game.Seen = 210;
        game.Arrive("Sinnoh", 168, 846);
        string Says() => string.Join(" ", TalkFrom(game, "sandgem_counterpart", Direction.Up, "sandgem_town").Transcript.Select(l => l.Text));

        // In the original's order: her sister, Bebe, the Poké Radar's lesson
        Assert.Contains("little sister", Says());
        game.Story.Set("FLAG_TALKED_TO_COUNTERPART_SISTER_WITH_NATIONAL_DEX");
        Assert.Contains("Bebe", Says());
        game.Story.Set("FLAG_RECEIVED_HEARTHOME_CITY_NORTHWEST_HOUSE_EEVEE");
        Assert.Contains("Route 202", Says());
        Assert.True(game.Story.Has("FLAG_STARTED_COUNTERPART_POKE_RADAR_TUTORIAL"));
        Assert.Equal(2, game.Story.Var("VAR_ROUTE_202_STATE"));
        // She has gone off up the road
        Assert.Null(game.Present("sandgem_counterpart", "sandgem_town"));
        Assert.True(game.Story.Has("FLAG_HIDE_SANDGEM_TOWN_COUNTERPART"));

        // On Route 202 she waits in the grass, shows the patch to the east, and goes back to stand before the lab
        game.Arrive("Sinnoh", 186, 822);
        var lesson = game.Present("counterpart", "route_202")!;
        Assert.Equal((186, 817, Direction.Down), (lesson.GridX, lesson.GridY, lesson.Facing));
        Assert.False(game.Fires("CatchingLesson"));
        var shown = TalkFrom(game, "counterpart", Direction.Up, "route_202");
        Assert.Contains("camera Pan 189 818", shown.Log);
        Assert.Contains(shown.Transcript, l => l.Text.Contains("chain"));
        Assert.Equal(50, RadarChain.BatterySteps);
        Assert.Contains(shown.Transcript, l => l.Text.Contains("Fifty steps"));
        Assert.Equal(3, game.Story.Var("VAR_ROUTE_202_STATE"));
        Assert.Null(game.Present("counterpart", "route_202"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_202_COUNTERPART"));
        game.Arrive("Sinnoh", 168, 846);
        Assert.NotNull(game.Present("sandgem_counterpart", "sandgem_town"));

        // After the lesson, one of three things, by the die
        var lines = new HashSet<string>();
        foreach (int roll in new[] { 0, 1, 2 })
        {
            game.Rolls.Enqueue(roll);
            lines.Add(Says());
        }
        Assert.Equal(3, lines.Count);
        Assert.Contains(lines, l => l.Contains("sparkles"));
    }

    // ------------------------------------------------------------------ Pal Park, Eterna City, Iron Island

    [Fact]
    public void PalParksWorkersGoWithTheNationalPokedexAndProfessorOakWaitsInItsLobby()
    {
        var game = Champion();
        game.Arrive("Sinnoh", 306, 914);
        Assert.NotNull(game.Present("worker_west"));
        Assert.NotNull(game.Present("worker_east"));
        game.NationalDex = true;
        game.Arrive("Sinnoh", 306, 914);
        Assert.Null(game.Present("worker_west"));
        Assert.Null(game.Present("worker_east"));
        // The doors behind them lead into the lobby
        Assert.Equal("PalParkLobby", game.Map.GetWarpAt(306, 909)?.TargetMap);

        // Professor Oak comes down the lobby to whoever comes in by the door
        game.Facing = Direction.Up;
        game.Arrive("PalParkLobby", 7, 19);
        var welcome = game.Last;
        Assert.Contains(welcome.Transcript, l => l.Speaker == "Prof. Oak" && l.Text.Contains("Pal Park"));
        Assert.True(welcome.Poketch.Has(PoketchApp.TrainerCounter));
        Assert.Contains(welcome.Transcript, l => l.Text.Contains("Trainer Counter"));
        Assert.Contains(welcome.Transcript, l => l.Text.Contains("Eterna City"));
        Assert.Equal(1, game.Story.Var("VAR_PAL_PARK_LOBBY_STATE"));
        Assert.Equal(2000, game.Story.Var("VAR_CATCHING_SHOW_RECORD"));
        Assert.False(game.Story.Has("FLAG_HIDE_ETERNA_CITY_SOUTH_HOUSE_PROF_OAK"));
        Assert.True(game.Story.Has("FLAG_HIDE_PAL_PARK_LOBBY_PROF_OAK"));
        Assert.Null(game.Present("prof_oak"));
        // The player went up to him, clear of the text, and stepped aside to let him out
        Assert.Equal((8, 16), game.Tile);
        var left = game.Map.Everyone.Single(n => n.Key == "prof_oak");
        Assert.Equal((7, 19), (left.GridX, left.GridY));

        // The record he leaves is the rival's
        Assert.Contains(OpeningTests.Game.Rival, TalkFrom(game, "pokemon_breeder_m", Direction.Up).Transcript.Single().Text);
        // He isn't there a second time
        int played = game.Played.Count;
        game.Arrive("PalParkLobby", 7, 19);
        Assert.DoesNotContain(game.Played.Skip(played), p => p.Host.Transcript.Any(l => l.Speaker == "Prof. Oak"));
    }

    [Fact]
    public void NobodyMeetsProfessorOakInTheLobbyWithoutTheNationalPokedex()
    {
        // The lobby as plan 06 · R16b's tests and tools come into it, without the workers having stood aside
        var game = Challenger();
        game.Arrive("PalParkLobby", 7, 19);
        Assert.Equal(0, game.Story.Var("VAR_PAL_PARK_LOBBY_STATE"));
        Assert.Empty(game.Last.Transcript);
        Assert.Equal(0, game.Story.Var("VAR_CATCHING_SHOW_RECORD"));
    }

    [Fact]
    public void ProfessorOakGivesTheUpgradeInEternaCityAndSetsTheThreeBirdsRoaming()
    {
        var game = Champion();
        game.NationalDex = true;
        game.Arrive("EternaCitySouthHouse", 4, 8);
        Assert.Null(game.Present("prof_oak"));
        // He stays here once he has left Pal Park's lobby
        game.Facing = Direction.Up;
        game.Arrive("PalParkLobby", 7, 19);
        game.Arrive("EternaCitySouthHouse", 4, 8);
        var oak = game.Present("prof_oak")!;
        Assert.Equal((4, 5), (oak.GridX, oak.GridY));

        var first = TalkFrom(game, "prof_oak", Direction.Up);
        Assert.Equal(1, game.Bag.GetQuantity(Item("Upgrade")));
        Assert.True(game.Story.Has("FLAG_RECEIVED_ETERNA_CITY_SOUTH_HOUSE_UPGRADE") && game.Story.Has("FLAG_ACTIVATED_ROAMING_LEGENDARY_BIRDS"));
        foreach (string bird in new[] { "Moltres", "Zapdos", "Articuno" })
        {
            Assert.True(first.Encounters.Roamers[Roamers.SlotOf(bird)!.Value].Active, bird);
            Assert.Contains(first.Transcript, l => l.Text.Contains(bird));
        }
        Assert.False(first.Encounters.Roamers[Roamers.SlotOf("Mesprit")!.Value].Active);

        // Afterwards he only speaks of them; one the Hall of Fame has brought back is set loose again
        var again = TalkFrom(game, "prof_oak", Direction.Up);
        Assert.Equal(1, game.Bag.GetQuantity(Item("Upgrade")));
        Assert.DoesNotContain(again.Encounters.Roamers, r => r.Active);
        game.Story.SetVar(Roamers.StateVariable("Zapdos"), Roamers.Reset);
        var back = TalkFrom(game, "prof_oak", Direction.Up);
        Assert.True(back.Encounters.Roamers[Roamers.SlotOf("Zapdos")!.Value].Active);
        Assert.False(back.Encounters.Roamers[Roamers.SlotOf("Moltres")!.Value].Active);
        Assert.Equal(Roamers.Roaming, game.Story.Var(Roamers.StateVariable("Zapdos")));

        // All three caught, he has his say once, and then talks of the Pokédex
        foreach (string bird in new[] { "Moltres", "Zapdos", "Articuno" }) game.Story.SetVar(Roamers.StateVariable(bird), Roamers.Captured);
        Assert.Contains("All three", string.Join(" ", TalkFrom(game, "prof_oak", Direction.Up).Transcript.Select(l => l.Text)));
        Assert.True(game.Story.Has("FLAG_CAUGHT_ROAMING_LEGENDARY_BIRDS"));
        Assert.Contains("never truly finished", TalkFrom(game, "prof_oak", Direction.Up).Transcript.Single().Text);
    }

    [Theory]
    [InlineData(Direction.Up)]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Right)]
    public void ByronGivesAMetalCoatInIronIslandsCabinOnceThePokedexIsTheNationalOne(Direction facing)
    {
        var game = Champion();
        game.Arrive("IronIslandHouse", 4, 8);
        Assert.Null(game.Present("byron"));
        game.NationalDex = true;
        game.Arrive("IronIslandHouse", 4, 8);
        Assert.NotNull(game.Present("byron"));

        var given = TalkFrom(game, "byron", facing);
        Assert.Equal(1, game.Bag.GetQuantity(Item("Metal Coat")));
        Assert.True(game.Story.Has("FLAG_RECEIVED_IRON_ISLAND_HOUSE_METAL_COAT"));
        Assert.Contains("sound stairs", given.Log);
        // He has gone out by the door, and isn't back
        Assert.Null(game.Present("byron"));
        game.Arrive("IronIslandHouse", 4, 8);
        Assert.Null(game.Present("byron"));
        Assert.Equal(1, game.Bag.GetQuantity(Item("Metal Coat")));
    }

    // ------------------------------------------------------------------ the ship

    /// <summary>Speaks to the sailor of the ship to the Battle Zone from the head of his gangway.</summary>
    private static (HeadlessScriptHost Ship, List<(string Script, HeadlessScriptHost Host)> After) AskTheSailor(OpeningTests.Game game, params int[] answers)
    {
        game.Arrive("Sinnoh", 356, 246);
        var sailor = game.Present("sailor_ss_spiral")!;
        Assert.Equal((356, 247), (sailor.GridX, sailor.GridY));
        game.Tile = (356, 246);
        game.Facing = Direction.Down;
        foreach (int answer in answers) game.Answers.Enqueue(answer);
        int before = game.Played.Count;
        var host = game.Play(Find("Ship", "snowpoint_city"), sailor);
        return (host, game.Played.Skip(before + 1).ToList());
    }

    [Fact]
    public void TheShipSailsOnlyForAChampionAndCynthiaSeesThemOffTheFirstTime()
    {
        var game = Challenger();
        // Before the Hall of Fame the sailor only says where his ship goes
        var (waiting, _) = AskTheSailor(game);
        Assert.Empty(waiting.Asked);
        Assert.DoesNotContain(waiting.Log, l => l.StartsWith("warp", StringComparison.Ordinal));
        Assert.Contains("Battle Zone", waiting.Transcript[0].Text);

        game = Champion();
        // Refused, the ship stays
        var (refused, _) = AskTheSailor(game, 1);
        Assert.DoesNotContain(refused.Log, l => l.StartsWith("warp", StringComparison.Ordinal));
        Assert.False(game.Story.Has("FLAG_SAILED_TO_BATTLE_ZONE"));

        // The first time, Cynthia comes down to the quay: she ends on the tile behind the player, and goes back up the town
        var (sailed, _) = AskTheSailor(game, 0);
        Assert.Contains(sailed.Transcript, l => l.Speaker == "Cynthia" && l.Text.Contains("glad I waited"));
        Assert.True(game.Story.Has("FLAG_SAILED_TO_BATTLE_ZONE") && game.Story.Has("FLAG_HIDE_SNOWPOINT_CITY_CYNTHIA"));
        // The original's landing: the Fight Area's pier, facing east
        Assert.Contains("warp Sinnoh 623 434", sailed.Log);
        Assert.Equal("Sinnoh", game.Map.Name);
        Assert.Equal("fight_area", AreaKey(game.Map, 623, 434));
        Assert.Equal(TileType.Planks, game.Map.GetGroundTile(623, 434));
        var landing = Find("Ship", "snowpoint_city").Everything().Single(i => i.Op == Op.Warp);
        Assert.Equal(Direction.Right, landing.Direction);

        // The pier's own sailor sails back to the quay, and the second crossing has no farewell
        game.Story.SetVar(FightArea, 2);
        var (second, _) = AskTheSailor(game, 0);
        Assert.DoesNotContain(second.Transcript, l => l.Speaker == "Cynthia");
        Assert.Contains("warp Sinnoh 623 434", second.Log);
    }

    [Fact]
    public void TheWholeGamesWalkFindsTheBattleZoneByTheShipAlone()
    {
        Assert.DoesNotContain(WorldWalk.StoryArrivals, s => s.Map == "Sinnoh" && s.X is > 590 and < 870 && s.Y is > 290 and < 500);
        Assert.DoesNotContain(WorldWalk.StoryArrivals, s => s.Map == "PalParkLobby");
        var maps = MapDatabase.Fresh();
        var reached = WorldWalk.From(maps, RegionDatabase.Get(RegionDatabase.Sinnoh)!.Start!);
        var sinnoh = maps("Sinnoh");
        foreach (string place in new[] { "fight_area", "survival_area", "resort_area", "route_227", "route_230" })
            Assert.Contains(reached[sinnoh], t => AreaKey(sinnoh, t.X, t.Y) == place);
        Assert.Contains((7, 19), reached[maps("PalParkLobby")]);
        Assert.True(reached.ContainsKey(maps("PalPark")));
    }

    // ------------------------------------------------------------------ the Fight Area

    /// <summary>Ashore from the ship: the rival's scene plays as the pier is come to. The answers are the scene's own.</summary>
    private static HeadlessScriptHost StepAshore(OpeningTests.Game game, params int[] answers)
    {
        foreach (int answer in answers) game.Answers.Enqueue(answer);
        int before = game.Played.Count;
        game.Arrive("Sinnoh", 623, 434);
        Assert.Equal("fight_area.OnEnter", game.Played[before].Script);
        return game.Played[before].Host;
    }

    [Theory]
    [InlineData(0, "rival_fight_area_turtwig")]
    [InlineData(1, "rival_fight_area_chimchar")]
    [InlineData(2, "rival_fight_area_piplup")]
    public void TheRivalTakesTheChampionToTheGateAndTheyBeatFlintAndVolknerTogether(int starter, string partner)
    {
        var game = Champion(starter);
        game.Arrive("Sinnoh", 630, 434);
        // Come to some other way, nothing begins by itself
        Assert.Empty(game.Last.Transcript);
        var rival = game.Present("rival")!;
        Assert.Equal((628, 434), (rival.GridX, rival.GridY));

        var scene = StepAshore(game);
        Assert.Contains(scene.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("come with me"));
        // The two against the two, with the rival's team that goes with the player's starter
        Assert.Contains($"battle leader_volkner_fight_area and elite_four_flint_fight_area with {partner} Won", scene.Log);
        Assert.Equal(2, game.Story.Var(FightArea));
        // They stood below Flint and Volkner, the player on the west
        Assert.Equal((654, 425), game.Tile);

        // Everyone the scene sends away is gone: the two, their crowd, Palmer, the rival, Buck
        foreach (string flag in new[] { "FLAG_HIDE_FIGHT_AREA_BATTLERS_AND_SPECTATORS", "FLAG_HIDE_FIGHT_AREA_PALMER", "FLAG_HIDE_FIGHT_AREA_RIVAL", "FLAG_HIDE_FIGHT_AREA_BUCK" })
            Assert.True(game.Story.Has(flag), flag);
        foreach (string gone in new[] { "flint", "volkner", "palmer", "rival", "buck", "ace_trainer_m_3", "ace_trainer_f_2", "black_belt_3", "battle_girl_2", "sailor_3" })
            Assert.Null(game.Present(gone));
        // In the original's order: the battle, the crowd gone behind the dark, Palmer's thud, the rival, Buck
        int At(string line) => scene.Log.FindIndex(l => l.StartsWith(line, StringComparison.Ordinal));
        Assert.True(At("battle ") < At("sound bump") && At("sound bump") < At("sound stairs"));
        int Said(string speaker) => scene.Transcript.FindLastIndex(l => l.Speaker == speaker);
        Assert.True(Said("Flint") < Said("Volkner") && Said("Volkner") < Said("Palmer") && Said("Palmer") < Said(OpeningTests.Game.Rival) && Said(OpeningTests.Game.Rival) < Said("Buck"));
        Assert.Contains(scene.Transcript, l => l.Speaker == "Palmer" && l.Text.Contains(OpeningTests.Game.Rival));
        Assert.Contains(scene.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("Tower Tycoon"));
        Assert.Contains(scene.Transcript, l => l.Speaker == "Buck" && l.Text.Contains("Survival Area"));

        // Without the National Pokédex the two blockades stand, and say why
        Assert.False(game.Story.Has(Blockade));
        Assert.Contains("Pokédex", string.Join(" ", TalkFrom(game, "ace_trainer_m_2", Direction.Up).Transcript.Select(l => l.Text)));
        Assert.Contains("Battle Frontier", string.Join(" ", TalkFrom(game, "fisherman", Direction.Right, "route_230").Transcript.Select(l => l.Text)));

        // The scene doesn't come again
        game.Arrive("Sinnoh", 623, 434);
        Assert.Empty(game.Last.Transcript);
    }

    [Fact]
    public void WonWithTheNationalPokedexTheTagBattleLiftsTheBlockades()
    {
        var game = Champion();
        game.NationalDex = true;
        StepAshore(game);
        Assert.True(game.Story.Has(Blockade));
        Assert.Null(game.Present("ace_trainer_m_2"));
        Assert.Null(game.Present("black_belt_2"));
        Assert.Null(game.Present("fisherman", "route_230"));
        Assert.Null(game.Present("rich_boy", "route_230"));
    }

    [Theory]
    [InlineData(Direction.Right)]
    [InlineData(Direction.Left)]
    [InlineData(Direction.Down)]
    [InlineData(Direction.Up)]
    public void RefusedTheRivalWaitsBeforeTheGateAndAsksAgainFromAnySide(Direction facing)
    {
        var game = Champion();
        var refused = StepAshore(game, 1);
        Assert.DoesNotContain(refused.Log, l => l.StartsWith("battle", StringComparison.Ordinal));
        Assert.Equal(1, game.Story.Var(FightArea));
        Assert.Contains("alone", refused.Transcript[^1].Text);
        Assert.Equal((654, 426), game.Tile);

        // He is before the gate from then on, however the town is come to, with the two still at it
        game.Arrive("Sinnoh", 640, 434);
        var rival = game.Present("rival")!;
        Assert.Equal((655, 426, Direction.Up), (rival.GridX, rival.GridY, rival.Facing));
        Assert.Contains("partner", TalkFrom(game, "flint", Direction.Up).Transcript.Single().Text);
        Assert.Contains("friend", TalkFrom(game, "volkner", Direction.Up).Transcript.Single().Text);
        Assert.Contains("can't take my eyes", TalkFrom(game, "buck", Direction.Right).Transcript.Single().Text);

        // Spoken to from any side, the player is brought round to his left hand, and the battle follows a yes
        game.Answers.Enqueue(1);
        var again = TalkFrom(game, "rival", facing);
        Assert.Equal((654, 426), game.Tile);
        Assert.DoesNotContain(again.Log, l => l.StartsWith("battle", StringComparison.Ordinal));
        var battle = TalkFrom(game, "rival", facing);
        Assert.Contains(battle.Log, l => l.StartsWith("battle leader_volkner_fight_area and elite_four_flint_fight_area with rival_fight_area_turtwig", StringComparison.Ordinal));
        Assert.Equal(2, game.Story.Var(FightArea));
    }

    [Fact]
    public void ATagBattleLostLeavesTheRivalWaitingBeforeTheGate()
    {
        var game = Champion(fight: BattleOutcome.Lost);
        var lost = StepAshore(game);
        Assert.Contains(lost.Log, l => l.StartsWith("battle", StringComparison.Ordinal) && l.EndsWith("Lost", StringComparison.Ordinal));
        Assert.Equal(1, game.Story.Var(FightArea));
        Assert.False(game.Story.Has("FLAG_HIDE_FIGHT_AREA_BATTLERS_AND_SPECTATORS"));
        game.Arrive("Sinnoh", 640, 434);
        var rival = game.Present("rival")!;
        Assert.Equal((655, 426), (rival.GridX, rival.GridY));
        Assert.NotNull(game.Present("flint"));
    }

    [Fact]
    public void TheRivalOnThePierTakesWhoeverSpeaksToHimToTheGate()
    {
        // Someone who came to the Fight Area some other way and finds him on the pier
        var game = Champion();
        game.Arrive("Sinnoh", 630, 434);
        game.Answers.Enqueue(1);
        var pier = TalkFrom(game, "rival", Direction.Left);
        Assert.Contains(pier.Transcript, l => l.Text.Contains("come with me"));
        Assert.Equal(1, game.Story.Var(FightArea));
        Assert.Equal((654, 426), game.Tile);
    }

    /// <summary>
    /// Every tile of the Battle Zone come to from a tile by the field's own rules, on foot and by Surf, Waterfall and
    /// Rock Climb, through the gate houses that are walked through. Whoever <paramref name="blocks"/> names stands in
    /// the way; anyone else (a trainer, an item's ball, an obstacle) is walked past.
    /// </summary>
    private static HashSet<(int X, int Y)> Reached(Map map, (int X, int Y) from, Func<NPC, bool> blocks)
    {
        const FieldMoves everything = FieldMoves.Surf | FieldMoves.Waterfall | FieldMoves.RockClimb;
        var seen = new HashSet<(int, int, TravelMode, int)>();
        var tiles = new HashSet<(int X, int Y)>();
        var queue = new Queue<(int X, int Y, TravelMode Mode, float Height)>();
        void Visit(int x, int y, TravelMode mode, float height)
        {
            if (x < 596 || x > 870 || y < 290 || y > 500) return;
            if (!seen.Add((x, y, mode, (int)MathF.Round(height * 2f)))) return;
            tiles.Add((x, y));
            queue.Enqueue((x, y, mode, height));
        }
        Visit(from.X, from.Y, TravelMode.OnFoot, map.HeightAt(from.X, from.Y));
        while (queue.Count > 0)
        {
            var (x, y, mode, height) = queue.Dequeue();
            foreach (var warp in map.WarpsAt(x, y))
            {
                // A gate house is a room since plan 01 · M11, part d: in at one door and out of the far one
                var through = warp.TargetMap != map.Name && warp.TargetMap.Contains("Gate", StringComparison.Ordinal) ? GateWalk.FarDoor(warp) : warp;
                if (through.TargetMap == map.Name) Visit(through.TargetX, through.TargetY, TravelMode.OnFoot, map.HeightAt(through.TargetX, through.TargetY));
            }
            var walker = new Walker(mode, height, Moves: everything, Climbing: true);
            foreach (var way in new[] { Direction.Up, Direction.Right, Direction.Left, Direction.Down })
            {
                var step = FieldMovement.Step(map, x, y, way, walker);
                if (step.Moves)
                {
                    Visit(step.X, step.Y, step.Mode, step.Height);
                    continue;
                }
                var (dx, dy) = FieldMovement.Delta(way);
                int nx = x + dx, ny = y + dy;
                if (step.Obstacle == Obstacle.Water && FieldMovement.CanStartSurf(map, x, y, way, walker))
                    Visit(nx, ny, TravelMode.Surfing, map.SurfaceAt(nx, ny, height).Height);
                else if (step.Obstacle == Obstacle.Person && map.GetNpcAt(nx, ny) is { } person && !blocks(person)
                         && !map.IsSolid(nx, ny) && MathF.Abs(map.HeightAt(nx, ny) - height) < FieldMovement.StepLimit)
                    Visit(nx, ny, mode, map.HeightAt(nx, ny));
            }
        }
        return tiles;
    }

    [Fact]
    public void TheTwoBlockadesShutTheFightAreaOffFromTheRestOfTheBattleZone()
    {
        var game = Champion();
        game.Arrive("Sinnoh", 623, 434);
        var map = game.Map;
        var four = new[] { game.Present("ace_trainer_m_2")!, game.Present("black_belt_2")!, game.Present("fisherman", "route_230")!, game.Present("rich_boy", "route_230")! };
        var beyond = new[] { "route_225", "survival_area", "route_226", "route_227", "route_228", "route_229", "resort_area" };

        // With the four in their places, nothing past them is come to from the pier, on foot or by sea
        var shut = Reached(map, (623, 434), four.Contains);
        Assert.Contains(shut, t => AreaKey(map, t.X, t.Y) == "fight_area" && t.X > 650);
        // (Route 230 begins at the water's edge: a strip of its shore runs north from the town and leads nowhere)
        var leaked = shut.Where(t => beyond.Contains(AreaKey(map, t.X, t.Y)) || (AreaKey(map, t.X, t.Y) == "route_230" && t.X > 690)).ToList();
        Assert.True(leaked.Count == 0, $"past the blockades: {string.Join(" ", leaked.Take(12))}");
        Assert.DoesNotContain((683, 430), shut);
        Assert.DoesNotContain((683, 431), shut);
        Assert.DoesNotContain((629, 428), shut);

        // Each pair alone would let the player out: both stand until the battle is won
        Assert.Contains(Reached(map, (623, 434), n => n == four[0] || n == four[1]), t => AreaKey(map, t.X, t.Y) == "resort_area");
        Assert.Contains(Reached(map, (623, 434), n => n == four[2] || n == four[3]), t => AreaKey(map, t.X, t.Y) == "survival_area");

        // Lifted, the whole island lies open
        var open = Reached(map, (623, 434), _ => false);
        Assert.All(beyond, place => Assert.Contains(open, t => AreaKey(map, t.X, t.Y) == place));
    }

    // ------------------------------------------------------------------ Route 227 and the Survival Area

    [Theory]
    [InlineData(738)]
    [InlineData(739)]
    public void CrasherWakeAndTheRivalAreMetAtTheFootOfRoute227(int x)
    {
        var game = Champion();
        game.Arrive("Sinnoh", 738, 310);
        Assert.NotNull(game.Present("rival", "route_227"));
        Assert.NotNull(game.Present("crasher_wake", "route_227"));
        game.Facing = Direction.Up;
        var met = StepOnto(game, x, 306, "WakeAndRival");
        Assert.Contains(met.Transcript, l => l.Speaker == "Crasher Wake" && l.Text.Contains("Champion"));
        Assert.Contains(met.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("master"));
        Assert.Contains(met.Transcript, l => l.Speaker == OpeningTests.Game.Rival && l.Text.Contains("Battle Frontier"));
        // The player ends where Crasher Wake stood, beside the rival; both have gone down the road
        Assert.Equal((738, 305), game.Tile);
        Assert.Equal(1, game.Story.Var("VAR_ROUTE_227_WAKE_RIVAL_STATE"));
        Assert.True(game.Story.Has("FLAG_HIDE_ROUTE_227_RIVAL") && game.Story.Has("FLAG_HIDE_ROUTE_227_CRASHER_WAKE"));
        Assert.Null(game.Present("rival", "route_227"));
        Assert.Null(game.Present("crasher_wake", "route_227"));
        Assert.Null(FieldScripts.TriggerAt(game.Map, 738, 306, game.Story));
    }

    [Fact]
    public void BuckAsksThePlayerToLookRoundStarkMountainUntilTheyAgree()
    {
        var game = Champion();
        game.Arrive("Sinnoh", 758, 280);
        game.Facing = Direction.Up;
        // Two refusals, then yes
        game.Answers.Enqueue(1);
        game.Answers.Enqueue(1);
        var asked = StepOnto(game, 758, 277, "BuckAsks");
        Assert.Equal(3, asked.Asked.Count);
        Assert.Equal(2, asked.Transcript.Count(l => l.Text.Contains("go on asking")));
        Assert.Contains(asked.Transcript, l => l.Speaker == "Buck" && l.Text.Contains("goons"));
        Assert.Equal(1, game.Story.Var("VAR_ROUTE_227_BUCK_STATE"));
        // He came a step down the road to meet the player, and waits there (the mountain's first room hides him, part b)
        var buck = game.Present("buck", "route_227")!;
        Assert.Equal((758, 276), (buck.GridX, buck.GridY));
        Assert.False(game.Story.Has("FLAG_HIDE_ROUTE_227_BUCK"));
        Assert.Contains("in charge", TalkFrom(game, "buck", Direction.Up, "route_227").Transcript.Single().Text);
        Assert.Null(FieldScripts.TriggerAt(game.Map, 758, 277, game.Story));
    }

    [Fact]
    public void BuckAndTheRivalComeToTheBattlegroundsStepOnlyOnceTheirFlagsAreCleared()
    {
        var game = Champion();
        game.Arrive("Sinnoh", 655, 341);
        Assert.Null(game.Present("buck", "survival_area"));
        Assert.Null(game.Present("rival", "survival_area"));
        // The Battleground's door is shut until Charon is caught, and read where it stands (plan 02 · S15, part c)
        Assert.True(game.Map.IsSolid(652, 338));
        Assert.Equal("BattlegroundDoor", game.Map.TileScripts[(652, 338)]);

        // What Stark Mountain's last scene does (the original's ClearFlag there: plan 02 · S15, part b)
        Run(game, "survival_area", "clearflag FLAG_HIDE_SURVIVAL_AREA_BUCK\nclearflag FLAG_HIDE_SURVIVAL_AREA_RIVAL");
        game.Arrive("Sinnoh", 655, 341);
        var buck = game.Present("buck", "survival_area")!;
        Assert.Equal((652, 339), (buck.GridX, buck.GridY));
        var goesIn = TalkFrom(game, "buck", Direction.Up, "survival_area");
        Assert.Contains(goesIn.Transcript, l => l.Text.Contains("Battleground"));
        Assert.Contains("sound door_open", goesIn.Log);
        Assert.Null(game.Present("buck", "survival_area"));
        Assert.True(game.Story.Has("FLAG_HIDE_SURVIVAL_AREA_BUCK"));
    }

    [Theory]
    [InlineData(0, 0, "rival_survival_area_1_turtwig")]
    [InlineData(1, 19, "rival_survival_area_1_chimchar")]
    [InlineData(2, 20, "rival_survival_area_2_piplup")]
    [InlineData(0, 55, "rival_survival_area_2_turtwig")]
    public void TheRivalBattlesInTheSurvivalAreaAtWeekendsOnceADay(int starter, int victories, string team)
    {
        var game = Champion(starter);
        Run(game, "survival_area", "clearflag FLAG_HIDE_SURVIVAL_AREA_RIVAL");
        game.Arrive("Sinnoh", 655, 341);
        game.LeagueVictories = victories;

        // On a weekday, one of three things by the die
        game.Weekday = DayOfWeek.Wednesday;
        var lines = new HashSet<string>();
        foreach (int roll in new[] { 0, 1, 2 })
        {
            game.Rolls.Enqueue(roll);
            var talk = TalkFrom(game, "rival", Direction.Up, "survival_area");
            Assert.DoesNotContain(talk.Log, l => l.StartsWith("battle", StringComparison.Ordinal));
            lines.Add(talk.Transcript.Single().Text);
        }
        Assert.Equal(3, lines.Count);

        // On Saturday and Sunday he battles, with the team the Hall of Fame's count picks, once a day
        foreach (var day in new[] { DayOfWeek.Saturday, DayOfWeek.Sunday })
        {
            game.Weekday = day;
            var battle = TalkFrom(game, "rival", Direction.Up, "survival_area");
            Assert.Contains($"battle {team} Won", battle.Log);
            Assert.True(game.Story.Has("FLAG_DAILY_DEFEATED_SURVIVAL_AREA_RIVAL"));
            var after = TalkFrom(game, "rival", Direction.Up, "survival_area");
            Assert.DoesNotContain(after.Log, l => l.StartsWith("battle", StringComparison.Ordinal));
            Assert.Contains("next week", after.Transcript.Single().Text);
            // The day's flags are cleared with the day
            game.Story.ClearDaily();
            Assert.False(game.Story.Has("FLAG_DAILY_DEFEATED_SURVIVAL_AREA_RIVAL"));
        }
    }

    // ------------------------------------------------------------------ Victory Road's back room

    [Theory]
    [InlineData(55)]
    [InlineData(58)]
    [InlineData(60)]
    public void MarleyJoinsWhoeverStepsTowardsHerAndGoesBackIfTheyTurnBack(int row)
    {
        var game = Champion();
        game.NationalDex = true;
        game.Arrive("VictoryRoad1FRoom2", 4, 58);
        Assert.Equal(0, game.Story.Var(MarleyState));
        Assert.NotNull(game.Present("marley"));

        game.Facing = Direction.Right;
        var joined = StepOnto(game, 5, row, "MarleyJoins");
        Assert.Contains(joined.Transcript, l => l.Speaker == "Marley" && l.Text.Contains("go with you"));
        Assert.Contains(joined.Transcript, l => l.Text.Contains("decided to go with Marley"));
        Assert.Contains("partner Marley marley_victory_road", joined.Log);
        Assert.Equal(1, game.Story.Var(MarleyState));
        // She came up to the tile east of the player's
        var marley = game.Present("marley")!;
        Assert.Equal((6, row), (marley.GridX, marley.GridY));
        Assert.NotNull(TrainerDatabase.Get("marley_victory_road"));
        // On the way she has two things to say, the second again and again
        Assert.Contains("talk a lot", TalkFrom(game, "marley", Direction.Right).Transcript.Single().Text);
        Assert.Contains("flowers", TalkFrom(game, "marley", Direction.Right).Transcript[0].Text);
        Assert.Contains("flowers", TalkFrom(game, "marley", Direction.Right).Transcript[0].Text);

        // Turning back for the way in sends her to wait where she stood
        game.Facing = Direction.Left;
        var back = StepOnto(game, 4, row, "PlayerTurnsBack");
        Assert.Contains("partner off", back.Log);
        Assert.Contains(back.Transcript, l => l.Text.Contains("going back"));
        Assert.Equal(0, game.Story.Var(MarleyState));
        Assert.Equal((7, 58, Direction.Right), (marley.GridX, marley.GridY, marley.Facing));
        Assert.False(game.Story.Has("FLAG_TRAVELED_WITH_MARLEY"));

        // And she has other words for whoever comes back for her
        game.Facing = Direction.Right;
        var again = StepOnto(game, 5, row, "MarleyJoins");
        Assert.Contains(again.Transcript, l => l.Text.Contains("took so long"));
        Assert.Equal(1, game.Story.Var(MarleyState));
    }

    [Theory]
    [InlineData(51)]
    [InlineData(52)]
    [InlineData(53)]
    [InlineData(54)]
    public void MarleyPartsAtTheFarEndAndHasTravelledWithThePlayerForGood(int column)
    {
        var game = Champion();
        game.Arrive("VictoryRoad1FRoom2", 4, 58);
        game.Facing = Direction.Right;
        StepOnto(game, 5, 58, "MarleyJoins");
        Assert.Null(FieldScripts.TriggerAt(game.Map, 5, 57, game.Story));

        game.Facing = Direction.Up;
        var parted = StepOnto(game, column, 19, "MarleyParts");
        Assert.Contains("partner off", parted.Log);
        Assert.Contains(parted.Transcript, l => l.Speaker == "Marley" && l.Text.Contains("Thank you"));
        Assert.True(game.Story.Has("FLAG_TRAVELED_WITH_MARLEY") && game.Story.Has("FLAG_HIDE_VICTORY_ROAD_1F_ROOM_2_MARLEY"));
        Assert.Equal(2, game.Story.Var(MarleyState));
        Assert.Null(game.Present("marley"));
        // She went on to the way out to Route 224
        var marley = game.Map.Everyone.Single(n => n.Key == "marley");
        Assert.Equal((55, 13), (marley.GridX, marley.GridY));
        Assert.Equal((56, 13), game.Map.Warps.Where(w => w.TargetMap == "VictoryRoad1FRoom3").Select(w => (w.SourceX, w.SourceY)).Single());

        // The room is hers no longer: nothing fires, and coming in again leaves the state alone
        game.Arrive("VictoryRoad1FRoom2", 4, 58);
        Assert.Equal(2, game.Story.Var(MarleyState));
        Assert.Null(game.Present("marley"));
        Assert.Null(FieldScripts.TriggerAt(game.Map, 5, 58, game.Story));
        // The flag is what lets her into the Battle Tower's Battle Salon (plan 06 · R18a)
        Assert.Contains("FLAG_TRAVELED_WITH_MARLEY", PokemonPlatinumEngine.Models.Frontier.BattleTowerChallenge.PartnerFlags);
    }

    [Fact]
    public void MarleyWaitsWhereSheStoodForAPlayerWhoLeftWithoutHer()
    {
        var game = Champion();
        game.Arrive("VictoryRoad1FRoom2", 4, 58);
        game.Facing = Direction.Right;
        StepOnto(game, 5, 56, "MarleyJoins");
        // Lost on the way (a white-out drops whoever travels along), and the room come to again
        Run(game, "victory_road_1f_room_2", "partner off");
        game.Arrive("VictoryRoad1FRoom2", 4, 58);
        Assert.Equal(0, game.Story.Var(MarleyState));
        var marley = game.Present("marley")!;
        Assert.Equal((7, 58), (marley.GridX, marley.GridY));
    }

    // ------------------------------------------------------------------ Lucian, Looker

    [Fact]
    public void LucianReadsInCanalavesLibraryAfterEveryVictoryAndLeavesWhenSpokenTo()
    {
        var before = Challenger();
        before.Arrive("CanalaveLibrary1F", 11, 11);
        Assert.Null(before.Present("lucian"));

        // One of five books, by the die
        var books = new HashSet<string>();
        foreach (int roll in new[] { 0, 1, 2, 3, 4 })
        {
            var game = Champion();
            game.Arrive("CanalaveLibrary1F", 11, 11);
            var lucian = game.Present("lucian")!;
            Assert.Equal((16, 5, Direction.Up), (lucian.GridX, lucian.GridY, lucian.Facing));
            game.Rolls.Enqueue(roll);
            var talk = TalkFrom(game, "lucian", roll % 2 == 0 ? Direction.Up : Direction.Right);
            Assert.Equal(3, talk.Transcript.Count);
            books.Add(talk.Transcript[1].Text);
            Assert.Contains("Pokémon League", talk.Transcript[2].Text);
            // He goes out by the door
            Assert.Contains("sound stairs", talk.Log);
            Assert.Equal((11, 11), (lucian.GridX, lucian.GridY));
            Assert.Null(game.Present("lucian"));
            Assert.True(game.Story.Has("FLAG_HIDE_CANALAVE_LIBRARY_1F_LUCIAN"));

            if (roll != 0) continue;
            // The next victory brings him back
            game.Play(Find("Victory", Hall));
            game.Arrive("CanalaveLibrary1F", 11, 11);
            Assert.NotNull(game.Present("lucian"));
        }
        Assert.Equal(5, books.Count);
    }

    [Fact]
    public void LookerHasOtherWordsAtTheGameCornerOnceCharonIsCaught()
    {
        var corner = GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, "VeilstoneGameCorner.json")).ToMap();
        var looker = corner.Everyone.Single(n => n.Key == "looker");
        string Says(Action<HeadlessScriptHost>? before = null)
        {
            var host = new HeadlessScriptHost { Map = corner };
            before?.Invoke(host);
            var runner = new ScriptRunner(Scripts, host);
            runner.Start(Find("Looker", "VeilstoneGameCorner"), looker);
            runner.RunToEnd();
            return string.Join(" ", host.Transcript.Select(l => l.Text));
        }
        Assert.Contains("I am not here", Says());
        // The flag is Stark Mountain's last scene's to set (plan 02 · S15, part b: StarkMountainAndTheLegendaryPokemonTests)
        Assert.Contains("holiday", Says(host => host.Story.Set("FLAG_ARRESTED_CHARON_STARK_MOUNTAIN")));
        Assert.Contains("FLAG_ARRESTED_CHARON_STARK_MOUNTAIN", Scripts.FlagsWritten);
    }

    // ------------------------------------------------------------------ saves from before

    [Fact]
    public void ASaveFromBeforeTheTimeAfterTheHallOfFameIsBroughtUpToDate()
    {
        // One that has never been near any of it: its people are hidden until their time, and nothing else has moved
        var early = new StoryState();
        StoryMigration.Upgrade(early, 17, Array.Empty<Pokemon>(), Scripts);
        Assert.All(HiddenFromTheStart, flag => Assert.True(early.Has(flag), flag));
        Assert.Equal(0, early.Var(FightArea));
        Assert.False(early.Has(Blockade) || early.Has("FLAG_HIDE_FIGHT_AREA_RIVAL"));

        // A Champion from before, who has never sailed: the rival waits on the pier for them, and the blockades stand
        var champion = new StoryState();
        champion.Set(Completed);
        champion.CompleteRegion("Sinnoh");
        StoryMigration.Upgrade(champion, 17, Array.Empty<Pokemon>(), Scripts);
        Assert.All(HiddenFromTheStart, flag => Assert.True(champion.Has(flag), flag));
        Assert.Equal(0, champion.Var(FightArea));
        Assert.False(champion.Has(Blockade) || champion.Has("FLAG_HIDE_FIGHT_AREA_RIVAL") || champion.Has("FLAG_HIDE_ROUTE_227_RIVAL"));

        // One that has walked the Battle Zone while its roads stood open: for it the scenes before the gate and at the
        // foot of Route 227 are played, everyone they send away is gone, and no blockade shuts it in or out
        foreach (string arrival in StoryMigration.ArrivedInTheBattleZone)
        {
            var walked = new StoryState();
            walked.Set(Completed);
            walked.Set(arrival);
            StoryMigration.Upgrade(walked, 17, Array.Empty<Pokemon>(), Scripts);
            Assert.Equal((2, 1), (walked.Var(FightArea), walked.Var("VAR_ROUTE_227_WAKE_RIVAL_STATE")));
            foreach (string flag in new[] { Blockade, "FLAG_HIDE_FIGHT_AREA_RIVAL", "FLAG_HIDE_FIGHT_AREA_BUCK", "FLAG_HIDE_FIGHT_AREA_BATTLERS_AND_SPECTATORS", "FLAG_HIDE_FIGHT_AREA_PALMER",
                         "FLAG_HIDE_ROUTE_227_RIVAL", "FLAG_HIDE_ROUTE_227_CRASHER_WAKE" })
                Assert.True(walked.Has(flag), flag);
            // Buck still waits on Route 227, where the way into Stark Mountain begins
            Assert.False(walked.Has("FLAG_HIDE_ROUTE_227_BUCK"));
            Assert.Equal(0, walked.Var("VAR_ROUTE_227_BUCK_STATE"));
        }
        Assert.All(StoryMigration.ArrivedInTheBattleZone, flag => Assert.Contains(SpawnLocations.All, s => s.ArrivalFlag == flag));

        // A save of today is left as it is
        var today = new StoryState();
        today.Set(Completed);
        today.Set("FLAG_FIRST_ARRIVAL_FIGHT_AREA");
        StoryMigration.Upgrade(today, StoryState.CurrentVersion, Array.Empty<Pokemon>(), Scripts);
        Assert.Equal(0, today.Var(FightArea));
        Assert.False(today.Has(Blockade));

        // And the oldest saves come through every step to the same place as a new game
        var oldest = new StoryState();
        StoryMigration.Upgrade(oldest, 0, Array.Empty<Pokemon>(), Scripts, new Inventory());
        Assert.All(HiddenFromTheStart, flag => Assert.True(oldest.Has(flag), flag));
    }

    [Fact]
    public void ASaveThatWalkedTheBattleZoneMeetsNobodyOnThePierOrBeforeTheGate()
    {
        var game = Champion();
        game.Story.Set("FLAG_FIRST_ARRIVAL_SURVIVAL_AREA");
        StoryMigration.Upgrade(game.Story, 17, Array.Empty<Pokemon>(), Scripts);
        int played = game.Played.Count;
        game.Arrive("Sinnoh", 623, 434);
        Assert.Empty(game.Played[played].Host.Transcript);
        foreach (string gone in new[] { "rival", "flint", "volkner", "buck", "palmer", "ace_trainer_m_2", "black_belt_2", "ace_trainer_m_3" })
            Assert.Null(game.Present(gone));
        Assert.Null(game.Present("rich_boy", "route_230"));
        game.Arrive("Sinnoh", 738, 310);
        Assert.Null(game.Present("rival", "route_227"));
        Assert.Null(FieldScripts.TriggerAt(game.Map, 738, 306, game.Story));
        game.Arrive("Sinnoh", 758, 280);
        Assert.NotNull(game.Present("buck", "route_227"));
    }
}
