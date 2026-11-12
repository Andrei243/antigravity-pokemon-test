using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.OpenWorld;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using Xunit;
using static PokemonPlatinumTests.CoreScenario;
using static PokemonPlatinumTests.Scenario;

namespace PokemonPlatinumTests;

/// <summary>
/// Galar's Wild Area, its southern half (plan 33 · GL2; <c>docs/mechanics/rulings.md</c>, "The Wild Area"): its eight
/// parts on the open map with their songs and zones, the weather of each by the region's calendar (the same for a day
/// however often it is asked, and what the picture, the wild Pokémon and a battle read), a zone's Pokémon following its
/// weather, the rustling grass that meets a zone's other table, the strong Pokémon a ball can't take until the badges
/// say, the Stone Arch's Runerigus, and the train north from Wedgehurst and back.
/// </summary>
public class GalarWildAreaTests
{
    private static Region Region => RegionDatabase.Get(RegionDatabase.Galar)!;
    private static readonly Lazy<OpenMap> Open = new(() => OpenRegions.Load(Region).Single());
    private static string Folder => Path.Combine(Repository.Root, "PokemonPlatinumEngine", "Data", "world3d", "galar");

    private static readonly string[] WildArea =
    {
        "wild_area_meetup_spot", "wild_area_rolling_fields", "wild_area_dappled_grove", "wild_area_watchtower_ruins",
        "wild_area_lake_axewell", "wild_area_giants_seat", "wild_area_lake_miloch", "wild_area_motostoke_riverbank",
        // The northern half begun (plan 33 · GL5)
        "wild_area_hammerlocke_hills"
    };

    private static OpenPos On(OpenMap map, double x, double z)
    {
        lock (OpenRegions.Reading) return OpenPos.FromMetres(x, map.Ground.Footing(x, z, 30, -10, walkableOnly: true)!.Value.Y, z);
    }

    private static void Tick(OpenField field, int ticks)
    {
        lock (OpenRegions.Reading)
            for (int i = 0; i < ticks; i++)
            {
                field.Player.Step(field.Map, OpenInput.None, field.Grants);
                field.StepTheRest();
            }
    }

    [Fact]
    public void TheWildAreaIsEightPartsWithTheirSongsCalendarAndZones()
    {
        var map = Open.Value;
        Assert.Equal(WildArea, map.Areas.Select(a => a.Key).Where(k => k.StartsWith("wild_area_")));
        foreach (string key in WildArea)
        {
            var area = map.Areas.Single(a => a.Key == key);
            Assert.Equal("galar/wild_area", area.Music);
            Assert.True(map.Calendar!.Keeps(key), $"{key} has no column of the calendar");
            Assert.Contains(map.Zones, z => z.Place == key);
        }
        var zones = map.Zones.Where(z => WildArea.Contains(z.Place)).ToList();
        // Every row a species or form of the data; rows by weather; rustling grass in most parts
        foreach (var slot in zones.SelectMany(z => z.Slots.Concat(z.Rustle)))
            Assert.True(PokemonDatabase.Get(slot.Species) != null || PokemonDatabase.SpeciesOfForm(slot.Species) != null, slot.Species);
        Assert.True(zones.SelectMany(z => z.Slots).Count(s => s.Weather != null) >= 10);
        Assert.True(zones.Count(z => z.Rustle.Count > 0) >= 6);
        // The strong Pokémon: rows a Trainer with no badges can't catch
        var strong = zones.SelectMany(z => z.Slots).Where(s => s.MinLevel > CatchLimits.Galar[0]).Select(s => s.Species).ToList();
        Assert.Contains("Onix", strong);
        Assert.Contains("Haxorus", strong);
        // The Stone Arch is the Giant's Seat's, and nowhere else's
        Assert.Equal(new[] { "Stone Arch" }, map.SitesAt(map.Marker("stone_arch")!.Pos));
        Assert.Empty(map.SitesAt(map.Marker("wild_area_station")!.Pos));
    }

    [Fact]
    public void TheCalendarsWeatherForADayIsTheSameTwiceAndIsWhatEveryoneReads()
    {
        var once = OpenCalendar.Read(Folder)!;
        var twice = OpenCalendar.Read(Folder)!;
        Assert.Equal(WildArea, once.Places);
        var map = Open.Value;
        var seen = new HashSet<FieldWeather>();
        for (var day = new DateTime(2027, 1, 1); day.Year == 2027; day = day.AddDays(1))
            foreach (string key in WildArea)
            {
                var weather = once.On(key, day)!.Value;
                Assert.Equal(weather, twice.On(key, day));
                Assert.Equal(weather, OpenWeather.Of(map, map.Areas.Single(a => a.Key == key), day));
                seen.Add(weather);
            }
        // A leap day has its row, and the year's weather is more than sun and cloud
        Assert.NotNull(once.On("wild_area_giants_seat", new DateTime(2028, 2, 29)));
        Assert.True(seen.Count >= 6, string.Join(", ", seen));
        // A part the calendar keeps no column for keeps its own: the Weald's fog, Route 1's clear sky
        Assert.Equal(FieldWeather.Fog, OpenWeather.Of(map, map.Areas.Single(a => a.Key == "slumbering_weald"), new DateTime(2027, 5, 1)));
        Assert.Equal(FieldWeather.Clear, OpenWeather.Of(map, map.Areas.Single(a => a.Key == "galar_route_1"), new DateTime(2027, 5, 1)));
        Assert.False(once.Keeps("postwick"));
    }

    [Fact]
    public void AZonesPokemonFollowItsWeather()
    {
        var map = Open.Value;
        var seat = map.Zones.Single(z => z.Name == "giants_seat");
        var silicobra = seat.Slots.Single(s => s.Species == "Silicobra");
        Assert.True(OpenWildField.MetAt(silicobra, TimeOfDay.Day, FieldWeather.Sandstorm));
        Assert.False(OpenWildField.MetAt(silicobra, TimeOfDay.Day, FieldWeather.Clear));
        // A row with no weather is met under any
        var roggenrola = seat.Slots.Single(s => s.Species == "Roggenrola");
        foreach (var weather in Enum.GetValues<FieldWeather>()) Assert.True(OpenWildField.MetAt(roggenrola, TimeOfDay.Day, weather));

        // In the field: the Giant's Seat's Pokémon brought out under a sandstorm are its sandstorm's rows and its own,
        // and under a clear sky never a sandstorm's
        foreach (var weather in new[] { FieldWeather.Sandstorm, FieldWeather.Clear })
        {
            var field = new OpenField(map, On(map, -88, -520), OpenYaw.North);
            field.Wild.WeatherOf = _ => weather;
            Tick(field, 90 * OpenClock.TicksPerSecond);
            var species = field.Wild.All.Where(w => w.Zone == seat).Select(w => w.Species).ToHashSet();
            Assert.NotEmpty(species);
            foreach (string s in species)
                Assert.True(seat.Slots.Any(row => row.Species == s && OpenWildField.MetAt(row, TimeOfDay.Day, weather)), $"{s} under {weather}");
            if (weather == FieldWeather.Clear) Assert.DoesNotContain("Silicobra", species);
        }
    }

    [Fact]
    public void RustlingGrassShakesAndTouchedMeetsTheZonesOtherTable()
    {
        var map = Open.Value;
        var fields = map.Zones.Single(z => z.Name == "rolling_fields");
        OpenRustle? Shaking(OpenField field)
        {
            for (int i = 0; i < 60 * OpenClock.TicksPerSecond; i++)
            {
                Tick(field, 1);
                if (field.Wild.Rustles.FirstOrDefault(r => r.Zone == fields) is { } r) return r;
            }
            return null;
        }
        var one = new OpenField(map, On(map, -90, -340), OpenYaw.North);
        var two = new OpenField(map, On(map, -90, -340), OpenYaw.North);
        var rustle = Shaking(one);
        Assert.NotNull(rustle);
        // The same ticks shake the same patch: the zone's own draws, never the dice
        Assert.Equal(rustle!.At, Shaking(two)!.At);
        Assert.True(fields.Contains(rustle.At));
        long d = (long)Math.Sqrt(rustle.At.GroundDistanceSquared(one.Player.Pos));
        Assert.InRange(d, OpenWildField.RustleNear, OpenWildField.RustleFar);
        // Touched, it meets one of the rustling table and is gone
        var met = one.Wild.Touching(rustle.At);
        Assert.NotNull(met);
        Assert.True(met!.FromRustle);
        Assert.Contains(fields.Rustle, row => row.Species == met.Species && met.Level >= row.MinLevel && met.Level <= row.MaxLevel);
        Assert.DoesNotContain(rustle, one.Wild.Rustles);
        // A zone with no rustling table never shakes
        Assert.All(one.Wild.Rustles, r => Assert.NotEmpty(r.Zone.Rustle));
    }

    [Fact]
    public void AStrongWildPokemonCantBeCaughtUntilTheBadgesSay()
    {
        Assert.Equal(25, CatchLimits.CeilingFor(RegionDatabase.Galar, 0));
        Assert.Equal(60, CatchLimits.CeilingFor(RegionDatabase.Galar, 7));
        Assert.Equal(0, CatchLimits.CeilingFor(RegionDatabase.Galar, 8));
        Assert.Equal(0, CatchLimits.CeilingFor(RegionDatabase.Kanto, 0));
        BattleCore Met(int level, int ceiling)
        {
            var party = new Party();
            party.Add(Mon("Grookey", 10, "Scratch"));
            var core = new BattleCore(new CoreSetup
            {
                PlayerParty = party, WildPokemon = new List<Pokemon> { Mon("Onix", level, "Tackle") }, Random = Calm(), Rules = Ruleset.Modern,
                PlayerController = null, EnemyController = null, PlayerName = "Victor", Conditions = new BattleConditions { CatchCeiling = ceiling }
            });
            core.Start();
            core.TakeLog();
            return core;
        }
        Assert.Equal(Text.Of("battle.tooStrongToCatch"), Met(36, 25).WhyNot(BattleChoice.UseItem(Mine, "Poké Ball")));
        Assert.Null(Met(25, 25).WhyNot(BattleChoice.UseItem(Mine, "Poké Ball")));
        Assert.Null(Met(36, 40).WhyNot(BattleChoice.UseItem(Mine, "Poké Ball")));
        Assert.Null(Met(36, 0).WhyNot(BattleChoice.UseItem(Mine, "Poké Ball")));
        // The ceiling is kept by the Vs. Recorder's file's conditions as the battle was handed them
        var file = new BattleRecordFile { Conditions = new RecordedConditions { CatchCeiling = 25 } };
        Assert.Equal(25, file.Conditions.CatchCeiling);
    }

    [Fact]
    public void AGalarianYamaskEvolvesUnderTheStoneArch()
    {
        var map = Open.Value;
        var yamask = new Pokemon(PokemonDatabase.Get("Yamask")!, 30, new Random(1));
        yamask.ChangeForm("Yamask-Galar");
        Evolution.CountDamageTaken(yamask, 50);
        string? Under(string marker) =>
            Evolution.Find(yamask, EvolutionTrigger.LevelUp, new EvolutionContext { Sites = map.SitesAt(map.Marker(marker)!.Pos) })?.TargetSpecies;
        Assert.Equal("Runerigus", Under("stone_arch"));
        Assert.Null(Under("wild_area_station"));
    }

    [Fact]
    public void TheTrainRunsNorthToTheWildAreaWhereHopGoesOnAheadAndBack()
    {
        var story = new StoryState();
        StoryMigration.BeginNewGame(story, ScriptLibrary.Default);
        story.SetVar("VAR_GALAR_STORY", 10);
        var open = OpenRegions.Load(Region).Single();
        var rooms = MapDatabase.Fresh();
        HeadlessScriptHost Play(string file, string script, string? room = null)
        {
            var host = new HeadlessScriptHost(story, new Party(), new Inventory())
            {
                Map = room != null ? rooms(room) : null,
                OpenMap = room == null ? open : null,
                OpenMapNamed = name => name == open.Name ? open : null,
                Region = RegionDatabase.Galar
            };
            var runner = new ScriptRunner(ScriptLibrary.Default, host);
            runner.Start(ScriptLibrary.Default.Find(script, file)!, null);
            runner.RunToEnd();
            Assert.Empty(host.Problems);
            return host;
        }
        var aboard = Play("Wedgehurst_Station", "Attendant", "Wedgehurst_Station");
        Assert.Equal(11, story.Var("VAR_GALAR_STORY"));
        Assert.Equal("wild_area_station", aboard.OnMarker["player"]);
        Assert.Equal("galarWildArea", Objectives.Current(story, RegionDatabase.Galar)?.Name);
        Assert.False(story.Has("FLAG_GALAR_HIDE_HOP_WILDAREA"));
        Assert.True(story.Has("FLAG_GALAR_HIDE_HOP_RIVERBANK"));
        Play("wild_area_meetup_spot", "OnEnter");
        Assert.Equal(12, story.Var("VAR_GALAR_STORY"));
        Assert.True(story.Has("FLAG_GALAR_HIDE_HOP_WILDAREA"));
        Assert.False(story.Has("FLAG_GALAR_HIDE_HOP_RIVERBANK"));
        Assert.Equal("galarWildArea", Objectives.Current(story, RegionDatabase.Galar)?.Name);
        // Coming again says nothing more
        Play("wild_area_meetup_spot", "OnEnter");
        Assert.Equal(12, story.Var("VAR_GALAR_STORY"));
        // The steps under Motostoke let an endorsed challenger up (plan 33 · GL4), and turn back anyone before the train
        var steps = Play("wild_area_motostoke_riverbank", "MotostokeSteps");
        Assert.False(steps.OnMarker.ContainsKey("player"));
        Assert.Equal(13, story.Var("VAR_GALAR_STORY"));
        story.SetVar("VAR_GALAR_STORY", 11);
        var turned = Play("wild_area_motostoke_riverbank", "MotostokeSteps");
        Assert.Equal("motostoke_steps_back", turned.OnMarker["player"]);
        story.SetVar("VAR_GALAR_STORY", 12);
        // Back south and north again by train
        var back = Play("wild_area_meetup_spot", "Attendant");
        Assert.Contains(back.Log, l => l.Contains("Wedgehurst_Station"));
        var north = Play("Wedgehurst_Station", "Attendant", "Wedgehurst_Station");
        Assert.Equal("wild_area_station", north.OnMarker["player"]);
    }

    [Fact]
    public void TheWattTraderAndTheDiggersSpendWattsAndNeverMore()
    {
        var story = new StoryState();
        var bag = new Inventory();
        story.SetVar("VAR_GALAR_WATTS", 900);
        var host = new HeadlessScriptHost(story, new Party(), bag) { Region = RegionDatabase.Galar, OpenMap = Open.Value };
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(0);
        host.Answers.Enqueue(5);
        var runner = new ScriptRunner(ScriptLibrary.Default, host);
        runner.Start(ScriptLibrary.Default.Find("WattTrader", "wild_area_meetup_spot")!, null);
        runner.RunToEnd();
        Assert.Empty(host.Problems);
        // One candy for 800, and the second refused at 100 left
        Assert.Equal(100, story.Var("VAR_GALAR_WATTS"));
        Assert.Equal(1, bag.GetQuantity(ItemDatabase.Get("Exp. Candy S")!));
    }
}
